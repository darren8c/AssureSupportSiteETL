// See https://aka.ms/new-console-template for more information

using SupportSiteETL;


//testAnonNames();
testDatabaseConnections();
//testTransfer();

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
    QTAConnection q2aConnection = new QTAConnection();
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