using SkiaSharp;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiHeiretsuLib.Util
{
    public static class Helpers
    {
        private const ulong F2 = 0x4330000080000000L;
        private const ulong F0 = 0x3F50000000000000L;

        public static short? ToShortOrDefault(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length != 2)
            {
                return null;
            }
            else
            {
                return IO.ReadShort(bytes, 0);
            }
        }

        public static int RoundToNearest16(int i)
        {
            return (int)((i + 0xF) & 0xFFFFFFF0);
        }

        public static void PadToNearest16(this List<byte> bytes)
        {
            int padding = 16 - (bytes.Count % 16);
            if (padding != 16)
            {
                bytes.AddRange(new byte[padding]);
            }
        }

        public static int FloatToInt(float value)
        {
            double d1 = value / BitConverter.UInt64BitsToDouble(F0);
            ulong f1 = BitConverter.DoubleToUInt64Bits(d1 + BitConverter.UInt64BitsToDouble(F2));
            uint adjustedInt = (uint)f1;
            return (int)(adjustedInt ^ 0x80000000);
        }

        public static float IntToFloat(int startingInt)
        {
            uint adjustedInt = (uint)startingInt ^ 0x80000000;
            ulong f1 = F2 & 0xFFFFFFFF00000000L | adjustedInt;
            double d1 = BitConverter.UInt64BitsToDouble(f1) - BitConverter.UInt64BitsToDouble(F2);
            double d31 = d1 * BitConverter.UInt64BitsToDouble(F0);

            return (float)d31;
        }

        public static SKBitmap FlipBitmap(this SKBitmap bitmap)
        {
            SKBitmap flippedBitmap = new(bitmap.Width, bitmap.Height);
            using SKCanvas canvas = new(flippedBitmap);
            canvas.Scale(1, -1, 0, bitmap.Height / 2);
            canvas.DrawBitmap(bitmap, 0, 0);
            return flippedBitmap;
        }

        public static List<byte> GetPaddedByteArrayFromString(string text)
        {
            List<byte> stringBytes = [.. Encoding.GetEncoding("Shift-JIS").GetBytes(text)];
            stringBytes.AddRange(new byte[stringBytes.Count % 4 == 0 ? 4 : 4 - stringBytes.Count % 4]);
            return stringBytes;
        }

        // redmean color distance formula with alpha term
        public static double ColorDistance(SKColor color1, SKColor color2)
        {
            double redmean = (color1.Red + color2.Red) / 2.0;

            return Math.Sqrt((2 + redmean / 256) * Math.Pow(color1.Red - color2.Red, 2)
                + 4 * Math.Pow(color1.Green - color2.Green, 2)
                + (2 + (255 - redmean) / 256) * Math.Pow(color1.Blue - color2.Blue, 2)
                + Math.Pow(color1.Alpha - color2.Alpha, 2));
        }

        public static int ClosestColorIndex(List<SKColor> colors, SKColor color)
        {
            var colorDistances = colors.Select(c => ColorDistance(c, color)).ToList();

            return colorDistances.IndexOf(colorDistances.Min());
        }

        public static bool AddWillCauseCarry(int x, int y)
        {
            return ((x & 0xFFFFFFFFfL) + (y & 0xFFFFFFFFL) & 0x1000000000) > 0;
        }

        public static bool BytesInARowLessThan(this IEnumerable<byte> sequence, int numBytesInARowLessThan, byte targetByte)
        {
            for (int i = 0; i < sequence.Count() - numBytesInARowLessThan; i++)
            {
                if (sequence.Skip(i).TakeWhile(b => b == targetByte).Count() > numBytesInARowLessThan)
                {
                    return false;
                }
            }
            return true;
        }

        public static int GetIntFromByteArray(ReadOnlySpan<byte> data, int position)
        {
            return IO.ReadInt(data, position * 4);
        }

        public static byte[] GetStringBytes(string str)
        {
            byte[] stringBytes = Encoding.GetEncoding("Shift-JIS").GetBytes(str);
            byte[] bytes = new byte[stringBytes.Length + 5];
            BinaryPrimitives.WriteInt32BigEndian(bytes, stringBytes.Length);
            Array.Copy(stringBytes, 0, bytes, 4, stringBytes.Length);

            return bytes;
        }

        public static byte[] CompressData(byte[] decompressedData)
        {
            // nonsense hack to deal with a rare edge case where the last byte of a file could get dropped
            List<byte> temp = [.. decompressedData];
            temp.Add(0x00);
            decompressedData = [.. temp];

            List<byte> compressedData = [];

            int directBytesToWrite = 0;
            Dictionary<LookbackEntry, List<int>> lookbackDictionary = [];
            for (int i = 0; i < decompressedData.Length;)
            {
                int numNext = Math.Min(decompressedData.Length - i - 1, 4);
                if (numNext == 0)
                {
                    break;
                }

                List<byte> nextBytes = decompressedData.Skip(i).Take(numNext).ToList();
                LookbackEntry nextEntry = new(nextBytes, i);
                if (lookbackDictionary.ContainsKey(nextEntry) && i - lookbackDictionary[nextEntry].Max() <= 0x1FFF)
                {
                    if (directBytesToWrite > 0)
                    {
                        WriteDirectBytes(decompressedData, compressedData, i, directBytesToWrite);
                        directBytesToWrite = 0;
                    }

                    int lookbackIndex = 0;
                    int longestSequenceLength = 0;
                    foreach (int index in lookbackDictionary[nextEntry])
                    {
                        if (i - index <= 0x1FFF)
                        {
                            List<byte> lookbackSequence = [];
                            for (int j = 0; i + j < decompressedData.Length && decompressedData[index + j] == decompressedData[i + j]; j++)
                            {
                                lookbackSequence.Add(decompressedData[lookbackIndex + j]);
                            }
                            if (lookbackSequence.Count > longestSequenceLength)
                            {
                                longestSequenceLength = lookbackSequence.Count;
                                lookbackIndex = index;
                            }
                        }
                    }
                    lookbackDictionary[nextEntry].Add(i);

                    int encodedLookbackIndex = i - lookbackIndex;
                    int encodedLength = longestSequenceLength - 4;
                    int remainingEncodedLength = 0;
                    if (encodedLength > 3)
                    {
                        remainingEncodedLength = encodedLength - 3;
                        encodedLength = 3;
                    }
                    byte firstByte = (byte)(encodedLookbackIndex / 0x100 | encodedLength << 5 | 0x80);
                    byte secondByte = (byte)(encodedLookbackIndex & 0xFF);
                    compressedData.AddRange(new byte[] { firstByte, secondByte });
                    if (remainingEncodedLength > 0)
                    {
                        while (remainingEncodedLength > 0)
                        {
                            int currentEncodedLength = Math.Min(remainingEncodedLength, 0x1F);
                            remainingEncodedLength -= currentEncodedLength;

                            compressedData.Add((byte)(0x60 | currentEncodedLength));
                        }
                    }

                    i += longestSequenceLength;
                }
                else if (nextBytes.Count == 4 && nextBytes.All(b => b == nextBytes[0]))
                {
                    if (directBytesToWrite > 0)
                    {
                        WriteDirectBytes(decompressedData, compressedData, i, directBytesToWrite);
                        directBytesToWrite = 0;
                    }

                    List<byte> repeatedBytes = decompressedData.Skip(i).TakeWhile(b => b == nextBytes[0]).ToList();
                    int numRepeatedBytes = Math.Min(0x1F3, repeatedBytes.Count);
                    if (numRepeatedBytes <= 0x13)
                    {
                        compressedData.Add((byte)(0x40 | numRepeatedBytes - 4)); // 0x40 -- repeated byte, 4-bit length
                    }
                    else
                    {
                        int numToEncode = numRepeatedBytes - 4;
                        int msb = numToEncode & 0xF00;
                        byte firstByte = (byte)(0x50 | msb / 0x100);
                        byte secondByte = (byte)(numToEncode - msb); // 0x50 -- repeated byte, 12-bit length
                        compressedData.AddRange(new byte[] { firstByte, secondByte });
                    }
                    compressedData.Add(repeatedBytes[0]);
                    i += numRepeatedBytes;
                }
                else
                {
                    if (directBytesToWrite + numNext > 0x1FFF)
                    {
                        WriteDirectBytes(decompressedData, compressedData, i, directBytesToWrite);
                        directBytesToWrite = 0;
                    }
                    if (!lookbackDictionary.ContainsKey(nextEntry))
                    {
                        lookbackDictionary.Add(nextEntry, [i]);
                    }
                    else
                    {
                        lookbackDictionary[nextEntry].Add(i);
                    }
                    directBytesToWrite++;
                    i++;
                }
            }

            if (directBytesToWrite > 0)
            {
                WriteDirectBytes(decompressedData, compressedData, decompressedData.Length - 1, directBytesToWrite);
            }

            return [.. compressedData];
        }

        private class LookbackEntry
        {
            public byte[] Bytes { get; set; }

            public LookbackEntry(List<byte> bytes, int index)
            {
                Bytes = [.. bytes];
            }

            public override bool Equals(object obj)
            {
                var other = (LookbackEntry)obj;
                if (other.Bytes.Length != Bytes.Length)
                {
                    return false;
                }
                bool equals = true;
                for (int i = 0; i < Bytes.Length; i++)
                {
                    equals = equals && Bytes[i] == other.Bytes[i];
                }
                return equals;
            }

            public override int GetHashCode()
            {
                string hash = "";
                foreach (byte @byte in Bytes)
                {
                    hash += $"{@byte}";
                }
                return hash.GetHashCode();
            }
        }

        private static void WriteDirectBytes(byte[] writeFrom, List<byte> writeTo, int position, int numBytesToWrite)
        {
            if (numBytesToWrite < 0x20)
            {
                writeTo.Add((byte)numBytesToWrite);
            }
            else
            {
                int msb = 0x1F00 & numBytesToWrite;
                byte firstByte = (byte)(0x20 | msb / 0x100);
                byte secondByte = (byte)(numBytesToWrite - msb);
                writeTo.AddRange(new byte[] { firstByte, secondByte });
            }
            writeTo.AddRange(writeFrom.Skip(position - numBytesToWrite).Take(numBytesToWrite));
        }

        public static byte[] DecompressData(byte[] compressedData)
        {
            List<byte> decompressedData = [];

            // documentation note: bits 1234 5678 in a byte
            for (int z = 0; z < compressedData.Length;)
            {
                int blockByte = compressedData[z++];
                if (blockByte == 0)
                {
                    break;
                }

                if ((blockByte & 0x80) == 0)
                {
                    if ((blockByte & 0x40) == 0)
                    {
                        // bits 1 & 2 == 0 --> direct data read
                        int numBytes;
                        if ((blockByte & 0x20) == 0)
                        {
                            numBytes = blockByte; // the `& 0x1F` is unnecessary since we've already determined bits 1-3 to be 0
                        }
                        else
                        {
                            // bit 3 == 1 --> need two bytes to indicate how much data to read
                            numBytes = compressedData[z++] + (blockByte & 0x1F) * 0x100;
                        }
                        for (int i = 0; i < numBytes; i++)
                        {
                            decompressedData.Add(compressedData[z++]);
                        }
                    }
                    else
                    {
                        // bit 1 == 0 && bit 2 == 1 --> repeated byte
                        int numBytes;
                        if ((blockByte & 0x10) == 0)
                        {
                            numBytes = (blockByte & 0x0F) + 4;
                        }
                        else
                        {
                            numBytes = compressedData[z++] + (blockByte & 0x0F) * 0x100 + 4;
                        }
                        byte repeatedByte = compressedData[z++];
                        for (int i = 0; i < numBytes; i++)
                        {
                            decompressedData.Add(repeatedByte);
                        }
                    }
                }
                else
                {
                    // bit 1 == 1 --> backreference
                    int numBytes = ((blockByte & 0x60) >> 0x05) + 4;
                    int backReferenceIndex = decompressedData.Count - (compressedData[z++] + (blockByte & 0x1F) * 0x100);
                    for (int i = backReferenceIndex; i < backReferenceIndex + numBytes; i++)
                    {
                        decompressedData.Add(decompressedData[i]);
                    }
                    while ((compressedData[z] & 0xE0) == 0x60)
                    {
                        int nextNumBytes = compressedData[z++] & 0x1F;
                        if (nextNumBytes > 0)
                        {
                            for (int i = backReferenceIndex + numBytes; i < backReferenceIndex + numBytes + nextNumBytes; i++)
                            {
                                decompressedData.Add(decompressedData[i]);
                            }
                        }
                        backReferenceIndex += nextNumBytes;
                    }
                }
            }

            // nonsense hack which corresponds to above nonsense hack
            if (decompressedData.Count % 16 == 1 && decompressedData.Last() == 0x00)
            {
                decompressedData.RemoveAt(decompressedData.Count - 1);
            }

            while (decompressedData.Count % 0x10 != 0)
            {
                decompressedData.Add(0x00);
            }
            return [.. decompressedData];
        }
    }
}
