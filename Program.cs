// See https://aka.ms/new-console-template for more information

using SupportSiteETL.Databases;
using SupportSiteETL.Migration.Transform;
using SupportSiteETL.Migration.Extract;
using SupportSiteETL.Migration.Load;


//testAnonNames();
//testDatabaseConnections();
//testDiscourseDataFetch();
//testQ2AUserFetch();
//testQ2ADeleteUsers();
//testTransferUsers(); 
//testTransferCategories();
//testWordTables();
//testQ2ADeleteData(); //delete all the entered site data
//testTransferAll();

testImageTransferer();

void testTransferAll() //transfer both users and posts
{
    Console.WriteLine("Migrating data...");
    Transformer transferer = new Transformer();
    transferer.Extract();
    transferer.Load();
    Console.WriteLine("Data migrated!");
}

void testWordTables()
{
    WordsTransformer wt = new WordsTransformer();
    wt.Load();
}

void testImageTransferer()
{
    ImageTransformer imt = new ImageTransformer();
}


void testQ2ADeleteData() //deletes users, categories, and all posts
{
    Loader loader = new Loader();
    Deleter deleter = new Deleter();

    Console.WriteLine("Deleting existing site data...");

    deleter.DeletePosts(); //note this deletes word/search table data as well
    Console.WriteLine("Posts deleted!");
    deleter.DeleteCategories();
    Console.WriteLine("Categories deleted!");
    deleter.DeleteUsers();
    Console.WriteLine("(non super-admin) Users deleted!");
    deleter.DeleteImages();
    Console.WriteLine("Migrated images deleted!");

    Console.WriteLine("Existing site data deleted!\n");

    loader.UpdateSiteStats();
}

void testTransferCategories()
{
    CategoryTransformer cTran = new CategoryTransformer();
    cTran.Extract();
    cTran.Load();
}

void testTransferUsers()
{
    UserTransformer ut = new UserTransformer();
    ut.Extract();

    //Testing to see if user data looks right
    //
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

    ut.Load();
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
    Extractor extracter = new Extractor();
    var q2aUsers = extracter.GetQ2AUsers();
    var discourseUsers = extracter.GetDiscourseUsers();

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

    Extractor extractor = new Extractor();
    var q2aUsers = extractor.GetQ2AUsers();

    Console.WriteLine("\nAll Q2A Users:");
    foreach (var qUser in q2aUsers)
    {
        Console.WriteLine(userToString(qUser) + "\n");
    }
}

void testQ2ADeleteUsers()
{
    Extractor extractor = new Extractor();
    Deleter deleter = new Deleter();

    var beforeDelete = extractor.GetQ2AUsers();
    int result = deleter.DeleteUsers();
    var afterDelete = extractor.GetQ2AUsers();

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
