using Kanvas;
using Kontract.Models.Image;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaruhiHeiretsuLib.Graphics
{
    public class GraphicsFile : FileInArchive
    {
        public GraphicsFileType FileType { get; set; }

        // Tile File Properties
        public int Width { get; set; }
        public int Height { get; set; }
        public ImageMode Mode  { get; set; }
        public int PointerPointer { get; set; }
        public int SizePointer { get; set; }
        public int DataPointer { get; set; }

        // Map File Properties
        public byte[] UnknownMapHeaderInt1 { get; set; }
        public List<LayoutComponent> LayoutComponents { get; set; }

        public GraphicsFile()
        {
        }

        public override void Initialize(byte[] decompressedData, int offset)
        {
            Data = decompressedData.ToList();
            Offset = offset;
            if (Encoding.ASCII.GetString(Data.Take(3).ToArray()) == "SGE")
            {
                FileType = GraphicsFileType.SGE;
            }
            else if (Data.Take(4).SequenceEqual(new byte[] { 0x00, 0x20, 0xAF, 0x30 }))
            {
                FileType = GraphicsFileType.TILE_20AF30;
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
                UnknownMapHeaderInt1 = Data.Skip(4).Take(4).ToArray();
                LayoutComponents = new();
                for (int i = 8; i < Data.Count - 0x1C; i += 0x1C)
                {
                    LayoutComponents.Add(new LayoutComponent
                    {
                        UnknownShort1 = BitConverter.ToInt16(Data.Skip(i).Take(2).ToArray()),
                        RelativeFileIndex = BitConverter.ToInt16(Data.Skip(i + 0x02).Take(2).ToArray()),
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
            else
            {
                FileType = GraphicsFileType.UNKNOWN;
            }
        }

        public SKBitmap GetImage()
        {
            if (FileType == GraphicsFileType.TILE_20AF30)
            {
                SKBitmap bitmap = new(Width, Height);

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
                                        byte colorComponent = (byte)((colorByte & 0x0F) * 0x11);
                                        byte alphaComponent = (byte)((colorByte >> 4) * 0x11);
                                        SKColor color = new(colorComponent, colorComponent, colorComponent, alphaComponent);

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
                                        byte colorComponent = colorBytes[1];
                                        byte alphaComponent = colorBytes[0];
                                        SKColor color = new(colorComponent, colorComponent, colorComponent, alphaComponent);

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

                                        SKColor color;
                                        if (colorData >> 15 == 0)
                                        {
                                            color = new((byte)(((colorData >> 8) & 0x0F) * 0x11), (byte)(((colorData >> 4) & 0x0F) * 0x11), (byte)((colorData & 0x0F) * 0x11), (byte)(((colorData >> 12) & 0x07) * 0x20));
                                        }
                                        else
                                        {
                                            color = new((byte)(((colorData >> 10) & 0x1F) * 0x08), (byte)(((colorData >> 5) & 0x1F) * 0x08), (byte)((colorData & 0x1F) * 0x08), 0xFF);
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

                                        SKColor color = new(
                                            Data[index + 1],
                                            Data[index + 32],
                                            Data[index + 33],
                                            Data[index]);

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
                                        SKColor[] palette = new SKColor[4];
                                        for (int i = 0; i < paletteData.Length; i++)
                                        {
                                            palette[i] = new((byte)(((paletteData[i] >> 11) & 0x1F) * 0x08), (byte)(((paletteData[i] >> 5) & 0x3F) * 0x04), (byte)((paletteData[i] & 0x1F) * 0x08), 0xFF);
                                        }
                                        if (paletteData[0] > paletteData[1])
                                        {
                                            palette[2] = new((byte)((palette[0].Red * 2 + palette[1].Red) / 3), (byte)((palette[0].Green * 2 + palette[1].Green) / 3), (byte)((palette[0].Blue * 2 + palette[1].Blue) / 3), 0xFF);
                                            palette[3] = new((byte)((palette[0].Red + palette[1].Red * 2) / 3), (byte)((palette[0].Green + palette[1].Green * 2) / 3), (byte)((palette[0].Blue + palette[1].Blue * 2) / 3), 0xFF);
                                        }
                                        else
                                        {
                                            palette[2] = new((byte)((palette[0].Red + palette[1].Red) / 2), (byte)((palette[0].Green + palette[1].Green) / 2), (byte)((palette[0].Blue + palette[1].Blue) / 2), 0xFF);
                                            palette[3] = SKColors.Transparent;
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
            else if (FileType == GraphicsFileType.FONT_CHARACTER)
            {
                SKBitmap bitmap = new(Width, Height);
                int i = 0;
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        byte alpha = (byte)(((Data[i] & 0xF0) >> 4) * 0x11);
                        byte grayscale = (byte)((Data[i] & 0x0F) * 0x11);
                        bitmap.SetPixel(x, y, new(grayscale, grayscale, grayscale, alpha));
                        i++;
                    }
                }
                SKBitmap transformedBitmap = new(Character.SCALED_WIDTH, Character.SCALED_HEIGHT);
                using SKCanvas canvas = new(transformedBitmap);
                SKRect dest = new(0, 0, transformedBitmap.Width, transformedBitmap.Height);
                canvas.DrawBitmap(bitmap, dest);
                return transformedBitmap;
            }
            return null;
        }

        public SKBitmap GetLayout(List<GraphicsFile> archiveGraphicsFiles)
        {
            if (FileType == GraphicsFileType.LAYOUT)
            {
                SKBitmap bitmap = new(Width, Height);
                using SKCanvas canvas = new(bitmap);
                foreach (LayoutComponent layout in LayoutComponents)
                {
                    SKRect boundingBox = new()
                    {
                        Left = layout.ImageX,
                        Top = layout.ImageY,
                        Right = layout.ImageX + layout.ImageWidth,
                        Bottom = layout.ImageY + layout.ImageHeight,
                    };
                    SKRect destination = new()
                    {
                        Left = layout.ScreenX,
                        Top = layout.ScreenY,
                        Right = layout.ScreenX + layout.ScreenWidth,
                        Bottom = layout.ScreenY + layout.ScreenHeight,
                    };

                    if (layout.RelativeFileIndex == -1)
                    {
                        continue;
                    }

                    int grpIndex = 0;
                    for (int i = 0; i <= layout.RelativeFileIndex && grpIndex < archiveGraphicsFiles.Count; grpIndex++)
                    {
                        if (archiveGraphicsFiles[grpIndex].FileType == GraphicsFileType.TILE_20AF30)
                        {
                            i++;
                        }
                    }
                    GraphicsFile grpFile = new();
                    int index = 0, relativeIndex = 0;
                    while (index < archiveGraphicsFiles.Count)
                    {
                        if (relativeIndex == layout.RelativeFileIndex)
                        {
                            grpFile = archiveGraphicsFiles[index];
                            break;
                        }
                        if (archiveGraphicsFiles[index].FileType == GraphicsFileType.TILE_20AF30)
                        {
                            relativeIndex++;
                        }
                        index++;
                    }

                    SKBitmap texture = grpFile.GetImage();
                    SKBitmap tile = new((int)Math.Abs(boundingBox.Right - boundingBox.Left), (int)Math.Abs(boundingBox.Bottom - boundingBox.Top));
                    SKCanvas transformCanvas = new(tile);
                    if (layout.ScreenWidth < 0)
                    {
                        transformCanvas.Scale(-1, 1, tile.Width / 2.0f, 0);
                    }
                    if (layout.ScreenHeight < 0)
                    {
                        transformCanvas.Scale(1, -1, 0, tile.Height / 2.0f);
                    }
                    transformCanvas.DrawBitmap(texture, boundingBox, new SKRect(0, 0, tile.Width, tile.Height));

                    for (int x = 0; x < tile.Width; x++)
                    {
                        for (int y = 0; y < tile.Height; y++)
                        {
                            SKColor color = tile.GetPixel(x, y);
                            SKColor newColor = new((byte)(color.Red * layout.RedTint / 128.0), (byte)(color.Green * layout.GreenTint / 128.0), (byte)(color.Blue * layout.BlueTint / 128.0), (byte)(color.Alpha * Math.Min(layout.AlphaTint, (byte)0x80) / 128.0));
                            tile.SetPixel(x, y, newColor);
                        }
                    }

                    canvas.DrawBitmap(tile, destination);
                }

                return bitmap;
            }
            else
            {
                return null;
            }
        }

        public void Set20AF30Image(SKBitmap bitmap)
        {
            Edited = true;
            switch (Mode)
            {
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


                                    SKColor color = bitmap.GetPixel(x + col, y + row);

                                    Data[index] = color.Alpha;
                                    Data[index + 1] = color.Red;
                                    Data[index + 32] = color.Green;
                                    Data[index + 33] = color.Blue;
                                }
                            }
                        }
                    }
                    break;
            }
        }

        public void SetFontCharacterImage(string character, SKFont font, int fontSize)
        {
            SKBitmap bitmap = new(Character.SCALED_WIDTH, Character.SCALED_HEIGHT);
            using SKCanvas canvas = new(bitmap);
            SKPaint shadowPaint = new(font) { IsAntialias = true, Color = SKColors.Black };
            SKPaint mainPaint = new(font) { IsAntialias = true, Color = SKColors.White };

            canvas.Clear();
            canvas.DrawText(character, 0, Character.SCALED_HEIGHT - fontSize / 5, shadowPaint);
            canvas.DrawText(character, 2, Character.SCALED_HEIGHT - fontSize / 5, mainPaint);
            canvas.Flush();

            SKBitmap scaledBitmap = new(Width, Height);
            using SKCanvas scaledCanvas = new(scaledBitmap);
            scaledCanvas.DrawBitmap(bitmap, new SKRect(0, 0, Width, Height));
            scaledCanvas.Flush();

            int i = 0;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Data[i] = (byte)(((scaledBitmap.GetPixel(x, y).Alpha / 0x11) << 4) | (scaledBitmap.GetPixel(x, y).Red / 0x11));
                    i++;
                }
            }
        }

        public override byte[] GetBytes()
        {
            return Data.ToArray();
        }

        public override string ToString()
        {
            if (Location != (-1, -1))
            {
                return $"{McbId:X4} ({Location.parent}),{Location.child} (0x{Offset:X8}) - {FileType}";
            }
            else
            {
                return $"{Index:X3} {Index:D4} 0x{Offset:X8} - {FileType}";
            }
        }

        public enum GraphicsFileType
        {
            FONT_CHARACTER,
            LAYOUT,
            SGE,
            TILE_20AF30,
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

    public class LayoutComponent
    {
        public short UnknownShort1 { get; set; }
        public short RelativeFileIndex { get; set; }
        public short UnknownShort2 { get; set; }
        public short ScreenX { get; set; }
        public short ScreenY { get; set; }
        public short ImageWidth { get; set; }
        public short ImageHeight { get; set; }
        public short ImageX { get; set; }
        public short ImageY { get; set; }
        public short ScreenWidth { get; set; }
        public short ScreenHeight { get; set; }
        public short UnknownShort3 { get; set; }
        public byte AlphaTint { get; set; }
        public byte RedTint { get; set; }
        public byte GreenTint { get; set; }
        public byte BlueTint { get; set; }
    }
}
