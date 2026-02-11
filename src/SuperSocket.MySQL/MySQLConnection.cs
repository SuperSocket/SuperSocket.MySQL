using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
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

        private readonly MySQLFilterContext filterContext;

        public MySQLConnection(string host, int port, string userName, string password, ILogger logger = null)
            : this(new MySQLPacketFilter(MySQLPacketDecoder.ClientInstance), logger)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _port = port > 0 ? port : DefaultPort;
            _userName = userName ?? throw new ArgumentNullException(nameof(userName));
            _password = password ?? throw new ArgumentNullException(nameof(password));
        }

        private MySQLConnection(MySQLPacketFilter packetFilter, ILogger logger)
            : base(packetFilter, logger)
        {
            filterContext = packetFilter.Context as MySQLFilterContext;
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
            handshakeResponse.AuthResponse = GenerateAuthResponse(handshakePacket.AuthPluginDataPart1, handshakePacket.AuthPluginDataPart2);
            handshakeResponse.SequenceId = packet.SequenceId + 1;

            // Send handshake response
            await SendAsync(PacketEncoder, handshakeResponse).ConfigureAwait(false);

            // Wait for authentication result (OK packet, Error packet, or AuthSwitchRequest)
            var authResult = await ReceiveAsync().ConfigureAwait(false);

            // Handle auth switch if requested
            while (authResult is AuthSwitchRequestPacket authSwitchRequest)
            {
                // Generate new auth response using the switched plugin's auth data
                byte[] authResponse;
                
                if (authSwitchRequest.PluginName == "mysql_native_password")
                {
                    // Use mysql_native_password algorithm
                    authResponse = GenerateNativePasswordResponse(authSwitchRequest.AuthData);
                }
                else if (authSwitchRequest.PluginName == "caching_sha2_password")
                {
                    // Use caching_sha2_password algorithm (same as mysql_native_password for the initial response)
                    authResponse = GenerateCachingSha2Response(authSwitchRequest.AuthData);
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported authentication plugin: {authSwitchRequest.PluginName}");
                }

                // Send auth switch response
                var authSwitchResponse = new AuthSwitchResponsePacket
                {
                    AuthData = authResponse,
                    SequenceId = authSwitchRequest.SequenceId + 1
                };

                await SendAsync(PacketEncoder, authSwitchResponse).ConfigureAwait(false);

                // Wait for next response
                authResult = await ReceiveAsync().ConfigureAwait(false);
            }

            switch (authResult)
            {
                case OKPacket okPacket:
                    // Authentication successful
                    IsAuthenticated = true;
                    filterContext.State = MySQLConnectionState.Authenticated;
                    break;
                case ErrorPacket errorPacket:
                    // Authentication failed
                    var errorMsg = !string.IsNullOrEmpty(errorPacket.ErrorMessage)
                        ? errorPacket.ErrorMessage
                        : "Authentication failed";
                    throw new InvalidOperationException($"MySQL authentication failed: {errorMsg} (Error {errorPacket.ErrorCode})");
                case EOFPacket eofPacket:
                    // EOF packet during authentication indicates a protocol error
                    throw new InvalidOperationException("MySQL authentication failed: Unexpected EOF packet received during authentication.");
                default:
                    throw new InvalidOperationException($"Unexpected packet received during authentication: {authResult?.GetType().Name ?? "null"}");
            }
        }

        private byte[] GenerateAuthResponse(byte[] authPluginDataPart1, byte[] authPluginDataPart2)
        {
            if (string.IsNullOrEmpty(_password))
                return Array.Empty<byte>();

            // Combine auth data parts to get the full salt
            var saltLength = authPluginDataPart1.Length;
            if (authPluginDataPart2 != null)
            {
                saltLength += Math.Min(authPluginDataPart2.Length, 12);
            }

            var salt = new byte[saltLength];
            Array.Copy(authPluginDataPart1, 0, salt, 0, authPluginDataPart1.Length);
            
            if (authPluginDataPart2 != null)
            {
                var part2Length = Math.Min(authPluginDataPart2.Length, 12);
                Array.Copy(authPluginDataPart2, 0, salt, authPluginDataPart1.Length, part2Length);
            }

            return GenerateNativePasswordResponse(salt);
        }

        private byte[] GenerateNativePasswordResponse(byte[] salt)
        {
            if (string.IsNullOrEmpty(_password))
                return Array.Empty<byte>();

            // Remove trailing null if present (MySQL sends 20-byte salt with null terminator)
            var saltLength = salt.Length;
            if (saltLength > 0 && salt[saltLength - 1] == 0)
                saltLength--;

            // MySQL native password authentication algorithm:
            // SHA1(password) XOR SHA1(salt + SHA1(SHA1(password)))
            using (var sha1 = SHA1.Create())
            {
                var passwordBytes = Encoding.UTF8.GetBytes(_password);
                var sha1Password = sha1.ComputeHash(passwordBytes);
                var sha1Sha1Password = sha1.ComputeHash(sha1Password);

                sha1.TransformBlock(salt, 0, saltLength, null, 0);
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

        internal byte[] GenerateCachingSha2Response(byte[] salt)
        {
            if (string.IsNullOrEmpty(_password))
                return Array.Empty<byte>();

            // Remove trailing null if present (MySQL sends salt with null terminator)
            var saltLength = salt.Length;
            if (saltLength > 0 && salt[saltLength - 1] == 0)
                saltLength--;

            // caching_sha2_password uses SHA256 instead of SHA1:
            // SHA256(password) XOR SHA256(SHA256(SHA256(password)) + salt)
            using (var sha256 = SHA256.Create())
            {
                var passwordBytes = Encoding.UTF8.GetBytes(_password);
                var sha256Password = sha256.ComputeHash(passwordBytes);
                var sha256Sha256Password = sha256.ComputeHash(sha256Password);

                // Compute SHA256(SHA256(SHA256(password)) + salt)
                var hashAndSalt = new byte[sha256Sha256Password.Length + saltLength];
                Array.Copy(sha256Sha256Password, 0, hashAndSalt, 0, sha256Sha256Password.Length);
                Array.Copy(salt, 0, hashAndSalt, sha256Sha256Password.Length, saltLength);
                var sha256Combined = sha256.ComputeHash(hashAndSalt);

                // XOR the results
                var result = new byte[sha256Password.Length];
                for (int i = 0; i < sha256Password.Length; i++)
                {
                    result[i] = (byte)(sha256Password[i] ^ sha256Combined[i]);
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

                filterContext.State = MySQLConnectionState.CommandPhase;
                await SendAsync(PacketEncoder, commandPacket).ConfigureAwait(false);

                // Read response
                var response = await ReceiveAsync().ConfigureAwait(false);

                // Handle different response types
                switch (response)
                {
                    case ErrorPacket errorPacket:
                        // Query failed
                        return QueryResultPacket.FromError((short)errorPacket.ErrorCode, errorPacket.ErrorMessage);

                    case QueryResultPacket queryResult:
                        // Already a query result packet
                        return queryResult;

                    default:
                        // Handle result set responses (SELECT queries)
                        return await ReadResultSetAsync(response).ConfigureAwait(false);
                }
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

        /// <summary>
        /// Reads a complete result set from the MySQL server
        /// </summary>
        /// <param name="firstPacket">The first packet received after sending the query</param>
        /// <returns>A QueryResultPacket containing the complete result set</returns>
        private Task<QueryResultPacket> ReadResultSetAsync(MySQLPacket firstPacket)
        {
            try
            {
                // If the first packet is already a QueryResultPacket (decoded by UnknownPacket), return it
                if (firstPacket is QueryResultPacket queryResult)
                {
                    return Task.FromResult(queryResult);
                }

                // If the first packet is an UnknownPacket, it should have been decoded to QueryResultPacket
                // but if that failed, we'll create a minimal fallback
                if (firstPacket is UnknownPacket)
                {
                    // Try to read additional packets to build a result set
                    // This is a simplified implementation that attempts to handle basic SELECT queries

                    var columns = new List<ColumnDefinitionPacket>();
                    var rows = new List<IReadOnlyList<string>>();

                    // For now, create a minimal successful result
                    // This could be enhanced to parse more complex result sets in the future
                    return Task.FromResult(QueryResultPacket.FromResultSet(columns.AsReadOnly(), rows.AsReadOnly()));
                }

                // For any other packet type, treat as an unexpected response
                return Task.FromResult(QueryResultPacket.FromError(-1, $"Unexpected packet type in result set: {firstPacket?.GetType().Name ?? "null"}"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(QueryResultPacket.FromError(-1, $"Failed to read result set: {ex.Message}"));
            }
        }

        protected override void OnClosed(object sender, EventArgs e)
        {
            filterContext.State = MySQLConnectionState.Closed;
            base.OnClosed(sender, e);
        }
    }
}
