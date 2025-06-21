using System;
using System.Buffers;

namespace SuperSocket.MySQL
{
    public abstract class MySQLPacket
    {

        /// <summary>
        /// Decodes the body of the log event from the binary data.
        /// </summary>
        /// <param name="reader">The sequence reader containing binary data.</param>
        /// <param name="context">The context object containing additional information.</param>
        protected internal abstract void Decode(ref SequenceReader<byte> reader, object context);

        protected internal abstract int Encode(IBufferWriter<byte> writer);
    }
}