using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using SupportSiteETL.Models.DiscourseModels;

namespace SupportSiteETL
{
    public class DiscourseConnection
    {
        // Connection information for the discourse database. Sourced from `App.config`
        private string _connectionString;

        public DiscourseConnection()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["discourse"].ConnectionString;
        }

        public void TestConnection()
        {
            using NpgsqlConnection conn = new NpgsqlConnection(_connectionString);
            try
            {
                Console.WriteLine("Connecting to PostgreSQL...");
                conn.Open();

                string sql = "select * from public.users;";
                // Retrieve all rows
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        Console.WriteLine(reader.GetInt32(0));
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
            }

            conn.Close();
        }

        // Retrieves all users from the joined table of public.users and public.user_stats.
        // Returns a list of users, where each user is a dictionary with keys being database field names.
        public List<Dictionary<string, string>> GetUsers()
        {
            List<Dictionary<string, string>> discourseUsers = new List<Dictionary<string, string>>();

            using NpgsqlConnection conn = new NpgsqlConnection(_connectionString);
            try
            {
                conn.Open();

                //string sql = "select * from public.users order by id asc;";
                //string sql = "select * from public.users join public.user_stats on public.users.id=public.user_stats.user_id limit 10;";
                string sql = "select * from public.users join public.user_stats on public.users.id=public.user_stats.user_id;";

                // Retrieve all rows
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
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

                        discourseUsers.Add(user);
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


            return discourseUsers;
        }
    }
}
