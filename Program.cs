// See https://aka.ms/new-console-template for more information

using SupportSiteETL.Databases;
using SupportSiteETL.Migration;


//testAnonNames();
//testDatabaseConnections();
//testDiscourseDataFetch();
//testQ2AUserFetch();
//testQ2ADeleteUsers();
testTransfer();

Console.WriteLine("\nProgram done");

void testTransfer()
{
    UserTransferer ut = new UserTransferer();
    ut.loadUserData();

    //Testing to see if user data looks right

    /*
    for(int i = 0; i < ut.newUsers.Count; i++)
    {
        var u = ut.newUsers[i];

        Console.WriteLine("id:" + u.userId.ToString());
        Console.WriteLine("name:" + u.handle.ToString());
        Console.WriteLine("about:" + u.about.ToString());
        Console.WriteLine("level:" + u.level.ToString());
        Console.WriteLine("email:" + u.email.ToString());
        Console.WriteLine("upvotes:" + u.upvoteds.ToString());

        Console.WriteLine("");
    }
    */

    ut.storeUserData();
}

void testAnonNames()
{
    Console.WriteLine("New User Names:");
    AnonNameGen anonGen = new AnonNameGen();

    for (int i = 0; i < 101; i++)
    {
        Console.WriteLine("\t" + anonGen.getNewUser().name);
    }
}

void testDatabaseConnections()
{
    Q2AConnection q2aConnection = new Q2AConnection();
    var q2aUsers = q2aConnection.GetUsers();

    DiscourseConnection discourseConnection = new DiscourseConnection();
    var discourseUsers = discourseConnection.GetUsers();

    Console.WriteLine("Databases Connected!");

    Console.WriteLine("\nAll Q2A Users:");
    foreach (var qUser in q2aUsers)
    {
        Console.WriteLine("\t" + qUser["userid"] + "\t" + qUser["handle"]);
    }

    Console.WriteLine("\nAll Discourse Users:");
    foreach (var dUser in discourseUsers)
    {
        Console.WriteLine("\t" + dUser["id"] + "\t" + dUser["username"]);
    }
}

void testDiscourseDataFetch()
{
    // Stringifies a *single* user in KEY: VALUE format
    Func<Dictionary<string, string>, string> userToString = user =>
    {
        string str = "";
        foreach (KeyValuePair<string, string> kvp in user)
        {
            str += string.Format("{0}: {1}\n", kvp.Key, kvp.Value);
        }
        return str.TrimEnd();
    };

    // Fetch all users from the Discourse database
    DiscourseConnection discourseConnection = new DiscourseConnection();

    string getUsers = "select * from public.users order by id asc;";
    //string getUsersAndStats = "select * from public.users join public.user_stats on public.users.id=public.user_stats.user_id limit 10;";
    string getUsersAndStats = "select * from public.users join public.user_stats on public.users.id=public.user_stats.user_id order by id;";

    var discourseUsers = discourseConnection.ExecuteQuery(getUsersAndStats);
    Console.WriteLine("\nAll Discourse Users:");
    foreach (var user in discourseUsers)
    {
        Console.WriteLine(userToString(user) + "\n");
    }
}

void testQ2AUserFetch()
{
    // Stringifies a *single* user in KEY: VALUE format
    Func<Dictionary<string, string>, string> userToString = user =>
    {
        string str = "";
        foreach (KeyValuePair<string, string> kvp in user)
        {
            str += string.Format("{0}: {1}\n", kvp.Key, kvp.Value);
        }
        return str.TrimEnd();
    };

    Q2AConnection q2aConnection = new Q2AConnection();
    var q2aUsers = q2aConnection.GetUsers();

    Console.WriteLine("\nAll Q2A Users:");
    foreach (var qUser in q2aUsers)
    {
        Console.WriteLine(userToString(qUser) + "\n");
    }
}

void testQ2ADeleteUsers()
{
    Q2AConnection q2aConnection = new Q2AConnection();
    var beforeDelete = q2aConnection.GetUsers();
    int result = q2aConnection.DeleteUsers();
    var afterDelete = q2aConnection.GetUsers();

    Console.WriteLine("\nAll Q2A Users before deleting:");
    foreach (var qUser in beforeDelete)
    {
        Console.WriteLine("\t" + qUser["userid"] + ": " + qUser["handle"]);
    }

    Console.WriteLine("\nModified {0} rows", result);

    Console.WriteLine("\nAll Q2A Users after deleting:");
    foreach (var qUser in afterDelete)
    {
        Console.WriteLine("\t" + qUser["userid"] + ": " + qUser["handle"]);
    }
}