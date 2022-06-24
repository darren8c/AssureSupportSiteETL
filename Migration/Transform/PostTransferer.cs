using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SupportSiteETL.Migration.Extract;
using SupportSiteETL.Migration.Load;
using SupportSiteETL.Migration.Transform.Models;

namespace SupportSiteETL.Migration.Transform
{

    using Topic = Dictionary<string, string>;
    using Post = Dictionary<string, string>;
    using VoteDetail = Dictionary<string, string>;
    public class PostTransformer
    {
        private Dictionary<int, int> oldToNewId; //contains mapping from discourse id to new id.
        //private Dictionary<int, int> oldToNewPostId; //contains mapping from discourse post id to new id.
        private Dictionary<int, int> oldToNewCatId; //contains mapping from discourse category id to new category id.

        private Dictionary<int, string> specialUserRules; //contains userid's and what special condition they follow see resourceinfo.txt
        
        private List<int> devUserIds; //used for selecting answers, ids of all dev users

        private uint currPostId;

        private List<Q2APost> allPosts;

        Extractor extractor;
        Loader loader;

        public PostTransformer()
        {
            allPosts = new List<Q2APost>();
            extractor = new Extractor();
            loader = new Loader();

            currPostId = extractor.GetQ2ALastPostId() + 1; //this will be the first post id to write to
            PopulateSpecialUserRules(); //set this dictionary
        }

        //extract the post data, to fill the fields we need the map of old id's to new id's which requires the user transferer
        //a map for old cat id's to new is also needed, lastly devUser list is needed to select answers
        public void Extract(Dictionary<int, int> o2nId, Dictionary<int,int> o2nIdCat, List<int> devUsers)
        {
            Console.WriteLine("Extracting topics and assembling posts...");

            oldToNewId = o2nId;
            oldToNewCatId = o2nIdCat;
            devUserIds = devUsers;

            //retrieve all the topics, then for each topic create all the posts
            List<Topic> topics = extractor.GetDiscourseTopics();

            //for each topic in the list, create the corresponding posts
            foreach (Topic topic in topics)
            {
                if (topic["archetype"] != "regular") //all other types are private messages and shouldn't be transferred
                    continue;
                if (specialUserRules.ContainsKey(int.Parse(topic["user_id"]))) //special user rules apply
                {
                    string flag = specialUserRules[int.Parse(topic["user_id"])]; //the flag for this user, i.e. resourceinfo.txt
                    if(flag == "Nathan") //posted by the dev Nathan
                    {
                        if (DateTime.Parse(topic["created_at"]) < DateTime.Parse("2015-04-05")) //outdated tutorial, happened before Apr 5th 2015
                            continue;
                        if (topic["title"].Substring(0, 3) == "FB ") //fog bug, ignore these posts
                            continue;
                    }
                }


                bool deleteCategory = (oldToNewCatId[int.Parse(topic["category_id"])] == -1);
                if (deleteCategory) //if something belongs to "Delete" don't add to q2a
                    continue;

                allPosts.AddRange( createPostsFromTopic(topic) );
                //Console.WriteLine("Topic " + topic["id"] + " extracted");
            }

            Console.WriteLine("Posts assembled!");
        }

        //save all the post data to the tables, includes, likes, tags, etc.
        public void Load()
        {
            Console.WriteLine("Transfering Posts...");
            
            loader.AddPosts(allPosts);

            List<UserVote> allVotes = new List<UserVote>(); //get the vote data from the posts in one list
            foreach (Q2APost post in allPosts)
                allVotes.AddRange(post.votes);
            loader.AddUserVotes(allVotes);

            Console.WriteLine("Posts Transfered!");
        }

        //from a topic, look up all posts in that topic and generate the post lists
        private List<Q2APost> createPostsFromTopic(Topic topic)
        {
            //all the posts on this thread
            List<Post> dcPosts = extractor.GetDiscoursePostsOnTopic(int.Parse(topic["id"]));
            Dictionary<int, Q2APost> replyIdMap = new Dictionary<int, Q2APost>(); //maps from the discourse post_number to the corresponding Q2APost.

            foreach (Post dcPost in dcPosts) //go through each post and gathter the proper data
            {
                Q2APost newPost = new Q2APost();
                SetBasicPostAttributes(ref newPost, topic, dcPost); //set basic fields like id, category, title, etc.
                SetAdvancedPostAttributes(ref newPost, topic, dcPost, ref replyIdMap); //set complicated fields like parent id and type

                //all fields set, add to map
                replyIdMap.Add(int.Parse(dcPost["post_number"]), newPost);
            }

            List<Q2APost> newPosts = replyIdMap.Values.ToList();
            SetAnswer(ref newPosts); //go through the posts and select a best answer if possible
            return newPosts;
        }

        //from a set of posts on a question select a best answer if possible
        //each post in a topic is scored by a number of factors, the highest score is the selected answer (assuming it passes a min threshhold)
        private void SetAnswer(ref List<Q2APost> posts)
        {
            posts = posts.OrderBy(p => p.created).ToList(); //order by date, question post is now first index
            if (posts.Count(p => p.type == "A") == 0) //no need to try selecting an answer if there are none
                return;

            double threshold = 15; //min points needed to be a selected answer
            double ordW = 1; //weight of recency / post order, i.e. first post is 1, second is 2 ...
            double devW = 15; //weight of being a dev, admin, expert , etc.
            double votW = 10; //weight of upvotes
            double oprW = 15; //weight of the op replying to the post
            double comW = 4; //weight of number of comments on an answer, comW * numOfComments
            //weight of post length (compared to the average), i.e. avg is 100 words, a post with 150 is lenW * .5
            double lenW = 20; //i.e. if the length is double average this is how many points they get
            double lenWBonusMax = 30; //don't allow the bonus from the lenBonus to go beyond this value
            double lenWBonusMin = -5; //don't allow the bonus from the lenBonus to go below this value, below 0 allows deductions
            //when badges are added, a weight (or several) can be added.

            int? opId = posts[0].userid; //userid of the the question poster

            int bestScoreIndex = 0;
            double bestScore = 0;
            double avgAnswerLength = posts.Where(p => p.type == "A").Average(a => a.content.Length); //average char count of the answers

            int postNum = 0; //used to track post order
            for(int i = 0; i < posts.Count; i++) //score each post
            {
                if (posts[i].type != "A") //only answers should get a score
                    continue;
                postNum++;


                //------------------ Determine the score for this post
                double score = 0; //score for this post

                score += ordW * postNum; //post order
                if (devUserIds.Contains((int)posts[i].userid)) //is dev
                    score += devW * 1;
                
                score += votW * posts[i].netvotes; //votes

                bool opReplied = false; //check if the question author replied to this answer
                uint answerId = posts[i].postid;
                for (int j = i + 1; j < posts.Count; j++) //if the following answer (ignoring comments in between) is made by the op
                {
                    if (posts[j].type=="A") //the next answer
                    {
                        if (posts[j].userid == opId) //counts as a reply
                            opReplied = true;
                        break; //only look at the first answer
                    }
                }
                if(!opReplied) //additional check by seeing if op replied as a comment
                    opReplied = posts.Exists(p => p.parentid == answerId && p.userid == opId); //questioner replied as a comment, counts as a reply
                if (opReplied) //original poster replied to this answer
                    score += oprW * 1;

                uint thisPostId = posts[i].postid;
                score += comW * posts.Count(p => p.type == "C" && p.parentid == thisPostId); //number of comments

                //length bonus, only add if above average, value must be between the min and max values
                score += Math.Clamp(lenW * ((posts[i].content.Length - avgAnswerLength) / avgAnswerLength), lenWBonusMin, lenWBonusMax);
                //------------------

                if(score > bestScore)
                {
                    bestScore = score;
                    bestScoreIndex = i;
                }
            }
            if (bestScore >= threshold) //only select an answer if it passes a minimum score
                posts[0].selchildid = (int)posts[bestScoreIndex].postid; //select the best score 

            posts = posts.OrderBy(p => p.postid).ToList(); //reorder by postid
        }

        private void SetBasicPostAttributes(ref Q2APost newPost, Topic topic, Post dcPost)
        {
            //Set the fields of the post
            newPost.postid = currPostId;
            currPostId++; //iterate ids
            
            //user_id details
            if (dcPost["user_id"] != "") //not null user
                newPost.userid = oldToNewId[int.Parse(dcPost["user_id"])];
            else //must be null user (deleted/banned), it will now show up as anonymous
                newPost.userid = null;
            //category details
            newPost.categoryid = oldToNewCatId[int.Parse(topic["category_id"])];
            newPost.catidpath1 = newPost.categoryid;
            //point details
            newPost.views = int.Parse(dcPost["reads"]);
            newPost.upvotes = int.Parse(dcPost["like_count"]);
            newPost.netvotes = newPost.upvotes - newPost.downvotes;
            //populate the table of qa_uservotes for a specific post from discourse
            newPost.votes = getVotesDetails(int.Parse(dcPost["id"]), (int)newPost.postid);

            //get the processed content in html formatting
            newPost.format = "html"; //keep everything in html
            newPost.content = ParseContent(dcPost["cooked"]); //the html format of the post

            newPost.created = DateTime.Parse(dcPost["created_at"]);
            if (dcPost["created_at"] == dcPost["updated_at"]) //there has never been an update if update time is the same
                newPost.updated = null;
            else //different update time
            {
                newPost.updated = DateTime.Parse(dcPost["updated_at"]);
                newPost.updateType = "H";
            }
        }

        //handle all the complicated details of taking a post and adding it to the list
        private void SetAdvancedPostAttributes(ref Q2APost newPost, Topic topic, Post dcPost, ref Dictionary<int, Q2APost> replyIdMap)
        {
            //Determine the type, and parent id, note for the sake of finding the parent, the posts are in post_number order
            if (int.Parse(dcPost["post_number"]) == 1) //this must be a question as it is the first post
            {
                newPost.title = topic["title"];
                newPost.type = "Q";
                newPost.parentid = null;
            }
            else //either an answer or a comment
            {
                int replyNum = -1;
                if (dcPost["reply_to_post_number"] != "") //not null
                    replyNum = int.Parse(dcPost["reply_to_post_number"]);
                if (replyNum == -1 || replyNum == 1) //not replying to anything or reply to question, assume answer
                {
                    newPost.type = "A"; //answer
                    newPost.parentid = (int)replyIdMap[1].postid; //parent is the original question id

                    replyIdMap[1].acount++; //add one to the answer count of the question post.
                    if (newPost.upvotes > replyIdMap[1].amaxvote) //check if this is a new maximum for the most up voted answer
                        replyIdMap[1].amaxvote = newPost.upvotes;
                }
                else //must be a comment
                {
                    newPost.type = "C"; //comment
                    newPost.parentid = (int)replyIdMap[replyNum].postid;
                }
            }
            //check if the post should be hidden
            if (bool.Parse(dcPost["hidden"]) || bool.Parse(dcPost["user_deleted"]))
                newPost.type += "_HIDDEN"; //add hidden to the type, i.e. C_HIDDEN
        }

        //q2a has some constraints that discourse didn't
        //max length is 12000 chars
        //each char can be encoded with 3 bytes max
        private string ParseContent(string orig)
        {

            string s = orig;
            if (s.Length >= 12000) //q2a doesn't permit this field to have over 12000 chars
                s = s.Substring(0, 11500); //cutoff a bit earlier just in case
            
            //basically if a char is stored in the string using surrogates, it is too long to be displayed in q2a
            for (int i = 0; i < s.Length; i++) //this may be slow, look into a faster way to switch out chars that decode to more than 3 
                if (Char.IsSurrogate(s[i])) //the content column only supports characters encoded with 3 or less bytes
                    s = s.Substring(0, i) + "\u2610" + s.Substring(i + 1, s.Length - (i + 1)); //swap char for block character █

            return s;
        }

        private UserVote[] getVotesDetails(int postIdDisc, int postIdQ2A)
        {
            List<VoteDetail> dcPostsActions = extractor.GetDiscoursePostsOnActions(postIdDisc, 2);
            UserVote[] voteDetails = new UserVote[dcPostsActions.Count];
            for (int i = 0; i < dcPostsActions.Count; i++)
            {
                voteDetails[i] = new UserVote();
                voteDetails[i].postid = postIdQ2A;
                voteDetails[i].userid = oldToNewId[int.Parse(dcPostsActions[i]["user_id"])];
                voteDetails[i].votecreated = DateTime.Parse(dcPostsActions[i]["created_at"]);
                voteDetails[i].voteupdated = DateTime.Parse(dcPostsActions[i]["updated_at"]);
            }
            return voteDetails;
        }

        private void PopulateSpecialUserRules()
        {
            specialUserRules = new Dictionary<int, string>(); //old discourse id to flag indicating rules

            //fill in the lookup table from old discourse usernames to their new role name under Q2A
            var lines = File.ReadLines("Resources/specialUserPosts.txt");
            foreach (string line in lines)
            {
                string[] data = line.Split(',');
                if (data.Length != 2) //there should always be 2 fields (discourse_user_id, flag_type)
                {
                    Console.WriteLine("Error, specialUserPosts.txt is not in the correct format!");
                    return;
                }
                int id = int.Parse(data[0]);
                string flag = data[1];

                specialUserRules.Add(id, flag);
            }
        }
    }
}