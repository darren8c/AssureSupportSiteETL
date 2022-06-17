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

        /// <summary>
        /// Deletes all users in the Q2A database EXCEPT the Super Admin (first account created)
        /// This removes the user's information from the following tables:
        /// </summary>
        /// 
        /// <returns>
        /// The number of users deleted
        /// </returns>
        public int DeleteUsers()
        {
            int rowsAffected = 0;
            int result = 0;

            // We fetch [0] because there should only be one super-admin
            var superAdmin = q2a.ExecuteQuery("SELECT userid FROM qa_users WHERE level = '120'")[0];
            string superAdminId = superAdmin["userid"];

            // All tables to delete the user from
            string[] tablesToDeleteFrom = {
                "qa_userevents",
                "qa_userfavorites",
                // "qa_userfields", // This doesn't contain any user-specific information
                "qa_userlevels",
                "qa_userlimits",
                "qa_userlogins",
                "qa_usermetas",
                "qa_usernotices",
                "qa_userpoints",
                "qa_userprofile",
                "qa_users",
                "qa_uservotes",
            };

            // Execute a delete statement for each table, keeping the super-admin
            foreach (string table in tablesToDeleteFrom)
            {
                string sql = string.Format("DELETE FROM {0} WHERE userid <> {1};", table, superAdminId);
                result = q2a.ExecuteUpdate(sql);
                rowsAffected += result;

                Console.WriteLine(string.Format("Deleted {0} rows from {1}", result, table));
            }

            // Separate query because the ID field is named different in this table
            string sharedevents = string.Format("DELETE FROM qa_sharedevents WHERE lastuserid <> {0};", superAdminId);
            result = q2a.ExecuteUpdate(sharedevents);
            Console.WriteLine(string.Format("Deleted {0} rows from qa_sharedevents", result));

            loader.UpdateUserCount(); //update settings table with correct user count

            // Compute the number of users deleted
            return rowsAffected;
        }

        public int DeletePosts()
        {
            int rowsAffected = 0;

            string sql = "DELETE FROM qa_posts";

            rowsAffected = q2a.ExecuteUpdate(sql);
            return rowsAffected;
        }
    }
}
