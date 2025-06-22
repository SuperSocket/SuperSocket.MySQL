using System;

namespace SuperSocket.MySQL
{
    internal interface IMySQLPacketFactory
    {
        MySQLPacket Create(int packageType);
    }
}