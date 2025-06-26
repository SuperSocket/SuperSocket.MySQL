using System;
using System.Buffers;
using SuperSocket.MySQL.Packets;
using SuperSocket.ProtoBase;

namespace SuperSocket.MySQL
{
    internal class MySQLPacketEncoder : IPackageEncoder<MySQLPacket>
    {
        public int Encode(IBufferWriter<byte> bufferWriter, MySQLPacket package)
        {
            var packetContentWriter = new ArrayBufferWriter<byte>();
            var packetLen = package.Encode(packetContentWriter);

            var headerSpan = bufferWriter.GetSpan(4);
            headerSpan[0] = (byte)(packetLen & 0xFF);
            headerSpan[1] = (byte)((packetLen >> 8) & 0xFF);
            headerSpan[2] = (byte)((packetLen >> 16) & 0xFF);
            headerSpan[3] = (byte)package.SequenceId; // Sequence ID, typically starts at 0 for the first packet  

            bufferWriter.Advance(4);

            packetLen += 4;

            if (package is IPacketWithHeaderByte packetWithHeader)
            {
                // If the packet type byte is to be encoded, write it as the first byte of the content
                var contentSpan = packetContentWriter.GetSpan(1);
                contentSpan[0] = packetWithHeader.Header; // Example: using first character of type name as packet type byte
                packetContentWriter.Advance(1);

                packetLen++;
            }

            bufferWriter.Write(packetContentWriter.WrittenSpan);
            return packetLen;
        }
    }
}