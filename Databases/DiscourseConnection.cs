using System.Configuration;
using Npgsql;

namespace SupportSiteETL.Databases
{
    public class DiscourseConnection
    {
        // Connection information for the discourse database. Sourced from `App.config`
        private string _connectionString;

        // Constructor to set up connection string
        public DiscourseConnection()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["discourse"].ConnectionString;
        }
        public NpgsqlConnection retrieveConnection()
        {
            string connStr = _connectionString;
            return new NpgsqlConnection(connStr);
        }

        // Tests the connection to the database.
        // Returns true if the query was successful, else false
        public bool TestConnection()
        {
            bool result = true;
            using NpgsqlConnection conn = new NpgsqlConnection(_connectionString);
            try
            {
                Console.WriteLine("Connecting to PostgreSQL...");
                conn.Open();

                string sql = "select * from public.users limit 10;";
                // Retrieve all rows
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        Console.WriteLine(reader.GetValue(0).ToString() ?? "NULL");
                }
            }
            catch (Exception err)
            {
                result = false;
                Console.WriteLine(err.ToString());
            }

            conn.Close();
            return result;
        }
        
        // Executes an update, such as INSERT or DELETE, on the Discourse database.
        // Returns the number of rows affected, if any, else -1.
        public int ExecuteUpdate(string statement)
        {
            int rowsAffected = -1;

            using NpgsqlConnection conn = new NpgsqlConnection(_connectionString);
            try
            {
                conn.Open();

                // Execute the command and retrieve the number of rows affected
                using (NpgsqlCommand cmd = new NpgsqlCommand(statement, conn))
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

        // Executes a query on the Discourse database, returning the result as a list of dictionaries.
        // Each entry represents a single row in the query, with the keys being row/field names.
        public List<Dictionary<string, string>> ExecuteQuery(string query)
        {
            List<Dictionary<string, string>> data = new List<Dictionary<string, string>>();

            using NpgsqlConnection conn = new NpgsqlConnection(_connectionString);
            try
            {
                conn.Open();

                // Retrieve all rows
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                using (NpgsqlDataReader reader = cmd.ExecuteReader())
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

                        data.Add(user);
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

            return data;
        }

    }

}
