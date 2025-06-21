using System;
using System.Buffers;
using System.Text;

namespace SuperSocket.MySQL.Authentication
{
    /// <summary>
    /// Represents the handshake response packet from the client.
    /// Based on MySQL Protocol specification:
    /// https://dev.mysql.com/doc/dev/mysql-server/8.0.11/page_protocol_connection_phase_packets_protocol_handshake_response.html
    /// </summary>
    public class MySQLHandshakeResponsePacket
    {
        public uint CapabilityFlags { get; set; }
        public uint MaxPacketSize { get; set; }
        public byte CharacterSet { get; set; }
        public byte[] Reserved { get; set; } = new byte[23];
        public string Username { get; set; } = string.Empty;
        public byte[] AuthResponse { get; set; } = new byte[0];
        public string Database { get; set; } = string.Empty;
        public string AuthPluginName { get; set; } = "mysql_native_password";

        public static MySQLHandshakeResponsePacket ParseFromBytes(byte[] data, int offset, int length)
        {
            var packet = new MySQLHandshakeResponsePacket();
            int pos = offset + 4; // Skip packet header
            
            // Capability flags (4 bytes)
            packet.CapabilityFlags = BitConverter.ToUInt32(data, pos);
            pos += 4;
            
            // Max packet size (4 bytes)
            packet.MaxPacketSize = BitConverter.ToUInt32(data, pos);
            pos += 4;
            
            // Character set (1 byte)
            packet.CharacterSet = data[pos];
            pos += 1;
            
            // Reserved (23 bytes)
            Array.Copy(data, pos, packet.Reserved, 0, 23);
            pos += 23;
            
            // Username (null-terminated string)
            int usernameStart = pos;
            while (pos < data.Length && data[pos] != 0)
                pos++;
            packet.Username = Encoding.UTF8.GetString(data, usernameStart, pos - usernameStart);
            pos++; // Skip null terminator
            
            // Auth response length + data
            if (pos < data.Length)
            {
                byte authResponseLength = data[pos];
                pos++;
                
                if (authResponseLength > 0 && pos + authResponseLength <= data.Length)
                {
                    packet.AuthResponse = new byte[authResponseLength];
                    Array.Copy(data, pos, packet.AuthResponse, 0, authResponseLength);
                    pos += authResponseLength;
                }
            }
            
            // Database name (null-terminated string) - optional
            if (pos < data.Length)
            {
                int databaseStart = pos;
                while (pos < data.Length && data[pos] != 0)
                    pos++;
                if (pos > databaseStart)
                    packet.Database = Encoding.UTF8.GetString(data, databaseStart, pos - databaseStart);
                pos++; // Skip null terminator
            }
            
            // Auth plugin name (null-terminated string) - optional
            if (pos < data.Length)
            {
                int pluginStart = pos;
                while (pos < data.Length && data[pos] != 0)
                    pos++;
                if (pos > pluginStart)
                    packet.AuthPluginName = Encoding.UTF8.GetString(data, pluginStart, pos - pluginStart);
            }
            
            return packet;
        }

        public static MySQLHandshakeResponsePacket ParseFromSequenceReader(ref SequenceReader<byte> reader)
        {
            var packet = new MySQLHandshakeResponsePacket();
            
            // Skip packet header (already handled by pipeline)
            
            // Capability flags (4 bytes)
            if (reader.Length < 4)
                throw new InvalidOperationException("Cannot read capability flags");
            
            var capabilityFlags = 0u;
            for (int i = 0; i < 4; i++)
            {
                if (!reader.TryRead(out byte b))
                    throw new InvalidOperationException("Cannot read capability flags");
                capabilityFlags |= (uint)(b << (i * 8));
            }
            packet.CapabilityFlags = capabilityFlags;
            
            // Max packet size (4 bytes)
            if (reader.Length < 4)
                throw new InvalidOperationException("Cannot read max packet size");
            
            var maxPacketSize = 0u;
            for (int i = 0; i < 4; i++)
            {
                if (!reader.TryRead(out byte b))
                    throw new InvalidOperationException("Cannot read max packet size");
                maxPacketSize |= (uint)(b << (i * 8));
            }
            packet.MaxPacketSize = maxPacketSize;
            
            // Character set (1 byte)
            if (!reader.TryRead(out byte characterSet))
                throw new InvalidOperationException("Cannot read character set");
            packet.CharacterSet = characterSet;
            
            // Reserved (23 bytes)
            packet.Reserved = new byte[23];
            for (int i = 0; i < 23; i++)
            {
                if (!reader.TryRead(out byte reservedByte))
                    throw new InvalidOperationException("Cannot read reserved bytes");
                packet.Reserved[i] = reservedByte;
            }
            
            // Username (null-terminated string)
            if (!reader.TryReadTo(out ReadOnlySequence<byte> usernameSequence, 0x00, false))
                throw new InvalidOperationException("Cannot read username");
            packet.Username = Encoding.UTF8.GetString(usernameSequence);
            reader.Advance(1); // Skip null terminator
            
            // Auth response length + data
            if (reader.TryRead(out byte authResponseLength) && authResponseLength > 0)
            {
                packet.AuthResponse = new byte[authResponseLength];
                for (int i = 0; i < authResponseLength; i++)
                {
                    if (!reader.TryRead(out byte authByte))
                        throw new InvalidOperationException("Cannot read auth response");
                    packet.AuthResponse[i] = authByte;
                }
            }
            
            // Database name (null-terminated string) - optional
            if (reader.TryReadTo(out ReadOnlySequence<byte> databaseSequence, 0x00, false))
            {
                packet.Database = Encoding.UTF8.GetString(databaseSequence);
                reader.Advance(1); // Skip null terminator
            }
            
            // Auth plugin name (null-terminated string) - optional
            if (reader.TryReadTo(out ReadOnlySequence<byte> pluginSequence, 0x00, false))
            {
                packet.AuthPluginName = Encoding.UTF8.GetString(pluginSequence);
                reader.Advance(1); // Skip null terminator
            }
            
            return packet;
        }
    }
}