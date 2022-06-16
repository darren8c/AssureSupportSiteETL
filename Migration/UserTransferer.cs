using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SupportSiteETL.Models.DiscourseModels;
using SupportSiteETL.Models.Q2AModels;
using MySql.Data.MySqlClient;
using System.Configuration;
using SupportSiteETL.Databases;

namespace SupportSiteETL.Migration
{
    //class that handles the necessary info from discourse and q2a and then can write to the q2a
    public class UserTransferer
    {
        public List<Q2AUserData> newUsers;
        public Dictionary<int, string> discourseLookupTable;

        private Q2AConnection q2aConnection;
        private DiscourseConnection discourseConnection;
        private AnonNameGen nameGen;

        public UserTransferer()
        {
            newUsers = new List<Q2AUserData>();
            nameGen = new AnonNameGen(); //generates the usernames, e.g. anon127443

            //contains all the info for what level 4 users on discourse and what new role they have on Q2A
            discourseLookupTable = new Dictionary<int, string>();
            populateLookupTable();

            q2aConnection = new Q2AConnection();
            discourseConnection = new DiscourseConnection();
        }

        private void populateLookupTable()
        {
            //fill in the lookup table from old discourse usernames to their new role name under Q2A
            var lines = File.ReadLines("Resources/roleMappings.txt");
            foreach (string line in lines)
            {
                string[] data = line.Split(',');
                if (data.Length != 2) //there should always be 2 fields (discourse_user_id, newRole)
                {
                    Console.WriteLine("Error, roleMappings.txt is not in the correct format!");
                    return;
                }
                int id = int.Parse(data[0]);
                string newRole = data[1];

                discourseLookupTable.Add(id, newRole);
            }
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
            int lastId = -1000; //arbitrary minimum
            var q2aCurrUsers = q2aConnection.GetUsers();
            foreach (var user in q2aCurrUsers)
                if (int.Parse(user["userid"]) > lastId)
                    lastId = int.Parse(user["userid"]);
            //this will be the first id the new user receives (i.e. if the q2a site had 3 users, the first discourse user is not id 4)
            int currId = lastId + 1;

            //for each of the discourse users, fill in the needed data
            var discourseUsers = discourseConnection.GetUsers();
            foreach (var dUser in discourseUsers)
            {
                newUsers.Add(gatherData(currId, dUser));
                currId++;
            }
        }

        public async void storeUserData() //save the loaded user data to Q2A
        {
            Console.WriteLine("Transfering users...");
            //add all the user data over to the tables
            foreach (var user in newUsers)
                addUser(user); //add each users piece of data
            
            q2aConnection.UpdateUserCount(); //update user count so the site has the User count stat correct.

        }

        //add user data to all the different tables
        private void addUser(Q2AUserData user)
        {
            MySqlConnection conn = q2aConnection.retrieveConnection();

            // Queries to insert users
            string sql4Users = "INSERT INTO qa_users (userid, created, createip, loggedin, loginip, email, handle, level, flags, wallposts) VALUES (@userid, @created, @createip, @loggedin, @loginip, @email, @handle, @level, @flags, @wallposts)";
            string sql4Points = "Insert INTO qa_userpoints (userid, points, qposts, qupvotes, qvoteds, upvoteds) VALUES (@userid, @points, @qposts, @qupvotes, @qvoteds, @upvoteds)";
            //the profile writing is actually 4 inserts
            string sql4Profiles = "Insert INTO qa_userprofile (userid, title, content) VALUES (@userid, @title, @content)";
            
            conn.Open();
            try
            {
                using (MySqlCommand cmd = new MySqlCommand(sql4Users, conn)) //to qa_users
                {
                    cmd.Parameters.AddWithValue("@userid", user.userId);
                    cmd.Parameters.AddWithValue("@created", user.created_at);
                    cmd.Parameters.AddWithValue("@createip", "");
                    cmd.Parameters.AddWithValue("@loggedin", user.loggedin);
                    cmd.Parameters.AddWithValue("@loginip", "");
                    cmd.Parameters.AddWithValue("@email", user.email);
                    cmd.Parameters.AddWithValue("@handle", user.handle);
                    cmd.Parameters.AddWithValue("@level", user.level);
                    cmd.Parameters.AddWithValue("@flags", user.flags);
                    cmd.Parameters.AddWithValue("@wallposts", user.wallposts);
                    cmd.ExecuteNonQuery();
                }
                using (MySqlCommand cmd = new MySqlCommand(sql4Points, conn)) //to qa_points
                {
                    cmd.Parameters.AddWithValue("@userid", user.userId);
                    cmd.Parameters.AddWithValue("@qposts", user.qposts);
                    cmd.Parameters.AddWithValue("@qupvotes", user.qupvotes);
                    cmd.Parameters.AddWithValue("@qvoteds", user.qvoteds);
                    cmd.Parameters.AddWithValue("@upvoteds", user.upvoteds);
                    cmd.Parameters.AddWithValue("@points", user.points);
                    cmd.ExecuteNonQuery();
                }
                //each of the profile writes
                using (MySqlCommand cmd = new MySqlCommand(sql4Profiles, conn)) //about
                {
                    cmd.Parameters.AddWithValue("@userid", user.userId);
                    cmd.Parameters.AddWithValue("@title", "about");
                    cmd.Parameters.AddWithValue("@content", user.about);
                    cmd.ExecuteNonQuery();
                }
                using (MySqlCommand cmd = new MySqlCommand(sql4Profiles, conn)) //location
                {
                    cmd.Parameters.AddWithValue("@userid", user.userId);
                    cmd.Parameters.AddWithValue("@title", "location");
                    cmd.Parameters.AddWithValue("@content", user.location);
                    cmd.ExecuteNonQuery();
                }
                using (MySqlCommand cmd = new MySqlCommand(sql4Profiles, conn)) //name
                {
                    cmd.Parameters.AddWithValue("@userid", user.userId);
                    cmd.Parameters.AddWithValue("@title", "name");
                    cmd.Parameters.AddWithValue("@content", user.name);
                    cmd.ExecuteNonQuery();
                }
                using (MySqlCommand cmd = new MySqlCommand(sql4Profiles, conn)) //website
                {
                    cmd.Parameters.AddWithValue("@userid", user.userId);
                    cmd.Parameters.AddWithValue("@title", "website");
                    cmd.Parameters.AddWithValue("@content", user.website);
                    cmd.ExecuteNonQuery();
                }

                Console.WriteLine(string.Format("Inserted user with ID: {0} - {1}", user.userId, user.handle));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error adding new user: " + ex.Message);
            }
            conn.Close();
        }

        //fill in the needed data from the databases for this user
        private Q2AUserData gatherData(int userId, Dictionary<string, string> dUser)
        {
            Q2AUserData newUser = new Q2AUserData();

            int dUserId = int.Parse(dUser["id"]);
            newUser.userId = userId;
            newUser.handle = nameGen.getNewUser().name;

            newUser.created_at = DateTime.Now; //time doesn't matter just use now
            newUser.loggedin = DateTime.Now;
            newUser.email = newUser.handle + "@example.com"; //just a default email

            newUser.level = 0; //default user
            if (discourseLookupTable.ContainsKey(dUserId)) //check for a mapping
                newUser.level = getRoleMap(discourseLookupTable[dUserId]); //oldId -> newRole -> roleLevel#

            newUser.flags = 0;
            newUser.wallposts = 0;

            newUser.about = "This is an archived user.";
            newUser.location = "";
            newUser.name = "";
            newUser.website = "";

            //these analogs aren't perfect matches but they are likely close enough
            newUser.qposts = int.Parse(dUser["post_count"]); //number of question posts made
            newUser.qupvotes = int.Parse(dUser["likes_given"]); //number of upvotes on questions
            newUser.qvoteds = int.Parse(dUser["likes_received"]); //number of question upvotes received
            newUser.upvoteds = newUser.qvoteds; //number of total upvotes received


            //basic formula to calculate points
            int basePoints = 100;
            int mult = 10; //multiply value for all other point sums
            newUser.points = 100 + mult * (2 * newUser.qposts + newUser.qupvotes + newUser.qvoteds);
            //there are other fields here that correspond to points but they are already zeros


            return newUser;
        }
    }
}
