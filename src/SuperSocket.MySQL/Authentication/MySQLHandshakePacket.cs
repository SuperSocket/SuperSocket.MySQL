using System;
using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace SuperSocket.MySQL.Authentication
{
    /// <summary>
    /// Represents the initial handshake packet sent by the server to the client.
    /// Based on MySQL Protocol specification:
    /// https://dev.mysql.com/doc/dev/mysql-server/8.0.11/page_protocol_connection_phase_packets_protocol_handshake_v10.html
    /// </summary>
    public class MySQLHandshakePacket
    {
        public byte ProtocolVersion { get; set; } = 10;
        public string ServerVersion { get; set; } = "8.0.0-supersocket";
        public uint ConnectionId { get; set; }
        public byte[] AuthPluginDataPart1 { get; set; } = new byte[8];
        public byte Filler { get; set; } = 0x00;
        public ushort CapabilityFlagsLower { get; set; } = 0xF7FF; // Default capabilities
        public byte CharacterSet { get; set; } = 0x21; // utf8_general_ci
        public ushort StatusFlags { get; set; } = 0x0002; // SERVER_STATUS_AUTOCOMMIT
        public ushort CapabilityFlagsUpper { get; set; } = 0x0000;
        public byte AuthPluginDataLength { get; set; } = 21;
        public byte[] Reserved { get; set; } = new byte[10];
        public byte[] AuthPluginDataPart2 { get; set; } = new byte[12];
        public string AuthPluginName { get; set; } = "mysql_native_password";

        public MySQLHandshakePacket()
        {
            // Generate random salt for authentication
            GenerateAuthPluginData();
        }

        private void GenerateAuthPluginData()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(AuthPluginDataPart1);
                rng.GetBytes(AuthPluginDataPart2);
                
                // Ensure no null bytes in the salt
                for (int i = 0; i < AuthPluginDataPart1.Length; i++)
                {
                    if (AuthPluginDataPart1[i] == 0)
                        AuthPluginDataPart1[i] = 1;
                }
                
                for (int i = 0; i < AuthPluginDataPart2.Length; i++)
                {
                    if (AuthPluginDataPart2[i] == 0)
                        AuthPluginDataPart2[i] = 1;
                }
            }
        }

        public byte[] GetFullSalt()
        {
            var salt = new byte[20];
            Array.Copy(AuthPluginDataPart1, 0, salt, 0, 8);
            Array.Copy(AuthPluginDataPart2, 0, salt, 8, 12);
            return salt;
        }

        public byte[] ToBytes()
        {
            var serverVersionBytes = Encoding.UTF8.GetBytes(ServerVersion);
            var authPluginNameBytes = Encoding.UTF8.GetBytes(AuthPluginName);
            
            var packetLength = 1 + serverVersionBytes.Length + 1 + 4 + 8 + 1 + 2 + 1 + 2 + 2 + 1 + 10 + 12 + 1 + authPluginNameBytes.Length + 1;
            var packet = new byte[4 + packetLength]; // 4 bytes for packet header
            
            int offset = 0;
            
            // Packet header
            packet[offset++] = (byte)(packetLength & 0xFF);
            packet[offset++] = (byte)((packetLength >> 8) & 0xFF);
            packet[offset++] = (byte)((packetLength >> 16) & 0xFF);
            packet[offset++] = 0x00; // Packet sequence ID
            
            // Protocol version
            packet[offset++] = ProtocolVersion;
            
            // Server version
            Array.Copy(serverVersionBytes, 0, packet, offset, serverVersionBytes.Length);
            offset += serverVersionBytes.Length;
            packet[offset++] = 0x00; // Null terminator
            
            // Connection ID
            packet[offset++] = (byte)(ConnectionId & 0xFF);
            packet[offset++] = (byte)((ConnectionId >> 8) & 0xFF);
            packet[offset++] = (byte)((ConnectionId >> 16) & 0xFF);
            packet[offset++] = (byte)((ConnectionId >> 24) & 0xFF);
            
            // Auth plugin data part 1
            Array.Copy(AuthPluginDataPart1, 0, packet, offset, 8);
            offset += 8;
            
            // Filler
            packet[offset++] = Filler;
            
            // Capability flags lower
            packet[offset++] = (byte)(CapabilityFlagsLower & 0xFF);
            packet[offset++] = (byte)((CapabilityFlagsLower >> 8) & 0xFF);
            
            // Character set
            packet[offset++] = CharacterSet;
            
            // Status flags
            packet[offset++] = (byte)(StatusFlags & 0xFF);
            packet[offset++] = (byte)((StatusFlags >> 8) & 0xFF);
            
            // Capability flags upper
            packet[offset++] = (byte)(CapabilityFlagsUpper & 0xFF);
            packet[offset++] = (byte)((CapabilityFlagsUpper >> 8) & 0xFF);
            
            // Auth plugin data length
            packet[offset++] = AuthPluginDataLength;
            
            // Reserved
            Array.Copy(Reserved, 0, packet, offset, 10);
            offset += 10;
            
            // Auth plugin data part 2
            Array.Copy(AuthPluginDataPart2, 0, packet, offset, 12);
            offset += 12;
            packet[offset++] = 0x00; // Null terminator for auth plugin data
            
            // Auth plugin name
            Array.Copy(authPluginNameBytes, 0, packet, offset, authPluginNameBytes.Length);
            offset += authPluginNameBytes.Length;
            packet[offset++] = 0x00; // Null terminator
            
            return packet;
        }
    }
}