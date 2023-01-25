using HaruhiHeiretsuLib.Util;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HaruhiHeiretsuLib.Graphics
{
    public partial class GraphicsFile
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public ImageMode Mode { get; set; }
        public int PointerPointer { get; set; }
        public int SizePointer { get; set; }
        public int DataPointer { get; set; }

        public SKBitmap GetImage()
        {
            if (FileType == GraphicsFileType.TEXTURE)
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
                    for (int x = 0; x < Width; x += 2)
                    {
                        byte alpha = (byte)(((Data[i] & 0xF0) >> 4) * 0x11);
                        byte grayscale = (byte)((Data[i] & 0x0F) * 0x11);
                        bitmap.SetPixel(x, y, new(grayscale, grayscale, grayscale, alpha));
                        if (x + 1 < Width)
                        {
                            bitmap.SetPixel(x + 1, y, new(grayscale, grayscale, grayscale, alpha));
                        }
                        i++;
                    }
                }
                return bitmap;
            }
            return null;
        }

        public void Set20AF30Image(SKBitmap bitmap)
        {
            Edited = true;
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

                                    SKColor color = bitmap.GetPixel(x + col, y + row);

                                    byte colorComponent = (byte)(((color.Red / 3) + (color.Blue / 3) + (color.Green / 3)) / 0x11);
                                    byte alphaComponent = (byte)(color.Alpha / 0x11);
                                    Data[ia4Index++] = (byte)((alphaComponent << 4) | colorComponent);
                                }
                            }
                        }
                    }
                    break;

                case ImageMode.IA8:
                    int ia8Index = DataPointer;
                    int ia8HeightMod = (4 - (Height % 4)) == 4 ? 0 : 4 - (Height % 4);
                    for (int y = 0; y < Height + ia8HeightMod; y += 4)
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

                                    SKColor color = bitmap.GetPixel(x + col, y + row);

                                    Data[ia8Index] = color.Alpha;
                                    Data[ia8Index + 1] = (byte)((color.Red / 3) + (color.Blue / 3) + (color.Green / 3));
                                    ia8Index += 2;
                                }
                            }
                        }
                    }
                    break;

                case ImageMode.RGB5A3:
                    int rgb5a3Index = DataPointer;
                    bool useAlpha = bitmap.Pixels.Any(p => p.Alpha < 0xFF); // if the alpha channel is used, we want to set top bit to 0
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

                                    SKColor color = bitmap.GetPixel(x + col, y + row);

                                    if (useAlpha)
                                    {
                                        byte alphaComponent = (byte)(color.Alpha / 0x20);
                                        byte redComponent = (byte)(color.Red / 0x11);
                                        byte greenComponent = (byte)(color.Green / 0x11);
                                        byte blueComponent = (byte)(color.Blue / 0x11);

                                        Data[rgb5a3Index] = (byte)((alphaComponent << 4) | redComponent);
                                        Data[rgb5a3Index + 1] = (byte)((greenComponent << 4) | blueComponent);
                                    }
                                    else
                                    {
                                        byte topBit = 0x80;
                                        byte redComponent = (byte)(color.Red / 0x08);
                                        byte greenComponent = (byte)(color.Green / 0x08);
                                        byte blueComponent = (byte)(color.Blue / 0x08);

                                        Data[rgb5a3Index] = (byte)(topBit | (redComponent << 2) | (greenComponent >> 3));
                                        Data[rgb5a3Index + 1] = (byte)((greenComponent & 0x07) << 5 | blueComponent);
                                    }

                                    rgb5a3Index += 2;
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

                                    List<SKColor> texel = new();
                                    for (int tY = 0; tY < 4; tY++)
                                    {
                                        for (int tX = 0; tX < 4; tX++)
                                        {
                                            texel.Add(bitmap.GetPixel(x + col + tX, y + row + tY));
                                        }
                                    }

                                    SKColor[] palette = new SKColor[4];
                                    palette[0] = new(texel.Max(p => p.Red), texel.Max(p => p.Green), texel.Max(p => p.Blue), 0xFF);
                                    palette[1] = new(texel.Min(p => p.Red), texel.Min(p => p.Green), texel.Min(p => p.Blue), 0xFF);

                                    if (texel.Any(p => p.Alpha < 0x80)) // arbitrary alpha clipping cutoff
                                    {
                                        (palette[1], palette[0]) = (palette[0], palette[1]);
                                        palette[2] = new((byte)((palette[0].Red + palette[1].Red) / 2), (byte)((palette[0].Green + palette[1].Green) / 2), (byte)((palette[0].Blue + palette[1].Blue) / 2), 0xFF);
                                        palette[3] = SKColors.Transparent;
                                    }
                                    else
                                    {
                                        palette[2] = new((byte)((palette[0].Red * 2 + palette[1].Red) / 3), (byte)((palette[0].Green * 2 + palette[1].Green) / 3), (byte)((palette[0].Blue * 2 + palette[1].Blue) / 3), 0xFF);
                                        palette[3] = new((byte)((palette[0].Red + palette[1].Red * 2) / 3), (byte)((palette[0].Green + palette[1].Green * 2) / 3), (byte)((palette[0].Blue + palette[1].Blue * 2) / 3), 0xFF);
                                    }

                                    ushort[] paletteData = new ushort[2];
                                    for (int i = 0; i < paletteData.Length; i++)
                                    {
                                        paletteData[i] = (ushort)(((palette[i].Red / 0x08) << 11) | ((palette[i].Green / 0x04) << 5) | ((palette[i].Blue / 0x08)));
                                    }

                                    byte[] paletteDataBytes = paletteData.SelectMany(s => BitConverter.GetBytes(s).Reverse()).ToArray();
                                    Data[cmprIndex] = paletteDataBytes[0];
                                    Data[cmprIndex + 1] = paletteDataBytes[1];
                                    Data[cmprIndex + 2] = paletteDataBytes[2];
                                    Data[cmprIndex + 3] = paletteDataBytes[3];
                                    cmprIndex += 4;

                                    for (int subRow = 0; subRow < 4; subRow++)
                                    {
                                        Data[cmprIndex++] =
                                            (byte)((Helpers.ClosestColorIndex(palette.ToList(), texel[subRow * 4]) << 6)
                                            | (Helpers.ClosestColorIndex(palette.ToList(), texel[subRow * 4 + 1]) << 4)
                                            | (Helpers.ClosestColorIndex(palette.ToList(), texel[subRow * 4 + 2]) << 2)
                                            | Helpers.ClosestColorIndex(palette.ToList(), texel[subRow * 4 + 3]));
                                    }
                                }
                            }
                        }
                    }
                    break;
            }
        }

        public void SetFontCharacterImage(string character, SKFont font, float fontSize, int verticalOffset = 0)
        {
            SKBitmap bitmap = new(Character.SCALED_WIDTH, Character.SCALED_HEIGHT);
            using SKCanvas canvas = new(bitmap);
            SKPaint shadowPaint = new(font) { IsAntialias = true, Color = SKColors.Black, FilterQuality = SKFilterQuality.High };
            SKPaint mainPaint = new(font) { IsAntialias = true, Color = SKColors.White, FilterQuality = SKFilterQuality.High };
            font.Edging = SKFontEdging.SubpixelAntialias;

            canvas.Clear();
            canvas.DrawText(character, 0, Character.SCALED_HEIGHT - fontSize / 5 + verticalOffset - 3, font, shadowPaint);
            canvas.DrawText(character, 1, Character.SCALED_HEIGHT - fontSize / 5 + verticalOffset - 3, font, mainPaint);
            canvas.Flush();

            Data = Data.Select(b => (byte)0).ToList();

            int i = 0;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x += 2)
                {
                    if (x + 1 < Width)
                    {
                        Data[i] = (byte)(((bitmap.GetPixel(x, y).Alpha / 0x11) << 4) | (bitmap.GetPixel(x, y).Red / 0x11)
                            + ((bitmap.GetPixel(x + 1, y).Alpha / 0x11) << 4) | (bitmap.GetPixel(x, y).Red / 0x11)
                            / 2);
                    }
                    else
                    {
                        Data[i] = (byte)(((bitmap.GetPixel(x, y).Alpha / 0x11) << 4) | (bitmap.GetPixel(x, y).Red / 0x11));
                    }
                    i++;
                }
            }
        }
    }
}
