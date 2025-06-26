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

            reader.Advance(3); // Skip the first 3 bytes of the header
            reader.TryRead(out var sequenceId); // Read the sequence ID

            var filter = context as MySQLPacketFilter;

            var packetType = -1;

            // Read the first byte to determine packet type
            if (filter.ReceivedHandshake)
            {
                if (!reader.TryRead(out var packetTypeByte))
                    return null;

                packetType = (int)packetTypeByte;
            }

            var package = _packetFactory.Create(packetType);

            package.Decode(ref reader, context);
            package.SequenceId = sequenceId;

            if (!filter.ReceivedHandshake)
                filter.ReceivedHandshake = true;

            return package;
        }
    }
}