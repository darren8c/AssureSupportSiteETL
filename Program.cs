// See https://aka.ms/new-console-template for more information

using SupportSiteETL;

Console.WriteLine("Begin Program");



DiscourseConnection discourseConnection = new DiscourseConnection();
var discourseUsers = discourseConnection.GetUsers();

Console.WriteLine("\nAll Discourse Users:");
foreach(var dUser in discourseUsers) {
    //Console.WriteLine(dUser.ToCSV() + "\n");
    Console.WriteLine(dUser);
}


QTAConnection q2aConnection = new QTAConnection();
var q2aUsers = q2aConnection.GetUsers();

Console.WriteLine("\nAll Q2A Users:");
foreach(var qUser in q2aUsers) {
    Console.WriteLine(qUser.ToCSV() + "\n");
    //Console.WriteLine(qUser);
}
Console.WriteLine("End Program");