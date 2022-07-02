using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HaruhiHeiretsuLib.Graphics
{
    public partial class GraphicsFile
    {
        public byte[] UnknownLayoutHeaderInt1 { get; set; }
        public List<LayoutComponent> LayoutComponents { get; set; }

        public delegate SKBitmap GetLayoutAsync(List<GraphicsFile> archiveGraphicsFiles);

        public SKBitmap GetLayout(List<GraphicsFile> archiveGraphicsFiles)
        {
            if (FileType == GraphicsFileType.LAYOUT)
            {
                var bitmap = new SKBitmap(Width, Height);
                using var canvas = new SKCanvas(bitmap);
                foreach (LayoutComponent layout in LayoutComponents)
                {
                    SKRect boundingBox = new SKRect()
                    {
                        Left = layout.ImageX,
                        Top = layout.ImageY,
                        Right = layout.ImageX + layout.ImageWidth,
                        Bottom = layout.ImageY + layout.ImageHeight,
                    };
                    SKRect destination = new SKRect()
                    {
                        Left = layout.ScreenX,
                        Top = layout.ScreenY,
                        Right = layout.ScreenX + Math.Abs(layout.ScreenWidth),
                        Bottom = layout.ScreenY + Math.Abs(layout.ScreenHeight),
                    };

                    if (layout.Index == -1)
                    {
                        continue;
                    }

                    GraphicsFile grpFile = archiveGraphicsFiles[layout.Index];

                    SKBitmap texture = grpFile.GetImage();
                    SKBitmap tile = new SKBitmap((int)Math.Abs(boundingBox.Right - boundingBox.Left), (int)Math.Abs(boundingBox.Bottom - boundingBox.Top));
                    SKCanvas transformCanvas = new SKCanvas(tile);
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
                            SKColor newColor = new SKColor((byte)(color.Red * layout.RedTint / 128.0), (byte)(color.Green * layout.GreenTint / 128.0), (byte)(color.Blue * layout.BlueTint / 128.0), (byte)(color.Alpha * Math.Min(layout.AlphaTint, (byte)0x80) / 128.0));
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
        public short Index { get; set; }
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

    public static class KnownLayoutGraphicsSets
    {
        public static LayoutGraphic[] TitleScreenGraphics = new LayoutGraphic[10]
        {
            new LayoutGraphic(0x6B, (58, 0)),
            new LayoutGraphic(0x1E, (58, 1)),
            new LayoutGraphic(0x1C, (58, 2)),
            new LayoutGraphic(0x1D, (58, 3)),
            new LayoutGraphic(0x0E, (58, 4)),
            new LayoutGraphic(0x0F, (58, 5)),
            new LayoutGraphic(0x10, (58, 6)),
            new LayoutGraphic(0x11, (58, 7)),
            new LayoutGraphic(0x12, (58, 8)),
            new LayoutGraphic(0x1B, (58, 9))
        };

        public static LayoutGraphic[] SpecialVersionGraphics = new LayoutGraphic[10]
        {
            new LayoutGraphic(0x6B, (69, 0)),
            new LayoutGraphic(0x1E, (69, 1)),
            new LayoutGraphic(0xCE, (69, 2)),
            new LayoutGraphic(0xCB, (69, 3)),
            new LayoutGraphic(0x1C, (69, 4)),
            new LayoutGraphic(0x0E, (69, 5)),
            new LayoutGraphic(0x0F, (69, 6)),
            new LayoutGraphic(0x10, (69, 7)),
            new LayoutGraphic(0x11, (69, 8)),
            new LayoutGraphic(0x12, (69, 9)),
        };
    }

    public struct LayoutGraphic
    {
        public int GrpIndex { get; }
        public (int parent, int child) McbLocation { get; }

        public LayoutGraphic(int grpIndex, (int, int) mcbLoc)
        {
            GrpIndex = grpIndex;
            McbLocation = mcbLoc;
        }
    }
}
