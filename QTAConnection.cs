using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using SupportSiteETL.Models;

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
                    Q2AUser qtaUser = new Q2AUser();
                    qtaUser.Id = rdr.GetValue(rdr.GetOrdinal("userid")).ToString();
                    qtaUser.Username = rdr.GetString("handle");
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
    }
}
