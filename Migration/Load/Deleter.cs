using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SupportSiteETL.Databases;
using MySql.Data.MySqlClient;

namespace SupportSiteETL.Migration.Load
{
    //handles clearing the q2a site of all added data
    public class Deleter
    {
        public Q2AConnection q2a;
        public Loader loader;

        public Deleter()
        {
            q2a = new Q2AConnection();
            loader = new Loader();
        }

        //delete from the table, optionally specifiy an error message
        //deletefrom can also specify a where parameter
        ///i.e.: DELETE FROM qa_users WHERE userid!=1, deleteFrom: qa_users where userid!=1 
        public void DeleteFromTable(string deleteFrom, string? errorMessage=null)
        {
            string deleteCommand = "DELETE FROM " + deleteFrom; //delete from specified table

            MySqlConnection conn = q2a.retrieveConnection();
            conn.Open();
            try
            {
                using (MySqlCommand cmd = new MySqlCommand(deleteCommand, conn)) //remove all data in table
                {
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                if (errorMessage != null) //argument specified 
                    Console.WriteLine($"{errorMessage}: {ex.Message}");
                else //default error message
                    Console.WriteLine($"Error when deleteing from {deleteFrom}: {ex.Message}");
                throw;
            }
            conn.Close();
        }

        /// <summary>
        /// Deletes all users in the Q2A database EXCEPT the Super Admin (first account created)
        /// This removes the user's information from the following tables:
        /// </summary>
        /// 
        /// <returns>
        /// The number of users deleted
        /// </returns>
        public void DeleteUsers()
        {
            //all users like anon123456, any other users will be kept
            var usersToDelete = q2a.ExecuteQuery("SELECT userid FROM qa_users where handle like 'anon______'");

            if (usersToDelete.Count == 0) //no users to delete, return
                return;

            //looks like "where userid in (2,3,4,5,...)
            StringBuilder deleteSpecifier = new StringBuilder(" where userid in ( ");
            for (int i = 0; i + 1 < usersToDelete.Count; i++) //all but the last
                deleteSpecifier.Append(usersToDelete[i]["userid"] + ", ");
            deleteSpecifier.Append(usersToDelete.Last()["userid"] + ")"); //close, e.g. ... 853, 854)
            string deleteUserRange = deleteSpecifier.ToString();


            // All tables to delete the user from
            string[] tablesToDeleteFrom = {
                "qa_userevents",
                "qa_userfavorites",
                "qa_userlevels",
                "qa_userlimits",
                "qa_userlogins",
                "qa_usermetas",
                "qa_usernotices",
                "qa_userpoints",
                "qa_userprofile",
                "qa_users",
                "qa_uservotes",
                // - "qa_userfields", // This doesn't contain any user-specific information
            };

            // Execute a delete statement for each table, keeping the non ported over users
            foreach (string table in tablesToDeleteFrom)
                DeleteFromTable(table + deleteUserRange, "Error deleting from " + table);
            // Separate query because the ID field is named different in this table
            DeleteFromTable("qa_sharedevents" + deleteUserRange.Replace("userid", "lastuserid"), "Error deleting from qa_sharedevents");

            loader.UpdateSiteStats(); //update settings table
        }


        //remove all the posts on the site, this clears all data from tables relating to posts, tags, likes, etc.
        public void DeletePosts()
        {
            // All tables to clear
            string[] tableList = {
                "qa_tagwords",
                "qa_titlewords",
                "qa_contentwords",
                "qa_posttags",
                "qa_uservotes",
                "qa_words"
            };
            foreach (string table in tableList)
                DeleteFromTable(table);
            //deleting from qa_posts is more difficult because foreign key constraints
            DeleteFromTable("qa_posts ORDER BY postid DESC", "Error deleting posts");
        }

        //remove the categories table data
        public void DeleteCategories()
        {
            DeleteFromTable("qa_categories");
        }

        //remove all the entries in the tables related to words and searching
        public void DeleteWordTables()
        {
            // All tables to clear
            string[] tableList = {
                "qa_titlewords",
                "qa_contentwords",
                "qa_tagwords",
                "qa_posttags",
                "qa_words",
            };
            foreach (string table in tableList)
                DeleteFromTable(table);
        }

        //delete the images migrated over, these are marked with Discourse_ at the beginning of the filename
        public void DeleteImages()
        {
            //delete images where the fileanames starts with Discourse_
            DeleteFromTable("qa_blobs where filename like 'Discourse_%'", "Error deleting images");
        }

        //delete data from qa_accountreclaim and the table itself
        public void DeleteAccountReclaim()
        {
            string deleteCommand = "DROP TABLE IF EXISTS qa_accountreclaim"; //delete the table

            MySqlConnection conn = q2a.retrieveConnection();
            conn.Open();
            try
            {
                using (MySqlCommand cmd = new MySqlCommand(deleteCommand, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error when deleteing qa_accountreclaim: " + ex.Message);
                throw;
            }
            conn.Close();
        }
    }
}
