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

        public List<Q2AUser> GetUsers()
        {
            List<Q2AUser> q2AUsers = new List<Q2AUser>();

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
                    Q2AUser qtaUser = ReadQ2AUser(rdr);
                    
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
        public static Q2AUser ReadQ2AUser(MySqlDataReader rdr) {
            Q2AUser qtaUser = new Q2AUser(
                SafeGetInt(rdr, 0),
                SafeGetDateTime(rdr, 1),
                SafeGetVarBinary(rdr, 2), // This needs to be encoded
                SafeGetString(rdr, 3),
                SafeGetString(rdr, 4),
                SafeGetInt(rdr, 5),
                SafeGetInt(rdr, 6),
                SafeGetInt(rdr, 7),
                SafeGetString(rdr, 8),
                SafeGetString(rdr, 9),
                SafeGetString(rdr, 10),
                SafeGetInt(rdr, 11),
                SafeGetDateTime(rdr, 12),
                SafeGetVarBinary(rdr, 13), // This also needs to be encoded
                SafeGetDateTime(rdr, 14),
                SafeGetVarBinary(rdr, 15), // As does this
                SafeGetString(rdr, 16),
                SafeGetString(rdr, 17),
                SafeGetString(rdr, 18),
                SafeGetInt(rdr, 19),
                SafeGetInt(rdr, 20)
            );

            return qtaUser;
        }

        // Safely fetches the integer value a the column index,
        // Returns `int.MaxValue` if the entry is null
        public static int SafeGetInt(MySqlDataReader rdr, int colIndex) {
            if (!rdr.IsDBNull(colIndex)) {
                return rdr.GetInt32(colIndex);
            }
            return int.MaxValue;
        }

        // Safely fetches the string value a the column index,
        // Returns `"NULL"` if the entry is null
        public static string SafeGetString(MySqlDataReader rdr, int colIndex) {
            if (!rdr.IsDBNull(colIndex) && !string.IsNullOrEmpty(rdr.GetString(colIndex))) {
                return rdr.GetString(colIndex);
            }
            return "NULL";
        }

        // Safely fetches the VarBinary value a the column index,
        // Returns `"NULL"` if the entry is null
        public static string SafeGetVarBinary(MySqlDataReader rdr, int colIndex) {
            if (!rdr.IsDBNull(colIndex)) {
                return rdr.GetString(colIndex);
            }
            return "NULL";
        }

        // Safely fetches the DateTime value a the column index,
        // Returns `DateTime.MaxValue` if the entry is null
        public static DateTime SafeGetDateTime(MySqlDataReader rdr, int colIndex) {
            if (!rdr.IsDBNull(colIndex)) {
                return rdr.GetDateTime(colIndex);
            }
            return DateTime.MaxValue;
        }
    }
}