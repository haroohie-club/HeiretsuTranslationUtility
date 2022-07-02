using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaruhiHeiretsuLib.Archive
{
    public class McbSubArchive
    {
        public ushort Id { get; set; }
        public short Padding { get; set; }
        public int Offset { get; set; }
        public int Size { get; set; }

        public List<FileInArchive> Files { get; set; } = new List<FileInArchive>();

        public McbSubArchive(int parentLoc, ushort id, short padding, int offset, int size, byte[] data)
        {
            Id = id;
            Padding = padding;
            Offset = offset;
            Size = size;
            int childLoc = 0;

            for (int i = Offset; i < Offset + Size;)
            {
                int archiveIndex = BitConverter.ToInt32(data.Skip(i).Take(4).ToArray());
                int archiveOffset = BitConverter.ToInt32(data.Skip(i + 4).Take(4).ToArray());
                int compressedSize = BitConverter.ToInt32(data.Skip(i + 8).Take(4).ToArray());

                if (archiveIndex == 0x7FFF)
                {
                    break;
                }

                byte[] compressedData = data.Skip(i + 12).Take(compressedSize).ToArray();

                Files.Add(new FileInArchive() { Location = (parentLoc, childLoc++), Offset = i, McbId = Id, McbEntryData = (archiveIndex, archiveOffset), CompressedData = compressedData, Data = Helpers.DecompressData(compressedData).ToList() });

                i += compressedSize + 12;
            }
        }

        public byte[] GetBytes()
        {
            var bytes = new List<byte>();

            foreach (FileInArchive file in Files)
            {
                bytes.AddRange(BitConverter.GetBytes(file.McbEntryData.archiveIndex));
                bytes.AddRange(BitConverter.GetBytes(file.McbEntryData.archiveOffset));

                if (!file.Edited)
                {
                    bytes.AddRange(BitConverter.GetBytes(file.CompressedData.Length));
                    bytes.AddRange(file.CompressedData);
                }
                else
                {
                    byte[] compressedData = Helpers.CompressData(file.GetBytes());
                    int padding = (compressedData.Length % 0x800) == 0 ? 0x800 : 0x800 - (compressedData.Length % 0x800);
                    
                    bytes.AddRange(BitConverter.GetBytes(compressedData.Length + padding));
                    bytes.AddRange(compressedData);
                    bytes.AddRange(new byte[padding]);
                }
            }
            bytes.AddRange(BitConverter.GetBytes(0x7FFF)); // end bytes
            bytes.AddRange(new byte[bytes.Count % 0x1000 == 0 ? 0x1000 : 0x1000 - (bytes.Count % 0x1000)]);

            return bytes.ToArray();
        }
    }
}
