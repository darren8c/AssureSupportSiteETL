// See https://aka.ms/new-console-template for more information

using SupportSiteETL;

Console.WriteLine("Hello, World!");
QTAConnection q2aConnection = new QTAConnection();
var q2aUsesrs = q2aConnection.GetUsers();

DiscourseConnection discourseConnection = new DiscourseConnection();
var discourseUsers = discourseConnection.GetUsers();
Console.WriteLine("Bye, World!");