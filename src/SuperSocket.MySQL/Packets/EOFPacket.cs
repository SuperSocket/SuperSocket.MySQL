using System;
using System.Buffers;
using SuperSocket.ProtoBase;

namespace SuperSocket.MySQL.Packets
{
    public class EOFPacket : MySQLPacket, IPacketWithHeaderByte
    {
        public byte Header { get; set; } = 0xFE;
        public ushort WarningCount { get; set; }
        public ushort StatusFlags { get; set; }

        protected internal override void Decode(ref SequenceReader<byte> reader, object context)
        {
            if (reader.TryReadLittleEndian(out ushort warningCount))
            {
                WarningCount = warningCount;
            }
            else
            {
                throw new InvalidOperationException("Failed to read WarningCount from EOFPacket.");
            }

            if (reader.TryReadLittleEndian(out ushort statusFlags))
            {
                StatusFlags = statusFlags;
            }
            else
            {
                throw new InvalidOperationException("Failed to read StatusFlags from EOFPacket.");
            }
        }

        protected internal override int Encode(IBufferWriter<byte> writer)
        {
            throw new NotImplementedException();
        }
    }
}