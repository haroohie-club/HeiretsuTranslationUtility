using HaruhiHeiretsuLib.Util;
using System.Collections.Generic;
using System.Linq;

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
            T newFile = new()
            {
                McbId = McbId,
                Location = Location,
                McbEntryData = McbEntryData,
                MagicInteger = MagicInteger,
                Index = Index,
                Offset = Offset,
                Length = Length,
                Data = Data,
                CompressedData = CompressedData,
                Edited = Edited
            };
            newFile.Initialize(Data.ToArray(), Offset);

            return newFile;
        }
    }

    public static class FileManager<T>
        where T : FileInArchive, new()
    {
        public static T FromCompressedData(byte[] compressedData, int offset = 0, uint magicInteger = 0, int index = -1, int length = -1)
        {
            T created = new()
            {
                MagicInteger = magicInteger,
                Index = index,
                Length = length,
                Location = (-1, -1)
            };
            created.Initialize(Helpers.DecompressData(compressedData), offset);
            return created;
        }
    }
}
