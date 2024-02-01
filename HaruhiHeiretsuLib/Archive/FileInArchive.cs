using HaruhiHeiretsuLib.Util;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HaruhiHeiretsuLib.Archive
{
    /// <summary>
    /// A representation of a file contained within an archive
    /// </summary>
    public partial class FileInArchive
    {
        /// <summary>
        /// The ID of the file in the MCB (if it is in the MCB)
        /// </summary>
        [JsonIgnore]
        public ushort McbId { get; set; }
        /// <summary>
        /// The location of the file in the MCB (if it is in the MCB)
        /// </summary>
        [JsonIgnore]
        public (int parent, int child) Location { get; set; } = (-1, -1);
        /// <summary>
        /// The data stored in the MCB that determines where this file is located in a bin archive (if it is in the MCB)
        /// </summary>
        [JsonIgnore]
        public (int ArchiveIndex, int ArchiveOffset) McbEntryData { get; set; }
        [JsonIgnore]
        internal uint MagicInteger { get; set; }
        /// <summary>
        /// The file's index in a bin archive (if in a bin archive)
        /// </summary>
        public int BinArchiveIndex { get; set; }
        /// <summary>
        /// The offset of the file in a bin archive (if in a bin archive)
        /// </summary>
        [JsonIgnore]
        public int Offset { get; set; }
        /// <summary>
        /// The length of the file
        /// </summary>
        [JsonIgnore]
        public int Length { get; internal set; }
        /// <summary>
        /// The binary data representation of teh file
        /// </summary>
        [JsonIgnore]
        public List<byte> Data { get; set; }
        /// <summary>
        /// The compressed data of the file in the archive
        /// </summary>
        [JsonIgnore]
        public byte[] CompressedData { get; internal set; }
        /// <summary>
        /// A boolean representing whether the file has been edited or not
        /// </summary>
        [JsonIgnore]
        public bool Edited { get; set; } = false;

        /// <summary>
        /// Initializes this file with its binary data
        /// </summary>
        /// <param name="decompressedData">The decompressed binary data of the file</param>
        /// <param name="offset">The offset of the file in the bin archive</param>
        public virtual void Initialize(byte[] decompressedData, int offset)
        {
            Data = [.. decompressedData];
            Offset = offset;
        }
        /// <summary>
        /// Gets the binary data representation of the file
        /// </summary>
        /// <returns>A byte array containing the binary representation of the file</returns>
        public virtual byte[] GetBytes()
        {
            return [.. Data];
        }

        /// <summary>
        /// Empty constructor
        /// </summary>
        public FileInArchive()
        {
        }

        /// <summary>
        /// Casts this file to a specific subtype of file
        /// </summary>
        /// <typeparam name="T">The subtype of file to cast this file to</typeparam>
        /// <returns>An object representing this file of the specified subtype</returns>
        public T CastTo<T>() where T : FileInArchive, new()
        {
            T newFile = new()
            {
                McbId = McbId,
                Location = Location,
                McbEntryData = McbEntryData,
                MagicInteger = MagicInteger,
                BinArchiveIndex = BinArchiveIndex,
                Offset = Offset,
                Length = Length,
                Data = Data,
                CompressedData = CompressedData,
                Edited = Edited
            };
            newFile.Initialize([.. Data], Offset);

            return newFile;
        }
    }

    /// <summary>
    /// A static class for creating files
    /// </summary>
    /// <typeparam name="T">The type of file to create (must inherit FileInArchive)</typeparam>
    public static class FileManager<T>
        where T : FileInArchive, new()
    {
        /// <summary>
        /// Creates a file from compressed data
        /// </summary>
        /// <param name="compressedData">The file's compressed data</param>
        /// <param name="offset">(Optional) The offset of the file in the archive it comes from</param>
        /// <param name="magicInteger">(Optional) The magic integer of the file</param>
        /// <param name="index">(Optional) The index of the file in the archive</param>
        /// <param name="length">(Optional) The length of the file</param>
        /// <returns>A file object of type T</returns>
        public static T FromCompressedData(byte[] compressedData, int offset = 0, uint magicInteger = 0, int index = -1, int length = -1)
        {
            T created = new()
            {
                MagicInteger = magicInteger,
                BinArchiveIndex = index,
                Length = length,
                Location = (-1, -1)
            };
            created.Initialize(Helpers.DecompressData(compressedData), offset);
            return created;
        }
    }
}
