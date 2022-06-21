using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportSiteETL.Migration.Transform.Models
{
    //keeps track of all post info
    public class Q2APost
    {
        public uint postid;
        public string type; //Q, A, C (attach _HIDDEN to make it hidden)
        public int? parentid; //the parent post id, allows null type
        public int? categoryid; //the category id, allows null type
        public int? catidpath1; //catidpath, should be the same as categoryid
        public int acount; //number of answers
        public int amaxvote; //the highest voted answer
        public int? userid; //userid of poster

        public int? selchildid; //the id of the selected answer (null if no selected answer)

        public int flagcount;

        public int upvotes; //upvotes
        public int downvotes; //downvotes on post
        public int netvotes; //total sum upvotes - downvotes

        public int views; //number of views

        public string format; //format of post (should be html)
        public string title; //title of post (only questions have titles)

        public string content; //the post content (use tags if format is html)

        public string notify; //@ to notify via email, should be blank normally

        public DateTime created; //date of creation yyyy-mm-dd hh:mm:ss
        public DateTime? updated; //date of update, can be null
        public string? updateType; //type of update, H is updated is non blank

        //for qa_uservotes
        public UserVote[] votes;

        public Q2APost()
        {
            //give default values, most of these fields should be set elsewhere
            postid = 1;
            type = "";
            parentid = null;
            categoryid = null;
            catidpath1 = null;
            acount = 0;
            amaxvote = 0;
            userid = 0;

            selchildid = null;

            flagcount = 0;

            upvotes = 0;
            downvotes = 0;
            netvotes = 0;

            views = 0;

            format = "html";
            title = "";
            content = "";

            notify = "";

            created = DateTime.Now;
            updated = null;
            updateType = null;
        }
    }   
    public class UserVote
    {
        public int postid;
        public int userid;
        public int vote;
        public int flag;
        public DateTime votecreated;
        public DateTime voteupdated;

        public UserVote()
        {
            //give default values, most of these fields should be set elsewhere
            postid = 0;
            userid = 1;
            vote = 1;
            flag = 0;
            votecreated = DateTime.Now;
            voteupdated = DateTime.Now;
        }
    }
}
