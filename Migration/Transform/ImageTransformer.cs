using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Text.RegularExpressions;


using SupportSiteETL.Migration.Extract;
using SupportSiteETL.Migration.Transform.Models;
using SupportSiteETL.Migration.Load;

namespace SupportSiteETL.Migration.Transform
{
    public class ImageTransformer
    {
        private Extractor extractor;
        private Loader loader;
        private Random rand;

        private List<ulong> usedIds; //blob ids currently in use
        private List<ImageBlob> images; //images to be migrated to q2a

        public ImageTransformer()
        {
            extractor = new Extractor();
            loader = new Loader();
            rand = new Random();

            images = new List<ImageBlob>();

            usedIds = extractor.GetQ2ABlobIds();
        }


        public void Extract(ref List<Q2APost> posts) //gets all the posts, removes links and creates the necessary images
        {
            Console.WriteLine("Migrating post images...");
            foreach (Q2APost p in posts) //transform the data in every post, note posts is passed by reference
                p.content = TransformPost(p);
            Console.WriteLine("Post images migrated!");
        }

        public void Load() //stores all the new images in 
        {
            Console.WriteLine("Loading images...");
            foreach (ImageBlob image in images)
                loader.AddImage(image);
            Console.WriteLine("Images loaded!");
        }

        //go through the post and create the necessary images and modify the image content
        public string TransformPost(Q2APost p)
        {
            string content = p.content;
            //find all paratext uploads/images and migrate them over
            //all images under paratext support fall under the uploads, images, user_avatar and letter_avatar directories
            //we want to ignore avatars so we only port over from images and unploads
            //the match looks for an img that either is a paratext upload, like src="https://support.../uploads..., src="/uploads..., or src="//support
            //note "" is just an escape for "
            while(Regex.IsMatch(content, @"<img[^>]*(src="")((https:)?\/\/support.paratext.org)?(\/uploads|\/images)[^>]*>")) //keep migrating until no more matches
            {
                Match m = Regex.Match(content, @"<img[^>]*(src="")((https:)?\/\/support.paratext.org)?(\/uploads|\/images)[^>]*>");
                string path = "";
                if (m.ToString().Contains("uploads")) //under uploads
                    path = Regex.Match(m.ToString(), @"/uploads[^""]*").ToString(); //i.e. /uploads/default/18492048423.png
                else //under images
                    path = Regex.Match(m.ToString(), @"/images[^""]*").ToString(); //i.e. /images/emoji/18492048423.png

                //create the new image from the given data and add it to the list
                ImageBlob newImage = CreateImage(path, p);
                images.Add(newImage);

                //create the new image tag, with the src (and possibly height and width) attributes
                StringBuilder newTag = new StringBuilder();
                newTag.Append($"<img src=\"/?qa=blob&qa_blobid={newImage.blobid}\" ");
                //if the original tag specified width, height or class, do the same
                int? width = null;
                int? height = null;
                string? classType = null; //for class field
                string? alt = null; //for alt field
                Match widthSearch = Regex.Match(m.ToString(), @"width=""\d*""");
                Match heightSearch = Regex.Match(m.ToString(), @"height=""\d*""");
                Match classSearch = Regex.Match(m.ToString(), @"class=""[^""]*"""); //class="..."
                Match altSearch = Regex.Match(m.ToString(), @"alt=""[^""]*"""); //class="..."
                if (widthSearch.Success)
                {
                    string text = widthSearch.ToString(); //i.e. width="374"
                    text = text.Trim('\"'); //i.e. width="374
                    text = text.Split('\"').Last(); //i.e. 374
                    width = int.Parse(text); 
                }
                if (heightSearch.Success)
                {
                    string text = heightSearch.ToString(); //i.e. height="374"
                    text = text.Trim('\"'); //i.e. height="374
                    text = text.Split('\"').Last(); //i.e. 374
                    height = int.Parse(text);
                }
                if(classSearch.Success)
                {
                    string text = classSearch.ToString(); //i.e. class="emoji"
                    text = text.Trim('\"'); //i.e. class="emoji
                    classType = text.Split('\"').Last(); //i.e. emoji
                }
                if (altSearch.Success)
                {
                    string text = altSearch.ToString(); //i.e. class="emoji"
                    text = text.Trim('\"'); //i.e. class="emoji
                    alt = text.Split('\"').Last(); //i.e. emoji
                }
                //specify width and height if they are known
                if (width != null)
                    newTag.Append($" width=\"{width}\" ");
                if (height != null)
                    newTag.Append($" height=\"{height}\" ");
                if (classType != null)
                    newTag.Append($" class=\"{classType}\" ");
                if (alt != null)
                    newTag.Append($" alt=\"{alt}\" ");
                newTag.Append(">"); //close the tag


                content = content.Replace(m.ToString(), newTag.ToString()); //swap out old discourse image tag for new q2a blob image tag
            }

            return content;
        }

        private ImageBlob CreateImage(string url, Q2APost p)
        {
            string fileName = url.Split(@"/").Last(); //get the last portion i.e. filename.png

            ImageBlob newI = new ImageBlob();
            newI.blobid = GenNewId();

            newI.format = url.Split(".").Last(); //i.e. png
            if(newI.format.Contains('?'))  //special case, i.e. gif?v=6, remove the question mark and ending
            {
                url = url.Substring(0, url.LastIndexOf('?'));
                newI.format = newI.format.Substring(0, newI.format.LastIndexOf('?'));
            }

            //newI.filename = $"Discourse_{newI.blobid}.{newI.format}"; //add "Discourse_" so we can easily determine migrated posts during deletion
            newI.filename = $"Discourse_{images.Count}.{newI.format}"; //DEBUGGING


            newI.created = p.created;
            newI.userid = p.userid;

            newI.content = GetImageData(url);

            return newI;
        }

        //generate a new unique blobid
        private ulong GenNewId()
        {
            ulong id = 0;
            do
            {
                id = (ulong)rand.NextInt64(); //get new id
            } while (usedIds.Contains(id)); //keep generating until we have a valid id, this will almost always take just 1 iteration
            
            usedIds.Add(id); //id is now in use

            return id;
        }


        //from something like /uploads/default/0/2x/... return the binary data from the file
        public byte[] GetImageData(string url)
        {
            byte[] data = new byte[0]; //default empty array
            string rootPath = System.Configuration.ConfigurationManager.ConnectionStrings["uploads"].ConnectionString;
            string fileDirectory = rootPath + url.Replace(@"/", @"\"); //make sure we are using the right slash for file directories
            //i.e. fileDirectory = C:Users\daniel\...298498jfds.png

            if (!File.Exists(fileDirectory)) //file is not local, download it and store it locally and then read
            {
                string folderPath = fileDirectory.Substring(0, fileDirectory.LastIndexOf('\\')); //remove the filename, just the path to the folder
                //i.e. folderPath = "C:Users\daniel\...uploads\2
                if (!Directory.Exists(folderPath)) //create the folder path in case it doesn't already exist
                    Directory.CreateDirectory(folderPath);

                string webPath = @"https://support.paratext.org" + url; //full url of the resource
                Console.Write($"Downloading image: {webPath} ... ");
                try //try downloading from the site
                {
                    using (WebClient client = new WebClient())
                    {
                        client.DownloadFile(webPath, fileDirectory); //store the file in the correct directory
                    }
                    Console.WriteLine("Done"); //to show resource found
                }
                catch (Exception ex) 
                {
                    Console.WriteLine("404"); //to show resource not found
                    using (File.Create(fileDirectory)) { }; //make a blank file here, so we don't re-request the file
                }
            }
            //file is already local or just downloaded locally.
            using (FileStream fs = new FileStream(fileDirectory, FileMode.Open, FileAccess.Read)) //read the image data
            {
                data = new byte[fs.Length];
                fs.Read(data, 0, data.Length);
            }

            return data;
        }
    }
}
