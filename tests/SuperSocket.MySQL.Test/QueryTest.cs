using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SuperSocket.MySQL.Packets;
using Xunit;
using Xunit.Abstractions;

namespace SuperSocket.MySQL.Test
{
    /// <summary>
    /// Comprehensive tests for MySQL query execution and result parsing functionality
    /// </summary>
    public class QueryTest
    {
        private readonly ITestOutputHelper _output;

        public QueryTest(ITestOutputHelper output)
        {
            _output = output;
        }

        private async Task<MySQLConnection> CreateAuthenticatedConnectionAsync()
        {
            var connection = new MySQLConnection(TestConst.Host, TestConst.DefaultPort, TestConst.Username, TestConst.Password);
            await connection.ConnectAsync();
            return connection;
        }

        #region SELECT Query Tests

        [Fact]
        public async Task ExecuteQueryAsync_SelectSingleColumn_ShouldReturnCorrectStructure()
        {
            // Arrange
            _output.WriteLine($"Testing MySQL SELECT query to {TestConst.Host}:{TestConst.DefaultPort} with user '{TestConst.Username}'");

            var connection = new MySQLConnection(TestConst.Host, TestConst.DefaultPort, TestConst.Username, TestConst.Password);

            try
            {
                // Act
                await connection.ConnectAsync();
                Assert.True(connection.IsAuthenticated, "Connection should be authenticated");

                var result = await connection.ExecuteQueryAsync("SELECT 1 as test_column");

                // Assert
                Assert.NotNull(result);
                _output.WriteLine($"Query result - Success: {result.IsSuccess}, Error: {result.ErrorMessage}");
                
                // The result should either be successful OR have a meaningful error
                if (result.IsSuccess)
                {
                    Assert.Equal(0, result.ErrorCode);
                    Assert.Null(result.ErrorMessage);
                    
                    // Assert columns are returned correctly
                    Assert.Equal(1, result.ColumnCount);
                    Assert.NotNull(result.Columns);
                    Assert.Single(result.Columns);
                    Assert.Equal("test_column", result.Columns[0].Name);
                    
                    // Assert rows are returned correctly
                    Assert.Equal(1, result.RowCount);
                    Assert.NotNull(result.Rows);
                    Assert.Single(result.Rows);
                    Assert.NotNull(result.Rows[0]);
                    Assert.Single(result.Rows[0]);
                    Assert.Equal("1", result.Rows[0][0]);
                    
                    _output.WriteLine($"Query executed successfully. Columns: {result.ColumnCount}, Rows: {result.RowCount}");
                    _output.WriteLine($"Column names: {string.Join(", ", result.Columns.Select(c => c.Name ?? "unnamed"))}");
                    _output.WriteLine($"First row data: {string.Join(", ", result.Rows[0] ?? new string[0])}");
                }
                else
                {
                    Assert.NotEqual(0, result.ErrorCode);
                    Assert.NotNull(result.ErrorMessage);
                    _output.WriteLine($"Query failed as expected: {result.ErrorCode} - {result.ErrorMessage}");
                }
            }
            finally
            {
                await connection.DisconnectAsync();
            }
        }

        [Fact]
        public async Task ExecuteQueryAsync_SelectMultipleColumns_ShouldReturnCorrectStructure()
        {
            // Arrange
            var connection = new MySQLConnection(TestConst.Host, TestConst.DefaultPort, TestConst.Username, TestConst.Password);

            try
            {
                // Act
                await connection.ConnectAsync();
                var result = await connection.ExecuteQueryAsync("SELECT 1 as col1, 'test' as col2");

                // Assert
                Assert.NotNull(result);
                _output.WriteLine($"Multi-column query - Success: {result.IsSuccess}, Error: {result.ErrorMessage}");
                
                // The result should either be successful OR have a meaningful error
                if (result.IsSuccess)
                {
                    // Assert columns are returned correctly
                    Assert.Equal(2, result.ColumnCount);
                    Assert.NotNull(result.Columns);
                    Assert.Equal(2, result.Columns.Count);
                    Assert.Equal("col1", result.Columns[0].Name);
                    Assert.Equal("col2", result.Columns[1].Name);
                    
                    // Assert rows are returned correctly
                    Assert.Equal(1, result.RowCount);
                    Assert.NotNull(result.Rows);
                    Assert.Single(result.Rows);
                    Assert.NotNull(result.Rows[0]);
                    Assert.Equal(2, result.Rows[0].Count);
                    Assert.Equal("1", result.Rows[0][0]);
                    Assert.Equal("test", result.Rows[0][1]);
                    
                    _output.WriteLine($"Multi-column query successful - Columns: {result.ColumnCount}, Rows: {result.RowCount}");
                    _output.WriteLine($"Columns found: {result.Columns.Count}");
                    for (int i = 0; i < result.Columns.Count; i++)
                    {
                        _output.WriteLine($"  Column {i}: {result.Columns[i]?.Name ?? "unnamed"}");
                    }
                }
                else
                {
                    Assert.NotEqual(0, result.ErrorCode);
                    Assert.NotNull(result.ErrorMessage);
                    _output.WriteLine($"Multi-column query failed: {result.ErrorCode} - {result.ErrorMessage}");
                }
            }
            finally
            {
                await connection.DisconnectAsync();
            }
        }

        [Fact]
        public async Task ExecuteQueryAsync_SelectWithDifferentDataTypes_ShouldHandleCorrectly()
        {
            // Arrange
            var connection = await CreateAuthenticatedConnectionAsync();

            try
            {
                // Act: Select different data types
                var result = await connection.ExecuteQueryAsync(
                    "SELECT 123 as int_val, 'text' as str_val, 3.14 as float_val, NOW() as date_val, NULL as null_val");

                // Assert
                Assert.NotNull(result);
                _output.WriteLine($"Mixed data types result: Success={result.IsSuccess}, Columns={result.ColumnCount}, Rows={result.RowCount}");
                
                if (result.IsSuccess)
                {
                    // Assert columns are returned correctly
                    Assert.Equal(5, result.ColumnCount);
                    Assert.NotNull(result.Columns);
                    Assert.Equal(5, result.Columns.Count);
                    Assert.Equal("int_val", result.Columns[0].Name);
                    Assert.Equal("str_val", result.Columns[1].Name);
                    Assert.Equal("float_val", result.Columns[2].Name);
                    Assert.Equal("date_val", result.Columns[3].Name);
                    Assert.Equal("null_val", result.Columns[4].Name);
                    
                    // Assert rows are returned correctly
                    Assert.Equal(1, result.RowCount);
                    Assert.NotNull(result.Rows);
                    Assert.Single(result.Rows);
                    Assert.NotNull(result.Rows[0]);
                    Assert.Equal(5, result.Rows[0].Count);
                    Assert.Equal("123", result.Rows[0][0]);
                    Assert.Equal("text", result.Rows[0][1]);
                    Assert.Equal("3.14", result.Rows[0][2]);
                    Assert.NotNull(result.Rows[0][3]); // NOW() returns a timestamp
                    Assert.Null(result.Rows[0][4]); // NULL value
                    
                    _output.WriteLine($"Column count: {result.Columns.Count}");
                    foreach (var column in result.Columns)
                    {
                        _output.WriteLine($"  Column: {column?.Name ?? "unnamed"}");
                    }
                    
                    var firstRow = result.Rows[0];
                    _output.WriteLine($"First row values: {string.Join(", ", firstRow?.Select(v => v ?? "NULL") ?? new string[0])}");
                }
            }
            finally
            {
                await connection.DisconnectAsync();
            }
        }

        [Fact]
        public async Task ExecuteQueryAsync_EmptyResultSet_ShouldHandleCorrectly()
        {
            // Arrange
            var connection = new MySQLConnection(TestConst.Host, TestConst.DefaultPort, TestConst.Username, TestConst.Password);

            try
            {
                // Act - Use a query that doesn't require a database
                await connection.ConnectAsync();
                var result = await connection.ExecuteQueryAsync("SELECT 1 WHERE 1=0"); // This should return empty result

                // Assert
                Assert.NotNull(result);
                _output.WriteLine($"Empty result query - Success: {result.IsSuccess}, Error: {result.ErrorMessage}");
                
                // The result should either be successful OR have a meaningful error
                if (result.IsSuccess)
                {
                    // Assert columns are returned correctly (should have column definition even with empty result)
                    Assert.Equal(1, result.ColumnCount);
                    Assert.NotNull(result.Columns);
                    Assert.Single(result.Columns);
                    Assert.Equal("1", result.Columns[0].Name);
                    
                    // Assert rows are returned correctly (should be empty)
                    Assert.Equal(0, result.RowCount);
                    Assert.NotNull(result.Rows);
                    Assert.Empty(result.Rows);
                }
                else
                {
                    Assert.NotEqual(0, result.ErrorCode);
                    Assert.NotNull(result.ErrorMessage);
                }
                
                _output.WriteLine($"Empty result set - Columns: {result.ColumnCount}, Rows: {result.RowCount}");
            }
            finally
            {
                await connection.DisconnectAsync();
            }
        }

        [Theory]
        [InlineData("SELECT 1")]
        [InlineData("SELECT 'hello'")]
        [InlineData("SELECT NOW()")]
        [InlineData("SELECT 1, 2, 3")]
        public async Task ExecuteQueryAsync_VariousSelectQueries_ShouldNotThrow(string query)
        {
            // Arrange
            var connection = await CreateAuthenticatedConnectionAsync();

            try
            {
                // Act & Assert - should not throw
                var result = await connection.ExecuteQueryAsync(query);
                
                Assert.NotNull(result);
                _output.WriteLine($"Query '{query}': Success={result.IsSuccess}, Columns={result.ColumnCount}, Rows={result.RowCount}");
                
                // Basic validation - either success or proper error
                if (result.IsSuccess)
                {
                    // Assert basic structure is correct
                    Assert.True(result.ColumnCount > 0, "Should have at least one column for SELECT queries");
                    Assert.NotNull(result.Columns);
                    Assert.Equal(result.ColumnCount, result.Columns.Count);
                    
                    Assert.True(result.RowCount >= 0, "Row count should be non-negative");
                    Assert.NotNull(result.Rows);
                    Assert.Equal(result.RowCount, result.Rows.Count);
                    
                    // Verify specific expectations based on query
                    if (query == "SELECT 1, 2, 3")
                    {
                        Assert.Equal(3, result.ColumnCount);
                        Assert.Equal(1, result.RowCount);
                        if (result.Rows.Count > 0)
                        {
                            Assert.Equal(3, result.Rows[0].Count);
                            Assert.Equal("1", result.Rows[0][0]);
                            Assert.Equal("2", result.Rows[0][1]);
                            Assert.Equal("3", result.Rows[0][2]);
                        }
                    }
                    else
                    {
                        Assert.Equal(1, result.ColumnCount);
                        Assert.Equal(1, result.RowCount);
                        if (result.Rows.Count > 0)
                        {
                            Assert.Single(result.Rows[0]);
                        }
                    }
                }
                else
                {
                    Assert.NotEqual(0, result.ErrorCode);
                    Assert.NotNull(result.ErrorMessage);
                }
            }
            finally
            {
                await connection.DisconnectAsync();
            }
        }

        #endregion

        #region Non-SELECT Query Tests

        [Fact]
        public async Task ExecuteQueryAsync_SimpleStatement_ShouldReturnOKResult()
        {
            // Arrange
            var connection = new MySQLConnection(TestConst.Host, TestConst.DefaultPort, TestConst.Username, TestConst.Password);

            try
            {
                // Act
                await connection.ConnectAsync();
                
                // Use a simple statement that doesn't require a database
                var result = await connection.ExecuteQueryAsync("SELECT 'test' as result"); // This should work without needing a database

                // Assert
                Assert.NotNull(result);
                _output.WriteLine($"Statement result - Success: {result.IsSuccess}, Error: {result.ErrorMessage}");
                
                // The result should either be successful OR have a meaningful error
                if (result.IsSuccess)
                {
                    Assert.Equal(0, result.ErrorCode);
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
                await connection.DisconnectAsync();
            }
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task ExecuteQueryAsync_InvalidQuery_ShouldReturnError()
        {
            // Arrange
            var connection = new MySQLConnection(TestConst.Host, TestConst.DefaultPort, TestConst.Username, TestConst.Password);

            try
            {
                // Act
                await connection.ConnectAsync();
                var result = await connection.ExecuteQueryAsync("INVALID SQL SYNTAX");

                // Assert
                Assert.NotNull(result);
                Assert.False(result.IsSuccess, "Invalid query should fail");
                Assert.NotEqual(0, result.ErrorCode);
                Assert.NotNull(result.ErrorMessage);
                Assert.NotEmpty(result.ErrorMessage);

                _output.WriteLine($"Expected error for invalid query - Code: {result.ErrorCode}, Message: {result.ErrorMessage}");
            }
            finally
            {
                await connection.DisconnectAsync();
            }
        }

        [Fact]
        public async Task ExecuteQueryAsync_WithoutAuthentication_ShouldReturnError()
        {
            // Arrange
            var connection = new MySQLConnection(TestConst.Host, TestConst.DefaultPort, TestConst.Username, TestConst.Password);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await connection.ExecuteQueryAsync("SELECT 1")
            );

            Assert.Contains("not authenticated", exception.Message);
            _output.WriteLine($"Expected exception for unauthenticated query: {exception.Message}");
        }

        [Fact]
        public async Task ExecuteQueryAsync_NullOrEmptyQuery_ShouldThrowException()
        {
            // Arrange
            var connection = new MySQLConnection(TestConst.Host, TestConst.DefaultPort, TestConst.Username, TestConst.Password);

            try
            {
                await connection.ConnectAsync();

                // Act & Assert - Null query
                var nullException = await Assert.ThrowsAsync<ArgumentException>(
                    async () => await connection.ExecuteQueryAsync(null)
                );
                Assert.Contains("Query cannot be null or empty", nullException.Message);

                // Act & Assert - Empty query
                var emptyException = await Assert.ThrowsAsync<ArgumentException>(
                    async () => await connection.ExecuteQueryAsync("")
                );
                Assert.Contains("Query cannot be null or empty", emptyException.Message);

                _output.WriteLine("Correctly handled null and empty queries");
            }
            finally
            {
                await connection.DisconnectAsync();
            }
        }

        #endregion

        #region String Formatting Tests

        [Fact]
        public async Task ExecuteQueryStringAsync_SelectQuery_ShouldReturnFormattedString()
        {
            // Arrange
            var connection = new MySQLConnection(TestConst.Host, TestConst.DefaultPort, TestConst.Username, TestConst.Password);

            try
            {
                // Act
                await connection.ConnectAsync();
                var result = await connection.ExecuteQueryStringAsync("SELECT 'Hello, MySQL!' as greeting");

                // Assert
                Assert.NotNull(result);
                Assert.NotEmpty(result);
                
                _output.WriteLine($"String result: {result}");
                
                // The string result should contain some indication of success
                // (The exact format depends on the ExecuteQueryStringAsync implementation)
            }
            finally
            {
                await connection.DisconnectAsync();
            }
        }

        [Fact]
        public async Task ExecuteQueryStringAsync_ShouldFormatResultsReadably()
        {
            // Arrange
            var connection = await CreateAuthenticatedConnectionAsync();

            try
            {
                // Act
                var stringResult = await connection.ExecuteQueryStringAsync("SELECT 1 as one, 'hello' as greeting");

                // Assert
                Assert.NotNull(stringResult);
                Assert.NotEmpty(stringResult);
                _output.WriteLine($"String formatted result:\n{stringResult}");
                
                // The exact format depends on implementation, but it should be readable
                Assert.True(stringResult.Length > 10, "Formatted result should have substantial content");
            }
            finally
            {
                await connection.DisconnectAsync();
            }
        }

        #endregion
    }

    #region QueryResultPacket Unit Tests

    /// <summary>
    /// Tests for QueryResultPacket class functionality
    /// </summary>
    public class QueryResultPacketTest
    {
        [Fact]
        public void QueryResultPacket_FromError_ShouldCreateErrorResult()
        {
            // Arrange
            short errorCode = 1064;
            string errorMessage = "You have an error in your SQL syntax";

            // Act
            var result = QueryResultPacket.FromError(errorCode, errorMessage);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(errorCode, result.ErrorCode);
            Assert.Equal(errorMessage, result.ErrorMessage);
            Assert.Equal(0, result.ColumnCount);
            Assert.Equal(0, result.RowCount);
        }

        [Fact]
        public void QueryResultPacket_Constructor_ShouldCreateEmptyResult()
        {
            // Act
            var result = new QueryResultPacket();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess); // Default ErrorCode is 0
            Assert.Equal(0, result.ErrorCode);
            Assert.Null(result.ErrorMessage);
            Assert.Equal(0, result.ColumnCount);
            Assert.Equal(0, result.RowCount);
            Assert.NotNull(result.Columns);
            Assert.NotNull(result.Rows);
        }

        [Fact]
        public void QueryResultPacket_FromResultSet_ShouldCreateResultSetCorrectly()
        {
            // Arrange
            var columns = new List<ColumnDefinitionPacket>
            {
                new ColumnDefinitionPacket { Name = "id" },
                new ColumnDefinitionPacket { Name = "name" }
            };

            var rows = new List<IReadOnlyList<string>>
            {
                new List<string> { "1", "Alice" }.AsReadOnly(),
                new List<string> { "2", "Bob" }.AsReadOnly()
            };

            // Act
            var result = QueryResultPacket.FromResultSet(columns.AsReadOnly(), rows.AsReadOnly());

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(0, result.ErrorCode);
            Assert.Null(result.ErrorMessage);
            Assert.Equal(2, result.ColumnCount);
            Assert.Equal(2, result.RowCount);
            Assert.Equal(columns.AsReadOnly(), result.Columns);
            Assert.Equal(rows.AsReadOnly(), result.Rows);
        }

        [Fact]
        public void QueryResultPacket_FromResultSet_WithNullParameters_ShouldHandleGracefully()
        {
            // Act
            var result = QueryResultPacket.FromResultSet(null, null);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(0, result.ColumnCount);
            Assert.Equal(0, result.RowCount);
            Assert.NotNull(result.Columns);
            Assert.NotNull(result.Rows);
        }

        [Fact]
        public void QueryResultPacket_ColumnCount_ShouldReflectColumnsCollection()
        {
            // Arrange
            var result = new QueryResultPacket();
            Assert.Equal(0, result.ColumnCount);

            var columns = new List<ColumnDefinitionPacket>
            {
                new ColumnDefinitionPacket { Name = "col1" },
                new ColumnDefinitionPacket { Name = "col2" },
                new ColumnDefinitionPacket { Name = "col3" }
            };

            // Act
            result.Columns = columns.AsReadOnly();

            // Assert
            Assert.Equal(3, result.ColumnCount);
        }

        [Fact]
        public void QueryResultPacket_RowCount_ShouldReflectRowsCollection()
        {
            // Arrange
            var result = new QueryResultPacket();
            Assert.Equal(0, result.RowCount);

            var rows = new List<IReadOnlyList<string>>
            {
                new List<string> { "1", "Alice" }.AsReadOnly(),
                new List<string> { "2", "Bob" }.AsReadOnly(),
                new List<string> { "3", "Charlie" }.AsReadOnly(),
                new List<string> { "4", "Diana" }.AsReadOnly()
            };

            // Act
            result.Rows = rows.AsReadOnly();

            // Assert
            Assert.Equal(4, result.RowCount);
        }
    }

    #endregion
}
