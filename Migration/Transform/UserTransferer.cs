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
using SupportSiteETL.Migration.Extract;
using SupportSiteETL.Migration.Load;

namespace SupportSiteETL.Migration.Transform
{
    //class that handles the necessary info from discourse and q2a and then can write to the q2a
    public class UserTransferer
    {
        public List<Q2AUserData> newUsers;
        public Dictionary<int, string> discourseLookupTable;

        private Extractor extractor;
        private Loader loader;
        private AnonNameGen nameGen;

        public UserTransferer()
        {
            newUsers = new List<Q2AUserData>();
            nameGen = new AnonNameGen(); //generates the usernames, e.g. anon127443

            //contains all the info for what level 4 users on discourse and what new role they have on Q2A
            discourseLookupTable = new Dictionary<int, string>();
            populateLookupTable();

            extractor = new Extractor();
            loader = new Loader();
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

        public void ExtractUserData() //fill in the list of new users, does not actually write any data yet
        {
            //find the last userid of the Q2A users
            int lastId = -1000; //arbitrary minimum
            var q2aCurrUsers = extractor.GetQ2AUsers();
            foreach (var user in q2aCurrUsers)
                if (int.Parse(user["userid"]) > lastId)
                    lastId = int.Parse(user["userid"]);
            //this will be the first id the new user receives (i.e. if the q2a site had 3 users, the first discourse user is not id 4)
            int currId = lastId + 1;

            //for each of the discourse users, fill in the needed data
            var discourseUsers = extractor.GetDiscourseUsers();
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
                loader.addUser(user); //add each users piece of data

            loader.UpdateSiteStats(); //update user count so the site has the User count stat correct.

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
            newUser.points = basePoints + mult * (2 * newUser.qposts + newUser.qupvotes + newUser.qvoteds);
            //there are other fields here that correspond to points but they are already zeros

            return newUser;
        }
    }
}
