using System;
using System.Buffers;
using System.Text;

namespace SuperSocket.MySQL.Packets
{
    /// <summary>
    /// Represents a MySQL command packet (COM_QUERY, COM_PING, etc.)
    /// </summary>
    internal class CommandPacket : MySQLPacket
    {
        public MySQLCommand Command { get; set; }

        public string QueryText { get; set; }

        public CommandPacket()
        {
        }

        public CommandPacket(MySQLCommand command, string queryText = null)
        {
            Command = command;
            QueryText = queryText;
        }

        protected internal override MySQLPacket Decode(ref SequenceReader<byte> reader, object context)
        {
            // Read command byte
            if (reader.TryRead(out byte commandByte))
            {
                Command = (MySQLCommand)commandByte;
            }

            // Read query text if present
            if (reader.Remaining > 0)
            {
                var queryBytes = new byte[reader.Remaining];
                reader.TryCopyTo(queryBytes);
                reader.Advance(queryBytes.Length);
                QueryText = Encoding.UTF8.GetString(queryBytes);
            }

            return this;
        }

        protected internal override int Encode(IBufferWriter<byte> writer)
        {
            var bytesWritten = 0;

            // Write command byte
            bytesWritten += writer.WriteUInt8((byte)Command);

            // Write query text if present
            if (!string.IsNullOrEmpty(QueryText))
            {
                var queryBytes = Encoding.UTF8.GetBytes(QueryText);
                writer.Write(queryBytes);
                bytesWritten += queryBytes.Length;
            }

            return bytesWritten;
        }
    }
}
