using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SuperSocket.Client;
using SuperSocket.MySQL.Packets;
using SuperSocket.ProtoBase;

namespace SuperSocket.MySQL
{
    public class MySQLConnection : EasyClient<MySQLPacket>
    {
        private const int DefaultPort = 3306;
        private readonly string _host;
        private readonly int _port;
        private readonly string _userName;
        private readonly string _password;

        private static readonly MySQLPacketEncoder PacketEncoder = new MySQLPacketEncoder();

        public bool IsAuthenticated { get; private set; }

        public MySQLConnection(string host, int port, string userName, string password, ILogger logger = null)
            : base(new MySQLPacketFilter(MySQLPacketDecoder.ClientInstance), logger)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _port = port > 0 ? port : DefaultPort;
            _userName = userName ?? throw new ArgumentNullException(nameof(userName));
            _password = password ?? throw new ArgumentNullException(nameof(password));
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_host))
                throw new ArgumentException("Host cannot be null or empty.", nameof(_host));

            if (_port <= 0)
                throw new ArgumentOutOfRangeException(nameof(_port), "Port must be a positive integer.");

            var endPoint = new DnsEndPoint(_host, _port, AddressFamily.InterNetwork);

            var connected = await ConnectAsync(endPoint, cancellationToken).ConfigureAwait(false);

            if (!connected)
                throw new InvalidOperationException($"Failed to connect to MySQL server at {_host}:{_port}");

            // Wait for server's handshake packet
            var packet = await ReceiveAsync().ConfigureAwait(false);
            if (!(packet is HandshakePacket handshakePacket))
                throw new InvalidOperationException("Expected handshake packet from server.");

            // Prepare handshake response
            var handshakeResponse = new HandshakeResponsePacket
            {
                CapabilityFlags = (uint)(ClientCapabilities.CLIENT_PROTOCOL_41 |
                                       ClientCapabilities.CLIENT_SECURE_CONNECTION |
                                       ClientCapabilities.CLIENT_PLUGIN_AUTH),
                MaxPacketSize = 16777216, // 16MB
                CharacterSet = 0x21, // utf8_general_ci
                Username = _userName,
                Database = string.Empty, // Can be set later if needed
                AuthPluginName = "mysql_native_password"
            };

            // Generate authentication response
            handshakeResponse.AuthResponse = GenerateAuthResponse(handshakePacket);
            handshakeResponse.SequenceId = packet.SequenceId + 1;

            // Send handshake response
            await SendAsync(PacketEncoder, handshakeResponse).ConfigureAwait(false);

            // Wait for authentication result (OK packet or Error packet)
            var authResult = await ReceiveAsync().ConfigureAwait(false);

            switch (authResult)
            {
                case OKPacket okPacket:
                    // Authentication successful
                    IsAuthenticated = true;
                    break;
                case ErrorPacket errorPacket:
                    // Authentication failed
                    var errorMsg = !string.IsNullOrEmpty(errorPacket.ErrorMessage)
                        ? errorPacket.ErrorMessage
                        : "Authentication failed";
                    throw new InvalidOperationException($"MySQL authentication failed: {errorMsg} (Error {errorPacket.ErrorCode})");
                case EOFPacket eofPacket:
                    // EOF packet received, check if it indicates success
                    if ((eofPacket.StatusFlags & 0x0002) != 0)
                    {
                        IsAuthenticated = true;
                        break;
                    }
                    else
                    {
                        throw new InvalidOperationException("Authentication failed: EOF packet received without success status. Length: " + eofPacket.Length);
                    }
                default:
                    throw new InvalidOperationException($"Unexpected packet received during authentication: {authResult?.GetType().Name ?? "null"}");
            }
        }

        private byte[] GenerateAuthResponse(HandshakePacket handshakePacket)
        {
            if (string.IsNullOrEmpty(_password))
                return Array.Empty<byte>();

            // MySQL native password authentication algorithm:
            // SHA1(password) XOR SHA1(salt + SHA1(SHA1(password)))
            using (var sha1 = SHA1.Create())
            {
                var passwordBytes = Encoding.UTF8.GetBytes(_password);
                var sha1Password = sha1.ComputeHash(passwordBytes);
                var sha1Sha1Password = sha1.ComputeHash(sha1Password);

                sha1.TransformBlock(handshakePacket.AuthPluginDataPart1, 0, handshakePacket.AuthPluginDataPart1.Length, null, 0);

                if (handshakePacket.AuthPluginDataPart2 != null)
                {
                    var part2Length = Math.Min(handshakePacket.AuthPluginDataPart2.Length, 12);
                    sha1.TransformBlock(handshakePacket.AuthPluginDataPart2, 0, part2Length, null, 0);
                }

                sha1.TransformFinalBlock(sha1Sha1Password, 0, sha1Sha1Password.Length);

                var sha1Combined = sha1.Hash;

                var result = new byte[sha1Password.Length];

                for (int i = 0; i < sha1Password.Length; i++)
                {
                    result[i] = (byte)(sha1Password[i] ^ sha1Combined[i]);
                }

                return result;
            }
        }

        /// <summary>
        /// Executes a SQL query and returns the result
        /// </summary>
        /// <param name="query">The SQL query to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>QueryResult containing the query results</returns>
        public async Task<QueryResultPacket> ExecuteQueryAsync(string query, CancellationToken cancellationToken = default)
        {
            if (!IsAuthenticated)
                throw new InvalidOperationException("Connection is not authenticated. Call ConnectAsync first.");

            if (string.IsNullOrEmpty(query))
                throw new ArgumentException("Query cannot be null or empty.", nameof(query));

            try
            {
                // Create and send COM_QUERY command packet
                var commandPacket = new CommandPacket(MySQLCommand.COM_QUERY, query)
                {
                    SequenceId = 0
                };

                await SendAsync(PacketEncoder, commandPacket).ConfigureAwait(false);

                // Read response
                var response = await ReceiveAsync().ConfigureAwait(false);

                return (QueryResultPacket)response;
            }
            catch (Exception ex)
            {
                return QueryResultPacket.FromError(-1, ex.Message);
            }
        }

        /// <summary>
        /// Executes a SQL query and returns a simple string representation
        /// </summary>
        /// <param name="query">The SQL query to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>String representation of the results</returns>
        public async Task<string> ExecuteQueryStringAsync(string query, CancellationToken cancellationToken = default)
        {
            var result = await ExecuteQueryAsync(query, cancellationToken).ConfigureAwait(false);
            
            if (!result.IsSuccess)
            {
                return $"Error {result.ErrorCode}: {result.ErrorMessage}";
            }

            if (result.Columns == null || result.Columns.Count == 0)
            {
                return $"Query executed successfully. {result.AffectedRows} rows affected.";
            }

            var sb = new StringBuilder();
            
            // Add column headers
            sb.AppendLine(string.Join("\t", result.Columns));
            
            // Add separator line
            sb.AppendLine(new string('-', result.Columns.Count * 10));

            // Add data rows
            if (result.Rows != null)
            {
                foreach (var row in result.Rows)
                {
                    sb.AppendLine(string.Join("\t", row ?? new string[result.Columns.Count]));
                }
            }
            
            sb.AppendLine($"\n{result.Rows?.Count ?? 0} rows returned.");
            
            return sb.ToString();
        }


        /// <summary>
        /// Disconnects from the MySQL server and resets authentication state
        /// </summary>
        public async Task DisconnectAsync()
        {
            try
            {
                if (Connection != null)
                    await CloseAsync();
            }
            finally
            {
                IsAuthenticated = false;
            }
        }
    }
}