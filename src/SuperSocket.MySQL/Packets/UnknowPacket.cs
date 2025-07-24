using System;
using System.Buffers;

namespace SuperSocket.MySQL.Packets
{
    public class UnknownPacket : MySQLPacket
    {
        protected internal override MySQLPacket Decode(ref SequenceReader<byte> reader, object context)
        {
            var queryResultPacket = new QueryResultPacket();
            return queryResultPacket.Decode(ref reader, context);
        }

        protected internal override int Encode(IBufferWriter<byte> writer)
        {
            throw new NotSupportedException();
        }
    }
}