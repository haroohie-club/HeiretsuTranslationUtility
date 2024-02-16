using HaruhiHeiretsuLib.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HaruhiHeiretsuLib.Archive
{
    /// <summary>
    /// A representation of the *.bin archive files
    /// </summary>
    /// <typeparam name="T">The type of file contained by the archive</typeparam>
    public class BinArchive<T>
        where T : FileInArchive, new()
    {
        /// <summary>
        /// The offset of the first magic integer
        /// </summary>
        public const int FirstMagicIntegerOffset = 0x14;

        /// <summary>
        /// The file name of the bin archive
        /// </summary>
        public string FileName { get; private set; }
        /// <summary>
        /// The number of files in the archive
        /// </summary>
        public int NumFiles { get; private set; }
        /// <summary>
        /// The alignment in bytes of the files in the archive
        /// </summary>
        public int FileAlignment { get; private set; }
        internal int MagicIntegerLsbMultiplier { get; set; }
        internal int MagicIntegerLsbMask { get; set; }
        internal int MagicIntegerMsbShift { get; set; }
        internal List<uint> MagicIntegers { get; set; } = [];
        /// <summary>
        /// The list of files in the archive
        /// </summary>
        public List<T> Files { get; private set; } = [];
        private Dictionary<int, int> _lengthToMagicIntegerMap = [];

        /// <summary>
        /// Loads a bin archive from a file
        /// </summary>
        /// <param name="fileName">The file to load the bin archive from</param>
        /// <returns>An object representing the bin archive</returns>
        public static BinArchive<T> FromFile(string fileName)
        {
            byte[] archiveBytes = File.ReadAllBytes(fileName);
            return new BinArchive<T>(archiveBytes) { FileName = Path.GetFileName(fileName) };
        }

        internal BinArchive(byte[] archiveBytes)
        {
            // Convert the main header components
            NumFiles = IO.ReadIntLE(archiveBytes, 0);

            FileAlignment = IO.ReadIntLE(archiveBytes, 0x04);
            MagicIntegerLsbMultiplier = IO.ReadIntLE(archiveBytes, 0x08);

            MagicIntegerLsbMask = IO.ReadIntLE(archiveBytes, 0x10);
            MagicIntegerMsbShift = IO.ReadIntLE(archiveBytes, 0x0C);

            // Grab all the magic integers
            for (int i = 0; i <= MagicIntegerLsbMask; i++)
            {
                int length = GetFileLength((uint)i);
                if (!_lengthToMagicIntegerMap.ContainsKey(length))
                {
                    _lengthToMagicIntegerMap.Add(length, i);
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
                    file.BinArchiveIndex = i + 1;
                    file.Length = compressedLength;
                    file.CompressedData = [.. fileBytes];
                    Files.Add(file);
                }
            }
        }

        private int GetFileOffset(uint magicInteger)
        {
            return (int)((magicInteger >> MagicIntegerMsbShift) * FileAlignment);
        }

        private uint GetNewMagicInteger(T file, int compressedLength)
        {
            uint offsetComponent = (uint)(file.Offset / FileAlignment) << MagicIntegerMsbShift;
            int newLength = (compressedLength + 0x7FF) & ~0x7FF; // round to nearest 0x800
            int newLengthComponent = _lengthToMagicIntegerMap[newLength];

            return offsetComponent | (uint)newLengthComponent;
        }

        private int GetFileLength(uint magicInteger)
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

        /// <summary>
        /// Gets the binary representation of the bin archive as well as an offset adjustment dictionary
        /// </summary>
        /// <param name="offsetAdjustments">A dictionary of adjustments to make within the mcb</param>
        /// <returns>The binary representation of the bin archive</returns>
        public byte[] GetBytes(out Dictionary<int, int> offsetAdjustments)
        {
            List<byte> bytes = [];
            offsetAdjustments = new()
            {
                { Files[0].Offset, Files[0].Offset }
            };

            bytes.AddRange(BitConverter.GetBytes(NumFiles));
            bytes.AddRange(BitConverter.GetBytes(FileAlignment));
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
                    int magicIntegerOffset = FirstMagicIntegerOffset + ((Files[i].BinArchiveIndex - 1) * 4);
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
                        pointerShift = ((bytes.Count - Files[i + 1].Offset) / FileAlignment) + 1;
                    }
                    if (pointerShift > 0)
                    {
                        // Calculate the new magic integer factoring in pointer shift
                        Files[i + 1].Offset = ((Files[i + 1].Offset / FileAlignment) + pointerShift) * FileAlignment;
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

            return [.. bytes];
        }
    }
}
