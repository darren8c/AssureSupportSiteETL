using System.Configuration;
using MySql.Data.MySqlClient;

namespace SupportSiteETL
{
    public class QTAConnection
    {
        private string _connectionString;

        public QTAConnection()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["q2a"].ConnectionString;
        }

        public void TestConnection()
        {
            //your MySQL connection string
            string connStr = _connectionString;

            MySqlConnection conn = new MySqlConnection(connStr);
            try
            {
                Console.WriteLine("Connecting to MySQL...");
                conn.Open();

                //SQL Query to execute
                //selecting only first 10 rows for demo
                string sql = "select * from qa_users limit 0,10;";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                MySqlDataReader rdr = cmd.ExecuteReader();

                //read the data
                while (rdr.Read())
                {
                    Console.WriteLine(rdr.GetValue(rdr.GetOrdinal("userid")) + " -- " + rdr.GetDateTime("created") + " -- " + rdr.GetString("email") + "--" + rdr.GetString("handle"));
                }

                rdr.Close();
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
            }

            conn.Close();
        }

        /// <summary>
        /// Gets all of the users from the Q2A database
        /// </summary>
        /// 
        /// <returns>
        /// A list of users, stored as dictionaries
        /// </returns>
        public List<Dictionary<string, string>> GetUsers()
        {
            return ExecuteQuery("SELECT * FROM qa_users ORDER BY userid;");
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
            var superAdmin = ExecuteQuery("SELECT userid FROM qa_users WHERE level = '120'")[0];
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
            foreach(string table in tablesToDeleteFrom) {
                string sql = string.Format("DELETE FROM {0} WHERE userid <> {1};", table, superAdminId);
                result = ExecuteUpdate(sql);
                rowsAffected += result;

                Console.WriteLine(string.Format("Deleted {0} rows from {1}", result, table));
            }

            // Separate query because the ID field is named different in this table
            string sharedevents = string.Format("DELETE FROM qa_sharedevents WHERE lastuserid <> {0};", superAdminId);
            result = ExecuteUpdate(sharedevents);
            Console.WriteLine(string.Format("Deleted {0} rows from qa_sharedevents", result));

            // Compute the number of users deleted
            return rowsAffected;
        }

        public int DeletePosts() {
            int rowsAffected = 0;

            string sql = "DELETE FROM qa_posts";

            rowsAffected = ExecuteUpdate(sql);
            return rowsAffected;
        }

        /// <summary>
        /// Executes a query on the Discourse database, returning the result as a list of dictionaries.
        /// 
        /// </summary>
        /// <param name="query">The query to execute</param>
        /// <returns>
        /// List of dictionaries where each entry represents a single row in the query, with the keys being row/field names.
        /// </returns>
        public List<Dictionary<string, string>> ExecuteQuery(string query)
        {
            List<Dictionary<string, string>> users = new List<Dictionary<string, string>>();

            using MySqlConnection conn = new MySqlConnection(_connectionString);
            try
            {
                conn.Open();

                // Retrieve all rows
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Create a new Dictionary to represent the user
                        Dictionary<string, string> user = new Dictionary<string, string>();

                        // Iterate over all fields in the row
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            // Get the field's name
                            string fieldName = reader.GetName(i);
                            // Fetch the value as a string, defaulting to "NULL" if the value doesn't exist
                            string value = reader.GetValue(i).ToString() ?? "NULL";

                            // Add the fieldname and value to the user/dictionary
                            user.Add(fieldName, value);
                        }

                        users.Add(user);
                    }
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
            }
            finally
            {
                conn.Close();
            }

            return users;
        }


        /// <summary>
        /// Executes an update, such as INSERT or DELETE, on the Discourse database.
        /// </summary>
        /// <param name="statement">The statement to execute</param>
        /// <returns>
        /// The number of rows affected, if any, else -1.
        /// </returns>
        public int ExecuteUpdate(string statement)
        {
            int rowsAffected = -1;

            using MySqlConnection conn = new MySqlConnection(_connectionString);
            try
            {
                conn.Open();

                // Execute the command and retrieve the number of rows affected
                using (MySqlCommand cmd = new MySqlCommand(statement, conn))
                {
                    rowsAffected = cmd.ExecuteNonQuery();
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
            }
            finally
            {
                conn.Close();
            }

            return rowsAffected;
        }
    }
}