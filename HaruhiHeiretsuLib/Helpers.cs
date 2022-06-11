﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaruhiHeiretsuLib
{
    public static class Helpers
    {

        // redmean color distance formula with alpha term
        public static double ColorDistance(Color color1, Color color2)
        {
            double redmean = (color1.R + color2.R) / 2.0;

            return Math.Sqrt((2 + redmean / 256) * Math.Pow(color1.R - color2.R, 2)
                + 4 * Math.Pow(color1.G - color2.G, 2)
                + (2 + (255 - redmean) / 256) * Math.Pow(color1.B - color2.B, 2)
                + Math.Pow(color1.A - color2.A, 2));
        }

        public static int ClosestColorIndex(List<Color> colors, Color color)
        {
            var colorDistances = colors.Select(c => ColorDistance(c, color)).ToList();

            return colorDistances.IndexOf(colorDistances.Min());
        }

        public static bool AddWillCauseCarry(int x, int y)
        {
            return (((x & 0xFFFFFFFFfL) + (y & 0xFFFFFFFFL)) & 0x1000000000) > 0;
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

        public static int GetIntFromByteArray(IEnumerable<byte> data, int position)
        {
            return BitConverter.ToInt32(data.Skip(position * 4).Take(4).Reverse().ToArray());
        }

        public static byte[] GetStringBytes(string str)
        {
            List<byte> bytes = new();

            byte[] stringBytes = Encoding.GetEncoding("Shift-JIS").GetBytes(str);
            bytes.AddRange(BitConverter.GetBytes(stringBytes.Length + 1).Reverse());
            bytes.AddRange(stringBytes);
            bytes.Add(0);

            return bytes.ToArray();
        }

        public static byte[] CompressData(byte[] decompressedData)
        {
            // nonsense hack to deal with a rare edge case where the last byte of a file could get dropped
            List<byte> temp = decompressedData.ToList();
            temp.Add(0x00);
            decompressedData = temp.ToArray();

            List<byte> compressedData = new();

            int directBytesToWrite = 0;
            Dictionary<LookbackEntry, List<int>> lookbackDictionary = new();
            for (int i = 0; i < decompressedData.Length;)
            {
                int numNext = Math.Min(decompressedData.Length - i - 1, 4);
                if (numNext == 0)
                {
                    break;
                }

                List<byte> nextBytes = decompressedData.Skip(i).Take(numNext).ToList();
                LookbackEntry nextEntry = new(nextBytes, i);
                if (lookbackDictionary.ContainsKey(nextEntry) && (i - lookbackDictionary[nextEntry].Max()) <= 0x1FFF)
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
                            List<byte> lookbackSequence = new();
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
                    byte firstByte = (byte)((encodedLookbackIndex / 0x100) | (encodedLength << 5) | 0x80);
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
                        compressedData.Add((byte)(0x40 | (numRepeatedBytes - 4))); // 0x40 -- repeated byte, 4-bit length
                    }
                    else
                    {
                        int numToEncode = numRepeatedBytes - 4;
                        int msb = numToEncode & 0xF00;
                        byte firstByte = (byte)(0x50 | (msb / 0x100));
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
                    }
                    if (!lookbackDictionary.ContainsKey(nextEntry))
                    {
                        lookbackDictionary.Add(nextEntry, new List<int> { i });
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

            return compressedData.ToArray();
        }

        private class LookbackEntry
        {
            public byte[] Bytes { get; set; }

            public LookbackEntry(List<byte> bytes, int index)
            {
                Bytes = bytes.ToArray();
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
                    equals = equals && (Bytes[i] == other.Bytes[i]);
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
                byte firstByte = (byte)(0x20 | (msb / 0x100));
                byte secondByte = (byte)(numBytesToWrite - msb);
                writeTo.AddRange(new byte[] { firstByte, secondByte });
            }
            writeTo.AddRange(writeFrom.Skip(position - numBytesToWrite).Take(numBytesToWrite));
        }

        public static byte[] DecompressData(byte[] compressedData)
        {
            List<byte> decompressedData = new();

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
                            numBytes = compressedData[z++] + ((blockByte & 0x1F) * 0x100);
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
                            numBytes = compressedData[z++] + ((blockByte & 0x0F) * 0x100) + 4;
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
                    int backReferenceIndex = decompressedData.Count - (compressedData[z++] + ((blockByte & 0x1F) * 0x100));
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
            return decompressedData.ToArray();
        }
    }

    public class AsmDecompressionSimulator
    {
        private int z, c, l, n;
        private List<byte> _output = new();
        private byte[] _data;

        public byte[] Output { get { return _output.ToArray(); } }

        public AsmDecompressionSimulator(byte[] data)
        {
            _data = data;
            z = 0;
            Lxx_2026198();
        }

        private void Lxx_2026198()
        {
            if (z >= _data.Length)
            {
                return;
            }
            c = _data[z++];     // ldrb     r3,[r0],1h
            if (c == 0)         // cmp      r3,0h
            {
                return;         // beq      Lxx_20262A0h
            }
            if ((c & 0x80) == 0)
            {
                Lxx_2026224();
            }
            if (z >= _data.Length)
            {
                return;
            }
            l = _data[z++];                 // ldrb     r12,[r0],1h
            n = c & 0x60;                   // and      r14,r3,60h
            c = (int)((uint)c << 0x1B);     // mov      r3,r3,lsl 1Bh
            n >>= 0x05;                     // mov      r14,r14,asr 5h
            c = l | (int)((uint)c >> 0x13); // orr      r3,r12,r3,lsr 13h
            l = n + 4;                      // add      r12,r14,4h
            n = _output.Count - c;          // sub      r14,r1,r3
            Lxx_20261C8();
        }

        private void Lxx_20261C8()
        {
            while (l > 0)               // bgt      Lxx_20261C8h
            {
                if (n >= _output.Count)
                {
                    return;
                }
                c = _output[n++];       // ldrb     r3,[r14],1h
                l--;                    // sub      r12,r12,1h
                _output.Add((byte)c);   // strb     r3,[r1],1h     
            }
            c = _data[z];               // ldrb     r3,[r0]
            c &= 0xE0;                  // and      r3,r3,0E0h
            if (c != 0x60)              // cmp      r3,60h
            {
                Lxx_2026198();          // bne      Lxx_2026198h
            }
            Lxx_20261EC();
        }

        private void Lxx_20261EC()
        {
            if (z >= _data.Length)
            {
                return;
            }
            c = _data[z++];             // ldrb     r3,[r0],1h
            l = c & 0x1F;               // and      r12,r3,1Fh
            if (l <= 0)                 // cmp      r12,0h
            {
                Lxx_2026210();          // ble      Lxx_2026210h
            }
            Lxx_20261FC();
        }

        private void Lxx_20261FC()
        {
            while (l > 0)               // bgt      Lxx_20261FCh (self)
            {
                c = _output[n++];       // ldrb     r3,[r14],1h
                l--;                    // sub      r12,r12,1h
                _output.Add((byte)c);   // strb     r3,[r1],1h
            }
            Lxx_2026210();
        }

        private void Lxx_2026210()
        {
            if (z >= _data.Length)
            {
                return;
            }
            c = _data[z];               // ldrb     r3,[r0]
            c &= 0xE0;                  // and      r3,r3,0E0h
            if (c == 0x60)              // cmp      r3,60h
            {
                Lxx_20261EC();          // beq      Lxx_20261ECh
            }
            Lxx_2026198();              // b        Lxx_2026198h
        }

        private void Lxx_2026224()
        {
            if ((c & 0x40) == 0)            // tst      r3,40h
            {
                Lxx_2026268();              // beq      Lxx_2026268h
            }
            if ((c & 0x10) == 0)            // tst      r3,10h
            {
                c &= 0x0F;                  // andeq    r3,r3,0Fh
                Lxx_2026244();              // beq      Lxx_2026244h
            }
            if (z >= _data.Length)
            {
                return;
            }
            l = _data[z++];                 // ldrb     r12,[r0],1h
            c = (int)((uint)c << 0x1C);     // mov      r3,r3,lsl 1Ch
            c = l | (int)((uint)c >> 0x14); // orr      r3,r12,r3,lsr 14h
            Lxx_2026244();
        }

        private void Lxx_2026244()
        {
            l = c + 4;                  // add      r12,r3,4h
            if (z >= _data.Length)
            {
                return;
            }
            c = _data[z++];             // ldrb     r3,[r0],1h
            if (l <= 0)                 // cmp      r12,0h
            {
                Lxx_2026198();          // ble      Lxx_2026198h
            }
            Lxx_2026254();
        }

        private void Lxx_2026254()
        {
            while (l > 0)               // bgt      Lxx_2026254h
            {
                l--;                    // sub      r12,r12,1h
                _output.Add((byte)c);   // strb     r3,[r1],1h
            }
            Lxx_2026198();              // b        Lxx_2026198h
        }

        private void Lxx_2026268()
        {
            if ((c & 0x20) == 0)        // tst      r3,20h
            {
                l = c & 0x1F;           // andeq    r12,r3,1Fh
                Lxx_2026280();          // beq      Lxx_2026280h
            }
            if (z >= _data.Length)
            {
                return;
            }
            l = _data[z++];             // ldrb     r12,[r0],1h
            c = (int)((uint)c << 0x1B); // mov      r3,r3,lsl 1Bh
            l |= (int)((uint)c >> 0x13);// orr      r12,r12,r3,lsr 13h
            Lxx_2026280();
        }

        private void Lxx_2026280()
        {
            if (l <= 0)                 // cmp      r12,0h
            {
                Lxx_2026198();          // ble      Lxx_2026198h
            }
            while (l > 0)               // bgt      Lxx_2026288h
            {
                c = _data[z++];         // ldrb     r3,[r0],1h
                l--;                    // sub      r12,r12,1h
                _output.Add((byte)c);   // strb     r3,[r1],1h
            }
            Lxx_2026198();
        }
    }
}
