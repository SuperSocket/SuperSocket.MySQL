using System;
using System.Buffers;
using System.Text;

namespace SuperSocket.MySQL.Packets
{
    /// <summary>
    /// Represents a MySQL column definition packet
    /// Contains metadata about a column in the result set
    /// </summary>
    public class ColumnDefinitionPacket : MySQLPacket
    {
        public string Catalog { get; set; }
        public string Schema { get; set; }
        public string Table { get; set; }
        public string OrgTable { get; set; }
        public string Name { get; set; }
        public string OrgName { get; set; }
        public ushort NextLength { get; set; }
        public uint CharacterSet { get; set; }
        public uint ColumnLength { get; set; }
        public byte ColumnType { get; set; }
        public ushort Flags { get; set; }
        public byte Decimals { get; set; }

        public ColumnDefinitionPacket()
        {
        }

        protected internal override MySQLPacket Decode(ref SequenceReader<byte> reader, object context)
        {
            // Read catalog (length-encoded string)
            if (!reader.TryReadLengthEncodedString(out string catalog))
                throw new InvalidOperationException("Failed to read catalog");

            Catalog = catalog;

            // Read schema (length-encoded string)
            if (!reader.TryReadLengthEncodedString(out string schema))
                throw new InvalidOperationException("Failed to read schema");
            Schema = schema;

            // Read table (length-encoded string)
            if (!reader.TryReadLengthEncodedString(out string table))
                throw new InvalidOperationException("Failed to read table");
            Table = table;

            // Read org_table (length-encoded string)
            if (!reader.TryReadLengthEncodedString(out string orgTable))
                throw new InvalidOperationException("Failed to read org_table");
            OrgTable = orgTable;

            // Read name (length-encoded string)
            if (!reader.TryReadLengthEncodedString(out string name))
                throw new InvalidOperationException("Failed to read name");
            Name = name;

            // Read org_name (length-encoded string)
            if (!reader.TryReadLengthEncodedString(out string orgName))
                throw new InvalidOperationException("Failed to read org_name");
            OrgName = orgName;

            // Read next_length (length-encoded integer)
            NextLength = (ushort)reader.ReadLengthEncodedInteger();

            // Read character_set (2 bytes)
            if (!reader.TryReadLittleEndian(out short characterSetShort))
                throw new InvalidOperationException("Failed to read character_set");
            CharacterSet = (uint)characterSetShort;

            // Read column_length (4 bytes)
            if (!reader.TryReadLittleEndian(out int columnLengthInt))
                throw new InvalidOperationException("Failed to read column_length");
            ColumnLength = (uint)columnLengthInt;

            // Read column_type (1 byte)
            if (!reader.TryRead(out byte columnType))
                throw new InvalidOperationException("Failed to read column_type");
            ColumnType = columnType;

            // Read flags (2 bytes)
            if (!reader.TryReadLittleEndian(out short flagsShort))
                throw new InvalidOperationException("Failed to read flags");
            Flags = (ushort)flagsShort;

            // Read decimals (1 byte)
            if (!reader.TryRead(out byte decimals))
                throw new InvalidOperationException("Failed to read decimals");
            Decimals = decimals;

            // Skip the two null bytes that follow
            reader.Advance(2);

            var filterContext = context as MySQLFilterContext;
            filterContext.ColumnDefinitionPackets.Add(this);

            if (filterContext.QueryResultColumnCount > filterContext.ColumnDefinitionPackets.Count)
            {
                filterContext.NextPacket = new ColumnDefinitionPacket();
            }
            else
            {
                filterContext.NextPacket = new ResultRowsPacket();
            }

            return this;
        }

        protected internal override int Encode(IBufferWriter<byte> writer)
        {
            var bytesWritten = 0;

            // Write catalog
            var catalogBytes = Encoding.UTF8.GetBytes(Catalog ?? "def");
            bytesWritten += writer.WriteLengthEncodedInteger((ulong)catalogBytes.Length);
            writer.Write(catalogBytes);
            bytesWritten += catalogBytes.Length;

            // Write schema
            var schemaBytes = Encoding.UTF8.GetBytes(Schema ?? "");
            bytesWritten += writer.WriteLengthEncodedInteger((ulong)schemaBytes.Length);
            writer.Write(schemaBytes);
            bytesWritten += schemaBytes.Length;

            // Write table
            var tableBytes = Encoding.UTF8.GetBytes(Table ?? "");
            bytesWritten += writer.WriteLengthEncodedInteger((ulong)tableBytes.Length);
            writer.Write(tableBytes);
            bytesWritten += tableBytes.Length;

            // Write org_table
            var orgTableBytes = Encoding.UTF8.GetBytes(OrgTable ?? "");
            bytesWritten += writer.WriteLengthEncodedInteger((ulong)orgTableBytes.Length);
            writer.Write(orgTableBytes);
            bytesWritten += orgTableBytes.Length;

            // Write name
            var nameBytes = Encoding.UTF8.GetBytes(Name ?? "");
            bytesWritten += writer.WriteLengthEncodedInteger((ulong)nameBytes.Length);
            writer.Write(nameBytes);
            bytesWritten += nameBytes.Length;

            // Write org_name
            var orgNameBytes = Encoding.UTF8.GetBytes(OrgName ?? "");
            bytesWritten += writer.WriteLengthEncodedInteger((ulong)orgNameBytes.Length);
            writer.Write(orgNameBytes);
            bytesWritten += orgNameBytes.Length;

            // Write next_length
            bytesWritten += writer.WriteLengthEncodedInteger(NextLength);

            // Write character_set
            bytesWritten += writer.WriteUInt16((ushort)CharacterSet);

            // Write column_length
            bytesWritten += writer.WriteUInt32(ColumnLength);

            // Write column_type
            bytesWritten += writer.WriteUInt8(ColumnType);

            // Write flags
            bytesWritten += writer.WriteUInt16(Flags);

            // Write decimals
            bytesWritten += writer.WriteUInt8(Decimals);

            // Write two null bytes
            bytesWritten += writer.WriteUInt16(0);

            return bytesWritten;
        }

        internal override bool IsPartialPacket => true;
    }
}
