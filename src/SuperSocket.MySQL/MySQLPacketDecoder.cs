using System;
using System.Buffers;
using SuperSocket.ProtoBase;

namespace SuperSocket.MySQL
{
    internal class MySQLPacketDecoder : IPackageDecoder<MySQLPacket>
    {
        public static MySQLPacketDecoder Singleton { get; }
        static MySQLPacketDecoder()
        {
            Singleton = new MySQLPacketDecoder(MySQLPacketFactory.Singleton);
        }

        private readonly IMySQLPacketFactory _packetFactory;

        private MySQLPacketDecoder(IMySQLPacketFactory packetFactory)
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