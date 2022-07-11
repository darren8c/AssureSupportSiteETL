using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.RegularExpressions;
using SupportSiteETL.Migration.Transform.Models;

namespace SupportSiteETL.Migration.Transform
{
    using Tags = List<string>;

    //go through all questions and determine tags based on the words in the content and titles compared to the global appearance of those words
    public class AutoTagger
    {
        List<Q2APost> allPosts;

        Dictionary<string, int> contentCountGlobal; //how often a word appears in all questions
        Dictionary<int, Dictionary<string, int>> contentCountLocal; //how often a word appears in each question (key is postid)
        Dictionary<int, HashSet<string>> titleLocal; //set of words that appear in a title
        Dictionary<int, int> totalWordsLocal; //key: postid, value: total words in post (only in content)
        int totalWordsGlobal; //total words in all posts (only content)

        Dictionary<int, Tags> localTags;

        //global vars for tweaking tag assignment settings
        int MIN_LOCAL_COUNT = 2; //how many times a word must be in a post to be considered as a tag
        int TITLE_BOOST = 2; //how much of boost the word appearing in the title is, (adds to word count during % calculation)
        int MIN_TAG_POSTS = 3; //tags won't be used unless this many posts have this tag as a possiblity
        double MAX_APPEAR_CHANCE = .03; //if a word appears more than this % of the time, we don't consider it a possiblity
        int MAX_TAG_COUNT = 5; //max tags per post
        double MIN_SCORE = 2; //minimum abnormal score, i.e. 2 means word appears twice as much as normal

        public AutoTagger()
        {
            //the tranformer class will set allPosts (by reference) after data is loaded
            contentCountGlobal = new Dictionary<string, int>();
            contentCountLocal = new Dictionary<int, Dictionary<string, int>>();
            titleLocal = new Dictionary<int, HashSet<string>>();
            totalWordsLocal = new Dictionary<int, int>();
            int totalWordsGlobal = 0;

            localTags = new Dictionary<int, Tags>();
        }

        //go through the posts and assign tags automatically
        public void Extract(ref List<Q2APost> posts)
        {
            allPosts = posts; //assign is by reference
            foreach(Q2APost p in allPosts)
                if(p.type == "Q") //only index if it is a question
                    IndexPost(p);

            //get canidate tags for each post
            foreach (Q2APost p in allPosts)
                if (p.type == "Q") //only index if it is a question
                {
                    //Console.WriteLine(p.postid);
                    //Console.WriteLine(StripHTML(p.content));
                    //Console.WriteLine();
                    SetPossibleTags((int)p.postid);
                }

            //finalize the list of tags after the initial pass
            RefineTags();

            //now assign tags
            foreach (Q2APost p in allPosts)
                if(p.type == "Q") //only questions have tags
                    p.tags = GetTagString((int)p.postid);
        }
        //auto tagger has no extract as it only modifies the post list and doesn't write to the database

        //go through the title and content of the post and talley
        private void IndexPost(Q2APost p)
        {
            int id = (int)p.postid;
            //index the title
            List<string> titleWords = ParseText(p.title);
            titleLocal[id] = new HashSet<string>(titleWords); //a set of the title words

            //index the content
            List<string> contentWords = ParseText(StripHTML(p.content)); //text without any html formatting
            contentCountLocal[id] = new Dictionary<string, int>();
            foreach(string word in contentWords)
            {
                //add to local list
                if (!contentCountLocal[id].ContainsKey(word)) //new word
                    contentCountLocal[id][word] = 1;
                else
                    contentCountLocal[id][word]++; //add to count

                //add to global list
                if (!contentCountGlobal.ContainsKey(word)) //new word
                    contentCountGlobal[word] = 1;
                else
                    contentCountGlobal[word]++; //add to count
            }
            totalWordsLocal[id] = contentWords.Count; //update word counts
            totalWordsGlobal += contentWords.Count;

            /* debugging
            Console.WriteLine(StripHTML(p.content) + "\n");
            Console.WriteLine($"Local: {totalWordsLocal[id]}, Total: {totalWordsGlobal}");
            foreach (string word in titleLocal[id])
                Console.Write(word + " ");
            Console.WriteLine(); //spacer
            foreach (KeyValuePair<string, int> pair in contentCountLocal[id])
                Console.WriteLine($"{pair.Key}: {pair.Value}");
            Console.WriteLine("\n");
            Console.ReadLine();
            //*/
        }

        //set a list of possible tags based on popularity for a post id
        private void SetPossibleTags(int id)
        {
            localTags[id] = new Tags();

            Dictionary<string, double> localScore = new Dictionary<string, double>(); //the % the word takes up in the content in the post
            foreach(KeyValuePair<string, int> localWord in contentCountLocal[id])
            {
                int localCount = localWord.Value;
                if (titleLocal[id].Contains(localWord.Key)) //the title has this word too, give a boost to the word count
                    localCount += TITLE_BOOST; //add to count

                localScore[localWord.Key] = ((double)localCount) / totalWordsLocal[id]; //% of the words in this post's content
            }

            //relative comparison of how often the words shows up in the post compared to the global %
            Dictionary<string, double> abnormalScore = new Dictionary<string, double>();
            foreach(KeyValuePair<string, double> localWord in localScore)
                abnormalScore[localWord.Key] = localWord.Value / CalculateWordChance(localWord.Key); // localChance / globalhance


            abnormalScore = abnormalScore.OrderBy(p => p.Value).Reverse().ToDictionary(a => a.Key, a => a.Value); //reorder the dictionary descending score

            foreach(string word in abnormalScore.Keys)
            {
                //appears the minimum time in the post, high enough score, and not too common of a word
                bool validWord = contentCountLocal[id][word] > MIN_LOCAL_COUNT &&
                    abnormalScore[word] > MIN_SCORE && CalculateWordAppearance(word) < MAX_APPEAR_CHANCE;

                if (validWord) //all the check are passed add this to the list of possible tags
                    localTags[id].Add(word);

                //Console.WriteLine($"{word}, {localScore[word]}, {abnormalScore[word]}, {CalculateWordAppearance(word)}");
            }

            //Console.WriteLine();
            //Console.ReadLine();
        }

        //from wordsTransformer, parse text into words (all lowercase), minor modification to alt chars (- and \)
        private List<string> ParseText(string title)
        {
            List<string> words = new List<string>();
            string altChars = @"@#$_\-"; //alternative chars (besides letteers and nums) that can be a part of a word
            string currWord = "";
            foreach (char c in title)
            {
                if (char.IsLetter(c)) //append lowercase version
                    currWord = currWord + char.ToLower(c);
                else if (char.IsDigit(c) || char.IsSymbol(c) || altChars.Contains(c)) //digit or @#$&_
                    currWord = currWord + c;
                else //on split letter e.g. space ! . ,
                {
                    if (currWord.Length > 0) //only add if non empty
                        words.Add(currWord);
                    currWord = ""; //reset
                }
                if (currWord.Length == 80) //words can only be 80 digits long, split here as well
                {
                    words.Add(currWord);
                    currWord = ""; //reset
                }
            }
            if (currWord != "") //add final word
                words.Add(currWord);
            return words;
        }

        //finalize the tag list after we do the initial pass, remove canidates if they are too abnormal
        private void RefineTags()
        {
            Dictionary<string, int> tagCounts = new Dictionary<string, int>(); //each tag and how many different posts have it as a canidate
            foreach(Tags tags in localTags.Values) //go through every post's tags
            {
                foreach(string tag in tags) //every tag in post (note tags are unique)
                {
                    if (!tagCounts.ContainsKey(tag)) //new tag
                        tagCounts[tag] = 1;
                    else
                        tagCounts[tag]++;
                }
            }
            //talley of tag canidates is complete now remove if threshold is not met
            foreach (KeyValuePair<int,Tags> tagsPair in localTags) //go through every post
            {
                Tags tagListCopy = new Tags(tagsPair.Value); //make a copy of the tags, do this instead of removing to avoid indexing problems
                localTags[tagsPair.Key].Clear(); //empty out the list

                foreach (string tag in tagListCopy) //though every tag in this post
                    if (tagCounts[tag] >= MIN_TAG_POSTS) //bases mark for enough posts with this tag
                        localTags[tagsPair.Key].Add(tag);
            }

            //lastly if a post has too many tags cut it off at
            foreach (KeyValuePair<int, Tags> tagsPair in localTags) //go through every post
                if (tagsPair.Value.Count > MAX_TAG_COUNT)
                    tagsPair.Value.RemoveRange(MAX_TAG_COUNT, tagsPair.Value.Count - MAX_TAG_COUNT); //cut off remaining elements
        }

        //return the list of tags as a string, if no tags return null
        public string? GetTagString(int id)
        {
            if (localTags[id].Count == 0) //empty
                return null;

            string text = "";
            for (int i = 0; i + 1 < localTags[id].Count; i++) //all but last
                text += localTags[id][i] + ",";
            text += localTags[id].Last(); //assign last tag (no comma)

            return text;
        }

        //remove the tags from html formatting
        //from: https://www.puresourcecode.com/dotnet/csharp/how-to-strip-all-html-tags-and-entities-and-get-clear-text/
        private string StripHTML(string html)
        {
            html = Regex.Replace(html, @"&[^\s]+;", ""); //replace html entities
            return Regex.Replace(html, @"<.*?>", ""); //replace any tags with empty string
        }

        private double CalculateWordChance(string word)
        {
            return ((double)contentCountGlobal[word]) / totalWordsGlobal; //% of words in content globally
        }

        //% of posts that contain this word
        private double CalculateWordAppearance(string word)
        {
            int count = 0;
            foreach(var post in contentCountLocal.Values) //each post's Dictionary<string, word> 
                if(post.ContainsKey(word))
                    count++;
            return ((double)count) / contentCountLocal.Count(); // appearances / # of posts
        }
    }
}
