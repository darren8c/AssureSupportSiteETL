namespace SupportSiteETL
{
    public class UserConverter
    {

        // List of all Discourse users read into the converter
        List<Dictionary<string, string>> DUsers;

        // List of all Q2A users created
        List<Dictionary<string, string>> QUsers { get; }

        // Basic constructor that takes in a list of Discourse users
        public UserConverter(List<Dictionary<string, string>> dUsers)
        {
            DUsers = dUsers;
            QUsers = new List<Dictionary<string, string>>();
        }

        // Converts Discourse users to Q2A users
        public List<Dictionary<string, string>> ConvertDiscourseUsers()
        {

            foreach (var dUser in DUsers)
            {

                // Create a dictionary to represent the Q2A user
                Dictionary<string, string> newQUser = new Dictionary<string, string>();

                // Populate all fields of the Q2A user
                // This is where thing such as anonymous names will come in
                newQUser.Add("userid", dUser["id"]);
                newQUser.Add("created", dUser["created_at"]);
                newQUser.Add("createip", "");
                newQUser.Add("email", "");
                newQUser.Add("handle", dUser["username"]);
                newQUser.Add("avatarblobid", dUser["uploaded_avatar_id"]);
                newQUser.Add("avatarwidth", "");
                newQUser.Add("avatarheight", "");
                newQUser.Add("passsalt", dUser["salt"]);
                newQUser.Add("passcheck", "");
                newQUser.Add("passhash", dUser["password_hash"]);
                newQUser.Add("level", "");
                newQUser.Add("loggedin", "");
                newQUser.Add("loginip", "");
                newQUser.Add("written", "");
                newQUser.Add("writeip", "");
                newQUser.Add("emailcode", "");
                newQUser.Add("sessioncode", "");
                newQUser.Add("sessionsource", "");
                newQUser.Add("flags", "");
                newQUser.Add("wallposts", "");

                // Add the new user to the list
                QUsers.Add(newQUser);
            }

            return QUsers;
        }
    }
}