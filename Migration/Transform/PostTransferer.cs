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
    public class PostTransferer
    {
        private Dictionary<int, int> oldToNewId; //contains mapping from discourse id to new id.
        private Dictionary<int, int> oldToNewCatId; //contains mapping from discourse category id to new category id.

        private uint currPostId = 0;

        private List<Q2APost> allPosts;

        Extractor extractor;
        Loader loader;

        public PostTransferer()
        {
            allPosts = new List<Q2APost>();
            extractor = new Extractor();
            loader = new Loader();

            currPostId = extractor.GetQ2ALastPostId() + 1; //this will be the first post id to write to
        }

        //extract the post data, to fill the fields we need the map of old id's to new id's which requires the user transferer
        //a map for old cat id's to new is also needed
        public void Extract(Dictionary<int, int> o2nId, Dictionary<int,int> o2nIdCat)
        {
            oldToNewId = o2nId;
            oldToNewCatId = o2nIdCat;

            //retrieve all the topics, then for each topic create all the posts
            List<Topic> topics = extractor.GetDiscourseTopics();

            //for each topic in the list, create the corresponding posts
            foreach (Topic topic in topics)
            {
                if (topic["archetype"] != "regular") //all other types are private messages and shouldn't be transferred
                    continue;
                
                allPosts.AddRange( createPostsFromTopic(topic) );
                Console.WriteLine("Topic " + topic["id"] + " extracted");
            }
        }

        //save all the post data to the tables, includes, likes, tags, etc.
        public void Load()
        {
            foreach(Q2APost p in allPosts)
                loader.addPost(p);
        }

        //from a topic, look up all posts in that topic and generate the post lists
        private List<Q2APost> createPostsFromTopic(Topic topic)
        {
            //all the posts on this thread
            List<Post> dcPosts = extractor.GetDiscoursePostsOnTopic(int.Parse(topic["id"]));
            Dictionary<int, Q2APost> replyIdMap = new Dictionary<int, Q2APost>(); //maps from the discourse post_number to the corresponding Q2APost.
            foreach (var dcPost in dcPosts) //go through each post and gathter the proper ddata
            {
                Q2APost newPost = new Q2APost();

                newPost.postid = currPostId;
                currPostId++; //iterate id

                //Set the fields of the post
                if (dcPost["user_id"] != "") //not null user
                    newPost.userid = oldToNewId[int.Parse(dcPost["user_id"])];
                else //must be null user (deleted/banned), it will now show up as anonymous
                    newPost.userid = null;

                //uncomment if categories are being used
                newPost.categoryid = oldToNewCatId[int.Parse(topic["category_id"])];
                newPost.catidpath1 = newPost.categoryid;

                newPost.views = int.Parse(dcPost["reads"]);
                newPost.upvotes = int.Parse(dcPost["like_count"]);
                newPost.netvotes = newPost.upvotes - newPost.downvotes; 


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

                //Determine the type, and parent id, not for the sake of finding the parent, the posts are in post_number order
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
                    if(replyNum == -1 || replyNum == 1) //not replying to anything or reply to question, assume answer
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

                //all fields set, add to map
                replyIdMap.Add(int.Parse(dcPost["post_number"]), newPost);
            }

            return replyIdMap.Values.ToList();
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
    }
}