﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiHeiretsuLib.Util
{
    internal static class IO
    {
        public static int ReadInt(IEnumerable<byte> data, int offset)
        {
            return BitConverter.ToInt32(data.Skip(offset).Take(4).Reverse().ToArray());
        }
        public static int ReadIntLE(IEnumerable<byte> data, int offset)
        {
            return BitConverter.ToInt32(data.Skip(offset).Take(4).ToArray());
        }

        public static uint ReadUInt(IEnumerable<byte> data, int offset)
        {
            return BitConverter.ToUInt32(data.Skip(offset).Take(4).Reverse().ToArray());
        }
        public static uint ReadUIntLE(IEnumerable<byte> data, int offset)
        {
            return BitConverter.ToUInt32(data.Skip(offset).Take(4).ToArray());
        }

        public static short ReadShort(IEnumerable<byte> data, int offset)
        {
            return BitConverter.ToInt16(data.Skip(offset).Take(2).Reverse().ToArray());
        }
        public static short ReadShortLE(IEnumerable<byte> data, int offset)
        {
            return BitConverter.ToInt16(data.Skip(offset).Take(2).ToArray());
        }

        public static ushort ReadUShort(IEnumerable<byte> data, int offset)
        {
            return BitConverter.ToUInt16(data.Skip(offset).Take(2).Reverse().ToArray());
        }
        public static ushort ReadUShortLE(IEnumerable<byte> data, int offset)
        {
            return BitConverter.ToUInt16(data.Skip(offset).Take(2).ToArray());
        }

        public static float ReadFloat(IEnumerable<byte> data, int offset)
        {
            return BitConverter.ToSingle(data.Skip(offset).Take(4).ToArray());
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
