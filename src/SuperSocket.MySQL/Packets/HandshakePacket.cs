using System;
using System.Buffers;
using System.Text;

namespace SuperSocket.MySQL.Packets
{
    public class HandshakePacket : MySQLPacket
    {
        public byte ProtocolVersion { get; set; }
        public string ServerVersion { get; set; }
        public uint ConnectionId { get; set; }
        public byte[] AuthPluginDataPart1 { get; set; } = new byte[8];
        public uint CapabilityFlagsLower { get; set; }
        public byte CharacterSet { get; set; }
        public ushort StatusFlags { get; set; }
        public uint CapabilityFlagsUpper { get; set; }
        public byte AuthPluginDataLength { get; set; }
        public byte[] AuthPluginDataPart2 { get; set; }
        public string AuthPluginName { get; set; }

        public uint CapabilityFlags => CapabilityFlagsLower | ((uint)CapabilityFlagsUpper << 16);

        protected internal override void Decode(ref SequenceReader<byte> reader, object context)
        {
            // Read protocol version (1 byte)
            reader.TryRead(out byte protocolVersion);
            ProtocolVersion = protocolVersion;

            // Read null-terminated server version string
            ServerVersion = reader.TryReadNullTerminatedString(out string serverVersion) ? serverVersion : string.Empty;

            // Read connection ID (4 bytes)
            reader.TryReadLittleEndian(out int connectionId);
            ConnectionId = (uint)connectionId;

            // Read auth plugin data part 1 (8 bytes)
            AuthPluginDataPart1 = new byte[8];
            reader.TryCopyTo(AuthPluginDataPart1);
            reader.Advance(8);

            // Skip filler byte (1 byte)
            reader.Advance(1);

            // Read capability flags lower (2 bytes)
            reader.TryReadLittleEndian(out short capabilityFlagsLower);
            CapabilityFlagsLower = (uint)(ushort)capabilityFlagsLower;

            // Check if more data is available (for MySQL 4.1+)
            if (reader.Remaining > 0)
            {
                // Read character set (1 byte)
                reader.TryRead(out byte characterSet);
                CharacterSet = characterSet;

                // Read status flags (2 bytes)
                reader.TryReadLittleEndian(out short statusFlags);
                StatusFlags = (ushort)statusFlags;

                // Read capability flags upper (2 bytes)
                reader.TryReadLittleEndian(out short capabilityFlagsUpper);
                CapabilityFlagsUpper = (uint)(ushort)capabilityFlagsUpper;

                // Read auth plugin data length (1 byte)
                reader.TryRead(out byte authPluginDataLength);
                AuthPluginDataLength = authPluginDataLength;

                // Skip reserved bytes (10 bytes)
                reader.Advance(10);

                // Read auth plugin data part 2 if present
                if (AuthPluginDataLength > 8)
                {
                    var part2Length = Math.Max(13, AuthPluginDataLength - 8);
                    AuthPluginDataPart2 = new byte[part2Length];
                    reader.TryCopyTo(AuthPluginDataPart2);
                    reader.Advance(part2Length);
                }

                // Read auth plugin name if present
                if ((CapabilityFlags & 0x00080000) != 0) // CLIENT_PLUGIN_AUTH
                {
                    AuthPluginName = reader.TryReadNullTerminatedString(out string authPluginName) ? authPluginName : string.Empty;
                }
            }
        }

        protected internal override int Encode(IBufferWriter<byte> writer)
        {
            var bytesWritten = 0;
            
            // Write protocol version (1 byte)
            var span = writer.GetSpan(1);
            span[0] = ProtocolVersion;
            writer.Advance(1);
            bytesWritten += 1;
            
            // Write null-terminated server version
            var serverVersionBytes = Encoding.UTF8.GetBytes(ServerVersion ?? string.Empty);
            span = writer.GetSpan(serverVersionBytes.Length + 1);
            serverVersionBytes.CopyTo(span);
            span[serverVersionBytes.Length] = 0; // null terminator
            writer.Advance(serverVersionBytes.Length + 1);
            bytesWritten += serverVersionBytes.Length + 1;
            
            // Write connection ID (4 bytes)
            span = writer.GetSpan(4);
            BitConverter.TryWriteBytes(span, ConnectionId);
            writer.Advance(4);
            bytesWritten += 4;
            
            // Write auth plugin data part 1 (8 bytes)
            span = writer.GetSpan(8);
            (AuthPluginDataPart1 ?? new byte[8]).CopyTo(span);
            writer.Advance(8);
            bytesWritten += 8;
            
            // Write filler byte (1 byte)
            span = writer.GetSpan(1);
            span[0] = 0;
            writer.Advance(1);
            bytesWritten += 1;
            
            // Write capability flags lower (2 bytes)
            span = writer.GetSpan(2);
            BitConverter.TryWriteBytes(span, (ushort)CapabilityFlagsLower);
            writer.Advance(2);
            bytesWritten += 2;
            
            // Write character set (1 byte)
            span = writer.GetSpan(1);
            span[0] = CharacterSet;
            writer.Advance(1);
            bytesWritten += 1;
            
            // Write status flags (2 bytes)
            span = writer.GetSpan(2);
            BitConverter.TryWriteBytes(span, StatusFlags);
            writer.Advance(2);
            bytesWritten += 2;
            
            // Write capability flags upper (2 bytes)
            span = writer.GetSpan(2);
            BitConverter.TryWriteBytes(span, (ushort)CapabilityFlagsUpper);
            writer.Advance(2);
            bytesWritten += 2;
            
            // Write auth plugin data length (1 byte)
            span = writer.GetSpan(1);
            span[0] = AuthPluginDataLength;
            writer.Advance(1);
            bytesWritten += 1;
            
            // Write reserved bytes (10 bytes of zeros)
            span = writer.GetSpan(10);
            span.Slice(0, 10).Clear();
            writer.Advance(10);
            bytesWritten += 10;
            
            // Write auth plugin data part 2 if present
            if ((CapabilityFlags & 0x00008000) != 0 && AuthPluginDataPart2 != null) // CLIENT_SECURE_CONNECTION
            {
                span = writer.GetSpan(AuthPluginDataPart2.Length);
                AuthPluginDataPart2.CopyTo(span);
                writer.Advance(AuthPluginDataPart2.Length);
                bytesWritten += AuthPluginDataPart2.Length;
            }
            
            // Write auth plugin name if present
            if ((CapabilityFlags & 0x00080000) != 0 && !string.IsNullOrEmpty(AuthPluginName)) // CLIENT_PLUGIN_AUTH
            {
                var pluginNameBytes = Encoding.UTF8.GetBytes(AuthPluginName);
                span = writer.GetSpan(pluginNameBytes.Length + 1);
                pluginNameBytes.CopyTo(span);
                span[pluginNameBytes.Length] = 0; // null terminator
                writer.Advance(pluginNameBytes.Length + 1);
                bytesWritten += pluginNameBytes.Length + 1;
            }
            
            return bytesWritten;
        }
    }
}
