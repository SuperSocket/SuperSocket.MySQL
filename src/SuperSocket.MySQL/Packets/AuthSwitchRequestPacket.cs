using System;
using System.Buffers;
using System.Text;
using SuperSocket.ProtoBase;

namespace SuperSocket.MySQL.Packets
{
    /// <summary>
    /// Represents an authentication switch request from the MySQL server.
    /// This packet is sent when the server wants the client to use a different
    /// authentication plugin than the one initially specified.
    /// </summary>
    public class AuthSwitchRequestPacket : MySQLPacket, IPacketWithHeaderByte
    {
        public byte Header { get; set; } = 0xFE;
        
        /// <summary>
        /// The name of the authentication plugin to switch to.
        /// </summary>
        public string PluginName { get; set; }
        
        /// <summary>
        /// The authentication data (salt) for the new plugin.
        /// </summary>
        public byte[] AuthData { get; set; }

        protected internal override MySQLPacket Decode(ref SequenceReader<byte> reader, object context)
        {
            // Read plugin name (null-terminated string)
            var startPosition = reader.Consumed;
            
            // Find the null terminator
            if (reader.TryAdvanceTo(0x00, advancePastDelimiter: false))
            {
                var pluginNameLength = reader.Consumed - startPosition;
                reader.Rewind(pluginNameLength);
                
                var pluginNameBytes = new byte[pluginNameLength];
                reader.TryCopyTo(pluginNameBytes.AsSpan());
                reader.Advance(pluginNameLength);
                
                PluginName = Encoding.UTF8.GetString(pluginNameBytes);
                
                // Skip the null terminator
                reader.Advance(1);
            }
            else
            {
                // No null terminator found - read rest as plugin name
                reader.Rewind(reader.Consumed - startPosition);
                var remaining = new byte[reader.Remaining];
                reader.TryCopyTo(remaining.AsSpan());
                reader.Advance(remaining.Length);
                PluginName = Encoding.UTF8.GetString(remaining);
                AuthData = Array.Empty<byte>();
                return this;
            }
            
            // Read remaining bytes as auth data
            if (reader.Remaining > 0)
            {
                AuthData = new byte[reader.Remaining];
                reader.TryCopyTo(AuthData.AsSpan());
                reader.Advance(AuthData.Length);
            }
            else
            {
                AuthData = Array.Empty<byte>();
            }

            return this;
        }

        protected internal override int Encode(IBufferWriter<byte> writer)
        {
            throw new NotImplementedException();
        }
    }
}
