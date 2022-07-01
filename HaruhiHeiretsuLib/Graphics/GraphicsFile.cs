using HaruhiHeiretsuLib.Archive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiHeiretsuLib.Graphics
{
    public partial class GraphicsFile : FileInArchive
    {
        public GraphicsFileType FileType { get; set; }
        public string Name { get; set; } = string.Empty;

        // SGE Properties

        public GraphicsFile()
        {
        }

        public override void Initialize(byte[] decompressedData, int offset)
        {
            Data = decompressedData.ToList();
            Offset = offset;
            if (Encoding.ASCII.GetString(Data.Take(6).ToArray()) == "SGE008")
            {
                FileType = GraphicsFileType.SGE;
                Sge = new(Data, Location);
            }
            else if (Data.Take(4).SequenceEqual(new byte[] { 0x00, 0x20, 0xAF, 0x30 }))
            {
                FileType = GraphicsFileType.TEXTURE;
                PointerPointer = BitConverter.ToInt32(Data.Skip(0x08).Take(4).Reverse().ToArray());
                SizePointer = BitConverter.ToInt32(Data.Skip(PointerPointer).Take(4).Reverse().ToArray());
                Height = BitConverter.ToUInt16(Data.Skip(SizePointer).Take(2).Reverse().ToArray());
                Width = BitConverter.ToUInt16(Data.Skip(SizePointer + 2).Take(2).Reverse().ToArray());
                Mode = (ImageMode)BitConverter.ToInt32(Data.Skip(SizePointer + 4).Take(4).Reverse().ToArray());
                DataPointer = BitConverter.ToInt32(Data.Skip(SizePointer + 8).Take(4).Reverse().ToArray());
            }
            else if (Data.Take(4).SequenceEqual(new byte[] { 0x80, 0x02, 0xE0, 0x01 }))
            {
                FileType = GraphicsFileType.LAYOUT;
                Width = 640;
                Height = 480;
                UnknownLayoutHeaderInt1 = Data.Skip(4).Take(4).ToArray();
                LayoutComponents = new();
                for (int i = 8; i < Data.Count - 0x1C; i += 0x1C)
                {
                    LayoutComponents.Add(new LayoutComponent
                    {
                        UnknownShort1 = BitConverter.ToInt16(Data.Skip(i).Take(2).ToArray()),
                        Index = BitConverter.ToInt16(Data.Skip(i + 0x02).Take(2).ToArray()),
                        UnknownShort2 = BitConverter.ToInt16(Data.Skip(i + 0x04).Take(2).ToArray()),
                        ScreenX = BitConverter.ToInt16(Data.Skip(i + 0x06).Take(2).ToArray()),
                        ScreenY = BitConverter.ToInt16(Data.Skip(i + 0x08).Take(2).ToArray()),
                        ImageWidth = BitConverter.ToInt16(Data.Skip(i + 0x0A).Take(2).ToArray()),
                        ImageHeight = BitConverter.ToInt16(Data.Skip(i + 0x0C).Take(2).ToArray()),
                        ImageX = BitConverter.ToInt16(Data.Skip(i + 0x0E).Take(2).ToArray()),
                        ImageY = BitConverter.ToInt16(Data.Skip(i + 0x10).Take(2).ToArray()),
                        ScreenWidth = BitConverter.ToInt16(Data.Skip(i + 0x12).Take(2).ToArray()),
                        ScreenHeight = BitConverter.ToInt16(Data.Skip(i + 0x14).Take(2).ToArray()),
                        UnknownShort3 = BitConverter.ToInt16(Data.Skip(i + 0x16).Take(2).ToArray()),
                        AlphaTint = Data[i + 0x1B],
                        RedTint = Data[i + 0x1A],
                        GreenTint = Data[i + 0x19],
                        BlueTint = Data[i + 0x18],
                    });
                }
            }
            else if (Data.Take(4).SequenceEqual(new byte[] { 0x1B, 0x2F, 0x5D, 0xBF }))
            {
                FileType = GraphicsFileType.MAP;
                MapHeader = Data.Take(0xB0).ToArray();
                MapModel = Encoding.ASCII.GetString(Data.Skip(0xB0).TakeWhile(b => b != 0x00).ToArray());
                MapBackgroundModel = Encoding.ASCII.GetString(Data.Skip(0xC0).TakeWhile(b => b != 0x00).ToArray());
                for (int i = 0; i < 256; i++)
                {
                    MapModelNames.Add(Encoding.ASCII.GetString(Data.Skip(i * 0x10 + 0x100).TakeWhile(b => b != 0x00).ToArray()));
                }
                for (int i = 0; i < 512; i++)
                {
                    int nameIndex = BitConverter.ToInt16(Data.Skip(i * 0x2C + 0x110E).Take(2).ToArray());
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

        public override byte[] GetBytes()
        {
            return Data.ToArray();
        }

        public void TryResolveName(Dictionary<int, int> offsetIndexDictionary, Dictionary<int, string> textureNameDictionary)
        {
            if (textureNameDictionary.TryGetValue(offsetIndexDictionary[McbEntryData.archiveOffset], out string name))
            {
                Name = name;
            }
        }

        public void TryResolveName(Dictionary<int, string> textureNameDictionary)
        {
            if (textureNameDictionary.TryGetValue(Index, out string name))
            {
                Name = name;
            }
        }

        public override string ToString()
        {
            if (Location != (-1, -1))
            {
                return $"{McbId:X4}/{Location.parent},{Location.child} {Name} - {FileType}";
            }
            else
            {
                return $"{Index:X3} {Index:D4} {Offset:X8} - {FileType}";
            }
        }

        public enum GraphicsFileType
        {
            FONT_CHARACTER,
            LAYOUT,
            SGE,
            TEXTURE,
            MAP,
            UNKNOWN
        }

        public enum ImageMode
        {
            I4 = 0x00,
            I8 = 0x01,
            IA4 = 0x02,
            IA8 = 0x03,
            RGB565 = 0x04,
            RGB5A3 = 0x05,
            RGBA8 = 0x06,
            CI4 = 0x08,
            CI8 = 0x09,
            CI14X2 = 0x0A,
            CMPR = 0x0E,
        }
    }
}
