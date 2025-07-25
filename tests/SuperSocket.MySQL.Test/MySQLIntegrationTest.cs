using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using SuperSocket.MySQL;

namespace SuperSocket.MySQL.Test
{
    /// <summary>
    /// Integration tests for MySQL connection and handshake process.
    /// These tests require a running MySQL server and should be configured via environment variables.
    /// </summary>
    public class MySQLIntegrationTest
    {
        [Fact]
        [Trait("Category", "Integration")]
        public async Task MySQLConnection_CompleteHandshakeFlow_ShouldAuthenticate()
        {
            // Arrange
            var connection = new MySQLConnection(TestConst.Host, TestConst.DefaultPort, TestConst.Username, TestConst.Password);

            try
            {
                // Act
                await connection.ConnectAsync();

                // Assert
                Assert.True(connection.IsAuthenticated, 
                    "Connection should be authenticated after successful handshake");
            }
            finally
            {
                // Cleanup
                await connection.DisconnectAsync();
            }
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task MySQLConnection_InvalidCredentials_ShouldFailHandshake()
        {
            // Arrange
            var connection = new MySQLConnection(TestConst.Host, TestConst.DefaultPort, "nonexistent_user", "wrong_password");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await connection.ConnectAsync()
            );

            Assert.Contains("authentication failed", exception.Message.ToLower());
            Assert.False(connection.IsAuthenticated, 
                "Connection should not be authenticated after failed handshake");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task MySQLConnection_ConcurrentConnections_ShouldWork()
        {
            // Arrange
            const int connectionCount = 5;
            var connections = new MySQLConnection[connectionCount];
            var tasks = new Task[connectionCount];

            try
            {
                // Act - Create multiple concurrent connections
                for (int i = 0; i < connectionCount; i++)
                {
                    connections[i] = new MySQLConnection(TestConst.Host, TestConst.DefaultPort, TestConst.Username, TestConst.Password);
                    tasks[i] = connections[i].ConnectAsync();
                }

                await Task.WhenAll(tasks);

                // Assert
                for (int i = 0; i < connectionCount; i++)
                {
                    Assert.True(connections[i].IsAuthenticated, 
                        $"Connection {i} should be authenticated");
                }
            }
            finally
            {
                // Cleanup
                var disconnectTasks = new Task[connectionCount];
                for (int i = 0; i < connectionCount; i++)
                {
                    if (connections[i] != null)
                    {
                        disconnectTasks[i] = connections[i].DisconnectAsync();
                    }
                }
                await Task.WhenAll(disconnectTasks.Where(t => t != null));
            }
        }

        /*
        [Fact]
        [Trait("Category", "Integration")]
        public async Task MySQLConnection_ReconnectAfterDisconnect_ShouldWork()
        {
            // Arrange
            var connection = new MySQLConnection(_host, _port, _username, _password);

            try
            {
                // Act - First connection
                await connection.ConnectAsync();
                Assert.True(connection.IsAuthenticated, "First connection should be authenticated");

                // Disconnect
                await connection.DisconnectAsync();
                Assert.False(connection.IsAuthenticated, "Should not be authenticated after disconnect");

                // Reconnect
                await connection.ConnectAsync();

                // Assert
                Assert.True(connection.IsAuthenticated, "Reconnection should be authenticated");
            }
            finally
            {
                // Cleanup
                await connection.DisconnectAsync();
            }
        }
        */

        [Fact]
        [Trait("Category", "Integration")]
        public async Task MySQLConnection_HandshakeTimeout_ShouldBeHandled()
        {
            // Skip test if MySQL is not available            // Arrange
            var connection = new MySQLConnection(TestConst.Host, TestConst.DefaultPort, TestConst.Username, TestConst.Password);
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));

            try
            {
                // Act
                await connection.ConnectAsync(cts.Token);

                // Assert
                Assert.True(connection.IsAuthenticated, "Connection should complete within timeout");
            }
            finally
            {
                // Cleanup
                await connection.DisconnectAsync();
            }
        }
    }
}
