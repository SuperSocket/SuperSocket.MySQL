using System;
using System.Buffers;
using SuperSocket.ProtoBase;

namespace SuperSocket.MySQL.Packets
{
    /// <summary>
    /// Represents the client's response to an authentication switch request.
    /// Contains the authentication data for the new plugin.
    /// </summary>
    public class AuthSwitchResponsePacket : MySQLPacket
    {
        /// <summary>
        /// The authentication response data for the switched plugin.
        /// </summary>
        public byte[] AuthData { get; set; }

        protected internal override MySQLPacket Decode(ref SequenceReader<byte> reader, object context)
        {
            throw new NotImplementedException();
        }

        protected internal override int Encode(IBufferWriter<byte> writer)
        {
            var bytesWritten = 0;

            if (AuthData != null && AuthData.Length > 0)
            {
                writer.Write(AuthData.AsSpan());
                bytesWritten += AuthData.Length;
            }

            return bytesWritten;
        }
    }
}
