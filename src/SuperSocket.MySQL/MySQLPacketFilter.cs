using System;
using System.Buffers;
using SuperSocket.ProtoBase;

namespace SuperSocket.MySQL
{
    internal class MySQLPacketFilter : FixedHeaderPipelineFilter<MySQLPacket>
    {
        private const int headerSize = 4; // MySQL package header size is 4 bytes

        public MySQLPacketFilter(IPackageDecoder<MySQLPacket> decoder)
            : base(headerSize)
        {
            this.Decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
            this.Context = new MySQLFilterContext();
        }

        protected override int GetBodyLengthFromHeader(ref ReadOnlySequence<byte> buffer)
        {
            var reader = new SequenceReader<byte>(buffer);

            reader.TryRead(out byte byte0);
            reader.TryRead(out byte byte1);
            reader.TryRead(out byte byte2);

            return byte2 * 256 * 256 + byte1 * 256 + byte0;
        }

        public override MySQLPacket Filter(ref SequenceReader<byte> reader)
        {
            var packet = base.Filter(ref reader);

            if (packet == null || packet.IsPartialPacket)
            {
                // If the packet is null or a partial packet, we cannot return it yet
                // We will wait for more data to complete the packet
                return null;
            }

            return packet;
        }
    }
}