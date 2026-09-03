using HaruhiHeiretsuLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HaruhiHeiretsuLib.Archive;

/// <summary>
/// Representation of an MCB child archive
/// </summary>
public class McbSubArchive
{
    /// <summary>
    /// The ID of the archive
    /// </summary>
    public ushort Id { get; set; }
    /// <summary>
    /// Unused
    /// </summary>
    public short Padding { get; set; }
    /// <summary>
    /// The offset of the child archive in the MCB parent archive
    /// </summary>
    public int Offset { get; set; }
    /// <summary>
    /// The size of the subarchive
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// A list of files contained in this sub archive
    /// </summary>
    public List<FileInArchive> Files { get; set; } = [];

    /// <summary>
    /// Manually creates an MCB child archive
    /// </summary>
    /// <param name="parentLoc">The location of the parent archive</param>
    /// <param name="id">The ID of the child archive</param>
    /// <param name="padding">Unused</param>
    /// <param name="offset">The offset of the archive</param>
    /// <param name="size">The size of the archive</param>
    /// <param name="data">The binary representation of the archive</param>
    public McbSubArchive(int parentLoc, ushort id, short padding, int offset, int size, byte[] data)
    {
        Id = id;
        Padding = padding;
        Offset = offset;
        Size = size;
        int childLoc = 0;

        for (int i = Offset; i < Offset + Size;)
        {
            int archiveIndex = IO.ReadIntLE(data, i);
            int archiveOffset = IO.ReadIntLE(data, i + 4);
            int compressedSize = IO.ReadIntLE(data, i + 8);

            if (archiveIndex == 0x7FFF)
            {
                break;
            }

            byte[] compressedData = data.Skip(i + 12).Take(compressedSize).ToArray();

            Files.Add(new() { Location = (parentLoc, childLoc++), Offset = i, McbId = Id, McbEntryData = (archiveIndex, archiveOffset), CompressedData = compressedData, Data = [.. Helpers.DecompressData(compressedData)] });

            i += compressedSize + 12;
        }
    }

    /// <summary>
    /// Gets the binary representation of the MCB child archive
    /// </summary>
    /// <returns>A binary representation of the MCB child archive with all its containing files</returns>
    public byte[] GetBytes()
    {
        List<byte> bytes = [];

        foreach (FileInArchive file in Files)
        {
            bytes.AddRange(BitConverter.GetBytes(file.McbEntryData.ArchiveIndex));
            bytes.AddRange(BitConverter.GetBytes(file.McbEntryData.ArchiveOffset));

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

        return [.. bytes];
    }
}