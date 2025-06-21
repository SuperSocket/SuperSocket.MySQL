using System;
using System.Buffers;
using SuperSocket.ProtoBase;

namespace SuperSocket.MySQL
{
    internal class MySQLPacketEncoder : IPackageEncoder<MySQLPacket>
    {
        public int Encode(IBufferWriter<byte> bufferWriter, MySQLPacket package)
        {
            return package.Encode(bufferWriter);
        }
    }
}