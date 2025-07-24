using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using SuperSocket.MySQL.Packets;

namespace SuperSocket.MySQL
{
    internal class MySQLPacketFactory : IMySQLPacketFactory
    {
        public static MySQLPacketFactory ClientInstance { get; }

        private static readonly MySQLPacket unknownPacket = new UnknownPacket();

        static MySQLPacketFactory()
        {
            ClientInstance = new MySQLPacketFactory()
                .RegisterPacketType<HandshakePacket>(-1)
                .RegisterPacketType<OKPacket>(0x00)
                .RegisterPacketType<ErrorPacket>(0xFF)
                .RegisterPacketType<EOFPacket>(0xFE);
        }

        private readonly Dictionary<int, Func<MySQLPacket>> _packetCreators = new();

        private MySQLPacketFactory()
        {
            // Private constructor to enforce singleton pattern
        }

        private MySQLPacketFactory RegisterPacketType<TMySQLPacket>(int packageType)
            where TMySQLPacket : MySQLPacket, new()
        {
            _packetCreators[packageType] = () => new TMySQLPacket();
            return this;
        }

        public MySQLPacket Create(int packageType)
        {
            if (!_packetCreators.TryGetValue(packageType, out var creator))
            {
               //throw new InvalidDataException($"No packet registered for package type {packageType}");
               return unknownPacket;
            }

            var packet = creator();

            if (packet is IPacketWithHeaderByte packetWithHeader)
            {
                // If the packet type byte is to be encoded, set it as the first byte of the content
                packetWithHeader.Header = (byte)packageType;
            }

            return packet;
        }
    }
}