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
        public int ColumnCount => Columns?.Count ?? 0;

        /// <summary>
        /// Gets or sets the number of rows affected by INSERT, UPDATE, or DELETE operations.
        /// </summary>
        public long AffectedRows { get; set; }

        /// <summary>
        /// Gets or sets the last insert ID for AUTO_INCREMENT columns.
        /// </summary>
        public long LastInsertId { get; set; }

        /// <summary>
        /// Initializes a new instance of the QueryResultPacket class.
        /// </summary>
        public QueryResultPacket()
        {
            Columns = Array.Empty<ColumnDefinitionPacket>();
            Rows = Array.Empty<IReadOnlyList<string>>();
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
        /// Creates a QueryResultPacket from an OK packet (non-SELECT queries).
        /// </summary>
        /// <param name="affectedRows">Number of affected rows</param>
        /// <param name="lastInsertId">Last insert ID</param>
        /// <returns>A QueryResultPacket representing the OK response</returns>
        public static QueryResultPacket FromOK(long affectedRows, long lastInsertId)
        {
            return new QueryResultPacket
            {
                ErrorCode = 0,
                AffectedRows = affectedRows,
                LastInsertId = lastInsertId
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
                Columns = columns ?? Array.Empty<ColumnDefinitionPacket>(),
                Rows = rows ?? Array.Empty<IReadOnlyList<string>>()
            };
        }

        protected internal override MySQLPacket Decode(ref SequenceReader<byte> reader, object context)
        {
            // QueryResultPacket is a composite packet that represents the final result
            // It's not directly decoded from the wire protocol, but rather constructed
            // from multiple lower-level packets (OK, Error, ResultSetHeader, ColumnDefinition, RowData, EOF)

            // The actual decoding logic would be handled by the connection layer
            // which reads multiple packets and constructs this composite result

            // For now, we'll implement a basic structure that could be used
            // if this packet were to be serialized/deserialized

            // Read error code
            if (!reader.TryReadLittleEndian(out short errorCode))
                throw new InvalidOperationException("Failed to read error code");
            ErrorCode = errorCode;

            // If there's an error, read the error message
            if (ErrorCode != 0)
            {
                if (reader.TryReadLengthEncodedString(out string errorMessage))
                {
                    ErrorMessage = errorMessage;
                }

                return this;
            }

            // Read affected rows and last insert ID for non-SELECT queries
            if (reader.TryReadLengthEncodedInteger(out long affectedRows))
            {
                AffectedRows = affectedRows;
            }

            if (reader.TryReadLengthEncodedInteger(out long lastInsertId))
            {
                LastInsertId = lastInsertId;
            }

            // Read column count for SELECT queries
            if (reader.TryReadLengthEncodedInteger(out long columnCount))
            {
                var columns = new List<ColumnDefinitionPacket>();
                var rows = new List<IReadOnlyList<string>>();

                // Read column definitions (simplified)
                for (int i = 0; i < columnCount; i++)
                {
                    if (reader.TryReadLengthEncodedString(out string columnName))
                    {
                        var column = new ColumnDefinitionPacket
                        {
                            Name = columnName
                        };
                        columns.Add(column);
                    }
                }

                // Read row count
                if (reader.TryReadLengthEncodedInteger(out long rowCount))
                {
                    // Read rows (simplified)
                    for (int i = 0; i < rowCount; i++)
                    {
                        var row = new List<string>();
                        for (int j = 0; j < columnCount; j++)
                        {
                            if (reader.TryReadLengthEncodedString(out string value))
                            {
                                row.Add(value);
                            }
                            else
                            {
                                row.Add(null);
                            }
                        }
                        rows.Add(row.AsReadOnly());
                    }
                }

                Columns = columns.AsReadOnly();
                Rows = rows.AsReadOnly();
            }
            
            return this;
        }

        protected internal override int Encode(IBufferWriter<byte> writer)
        {
            var bytesWritten = 0;

            // Write error code
            bytesWritten += writer.WriteUInt16((ushort)ErrorCode);

            // If there's an error, write the error message and return
            if (ErrorCode != 0)
            {
                if (!string.IsNullOrEmpty(ErrorMessage))
                {
                    var errorBytes = System.Text.Encoding.UTF8.GetBytes(ErrorMessage);
                    bytesWritten += writer.WriteLengthEncodedInteger((ulong)errorBytes.Length);
                    writer.Write(errorBytes);
                    bytesWritten += errorBytes.Length;
                }
                return bytesWritten;
            }

            // Write affected rows and last insert ID
            bytesWritten += writer.WriteLengthEncodedInteger((ulong)AffectedRows);
            bytesWritten += writer.WriteLengthEncodedInteger((ulong)LastInsertId);

            // Write column information
            if (Columns != null && Columns.Count > 0)
            {
                // Write column count
                bytesWritten += writer.WriteLengthEncodedInteger((ulong)Columns.Count);

                // Write column names (simplified)
                foreach (var column in Columns)
                {
                    var nameBytes = System.Text.Encoding.UTF8.GetBytes(column.Name ?? "");
                    bytesWritten += writer.WriteLengthEncodedInteger((ulong)nameBytes.Length);
                    writer.Write(nameBytes);
                    bytesWritten += nameBytes.Length;
                }

                // Write row data
                if (Rows != null)
                {
                    // Write row count
                    bytesWritten += writer.WriteLengthEncodedInteger((ulong)Rows.Count);

                    // Write rows
                    foreach (var row in Rows)
                    {
                        for (int i = 0; i < Columns.Count; i++)
                        {
                            var value = i < row.Count ? row[i] : null;
                            if (value == null)
                            {
                                // Write NULL marker
                                bytesWritten += writer.WriteUInt8(0xFB);
                            }
                            else
                            {
                                var valueBytes = System.Text.Encoding.UTF8.GetBytes(value);
                                bytesWritten += writer.WriteLengthEncodedInteger((ulong)valueBytes.Length);
                                writer.Write(valueBytes);
                                bytesWritten += valueBytes.Length;
                            }
                        }
                    }
                }
            }

            return bytesWritten;
        }
    }
}