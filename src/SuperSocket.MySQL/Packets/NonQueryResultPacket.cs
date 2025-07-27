using System;
using System.Buffers;

namespace SuperSocket.MySQL.Packets
{
    public class NonQueryResultPacket : MySQLPacket
    {
        public long AffectedRows { get; private set; }
        public long LastInsertId { get; private set; }

        protected internal override MySQLPacket Decode(ref SequenceReader<byte> reader, object context)
        {
            // Read affected rows and last insert ID for non-SELECT queries
            if (reader.TryReadLengthEncodedInteger(out long affectedRows))
            {
                AffectedRows = affectedRows;
            }

            if (reader.TryReadLengthEncodedInteger(out long lastInsertId))
            {
                LastInsertId = lastInsertId;
            }

            return this;
        }

        protected internal override int Encode(IBufferWriter<byte> writer)
        {
            throw new NotImplementedException();
        }
    }
}