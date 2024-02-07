using HaruhiHeiretsuLib.Archive;

namespace HaruhiHeiretsuLib.Data
{
    /// <summary>
    /// A representation of a file in dat.bin
    /// </summary>
    public class DataFile : FileInArchive
    {
        /// <summary>
        /// The name of this data file (for descriptive purposes)
        /// </summary>
        public string Name { get; set; }

        /// <inheritdoc/>
        public override void Initialize(byte[] decompressedData, int offset)
        {
            Offset = offset;
            Data = [.. decompressedData];
        }

        /// <inheritdoc/>
        public override byte[] GetBytes() => Data.ToArray();

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{BinArchiveIndex:X3} 0x{Offset:X8} {Name}";
        }
    }
}
