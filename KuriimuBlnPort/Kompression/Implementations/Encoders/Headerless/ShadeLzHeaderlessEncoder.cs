using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kompression.Implementations.PriceCalculators;
using Kontract.Kompression.Configuration;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.Implementations.Encoders.Headerless
{
    class ShadeLzHeaderlessEncoder : ILzEncoder
    {
        public void Configure(IInternalMatchOptions matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new SpikeChunsoftPriceCalculator())
                .FindMatches().WithinLimitations(4, -1, 1, 0x1FFF)
                .AndFindRunLength().WithinLimitations(4, 0x1003);
        }

        public void Encode(Stream input, Stream output, IEnumerable<Match> matches)
        {
            foreach (var match in matches)
            {
                if (input.Position < match.Position)
                    WriteRawData(input, output, match.Position - input.Position);

                WriteMatchData(input, output, match);
            }

            if (input.Position < input.Length)
                WriteRawData(input, output, input.Length - input.Position);
        }

        private void WriteRawData(Stream input, Stream output, long length)
        {
            while (length > 0)
            {
                var cappedLength = Math.Min(length, 0x1FFF);
                if (cappedLength <= 0x1F)
                    output.WriteByte((byte)cappedLength);
                else
                {
                    output.WriteByte((byte)(0x20 | (cappedLength >> 8)));
                    output.WriteByte((byte)cappedLength);
                }

                for (var i = 0; i < cappedLength; i++)
                    output.WriteByte((byte)input.ReadByte());

                length -= cappedLength;
            }
        }

        private void WriteMatchData(Stream input, Stream output, Match match)
        {
            var length = match.Length - 4;
            if (match.Displacement == 0)
            {
                // Rle
                if (length <= 0xF)
                    output.WriteByte((byte)(0x40 | length));
                else
                {
                    output.WriteByte((byte)(0x50 | (length >> 8)));
                    output.WriteByte((byte)length);
                }

                output.WriteByte((byte)input.ReadByte());
                input.Position--;
            }
            else
            {
                // Lz

                // Write displacement part first
                var cappedLength = Math.Min(length, 3);

                output.WriteByte((byte)(0x80 | (cappedLength << 5) | (match.Displacement >> 8)));
                output.WriteByte((byte)match.Displacement);

                length -= cappedLength;
                while (length > 0)
                {
                    cappedLength = Math.Min(length, 0x1F);

                    output.WriteByte((byte)(0x60 | cappedLength));

                    length -= cappedLength;
                }
            }

            input.Position += match.Length;
        }

        public void Dispose()
        {
        }

        public static byte[] CompressData(byte[] decompressedData)
        {
            // nonsense hack to deal with a rare edge case where the last byte of a file could get dropped
            List<byte> temp = decompressedData.ToList();
            temp.Add(0x00);
            decompressedData = temp.ToArray();

            List<byte> compressedData = new List<byte>();

            int directBytesToWrite = 0;
            Dictionary<LookbackEntry, List<int>> lookbackDictionary = new Dictionary<LookbackEntry, List<int>>();
            for (int i = 0; i < decompressedData.Length;)
            {
                int numNext = Math.Min(decompressedData.Length - i - 1, 4);
                if (numNext == 0)
                {
                    break;
                }

                List<byte> nextBytes = decompressedData.Skip(i).Take(numNext).ToList();
                LookbackEntry nextEntry = new LookbackEntry(nextBytes, i);
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
                            List<byte> lookbackSequence = new List<byte>();
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
    }
}
