using Kanvas;
using Kontract.Models.Image;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaruhiHeiretsuLib
{
    public class GraphicsFile : FileInArchive
    {
        public GraphicsFileType FileType { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public ImageMode Mode  { get; set; }

        public int PointerPointer { get; set; }
        public int SizePointer { get; set; }
        public int DataPointer { get; set; }

        public GraphicsFile()
        {
        }

        public override void Initialize(byte[] compressedData, int offset)
        {
            Data = compressedData.ToList();
            Offset = offset;
            if (Encoding.ASCII.GetString(Data.Take(3).ToArray()) == "SGE")
            {
                FileType = GraphicsFileType.SGE;
            }
            if (Data[0] == 0x00 && Data[1] == 0x20 && Data[2] == 0xAF && Data[3] == 0x30)
            {
                FileType = GraphicsFileType.TYPE_20AF30;
                PointerPointer = BitConverter.ToInt32(Data.Skip(0x08).Take(4).Reverse().ToArray());
                SizePointer = BitConverter.ToInt32(Data.Skip(PointerPointer).Take(4).Reverse().ToArray());
                Height = BitConverter.ToInt16(Data.Skip(SizePointer).Take(2).Reverse().ToArray());
                Width = BitConverter.ToInt16(Data.Skip(SizePointer + 2).Take(2).Reverse().ToArray());
                Mode = (ImageMode)BitConverter.ToInt32(Data.Skip(SizePointer + 4).Take(4).Reverse().ToArray());
                DataPointer = BitConverter.ToInt32(Data.Skip(SizePointer + 8).Take(4).Reverse().ToArray());
            }
            else
            {
                FileType = GraphicsFileType.UNKNOWN;
            }
        }

        public Bitmap GetImage()
        {
            if (FileType == GraphicsFileType.TYPE_20AF30)
            {
                Bitmap bitmap = new(Width, Height);

                switch (Mode)
                {
                    case ImageMode.IA4:
                        int ia4Index = DataPointer;
                        for (int y = 0; y < Height; y += 4)
                        {
                            int widthMod = (8 - (Width % 8)) == 8 ? 0 : 8 - (Width % 8);
                            for (int x = 0; x < Width + widthMod; x += 8)
                            {
                                for (int row = 0; row < 4; row++)
                                {
                                    for (int col = 0; col < 8; col++)
                                    {
                                        if (ia4Index >= Data.Count || x + col >= Width || y + row >= Height)
                                        {
                                            ia4Index++;
                                            continue;
                                        }

                                        byte colorByte = Data[ia4Index++];
                                        int colorComponent = (colorByte & 0x0F) * 0x11;
                                        int alphaComponent = (colorByte >> 4) * 0x11;
                                        Color color = Color.FromArgb(alphaComponent, colorComponent, colorComponent, colorComponent);

                                        bitmap.SetPixel(x + col, y + row, color);
                                    }
                                }
                            }
                        }
                        break;

                    case ImageMode.IA8:
                        int ia8Index = DataPointer;
                        for (int y = 0; y < Height; y += 4)
                        {
                            int widthMod = (4 - (Width % 4)) == 4 ? 0 : 4 - (Width % 4);
                            for (int x = 0; x < Width + widthMod; x += 4)
                            {
                                for (int row = 0; row < 4; row++)
                                {
                                    for (int col = 0; col < 4; col++)
                                    {
                                        if (ia8Index + 1 >= Data.Count || x + col >= Width || y + row >= Height)
                                        {
                                            ia8Index += 2;
                                            continue;
                                        }

                                        byte[] colorBytes = Data.Skip(ia8Index).Take(2).ToArray();
                                        ia8Index += 2;
                                        int colorComponent = colorBytes[1];
                                        int alphaComponent = colorBytes[0];
                                        Color color = Color.FromArgb(alphaComponent, colorComponent, colorComponent, colorComponent);

                                        bitmap.SetPixel(x + col, y + row, color);
                                    }
                                }
                            }
                        }
                        break;

                    case ImageMode.RGB5A3:
                        int rgb5a3Index = DataPointer;
                        for (int y = 0; y < Height; y += 4)
                        {
                            int widthMod = (4 - (Width % 4)) == 4 ? 0 : 4 - (Width % 4);
                            for (int x = 0; x < Width + widthMod; x += 4)
                            {
                                for (int row = 0; row < 4; row++)
                                {
                                    for (int col = 0; col < 4; col++)
                                    {
                                        if (rgb5a3Index + 1 >= Data.Count || x + col >= Width || y + row >= Height)
                                        {
                                            rgb5a3Index += 2;
                                            continue;
                                        }
                                        ushort colorData = BitConverter.ToUInt16(Data.Skip(rgb5a3Index).Take(2).Reverse().ToArray());
                                        rgb5a3Index += 2;

                                        Color color;
                                        if (colorData >> 15 == 0)
                                        {
                                            color = Color.FromArgb(((colorData >> 12) & 0x07) * 0x20, ((colorData >> 8) & 0x0F) * 0x11, ((colorData >> 4) & 0x0F) * 0x11, (colorData & 0x0F) * 0x11);
                                        }
                                        else
                                        {
                                            color = Color.FromArgb(0xFF, ((colorData >> 10) & 0x1F) * 0x08, ((colorData >> 5) & 0x1F) * 0x08, (colorData & 0x1F) * 0x08);
                                        }

                                        bitmap.SetPixel(x + col, y + row, color);
                                    }
                                }
                            }
                        }
                        break;
                    case ImageMode.RGBA8:
                        int rgba8HeightMod = (4 - (Height % 4)) == 4 ? 0 : 4 - (Height % 4);
                        for (int y = 0; y < Height + rgba8HeightMod; y += 4)
                        {
                            int widthMod = (4 - (Width % 4)) == 4 ? 0 : 4 - (Width % 4);
                            for (int x = 0; x < Width + widthMod; x += 4)
                            {
                                for (int row = 0; row < 4; row++)
                                {
                                    for (int col = 0; col < 4; col++)
                                    {
                                        int index = y * (Width + widthMod) * 4 + x * 16 + col * 2 + row * 8 + DataPointer;
                                        if (index + 33 >= Data.Count || x + col >= Width || y + row >= Height)
                                        {
                                            continue;
                                        }

                                        Color color = Color.FromArgb(
                                            Data[index],
                                            Data[index + 1],
                                            Data[index + 32],
                                            Data[index + 33]);

                                        bitmap.SetPixel(x + col, y + row, color);
                                    }
                                }
                            }
                        }
                        break;

                    case ImageMode.CMPR:
                        int cmprIndex = DataPointer;
                        for (int y = 0; y < Height; y += 8)
                        {
                            int widthMod = (8 - (Width % 8)) == 8 ? 0 : 8 - (Width % 8);
                            for (int x = 0; x < Width + widthMod; x += 8)
                            {
                                // 8x8 bytes, 4x4 sub-blocks
                                for (int row = 0; row < 8; row += 4)
                                {
                                    for (int col = 0; col < 8; col += 4)
                                    {
                                        if (cmprIndex >= Data.Count)
                                        {
                                            break;
                                        }
                                        ushort[] paletteData = new ushort[]
                                        { 
                                            BitConverter.ToUInt16(Data.Skip(cmprIndex).Take(2).Reverse().ToArray()),
                                            BitConverter.ToUInt16(Data.Skip(cmprIndex + 2).Take(2).Reverse().ToArray())
                                        };
                                        Color[] palette = new Color[4];
                                        for (int i = 0; i < paletteData.Length; i++)
                                        {
                                            palette[i] = Color.FromArgb(0xFF, ((paletteData[i] >> 11) & 0x1F) * 0x08, ((paletteData[i] >> 5) & 0x3F) * 0x04, (paletteData[i] & 0x1F) * 0x08);
                                        }
                                        if (paletteData[0] > paletteData[1])
                                        {
                                            palette[2] = Color.FromArgb(0xFF, (palette[0].R * 2 + palette[1].R) / 3, (palette[0].G * 2 + palette[1].G) / 3, (palette[0].B * 2 + palette[1].B) / 3);
                                            palette[3] = Color.FromArgb(0xFF, (palette[0].R + palette[1].R * 2) / 3, (palette[0].G + palette[1].G * 2) / 3, (palette[0].B + palette[1].B * 2) / 3);
                                        }
                                        else
                                        {
                                            palette[2] = Color.FromArgb(0xFF, (palette[0].R + palette[1].R) / 2, (palette[0].G + palette[1].G) / 2, (palette[0].B + palette[1].B) / 2);
                                            palette[3] = Color.Transparent;
                                        }
                                        cmprIndex += 4;
                                        for (int subRow = 0; subRow < 4; subRow++)
                                        {
                                            byte pixelData = Data[cmprIndex++];
                                            for (int subCol = 0; subCol < 4; subCol++)
                                            {
                                                if (x + col + subCol >= Width || y + row + subRow >= Height)
                                                {
                                                    continue;
                                                }
                                                bitmap.SetPixel(x + col + subCol, y + row + subRow, palette[(pixelData >> (2 * (3 - subCol))) & 0x03]);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                }
                return bitmap;
            }
            return null;
        }

        public override string ToString()
        {
            return $"{Index:X3} {Index:D4} 0x{Offset:X8}";
        }

        public enum GraphicsFileType
        {
            SGE,
            TYPE_20AF30,
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
