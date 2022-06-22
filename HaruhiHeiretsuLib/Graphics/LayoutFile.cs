using SkiaSharp;
using System;
using System.Collections.Generic;

namespace HaruhiHeiretsuLib.Graphics
{
    public partial class GraphicsFile
    {
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
                        Right = layout.ScreenX + Math.Abs(layout.ScreenWidth),
                        Bottom = layout.ScreenY + Math.Abs(layout.ScreenHeight),
                    };

                    if (layout.RelativeFileIndex == -1)
                    {
                        continue;
                    }

                    int grpIndex = 0;
                    for (int i = 0; i <= layout.RelativeFileIndex && grpIndex < archiveGraphicsFiles.Count; grpIndex++)
                    {
                        if (archiveGraphicsFiles[grpIndex].FileType == GraphicsFileType.TEXTURE)
                        {
                            i++;
                        }
                    }
                    GraphicsFile grpFile = archiveGraphicsFiles[grpIndex];

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
