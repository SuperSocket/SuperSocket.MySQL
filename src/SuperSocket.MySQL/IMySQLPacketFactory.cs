using System;

namespace SuperSocket.MySQL
{
    public interface IMySQLPacketFactory
    {
        MySQLPacket Create(int packageType);
    }
}