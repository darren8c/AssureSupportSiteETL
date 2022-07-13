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
            UpdateStatHelper("SELECT COUNT(DISTINCT wordid) FROM qa_tagwords", "cache_tagcount"); //the amount of tags on the site
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
                throw;
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
                throw;
            }
        }



        //add user data to all the different tables given user data, Use batch inserting for much faster write speed
        //basically the commands look something like INSERT INTO table (column names) VALUES (row1), (row2) ... (rowBatchSize)
        public void AddUsers(List<Q2AUser> data, int batchSize = 300)
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
                for (int dataLoadedCount = 0; dataLoadedCount < data.Count; dataLoadedCount += batchSize)
                {
                    //add (@userid, @created, @createip, @loggedin, @loginip, @email, @handle, @level, @flags, @wallposts) to userCommand
                    //add (@userid, @points, @qposts, @qupvotes, @qvoteds, @upvoteds) to pointCommand
                    //add (@userid, @title, @content) to profileCommand (actually 4 entries) (user prefix A, B, C, D for each 4)
                    StringBuilder userCommandF = new StringBuilder(userCommand);
                    StringBuilder pointCommandF = new StringBuilder(pointCommand);
                    StringBuilder profileCommandF = new StringBuilder(profileCommand);

                    var currentBatch = data.Skip(dataLoadedCount).Take(batchSize);
                    foreach (var entry in currentBatch) //create insert commands
                    {
                        string id = entry.userId.ToString(); //appended to each parameter (id is unique for every entry)

                        userCommandF.Append($"(@userid{id}, @created{id}, @createip{id}, @loggedin{id}, @loginip{id}, @email{id}, @handle{id}, " +
                                            $"@level{id}, @flags{id}, @wallposts{id}),");
                        pointCommandF.Append($"(@userid{id}, @points{id}, @qposts{id}, @qupvotes{id}, @qvoteds{id}, @upvoteds{id}),");

                        profileCommandF.Append($"(@useridA{id}, @titleA{id}, @contentA{id}),");
                        profileCommandF.Append($"(@useridB{id}, @titleB{id}, @contentB{id}),");
                        profileCommandF.Append($"(@useridC{id}, @titleC{id}, @contentC{id}),");
                        profileCommandF.Append($"(@useridD{id}, @titleD{id}, @contentD{id}),");
                    }
                    userCommandF.Length--; //remove the last comma
                    profileCommandF.Length--;
                    pointCommandF.Length--;

                    //fill in entries 
                    using (MySqlCommand cmd = new MySqlCommand(userCommandF.ToString(), conn)) //to qa_users
                    {
                        foreach (var entry in currentBatch) //fill in the entries
                        {
                            string id = entry.userId.ToString(); //appended to each parameter (id is unique for every entry)
                            cmd.Parameters.AddWithValue($"@userid{id}", entry.userId);
                            cmd.Parameters.AddWithValue($"@created{id}", entry.created_at);
                            cmd.Parameters.AddWithValue($"@createip{id}", "");
                            cmd.Parameters.AddWithValue($"@loggedin{id}", entry.loggedin);
                            cmd.Parameters.AddWithValue($"@loginip{id}", "");
                            cmd.Parameters.AddWithValue($"@email{id}", entry.email);
                            cmd.Parameters.AddWithValue($"@handle{id}", entry.handle);
                            cmd.Parameters.AddWithValue($"@level{id}", entry.level);
                            cmd.Parameters.AddWithValue($"@flags{id}", entry.flags);
                            cmd.Parameters.AddWithValue($"@wallposts{id}", entry.wallposts);
                        }
                        cmd.ExecuteNonQuery();
                    }
                    using (MySqlCommand cmd = new MySqlCommand(pointCommandF.ToString(), conn)) //to qa_points
                    {
                        foreach (var entry in currentBatch) //fill in the entries
                        {
                            string id = entry.userId.ToString(); //appended to each parameter (id is unique for every entry)
                            cmd.Parameters.AddWithValue($"@userid{id}", entry.userId);
                            cmd.Parameters.AddWithValue($"@qposts{id}", entry.qposts);
                            cmd.Parameters.AddWithValue($"@qupvotes{id}", entry.qupvotes);
                            cmd.Parameters.AddWithValue($"@qvoteds{id}", entry.qvoteds);
                            cmd.Parameters.AddWithValue($"@upvoteds{id}", entry.upvoteds);
                            cmd.Parameters.AddWithValue($"@points{id}", entry.points);
                        }
                        cmd.ExecuteNonQuery();
                    }
                    using (MySqlCommand cmd = new MySqlCommand(profileCommandF.ToString(), conn)) //profile sections, 4 lines
                    {
                        foreach (var entry in currentBatch) //fill in the entries
                        {
                            string id = entry.userId.ToString(); //appended to each parameter (id is unique for every entry)
                            //about
                            cmd.Parameters.AddWithValue($"@useridA{id}", entry.userId);
                            cmd.Parameters.AddWithValue($"@titleA{id}", "about");
                            cmd.Parameters.AddWithValue($"@contentA{id}", entry.about);
                            //location
                            cmd.Parameters.AddWithValue($"@useridB{id}", entry.userId);
                            cmd.Parameters.AddWithValue($"@titleB{id}", "loation");
                            cmd.Parameters.AddWithValue($"@contentB{id}", entry.location);
                            //name
                            cmd.Parameters.AddWithValue($"@useridC{id}", entry.userId);
                            cmd.Parameters.AddWithValue($"@titleC{id}", "name");
                            cmd.Parameters.AddWithValue($"@contentC{id}", entry.name);
                            //website
                            cmd.Parameters.AddWithValue($"@useridD{id}", entry.userId);
                            cmd.Parameters.AddWithValue($"@titleD{id}", "website");
                            cmd.Parameters.AddWithValue($"@contentD{id}", entry.website);
                        }
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error adding users: " + ex.Message);
                throw;
            }
            conn.Close();
        }

        //add user data to all the different tables given user data, Use batch inserting for much faster write speed
        //basically the commands look something like INSERT INTO table (column names) VALUES (row1), (row2) ... (rowBatchSize)
        public void AddPosts(List<Q2APost> data, int batchSize = 500)
        {
            MySqlConnection conn = q2a.retrieveConnection();

            // Queries to posts
            string addPostCommand = "INSERT INTO qa_posts (postid,  type,  parentid,  categoryid, catidpath1, acount, amaxvote, userid, " +
                "upvotes, downvotes, netvotes, views, flagcount, format, created, updated, updatetype, title, content, tags, notify, selchildid) VALUES ";
            conn.Open();
            try
            {
                //execute the statement in batches, total statements to execute is words.Count / batchSize
                for (int dataLoadedCount = 0; dataLoadedCount < data.Count; dataLoadedCount += batchSize)
                {
                    //add (@postid#,  @type#,  @parentid#, @categoryid#, @catidpath1#, @acount#, @amaxvote#, @userid#, @upvotes#, @downvotes#,
                    //@netvotes#, @views#, @flagcount#, @format#, @created#, @updated#, @updatetype#, @title#, @content# @tags#, @notify#,
                    //@selchildid)" per row
                    StringBuilder finalCommand = new StringBuilder(addPostCommand);

                    var currentBatch = data.Skip(dataLoadedCount).Take(batchSize);
                    foreach (var entry in currentBatch) //setup full insert query
                    {
                        string id = entry.postid.ToString(); //unique id for every entry
                        finalCommand.Append($"(@postid{id}, @type{id}, @parentid{id}, @categoryid{id}, @catidpath1{id}, @acount{id}, @amaxvote{id}, " +
                                            $"@userid{id}, @upvotes{id}, @downvotes{id}, @netvotes{id}, @views{id}, @flagcount{id}, @format{id}, " +
                                            $"@created{id}, @updated{id}, @updatetype{id}, @title{id}, @content{id}, @tags{id}, @notify{id}, " +
                                            $"@selchildid{id}),");
                    }
                    finalCommand.Length--; //remove the last comma
                    //add entry data and execute command
                    using (MySqlCommand cmd = new MySqlCommand(finalCommand.ToString(), conn))
                    {
                        foreach (var entry in currentBatch) //fill in the data of each entry
                        {
                            string id = entry.postid.ToString(); //unique id for every entry
                            cmd.Parameters.AddWithValue($"@postid{id}", entry.postid);
                            cmd.Parameters.AddWithValue($"@type{id}", entry.type);
                            cmd.Parameters.AddWithValue($"@parentid{id}", entry.parentid);
                            cmd.Parameters.AddWithValue($"@categoryid{id}", entry.categoryid);
                            cmd.Parameters.AddWithValue($"@catidpath1{id}", entry.catidpath1);
                            cmd.Parameters.AddWithValue($"@acount{id}", entry.acount);
                            cmd.Parameters.AddWithValue($"@amaxvote{id}", entry.amaxvote);
                            cmd.Parameters.AddWithValue($"@userid{id}", entry.userid);
                            cmd.Parameters.AddWithValue($"@upvotes{id}", entry.upvotes);
                            cmd.Parameters.AddWithValue($"@downvotes{id}", entry.downvotes);
                            cmd.Parameters.AddWithValue($"@netvotes{id}", entry.netvotes);
                            cmd.Parameters.AddWithValue($"@views{id}", entry.views);
                            cmd.Parameters.AddWithValue($"@flagcount{id}", entry.flagcount);
                            cmd.Parameters.AddWithValue($"@format{id}", entry.format);
                            cmd.Parameters.AddWithValue($"@created{id}", entry.created);
                            cmd.Parameters.AddWithValue($"@updated{id}", entry.updated);
                            cmd.Parameters.AddWithValue($"@updatetype{id}", entry.updateType);
                            cmd.Parameters.AddWithValue($"@title{id}", entry.title);
                            cmd.Parameters.AddWithValue($"@content{id}", entry.content);
                            cmd.Parameters.AddWithValue($"@tags{id}", entry.tags);
                            cmd.Parameters.AddWithValue($"@notify{id}", entry.notify);
                            cmd.Parameters.AddWithValue($"@selchildid{id}", entry.selchildid);
                        }
                        cmd.ExecuteNonQuery(); //finally execute the command
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error adding posts: " + ex.Message);
                throw;
            }
            conn.Close();
        }

        //add the user votes into qa_uservotes, Use batch inserting for much faster write speed
        //basically the commands look something like INSERT INTO table (column names) VALUES (row1), (row2) ... (rowBatchSize)
        public void AddUserVotes(List<UserVote> data, int batchSize = 500)
        {
            // Queries to uservotes
            string addVotesCommand = "INSERT INTO qa_uservotes (postid, userid, vote, flag, votecreated, voteupdated) VALUES ";
            MySqlConnection conn = q2a.retrieveConnection();
            conn.Open();
            try
            {
                //execute the statement in batches, total statements to execute is words.Count / batchSize
                for (int dataLoadedCount = 0; dataLoadedCount < data.Count; dataLoadedCount += batchSize)
                {
                    //add (@postid, @userid, @vote, @flag, @votecreated, @voteupdated) in each row
                    StringBuilder finalCommand = new StringBuilder(addVotesCommand);

                    var currentBatch = data.Skip(dataLoadedCount).Take(batchSize);
                    for (int i = 0; i < currentBatch.Count(); i++) //not using for each since id has to be unique, we use the indexes as the id
                    {
                        string id = (i + dataLoadedCount).ToString(); //index as a string, unique for every entry
                        finalCommand.Append($"(@postid{id}, @userid{id}, @vote{id}, @flag{id}, @votecreated{id}, @voteupdated{id}),");
                    }
                    finalCommand.Length--; //remove the last comma
                    using (MySqlCommand cmd = new MySqlCommand(finalCommand.ToString(), conn))
                    {
                        for (int i = 0; i < currentBatch.Count(); i++) //fill in the data of each entry
                        {
                            string id = (i + dataLoadedCount).ToString(); //index as a string, unique for every entry
                            var entry = data[i + dataLoadedCount];
                            cmd.Parameters.AddWithValue($"@postid{id}", entry.postid);
                            cmd.Parameters.AddWithValue($"@userid{id}", entry.userid);
                            cmd.Parameters.AddWithValue($"@vote{id}", entry.vote);
                            cmd.Parameters.AddWithValue($"@flag{id}", entry.flag);
                            cmd.Parameters.AddWithValue($"@votecreated{id}", entry.votecreated);
                            cmd.Parameters.AddWithValue($"@voteupdated{id}", entry.voteupdated);
                        }
                        cmd.ExecuteNonQuery(); //finally execute the command
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error adding new user_vote for a post: " + ex.Message);
                throw;
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
                throw;
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
        private void AddWordTable(List<WordEntry> data, int batchSize = 500)
        {
            MySqlConnection conn = q2a.retrieveConnection();
            conn.Open();
            string wordsCommand = "INSERT INTO qa_words (wordid, word, titlecount, contentcount, tagwordcount, tagcount) VALUES ";
            try
            {
                //execute the statement in batches, total statements to execute is words.Count / batchSize
                for (int dataLoadedCount = 0; dataLoadedCount < data.Count; dataLoadedCount += batchSize)
                {
                    //add (@wordid, @word, @titlecount, @contentcount, @tagwordcount, @tagcount) in each row
                    StringBuilder finalCommand = new StringBuilder(wordsCommand);

                    var currentBatch = data.Skip(dataLoadedCount).Take(batchSize);
                    for (int i = 0; i < currentBatch.Count(); i++) //not using for each since id has to be unique, we use the indes as the id
                    {
                        string id = (i + dataLoadedCount).ToString(); //index as a string, unique for every entry
                        finalCommand.Append($"(@wordid{id}, @word{id}, @titlecount{id}, @contentcount{id}, @tagwordcount{id}, @tagcount{id}),");
                    }
                    finalCommand.Length--; //remove the last comma
                    using (MySqlCommand cmd = new MySqlCommand(finalCommand.ToString(), conn))
                    {
                        for (int i = 0; i < currentBatch.Count(); i++) //fill in the data of each entry
                        {
                            string id = (i + dataLoadedCount).ToString(); //index as a string, unique for every entry
                            var entry = data[i + dataLoadedCount];
                            cmd.Parameters.AddWithValue($"@wordid{id}", entry.wordid);
                            cmd.Parameters.AddWithValue($"@word{id}", entry.word);
                            cmd.Parameters.AddWithValue($"@titlecount{id}", entry.titlecount);
                            cmd.Parameters.AddWithValue($"@contentcount{id}", entry.contentcount);
                            cmd.Parameters.AddWithValue($"@tagwordcount{id}", entry.tagwordcount);
                            cmd.Parameters.AddWithValue($"@tagcount{id}", entry.tagcount);
                        }
                        cmd.ExecuteNonQuery(); //finally execute the command
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error setting word table: " + ex.Message);
                throw;
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
                for (int dataLoadedCount = 0; dataLoadedCount < data.Count; dataLoadedCount += batchSize)
                {
                    //add (@postid, @wordid) in each row
                    StringBuilder finalCommand = new StringBuilder(wordsCommand);

                    var currentBatch = data.Skip(dataLoadedCount).Take(batchSize);
                    for (int i = 0; i < currentBatch.Count(); i++) //not using for each since id has to be unique, we use the indes as the id
                    {
                        string id = (i + dataLoadedCount).ToString(); //index as a string, unique for every entry
                        finalCommand.Append($"(@postid{id}, @wordid{id}),");
                    }
                    finalCommand.Length--; //remove the last comma
                    using (MySqlCommand cmd = new MySqlCommand(finalCommand.ToString(), conn))
                    {
                        for (int i = 0; i < currentBatch.Count(); i++) //fill in the data of each entry
                        {
                            string id = (i + dataLoadedCount).ToString(); //index as a string, unique for every entry
                            var entry = data[i + dataLoadedCount];
                            cmd.Parameters.AddWithValue($"@postid{id}", entry.postid);
                            cmd.Parameters.AddWithValue($"@wordid{id}", entry.wordid);
                        }
                        cmd.ExecuteNonQuery(); //finally execute the command
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error setting tag table: " + ex.Message);
                throw;
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
                for (int dataLoadedCount = 0; dataLoadedCount < data.Count; dataLoadedCount += batchSize)
                {
                    //add (@postid#, @wordid#, @postcreated#)
                    StringBuilder finalCommand = new StringBuilder(wordsCommand);

                    var currentBatch = data.Skip(dataLoadedCount).Take(batchSize);
                    for (int i = 0; i < currentBatch.Count(); i++) //not using for each since id has to be unique, we use the indexes as the id
                    {
                        string id = (i + dataLoadedCount).ToString(); //index as a string, unique for every entry
                        finalCommand.Append($"(@postid{id}, @wordid{id}, @postcreated{id}),");
                    }
                    finalCommand.Length--; //remove the last comma
                    using (MySqlCommand cmd = new MySqlCommand(finalCommand.ToString(), conn))
                    {
                        for (int i = 0; i < currentBatch.Count(); i++) //fill in the data of each entry
                        {
                            string id = (i + dataLoadedCount).ToString(); //index as a string, unique for every entry
                            var entry = data[i + dataLoadedCount];
                            cmd.Parameters.AddWithValue($"@postid{id}", entry.postid);
                            cmd.Parameters.AddWithValue($"@wordid{id}", entry.wordid);
                            cmd.Parameters.AddWithValue($"@postcreated{id}", entry.postcreated);
                        }
                        cmd.ExecuteNonQuery(); //finally execute the command
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error setting PostTags table: " + ex.Message);
                throw;
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
                for (int dataLoadedCount = 0; dataLoadedCount < data.Count; dataLoadedCount += batchSize)
                {
                    //add (@postid#, @wordid#, @count#, @type#, @questionid#)
                    StringBuilder finalCommand = new StringBuilder(wordsCommand);

                    var currentBatch = data.Skip(dataLoadedCount).Take(batchSize);
                    for (int i = 0; i < currentBatch.Count(); i++) //not using for each since id has to be unique, we use the indes as the id
                    {
                        string id = (i + dataLoadedCount).ToString(); //index as a string, unique for every entry
                        finalCommand.Append($"(@postid{id}, @wordid{id}, @count{id}, @type{id}, @questionid{id}),");
                    }
                    finalCommand.Length--; //remove the last comma
                    using (MySqlCommand cmd = new MySqlCommand(finalCommand.ToString(), conn))
                    {
                        for (int i = 0; i < currentBatch.Count(); i++) //fill in the data of each entry
                        {
                            string id = (i + dataLoadedCount).ToString(); //index as a string, unique for every entry
                            var entry = data[i + dataLoadedCount];
                            cmd.Parameters.AddWithValue($"@postid{id}", entry.postid);
                            cmd.Parameters.AddWithValue($"@wordid{id}", entry.wordid);
                            cmd.Parameters.AddWithValue($"@count{id}", entry.count);
                            cmd.Parameters.AddWithValue($"@type{id}", entry.type);
                            cmd.Parameters.AddWithValue($"@questionid{id}", entry.questionid);
                        }
                        cmd.ExecuteNonQuery(); //finally execute the command
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error setting content table: " + ex.Message);
                throw;
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
                for (int dataLoadedCount = 0; dataLoadedCount < data.Count; dataLoadedCount += batchSize)
                {
                    //add (@postid, @wordid) in each row
                    StringBuilder finalCommand = new StringBuilder(wordsCommand);

                    var currentBatch = data.Skip(dataLoadedCount).Take(batchSize);
                    for (int i = 0; i < currentBatch.Count(); i++) //not using for each since id has to be unique, we use the indes as the id
                    {
                        string id = (i + dataLoadedCount).ToString(); //index as a string, unique for every entry
                        finalCommand.Append($"(@postid{id}, @wordid{id}),");
                    }
                    finalCommand.Length--; //remove the last comma
                    using (MySqlCommand cmd = new MySqlCommand(finalCommand.ToString(), conn))
                    {
                        for (int i = 0; i < currentBatch.Count(); i++) //fill in the data of each entry
                        {
                            string id = (i + dataLoadedCount).ToString(); //index as a string, unique for every entry
                            var entry = data[i + dataLoadedCount];
                            cmd.Parameters.AddWithValue($"@postid{id}", entry.postid);
                            cmd.Parameters.AddWithValue($"@wordid{id}", entry.wordid);
                        }
                        cmd.ExecuteNonQuery(); //finally execute the command
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error setting title table: " + ex.Message);
                throw;
            }
            conn.Close();
        }

        //add the image blob to the table, not done in batches as byte[] will be quite long
        public void AddImage(ImageBlob data)
        {

            //command to add a new category
            string addImageCommand = "INSERT INTO qa_blobs (blobid, format, content, filename, userid, cookieid, created) " +
                "VALUES (@blobid, @format, @content, @filename, @userid, @cookieid, @created)";

            MySqlConnection conn = q2a.retrieveConnection();
            conn.Open();
            try
            {
                using (MySqlCommand cmd = new MySqlCommand(addImageCommand, conn)) //to qa_blobs
                {
                    cmd.Parameters.AddWithValue("@blobid", data.blobid);
                    cmd.Parameters.AddWithValue("@format", data.format);
                    cmd.Parameters.AddWithValue("@content", data.content);
                    cmd.Parameters.AddWithValue("@filename", data.filename);
                    cmd.Parameters.AddWithValue("@userid", data.userid);
                    cmd.Parameters.AddWithValue("@cookieid", data.cookieid);
                    cmd.Parameters.AddWithValue("@created", data.created);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error adding new image: " + ex.Message);
                throw;
            }
            conn.Close();
        }

        public void AddAccountReclaimTable()
        {
            //create the qa_account reclaim
            //fields: userid, email
            q2a.ExecuteUpdate("CREATE TABLE qa_accountreclaim (userid INT UNSIGNED PRIMARY KEY, email VARCHAR(513), " +
                              "reclaimcode CHAR(8) DEFAULT '', lastreclaim DATETIME)");
        }

        //add data to the qa_accountreclaim table, q2a userid and discourse email
        //add many rows for every insert statement, basically by having INSERT INTO qa_words (columns) VALUES (row1), (row2), ...
        public void AddAccountReclaimData(List<Q2AUser> data, int batchSize=500)
        {
            // Queries to uservotes
            string addVotesCommand = "INSERT INTO qa_accountreclaim (userid, email) VALUES ";
            MySqlConnection conn = q2a.retrieveConnection();
            conn.Open();
            try
            {
                //execute the statement in batches, total statements to execute is data.Count / batchSize
                for (int dataLoadedCount = 0; dataLoadedCount < data.Count; dataLoadedCount += batchSize)
                {
                    //add (@userid, @email) in each row
                    StringBuilder finalCommand = new StringBuilder(addVotesCommand);

                    var currentBatch = data.Skip(dataLoadedCount).Take(batchSize);
                    foreach(Q2AUser user in currentBatch)
                    {
                        string id = user.userId.ToString(); //index as a string, unique for every entry
                        finalCommand.Append($"(@userid{id}, @email{id}),");
                    }
                    finalCommand.Length--; //remove the last comma
                    using (MySqlCommand cmd = new MySqlCommand(finalCommand.ToString(), conn))
                    {
                        foreach(Q2AUser user in currentBatch) //fill in the data of each entry
                        {
                            string id = user.userId.ToString(); //index as a string, unique for every entry
                            cmd.Parameters.AddWithValue($"@userid{id}", user.userId);
                            cmd.Parameters.AddWithValue($"@email{id}", user.discourseEmail);
                        }
                        cmd.ExecuteNonQuery(); //finally execute the command
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error adding to qa_accountreclaim: " + ex.Message);
                throw;
            }
            conn.Close();
        }
    }


}
