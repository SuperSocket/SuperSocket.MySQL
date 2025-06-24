using System;
using System.Buffers;
using System.IO;
using SuperSocket.ProtoBase;

namespace SuperSocket.MySQL
{
    internal class MySQLPacketDecoder : IPackageDecoder<MySQLPacket>
    {
        public static MySQLPacketDecoder ClientInstance { get; }
        static MySQLPacketDecoder()
        {
            ClientInstance = new MySQLPacketDecoder(MySQLPacketFactory.ClientInstance);
        }

        private readonly IMySQLPacketFactory _packetFactory;

        private MySQLPacketDecoder(IMySQLPacketFactory packetFactory)
        {
            _packetFactory = packetFactory ?? throw new ArgumentNullException(nameof(packetFactory));
        }

        public MySQLPacket Decode(ref ReadOnlySequence<byte> buffer, object context)
        {
            if (buffer.Length == 0)
                return null;

            var reader = new SequenceReader<byte>(buffer);
            
            // Read the first byte to determine packet type
            if (!reader.TryRead(out byte packetType))
                return null;

            var filter = context as MySQLPacketFilter;

            // Reset reader to beginning
            reader = new SequenceReader<byte>(buffer);

            var package = _packetFactory.Create(
                    (filter.ReceivedHandshake == true
                        ? packetType
                        : -1)); // Use -1 for HandshakePacket if not received yet

            package.Decode(ref reader, context);

            filter.ReceivedHandshake = true;
            return package;
        }
    }
}