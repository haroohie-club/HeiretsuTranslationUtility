using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaruhiHeiretsuLib.Archive
{
    public partial class FileInArchive
    {
        public ushort McbId { get; set; }
        public (int parent, int child) Location { get; set; } = (-1, -1);
        public (int archiveIndex, int archiveOffset) McbEntryData { get; set; }
        public uint MagicInteger { get; set; }
        public int Index { get; set; }
        public int Offset { get; set; }
        public int Length { get; set; }
        public List<byte> Data { get; set; }
        public byte[] CompressedData { get; set; }
        public bool Edited { get; set; } = false;

        public virtual void Initialize(byte[] decompressedData, int offset)
        {
            Data = decompressedData.ToList();
            Offset = offset;
        }
        public virtual byte[] GetBytes()
        {
            return Data.ToArray();
        }

        public FileInArchive()
        {
        }

        public T CastTo<T>() where T : FileInArchive, new()
        {
            T newFile = new();
            newFile.McbId = McbId;
            newFile.Location = Location;
            newFile.McbEntryData = McbEntryData;
            newFile.MagicInteger = MagicInteger;
            newFile.Index = Index;
            newFile.Offset = Offset;
            newFile.Length = Length;
            newFile.Data = Data;
            newFile.CompressedData = CompressedData;
            newFile.Edited = Edited;
            newFile.Initialize(Data.ToArray(), Offset);

            return newFile;
        }
    }

    public static class FileManager<T>
        where T : FileInArchive, new()
    {
        public static T FromCompressedData(byte[] compressedData, int offset = 0, uint magicInteger = 0, int index = -1, int length = -1)
        {
            T created = new();
            created.MagicInteger = magicInteger;
            created.Index = index;
            created.Length = length;
            created.Location = (-1, -1);
            created.Initialize(Helpers.DecompressData(compressedData), offset);
            return created;
        }
    }
}
