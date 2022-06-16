using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportSiteETL.Migration
{
    public class Q2AUserData
    {
        //for qa_users
        public int userId = -1;
        public string handle = "";
        public DateTime created_at;
        public DateTime loggedin;
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
        public int points;
        public int qposts = 0; //number of question posts made
        public int qupvotes = 0; //number of upvotes on questions
        public int qvoteds = 0; //number of question upvotes received
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
        public int avoteds;
        public int cvoteds;
        public int downvoteds;
        public int bonus;

        public Q2AUserData()
        {
            //for qa_users
            userId = -1;
            handle = "";
            DateTime created_at = new DateTime();
            DateTime loggedin = new DateTime();
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
            qvoteds = 0; //number of question upvotes received
            upvoteds = 0; //number of total upvotes received
            points = 0;

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
            avoteds = 0;
            cvoteds = 0;
            downvoteds = 0;
            bonus = 0;
        }
    }
}
