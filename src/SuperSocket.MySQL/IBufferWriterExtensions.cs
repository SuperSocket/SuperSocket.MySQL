using System;
using System.Buffers;
using System.Text;

namespace SuperSocket.MySQL
{
    public static class IBufferWriterExtensions
    {
        public static int WriteUInt8(this IBufferWriter<byte> writer, byte value)
        {
            var span = writer.GetSpan(1);
            span[0] = value;
            writer.Advance(1);
            return 1;
        }

        public static int WriteUInt16(this IBufferWriter<byte> writer, ushort value)
        {
            var span = writer.GetSpan(2);
            span[0] = (byte)value;
            span[1] = (byte)(value >> 8);
            writer.Advance(2);
            return 2;
        }

        public static int WriteUInt24(this IBufferWriter<byte> writer, uint value)
        {
            var span = writer.GetSpan(3);
            span[0] = (byte)value;
            span[1] = (byte)(value >> 8);
            span[2] = (byte)(value >> 16);
            writer.Advance(3);
            return 3;
        }

        public static int WriteUInt32(this IBufferWriter<byte> writer, uint value)
        {
            var span = writer.GetSpan(4);
            span[0] = (byte)value;
            span[1] = (byte)(value >> 8);
            span[2] = (byte)(value >> 16);
            span[3] = (byte)(value >> 24);
            writer.Advance(4);
            return 4;
        }

        public static int WriteUInt64(this IBufferWriter<byte> writer, ulong value)
        {
            var span = writer.GetSpan(8);
            span[0] = (byte)value;
            span[1] = (byte)(value >> 8);
            span[2] = (byte)(value >> 16);
            span[3] = (byte)(value >> 24);
            span[4] = (byte)(value >> 32);
            span[5] = (byte)(value >> 40);
            span[6] = (byte)(value >> 48);
            span[7] = (byte)(value >> 56);
            writer.Advance(8);
            return 8;
        }

        public static int WriteLengthEncodedInteger(this IBufferWriter<byte> writer, ulong value)
        {
            if (value < 251)
            {
                writer.WriteUInt8((byte)value);
                return 1;
            }
            else if (value < 65536)
            {
                writer.WriteUInt8(0xFC);
                writer.WriteUInt16((ushort)value);
                return 3;
            }
            else if (value < 16777216)
            {
                writer.WriteUInt8(0xFD);
                writer.WriteUInt24((uint)value);
                return 4;
            }
            else
            {
                writer.WriteUInt8(0xFE);
                writer.WriteUInt64(value);
                return 9;
            }
        }

        public static int WriteFixedString(this IBufferWriter<byte> writer, string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            var maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);
            var span = writer.GetSpan(maxByteCount);
            var bytesWritten = Encoding.UTF8.GetBytes(value, span);
            writer.Advance(bytesWritten);
            return bytesWritten;
        }

        public static int WriteNullTerminatedString(this IBufferWriter<byte> writer, string value)
        {
            var bytesWritten = 0;
            
            if (!string.IsNullOrEmpty(value))
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(value);
                var span = writer.GetSpan(bytes.Length + 1);
                bytes.CopyTo(span);
                span[bytes.Length] = 0; // null terminator
                writer.Advance(bytes.Length + 1);
                bytesWritten = bytes.Length + 1;
            }
            else
            {
                var span = writer.GetSpan(1);
                span[0] = 0; // null terminator
                writer.Advance(1);
                bytesWritten = 1;
            }
            
            return bytesWritten;
        }

        public static void WriteLengthEncodedString(this IBufferWriter<byte> writer, string value)
        {
            writer.WriteLengthEncodedInteger((ulong)value.Length);
            writer.WriteFixedString(value);
        }
    }
}