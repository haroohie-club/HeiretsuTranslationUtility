using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace HaruhiHeiretsuLib.Graphics;

public partial class GraphicsFile
{
    /// <summary>
    /// Unknown
    /// </summary>
    public byte[] UnknownLayoutHeaderInt1 { get; set; }
    /// <summary>
    /// The entries in the layout
    /// </summary>
    public List<LayoutComponent> LayoutComponents { get; set; }

    /// <summary>
    /// Asynchronous layout preview getter
    /// </summary>
    public delegate SKBitmap GetLayoutAsync(List<GraphicsFile> archiveGraphicsFiles);

    /// <summary>
    /// Gets a layout preview given a list of graphics files the layout contains
    /// </summary>
    /// <param name="archiveGraphicsFiles">The list of graphics files the layout contains</param>
    /// <returns>A bitmap preview of the layout</returns>
    public SKBitmap GetLayout(List<GraphicsFile> archiveGraphicsFiles)
    {
        if (FileType == GraphicsFileType.LAYOUT)
        {
            SKBitmap bitmap = new(Width, Height);
            using SKCanvas canvas = new(bitmap);
            int i = 0;
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

                if (layout.Index == -1 || layout.Index >= archiveGraphicsFiles.Count)
                {
                    continue;
                }

                GraphicsFile grpFile = archiveGraphicsFiles[layout.Index];

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
                canvas.DrawText($"{i} ({layout.Index})", new SKPoint(layout.ScreenX, layout.ScreenY), new(SKTypeface.FromFamilyName("Arial"), 14f) { Embolden = true}, new());

                i++;
            }

            return bitmap;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Sets layout binary data in the ROM
    /// </summary>
    public void SetLayoutData()
    {
        Edited = true;

        List<byte> bytes =
        [
            .. Data.Take(4),
            .. UnknownLayoutHeaderInt1,
            .. LayoutComponents.SelectMany(l => l.GetBytes()),
        ];

        Data = bytes;
    }

    /// <summary>
    /// Gets the serialized JSON representation of the layout
    /// </summary>
    /// <returns></returns>
    public string GetLayoutJson()
    {
        return JsonSerializer.Serialize(this);
    }

    /// <summary>
    /// Imports layout JSON into the layout for setting it
    /// </summary>
    /// <param name="json">JSON string content</param>
    public void ImportLayoutJson(string json)
    {
        GraphicsFile file = JsonSerializer.Deserialize<GraphicsFile>(json);
        UnknownLayoutHeaderInt1 = file.UnknownLayoutHeaderInt1;
        LayoutComponents = file.LayoutComponents;
    }
}

/// <summary>
/// A layout component
/// </summary>
public class LayoutComponent
{
    /// <summary>
    /// Unknown
    /// </summary>
    public short UnknownShort1 { get; set; }
    /// <summary>
    /// Index of the graphic this component uses
    /// </summary>
    public short Index { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public short UnknownShort2 { get; set; }
    /// <summary>
    /// Screen X-position of the layout component
    /// </summary>
    public short ScreenX { get; set; }
    /// <summary>
    /// Screen Y-position of the layout component
    /// </summary>
    public short ScreenY { get; set; }
    /// <summary>
    /// Texture width of the crop
    /// </summary>
    public short ImageWidth { get; set; }
    /// <summary>
    /// Texture height of the crop
    /// </summary>
    public short ImageHeight { get; set; }
    /// <summary>
    /// Texture X-position
    /// </summary>
    public short ImageX { get; set; }
    /// <summary>
    /// Texture Y-position
    /// </summary>
    public short ImageY { get; set; }
    /// <summary>
    /// Width of the component on the screen
    /// </summary>
    public short ScreenWidth { get; set; }
    /// <summary>
    /// Height of the component on the screen
    /// </summary>
    public short ScreenHeight { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public short UnknownShort3 { get; set; }
    /// <summary>
    /// Alpha component of tint
    /// </summary>
    public byte AlphaTint { get; set; }
    /// <summary>
    /// Red component of tint
    /// </summary>
    public byte RedTint { get; set; }
    /// <summary>
    /// Green component of tint
    /// </summary>
    public byte GreenTint { get; set; }
    /// <summary>
    /// Blue component of tint
    /// </summary>
    public byte BlueTint { get; set; }

    /// <summary>
    /// Empty constructor for serialization
    /// </summary>
    public LayoutComponent()
    {
    }

    /// <summary>
    /// Gets binary representation of the layout component
    /// </summary>
    /// <returns>List of bytes for the layout</returns>
    public List<byte> GetBytes()
    {
        List<byte> bytes =
        [
            .. BitConverter.GetBytes(UnknownShort1),
            .. BitConverter.GetBytes(Index),
            .. BitConverter.GetBytes(UnknownShort2),
            .. BitConverter.GetBytes(ScreenX),
            .. BitConverter.GetBytes(ScreenY),
            .. BitConverter.GetBytes(ImageWidth),
            .. BitConverter.GetBytes(ImageHeight),
            .. BitConverter.GetBytes(ImageX),
            .. BitConverter.GetBytes(ImageY),
            .. BitConverter.GetBytes(ScreenWidth),
            .. BitConverter.GetBytes(ScreenHeight),
            .. BitConverter.GetBytes(UnknownShort3), BlueTint, GreenTint, RedTint, AlphaTint,
        ];

        return bytes;
    }
}

/// <summary>
/// Set of known hardcoded layout graphic sets
/// </summary>
public static class KnownLayoutGraphicsSets
{
    /// <summary>
    /// Used by the title screen layout
    /// </summary>
    public static LayoutGraphic[] TitleScreenGraphics =
    [
        new(0x6B, (58, 0)),
        new(0x1E, (58, 1)),
        new(0x1C, (58, 2)),
        new(0x1D, (58, 3)),
        new(0x0E, (58, 4)),
        new(0x0F, (58, 5)),
        new(0x10, (58, 6)),
        new(0x11, (58, 7)),
        new(0x12, (58, 8)),
        new(0x1B, (58, 9)),
    ];

    /// <summary>
    /// Used by the special version title screen graphics
    /// </summary>
    public static LayoutGraphic[] SpecialVersionGraphics =
    [
        new(0x6B, (69, 0)),
        new(0x1E, (69, 1)),
        new(0xCE, (69, 2)),
        new(0xCB, (69, 3)),
        new(0x1C, (69, 4)),
        new(0x0E, (69, 5)),
        new(0x0F, (69, 6)),
        new(0x10, (69, 7)),
        new(0x11, (69, 8)),
        new(0x12, (69, 9)),
    ];
    
    /// <summary>
    /// Used by the options menu (and others ig)
    /// </summary>
    public static LayoutGraphic[] OptionsBgAndOtherGraphics =
    [
        new(0x33, (0, 53)),
        new(0x34, (0, 96)),
        new(0x71, (0, 12)),
        new(0x72, (0, 13)),
        new(0x45, (0, 0)),
        new(0x47, (0, 95)),
        new(0x48, (0, 99)),
        new(0x4A, (0, 0)),
        new(0x4B, (0, 0)),
        new(0x4C, (0, 0)),
        new(0x4D, (0, 0)),
        new(0x4E, (0, 0)),
        new(0x4F, (0, 0)),
        new(0x50, (0, 100)),
        new(0x51, (0, 6)),
        new(0x52, (0, 7)),
        new(0x53, (0, 8)),
        new(0x54, (0, 0)),
        new(0x55, (0, 0)),
        new(0x56, (0, 0)),
        new(0x57, (0, 0)),
        new(0x58, (0, 0)),
        new(0x59, (0, 0)),
        new(0xB2, (0, 0)),
        new(0xB3, (0, 0)),
        new(0x5A, (0, 0)),
        new(0x5B, (0, 0)),
        new(0x5C, (0, 0)),
        new(0x5D, (0, 0)),
        new(0x5E, (0, 0)),
        new(0x5F, (0, 0)),
        new(0x60, (0, 0)),
        new(0x61, (0, 0)),
        new(0x62, (0, 0)),
        new(0x63, (0, 0)),
        new(0x64, (0, 0)),
        new(0x65, (0, 0)),
        new(0x66, (0, 0)),
        new(0x6E, (0, 0)),
        new(0x6F, (0, 0)),
        new(0x70, (0, 94)),
    ];
    
    /// <summary>
    /// Used for the main script interface
    /// </summary>
    public static LayoutGraphic[] MainInterfaceGraphics =
    [
        new(0x4C, (0, 0)),
        new(0x4D, (0, 0)),
        new(0x4E, (0, 0)),
        new(0x50, (0, 100)),
        new(0x53, (0, 8)),
        new(0x54, (0, 0)),
        new(0x4A, (0, 0)),
        new(0x4B, (0, 0)),
        new(0x45, (0, 0)),
        new(0x5F, (0, 0)),
        new(0x56, (0, 0)),
        new(0x61, (0, 0)),
        new(0x62, (0, 0)),
        new(0x63, (0, 0)),
        new(0x55, (0, 0)),
        new(0x47, (0, 95)),
        new(0x64, (0, 0)),
        new(0x45, (0, 0)),
        new(0x56, (0, 0)),
        new(0x60, (0, 0)),
        new(0x33, (0, 53)),
        new(0x48, (0, 99)),
        new(0x5A, (0, 0)),
        new(0x5B, (0, 0)),
        new(0x5C, (0, 0)),
        new(0x5D, (0, 0)),
        new(0x5E, (0, 0)),
    ];
    
    /// <summary>
    /// Used by the pause menu layout
    /// </summary>
    public static LayoutGraphic[] PauseMenuGraphics =
    [
        new(0x47, (0, 95)),
        new(0x45, (0, 0)),
        new(0x33, (0, 53)),
        new(0x48, (0, 99)),
    ];
    
    /// <summary>
    /// Unknown
    /// </summary>
    public static LayoutGraphic[] Unknown801BAB1C =
    [
        new(0x45, (0, 0)),
        new(0x60, (0, 0)),
        new(0x33, (0, 53)),
        new(0x48, (0, 99)),
    ];
    
    /// <summary>
    /// Unknown
    /// </summary>
    public static LayoutGraphic[] Unknown801BAB28 =
    [
        new(0x47, (0, 95)),
        new(0x65, (0, 0)),
        new(0x33, (0, 53)),
        new(0x48, (0, 99)),
        new(0x5C, (0, 0)),
    ];
}

/// <summary>
/// A graphic used by a layout
/// </summary>
public struct LayoutGraphic
{
    /// <summary>
    /// Index of the graphic in grp.bin
    /// </summary>
    public int GrpIndex { get; }
    /// <summary>
    /// Location of the graphic in the MCB
    /// </summary>
    public (int parent, int child) McbLocation { get; }

    /// <summary>
    /// Constructs a layout graphic
    /// </summary>
    /// <param name="grpIndex">Index of the graphic in grp.bin</param>
    /// <param name="mcbLoc">Location of the graphic in the MCB</param>
    public LayoutGraphic(int grpIndex, (int, int) mcbLoc)
    {
        GrpIndex = grpIndex;
        McbLocation = mcbLoc;
    }
}