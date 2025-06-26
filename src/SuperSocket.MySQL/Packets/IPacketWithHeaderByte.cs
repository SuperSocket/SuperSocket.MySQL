namespace SuperSocket.MySQL.Packets
{
    internal interface IPacketWithHeaderByte
    {
        byte Header { get; set; }
    }
}