using SuperSocket.MySQL.Tests;
using SuperSocket.MySQL.Integration;

class Program
{
    static void Main(string[] args)
    {
        System.Console.WriteLine("SuperSocket.MySQL Authentication Implementation\n");
        
        // Run basic authentication tests
        AuthenticationTest.RunAllTests();
        
        System.Console.WriteLine("\n" + new string('=', 50) + "\n");
        
        // Show integration example
        MySQLServerExample.ShowIntegrationExample();
        
        System.Console.WriteLine("\nPress any key to exit...");
        System.Console.ReadKey();
    }
}