// See https://aka.ms/new-console-template for more information

using SupportSiteETL;


//testAnonNames();
//testDatabaseConnections();

void testTransfer()
{
    UserTransferer ut = new UserTransferer();

    ut.loadUserData();
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
    foreach(var qUser in q2aUsers) {
        Console.WriteLine("\t" + qUser.UserId + "\t" + qUser.Handle);
    }

    Console.WriteLine("\nAll Discourse Users:");
    foreach (var dUser in discourseUsers)
    {
        Console.WriteLine("\t" + dUser.Id + "\t" + dUser.Username);
    }
}