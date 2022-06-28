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
    //aliases to improve readability
    using DiscoursePost = Dictionary<string, string>; //type alias
    using CensoredWords = Dictionary<string, string>; //key: censored word (i.e. name), value: what to replace it with
    using User = Dictionary<string, string>; //alias for the user data from the databases
    public class Anonymizer
    {
        private List<User> discourseUsers; //data for the discourse users, used for generating list of words to remove
        public List<Q2AUser> q2aUsers; //q2a users, this is set by Transferer sometime after the constructor
        public Dictionary<int, int> userIdMap; //go from discourse to q2a user id, WARNING this is set by Transferer sometime after the constructor
        
        private Extractor extractor;

        public Anonymizer()
        {
            extractor = new Extractor();
            discourseUsers = extractor.GetDiscourseUsers();
        }

        //if the discourse user's ip was from a sensitive location, uses a xml mapping file
        public bool IsSensitiveUser(int discId)
        {
            return false;
        }


        //from a given topic/thread remove all data that relates to anonymity, note at this time userIdMap should be set already
        public void AnonymizeThread(ref List<Q2APost> posts, List<DiscoursePost> postsD)
        {
            for (int i = 0; i < posts.Count; i++)
            {
                posts[i] = RemoveAvatars(posts[i]); //remove any avatar images in the post
                posts[i] = RemoveLinks(posts[i]); //remove any links to the paratext website
            }

            //remove user names mentioned in posts
            CensoredWords cw = GetWordList(postsD);
            for(int i = 0; i < posts.Count; i++) //censor the content of each post
            {
                posts[i].content = CensorContent(posts[i].content, cw); //censor the main text
                posts[i].title = CensorContent(posts[i].title, cw); //censor the title just in case
            }
        }            


        //censor the words in the content
        private string CensorContent(string content, CensoredWords cw)
        {
            content = RemovePhone(content, cw); //remove phone numbers in the post
            
            foreach(KeyValuePair<string, string> word in cw)
            {
                string searchTerm = GetExpressionFromWord(word.Key); //any case version of the searchWord when it appears as a full word
                content = Regex.Replace(content, searchTerm, word.Value); //swap the term out for it's replacement
            }

            return content;
        }
        
        //go from a search word i.e. "John" -> the regex expression search term
        private string GetExpressionFromWord(string word)
        {
            //special characters in regular expression need to be replaced with \\, i.e. ? -> \?
            char[] escapeChars = { '?', '*', '+', '|', '^', '$', '.', '\\', '[', ']', '(', ')', '{', '}' };
            foreach (char c in escapeChars) //some chars may be special chars in regex, replace so it works properly
                word.Replace(c.ToString(), "\\" + c); //change to escape sequence
            return @"(?i)\b" + word + @"\b"; //any case version of the search term when it appears as a full word
        }

        //Remove phone numbers, must be their own "word" of 6-15 digits, can have spaces ()'s, hyphens or periods in between
        //there could be many false positives (e.g. version numbers, dates, etc.) so we have to verify they are phone numbers
        private string RemovePhone(string content, CensoredWords cw)
        {
            int windowSize = 50; //how many chars on each side of match to look for "phone"

            List<string> numbersToRemove = new List<string>(); //a list of numbers to remove
            
            MatchCollection matches = Regex.Matches(content, @"\b(\d[\s-.\(\)]*){7,15}\b"); //every possible phone # match
            foreach(Match match in matches)
            {
                if(match.Success) //a match, but must be verified before removal
                {
                    int matchLocation = match.Index; //location of the match
                    //make sure start and end are in range to catch errors if match is near the end or beginning of text.
                    int startIndex = Math.Max(0, matchLocation - windowSize);
                    int endIndex = Math.Min(content.Length, matchLocation + windowSize + match.Length);
                    string surroundingText = content.Substring(startIndex, endIndex - startIndex); // [ windowSize |match| windowSize ]


                    //find if there is any sensitive info around the match in question
                    //this includes phone related words (any case) is nearby the match or any censored words
                    bool nearbyMatch = Regex.IsMatch(surroundingText, @"\b(?i)(phone|cell|tel)\b");
                    foreach (KeyValuePair<string, string> word in cw) //check if any censored words appear nearby
                    {
                        if (nearbyMatch) //if we already found a match stop searching
                            break;
                        if(Regex.IsMatch(surroundingText, GetExpressionFromWord(word.Key)))
                            nearbyMatch = true;
                    }
                    if (nearbyMatch) //the number appears to be a real phone number and should be removed
                        numbersToRemove.Add(match.ToString()); //if we remove now it will mess up the index of the other matches, remove later           
                }
            }
            foreach (string num in numbersToRemove) //go through each phone number that shows up and remove them
                content = content.Replace(num, "[Phone Removed]");

            return content;
        }


        //generate a list of censored words based on the users in the thread
        private CensoredWords GetWordList(List<DiscoursePost> postsD)
        {
            CensoredWords cw = new CensoredWords();

            List<int> discUsers = new List<int>(); //the discourse user ids of people on this thread.
            foreach(DiscoursePost p in postsD)
                if (!discUsers.Contains(int.Parse(p["user_id"])))
                    discUsers.Add(int.Parse(p["user_id"])); //add to list of known users for this thread

            foreach (int discId in discUsers) //go through each user id and generate a list of banned words
            {
                User dUser = discourseUsers.First(u => int.Parse(u["id"]) == discId);
                Q2AUser q2aUser = q2aUsers.First(u => u.userId == userIdMap[discId]);

                //mapping from a censored word to the replacement (censored_word) -> replace_with
                cw[dUser["email"]] = "[EMAIL REMOVED]";
                cw[dUser["username"]] = q2aUser.handle;
                cw[$"@{dUser["username"]}"] = $"@{q2aUser.handle}"; //add @ versions as it is common to @ users, i.e. @john_doe


                //for both the username and name, each piece is a censored word, i.e. for "John Doe" both "John" and "Doe" should be removed

                List<string> names = SplitName(dUser["username"], discId); //split the usernames in parts and add the parts
                if (names.Count > 0)
                    cw[names[0]] = q2aUser.handle; //first name maps to handle
                for (int i = 1; i < names.Count; i++)
                    cw[names[i]] = "";

                if (dUser["name"] == "") //skip censored words related to names if name is blank
                    continue;

                cw[dUser["name"]] = q2aUser.handle; //name corresponding to the full name of the user
                //add the different parts of the name, normally will be like {"John", "Doe"}, or {"Jane", "G.", "Doe"}
                names = SplitName(dUser["name"], discId);
                if(names.Count > 0)
                    cw[names[0]] = q2aUser.handle; //first name maps to handle
                for(int i = 1; i < names.Count; i++)
                    cw[names[i]] = "";
            }
            return cw;
        }

        //break a name into pieces, normally will be like {"John", "Doe"}, sometimes {"Jane", "G.", "Doe"}
        private List<string> SplitName(string name, int id)
        {

            List<string> parts = new List<string>();
            
            if (id == 1047 || id == 970) //special case for 2 users, none of these words should be censored
                return parts;


            string part = "";
            //break in parts, split on spaces or on upper case
            //i.e. "Marco C. Polo" -> {"Marco", "C." "Polo"}, "GeorgeWashington" -> {"George", "Washington"}
            char[] splitChars = { ' ', '_', '.', '-', '(', ')', '[', ']', '{', '}' }; //characters to split on
            foreach (char c in name)
            {
                if (splitChars.Contains(c)) //split on space or similar chars
                {
                    if (part != "")
                        parts.Add(part);
                    part = "";
                }
                else if (char.IsUpper(c) && part != "" && !Char.IsUpper(part.Last())) //split on upper, only when prev wasn't upper, i.e. SIL isn't split
                {
                    parts.Add(part);
                    part = "" + c;
                }
                else //not space and not upper, normal case
                {
                    part += c;
                }
            }
            if (part != "") //add the final part
                parts.Add(part);

            if (id == 116) //special case 4 user, only first 2 parts relate to anonymity
                return parts.GetRange(0, 2);

            return parts;
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
