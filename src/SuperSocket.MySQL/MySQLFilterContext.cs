using System;
using System.Collections.Generic;
using SuperSocket.MySQL.Packets;

namespace SuperSocket.MySQL
{
    internal class MySQLFilterContext
    {
        public int SequenceId { get; set; }
        
        public bool IsHandshakeCompleted { get; set; }
        
        public MySQLConnectionState State { get; set; }

        public MySQLPacket NextPacket { get; set; }

        public int QueryResultColumnCount { get; set; }

        public List<ColumnDefinitionPacket> ColumnDefinitionPackets { get; set; }

        public MySQLFilterContext()
        {
            SequenceId = 0;
            IsHandshakeCompleted = false;
            State = MySQLConnectionState.Initial;
        }
        
        public void Reset()
        {
            SequenceId = 0;
            IsHandshakeCompleted = false;
            State = MySQLConnectionState.Initial;
        }
        
        public void IncrementSequenceId()
        {
            SequenceId = (SequenceId + 1) % 256;
        }
    }
    
    public enum MySQLConnectionState
    {
        Initial,
        HandshakeInitiated,
        Authenticated,
        CommandPhase,
        Closed
    }
}