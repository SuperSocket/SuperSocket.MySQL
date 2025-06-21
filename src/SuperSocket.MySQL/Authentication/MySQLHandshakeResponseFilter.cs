using System;
using System.Buffers;
using SuperSocket.ProtoBase;

namespace SuperSocket.MySQL.Authentication
{
    /// <summary>
    /// Filter for parsing MySQL handshake response packets during authentication.
    /// </summary>
    public class MySQLHandshakeResponseFilter : PackagePartsPipelineFilter<MySQLHandshakeResponsePacket>
    {
        protected override MySQLHandshakeResponsePacket CreatePackage()
        {
            return new MySQLHandshakeResponsePacket();
        }

        protected override IPackagePartReader<MySQLHandshakeResponsePacket> GetFirstPartReader()
        {
            return MySQLHandshakeResponsePartReader.PackageHeadReader;
        }
    }

    /// <summary>
    /// Part reader for MySQL handshake response packets.
    /// </summary>
    public class MySQLHandshakeResponsePartReader : IPackagePartReader<MySQLHandshakeResponsePacket>
    {
        public static IPackagePartReader<MySQLHandshakeResponsePacket> PackageHeadReader { get; private set; }

        static MySQLHandshakeResponsePartReader()
        {
            PackageHeadReader = new MySQLHandshakeResponsePartReader();
        }

        public bool Process(MySQLHandshakeResponsePacket package, ref SequenceReader<byte> reader, out IPackagePartReader<MySQLHandshakeResponsePacket> nextPartReader, out bool needMoreData)
        {
            nextPartReader = null;
            needMoreData = false;

            try
            {
                var parsedPacket = MySQLHandshakeResponsePacket.ParseFromSequenceReader(ref reader);
                
                // Copy parsed data to the package
                package.CapabilityFlags = parsedPacket.CapabilityFlags;
                package.MaxPacketSize = parsedPacket.MaxPacketSize;
                package.CharacterSet = parsedPacket.CharacterSet;
                package.Reserved = parsedPacket.Reserved;
                package.Username = parsedPacket.Username;
                package.AuthResponse = parsedPacket.AuthResponse;
                package.Database = parsedPacket.Database;
                package.AuthPluginName = parsedPacket.AuthPluginName;

                return true;
            }
            catch
            {
                needMoreData = true;
                return false;
            }
        }
    }
}