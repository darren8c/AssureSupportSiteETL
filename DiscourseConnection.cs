using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Npgsql;
using SupportSiteETL.Models;

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

        public List<DiscourseUser> GetUsers()
        {
            List<DiscourseUser> discourseUsers = new List<DiscourseUser>();
        
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
                        DiscourseUser discourseUser = new DiscourseUser();
                        discourseUser.Id = reader.GetValue(reader.GetOrdinal("id")).ToString();
                        discourseUser.Username = reader.GetString(reader.GetOrdinal("username"));
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
    }
}
