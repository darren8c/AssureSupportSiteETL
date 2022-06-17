using System.Configuration;
using MySql.Data.MySqlClient;

namespace SupportSiteETL.Databases
{
    public class Q2AConnection
    {
        private string _connectionString;

        public Q2AConnection()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["q2a"].ConnectionString;
        }
        public MySqlConnection retrieveConnection()
        {
            string connStr = _connectionString;
            return new MySqlConnection(connStr);
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