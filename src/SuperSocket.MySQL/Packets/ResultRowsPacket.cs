using System;
using System.Buffers;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using SuperSocket.ProtoBase;

namespace SuperSocket.MySQL.Packets
{
    internal class ResultRowsPacket : MySQLPacket
    {
        public List<IReadOnlyList<string>> Rows { get; private set; }

        protected internal override MySQLPacket Decode(ref SequenceReader<byte> reader, object context)
        {
            if (!reader.TryReadLengthEncodedInteger(out long rowCount))
            {
                throw new InvalidOperationException("Failed to read row count");
            }

            Rows = new List<IReadOnlyList<string>>();

            var filterContext = context as MySQLFilterContext;
            var columnCount = filterContext.QueryResultColumnCount;

            for (long i = 0; i < rowCount; i++)
            {
                var cells = new List<string>(columnCount);

                for (int j = 0; j < columnCount; j++)
                {
                    // Read each row as a length-encoded string
                    if (!reader.TryReadLengthEncodedString(out string cellValue))
                        throw new InvalidOperationException("Failed to read cell value");

                    cells.Add(cellValue);
                }

                Rows.Add(cells);
            }

            filterContext.NextPacket = null;
            filterContext.ColumnDefinitionPackets = null;
            filterContext.QueryResultColumnCount = 0;
            filterContext.State = MySQLConnectionState.Authenticated;

            return QueryResultPacket.FromResultSet(
                filterContext.ColumnDefinitionPackets,
                Rows
            );
        }

        protected internal override int Encode(IBufferWriter<byte> writer)
        {
            throw new NotImplementedException();
        }

        internal override bool IsPartialPacket => true;
    }
}