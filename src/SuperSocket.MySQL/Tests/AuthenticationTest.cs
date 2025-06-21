using System;
using System.Text;
using SuperSocket.MySQL.Authentication;

namespace SuperSocket.MySQL.Tests
{
    /// <summary>
    /// Simple test to validate MySQL authentication components.
    /// </summary>
    public class AuthenticationTest
    {
        public static void TestHandshakePacket()
        {
            System.Console.WriteLine("Testing MySQL Handshake Packet...");
            
            var handshake = new MySQLHandshakePacket();
            var handshakeBytes = handshake.ToBytes();
            
            System.Console.WriteLine($"Handshake packet size: {handshakeBytes.Length} bytes");
            System.Console.WriteLine($"Connection ID: {handshake.ConnectionId}");
            System.Console.WriteLine($"Server Version: {handshake.ServerVersion}");
            System.Console.WriteLine($"Protocol Version: {handshake.ProtocolVersion}");
            System.Console.WriteLine($"Salt length: {handshake.GetFullSalt().Length}");
            
            System.Console.WriteLine("✓ Handshake packet created successfully");
        }

        public static void TestPasswordScrambling()
        {
            System.Console.WriteLine("\nTesting MySQL Password Scrambling...");
            
            var handler = new MySQLAuthenticationHandler();
            var handshake = handler.CreateHandshake();
            var salt = handshake.GetFullSalt();
            
            // Test with valid credentials (test/test)
            var response = new MySQLHandshakeResponsePacket
            {
                Username = "test",
                AuthResponse = handler.ScramblePasswordForTest("test", salt) // We need to expose this for testing
            };
            
            System.Console.WriteLine($"Username: {response.Username}");
            System.Console.WriteLine($"Auth response length: {response.AuthResponse?.Length ?? 0}");
            
            System.Console.WriteLine("✓ Password scrambling test completed");
        }

        public static void TestOkErrorPackets()
        {
            System.Console.WriteLine("\nTesting OK and Error Packets...");
            
            var handler = new MySQLAuthenticationHandler();
            
            var okPacket = handler.CreateOkPacket();
            System.Console.WriteLine($"OK packet size: {okPacket.Length} bytes");
            
            var errorPacket = handler.CreateErrorPacket(1045, "Access denied");
            System.Console.WriteLine($"Error packet size: {errorPacket.Length} bytes");
            
            System.Console.WriteLine("✓ OK and Error packets created successfully");
        }

        public static void RunAllTests()
        {
            System.Console.WriteLine("=== MySQL Authentication Component Tests ===\n");
            
            try
            {
                TestHandshakePacket();
                TestPasswordScrambling();
                TestOkErrorPackets();
                
                System.Console.WriteLine("\n✅ All tests passed!");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"\n❌ Test failed: {ex.Message}");
                System.Console.WriteLine(ex.StackTrace);
            }
        }
    }
}

// Extension to expose ScramblePassword for testing
namespace SuperSocket.MySQL.Authentication
{
    public partial class MySQLAuthenticationHandler
    {
        public byte[] ScramblePasswordForTest(string password, byte[] salt)
        {
            return ScramblePassword(password, salt);
        }
    }
}