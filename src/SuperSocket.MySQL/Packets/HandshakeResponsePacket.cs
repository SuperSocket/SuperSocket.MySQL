using System;
using System.Buffers;
using System.Text;

namespace SuperSocket.MySQL.Packets
{
    public class HandshakeResponsePacket : MySQLPacket
    {
        public uint CapabilityFlags { get; set; }
        public uint MaxPacketSize { get; set; }
        public byte CharacterSet { get; set; }
        public string Username { get; set; }
        public byte[] AuthResponse { get; set; }
        public string Database { get; set; }
        public string AuthPluginName { get; set; }
        protected internal override void Decode(ref SequenceReader<byte> reader, object context)
        {
            // Read capability flags (4 bytes)
            reader.TryReadLittleEndian(out int capabilityFlags);
            CapabilityFlags = (uint)capabilityFlags;
            
            // Read max packet size (4 bytes)
            reader.TryReadLittleEndian(out int maxPacketSize);
            MaxPacketSize = (uint)maxPacketSize;
            
            // Read character set (1 byte)
            reader.TryRead(out byte characterSet);
            CharacterSet = characterSet;
            
            // Skip reserved bytes (23 bytes)
            reader.Advance(23);
            
            // Read null-terminated username
            Username = reader.TryReadNullTerminatedString(out string username) ? username : string.Empty;
            
            // Read auth response length and data
            if ((CapabilityFlags & 0x00200000) != 0) // CLIENT_PLUGIN_AUTH_LENENC_CLIENT_DATA
            {
                var authResponseLength = reader.TryReadLengthEncodedInteger(out long lenEncAuthResponseLength) ? lenEncAuthResponseLength : 0;
                AuthResponse = new byte[authResponseLength];
                reader.TryCopyTo(AuthResponse);
                reader.Advance((int)authResponseLength);
            }
            else if ((CapabilityFlags & 0x00008000) != 0) // CLIENT_SECURE_CONNECTION
            {
                reader.TryRead(out byte authResponseLength);
                AuthResponse = new byte[authResponseLength];
                reader.TryCopyTo(AuthResponse);
                reader.Advance(authResponseLength);
            }
            else
            {
                AuthResponse = reader.TryReadNullTerminatedString(out string authResponseString) ? Encoding.UTF8.GetBytes(authResponseString) : Array.Empty<byte>();
            }
            
            // Read database name if present
            if ((CapabilityFlags & 0x00000008) != 0) // CLIENT_CONNECT_WITH_DB
            {
                Database = reader.TryReadNullTerminatedString(out string database) ? database : string.Empty;
            }
            
            // Read auth plugin name if present
            if ((CapabilityFlags & 0x00080000) != 0) // CLIENT_PLUGIN_AUTH
            {
                AuthPluginName = reader.TryReadNullTerminatedString(out string authPluginName) ? authPluginName : string.Empty;
            }
        }

        protected internal override int Encode(IBufferWriter<byte> writer)
        {
            var contentWriter = new ArrayBufferWriter<byte>();

            var bytesWritten = 0;
            var span = contentWriter.GetSpan(4);
            
            // Write capability flags (4 bytes)
            BitConverter.TryWriteBytes(span, CapabilityFlags);
            contentWriter.Advance(4);
            bytesWritten += 4;

            // Write max packet size (4 bytes)
            span = contentWriter.GetSpan(4);
            BitConverter.TryWriteBytes(span, MaxPacketSize);
            contentWriter.Advance(4);
            bytesWritten += 4;

            // Write character set (1 byte)
            span = contentWriter.GetSpan(1);
            span[0] = CharacterSet;
            contentWriter.Advance(1);
            bytesWritten += 1;
            
            // Write reserved bytes (23 bytes of zeros)
            span = contentWriter.GetSpan(23);
            span.Slice(0, 23).Clear();
            contentWriter.Advance(23);
            bytesWritten += 23;

            bytesWritten += contentWriter.WriteNullTerminatedString(Username);
            
            // Write auth response
            if (AuthResponse != null)
            {
                if ((CapabilityFlags & (uint)ClientCapabilities.CLIENT_PLUGIN_AUTH_LENENC_CLIENT_DATA) != 0) // CLIENT_PLUGIN_AUTH_LENENC_CLIENT_DATA
                {
                    bytesWritten += contentWriter.WriteLengthEncodedInteger((ulong)AuthResponse.Length);
                    contentWriter.Write(AuthResponse);
                    bytesWritten += AuthResponse.Length;
                }
                else if ((CapabilityFlags & (uint)ClientCapabilities.CLIENT_SECURE_CONNECTION) != 0) // CLIENT_SECURE_CONNECTION
                {
                    span = contentWriter.GetSpan(1);
                    span[0] = (byte)AuthResponse.Length;
                    contentWriter.Advance(1);
                    bytesWritten += 1;

                    contentWriter.Write(AuthResponse);
                    bytesWritten += AuthResponse.Length;
                }
                else
                {
                    contentWriter.Write(AuthResponse);
                    bytesWritten += AuthResponse.Length;

                    span = contentWriter.GetSpan(1);
                    span[0] = 0; // null terminator
                    contentWriter.Advance(1);
                    bytesWritten += 1;
                }
            }
            
            // Write database name if present
            if ((CapabilityFlags & 0x00000008) != 0 && !string.IsNullOrEmpty(Database))
            {
                bytesWritten += contentWriter.WriteNullTerminatedString(Database);
            }
            
            // Write auth plugin name if present
            if ((CapabilityFlags & 0x00080000) != 0 && !string.IsNullOrEmpty(AuthPluginName))
            {
                bytesWritten += contentWriter.WriteNullTerminatedString(AuthPluginName);
            }

            var headerSpan = writer.GetSpan(4);
            headerSpan[0] = (byte)(bytesWritten & 0xFF);
            headerSpan[1] = (byte)((bytesWritten >> 8) & 0xFF);
            headerSpan[2] = (byte)((bytesWritten >> 16) & 0xFF);
            headerSpan[3] = (byte)SequenceId; // Sequence ID, typically starts at 0 for the first packet  
            //headerSpan[4] = 0x00; // Set header byte
            writer.Advance(4);

            writer.Write(contentWriter.WrittenSpan);

            return bytesWritten + 4;
        }
    }
}