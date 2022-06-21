using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SupportSiteETL.Databases;
using MySql.Data.MySqlClient;
using SupportSiteETL.Migration.Transform.Models;
using SupportSiteETL.Migration.Transform;

namespace SupportSiteETL.Migration.Load
{
    //handles the specific sql queries for the q2a site (includes reading and writing)
    //discourse requires no loading, so no discourse calls
    public class Loader
    {
        public Q2AConnection q2a;

        public Loader()
        {
            q2a = new Q2AConnection();
        }
        
        //updates the stats on the bottom of the site
        //includes
        //cache_userpointscount # of users
        //cache_qcount # of questions
        //cache_acount # of answers
        //cache_ccount # of comments
        public void UpdateSiteStats()
        {
            UpdateStatHelper("SELECT COUNT(*) FROM qa_users", "cache_userpointscount"); //updates user count
            UpdateStatHelper("SELECT COUNT(*) FROM qa_posts WHERE type='Q'", "cache_qcount"); //updates question count
            UpdateStatHelper("SELECT COUNT(*) FROM qa_posts WHERE type='A'", "cache_acount"); //updates answer count
            UpdateStatHelper("SELECT COUNT(*) FROM qa_posts WHERE type='C'", "cache_ccount"); //updates comment count
        }
        //helps the updateSiteStats function, executes a scaler function and then saves the value into another field in qa_options
        //findCountCommand should return 1 value which will be stored into the content where title=titleName
        //setup for integers only
        private void UpdateStatHelper(string findCountCommand, string titleName)
        {
            int contentValue = 0;
            string updateCommand = "UPDATE qa_options set content = @content WHERE title='" + titleName + "'";

            MySqlConnection conn = q2a.retrieveConnection();
            conn.Open();
            try
            {
                using (MySqlCommand cmd = new MySqlCommand(findCountCommand, conn)) //count number of users
                {
                    contentValue = (int)(Int64)cmd.ExecuteScalar(); //executes query and returns entry in first row and column
                }
                using (MySqlCommand cmd = new MySqlCommand(updateCommand, conn)) //write to qa_users
                {
                    cmd.Parameters.AddWithValue("@content", contentValue);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error adding user count data: " + ex.Message);
            }
        }

        //updates the qcount field on a category (how many questions on the given category)
        public void UpdateCategoryCount(int categoryId)
        {
            int count = 0;
            //count query
            string findCountCommand = "SELECT COUNT(*) FROM qa_posts WHERE type='Q' AND categoryid='" + categoryId.ToString() + "'";
            //update query
            string updateCommand = "UPDATE qa_categories set qcount = @qcount WHERE categoryid='" + categoryId.ToString() + "'";
            MySqlConnection conn = q2a.retrieveConnection();
            conn.Open();
            try
            {
                using (MySqlCommand cmd = new MySqlCommand(findCountCommand, conn)) //count number of questions under this category
                {
                    count = (int)(Int64)cmd.ExecuteScalar(); //executes query and returns entry in first row and column
                }
                using (MySqlCommand cmd = new MySqlCommand(updateCommand, conn)) //write to qa_users
                {
                    cmd.Parameters.AddWithValue("@qcount", count);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error adding user count data: " + ex.Message);
            }
        }

        //add user data to all the different tables given user data
        public void addUser(Q2AUser user)
        {
            MySqlConnection conn = q2a.retrieveConnection();

            // Queries to insert users
            string sql4Users = "INSERT INTO qa_users (userid, created, createip, loggedin, loginip, email, handle, level, flags, wallposts) VALUES (@userid, @created, @createip, @loggedin, @loginip, @email, @handle, @level, @flags, @wallposts)";
            string sql4Points = "Insert INTO qa_userpoints (userid, points, qposts, qupvotes, qvoteds, upvoteds) VALUES (@userid, @points, @qposts, @qupvotes, @qvoteds, @upvoteds)";
            //the profile writing is actually 4 inserts
            string sql4Profiles = "Insert INTO qa_userprofile (userid, title, content) VALUES (@userid, @title, @content)";

            conn.Open();
            try
            {
                using (MySqlCommand cmd = new MySqlCommand(sql4Users, conn)) //to qa_users
                {
                    cmd.Parameters.AddWithValue("@userid", user.userId);
                    cmd.Parameters.AddWithValue("@created", user.created_at);
                    cmd.Parameters.AddWithValue("@createip", "");
                    cmd.Parameters.AddWithValue("@loggedin", user.loggedin);
                    cmd.Parameters.AddWithValue("@loginip", "");
                    cmd.Parameters.AddWithValue("@email", user.email);
                    cmd.Parameters.AddWithValue("@handle", user.handle);
                    cmd.Parameters.AddWithValue("@level", user.level);
                    cmd.Parameters.AddWithValue("@flags", user.flags);
                    cmd.Parameters.AddWithValue("@wallposts", user.wallposts);
                    cmd.ExecuteNonQuery();
                }
                using (MySqlCommand cmd = new MySqlCommand(sql4Points, conn)) //to qa_points
                {
                    cmd.Parameters.AddWithValue("@userid", user.userId);
                    cmd.Parameters.AddWithValue("@qposts", user.qposts);
                    cmd.Parameters.AddWithValue("@qupvotes", user.qupvotes);
                    cmd.Parameters.AddWithValue("@qvoteds", user.qvoteds);
                    cmd.Parameters.AddWithValue("@upvoteds", user.upvoteds);
                    cmd.Parameters.AddWithValue("@points", user.points);
                    cmd.ExecuteNonQuery();
                }
                //each of the profile writes
                using (MySqlCommand cmd = new MySqlCommand(sql4Profiles, conn)) //about
                {
                    cmd.Parameters.AddWithValue("@userid", user.userId);
                    cmd.Parameters.AddWithValue("@title", "about");
                    cmd.Parameters.AddWithValue("@content", user.about);
                    cmd.ExecuteNonQuery();
                }
                using (MySqlCommand cmd = new MySqlCommand(sql4Profiles, conn)) //location
                {
                    cmd.Parameters.AddWithValue("@userid", user.userId);
                    cmd.Parameters.AddWithValue("@title", "location");
                    cmd.Parameters.AddWithValue("@content", user.location);
                    cmd.ExecuteNonQuery();
                }
                using (MySqlCommand cmd = new MySqlCommand(sql4Profiles, conn)) //name
                {
                    cmd.Parameters.AddWithValue("@userid", user.userId);
                    cmd.Parameters.AddWithValue("@title", "name");
                    cmd.Parameters.AddWithValue("@content", user.name);
                    cmd.ExecuteNonQuery();
                }
                using (MySqlCommand cmd = new MySqlCommand(sql4Profiles, conn)) //website
                {
                    cmd.Parameters.AddWithValue("@userid", user.userId);
                    cmd.Parameters.AddWithValue("@title", "website");
                    cmd.Parameters.AddWithValue("@content", user.website);
                    cmd.ExecuteNonQuery();
                }

                Console.WriteLine(string.Format("Inserted user with ID: {0} - {1}", user.userId, user.handle));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error adding new user: " + ex.Message);
            }
            conn.Close();
        }


        //add user data to all the different tables given user data
        public void addPost(Q2APost post)
        {
            MySqlConnection conn = q2a.retrieveConnection();

            // Queries to posts
            string addPostCommand = "INSERT INTO qa_posts (postid,  type,  parentid,  categoryid, catidpath1, acount, amaxvote, userid, " +
                "upvotes, downvotes, netvotes, views, flagcount, format, created, updated, updatetype, title, content, notify) " +
                "VALUES (@postid,  @type,  @parentid,  @categoryid, @catidpath1, @acount, @amaxvote, @userid, @upvotes, @downvotes, " +
                "@netvotes, @views, @flagcount, @format, @created, @updated, @updatetype, @title, @content, @notify)";

            conn.Open();
            try
            {
                using (MySqlCommand cmd = new MySqlCommand(addPostCommand, conn)) //to qa_posts
                {
                    cmd.Parameters.AddWithValue("@postid", post.postid);
                    cmd.Parameters.AddWithValue("@type", post.type);
                    cmd.Parameters.AddWithValue("@parentid", post.parentid);
                    cmd.Parameters.AddWithValue("@categoryid", post.categoryid);
                    cmd.Parameters.AddWithValue("@catidpath1", post.catidpath1);
                    cmd.Parameters.AddWithValue("@acount", post.acount);
                    cmd.Parameters.AddWithValue("@amaxvote", post.amaxvote);
                    cmd.Parameters.AddWithValue("@userid", post.userid);
                    cmd.Parameters.AddWithValue("@upvotes", post.upvotes);
                    cmd.Parameters.AddWithValue("@downvotes", post.downvotes);
                    cmd.Parameters.AddWithValue("@netvotes", post.netvotes);
                    cmd.Parameters.AddWithValue("@views", post.views);
                    cmd.Parameters.AddWithValue("@flagcount", post.flagcount);
                    cmd.Parameters.AddWithValue("@format", post.format);
                    cmd.Parameters.AddWithValue("@created", post.created);
                    cmd.Parameters.AddWithValue("@updated", post.updated);
                    cmd.Parameters.AddWithValue("@updatetype", post.updateType);
                    cmd.Parameters.AddWithValue("@title", post.title);
                    cmd.Parameters.AddWithValue("@content", post.content);
                    cmd.Parameters.AddWithValue("@notify", post.notify);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error adding new user: " + ex.Message);
                Console.ReadLine();
            }
            conn.Close();
        }

        //add a new category to q2a

        public void addUserVote(Q2APost post)
        {
            MySqlConnection conn = q2a.retrieveConnection();
            // Queries to uservotes
            string addVotesCommand = "INSERT INTO qa_uservotes (postid, userid, vote, flag, votecreated, voteupdated) " +
                "VALUES(@postid, @userid, @vote, @flag, @votecreated, @voteupdated)";
            conn.Open();
            try
            {
                foreach (Q2APost.UserVotes detail in post.votes)
                {
                    using (MySqlCommand cmd = new MySqlCommand(addVotesCommand, conn)) //to qa_uservotes
                    {
                        cmd.Parameters.AddWithValue("@postid", post.postid);
                        cmd.Parameters.AddWithValue("@userid", detail.userid);
                        cmd.Parameters.AddWithValue("@vote", detail.vote);
                        cmd.Parameters.AddWithValue("@flag", detail.flag);
                        cmd.Parameters.AddWithValue("@votecreated", detail.votecreated);
                        cmd.Parameters.AddWithValue("@voteupdated", detail.voteupdated);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error adding new user_vote for a post: " + ex.Message);
                Console.ReadLine();
            }
            conn.Close();
        }

        public void addCategory(Q2ACategory cat)
        {
            MySqlConnection conn = q2a.retrieveConnection();

            //command to add a new category
            string addCategoryCommand = "INSERT INTO qa_categories (categoryid, parentid, title, tags, content, qcount, position, backpath) " +
                "VALUES (@categoryid, @parentid, @title, @tags, @content, @qcount, @position, @backpath)";

            conn.Open();
            try
            {
                using (MySqlCommand cmd = new MySqlCommand(addCategoryCommand, conn)) //to qa_users
                {
                    cmd.Parameters.AddWithValue("@categoryid", cat.id);
                    cmd.Parameters.AddWithValue("@parentid", cat.parentid);
                    cmd.Parameters.AddWithValue("@title", cat.title);
                    cmd.Parameters.AddWithValue("@tags", cat.tag);
                    cmd.Parameters.AddWithValue("@content", cat.content);
                    cmd.Parameters.AddWithValue("@qcount", cat.qcount);
                    cmd.Parameters.AddWithValue("@position", cat.position);
                    cmd.Parameters.AddWithValue("@backpath", cat.backpath);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error adding new category: " + ex.Message);
                Console.ReadLine();
            }
            conn.Close();
        }

        //set the word tables so searching works properly
        public void AddToWordTables(List<WordEntry> word, List<ContentWordsEntry> content, List<PostTagsEntry> post, List<TagWordsEntry> tag, List<TagWordsEntry> title)
        {
            MySqlConnection conn = q2a.retrieveConnection();

            //command to add a new category
            string wordsCommand = "INSERT INTO qa_words (wordid, word, titlecount, contentcount, tagwordcount, tagcount) " +
                "VALUES (@wordid, @word, @titlecount, @contentcount, @tagwordcount, @tagcount)";
            string contentCommand = "INSERT INTO qa_contentwords (postid, wordid, count, type, questionid) " +
                "VALUES (@postid, @wordid, @count, @type, @questionid)";
            string posttagsCommand = "INSERT INTO qa_posttags (postid, wordid, postcreated) " +
                "VALUES (@postid, @wordid, @postcreated)";
            string tagCommand = "INSERT INTO qa_tagwords (postid, wordid) " +
                "VALUES (@postid, @wordid)";
            string titleCommand = "INSERT INTO qa_titlewords (postid, wordid) " +
                "VALUES (@postid, @wordid)";

            conn.Open();
            try
            {
                foreach(var x in word) //qa_words
                {
                    using (MySqlCommand cmd = new MySqlCommand(wordsCommand, conn))
                    {
                        cmd.Parameters.AddWithValue("@wordid", x.wordid);
                        cmd.Parameters.AddWithValue("@word", x.word);
                        cmd.Parameters.AddWithValue("@titlecount", x.titlecount);
                        cmd.Parameters.AddWithValue("@contentcount", x.contentcount);
                        cmd.Parameters.AddWithValue("@tagwordcount", x.tagwordcount);
                        cmd.Parameters.AddWithValue("@tagcount", x.tagcount);
                        cmd.ExecuteNonQuery();
                    }
                }
                foreach (var x in content) //qa_contentwords
                {
                    using (MySqlCommand cmd = new MySqlCommand(contentCommand, conn))
                    {
                        cmd.Parameters.AddWithValue("@postid", x.postid);
                        cmd.Parameters.AddWithValue("@wordid", x.wordid);
                        cmd.Parameters.AddWithValue("@count", x.count);
                        cmd.Parameters.AddWithValue("@type", x.type);
                        cmd.Parameters.AddWithValue("@questionid", x.questionid);
                        cmd.ExecuteNonQuery();
                    }
                }
                foreach (var x in post) //qa_posttags
                {
                    using (MySqlCommand cmd = new MySqlCommand(posttagsCommand, conn))
                    {
                        cmd.Parameters.AddWithValue("@postid", x.postid);
                        cmd.Parameters.AddWithValue("@wordid", x.wordid);
                        cmd.Parameters.AddWithValue("@postcreated", x.postcreated);
                        cmd.ExecuteNonQuery();
                    }
                }
                foreach (var x in tag) //qa_tagwords
                {
                    using (MySqlCommand cmd = new MySqlCommand(tagCommand, conn))
                    {
                        cmd.Parameters.AddWithValue("@postid", x.postid);
                        cmd.Parameters.AddWithValue("@wordid", x.postid);
                        cmd.ExecuteNonQuery();
                    }
                }
                foreach (var x in title) //qa_titlewords
                {
                    using (MySqlCommand cmd = new MySqlCommand(titleCommand, conn))
                    {
                        cmd.Parameters.AddWithValue("@postid", x.postid);
                        cmd.Parameters.AddWithValue("@wordid", x.wordid);
                        cmd.ExecuteNonQuery();
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error setting word tables: " + ex.Message);
                Console.ReadLine();
            }
            conn.Close();
        }

    }


}
