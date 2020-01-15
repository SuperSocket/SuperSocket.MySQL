using System;
using System.Buffers;
using SuperSocket.ProtoBase;

namespace SuperSocket.MySQL.PackagePartReader
{
    /// <summary>
    /// https://dev.mysql.com/doc/dev/mysql-server/8.0.11/page_protocol_com_query_response.html
    /// </summary>
    sealed class PackageHeadReader : PackagePartReader
    {
        public override bool Process(QueryResult package, ref SequenceReader<byte> reader, out IPackagePartReader<QueryResult> nextPartReader, out bool needMoreData)
        {
            reader.TryRead(out byte firstByte);

            if (firstByte == 0xFF)
            {
                needMoreData = false;
                nextPartReader = ErrorCodePartReader;
                return false;
            }
            else if (firstByte == 0x00)
            {
                throw new NotSupportedException();
            }
            else if (firstByte == 0xFB)
            {
                throw new NotSupportedException();
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
