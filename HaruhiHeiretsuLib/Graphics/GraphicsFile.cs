using HaruhiHeiretsuLib.Archive;
using HaruhiHeiretsuLib.Util;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiHeiretsuLib.Graphics
{
    /// <summary>
    /// A representation of a file in grp.bin
    /// </summary>
    public partial class GraphicsFile : FileInArchive
    {
        /// <summary>
        /// The type of file (e.g. texture, SGE model, layout, etc.)
        /// </summary>
        public GraphicsFileType FileType { get; set; }
        /// <summary>
        /// The name of the file as defined by dat #0008
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Blank constructor
        /// </summary>
        public GraphicsFile()
        {
        }

        /// <inheritdoc/>
        public override void Initialize(byte[] decompressedData, int offset)
        {
            Data = [.. decompressedData];
            Offset = offset;
            if (Encoding.ASCII.GetString(Data.Take(6).ToArray()) == "SGE008")
            {
                FileType = GraphicsFileType.SGE;
                Sge = new(decompressedData);
            }
            else if (Data.Take(4).SequenceEqual(new byte[] { 0x00, 0x20, 0xAF, 0x30 }))
            {
                FileType = GraphicsFileType.TEXTURE;
                PointerPointer = IO.ReadInt(decompressedData, 0x08);
                SizePointer = IO.ReadInt(decompressedData, PointerPointer);
                Height = IO.ReadUShort(decompressedData, SizePointer);
                Width = IO.ReadUShort(decompressedData, SizePointer + 2);
                Format = (ImageFormat)IO.ReadInt(decompressedData, SizePointer + 4);
                DataPointer = IO.ReadInt(decompressedData, SizePointer + 8);
            }
            else if (Data.Take(4).SequenceEqual(new byte[] { 0x80, 0x02, 0xE0, 0x01 }))
            {
                FileType = GraphicsFileType.LAYOUT;
                Width = 640;
                Height = 480;
                UnknownLayoutHeaderInt1 = Data.Skip(4).Take(4).ToArray();
                LayoutComponents = [];
                for (int i = 8; i <= Data.Count - 0x1C; i += 0x1C)
                {
                    LayoutComponents.Add(new LayoutComponent
                    {
                        UnknownShort1 = IO.ReadShortLE(decompressedData, i),
                        Index = IO.ReadShortLE(decompressedData, i + 0x02),
                        UnknownShort2 = IO.ReadShortLE(decompressedData, i + 0x04),
                        ScreenX = IO.ReadShortLE(decompressedData, i + 0x06),
                        ScreenY = IO.ReadShortLE(decompressedData, i + 0x08),
                        ImageWidth = IO.ReadShortLE(decompressedData, i + 0x0A),
                        ImageHeight = IO.ReadShortLE(decompressedData, i + 0x0C),
                        ImageX = IO.ReadShortLE(decompressedData, i + 0x0E),
                        ImageY = IO.ReadShortLE(decompressedData, i + 0x10),
                        ScreenWidth = IO.ReadShortLE(decompressedData, i + 0x12),
                        ScreenHeight = IO.ReadShortLE(decompressedData, i + 0x14),
                        UnknownShort3 = IO.ReadShortLE(decompressedData, i + 0x16),
                        AlphaTint = decompressedData[i + 0x1B],
                        RedTint = decompressedData[i + 0x1A],
                        GreenTint = decompressedData[i + 0x19],
                        BlueTint = decompressedData[i + 0x18],
                    });
                }
            }
            else if (Data.Take(4).SequenceEqual(new byte[] { 0x1B, 0x2F, 0x5D, 0xBF }))
            {
                FileType = GraphicsFileType.MAP;
                MapHeader = Data.Take(0xB0).ToArray();
                MapModel = IO.ReadAsciiString(Data, 0xB0);
                MapBackgroundModel = Encoding.ASCII.GetString(Data.Skip(0xC0).TakeWhile(b => b != 0x00).ToArray());
                for (int i = 0; i < 256; i++)
                {
                    MapModelNames.Add(Encoding.ASCII.GetString(Data.Skip(i * 0x10 + 0x100).TakeWhile(b => b != 0x00).ToArray()));
                }
                for (int i = 0; i < 512; i++)
                {
                    int nameIndex = IO.ReadShortLE(decompressedData, i * 0x2C + 0x110E);
                    if (nameIndex > 0)
                    {
                        string name = MapModelNames[nameIndex - 1];
                        MapEntries.Add(new(Data.Skip(i * 0x2C + 0x1100).Take(0x2C), name));
                    }
                    else
                    {
                        MapEntries.Add(new(Data.Skip(i * 0x2C + 0x1100).Take(0x2C)));
                    }

                    MapFooterEntries.Add(Data.Skip(i * 0x18 + 0x6900).Take(0x18).ToArray());
                }
            }
            else
            {
                FileType = GraphicsFileType.UNKNOWN;
            }
        }

        /// <inheritdoc/>
        public override byte[] GetBytes()
        {
            return [.. Data];
        }

        /// <summary>
        /// Attempts to resolve the name of this file (if in the MCB)
        /// </summary>
        /// <param name="offsetIndexDictionary">The MCB's offset-index dictionary</param>
        /// <param name="textureNameDictionary">The texture name dictionary (grp.bin index to name) constructed from dat #0008</param>
        public void TryResolveName(Dictionary<int, int> offsetIndexDictionary, Dictionary<int, string> textureNameDictionary)
        {
            if (textureNameDictionary.TryGetValue(offsetIndexDictionary[McbEntryData.ArchiveOffset], out string name))
            {
                Name = name;
            }
        }

        /// <summary>
        /// Attempts to resolve the name of this file (if it's in grp.bin)
        /// </summary>
        /// <param name="textureNameDictionary">The texture name dictionary (grp.bin index to name) constructed from dat #0008</param>
        public void TryResolveName(Dictionary<int, string> textureNameDictionary)
        {
            if (textureNameDictionary.TryGetValue(BinArchiveIndex, out string name))
            {
                Name = name;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (Location != (-1, -1))
            {
                return $"{McbId:X4}/{Location.parent},{Location.child} {Name} - {FileType}";
            }
            else
            {
                return $"{BinArchiveIndex:X3} {BinArchiveIndex:D4} {Offset:X8} - {FileType}";
            }
        }

        /// <summary>
        /// The type of graphics file this is
        /// </summary>
        public enum GraphicsFileType
        {
            /// <summary>
            /// A character in the font file
            /// </summary>
            FONT_CHARACTER,
            /// <summary>
            /// A layout file
            /// </summary>
            LAYOUT,
            /// <summary>
            /// An SGE (Shade Graphics Engine) 3D model
            /// </summary>
            SGE,
            /// <summary>
            /// A texture file
            /// </summary>
            TEXTURE,
            /// <summary>
            /// A map file (containing level data)
            /// </summary>
            MAP,
            /// <summary>
            /// An unknown file
            /// </summary>
            UNKNOWN
        }
    }
}
