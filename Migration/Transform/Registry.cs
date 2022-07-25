using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration; //for config manager
using SupportSiteETL.Migration.Transform.Models;
using System.Text.RegularExpressions;

namespace SupportSiteETL.Migration.Transform
{
    //handles generating the banned word list based on the registry file
    //if the registry file isn't found, this check will just be skipped
    public class Registry
    {
        public Dictionary<string, string> censoredWords; //go from searchWord to replaceWord
        public bool found; //whether or not the registry is found
        
        public Registry()
        {
            found = false; //should be set true in GenerateWordList;
            GenerateWordList();
        }

        //load the word list from file and filter out
        private void GenerateWordList()
        {
            string filePath = ConfigurationManager.ConnectionStrings["registry"].ConnectionString;

            if (File.Exists(filePath))
            {
                Console.WriteLine("Generating registry word List...");
                found = true;
            }
            else //registry not found just return
            {
                Console.WriteLine("Registry not found! Skipping this step.");
                return;
            }

            censoredWords = new Dictionary<string, string>();

            var lines = File.ReadLines(filePath);
            foreach(string line in lines)
            {
                censoredWords[line] = "[Censored Name]";
            }
        }

        //remove entries in the dictionary if they are comprised of only popular words
        //i.e. publishing assistant should be removed but John Doe or Computer User1 wouldn't be
        public void ModifyListByPosts(List<Q2APost> posts)
        {
            List<string> keys = censoredWords.Keys.ToList();
            foreach(string key in keys)
            {
                bool valid = false; //if the key is okay, assume false until proven otherwise
                
                Regex searchRegex = new Regex(@$"(?i)\b{key}\b");
                int postCount = 0; //how many posts where there is 1 or more match

                foreach(Q2APost post in posts) //count the matches in all posts (up to 10 matches)
                {
                    if(searchRegex.IsMatch(post.content))
                        postCount++;
                    if (postCount >= 10) //word is too populars
                        break; //no need to keep searching posts
                }
                //if the case insensitve version is not popular enough
                if (postCount < 10)
                    valid = true; //word is okay (rare enough)
                
                if(!valid) //word is too common
                {
                    //Console.WriteLine($"\t{key} will not be searched for!");
                    censoredWords.Remove(key);
                }
            }
        }
    }
}
