using SuperSocket.MySQL.Tests;

class Program
{
    static void Main(string[] args)
    {
        AuthenticationTest.RunAllTests();
        
        System.Console.WriteLine("\nPress any key to exit...");
        System.Console.ReadKey();
    }
}