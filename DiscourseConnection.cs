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

        public List<Dictionary<string, string>> GetUsers()
        {
            List<Dictionary<string, string>> discourseUsers = new List<Dictionary<string, string>>();

            using NpgsqlConnection conn = new NpgsqlConnection(_connectionString);
            try
            {
                conn.Open();

                string sql = "select * from public.users;";
                // Retrieve all rows
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Dictionary<string, string> discourseUser = ReadDiscourseUser(reader);

                        discourseUsers.Add(discourseUser);
                    }
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
            }

            conn.Close();
            return discourseUsers;
        }

        public static Dictionary<string, string> ReadDiscourseUser(NpgsqlDataReader reader)
        {

            // Create a new Dictionary to represent the user
            Dictionary<string, string> user = new Dictionary<string, string>();

            // Define all fields to fetch
            string[] columnNames = {
                "id",
                "username",
                "created_at",
                "updated_at",
                "name",
                "seen_notification_id",
                "last_posted_at",
                "password_hash",
                "salt",
                "active",
                "username_lower",
                "last_seen_at",
                "admin",
                "last_emailed_at",
                "trust_level",
                "approved",
                "approved_by_id",
                "approved_at",
                "previous_visit_at",
                "suspended_at",
                "suspended_till",
                "date_of_birth",
                "views",
                "flag_level",
                "ip_address",
                "moderator",
                "title",
                "uploaded_avatar_id",
                "locale",
                "primary_group_id",
                "registration_ip_address",
                "staged",
                "first_seen_at",
                "silenced_till",
                "group_locked_trust_level",
                "manual_locked_trust_level",
                "secure_identifier",
                "flair_group_id"
            };

            // Add all the fields in key-value pairs
            foreach (var fieldName in columnNames)
            {
                user.Add(fieldName, SafeGetData(reader, fieldName));
            }

            return user;
        }

        // Safely fetches the data value at the column index as a string,
        // Returns `"NULL"` if the entry is null
        public static string SafeGetData(NpgsqlDataReader rdr, string colName)
        {
            int colIndex = rdr.GetOrdinal(colName);

            if (!rdr.IsDBNull(colIndex))
            {
                return rdr.GetValue(colIndex).ToString() ?? "NULL";
            }
            return "NULL";
        }
    }
}
