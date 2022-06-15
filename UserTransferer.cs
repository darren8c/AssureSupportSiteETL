using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SupportSiteETL.Models.DiscourseModels;
using SupportSiteETL.Models.Q2AModels;
using MySql.Data.MySqlClient;
using System.Configuration;

namespace SupportSiteETL
{
    public class Q2AUserData
    {
        //for qa_users
        public int userId = -1;
        public string handle = "";
        public DateTime created_at = new DateTime();
        public string email = "";
        public int level = 0;
        public int flags = 0;
        public int wallposts = 0;
        //remaining fields left as null

        //for qa_userprofile
        public string about = "";
        public string location = "";
        public string name = "";
        public string website = "";

        //for qa_userpoints
        //points will be calculated from q2a by the admin
        public int qposts = 0; //number of question posts made
        public int qupvotes = 0; //number of upvotes on questions
        public int qupvoteds = 0; //number of question upvotes received
        public int upvoteds = 0; //number of total upvotes received

        //other zero fields for qa_userpoints
        public int aposts;
        public int cposts;
        public int aselects;
        public int aselecteds;
        public int qdownvotes;
        public int adownvotes;
        public int cupvotes;
        public int cdownvotes;
        public int qvoteds;
        public int avoteds;
        public int cvoteds;
        public int downvoteds;
        public int bonus;

        public Q2AUserData()
        {
            //for qa_users
            userId = -1;
            handle = "";
            created_at = new DateTime();
            email = "";
            level = 0;
            flags = 0;
            wallposts = 0;
            //remaining fields left as null

            //for qa_userprofile
            about = "";
            location = "";
            name = "";
            website = "";

            //for qa_userpoints
            //points will be calculated from q2a by the admin
            qposts = 0; //number of question posts made
            qupvotes = 0; //number of upvotes on questions
            qupvoteds = 0; //number of question upvotes received
            upvoteds = 0; //number of total upvotes received

            //these are other fields for qa_userpoints but are just going to zeros
            aposts = 0;
            cposts = 0;
            aselects = 0;
            aselecteds = 0;
            qdownvotes = 0;
            adownvotes = 0;
            adownvotes = 0;
            cupvotes = 0;
            cdownvotes = 0;
            qvoteds = 0;
            avoteds = 0;
            cvoteds = 0;
            downvoteds = 0;
            bonus = 0;
        }
    }
    //class that handles the necessary info from discourse and q2a and then can write to the q2a
    public class UserTransferer
    {
        public List<Q2AUserData> newUsers;
        private Dictionary<string, string> discourseLookupTable;
        private Dictionary<string, int> roleLevelMap;

        private QTAConnection q2aConnection;
        private DiscourseConnection discourseConnection;
        private AnonNameGen nameGen;

        public UserTransferer()
        {
            newUsers = new List<Q2AUserData>();

            nameGen = new AnonNameGen(); //generates the usernames, e.g. anon127443

            //contains all the info for what level 4 users on discourse and what new role they have on Q2A
            discourseLookupTable = new Dictionary<string, string>();
            populateLookupTable();

            roleLevelMap = new Dictionary<string, int>(); //contains mapping from role to level number, i.e. admin = 100

            q2aConnection = new QTAConnection();
            discourseConnection = new DiscourseConnection();
        }

        private void populateLookupTable()
        {
            //fill in the lookup table from old discourse usernames to their new role name under Q2A
        }

        private int getRoleMap(string role)
        {
            if (role == "Super Administrator")
                return 120;
            else if (role == "Administrator")
                return 100;
            else if (role == "Moderator")
                return 80;
            else if (role == "Editor")
                return 50;
            else if (role == "Expert")
                return 20;
            else
                return 0; //normal user
        }

        public void loadUserData() //fill in the list of new users, does not actually write any data yet
        {
            //find the last userid of the Q2A users
            int lastId = -1;
            var q2aCurrUsers = q2aConnection.GetUsers();
            for (int i = 0; i < q2aCurrUsers.Count; i++)
                if (int.Parse(q2aCurrUsers[i]["user_id"]) > lastId)
                    lastId = int.Parse(q2aCurrUsers[i]["user_id"]);
            //this will be the first id the new user receives (i.e. if the q2a site had 3 users, the first discourse user is not id 4)
            int currId = lastId + 1;

            //for each of the discourse users, fill in the needed data
            var discourseUsers = discourseConnection.GetUsers();
            foreach (var dUser in discourseUsers)
            {
                gatherData(currId, int.Parse(dUser["id"]));
                currId++;
            }
        }

        public void storeUserData() //save the loaded user data to Q2A
        {
            string _connectionString = ConfigurationManager.ConnectionStrings["q2a"].ConnectionString;
            MySqlConnection conn = new MySqlConnection(_connectionString);
            Console.WriteLine("Connecting to MySQL...");
            conn.Open();
            string sql4Users = "INSERT INTO qa_users (userid, created, email, handle, level, flags, wallposts) VALUES (@userid, @created, @email, @handle, @level, @flags, @wallposts)";
            string sql4Profiles = "Insert INTO qa_userprofile (about, location, name, website) VALUES (@about, @location, @name, @website)";
            string sql4Points = "Insert INTO qa_userpoints (qposts, qupvotes, qupvoteds, upvoteds) VALUES (@qposts, @qupvotes, @qupvoteds, @upvoteds)";
            //make a write query for each user
            foreach (var user in newUsers)
            {
                //queries for each of the q2a tables
                try
                {
                    using (MySqlCommand cmd = new MySqlCommand(sql4Users, conn))
                    {
                        MySqlDataReader rdr = cmd.ExecuteReader();
                        cmd.Parameters.AddWithValue("@userid", user.userId);
                        cmd.Parameters.AddWithValue("@created", user.created_at);
                        cmd.Parameters.AddWithValue("@email", user.email);
                        cmd.Parameters.AddWithValue("@handle", user.handle);
                        cmd.Parameters.AddWithValue("@level", user.level);
                        cmd.Parameters.AddWithValue("@flags", user.flags);
                        cmd.Parameters.AddWithValue("@wallposts", user.wallposts);
                    }
                    using (MySqlCommand cmd = new MySqlCommand(sql4Profiles, conn))
                    {
                        MySqlDataReader rdr = cmd.ExecuteReader();
                        cmd.Parameters.AddWithValue("@about", user.about);
                        cmd.Parameters.AddWithValue("@location", user.location);
                        cmd.Parameters.AddWithValue("@name", user.name);
                        cmd.Parameters.AddWithValue("@website", user.website);
                    }
                    using (MySqlCommand cmd = new MySqlCommand(sql4Points, conn))
                    {
                        MySqlDataReader rdr = cmd.ExecuteReader();
                        cmd.Parameters.AddWithValue("@qposts", user.qposts);
                        cmd.Parameters.AddWithValue("@qupvotes", user.qupvotes);
                        cmd.Parameters.AddWithValue("@qupvoteds", user.qupvoteds);
                        cmd.Parameters.AddWithValue("@upvoteds", user.upvoteds);
                    }
                }
                catch (Exception ex)
                {
                    Console.Write(ex.Message);
                }
            }
            conn.Close();
        }

        //fill in the needed data from the databases for this user
        private Q2AUserData gatherData(int userId, int dUserId)
        {
            Q2AUserData newUser = new Q2AUserData();

            newUser.userId = userId;
            newUser.handle = nameGen.getNewUser().name;

            newUser.created_at = DateTime.Now; //time doesn't matter just use now
            newUser.email = userId + "@example.com"; //just a default email

            newUser.level = getUserLevel(dUserId);

            newUser.flags = 0;
            newUser.wallposts = 0;

            newUser.about = "This is an archived user.";
            newUser.location = "";
            newUser.name = "";
            newUser.website = "";

            newUser.qposts = 0; //number of question posts made
            newUser.qupvotes = 0; //number of upvotes on questions
            newUser.qupvoteds = 0; //number of question upvotes received
            newUser.upvoteds = 0; //number of total upvotes received
            //there are other fields here that correspond to points but they are already zeros

            return newUser;
        }

        private int getUserLevel(int dUserId)
        {
            int newLevel = 0;

            //if the user isn't in the mapping it is just a user

            return newLevel;
        }
    }
}
