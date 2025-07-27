using System;
using System.Buffers;
using System.Collections.Generic;
using SuperSocket.ProtoBase;

namespace SuperSocket.MySQL.Packets
{
    public class ColumnCountPacket : MySQLPacket
    {
        public ulong ColumnCount { get; set; }

        protected internal override MySQLPacket Decode(ref SequenceReader<byte> reader, object context)
        {
            ColumnCount = reader.ReadLengthEncodedInteger();

            var filterContext = context as MySQLFilterContext;
            filterContext.NextPacket = new ColumnDefinitionPacket();
            filterContext.QueryResultColumnCount = (int)ColumnCount;
            filterContext.ColumnDefinitionPackets = new List<ColumnDefinitionPacket>(filterContext.QueryResultColumnCount);

            return this;
        }

        protected internal override int Encode(IBufferWriter<byte> writer)
        {
            throw new NotImplementedException();
        }

        internal override bool IsPartialPacket => true;
    }
}