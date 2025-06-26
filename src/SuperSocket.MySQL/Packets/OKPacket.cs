using System.Buffers;

namespace SuperSocket.MySQL.Packets
{
    public class OKPacket : MySQLPacket, IPacketWithHeaderByte
    {
        public byte Header { get; set; }
        public ulong AffectedRows { get; set; }
        public ulong LastInsertId { get; set; }
        public ushort StatusFlags { get; set; }
        public ushort Warnings { get; set; }
        public string Info { get; set; }

        protected internal override void Decode(ref SequenceReader<byte> reader, object context)
        {
            // Read affected rows (length-encoded integer)
            AffectedRows = reader.TryReadLengthEncodedInteger(out long affectedRows) ? (ulong)affectedRows : 0;
            
            // Read last insert ID (length-encoded integer)
            LastInsertId = reader.TryReadLengthEncodedInteger(out long lastInsertId) ? (ulong)lastInsertId : 0;
            
            // Read status flags (2 bytes)
            reader.TryReadLittleEndian(out short statusFlags);
            StatusFlags = (ushort)statusFlags;
            
            // Read warnings (2 bytes)
            reader.TryReadLittleEndian(out short warnings);
            Warnings = (ushort)warnings;
            
            // Read info string if remaining data
            if (reader.Remaining > 0)
            {
                Info = reader.TryReadLengthEncodedString(out string info) ? info : string.Empty;
            }
        }

        protected internal override int Encode(IBufferWriter<byte> writer)
        {
            var bytesWritten = 0;
            
            // Write header
            bytesWritten += writer.WriteUInt8(Header);
            
            // Write affected rows
            bytesWritten += writer.WriteUInt64(AffectedRows);
            
            // Write last insert ID
            bytesWritten += writer.WriteUInt64(LastInsertId);
            
            // Write status flags
            bytesWritten += writer.WriteUInt16(StatusFlags);
            
            // Write warnings
            bytesWritten += writer.WriteUInt16(Warnings);
            
            // Write info string if present
            if (!string.IsNullOrEmpty(Info))
            {
                var infoBytes = System.Text.Encoding.UTF8.GetBytes(Info);
                var span = writer.GetSpan(infoBytes.Length);
                for (int i = 0; i < infoBytes.Length; i++)
                    span[i] = infoBytes[i];
                writer.Advance(infoBytes.Length);
                bytesWritten += infoBytes.Length;
            }
            
            return bytesWritten;
        }
    }
}
