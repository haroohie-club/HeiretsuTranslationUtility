using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HaruhiChokuretsuLib.Util;
using HaruhiHeiretsuLib.Util;

namespace HaruhiHeiretsuLib.Archive
{
    public class BinArchive<T>
        where T : FileInArchive, new()
    {
        public const int FirstMagicIntegerOffset = 0x14;

        public byte[] Header { get; set; }

        public string FileName { get; set; }
        public int NumFiles { get; set; }
        public int HeaderLength { get; set; }
        public int FileSpacing { get; set; }
        public int MagicIntegerLsbMultiplier { get; set; }
        public int MagicIntegerLsbMask { get; set; }
        public int MagicIntegerMsbShift { get; set; }
        public List<uint> MagicIntegers { get; set; } = new();
        public List<T> Files { get; set; } = new();
        public Dictionary<int, int> LengthToMagicIntegerMap { get; private set; } = new();

        public static BinArchive<T> FromFile(string fileName)
        {
            byte[] archiveBytes = File.ReadAllBytes(fileName);
            return new BinArchive<T>(archiveBytes) { FileName = Path.GetFileName(fileName) };
        }

        public BinArchive(byte[] archiveBytes)
        {
            // Convert the main header components
            NumFiles = IO.ReadIntLE(archiveBytes, 0);

            FileSpacing = IO.ReadIntLE(archiveBytes, 0x04);
            MagicIntegerLsbMultiplier = IO.ReadIntLE(archiveBytes, 0x08);

            MagicIntegerLsbMask = IO.ReadIntLE(archiveBytes, 0x10);
            MagicIntegerMsbShift = IO.ReadIntLE(archiveBytes, 0x0C);

            // Grab all the magic integers
            for (int i = 0; i <= MagicIntegerLsbMask; i++)
            {
                int length = GetFileLength((uint)i);
                if (!LengthToMagicIntegerMap.ContainsKey(length))
                {
                    LengthToMagicIntegerMap.Add(length, i);
                }
            }

            for (int i = FirstMagicIntegerOffset; i < (NumFiles * 4) + FirstMagicIntegerOffset; i += 4)
            {
                MagicIntegers.Add(IO.ReadUIntLE(archiveBytes, i));
            }

            // Add all the files to the archive from the magic integer offsets
            for (int i = 0; i < MagicIntegers.Count; i++)
            {
                int offset = GetFileOffset(MagicIntegers[i]);
                int compressedLength = GetFileLength(MagicIntegers[i]);
                byte[] fileBytes = archiveBytes.Skip(offset).Take(compressedLength).ToArray();
                if (fileBytes.Length > 0)
                {
                    T file = new();
                    try
                    {
                        file = FileManager<T>.FromCompressedData(fileBytes, offset);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        Console.WriteLine($"Failed to parse file at 0x{i:X8} due to index out of range exception (most likely during decompression)");
                    }
                    file.Offset = offset;
                    file.MagicInteger = MagicIntegers[i];
                    file.Index = i + 1;
                    file.Length = compressedLength;
                    file.CompressedData = fileBytes.ToArray();
                    Files.Add(file);
                }
            }
        }

        public int GetFileOffset(uint magicInteger)
        {
            return (int)((magicInteger >> MagicIntegerMsbShift) * FileSpacing);
        }

        public uint GetNewMagicInteger(T file, int compressedLength)
        {
            uint offsetComponent = (uint)(file.Offset / FileSpacing) << MagicIntegerMsbShift;
            int newLength = (compressedLength + 0x7FF) & ~0x7FF; // round to nearest 0x800
            int newLengthComponent = LengthToMagicIntegerMap[newLength];

            return offsetComponent | (uint)newLengthComponent;
        }

        public int GetFileLength(uint magicInteger)
        {
            // absolutely unhinged routine
            int magicLengthInt = 0x7FF + (int)((magicInteger & (uint)MagicIntegerLsbMask) * (uint)MagicIntegerLsbMultiplier);
            int standardLengthIncrement = 0x800;
            if (magicLengthInt < standardLengthIncrement)
            {
                magicLengthInt = 0;
            }
            else
            {
                int magicLengthIntLeftShift = 0x1C;
                uint salt = (uint)magicLengthInt >> 0x04;
                if (standardLengthIncrement <= salt >> 0x0C)
                {
                    magicLengthIntLeftShift -= 0x10;
                    salt >>= 0x10;
                }
                if (standardLengthIncrement <= salt >> 0x04)
                {
                    magicLengthIntLeftShift -= 0x08;
                    salt >>= 0x08;
                }
                if (standardLengthIncrement <= salt)
                {
                    magicLengthIntLeftShift -= 0x04;
                    salt >>= 0x04;
                }

                magicLengthInt = (int)((uint)magicLengthInt << magicLengthIntLeftShift);
                standardLengthIncrement = 0 - standardLengthIncrement;

                bool carryFlag = Helpers.AddWillCauseCarry(magicLengthInt, magicLengthInt);
                magicLengthInt *= 2;

                int pcIncrement = magicLengthIntLeftShift * 12;

                for (; pcIncrement <= 0x174; pcIncrement += 0x0C)
                {
                    bool nextCarryFlag = Helpers.AddWillCauseCarry(standardLengthIncrement, (int)(salt << 1) + (carryFlag ? 1 : 0));
                    salt = (uint)standardLengthIncrement + (salt << 1) + (uint)(carryFlag ? 1 : 0);
                    carryFlag = nextCarryFlag;
                    if (!carryFlag)
                    {
                        salt -= (uint)standardLengthIncrement;
                    }
                    nextCarryFlag = Helpers.AddWillCauseCarry(magicLengthInt, magicLengthInt + (carryFlag ? 1 : 0));
                    magicLengthInt = (magicLengthInt * 2) + (carryFlag ? 1 : 0);
                    carryFlag = nextCarryFlag;
                }
            }

            return magicLengthInt * 0x800;
        }

        public byte[] GetBytes(out Dictionary<int, int> offsetAdjustments)
        {
            List<byte> bytes = new();
            offsetAdjustments = new()
            {
                { Files[0].Offset, Files[0].Offset }
            };

            bytes.AddRange(BitConverter.GetBytes(NumFiles));
            bytes.AddRange(BitConverter.GetBytes(FileSpacing));
            bytes.AddRange(BitConverter.GetBytes(MagicIntegerLsbMultiplier));
            bytes.AddRange(BitConverter.GetBytes(MagicIntegerMsbShift));
            bytes.AddRange(BitConverter.GetBytes(MagicIntegerLsbMask));

            foreach (uint magicInteger in MagicIntegers)
            {
                bytes.AddRange(BitConverter.GetBytes(magicInteger));
            }

            bytes.AddRange(new byte[Files[0].Offset - bytes.Count]);

            for (int i = 0; i < Files.Count; i++)
            {
                byte[] compressedBytes;
                if (!Files[i].Edited || Files[i].Data is null || Files[i].Data.Count == 0)
                {
                    compressedBytes = Files[i].CompressedData;
                }
                else
                {
                    compressedBytes = Helpers.CompressData(Files[i].GetBytes());
                    byte[] newMagicalIntegerBytes = BitConverter.GetBytes(GetNewMagicInteger(Files[i], compressedBytes.Length));
                    int magicIntegerOffset = FirstMagicIntegerOffset + ((Files[i].Index - 1) * 4);
                    for (int j = 0; j < newMagicalIntegerBytes.Length; j++)
                    {
                        bytes[magicIntegerOffset + j] = newMagicalIntegerBytes[j];
                    }
                }
                bytes.AddRange(compressedBytes);
                if (i < Files.Count - 1) // If we aren't on the last file
                {
                    int originalOffset = Files[i + 1].Offset;
                    int pointerShift = 0;
                    while (bytes.Count % 0x10 != 0)
                    {
                        bytes.Add(0);
                    }
                    // If the current size of the archive we’ve constructed so far is greater than
                    // the next file’s offset, that means we need to adjust the next file’s offset
                    if (bytes.Count > Files[i + 1].Offset)
                    {
                        pointerShift = ((bytes.Count - Files[i + 1].Offset) / FileSpacing) + 1;
                    }
                    if (pointerShift > 0)
                    {
                        // Calculate the new magic integer factoring in pointer shift
                        Files[i + 1].Offset = ((Files[i + 1].Offset / FileSpacing) + pointerShift) * FileSpacing;
                        int magicIntegerOffset = FirstMagicIntegerOffset + (i + 1) * 4;
                        uint newMagicInteger = GetNewMagicInteger(Files[i + 1], Files[i + 1].Length);
                        Files[i + 1].MagicInteger = newMagicInteger;
                        MagicIntegers[i + 1] = newMagicInteger;
                        bytes.RemoveRange(magicIntegerOffset, 4);
                        bytes.InsertRange(magicIntegerOffset, BitConverter.GetBytes(Files[i + 1].MagicInteger));
                    }
                    bytes.AddRange(new byte[Files[i + 1].Offset - bytes.Count]);
                    offsetAdjustments.Add(originalOffset, Files[i + 1].Offset);
                }
            }
            while (bytes.Count % 0x800 != 0)
            {
                bytes.Add(0);
            }

            return bytes.ToArray();
        }
    }
}
