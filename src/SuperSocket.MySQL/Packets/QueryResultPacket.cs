using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace SuperSocket.MySQL.Packets
{
    /// <summary>
    /// Represents the result of a MySQL query execution.
    /// https://dev.mysql.com/doc/dev/mysql-server/latest/page_protocol_com_query_response.html
    /// </summary>
    public class QueryResultPacket : MySQLPacket
    {
        /// <summary>
        /// Gets or sets the error code. 0 indicates success.
        /// </summary>
        public short ErrorCode { get; set; }

        /// <summary>
        /// Gets or sets the error message. Null if no error occurred.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the column definitions for the result set.
        /// </summary>
        public IReadOnlyList<ColumnDefinitionPacket> Columns { get; set; }

        /// <summary>
        /// Gets or sets the rows of data returned by the query.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<string>> Rows { get; set; }

        /// <summary>
        /// Gets a value indicating whether the query executed successfully.
        /// </summary>
        public bool IsSuccess => ErrorCode == 0;

        /// <summary>
        /// Gets the number of rows returned by the query.
        /// </summary>
        public int RowCount => Rows?.Count ?? 0;

        /// <summary>
        /// Gets the number of columns in the result set.
        /// </summary>
        public int ColumnCount { get; set; }

        /// <summary>
        /// Initializes a new instance of the QueryResultPacket class.
        /// </summary>
        public QueryResultPacket()
        {
            Columns = new List<ColumnDefinitionPacket>();
            Rows = new List<IReadOnlyList<string>>();
        }

        /// <summary>
        /// Creates a QueryResultPacket from an error.
        /// </summary>
        /// <param name="errorCode">The error code</param>
        /// <param name="errorMessage">The error message</param>
        /// <returns>A QueryResultPacket representing the error</returns>
        public static QueryResultPacket FromError(short errorCode, string errorMessage)
        {
            return new QueryResultPacket
            {
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };
        }

        /// <summary>
        /// Creates a QueryResultPacket from a result set (SELECT queries).
        /// </summary>
        /// <param name="columns">Column definitions</param>
        /// <param name="rows">Row data</param>
        /// <returns>A QueryResultPacket representing the result set</returns>
        public static QueryResultPacket FromResultSet(IReadOnlyList<ColumnDefinitionPacket> columns, IReadOnlyList<IReadOnlyList<string>> rows)
        {
            return new QueryResultPacket
            {
                ErrorCode = 0,
                Columns = columns,
                Rows = rows
            };
        }

        protected internal override MySQLPacket Decode(ref SequenceReader<byte> reader, object context)
        {
            throw new NotSupportedException();
        }

        protected internal override int Encode(IBufferWriter<byte> writer)
        {
            throw new NotSupportedException();
        }
    }
}