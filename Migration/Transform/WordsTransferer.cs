using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SupportSiteETL.Migration.Extract;
using SupportSiteETL.Migration.Load;

namespace SupportSiteETL.Migration.Transform
{
    //qa makes posts searchable by filling tables of words and how they appear in titles, tags, and content
    //after all posts are migrated to q2a, all posts need to be searched and these tables need to be populated.

    //tables to be updated
    //qa_tagwords: postid, wordid |
    //qa_posttags: postid, wordid, postcreated | essentially the same as tagwords, only a date added
    //qa_titlewords: postid, wordid |
    //qa_contentwords: postid, wordid, count, type, questionid | (type should be Q, C, ... etc.), questionid is just the postid of the main question
    //qa_words: wordid, word, titlecount, contentcount, tagwordcount, tagcount | (does translation from wordid to word), each count refers to number 
    //  of posts with a appearance (i.e. a post with 7 "the"'s only adds 1 to this count, not 7).

    //note all words are all lowercase (and seperate on a space or hyphen).

    //Defining a couple simple types below to make tracking easier
    using Post = Dictionary<string, string>;
    using TitleWordsEntry = TagWordsEntry; //tables are in the same format, just an alias name
    public class PostTagsEntry
    {
        public int postid;
        public int wordid;
        public DateTime postcreated;
        public PostTagsEntry() { } //default constructor
    }
    public class TagWordsEntry
    {
        public int postid;
        public int wordid;
        public TagWordsEntry() { } //default constructor
    }
    public class ContentWordsEntry
    {
        public int postid;
        public int wordid;
        public int count;
        public string type;
        public int questionid;
        public ContentWordsEntry() { } //empty constructor
    }
    public class WordEntry
    {
        public int wordid;
        public string word;
        public int titlecount;
        public int contentcount;
        public int tagwordcount;
        public int tagcount;
        public WordEntry()
        {
            wordid = 0;
            word = "";
            titlecount = 0;
            contentcount = 0;
            tagwordcount = 0;
            tagcount = 0;
        }
    }


    public class WordsTransferer
    {
        private Extractor extractor;
        private Loader loader;
        private Deleter deleter;

        private Dictionary<string, int> wordMap; //go from word (e.g. "the") to the id (e.g. 7)
        private Dictionary<int, WordEntry> wordDataMap; //from word id to the entry including counts

        private List<Post> posts; //all the posts from q2a

        public WordsTransferer()
        {
            extractor = new Extractor();
            loader = new Loader();
            deleter = new Deleter();

            wordMap = new Dictionary<string, int>();
            wordDataMap = new Dictionary<int, WordEntry>();
        }

        //note that words transferer has no load function, since it essentially only updates the tables after the rest of the ETL is done

        //load/update the tables on q2a so searching works, this should be done last essentially in the ETL
        public void Load()
        {
            deleter.DeleteWordTables(); //clear the tables since we will be writing over them

            posts = extractor.GetQ2APosts(); //get all the posts from q2a


            List<TitleWordsEntry> tagWords = new List<TitleWordsEntry>(); //for qa_tagwords
            List<PostTagsEntry> postTags = new List<PostTagsEntry>(); //for qa_posttags
            List<TitleWordsEntry> titleWords = new List<TitleWordsEntry>(); //for qa_titlewords
            List<ContentWordsEntry> contentWords = new List<ContentWordsEntry>(); //for qa_contentwords

            Console.WriteLine("Gathering word table data...");

            foreach (Post post in posts)
            {
                //add to each of the lists
                tagWords.AddRange(GetTags(post));
                postTags.AddRange(GetPostTags(post));
                titleWords.AddRange(GetTitleWords(post));
                contentWords.AddRange(GetContentWords(post));
            }
            List<WordEntry> words = wordDataMap.Values.ToList();



            //now write to the tables
            Console.WriteLine("Saving word tables to q2a... (this may take a while)");
            loader.AddToWordTables(words, contentWords, postTags, tagWords, titleWords);
            Console.WriteLine("Word tables updated!");

        }

        //get the tags from a post
        public List<TitleWordsEntry> GetTags(Post p)
        {
            List<TitleWordsEntry> tags = new List<TitleWordsEntry>();

            int postid = int.Parse(p["postid"]);
            List<string> namesSeen = new List<string>(); //we aren't concerned with duplicates
            foreach (string tagName in p["tags"].Split(","))
            {
                //skip over words we have already done
                if (namesSeen.Contains(tagName) || tagName == "")
                    continue;
                namesSeen.Add(tagName);

                TitleWordsEntry tag = new TitleWordsEntry();
                tag.postid = postid;
                if (wordMap.ContainsKey(tagName)) //word is already known
                {
                    tag.wordid = wordMap[tagName];
                    wordDataMap[tag.wordid].tagcount++; //update the word entry data
                }
                else //new word, add to map data
                {
                    WordEntry newWord = new WordEntry();
                    newWord.wordid = wordMap.Count() + 1; //i.e. first is id 1, second 2, ...
                    newWord.word = tagName;
                    newWord.tagcount = 1;

                    tag.wordid = newWord.wordid;

                    wordMap.Add(newWord.word, newWord.wordid);
                    wordDataMap.Add(newWord.wordid, newWord);
                }
                tags.Add(tag);
            }
            return tags;
        }

        //get the post tags from a post (essentially the same as gettags plus a data)
        public List<PostTagsEntry> GetPostTags(Post p)
        {
            List<PostTagsEntry> tags = new List<PostTagsEntry>();

            int postid = int.Parse(p["postid"]);
            List<string> namesSeen = new List<string>(); //we aren't concerned with duplicates
            foreach (string tagName in p["tags"].Split(","))
            {
                //skip over words we have already done
                if (namesSeen.Contains(tagName) || tagName == "")
                    continue;
                namesSeen.Add(tagName);

                PostTagsEntry tag = new PostTagsEntry();
                tag.postid = postid;
                tag.postcreated = DateTime.Parse(p["created"]);
                if (wordMap.ContainsKey(tagName)) //word is already known
                {
                    tag.wordid = wordMap[tagName];
                    wordDataMap[tag.wordid].tagwordcount++; //update the word entry data
                }
                else //new word, add to map data
                {
                    WordEntry newWord = new WordEntry();
                    newWord.wordid = wordMap.Count() + 1; //i.e. first is id 1, second 2, ...
                    newWord.word = tagName;
                    newWord.tagwordcount = 1;

                    tag.wordid = newWord.wordid;

                    wordMap.Add(newWord.word, newWord.wordid);
                    wordDataMap.Add(newWord.wordid, newWord);
                }
                tags.Add(tag);
            }
            return tags;
        }

        //get the words entries in a title
        public List<TitleWordsEntry> GetTitleWords(Post p)
        {
            List<TitleWordsEntry> words = new List<TitleWordsEntry>();

            int postid = int.Parse(p["postid"]);
            List<string> namesSeen = new List<string>(); //we aren't concerned with duplicates

            foreach (string wordName in ParseText(p["title"]))
            {
                //skip over words we have already done
                if (namesSeen.Contains(wordName))
                    continue;
                namesSeen.Add(wordName);

                TitleWordsEntry word = new TitleWordsEntry();
                word.postid = postid;
                if (wordMap.ContainsKey(wordName)) //word is already known
                {
                    word.wordid = wordMap[wordName];
                    wordDataMap[word.wordid].titlecount++; //update the word entry data
                }
                else //new word, add to map data
                {
                    WordEntry newWord = new WordEntry();
                    newWord.wordid = wordMap.Count() + 1; //i.e. first is id 1, second 2, ...
                    newWord.word = wordName;
                    newWord.titlecount = 1;

                    word.wordid = newWord.wordid;

                    wordMap.Add(newWord.word, newWord.wordid);
                    wordDataMap.Add(newWord.wordid, newWord);
                }
                words.Add(word);
            }
            return words;
        }

        //get the words entries in the content
        public List<ContentWordsEntry> GetContentWords(Post p)
        {
            List<ContentWordsEntry> words = new List<ContentWordsEntry>();

            int postid = int.Parse(p["postid"]);
            int questionid = FindParentQuestion(p);
            string type = p["type"].Substring(0, 1); //only the first char, since only Q, A, C are accepted.
            List<string> namesSeen = new List<string>(); //we are concerned with duplicates but we will count how many times each word shows up

            List<string> wordNamesList = ParseText(StripHTML(p["content"]));
            foreach (string wordName in wordNamesList)
            {
                //skip over words we have already done
                if (namesSeen.Contains(wordName))
                    continue;
                namesSeen.Add(wordName);

                ContentWordsEntry word = new ContentWordsEntry();
                word.postid = postid;
                word.count = CountElement(wordName, wordNamesList);
                word.type = type;
                word.questionid = questionid;
                if (wordMap.ContainsKey(wordName)) //word is already known
                {
                    word.wordid = wordMap[wordName];
                    wordDataMap[word.wordid].contentcount++; //update the word entry data
                }
                else //new word, add to map data
                {
                    WordEntry newWord = new WordEntry();
                    newWord.wordid = wordMap.Count() + 1; //i.e. first is id 1, second 2, ...
                    newWord.word = wordName;
                    newWord.contentcount = 1;

                    word.wordid = newWord.wordid;

                    wordMap.Add(newWord.word, newWord.wordid);
                    wordDataMap.Add(newWord.wordid, newWord);
                }
                words.Add(word);
            }
            return words;
        }

        //words doesn't have its own get function because each the other functions will be updating the wordEntry data

        //a title/text should only have letters or numbers
        //i.e. input: "This title, this 1    is fine!" | output: {"this", "title", "this" "1", "is" "fine"}
        private List<string> ParseText(string title)
        {
            List<string> words = new List<string>();
            string currWord = "";
            foreach (char c in title)
            {
                if (char.IsLetter(c)) //append lowercase version
                    currWord = currWord + char.ToLower(c);
                else if (char.IsDigit(c))
                    currWord = currWord + c;
                else //on split letter
                {
                    if (currWord.Length > 0) //only add if non empty
                        words.Add(currWord);
                    currWord = ""; //reset
                }
            }
            return words;
        }

        //remove the tags from html formatting
        //from: https://www.puresourcecode.com/dotnet/csharp/how-to-strip-all-html-tags-and-entities-and-get-clear-text/
        private string StripHTML(string html)
        {
            return Regex.Replace(html, "<.*?>", ""); //replace any tags with spaces
        }


        //count how many times an element appears in a list
        private int CountElement(string x, List<string> A)
        {
            int count = 0;
            for (int i = 0; i < A.Count; i++)
                if (A[i] == x)
                    count++;
            return count;
        }

        //return the parent question id of a post
        private int FindParentQuestion(Post p)
        {
            //keep going up one level until we find the original question
            while (p["type"][0] != 'Q') //current post is not the parent question (first char because question can be Q or Q_HIDDEN)
            {
                foreach (Post p2 in posts)
                {
                    if (p2["postid"] == p["parentid"]) //match
                    {
                        p = p2; //replace the post
                        break;
                    }
                }
            }
            return int.Parse(p["postid"]);
        }
    }
}
