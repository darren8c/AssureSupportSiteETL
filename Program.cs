// See https://aka.ms/new-console-template for more information

using SupportSiteETL;

Console.WriteLine("Begin Program");

QTAConnection q2aConnection = new QTAConnection();
var q2aUsers = q2aConnection.GetUsers();

foreach(var qUser in q2aUsers) {
    Console.WriteLine(qUser);
}

DiscourseConnection discourseConnection = new DiscourseConnection();
var discourseUsers = discourseConnection.GetUsers();

foreach(var dUser in discourseUsers) {
    Console.WriteLine(dUser.Username); 
}

Console.WriteLine("End Program");