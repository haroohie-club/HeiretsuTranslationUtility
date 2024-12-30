using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiHeiretsuLib.Util
{
    internal static class IO
    {
        public static int ReadInt(ReadOnlySpan<byte> data, int offset)
        {
            return BinaryPrimitives.ReadInt32BigEndian(data[offset..(offset + 4)]);
        }

        public static int ReadIntLE(ReadOnlySpan<byte> data, int offset)
        {
            return BitConverter.ToInt32(data[offset..(offset + 4)]);
        }

        public static uint ReadUInt(ReadOnlySpan<byte> data, int offset)
        {
            return BinaryPrimitives.ReadUInt32BigEndian(data[offset..(offset + 4)]);
        }

        public static uint ReadUIntLE(ReadOnlySpan<byte> data, int offset)
        {
            return BitConverter.ToUInt32(data[offset..(offset + 4)]);
        }

        public static short ReadShort(ReadOnlySpan<byte> data, int offset)
        {
            return BinaryPrimitives.ReadInt16BigEndian(data[offset..(offset + 2)]);
        }

        public static short ReadShortLE(ReadOnlySpan<byte> data, int offset)
        {
            return BitConverter.ToInt16(data[offset..(offset + 2)]);
        }

        public static ushort ReadUShort(ReadOnlySpan<byte> data, int offset)
        {
            return BinaryPrimitives.ReadUInt16BigEndian(data[offset..(offset + 2)]);
        }

        public static ushort ReadUShortLE(ReadOnlySpan<byte> data, int offset)
        {
            return BitConverter.ToUInt16(data[offset..(offset + 2)]);
        }

        public static float ReadFloat(ReadOnlySpan<byte> data, int offset)
        {
            return BitConverter.ToSingle(data[offset..(offset + 4)]);
        }

        public static float ReadFloatReverse(ReadOnlySpan<byte> data, int offset)
        {
            return BinaryPrimitives.ReadSingleBigEndian(data[offset..(offset + 4)]);
        }

        public static string ReadShiftJisString(IEnumerable<byte> data, int offset)
        {
            return Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(offset).TakeWhile(b => b != 0x00).ToArray());
        }

        public static string ReadAsciiString(IEnumerable<byte> data, int offset)
        {
            return Encoding.ASCII.GetString(data.Skip(offset).TakeWhile(b => b != 0x00).ToArray());
        }
    }
}