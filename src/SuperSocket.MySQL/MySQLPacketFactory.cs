using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace SuperSocket.MySQL
{
    internal class MySQLPacketFactory : IMySQLPacketFactory
    {
        private readonly Dictionary<int, Func<MySQLPacket>> _packetCreators = new ();

        public MySQLPacketFactory RegisterPacketType<TMySQLPacket>(int packageType)
            where TMySQLPacket : MySQLPacket, new()
        {
            _packetCreators[packageType] = () => new TMySQLPacket();
            return this;
        }

        public MySQLPacket Create(int packageType)
        {
            if (!_packetCreators.TryGetValue(packageType, out var creator))
            {
                throw new InvalidDataException($"No packet registered for package type {packageType}");
            }

            return creator();
        }
    }
}