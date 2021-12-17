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
        public (int parent, int child) Location { get; set; } = (-1, -1);

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
        public List<MapComponent> MapComponents { get; set; }

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
                FileType = GraphicsFileType.MAP;
                Width = 640;
                Height = 480;
                UnknownMapHeaderInt1 = Data.Skip(4).Take(4).ToArray();
                MapComponents = new();
                for (int i = 8; i < Data.Count; i += 0x1C)
                {
                    MapComponents.Add(new MapComponent
                    {
                        UnknownShort1 = BitConverter.ToInt16(Data.Skip(i).Take(2).ToArray()),
                        FileIndex = BitConverter.ToInt16(Data.Skip(i + 0x02).Take(2).ToArray()),
                        UnknownShort2 = BitConverter.ToInt16(Data.Skip(i + 0x04).Take(2).ToArray()),
                        CanvasX = BitConverter.ToInt16(Data.Skip(i + 0x06).Take(2).ToArray()),
                        CanvasY = BitConverter.ToInt16(Data.Skip(i + 0x08).Take(2).ToArray()),
                        ImageWidth = BitConverter.ToInt16(Data.Skip(i + 0x0A).Take(2).ToArray()),
                        ImageHeight = BitConverter.ToInt16(Data.Skip(i + 0x0C).Take(2).ToArray()),
                        ImageX = BitConverter.ToInt16(Data.Skip(i + 0x0E).Take(2).ToArray()),
                        ImageY = BitConverter.ToInt16(Data.Skip(i + 0x10).Take(2).ToArray()),
                        CanvasWidth = BitConverter.ToInt16(Data.Skip(i + 0x12).Take(2).ToArray()),
                        CanvasHeight = BitConverter.ToInt16(Data.Skip(i + 0x14).Take(2).ToArray()),
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

        public Bitmap GetImage()
        {
            if (FileType == GraphicsFileType.TILE_20AF30)
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
            else if (FileType == GraphicsFileType.FONT_CHARACTER)
            {
                Bitmap bitmap = new(Width, Height);
                int i = 0;
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        int alpha = ((Data[i] & 0xF0) >> 4) * 0x11;
                        int grayscale = (Data[i] & 0x0F) * 0x11;
                        bitmap.SetPixel(x, y, Color.FromArgb(alpha, grayscale, grayscale, grayscale));
                        i++;
                    }
                }
                Bitmap transformedBitmap = new(Character.SCALED_WIDTH, Character.SCALED_HEIGHT);
                using Graphics graphics = Graphics.FromImage(transformedBitmap);
                graphics.DrawImage(bitmap, 0, 0, Character.SCALED_WIDTH, Character.SCALED_HEIGHT);
                graphics.Flush();
                return transformedBitmap;
            }
            return null;
        }

        public Bitmap GetMap(List<GraphicsFile> archiveGraphicsFiles)
        {
            if (FileType == GraphicsFileType.MAP)
            {
                Bitmap bitmap = new(Width, Height);
                using Graphics graphics = Graphics.FromImage(bitmap);
                foreach (MapComponent map in MapComponents)
                {
                    Rectangle boundingBox = new Rectangle
                    {
                        X = map.ImageX,
                        Y = map.ImageY,
                        Width = map.ImageWidth,
                        Height = map.ImageHeight,
                    };
                    try
                    {
                        if (map.FileIndex == -1)
                        {
                            continue;
                        }
                        Bitmap tile = archiveGraphicsFiles[map.FileIndex].GetImage().Clone(boundingBox, System.Drawing.Imaging.PixelFormat.DontCare);

                        for (int x = 0; x < tile.Width; x++)
                        {
                            for (int y = 0; y < tile.Height; y++)
                            {
                                Color color = tile.GetPixel(x, y);
                                Color newColor = Color.FromArgb((int)(color.A * Math.Min(map.AlphaTint, (byte)0x80) / 128.0), (int)(color.R * map.RedTint / 128.0), (int)(color.G * map.GreenTint / 128.0), (int)(color.B * map.BlueTint / 128.0));
                                tile.SetPixel(x, y, newColor);
                            }
                        }

                        graphics.DrawImage(tile, new Rectangle
                        {
                            X = map.CanvasX,
                            Y = map.CanvasY,
                            Width = map.CanvasWidth,
                            Height = map.CanvasHeight
                        });
                    }
                    catch (OutOfMemoryException)
                    {
                        // do nothing
                    }
                }

                return bitmap;
            }
            else
            {
                return null;
            }
        }

        public void Set20AF30Image(Bitmap bitmap)
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


                                    Color color = bitmap.GetPixel(x + col, y + row);

                                    Data[index] = color.A;
                                    Data[index + 1] = color.R;
                                    Data[index + 32] = color.G;
                                    Data[index + 33] = color.B;
                                }
                            }
                        }
                    }
                    break;
            }
        }

        public void SetFontCharacterImage(string character, FontFamily font, int fontSize)
        {
            Bitmap bitmap = new(Character.SCALED_WIDTH, Character.SCALED_HEIGHT);
            Rectangle rectangle = new Rectangle(0, 0, Character.SCALED_WIDTH, Character.SCALED_HEIGHT);
            using Graphics graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

            StringFormat format = new()
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center
            };

            graphics.FillRectangle(Brushes.Transparent, rectangle);
            graphics.DrawString(character, new Font(font, fontSize), Brushes.White, rectangle, format);
            graphics.TranslateTransform(-5f, 0);
            graphics.Flush();

            Bitmap scaledBitmap = new(Width, Height);
            using Graphics scaledGraphics = Graphics.FromImage(scaledBitmap);
            scaledGraphics.DrawImage(bitmap, 0, 0, Width, Height);
            scaledGraphics.Flush();

            int i = 0;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Data[i] = (byte)(((scaledBitmap.GetPixel(x, y).A / 0x11) << 4) | (scaledBitmap.GetPixel(x, y).R / 0x11));
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
                return $"{Location.parent},{Location.child} - {FileType}";
            }
            else
            {
                return $"{Index:X3} {Index:D4} 0x{Offset:X8} - {FileType}";
            }
        }

        public enum GraphicsFileType
        {
            FONT_CHARACTER,
            MAP,
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

    public class MapComponent
    {
        public short UnknownShort1 { get; set; }
        public short FileIndex { get; set; }
        public short UnknownShort2 { get; set; }
        public short CanvasX { get; set; }
        public short CanvasY { get; set; }
        public short ImageWidth { get; set; }
        public short ImageHeight { get; set; }
        public short ImageX { get; set; }
        public short ImageY { get; set; }
        public short CanvasWidth { get; set; }
        public short CanvasHeight { get; set; }
        public short UnknownShort3 { get; set; }
        public byte AlphaTint { get; set; }
        public byte RedTint { get; set; }
        public byte GreenTint { get; set; }
        public byte BlueTint { get; set; }
    }
}
