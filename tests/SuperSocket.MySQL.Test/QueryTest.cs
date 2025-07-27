using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace SuperSocket.MySQL.Test
{
    public class QueryTest
    {
        private readonly ITestOutputHelper _output;

        public QueryTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task TestQueryFunctionality()
        {
            _output.WriteLine($"Testing MySQL connection to {TestConst.Host}:{TestConst.DefaultPort} with user '{TestConst.Username}'");

            var connection = new MySQLConnection(TestConst.Host, TestConst.DefaultPort, TestConst.Username, TestConst.Password);

            // Test connection and authentication
            await connection.ConnectAsync();
            Assert.True(connection.IsAuthenticated, "Connection should be authenticated");

            _output.WriteLine("Successfully connected and authenticated");

            // Test simple query execution
            var result = await connection.ExecuteQueryAsync("SELECT 1 as test_column");
            Assert.NotNull(result);
            _output.WriteLine($"Query result - Success: {result.IsSuccess}, Error: {result.ErrorMessage}");

            if (!result.IsSuccess)
            {
                _output.WriteLine($"Query failed with error code {result.ErrorCode}: {result.ErrorMessage}");
            }
            else
            {
                _output.WriteLine($"Query executed successfully");
                if (result.Columns != null)
                {
                    _output.WriteLine($"Columns: {string.Join(", ", result.Columns)}");
                }
                if (result.Rows != null)
                {
                    _output.WriteLine($"Row count: {result.Rows.Count}");
                }
            }

            // Test string representation
            var stringResult = await connection.ExecuteQueryStringAsync("SELECT 'Hello, MySQL!' as greeting");
            _output.WriteLine($"String result: {stringResult}");

            // Test INSERT/UPDATE query (should work)
            var insertResult = await connection.ExecuteQueryAsync("CREATE TEMPORARY TABLE test_table (id INT, name VARCHAR(50))");
            Assert.NotNull(insertResult);
            _output.WriteLine($"CREATE TABLE result - Success: {insertResult.IsSuccess}");

            if (insertResult.IsSuccess)
            {
                var insertDataResult = await connection.ExecuteQueryAsync("INSERT INTO test_table (id, name) VALUES (1, 'Test')");
                Assert.NotNull(insertDataResult);
                //_output.WriteLine($"INSERT result - Success: {insertDataResult.IsSuccess}, Affected rows: {insertDataResult.AffectedRows}");
            }

            await connection.DisconnectAsync();
            _output.WriteLine("Disconnected successfully");            
        }
    }
}
