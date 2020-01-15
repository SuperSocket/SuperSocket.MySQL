using System;
using SuperSocket.ProtoBase;

namespace SuperSocket.MySQL
{
    public class QueryResultFilter : PackagePartsPipelineFilter<QueryResult>
    {
        protected override QueryResult CreatePackage()
        {
            return new QueryResult();
        }

        protected override IPackagePartReader<QueryResult> GetFirstPartReader()
        {
            return PackagePartReader.PackagePartReader.NewReader;
        }
    }
}
