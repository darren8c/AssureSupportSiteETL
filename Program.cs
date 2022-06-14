// See https://aka.ms/new-console-template for more information

using SupportSiteETL;


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

// Stringifies *all* users in KEY:VALUE format
Func<List<Dictionary<string, string>>, string> usersToString = users =>
{
    string str = "";
    foreach (Dictionary<string, string> user in users)
    {
        str += userToString(user) + "\n";
    }

    return str.TrimEnd();
};


// Stringifies a *single* user in CSV format
Func<Dictionary<string, string>, string> userToCSV = user =>
{
    string str = "";
    foreach (KeyValuePair<string, string> kvp in user)
    {
        str += kvp.Value + ", ";
    }
    return str.TrimEnd().TrimEnd(',');
};

// Stringifies *all* users in CSV format
Func<List<Dictionary<string, string>>, string> usersToCSV = users =>
{
    string str = "";
    foreach (Dictionary<string, string> user in users)
    {
        str += userToCSV(user) + "\n";
    }

    return str;
};


// Fetch all users from the Discourse database
DiscourseConnection discourseConnection = new DiscourseConnection();
var discourseUsers = discourseConnection.GetUsers();
Console.WriteLine("\nAll Discourse Users:");
foreach (var user in discourseUsers)
{
    //Console.WriteLine(userToString(user) + "\n");
    //Console.WriteLine(userToCSV(user) + "\n");
}


// Fetch all users from the Q2A database
QTAConnection q2aConnection = new QTAConnection();
var q2aUsers = q2aConnection.GetUsers();
Console.WriteLine("\nAll Q2A Users:");
foreach (var user in q2aUsers)
{
    //Console.WriteLine(userToString(user) + "\n");
    //Console.WriteLine(userToCSV(user) + "\n");
}


UserConverter converter = new UserConverter(discourseUsers);
var convertedDiscourseUsers = converter.ConvertDiscourseUsers();
Console.WriteLine("\nConverted Discourse Users:");
foreach (var user in convertedDiscourseUsers)
{
    //Console.WriteLine(userToString(user) + "\n");
    //Console.WriteLine(userToCSV(user) + "\n");
}

/*
Console.WriteLine("New User Names:");
AnonNameGen anonGen = new AnonNameGen();
for(int i = 0; i < 101; i++)
{
    Console.WriteLine("\t" + anonGen.getNewUser().name);
}
*/


Console.WriteLine("End Program");