using System;
using System.Buffers;
using SuperSocket.ProtoBase;

namespace SuperSocket.MySQL
{
    public class MySQLPacketDecoder : IPackageDecoder<MySQLPacket>
    {
        private readonly IMySQLPacketFactory _packetFactory;

        public MySQLPacketDecoder(IMySQLPacketFactory packetFactory)
        {
            _packetFactory = packetFactory ?? throw new ArgumentNullException(nameof(packetFactory));
        }

        public MySQLPacket Decode(ref ReadOnlySequence<byte> buffer, object context)
        {
            var package = _packetFactory.Create(0);
            var reader = new SequenceReader<byte>(buffer);
            package.Decode(ref reader, context);
            return package;
        }
    }
}