using System;
using System.Security.Cryptography;
using System.Text;

namespace SuperSocket.MySQL.Authentication
{
    /// <summary>
    /// Handles MySQL authentication flow including handshake, challenge, and response validation.
    /// Implements MySQL native password authentication using SHA1-based scrambling.
    /// Based on MySQL Protocol specification:
    /// https://dev.mysql.com/doc/dev/mysql-server/8.0.11/page_protocol_connection_phase_packets_protocol_handshake_v10.html
    /// </summary>
    public partial class MySQLAuthenticationHandler
    {
        private static uint _nextConnectionId = 1;
        private readonly string _validUsername = "test";
        private readonly string _validPassword = "test";

        public MySQLHandshakePacket CreateHandshake()
        {
            var handshake = new MySQLHandshakePacket
            {
                ConnectionId = GetNextConnectionId()
            };
            return handshake;
        }

        public bool ValidateCredentials(MySQLHandshakeResponsePacket response, byte[] salt)
        {
            if (string.IsNullOrEmpty(response.Username))
                return false;

            // Check username
            if (!string.Equals(response.Username, _validUsername, StringComparison.Ordinal))
                return false;

            // Validate password using MySQL native password scrambling
            if (response.AuthResponse == null || response.AuthResponse.Length == 0)
            {
                // Empty password - only valid if expected password is also empty
                return string.IsNullOrEmpty(_validPassword);
            }

            var expectedScramble = ScramblePassword(_validPassword, salt);
            return CompareByteArrays(response.AuthResponse, expectedScramble);
        }

        public byte[] CreateOkPacket()
        {
            // MySQL OK packet format:
            // Header (4 bytes) + OK byte (0x00) + affected_rows + last_insert_id + status_flags + warnings
            var packet = new byte[11];
            
            // Packet length (7 bytes)
            packet[0] = 0x07;
            packet[1] = 0x00;
            packet[2] = 0x00;
            
            // Packet sequence number
            packet[3] = 0x02;
            
            // OK indicator
            packet[4] = 0x00;
            
            // Affected rows (encoded integer)
            packet[5] = 0x00;
            
            // Last insert ID (encoded integer)
            packet[6] = 0x00;
            
            // Status flags
            packet[7] = 0x02; // SERVER_STATUS_AUTOCOMMIT
            packet[8] = 0x00;
            
            // Warnings
            packet[9] = 0x00;
            packet[10] = 0x00;
            
            return packet;
        }

        public byte[] CreateErrorPacket(ushort errorCode, string message)
        {
            var messageBytes = Encoding.UTF8.GetBytes(message ?? "Authentication failed");
            var sqlState = Encoding.UTF8.GetBytes("28000"); // Access denied error
            
            // Calculate packet length: error marker (1) + error code (2) + sql state marker (1) + sql state (5) + message
            var packetLength = 1 + 2 + 1 + 5 + messageBytes.Length;
            var packet = new byte[4 + packetLength];
            
            int offset = 0;
            
            // Packet header
            packet[offset++] = (byte)(packetLength & 0xFF);
            packet[offset++] = (byte)((packetLength >> 8) & 0xFF);
            packet[offset++] = (byte)((packetLength >> 16) & 0xFF);
            packet[offset++] = 0x02; // Packet sequence number
            
            // Error marker
            packet[offset++] = 0xFF;
            
            // Error code
            packet[offset++] = (byte)(errorCode & 0xFF);
            packet[offset++] = (byte)((errorCode >> 8) & 0xFF);
            
            // SQL state marker
            packet[offset++] = 0x23; // '#'
            
            // SQL state
            Array.Copy(sqlState, 0, packet, offset, 5);
            offset += 5;
            
            // Error message
            Array.Copy(messageBytes, 0, packet, offset, messageBytes.Length);
            
            return packet;
        }

        /// <summary>
        /// Implements MySQL native password scrambling algorithm.
        /// SHA1(password) XOR SHA1(salt + SHA1(SHA1(password)))
        /// </summary>
        private byte[] ScramblePassword(string password, byte[] salt)
        {
            if (string.IsNullOrEmpty(password))
                return new byte[0];

            using (var sha1 = SHA1.Create())
            {
                // Stage 1: SHA1(password)
                var passwordBytes = Encoding.UTF8.GetBytes(password);
                var stage1Hash = sha1.ComputeHash(passwordBytes);

                // Stage 2: SHA1(SHA1(password))
                var stage2Hash = sha1.ComputeHash(stage1Hash);

                // Stage 3: SHA1(salt + SHA1(SHA1(password)))
                var saltAndStage2 = new byte[salt.Length + stage2Hash.Length];
                Array.Copy(salt, 0, saltAndStage2, 0, salt.Length);
                Array.Copy(stage2Hash, 0, saltAndStage2, salt.Length, stage2Hash.Length);
                var stage3Hash = sha1.ComputeHash(saltAndStage2);

                // Final: SHA1(password) XOR SHA1(salt + SHA1(SHA1(password)))
                var scramble = new byte[stage1Hash.Length];
                for (int i = 0; i < stage1Hash.Length; i++)
                {
                    scramble[i] = (byte)(stage1Hash[i] ^ stage3Hash[i]);
                }

                return scramble;
            }
        }

        private bool CompareByteArrays(byte[] array1, byte[] array2)
        {
            if (array1.Length != array2.Length)
                return false;

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                    return false;
            }

            return true;
        }

        private static uint GetNextConnectionId()
        {
            return _nextConnectionId++;
        }
    }
}