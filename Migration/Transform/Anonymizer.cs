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

    using BlockIP = KeyValuePair<SimpleIP, SimpleIP>; //begin and end for an ip block (end is inclusive)

    public class Anonymizer
    {
        private List<User> discourseUsers; //data for the discourse users, used for generating list of words to remove
        public List<Q2AUser> q2aUsers; //q2a users, this is set by Transferer sometime after the constructor
        public Dictionary<int, int> userIdMap; //go from discourse to q2a user id, WARNING this is set by Transferer sometime after the constructor
        public Dictionary<string, string> mentionList; //old username to new handle, is set externally by Transferer after constructor

        private Dictionary<int, bool> sensitiveUser; //go from discourse id to sensitive region or not
        List<KeyValuePair<BlockIP, bool>> blockSensitivities; //stores ip ranges and whether they are a sensitive region or not


        private Extractor extractor;

        public Anonymizer()
        {
            extractor = new Extractor();
            discourseUsers = extractor.GetDiscourseUsers();

            Console.WriteLine("Determining sensitive users...");
            PopulateSensitiveList(); //sets sensitiveUser and blockSensitivities from the files
            Console.WriteLine("Sensitive users identified!");
        }

        //from a given topic/thread remove all data that relates to anonymity, note at this time userIdMap should be set already
        public void AnonymizeThread(ref List<Q2APost> posts, List<DiscoursePost> postsD)
        {
            for (int i = 0; i < posts.Count; i++)
            {
                posts[i] = RemoveAvatars(posts[i]); //remove any avatar images in the post
                posts[i] = RemoveLinks(posts[i]); //remove any links to the paratext website
                posts[i] = RemoveEmails(posts[i]);
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
            //note (?<!\<) and (?!\>) lookbehind/ahead and makes sure < > / is not where the word break starts/ends
            //this avoids removing a tag, i.e. <p ... or </p> or image /uploads/b
            return @"(?i)\b(?<![\<\/])" + word + @"(?!\>)\b"; //any case version of the search term when it appears as a full word
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
                cw[dUser["username"]] = q2aUser.handle;
                
                //removing mentiosn and emails are handled seperately and will globally remove all mentions and emails
                //cw[dUser["email"]] = "[EMAIL REMOVED]";
                //cw[$"@{dUser["username"]}"] = $"@{q2aUser.handle}"; //add @ versions as it is common to @ users, i.e. @john_doe


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
                return parts.GetRange(0, 2); //cuts off: SIL International, Language Technology Consultant

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

        //remove ANY email mentioned in a post
        private Q2APost RemoveEmails(Q2APost p)
        {
            //email is like [space] name (chars symbols numbers) @ (chars numbers -) . (chars numbers -) (chars case insensitive with (?i))
            p.content = Regex.Replace(p.content, @"(?i)\b[a-z.1-9!#$%&'*+\-/=?^_`{|}~]+@[a-z1-9-]+\.[a-z1-9-]+\b", "[Email Removed]");
            return p;
        }


        //old username to new username i.e. danielMarch -> anon123456, requires userIdMap to be set
        public Dictionary<string, string> SetMentionList()
        {
            mentionList = new Dictionary<string, string>();
            foreach(User dUser in discourseUsers)
            {
                Q2AUser qUser = q2aUsers.First(u => u.userId == userIdMap[int.Parse(dUser["id"])]); //matching q2a user
                mentionList[dUser["username"]] = qUser.handle;
            }
            return mentionList;
        }

        //remove any @username mentions and replace them, requires mentionList to be set
        public void RemoveMentions(ref List<Q2APost> posts)
        {
            foreach(KeyValuePair<string,string> entry in mentionList)
            {
                Regex searchKey = new Regex(@"(?i)\s@" + entry.Key + @"\b", RegexOptions.Compiled);
                foreach(Q2APost post in posts)
                    post.content = searchKey.Replace(post.content, $" @{entry.Value}"); //swap old username for 
            }
        }


        private void PopulateSensitiveList()
        {
            Dictionary<string, bool> locationMapper = new Dictionary<string, bool>(); //go from country code to sensitive status
            //fill in the lookup table
            var lines = File.ReadLines("Resources/CountryStatuses.csv");
            foreach (string line in lines)
            {
                string[] data = line.Split(',');
                if (data.Length != 2) //there should always be 2 fields (countryCode, blocked/unblocked)
                {
                    Console.WriteLine("Error, CountryStatuses.csv is not in the correct format!");
                    return;
                }
                locationMapper.Add(data[0], data[1] == "blocked");
            }

            blockSensitivities = new List<KeyValuePair<BlockIP, bool>>();
            //fill in from look up table
            lines = File.ReadLines("Resources/LookupIP.csv");
            foreach (string line in lines)
            {
                string[] data = line.Split(',');
                if (data.Length != 3) //there should always be 3 fields (ipRangeStart, ipRangeStart, countryCode)
                {
                    Console.WriteLine("Error, LookupIP.csv is not in the correct format!");
                    return;
                }
                BlockIP blockIP = new BlockIP(new SimpleIP(data[0]), new SimpleIP(data[1])); //the block for this line
                string countryCode = data[2];
                bool sensitive = false;
                if (locationMapper.ContainsKey(countryCode))
                    sensitive = locationMapper[countryCode]; //set from map, otherwise, it will be marked not sensitive

                blockSensitivities.Add(new KeyValuePair<BlockIP, bool>(blockIP, sensitive)); //add to the list the range and sensitivity status
            }

            //go through each user id and find out if they are sensitive, this ensures we only check once per user
            sensitiveUser = new Dictionary<int, bool>();
            foreach (User u in discourseUsers)
            {
                string ip1 = u["ip_address"]; //first ip to check
                string ip2 = u["registration_ip_address"]; //second ip to check

                bool sensitive = IsSensitiveIP(ip1) || IsSensitiveIP(ip2);
                sensitiveUser.Add(int.Parse(u["user_id"]), sensitive);
            }
        }
        //if the discourse user's ip was from a sensitive location, determined from the mapping file
        public bool IsSensitiveUser(int discId)
        {
            return sensitiveUser[discId]; //just look up the mapping to see if they are sensitive or not.
        }
        //from the tables we have determine if an ip is sensitive or not
        private bool IsSensitiveIP(string ipStr)
        {
            if (ipStr == "") //no need to check if not an actual ip
                return false;

            SimpleIP ip = new SimpleIP(ipStr); //note comparison operators are defined on ipStr

            //find the element where the ip corresponds to that block, and return if sensitive or not
            //note structure of blockSensitivities, Pair< Pair<ipStart, ipEnd>, isSensitive >
            //list is long, and linear lookups for 1000 users is slow, so use binary search
            int start = 0;
            int end = blockSensitivities.Count; //note end is not inclusive
            while (start != end)
            {
                int mid = (start + end) / 2;
                if (blockSensitivities[mid].Key.Key <= ip && ip <= blockSensitivities[mid].Key.Value) //valid match
                    return blockSensitivities[mid].Value; //return whether sensitive or not
                else if (ip < blockSensitivities[mid].Key.Key) //too high, eliminate last half
                    end = mid;
                else //too low, eliminate first half
                    start = mid + 1;
            }
            //element wasn't in list, this should never happen, default to not sensitive
            return false;
        }
    }
}
