using System;
using System.Buffers;

namespace SuperSocket.MySQL.Packets
{
    public class UnknownPacket : MySQLPacket
    {
        protected internal override MySQLPacket Decode(ref SequenceReader<byte> reader, object context)
        {
            var filterContext = context as MySQLFilterContext;

            if (filterContext.State != MySQLConnectionState.CommandPhase)
            {
                return new ErrorPacket
                {
                    ErrorCode = 1047, // ER_UNKNOWN_COM_ERROR
                    ErrorMessage = "Unknown command or packet type."
                };
            }
            
            return (filterContext.NextPacket ?? new ColumnCountPacket()).Decode(ref reader, context);
        }

        protected internal override int Encode(IBufferWriter<byte> writer)
        {
            throw new NotSupportedException();
        }
    }
}