using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SupportSiteETL.Migration.Extract;
using SupportSiteETL.Migration.Transform.Models;
using System.Text.RegularExpressions;

namespace SupportSiteETL.Migration.Transform
{

    using DiscoursePost = Dictionary<string, string>; //type alias
    public class Anonymizer
    {
        List<Dictionary<string, string>> DiscourseUsers; //data for the discourse users, used for generating list of words to remove
        Extractor extractor;

        public Anonymizer()
        {
            extractor = new Extractor();
            DiscourseUsers = extractor.GetDiscourseUsers();
        }


        //from a given topic/thread remove all data that relates to anonymity
        public void AnonymizeThread(ref List<Q2APost> posts, List<DiscoursePost> postsD)
        {
            for (int i = 0; i < posts.Count; i++)
            {
                posts[i] = RemoveAvatars(posts[i]); //remove any avatar images in the post
                posts[i] = RemoveLinks(posts[i]); //remove any links to the paratext website
            }

            //remove user names mentioned in posts
        }

        //if the post has anyone's user avatar, remove it
        private Q2APost RemoveAvatars(Q2APost p)
        {
            //remove any image tags that are avatars, anything under user_avatar and is an image must be an avatar.
            //just in case check for avatar class in case the first check missed some
            p.content = Regex.Replace(p.content, @"<img[^>]*support.paratext.org/user_avatar[^>]*>", ""); //<img ... support.paratext.org/user_avatar ... >
            p.content = Regex.Replace(p.content, @"<img[^>]*class=""avatar"">", ""); //note "" is an escape sequence for just "

            return p;
        }

        //if a link (a tag) goes to support.paratext, remove the link,
        private Q2APost RemoveLinks(Q2APost p)
        {
            //split by the hyperlink tag, keep the split delimiter in the parts array
            string[] parts = Regex.Split(p.content, @"(</*a[^>]*>)"); //split on either an opening or closing hyperlink tag
            for(int i = 0; i < parts.Count(); i++)
            {
                if (parts[i].Length >= 2 && parts[i].Substring(0, 2) != "<a") //not beginning of the hyperlink tag, skip by
                    continue;
                //must be the start of the <a> tag
                if ( !parts[i].Contains("support.paratext.org") && !parts[i].Contains("href=\"/") ) //does not link to paratext
                    continue;
                
                //otherwise a tag links to paratext, remove the tag and possibly content
                parts[i] = ""; //remove start of tag <a ...>
                i++; //advance to content
                if (parts[i].Contains("support.paratext.org/") && !parts[i].Contains("<")) //link is plain text and also goes to paratext, remove as well
                {
                    parts[i] = "[Link Removed]";
                }
                i++; //advance to end tag
                parts[i] = ""; //remove end of tag </a>
            }
            p.content = String.Join("", parts);

            return p;
        }
    }
}
