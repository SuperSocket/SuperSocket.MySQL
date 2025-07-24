using System;
using System.Buffers;
using System.Text;

namespace SuperSocket.MySQL
{
    public static class SequenceReaderExtensions
    {
        public static bool TryReadLengthEncodedInteger(this ref SequenceReader<byte> reader, out long value)
        {
            value = 0;

            if (!reader.TryRead(out byte firstByte))
                return false;

            if (firstByte < 251)
            {
                value = firstByte;
                return true;
            }
            else if (firstByte == 252)
            {
                return reader.TryReadLittleEndian(out short shortValue) && (value = shortValue) >= 0;
            }
            else if (firstByte == 253)
            {
                if (!reader.TryRead(out byte b1) || !reader.TryRead(out byte b2) || !reader.TryRead(out byte b3))
                    return false;
                value = b1 | (b2 << 8) | (b3 << 16);
                return true;
            }
            else if (firstByte == 254)
            {
                return reader.TryReadLittleEndian(out value);
            }

            return false;
        }

        public static bool TryReadLengthEncodedString(this ref SequenceReader<byte> reader, out string value)
        {
            value = string.Empty;

            if (!reader.TryReadLengthEncodedInteger(out long length))
                return false;

            if (length == 0)
                return true;

            if (!reader.TryReadExact((int)length, out ReadOnlySequence<byte> sequence))
                return false;

            value = Encoding.UTF8.GetString(sequence);
            return true;
        }

        public static bool TryReadNullTerminatedString(this ref SequenceReader<byte> reader, out string value)
        {
            value = string.Empty;

            if (!reader.TryReadTo(out ReadOnlySequence<byte> sequence, 0))
                return false;

            value = Encoding.UTF8.GetString(sequence);
            return true;
        }

        public static ulong ReadLengthEncodedInteger(this ref SequenceReader<byte> reader)
        {
            if (TryReadLengthEncodedInteger(ref reader, out long value))
            {
                return (ulong)value;
            }
            throw new InvalidOperationException("Failed to read length-encoded integer");
        }
    }
}