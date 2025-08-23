using System;
using System.Buffers;
using System.IO;
using Microsoft.Extensions.Logging;
using SuperSocket.ProtoBase;

namespace SuperSocket.MySQL
{
    internal class MySQLPacketDecoder : IPackageDecoder<MySQLPacket>
    {
        private readonly ILogger _logger;
        private readonly IMySQLPacketFactory _packetFactory;

        public MySQLPacketDecoder(IMySQLPacketFactory packetFactory, ILogger logger)
        {
            _packetFactory = packetFactory ?? throw new ArgumentNullException(nameof(packetFactory));
            this._logger = logger;
        }

        public MySQLPacket Decode(ref ReadOnlySequence<byte> buffer, object context)
        {
            if (buffer.Length == 0)
                return null;

            var reader = new SequenceReader<byte>(buffer);

            reader.Advance(3); // Skip the first 3 bytes of the header
            reader.TryRead(out var sequenceId); // Read the sequence ID

            var filterContext = context as MySQLFilterContext;

            var packetType = -1;

            // Read the first byte to determine packet type
            if (filterContext.State != MySQLConnectionState.Initial)
            {
                // In handshake state, we expect the first byte to be the packet type
                if (!reader.TryRead(out var packetTypeByte))
                    return null;

                packetType = (int)packetTypeByte;
            }

            _logger?.LogDebug("Decoding MySQL packet with sequence ID {SequenceId} and type {PacketType}", sequenceId, packetType);

            var package = _packetFactory.Create(packetType);

            package = package.Decode(ref reader, context);
            package.SequenceId = sequenceId;

            if (filterContext.State == MySQLConnectionState.Initial)
            {
                filterContext.State = MySQLConnectionState.HandshakeInitiated;
            }

            return package;
        }
    }
}