using System;
using System.Threading.Tasks;
using Xunit;
using SuperSocket.MySQL;

namespace SuperSocket.MySQL.Test
{
    /// <summary>
    /// Tests specifically for authentication error handling scenarios
    /// mentioned in the problem statement
    /// </summary>
    public class AuthenticationErrorHandlingTest
    {
        [Fact]
        public async Task ConnectAsync_WhenServerUnreachable_ShouldContainAuthenticationFailed()
        {
            // Arrange
            var connection = new MySQLConnection("unreachable-host-that-does-not-exist", 3306, "user", "password");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await connection.ConnectAsync()
            );

            Assert.Contains("authentication failed", exception.Message.ToLower());
        }

        [Fact]
        public async Task ConnectAsync_WhenPortClosed_ShouldContainAuthenticationFailed()
        {
            // Arrange - Use a port that's unlikely to be open
            var connection = new MySQLConnection("localhost", 9999, "user", "password");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await connection.ConnectAsync()
            );

            Assert.Contains("authentication failed", exception.Message.ToLower());
        }

        [Fact]
        public async Task ConnectAsync_WhenNetworkError_ShouldContainAuthenticationFailed()
        {
            // Arrange - Use an invalid hostname that should cause DNS resolution failure
            var connection = new MySQLConnection("invalid.invalid.invalid", 3306, "user", "password");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await connection.ConnectAsync()
            );

            Assert.Contains("authentication failed", exception.Message.ToLower());
        }

        [Fact]
        public async Task DisconnectAsync_WhenConnectionNeverEstablished_ShouldNotThrowNullReference()
        {
            // Arrange
            var connection = new MySQLConnection("localhost", 3306, "user", "password");

            // Act & Assert - This should not throw NullReferenceException
            await connection.DisconnectAsync();
            
            // Verify authentication state is reset
            Assert.False(connection.IsAuthenticated);
        }

        [Fact]
        public async Task DisconnectAsync_AfterFailedConnection_ShouldNotThrowNullReference()
        {
            // Arrange
            var connection = new MySQLConnection("unreachable-host", 3306, "user", "password");

            try
            {
                // Try to connect (this will fail)
                await connection.ConnectAsync();
            }
            catch (InvalidOperationException)
            {
                // Expected failure
            }

            // Act & Assert - This should not throw NullReferenceException
            await connection.DisconnectAsync();
            
            // Verify authentication state is reset
            Assert.False(connection.IsAuthenticated);
        }

        [Fact]
        public async Task DisconnectAsync_CalledMultipleTimes_ShouldNotThrow()
        {
            // Arrange
            var connection = new MySQLConnection("localhost", 3306, "user", "password");

            // Act & Assert - Multiple calls should not throw
            await connection.DisconnectAsync();
            await connection.DisconnectAsync();
            await connection.DisconnectAsync();
            
            // Verify authentication state is reset
            Assert.False(connection.IsAuthenticated);
        }
    }
}