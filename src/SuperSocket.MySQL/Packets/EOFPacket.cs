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

        public int Length { get;  private set; }

        protected internal override MySQLPacket Decode(ref SequenceReader<byte> reader, object context)
        {
            Length = (int)reader.Remaining;

            if (!reader.TryReadLittleEndian(out ushort warningCount))
                throw new InvalidOperationException("Failed to read WarningCount from EOFPacket.");

            WarningCount = warningCount;

            if (!reader.TryReadLittleEndian(out ushort statusFlags))
                throw new InvalidOperationException("Failed to read StatusFlags from EOFPacket.");

            StatusFlags = statusFlags;

            return this;
        }

        protected internal override int Encode(IBufferWriter<byte> writer)
        {
            throw new NotImplementedException();
        }
    }
}