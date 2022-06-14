using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using SupportSiteETL.Models.Q2AModels;

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

        public List<Dictionary<string, string>> GetUsers()
        {
            List<Dictionary<string, string>> q2AUsers = new List<Dictionary<string, string>>();

            MySqlConnection conn = new MySqlConnection(_connectionString);
            try
            {
                conn.Open();

                //SQL Query to execute
                string sql = "select * from qa_users";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                MySqlDataReader rdr = cmd.ExecuteReader();

                //read the data
                while (rdr.Read())
                {
                    Dictionary<string, string> qtaUser = ReadQ2AUser(rdr);

                    q2AUsers.Add(qtaUser);
                }

                rdr.Close();
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
            }

            conn.Close();
            return q2AUsers;
        }

        // From the reader, build out a new Q2A user
        public static Dictionary<string, string> ReadQ2AUser(MySqlDataReader rdr)
        {
            // Create a new Dictionary to represent the user
            Dictionary<string, string> user = new Dictionary<string, string>();

            // Define all fields to fetch
            string[] columnNames = {
                "userid",
                "created",
                "createip",
                "email",
                "handle",
                "avatarblobid",
                "avatarwidth",
                "avatarheight",
                "passsalt",
                "passcheck",
                "passhash",
                "level",
                "loggedin",
                "loginip",
                "written",
                "writeip",
                "emailcode",
                "sessioncode",
                "sessionsource",
                "flags",
                "wallposts"
            };

            // Add all the fields in key-value pairs
            foreach (var fieldName in columnNames)
            {
                user.Add(fieldName, SafeGetData(rdr, fieldName));
            }

            return user;
        }

        // Safely fetches the data value at the column index as a string,
        // Returns `"NULL"` if the entry is null
        public static string SafeGetData(MySqlDataReader rdr, string colName)
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