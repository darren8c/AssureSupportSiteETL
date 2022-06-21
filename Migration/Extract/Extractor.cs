using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SupportSiteETL.Databases;

using MySql.Data.MySqlClient;
using Npgsql;

namespace SupportSiteETL.Migration.Extract
{
    //handles the specific sql queries for the databases related to extracting info
    //includes calls for both q2a and discourse
    public class Extractor
    {
        private DiscourseConnection dc;
        private Q2AConnection q2a;

        public Extractor()
        {
            dc = new DiscourseConnection();
            q2a = new Q2AConnection();
        }

        // Gets all users joined with their user stats
        public List<Dictionary<string, string>> GetDiscourseUsers()
        {
            return dc.ExecuteQuery("SELECT * FROM public.users JOIN public.user_stats ON public.users.id=public.user_stats.user_id ORDER BY id;");
        }
        public List<Dictionary<string, string>> GetDiscourseTopics()
        {
            return dc.ExecuteQuery("SELECT * FROM public.topics ORDER BY id;");
        }

        //get all the posts with a certain topic id, basically all the posts relating to a certain thread.
        public List<Dictionary<string, string>> GetDiscoursePostsOnTopic(int topicId)
        {
            return dc.ExecuteQuery("SELECT * FROM public.posts where topic_id=" + topicId.ToString() + " ORDER BY post_number;");
        }

        //get all the posts with a certain post and action id from post_actions
        public List<Dictionary<string, string>> GetDiscoursePostsOnActions(int postId, int actionId)
        {
            return dc.ExecuteQuery("SELECT user_id, created_at, updated_at FROM public.post_actions where post_id=" + postId.ToString() 
                                    + " AND post_action_type_id=" + actionId.ToString() + " ORDER BY id;");
        }

        public uint GetQ2ALastPostId()
        {
            string getPostCountCommand = "SELECT COUNT(*) from qa_posts";
            string getLastPostIdCommand = "SELECT postid FROM qa_posts ORDER BY postid DESC LIMIT 1"; //find the highest postid
            uint postid = 0;

            MySqlConnection conn = q2a.retrieveConnection();
            conn.Open();
            try
            {
                int postNum = 0;
                using (MySqlCommand cmd = new MySqlCommand(getPostCountCommand, conn)) //count number of posts
                {
                    postNum = (int)(Int64)(cmd.ExecuteScalar()); //executes query and returns entry in first row and column
                }
                if(postNum != 0) //there is at least one post
                {
                    using (MySqlCommand cmd = new MySqlCommand(getLastPostIdCommand, conn)) //find last post id, requires at least one row to exist
                    {
                        postid = (uint)(cmd.ExecuteScalar()); //executes query and returns entry in first row and column
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error retrieving post count: " + ex.Message);
            }
            return postid;
        }


        /// <summary>
        /// Gets all of the users from the Q2A database
        /// </summary>
        /// 
        /// <returns>
        /// A list of users, stored as dictionaries
        /// </returns>
        public List<Dictionary<string, string>> GetQ2AUsers()
        {
            return q2a.ExecuteQuery("SELECT * FROM qa_users ORDER BY userid;");
        }

        //get all the categories currently on q2a
        public List<Dictionary<string, string>> GetQ2ACategories()
        {
            return q2a.ExecuteQuery("SELECT * FROM qa_categories ORDER BY categoryid;");
        }
    }

}
