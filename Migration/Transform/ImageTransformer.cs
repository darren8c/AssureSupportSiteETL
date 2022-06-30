using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SupportSiteETL.Migration.Extract;
using SupportSiteETL.Migration.Transform.Models;
using SupportSiteETL.Migration.Load;
using System.Text.RegularExpressions;

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
            //find all paratext uploads and migrate them over
            //the match looks for an img that either is a paratext upload, like src="https://support.paratext.org/uploads... or src="/uploads...
            //note "" is just an escape for "
            while(Regex.IsMatch(content, @"<img[^>]*(src="")(https:\/\/support.paratext.org)?(\/uploads)[^>]*>")) //keep migrating until no more matches
            {
                Match m = Regex.Match(content, @"<img[^>]*(src="")(https:\/\/support.paratext.org)?(\/uploads)[^>]*>");
                string path = Regex.Match(m.ToString(), @"/uploads[^""]*").ToString(); //i.e. /uploads/default/18492048423.png

                //create the new image from the given data and add it to the list
                ImageBlob newImage = CreateImage(path, p);
                images.Add(newImage);

                //create the new image tag, with the src (and possibly height and width) attributes
                StringBuilder newTag = new StringBuilder();
                newTag.Append($"<img src=\"/?qa=blob&qa_blobid={newImage.blobid}\" ");
                //if the original tag specified width and height, do the same
                int? width = null;
                int? height = null;
                Match widthSearch = Regex.Match(m.ToString(), @"width=""\d*""");
                Match heightSearch = Regex.Match(m.ToString(), @"height=""\d*""");
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
                //specify width and height if they are known
                if (width != null)
                    newTag.Append($" width=\"{width}\" ");
                if (height != null)
                    newTag.Append($" height=\"{height}\" ");
                newTag.Append(">"); //close the tag

                content = content.Replace(m.ToString(), newTag.ToString()); //swap out old discourse image tag for new q2a blob image tag
            }

            return content;
        }


        public ImageBlob CreateImage(string url, Q2APost p)
        {
            url = url.Replace(@"/", @"\"); //make sure we are using the right slash for file directories
            string fileName = url.Split(@"\").Last(); //get the last portion i.e. filename.png

            ImageBlob newI = new ImageBlob();
            newI.blobid = GenNewId();
            newI.format = url.Split(".").Last(); //i.e. png
            newI.filename = $"Discourse_{newI.blobid}.{newI.format}"; //add "Discourse_" so we can easily determine migrated posts during deletion

            newI.created = p.created;
            newI.userid = p.userid;

            newI.content = GetImageData(url);

            return newI;
        }

        //generate a new unique blobid
        public ulong GenNewId()
        {
            ulong id = 0;
            do
            {
                id = (ulong)rand.NextInt64(); //get new id

            } while (usedIds.Contains(id)); //keep generating until we have a valid id, this will almost always take just 1 iteration
            
            usedIds.Add(id); //id is now in use

            return id;
        }


        //from something like \uploads\default\0\2x\... return the binary data from the file
        public byte[] GetImageData(string path)
        {
            byte[] data;
            string rootPath = System.Configuration.ConfigurationManager.ConnectionStrings["uploads"].ConnectionString;

            try
            {
                using (FileStream fs = new FileStream(String.Concat(rootPath, path), FileMode.Open, FileAccess.Read)) //attempt to read file
                {
                    data = new byte[fs.Length];
                    fs.Read(data, 0, data.Length);
                }
            }
            catch (Exception) //couldn't read file, make it an empty array
            {
                data = new byte[0];
                //Console.WriteLine($"Couldn't find: {rootPath} {path}");
            }

            return data;
        }
    }
}
