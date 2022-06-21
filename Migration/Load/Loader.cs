using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SupportSiteETL.Databases;
using MySql.Data.MySqlClient;
using System.Data;

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
            UpdateStatHelper("SELECT COUNT(*) FROM qa_users", "cache_userpointscount"); //user count
            UpdateStatHelper("SELECT COUNT(*) FROM qa_posts WHERE type='Q'", "cache_qcount"); //question count
            UpdateStatHelper("SELECT COUNT(*) FROM qa_posts WHERE type='A'", "cache_acount"); //answer count
            UpdateStatHelper("SELECT COUNT(*) FROM qa_posts WHERE type='C'", "cache_ccount"); //comment count
            UpdateStatHelper("SELECT COUNT(*) FROM qa_tagwords", "cache_tagcount"); //the amount of tags on the site
            UpdateStatHelper("SELECT COUNT(*) FROM qa_posts where type='Q' and acount=0", "cache_unaqcount"); //unanswered question count
            UpdateStatHelper("SELECT COUNT(*) FROM qa_posts where type='Q' and selchildid is null", "cache_unselqcount"); //no selected answer count
            UpdateStatHelper("SELECT COUNT(*) FROM qa_posts where type='Q' and amaxvote=0", "cache_unupaqcount"); //unvoted answer count
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



        //add user data to all the different tables given user data, Use batch inserting for much faster write speed
        //basically the commands look something like INSERT INTO table (column names) VALUES (row1), (row2) ... (rowBatchSize)
        public void AddUsers(List<Q2AUser> data, int batchSize=300)
        {
            // Queries to insert users
            string userCommand = "INSERT INTO qa_users (userid, created, createip, loggedin, loginip, email, handle, level, flags, wallposts) VALUES ";
            string pointCommand = "INSERT INTO qa_userpoints (userid, points, qposts, qupvotes, qvoteds, upvoteds) VALUES ";
            //this is actually 4 rows per user
            string profileCommand = "INSERT INTO qa_userprofile (userid, title, content) VALUES ";

            MySqlConnection conn = q2a.retrieveConnection();
            conn.Open();
            try
            {
                //execute the statement in batches, total statements to execute is words.Count / batchSize
                for (int startIndex = 0; startIndex < data.Count; startIndex = startIndex + batchSize)
                {
                    int endIndex = Math.Min(data.Count, startIndex + batchSize); //[startIndex, endIndex) is our range

                    string userCommandF = userCommand;
                    string pointCommandF = pointCommand;
                    string profileCommandF = profileCommand;

                    //add (@userid, @created, @createip, @loggedin, @loginip, @email, @handle, @level, @flags, @wallposts) to userCommand
                    //add (@userid, @points, @qposts, @qupvotes, @qvoteds, @upvoteds) to pointCommand
                    //add (@userid, @title, @content) to profileCommand (actually 4 entries) (user prefix A, B, C, D for each 4)
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        string sI = i.ToString(); //index as a string
                        userCommandF = userCommandF + "(" + "@userid" + sI + ", @created" + sI + ", @createip" + sI + ", @loggedin" + sI +
                            ", @loginip" + sI + ", @email" + sI + ", @handle" + sI + ", @level" + sI + ", @flags" + sI + ", @wallposts" + sI + "),";
                        pointCommandF = pointCommandF + "(" + "@userid" + sI + ", @points" + sI + ", @qposts" + sI + ", @qupvotes" + sI +
                            ", @qvoteds" + sI + ", @upvoteds" + sI + "),";
                        profileCommandF = profileCommandF + "(" + "@useridA" + sI + ", @titleA" + sI + ", @contentA" + sI + "),";
                        profileCommandF = profileCommandF + "(" + "@useridB" + sI + ", @titleB" + sI + ", @contentB" + sI + "),";
                        profileCommandF = profileCommandF + "(" + "@useridC" + sI + ", @titleC" + sI + ", @contentC" + sI + "),";
                        profileCommandF = profileCommandF + "(" + "@useridD" + sI + ", @titleD" + sI + ", @contentD" + sI + "),";
                    }
                    userCommandF = userCommandF.Remove(userCommandF.Length - 1); //remove the last comma
                    pointCommandF = pointCommandF.Remove(pointCommandF.Length - 1); //remove the last comma
                    profileCommandF = profileCommandF.Remove(profileCommandF.Length - 1); //remove the last comma
                    //now fill in the data, and execute each command
                    using (MySqlCommand cmd = new MySqlCommand(userCommandF, conn)) //to qa_users
                    {
                        for (int i = startIndex; i < endIndex; i++) //fill in the entries
                        {
                            string sI = i.ToString(); //index as a string
                            cmd.Parameters.AddWithValue("@userid" + sI, data[i].userId);
                            cmd.Parameters.AddWithValue("@created" + sI, data[i].created_at);
                            cmd.Parameters.AddWithValue("@createip" + sI, "");
                            cmd.Parameters.AddWithValue("@loggedin" + sI, data[i].loggedin);
                            cmd.Parameters.AddWithValue("@loginip" + sI, "");
                            cmd.Parameters.AddWithValue("@email" + sI, data[i].email);
                            cmd.Parameters.AddWithValue("@handle" + sI, data[i].handle);
                            cmd.Parameters.AddWithValue("@level" + sI, data[i].level);
                            cmd.Parameters.AddWithValue("@flags" + sI, data[i].flags);
                            cmd.Parameters.AddWithValue("@wallposts" + sI, data[i].wallposts);
                        }
                        cmd.ExecuteNonQuery();
                    }
                    using (MySqlCommand cmd = new MySqlCommand(pointCommandF, conn)) //to qa_points
                    {
                        for (int i = startIndex; i < endIndex; i++) //fill in the entries
                        {
                            string sI = i.ToString(); //index as a string
                            cmd.Parameters.AddWithValue("@userid" + sI, data[i].userId);
                            cmd.Parameters.AddWithValue("@qposts" + sI, data[i].qposts);
                            cmd.Parameters.AddWithValue("@qupvotes" + sI, data[i].qupvotes);
                            cmd.Parameters.AddWithValue("@qvoteds" + sI, data[i].qvoteds);
                            cmd.Parameters.AddWithValue("@upvoteds" + sI, data[i].upvoteds);
                            cmd.Parameters.AddWithValue("@points" + sI, data[i].points);
                        }
                        cmd.ExecuteNonQuery();
                    }
                    using (MySqlCommand cmd = new MySqlCommand(profileCommandF, conn)) //profile sections, 4 lines
                    {
                        for (int i = startIndex; i < endIndex; i++) //fill in the entries
                        {
                            string sI = i.ToString(); //index as a string
                            //about
                            cmd.Parameters.AddWithValue("@useridA" + sI, data[i].userId);
                            cmd.Parameters.AddWithValue("@titleA" + sI, "about");
                            cmd.Parameters.AddWithValue("@contentA" + sI, data[i].about);
                            //location
                            cmd.Parameters.AddWithValue("@useridB" + sI, data[i].userId);
                            cmd.Parameters.AddWithValue("@titleB" + sI, "loation");
                            cmd.Parameters.AddWithValue("@contentB" + sI, data[i].location);
                            //name
                            cmd.Parameters.AddWithValue("@useridC" + sI, data[i].userId);
                            cmd.Parameters.AddWithValue("@titleC" + sI, "name");
                            cmd.Parameters.AddWithValue("@contentC" + sI, data[i].name);
                            //website
                            cmd.Parameters.AddWithValue("@useridD" + sI, data[i].userId);
                            cmd.Parameters.AddWithValue("@titleD" + sI, "website");
                            cmd.Parameters.AddWithValue("@contentD" + sI, data[i].website);
                        }
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error adding users: " + ex.Message);
                Console.ReadLine();
            }
            conn.Close();
        }

        //add user data to all the different tables given user data, Use batch inserting for much faster write speed
        //basically the commands look something like INSERT INTO table (column names) VALUES (row1), (row2) ... (rowBatchSize)
        public void AddPosts(List<Q2APost> data, int batchSize=500)
        {
            MySqlConnection conn = q2a.retrieveConnection();

            // Queries to posts
            string addPostCommand = "INSERT INTO qa_posts (postid,  type,  parentid,  categoryid, catidpath1, acount, amaxvote, userid, " +
                "upvotes, downvotes, netvotes, views, flagcount, format, created, updated, updatetype, title, content, notify, selchildid) VALUES ";
            conn.Open();
            try
            {
                //execute the statement in batches, total statements to execute is words.Count / batchSize
                for (int startIndex = 0; startIndex < data.Count; startIndex = startIndex + batchSize)
                {
                    int endIndex = Math.Min(data.Count, startIndex + batchSize); //[startIndex, endIndex) is our range

                    string finalCommand = addPostCommand;
                    //add (@wordid#, @word#, @titlecount#, @contentcount#, @tagwordcount#, @tagcount#)
                    //add (@postid#,  @type#,  @parentid#, @categoryid#, @catidpath1#, @acount#, @amaxvote#, @userid#, @upvotes#, @downvotes#,
                    //@netvotes#, @views#, @flagcount#, @format#, @created#, @updated#, @updatetype#, @title#, @content#, @notify#, @selchildid)" per row
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        string sI = i.ToString(); //index as a string
                        finalCommand = finalCommand + "(" + "@postid" + sI + ", @type" + sI + ", @parentid" + sI + ", @categoryid" + sI +
                            ", @catidpath1" + sI + ", @acount" + sI + ", @amaxvote" + sI + ", @userid" + sI + ", @upvotes" + sI +
                            ", @downvotes" + sI + ", @netvotes" + sI + ", @views" + sI + ", @flagcount" + sI + ", @format" + sI +
                            ", @created" + sI + ", @updated" + sI + ", @updatetype" + sI + ", @title" + sI + ", @content" + sI + 
                            ", @notify" + sI + ", @selchildid" + sI + "),";
                    }
                    finalCommand = finalCommand.Remove(finalCommand.Length - 1); //remove the last comma
                    using (MySqlCommand cmd = new MySqlCommand(finalCommand, conn))
                    {
                        for (int i = startIndex; i < endIndex; i++) //fill in the data of each entry
                        {
                            string sI = i.ToString(); //index as a string
                            cmd.Parameters.AddWithValue("@postid" + sI, data[i].postid);
                            cmd.Parameters.AddWithValue("@type" + sI, data[i].type);
                            cmd.Parameters.AddWithValue("@parentid" + sI, data[i].parentid);
                            cmd.Parameters.AddWithValue("@categoryid" + sI, data[i].categoryid);
                            cmd.Parameters.AddWithValue("@catidpath1" + sI, data[i].catidpath1);
                            cmd.Parameters.AddWithValue("@acount" + sI, data[i].acount);
                            cmd.Parameters.AddWithValue("@amaxvote" + sI, data[i].amaxvote);
                            cmd.Parameters.AddWithValue("@userid" + sI, data[i].userid);
                            cmd.Parameters.AddWithValue("@upvotes" + sI, data[i].upvotes);
                            cmd.Parameters.AddWithValue("@downvotes" + sI, data[i].downvotes);
                            cmd.Parameters.AddWithValue("@netvotes" + sI, data[i].netvotes);
                            cmd.Parameters.AddWithValue("@views" + sI, data[i].views);
                            cmd.Parameters.AddWithValue("@flagcount" + sI, data[i].flagcount);
                            cmd.Parameters.AddWithValue("@format" + sI, data[i].format);
                            cmd.Parameters.AddWithValue("@created" + sI, data[i].created);
                            cmd.Parameters.AddWithValue("@updated" + sI, data[i].updated);
                            cmd.Parameters.AddWithValue("@updatetype" + sI, data[i].updateType);
                            cmd.Parameters.AddWithValue("@title" + sI, data[i].title);
                            cmd.Parameters.AddWithValue("@content" + sI, data[i].content);
                            cmd.Parameters.AddWithValue("@notify" + sI, data[i].notify);
                            cmd.Parameters.AddWithValue("@selchildid" + sI, data[i].selchildid);
                        }
                        cmd.ExecuteNonQuery(); //finally execute the command
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error adding posts: " + ex.Message);
                Console.ReadLine();
            }
            conn.Close();
        }

        //add the user votes into qa_uservotes, Use batch inserting for much faster write speed
        //basically the commands look something like INSERT INTO table (column names) VALUES (row1), (row2) ... (rowBatchSize)
        public void AddUserVotes(List<UserVote> data, int batchSize=500)
        {
            // Queries to uservotes
            string addVotesCommand = "INSERT INTO qa_uservotes (postid, userid, vote, flag, votecreated, voteupdated) VALUES ";
            MySqlConnection conn = q2a.retrieveConnection();
            conn.Open();
            try
            {
                //execute the statement in batches, total statements to execute is words.Count / batchSize
                for (int startIndex = 0; startIndex < data.Count; startIndex = startIndex + batchSize)
                {
                    int endIndex = Math.Min(data.Count, startIndex + batchSize); //[startIndex, endIndex) is our range

                    string finalCommand = addVotesCommand;
                    //add (@postid, @userid, @vote, @flag, @votecreated, @voteupdated) in each row
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        string sI = i.ToString(); //index as a string
                        finalCommand = finalCommand + "(" + "@postid" + sI + ", @userid" + sI + ", @vote" + sI + ", @flag" + sI +
                            ", @votecreated" + sI + ", @voteupdated" + sI + "),";
                    }
                    finalCommand = finalCommand.Remove(finalCommand.Length - 1); //remove the last comma
                    using (MySqlCommand cmd = new MySqlCommand(finalCommand, conn))
                    {
                        for (int i = startIndex; i < endIndex; i++) //fill in the data of each entry
                        {
                            string sI = i.ToString(); //index as a string
                            cmd.Parameters.AddWithValue("@postid" + sI, data[i].postid);
                            cmd.Parameters.AddWithValue("@userid" + sI, data[i].userid);
                            cmd.Parameters.AddWithValue("@vote" + sI, data[i].vote);
                            cmd.Parameters.AddWithValue("@flag" + sI, data[i].flag);
                            cmd.Parameters.AddWithValue("@votecreated" + sI, data[i].votecreated);
                            cmd.Parameters.AddWithValue("@voteupdated" + sI, data[i].voteupdated);
                        }
                        cmd.ExecuteNonQuery(); //finally execute the command
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

        //add a new category to q2a
        public void AddCategory(Q2ACategory cat)
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
            AddWordTable(word, 500); //500 rows at a time
            AddContentTable(content, 500);
            AddPostTagTable(post, 500);
            AddTagTable(tag, 500);
            AddTitleTable(title, 500);
        }
        //functions to help AddToWordTables()
        //add many rows for every insert statement, basically by having INSERT INTO qa_words (columns) VALUES (row1), (row2), ...
        private void AddWordTable(List<WordEntry> data, int batchSize=500)
        {
            MySqlConnection conn = q2a.retrieveConnection();
            conn.Open();
            string wordsCommand = "INSERT INTO qa_words (wordid, word, titlecount, contentcount, tagwordcount, tagcount) VALUES ";
            try
            {
                //execute the statement in batches, total statements to execute is words.Count / batchSize
                for(int startIndex = 0; startIndex < data.Count; startIndex=startIndex+batchSize)
                {
                    int endIndex = Math.Min(data.Count, startIndex + batchSize); //[startIndex, endIndex) is our range

                    string finalCommand = wordsCommand;
                    //add (@wordid#, @word#, @titlecount#, @contentcount#, @tagwordcount#, @tagcount#)
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        string sI = i.ToString(); //index as a string
                        finalCommand = finalCommand + "(@wordid" + sI + ", @word" + sI + ", @titlecount" + sI + ", @contentcount" + sI + 
                            ", @tagwordcount" + sI + ", @tagcount" + sI + "),";
                    }
                    finalCommand = finalCommand.Remove(finalCommand.Length - 1); //remove the last comma
                    using (MySqlCommand cmd = new MySqlCommand(finalCommand, conn))
                    {
                        for (int i = startIndex; i < endIndex; i++) //fill in the entries
                        {
                            string sI = i.ToString(); //index as a string
                            cmd.Parameters.AddWithValue("@wordid" + sI,         data[i].wordid);
                            cmd.Parameters.AddWithValue("@word" + sI,           data[i].word);
                            cmd.Parameters.AddWithValue("@titlecount" + sI,     data[i].titlecount);
                            cmd.Parameters.AddWithValue("@contentcount" + sI,   data[i].contentcount);
                            cmd.Parameters.AddWithValue("@tagwordcount" + sI,   data[i].tagwordcount);
                            cmd.Parameters.AddWithValue("@tagcount" + sI,       data[i].tagcount);
                        }
                        cmd.ExecuteNonQuery(); //finally execute the command
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error setting word table: " + ex.Message);
                Console.ReadLine();
            }
            conn.Close();
        }
        private void AddTagTable(List<TagWordsEntry> data, int batchSize = 500)
        {
            MySqlConnection conn = q2a.retrieveConnection();
            conn.Open();
            string wordsCommand = "INSERT INTO qa_tagwords (postid, wordid) VALUES ";
            try
            {
                //execute the statement in batches, total statements to execute is words.Count / batchSize
                for (int startIndex = 0; startIndex < data.Count; startIndex = startIndex + batchSize)
                {
                    int endIndex = Math.Min(data.Count, startIndex + batchSize); //[startIndex, endIndex) is our range

                    string finalCommand = wordsCommand;
                    //add (@postid#, wordid#)
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        string sI = i.ToString(); //index as a string
                        finalCommand = finalCommand + "(@postid" + sI + ", @wordid" + sI + "),";
                    }
                    finalCommand = finalCommand.Remove(finalCommand.Length - 1); //remove the last comma
                    using (MySqlCommand cmd = new MySqlCommand(finalCommand, conn))
                    {
                        for (int i = startIndex; i < endIndex; i++) //fill in the entries
                        {
                            string sI = i.ToString(); //index as a string
                            cmd.Parameters.AddWithValue("@postid" + sI, data[i].postid);
                            cmd.Parameters.AddWithValue("@wordid" + sI, data[i].wordid);
                        }
                        cmd.ExecuteNonQuery(); //finally execute the command
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error setting tag table: " + ex.Message);
                Console.ReadLine();
            }
            conn.Close();
        }
        private void AddPostTagTable(List<PostTagsEntry> data, int batchSize = 500)
        {
            MySqlConnection conn = q2a.retrieveConnection();
            conn.Open();
            string wordsCommand = "INSERT INTO qa_posttags (postid, wordid, postcreated) VALUES ";
            try
            {
                //execute the statement in batches, total statements to execute is words.Count / batchSize
                for (int startIndex = 0; startIndex < data.Count; startIndex = startIndex + batchSize)
                {
                    int endIndex = Math.Min(data.Count, startIndex + batchSize); //[startIndex, endIndex) is our range

                    string finalCommand = wordsCommand;
                    //add (@postid#, @wordid#, @postcreated#)
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        string sI = i.ToString(); //index as a string
                        finalCommand = finalCommand + "(@postid" + sI + ", @wordid" + sI + ", @postcreated" + sI + "),";
                    }
                    finalCommand = finalCommand.Remove(finalCommand.Length - 1); //remove the last comma
                    using (MySqlCommand cmd = new MySqlCommand(finalCommand, conn))
                    {
                        for (int i = startIndex; i < endIndex; i++) //fill in the entries
                        {
                            string sI = i.ToString(); //index as a string
                            cmd.Parameters.AddWithValue("@postid" + sI, data[i].postid);
                            cmd.Parameters.AddWithValue("@wordid" + sI, data[i].wordid);
                            cmd.Parameters.AddWithValue("@postcreated" + sI, data[i].postcreated);
                        }
                        cmd.ExecuteNonQuery(); //finally execute the command
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error setting PostTags table: " + ex.Message);
                Console.ReadLine();
            }
            conn.Close();
        }
        private void AddContentTable(List<ContentWordsEntry> data, int batchSize = 500)
        {
            MySqlConnection conn = q2a.retrieveConnection();
            conn.Open();
            string wordsCommand = "INSERT INTO qa_contentwords (postid, wordid, count, type, questionid) VALUES ";
            try
            {
                //execute the statement in batches, total statements to execute is words.Count / batchSize
                for (int startIndex = 0; startIndex < data.Count; startIndex = startIndex + batchSize)
                {
                    int endIndex = Math.Min(data.Count, startIndex + batchSize); //[startIndex, endIndex) is our range

                    string finalCommand = wordsCommand;
                    //add (@postid#, @wordid#, @count#, @type#, @questionid#)
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        string sI = i.ToString(); //index as a string
                        finalCommand = finalCommand + "(@postid" + sI + ", @wordid" + sI + ", @count" + sI + ", @type" + sI +
                            ", @questionid" + sI + "),";
                    }
                    finalCommand = finalCommand.Remove(finalCommand.Length - 1); //remove the last comma
                    using (MySqlCommand cmd = new MySqlCommand(finalCommand, conn))
                    {
                        for (int i = startIndex; i < endIndex; i++) //fill in the entries
                        {
                            string sI = i.ToString(); //index as a string
                            cmd.Parameters.AddWithValue("@postid" + sI, data[i].postid);
                            cmd.Parameters.AddWithValue("@wordid" + sI, data[i].wordid);
                            cmd.Parameters.AddWithValue("@count" + sI, data[i].count);
                            cmd.Parameters.AddWithValue("@type" + sI, data[i].type);
                            cmd.Parameters.AddWithValue("@questionid" + sI, data[i].questionid);
                        }
                        cmd.ExecuteNonQuery(); //finally execute the command
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error setting content table: " + ex.Message);
                Console.ReadLine();
            }
            conn.Close();
        }
        private void AddTitleTable(List<TagWordsEntry> data, int batchSize = 500)
        {
            MySqlConnection conn = q2a.retrieveConnection();
            conn.Open();
            string wordsCommand = "INSERT INTO qa_titlewords (postid, wordid) VALUES ";
            try
            {
                //execute the statement in batches, total statements to execute is words.Count / batchSize
                for (int startIndex = 0; startIndex < data.Count; startIndex = startIndex + batchSize)
                {
                    int endIndex = Math.Min(data.Count, startIndex + batchSize); //[startIndex, endIndex) is our range

                    string finalCommand = wordsCommand;
                    //add (@postid#, wordid#)
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        string sI = i.ToString(); //index as a string
                        finalCommand = finalCommand + "(@postid" + sI + ", @wordid" + sI + "),";
                    }
                    finalCommand = finalCommand.Remove(finalCommand.Length - 1); //remove the last comma
                    using (MySqlCommand cmd = new MySqlCommand(finalCommand, conn))
                    {
                        for (int i = startIndex; i < endIndex; i++) //fill in the entries
                        {
                            string sI = i.ToString(); //index as a string
                            cmd.Parameters.AddWithValue("@postid" + sI, data[i].postid);
                            cmd.Parameters.AddWithValue("@wordid" + sI, data[i].wordid);
                        }
                        cmd.ExecuteNonQuery(); //finally execute the command
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error setting title table: " + ex.Message);
                Console.ReadLine();
            }
            conn.Close();
        }
    }


}
