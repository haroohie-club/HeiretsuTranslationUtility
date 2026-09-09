using HaruhiHeiretsuLib.Util;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiHeiretsuLib.Graphics;

/// <summary>
/// The game's font file, which is an archive of glyphs
/// </summary>
public class FontFile
{
    /// <summary>
    /// Number of glyphs
    /// </summary>
    public int NumGlyphs { get; set; }
    /// <summary>
    /// Unused 16-color palette for the font
    /// </summary>
    public List<int> UnusedPalette { get; set; } = [];
    /// <summary>
    /// List of glyphs in the font
    /// </summary>
    public List<Glyph> Glyphs { get; set; } = [];
    
    internal bool Edited { get; set; }

    private readonly Dictionary<ushort, int> _codepointsToIndexes = [];

    /// <summary>
    /// Constructs the font file from binary data
    /// </summary>
    /// <param name="data">Binary file data</param>
    public FontFile(byte[] data)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        NumGlyphs = BitConverter.ToInt32(data.Take(4).ToArray());

        for (ushort codepoint = 0x000; codepoint < 0xFFFF; codepoint++)
        {
            _codepointsToIndexes.Add(codepoint, Glyph.CodePointToIndex(codepoint));
        }

        for (int i = 0; i < NumGlyphs; i++)
        {
            int offset = BitConverter.ToInt32(data.Skip(4 * (i + 1)).Take(4).ToArray());
            int idx = i;
            Glyphs.Add(new(Helpers.DecompressData(data.Skip(offset).ToArray()),
                i, _codepointsToIndexes.Where(c => c.Value == idx).Select(c => c.Key), offset));
        }

        for (int i = (NumGlyphs + 1) * 4; i < ((NumGlyphs + 1) * 4) + 0x40; i += 4)
        {
            UnusedPalette.Add(BitConverter.ToInt32(data.Skip(i).Take(4).ToArray()));
        }
    }

    /// <summary>
    /// Gets binary data representing the font file
    /// </summary>
    /// <returns>The binary data of the font file</returns>
    public byte[] GetBytes()
    {
        List<byte> data = [];
        List<int> pointers =
        [
            ((NumGlyphs + 1) * 4) + (UnusedPalette.Count * 4)
        ];

        data.AddRange(BitConverter.GetBytes(NumGlyphs));
            
        foreach (Glyph character in Glyphs)
        {
            List<byte> charData = [.. Helpers.CompressData([.. character.Data])];
            charData.Add(0x00);
            pointers.Add(pointers.Last() + charData.Count);
            data.AddRange(charData);
        }
        pointers.RemoveAt(pointers.Count - 1); // remove pointer to end of file
        data.InsertRange(4, pointers.SelectMany(p => BitConverter.GetBytes(p)));
        data.InsertRange((NumGlyphs + 1) * 4, UnusedPalette.SelectMany(i => BitConverter.GetBytes(i)));

        return [.. data];
    }

    /// <summary>
    /// Overwrites the font data
    /// </summary>
    /// <param name="font"></param>
    /// <param name="fontSize"></param>
    /// <param name="fontReplacementMap"></param>
    public void OverwriteFont(string font, float fontSize, FontReplacementMap fontReplacementMap)
    {
        Edited = true;

        SKFont skFont;
        if (font.EndsWith(".ttf") || font.EndsWith(".otf"))
        {
            skFont = new(SKTypeface.FromFile(font), fontSize);
        }
        else
        {
            skFont = new(SKTypeface.FromFamilyName(font), fontSize);
        }

        foreach ((ushort codepoint, FontReplacementCharacter replacement) in fontReplacementMap.Map)
        {
            Glyphs.First(c => c.Codepoints.Contains(codepoint)).SetFontCharacterImage(replacement.Character, skFont, fontSize, replacement.VerticalOffset);
        }
    }
}

/// <summary>
/// A font glyph
/// </summary>
public class Glyph : GraphicsFile
{
    /// <summary>
    /// The codepoints associated with this glyph
    /// </summary>
    public ushort[] Codepoints { get; set; }

    internal const int ScaledWidth = 25;
    internal const int ScaledHeight = 24;

    /// <summary>
    /// Constructs a glyph from data
    /// </summary>
    /// <param name="data">The font file data</param>
    /// <param name="index">The index of the glyph</param>
    /// <param name="codepoint">The codepoint(s) represented by the glyph</param>
    /// <param name="offset">The offset of the glyph in the font file</param>
    public Glyph(byte[] data, int index, IEnumerable<ushort> codepoint, int offset)
    {
        Codepoints = codepoint.ToArray();
        BinArchiveIndex = index;
        Offset = offset;
        FileType = GraphicsFileType.FONT_CHARACTER;
        Data = [.. data];
        Height = 24;
        Width = (int)(data.Length / Height * 2.0);
    }

    /// <summary>
    /// Gets the Shift-JIS codepoints as a string
    /// </summary>
    /// <returns>A string representation of this glyph's codepoints</returns>
    public string GetCodepointsString()
    {
        string codepointsString = "";
        foreach (ushort codepoint in Codepoints)
        {
            codepointsString += $"'{Encoding.GetEncoding("Shift-JIS").GetString(BitConverter.GetBytes(codepoint).Reverse().ToArray())}' (0x{codepoint:X4}), ";
        }
        return codepointsString;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"'{Encoding.GetEncoding("Shift-JIS").GetString(BitConverter.GetBytes(Codepoints.Last()).Reverse().ToArray())}' {BinArchiveIndex:D4} {Offset:X8}";
    }

    /// <summary>
    /// Looks up a glyph index given a codepoint
    /// </summary>
    /// <param name="codepoint">A single codepoint</param>
    /// <returns>The index representing that codepoint</returns>
    public static int CodePointToIndex(ushort codepoint)
    {
        int index;
        if (codepoint >= 0x300)
        {
            int tempParsing = FontParseEncoding(codepoint);
            index = (((tempParsing >> 8) - 0x21) * 0x5E) + (tempParsing & 0xFF) - 0x21;
        }
        else if (codepoint >= 0x100)
        {
            if (codepoint < 0x200)
            {
                index = codepoint + 0x0398 - 0x120;
            }
            else
            {
                index = codepoint + 0x04D8 - 0x200;
            }
        }
        else
        {
            index = codepoint + 0x02F8 - 0x20;
        }

        return index;
    }

    /// <summary>
    /// Parses a codepoint the way the game code does
    /// </summary>
    /// <param name="codepoint">A given code point</param>
    /// <returns>A parsed index</returns>
    public static int FontParseEncoding(ushort codepoint)
    {
        byte msb = BitConverter.GetBytes(codepoint)[1];
        byte lsb = BitConverter.GetBytes(codepoint)[0];
        if (msb < 0x81 || msb > 0x9F)
        {
            if (msb >= 0xE0 && msb <= 0xEF)
            {
                msb -= 0xC1;
            }
        }
        else
        {
            msb -= 0x81;
        }

        msb *= 2;
        if (lsb >= 0x40 && lsb <= 0x7E)
        {
            lsb -= 0x40;
        }
        else
        {
            if (lsb >= 0x80 && lsb <= 0x9E)
            {
                lsb -= 0x41;
            }
            else if (lsb >= 0x9F && lsb <= 0xFC)
            {
                lsb -= 0x9F;
                msb += 1;
            }
        }

        return ((msb + 1) << 8) + lsb + 0x2021;
    }
}