using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaruhiHeiretsuLib
{
    public partial class FileInArchive
    {
        public short McbId { get; set; }
        public (int parent, int child) Location { get; set; } = (-1, -1);
        public uint MagicInteger { get; set; }
        public int Index { get; set; }
        public int Offset { get; set; }
        public int Length { get; set; }
        public List<byte> Data { get; set; }
        public byte[] CompressedData { get; set; }
        public bool Edited { get; set; } = false;

        public virtual void Initialize(byte[] decompressedData, int offset)
        {
        }
        public virtual byte[] GetBytes()
        {
            return Data.ToArray();
        }

        public FileInArchive()
        {
        }
    }

    public static class FileManager<T>
        where T : FileInArchive, new()
    {
        public static T FromCompressedData(byte[] compressedData, int offset = 0, uint magicInteger = 0, int index = -1, int length = -1)
        {
            T created = new();
            //try
            //{
            created.MagicInteger = magicInteger;
            created.Index = index;
            created.Length = length;
            created.Location = (-1, -1);
                created.Initialize(Helpers.DecompressData(compressedData), offset);
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine($"Failed to initialize file at offest {offset}: {e.Message}");
            //}
            return created;
        }
    }
}
