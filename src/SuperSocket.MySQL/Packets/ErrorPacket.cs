using System.Buffers;
using System.Text;

namespace SuperSocket.MySQL.Packets
{
    public class ErrorPacket : MySQLPacket, IPacketWithHeaderByte
    {
        public byte Header { get; set; }
        public ushort ErrorCode { get; set; }
        public string SqlStateMarker { get; set; } = "#";
        public string SqlState { get; set; }
        public string ErrorMessage { get; set; }

        protected internal override void Decode(ref SequenceReader<byte> reader, object context)
        {
            // Read error code (2 bytes)
            reader.TryReadLittleEndian(out short errorCode);
            ErrorCode = (ushort)errorCode;
            
            // Check for SQL state marker and state (optional, depends on capability flags)
            if (reader.Remaining >= 6 && reader.UnreadSequence.FirstSpan[0] == (byte)'#')
            {
                // Read SQL state marker
                reader.TryRead(out byte marker);
                SqlStateMarker = ((char)marker).ToString();
                
                // Read SQL state (5 characters)
                var sqlStateBytes = new byte[5];
                reader.TryCopyTo(sqlStateBytes);
                reader.Advance(5);
                SqlState = Encoding.UTF8.GetString(sqlStateBytes);
            }
            
            // Read error message (rest of the packet)
            if (reader.Remaining > 0)
            {
                var messageBytes = new byte[reader.Remaining];
                reader.TryCopyTo(messageBytes);
                reader.Advance((int)reader.Remaining);
                ErrorMessage = Encoding.UTF8.GetString(messageBytes);
            }
        }

        protected internal override int Encode(IBufferWriter<byte> writer)
        {
            var bytesWritten = 0;
            
            // Write header
            bytesWritten += writer.WriteUInt8(Header);
            
            // Write error code
            bytesWritten += writer.WriteUInt16(ErrorCode);
            
            // Write SQL state marker and state if present
            if (!string.IsNullOrEmpty(SqlState))
            {
                var markerBytes = Encoding.UTF8.GetBytes(SqlStateMarker ?? "#");
                var span = writer.GetSpan(markerBytes.Length);
                for (int i = 0; i < markerBytes.Length; i++)
                    span[i] = markerBytes[i];
                writer.Advance(markerBytes.Length);
                bytesWritten += markerBytes.Length;
                
                var sqlStateBytes = Encoding.UTF8.GetBytes(SqlState.PadRight(5).Substring(0, 5));
                span = writer.GetSpan(5);
                for (int i = 0; i < 5; i++)
                    span[i] = sqlStateBytes[i];
                writer.Advance(5);
                bytesWritten += 5;
            }
            
            // Write error message
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                var messageBytes = Encoding.UTF8.GetBytes(ErrorMessage);
                var span = writer.GetSpan(messageBytes.Length);
                for (int i = 0; i < messageBytes.Length; i++)
                    span[i] = messageBytes[i];
                writer.Advance(messageBytes.Length);
                bytesWritten += messageBytes.Length;
            }
            
            return bytesWritten;
        }
    }
}
