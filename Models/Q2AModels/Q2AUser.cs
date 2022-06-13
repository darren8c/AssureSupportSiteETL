using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportSiteETL.Models.Q2AModels
{
    public class Q2AUser
    {

        // These properties are defined by the Q2A database schema
        #region Properties
        public int UserId {get;set; }

        public DateTime Created { get; set; }

        public string CreateIp { get; set; }

        public string Email { get; set; }

        public string Handle { get; set; }

        public int AvatarBlobId{ get; set; }

        public int AvatarWidth { get; set; }

        public int AvatarHeight { get; set; }

        public string PassSalt { get; set; }

        public string PassCheck { get; set; }

        public string PassHash { get; set; }

        public int Level { get; set; }

        public DateTime LoggedIn { get; set; }

        public string LoginIp { get; set; }

        public DateTime Written { get; set; }

        public string WriteIp { get; set; }

        public string EmailCode { get; set; }

        public string SessionCode { get; set; }

        public string SessionSource { get; set; }

        public int Flags { get; set; }

        public int WallPosts { get; set; }

        #endregion


        public Q2AUser(int userid, DateTime created, string createip, string email, string handle, int avatarblobid, int avatarwidth, int avatarheight, string passsalt, string passcheck, string passhash, int level, DateTime loggedin, string loginip, DateTime written, string writeip, string emailcode, string sessioncode, string sessionsource, int flags, int wallposts) {
            UserId = userid;
            Created = created;
            CreateIp = createip;
            Email = email;
            Handle = handle;
            AvatarBlobId = avatarblobid;
            AvatarWidth = avatarwidth;
            AvatarHeight = avatarheight;
            PassSalt = passsalt;
            PassCheck = passcheck;
            PassHash = passhash;
            Level = level;
            LoggedIn = loggedin;
            LoginIp = loginip;
            Written = written;
            WriteIp = writeip;
            EmailCode = emailcode;
            SessionCode = sessioncode;
            SessionSource = sessionsource;
            Flags = flags;
            WallPosts = wallposts;
        }

        // Returns the `NAME: VAUE` of every field, separated by newlines
        public override string ToString()
        {
            // Modified from https://stackoverflow.com/questions/4023462/how-do-i-automatically-display-all-properties-of-a-class-and-their-values-in-a-s
            return GetType().GetProperties()
                .Select(info => (info.Name, Value: info.GetValue(this, null) ?? "NULL"))
                .Aggregate(
                    new StringBuilder(),
                    (sb, pair) => sb.AppendLine($"{pair.Name}: {pair.Value}"),
                    sb => sb.ToString());
        }

        // Returns the comma separated values of every field of this object
        public string ToCSV()
        {
            // Modified from https://stackoverflow.com/questions/4023462/how-do-i-automatically-display-all-properties-of-a-class-and-their-values-in-a-s
            return GetType().GetProperties()
                .Select(info => (info.Name, Value: info.GetValue(this, null) ?? "NULL"))
                .Aggregate(
                    new StringBuilder(),
                    (sb, pair) => sb.Append($"{pair.Value}, "),
                    sb => sb.ToString()).TrimEnd().TrimEnd(',');
        }
    }
}
