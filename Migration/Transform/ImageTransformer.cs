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
        public List<Q2APost> allPosts; //used for finding images, filtering out image content and replacing them with new urls


        public ImageTransformer()
        {
            extractor = new Extractor();
            loader = new Loader();
            rand = new Random();

            usedIds = extractor.GetQ2ABlobIds();

            foreach (ulong id in usedIds)
                Console.WriteLine(id);
        }


        public void Extract(List<Q2APost> posts) //gets all the posts, removes links and creates the necessary images
        {
            allPosts = posts; //copy over the posts

            foreach (Q2APost p in allPosts)
            {
                p.content = TransformPost(p);
            }
        }

        public void Load() //stores all the new images in 
        {

        }

        //go through the post and create the necessary images and modify the image content
        public string TransformPost(Q2APost p)
        {
            // <img[^>]*(src=")(https:\/\/support.paratext.org)?(\/uploads)[^>]*>
            // width="\d*"
            // height="\d*"
            // /uploads[^"]*

            string content = p.content;
            //find all paratext uploads and migrate them over
            //the match looks for an img that either is a paratext upload, like src="https://support.paratext.org/uploads... or src="/uploads...
            //note "" is just an escape for "
            while(Regex.IsMatch(content, @"<img[^>]*(src="")(https:\/\/support.paratext.org)?(\/uploads)[^>]*>")) //keep migrating until no more matches
            {
                Match m = Regex.Match(content, @"<img[^>]*(src="")(https:\/\/support.paratext.org)?(\/uploads)[^>]*>");
                
                string path = Regex.Match(m.ToString(), @"/uploads[^""]*").ToString(); //i.e. /uploads/default/18492048423.png


                StringBuilder newTag = new StringBuilder();
                newTag.Append("<img src=");

                //if the original tag specified width and height, do the same
                int? width = null;
                int? height = null;
                Match widthSearch = Regex.Match(m.ToString(), @"width=""\d*""");
                if(widthSearch.Success)
                {
                    string text = widthSearch.ToString(); //i.e. width="374"
                    text = text.Trim('\"'); //i.e. width="374
                    text = text.Split('\"').Last(); //i.e. 374
                    width = int.Parse(text); 
                }

                //specify width and height if they are known
                if (width != null)
                    newTag.Append($" width=\"{width}\" ");
                if (height != null)
                    newTag.Append($" width=\"{width}\" ");
            }

            return content;
        }


        public ImageBlob CreateImage(string url, Q2APost p)
        {
            url.Replace(@"/", @"\"); //make sure we are using the right slash for file directories
            string fileName = url.Split(@"\").Last(); //get the last portion i.e. filename.png

            ImageBlob newImage = new ImageBlob();
            newImage.blobid = GenNewId();
            newImage.filename = "Discourse_" + url.Split(@"\").Last(); //add "Discourse_" so we can easily determine migrated posts during deletion
            newImage.format = url.Split(".").Last(); //i.e. png
            
            newImage.created = p.created;
            newImage.userid = p.userid;

            newImage.content = GetImageData(url);

            return newImage;
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
            byte[]? data = null;
            string rootPath = System.Configuration.ConfigurationManager.ConnectionStrings["uploads"].ConnectionString;
            using (FileStream fs = new FileStream(String.Concat(rootPath, path), FileMode.Open, FileAccess.Read))
            {
                data = new byte[fs.Length];
                fs.Read(data, 0, data.Length);
            }
            return data;
        }
    }
}
