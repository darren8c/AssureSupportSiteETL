using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportSiteETL
{
    public struct AnonName
    {
        public string name;
        //anything else needed?
    }

    //stores all generated names and generates new names
    public class AnonNameGen
    {
        private List<AnonName> users;
        private int randDigitLength; //how many random digits after the "anon"

        public AnonNameGen() //constructor, nothing needed
        {
            users = new List<AnonName>();
            randDigitLength = 6;
        }

        public AnonName getNewUser()
        {
            Random random = new Random(); //for random digits
            AnonName newUser;
            newUser.name = "ERROR, MAX USERS!"; //in case max users, this will be returned
            if (users.Count == Math.Pow(10, randDigitLength)) //max users, no more can be generated
                return newUser;

            bool valid = false;
            while(!valid)
            {
                //generate a random name like anon274375
                newUser.name = "anon";
                for (int i = 0; i < randDigitLength; i++) //add 6 random digits
                    newUser.name += (random.Next() % 10).ToString(); //random "0" through "9"

                //check to make sure this name doesn't exist
                valid = true;
                foreach(AnonName user in users)
                {
                    if(user.name == newUser.name)
                    {
                        valid = false;
                        break; //match, not a valid name, redo process
                    }
                }
            }
            //valid newUser, add to list and return
            users.Add(newUser);
            return newUser;
        }
    }
}
