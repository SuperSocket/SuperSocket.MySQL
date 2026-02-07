using System;
using System.Net.Sockets;

namespace SuperSocket.MySQL.Test
{
    public static class TestConst
    {
        public const string Host = "localhost";

        public const string Username = "root";

        public const string Password = "root";

        public const int DefaultPort = 3306;

        private static bool? _isMySQLAvailable;

        /// <summary>
        /// Checks if MySQL server is available for integration tests.
        /// The result is cached after the first check.
        /// </summary>
        public static bool IsMySQLAvailable
        {
            get
            {
                if (_isMySQLAvailable.HasValue)
                    return _isMySQLAvailable.Value;

                _isMySQLAvailable = CheckMySQLAvailability();
                return _isMySQLAvailable.Value;
            }
        }

        /// <summary>
        /// Returns the skip reason if MySQL is not available, or null if it is available.
        /// </summary>
        public static string SkipIfMySQLNotAvailable =>
            IsMySQLAvailable ? null : $"MySQL server is not available at {Host}:{DefaultPort}";

        private static bool CheckMySQLAvailability()
        {
            try
            {
                using var client = new TcpClient();
                var result = client.BeginConnect(Host, DefaultPort, null, null);
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2));
                
                if (!success)
                    return false;

                client.EndConnect(result);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}