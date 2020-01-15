using System;
using System.Buffers;
using SuperSocket.ProtoBase;

namespace SuperSocket.MySQL.PackagePartReader
{
    abstract class PackagePartReader : IPackagePartReader<QueryResult>
    {
        public static IPackagePartReader<QueryResult> PackageHeadReader { get; private set; }

        public static IPackagePartReader<QueryResult> ErrorCodePartReader { get; private set; }

        public static IPackagePartReader<QueryResult> ErrorMessagePartRealer { get; private set; }

        static PackagePartReader()
        {
            PackageHeadReader = new PackageHeadReader();
            ErrorCodePartReader = new ErrorCodePartReader();
            ErrorMessagePartRealer = new ErrorMessagePartReader();
        }

        internal static IPackagePartReader<QueryResult> NewReader
        {
            get { return PackageHeadReader;  }
        }

        public abstract bool Process(QueryResult package, ref SequenceReader<byte> reader, out IPackagePartReader<QueryResult> nextPartReader, out bool needMoreData);    
        
    }
}
