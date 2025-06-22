using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SuperSocket.Client;
using SuperSocket.MySQL.Packets;
using SuperSocket.ProtoBase;

namespace SuperSocket.MySQL
{
    public class MySQLConnection : EasyClient<MySQLPacket>
    {
        private const int DefaultPort = 3306;
        private readonly string _host;
        private readonly int _port;
        private readonly string _userName;
        private readonly string _password;

        private static readonly MySQLPacketEncoder PacketEncoder = new MySQLPacketEncoder();

        public MySQLConnection(string host, int port, string userName, string password)
            : this(new MySQLPacketFactory().RegisterPacketType<HandshakeResponsePacket>(0x00))
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _port = port > 0 ? port : DefaultPort;
            _userName = userName ?? throw new ArgumentNullException(nameof(userName));
            _password = password ?? throw new ArgumentNullException(nameof(password));
        }

        internal MySQLConnection(IMySQLPacketFactory mySQLPacketFactory)
            : this(new MySQLPacketDecoder(mySQLPacketFactory))
        {
        }

        internal MySQLConnection(IPackageDecoder<MySQLPacket> packageDecoder)
            : base(new MySQLPacketFilter(packageDecoder))
        {
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_host))
                throw new ArgumentException("Host cannot be null or empty.", nameof(_host));

            if (_port <= 0)
                throw new ArgumentOutOfRangeException(nameof(_port), "Port must be a positive integer.");

            var endPoint = new DnsEndPoint(_host, _port);

            await ConnectAsync(endPoint, cancellationToken).ConfigureAwait(false);

            // Send initial handshake packet
            var handshakePacket = new HandshakePacket();
            await SendAsync(PacketEncoder, handshakePacket).ConfigureAwait(false);

            // Handle authentication to be implemented here
        }
    }
}