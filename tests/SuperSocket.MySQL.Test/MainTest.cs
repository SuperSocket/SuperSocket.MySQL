using System;
using System.Threading.Tasks;
using Xunit;
using SuperSocket.MySQL;

namespace SuperSocket.MySQL.Test
{
    public class MainTest
    {
        // Test configuration - these should be set via environment variables or test configuration

        [Fact]
        public async Task ConnectAsync_WithValidCredentials_ShouldAuthenticateSuccessfully()
        {
            // Arrange
            var connection = new MySQLConnection(TestConst.Host, TestConst.DefaultPort, TestConst.Username, TestConst.Password);

            try
            {
                // Act
                await connection.ConnectAsync();

                // Assert
                Assert.True(connection.IsAuthenticated, "Connection should be authenticated after successful handshake");
            }
            finally
            {
                // Cleanup
                await connection.DisconnectAsync();
            }
        }

        [Fact]
        public async Task ConnectAsync_WithInvalidCredentials_ShouldThrowException()
        {
            // Arrange
            var connection = new MySQLConnection(TestConst.Host, TestConst.DefaultPort, "invalid_user", "invalid_password");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await connection.ConnectAsync()
            );

            Assert.Contains("authentication failed", exception.Message.ToLower());
            Assert.False(connection.IsAuthenticated, "Connection should not be authenticated after failed handshake");
        }

        [Fact]
        public async Task ConnectAsync_WithEmptyPassword_ShouldHandleCorrectly()
        {
            // Arrange
            var connection = new MySQLConnection(TestConst.Host, TestConst.DefaultPort, TestConst.Username, "");

            try
            {
                // Act
                await connection.ConnectAsync();

                // Assert
                // This test depends on your MySQL server configuration
                // If empty passwords are allowed, it should succeed; otherwise, it should fail
                // We'll just verify that the connection attempt completes without throwing unexpected exceptions
                Assert.NotNull(connection);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("authentication failed"))
            {
                // Expected if empty passwords are not allowed
                Assert.False(connection.IsAuthenticated);
            }
            finally
            {
                // Cleanup
                await connection.DisconnectAsync();
            }
        }

        [Fact]
        public async Task ConnectAsync_MultipleConnections_ShouldWorkIndependently()
        {
            // Arrange
            var connection1 = new MySQLConnection(TestConst.Host, TestConst.DefaultPort, TestConst.Username, TestConst.Password);
            var connection2 = new MySQLConnection(TestConst.Host, TestConst.DefaultPort, TestConst.Username, TestConst.Password);

            try
            {
                // Act
                await connection1.ConnectAsync();
                await connection2.ConnectAsync();

                // Assert
                Assert.True(connection1.IsAuthenticated, "First connection should be authenticated");
                Assert.True(connection2.IsAuthenticated, "Second connection should be authenticated");
            }
            finally
            {
                // Cleanup
                await connection1.DisconnectAsync();
                await connection2.DisconnectAsync();
            }
        }

        [Fact]
        public async Task DisconnectAsync_AfterSuccessfulConnection_ShouldResetAuthenticationState()
        {
            // Arrange
            var connection = new MySQLConnection(TestConst.Host, TestConst.DefaultPort, TestConst.Username, TestConst.Password);
            await connection.ConnectAsync();
            Assert.True(connection.IsAuthenticated, "Precondition: Connection should be authenticated");

            // Act
            await connection.DisconnectAsync();

            // Assert
            Assert.False(connection.IsAuthenticated, "Connection should not be authenticated after disconnect");
        }

        [Fact]
        public void Constructor_WithNullHost_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new MySQLConnection(null, TestConst.DefaultPort, TestConst.Username, TestConst.Password)
            );
        }

        [Fact]
        public void Constructor_WithNullUsername_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new MySQLConnection(TestConst.Host, TestConst.DefaultPort, null, TestConst.Password)
            );
        }

        [Fact]
        public void Constructor_WithNullPassword_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new MySQLConnection(TestConst.Host, TestConst.DefaultPort, TestConst.Username, null)
            );
        }

        [Fact]
        public async Task ConnectAsync_WithInvalidHost_ShouldThrowException()
        {
            // Arrange
            var connection = new MySQLConnection("invalid-host-that-does-not-exist", TestConst.DefaultPort, TestConst.Username, TestConst.Password);

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(async () => await connection.ConnectAsync());
            Assert.False(connection.IsAuthenticated, "Connection should not be authenticated when host is invalid");
        }

        [Fact]
        public async Task ConnectAsync_WithInvalidPort_ShouldThrowException()
        {
            // Arrange
            var connection = new MySQLConnection(TestConst.Host, 12345, TestConst.Username, TestConst.Password);

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(async () => await connection.ConnectAsync());
            Assert.False(connection.IsAuthenticated, "Connection should not be authenticated when port is invalid");
        }

        [Fact]
        public async Task ExecuteQueryAsync_WithoutAuthentication_ShouldThrowException()
        {
            // Arrange
            var connection = new MySQLConnection(TestConst.Host, TestConst.DefaultPort, TestConst.Username, TestConst.Password);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await connection.ExecuteQueryAsync("SELECT 1")
            );

            Assert.Contains("not authenticated", exception.Message);
        }

        [Fact]
        public async Task ExecuteQueryAsync_WithAuthentication_ShouldNotThrow()
        {
            // Arrange
            var connection = new MySQLConnection(TestConst.Host, TestConst.DefaultPort, TestConst.Username, TestConst.Password);
            await connection.ConnectAsync();

            try
            {
                // Act
                var result = await connection.ExecuteQueryAsync("SELECT 1");

                // Assert
                Assert.NotNull(result);
                // Verify that the result has a proper structure
                Assert.True(result.IsSuccess || result.ErrorCode != 0, 
                    "Result should either be successful or have a proper error code");
                
                if (result.IsSuccess)
                {
                    Assert.Equal(0, result.ErrorCode);
                    Assert.Null(result.ErrorMessage);
                    Assert.True(result.ColumnCount >= 0, "Column count should be non-negative");
                    Assert.True(result.RowCount >= 0, "Row count should be non-negative");
                }
                else
                {
                    Assert.NotEqual(0, result.ErrorCode);
                    Assert.NotNull(result.ErrorMessage);
                }
            }
            finally
            {
                // Cleanup
                await connection.DisconnectAsync();
            }
        }
    }
}
