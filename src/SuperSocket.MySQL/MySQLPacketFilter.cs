using System;
using System.Buffers;
using SuperSocket.ProtoBase;

namespace SuperSocket.MySQL
{
    public class MySQLPacketFilter : FixedHeaderPipelineFilter<MySQLPacket>
    {
        private const int headerSize = 4; // MySQL package header size is 4 bytes

        public MySQLPacketFilter()
            : base(headerSize)
        {
        }

        protected override int GetBodyLengthFromHeader(ref ReadOnlySequence<byte> buffer)
        {
            var reader = new SequenceReader<byte>(buffer);

            reader.TryRead(out byte byte0);
            reader.TryRead(out byte byte1);
            reader.TryRead(out byte byte2);

            return byte2 * 256 * 256 + byte1 * 256 + byte0;
        }
    }
}