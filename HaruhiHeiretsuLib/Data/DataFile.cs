using HaruhiHeiretsuLib.Archive;
using System.Linq;

namespace HaruhiHeiretsuLib.Data
{
    public class DataFile : FileInArchive
    {
        public string Name { get; set; }

        public override void Initialize(byte[] decompressedData, int offset)
        {
            Offset = offset;
            Data = decompressedData.ToList();
        }

        public override byte[] GetBytes() => Data.ToArray();

        public override string ToString()
        {
            return $"{Index:X3} 0x{Offset:X8} {Name}";
        }
    }
}
