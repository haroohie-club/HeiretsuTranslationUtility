using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topten.RichTextKit;

namespace HaruhiHeiretsuLib.Graphics
{
    public class FontFile
    {
        public byte[] CompressedData { get; set; }
        public int NumCharacters { get; set; }
        public List<int> UnknownInts { get; set; } = new();
        public List<Character> Characters { get; set; } = new();
        public bool Edited { get; set; } = false;

        private Dictionary<ushort, int> _codepointsToIndexes = new();

        public FontFile(byte[] data)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            NumCharacters = BitConverter.ToInt32(data.Take(4).ToArray());

            for (ushort codepoint = 0x000; codepoint < 0xFFFF; codepoint++)
            {
                _codepointsToIndexes.Add(codepoint, Character.CodePointToIndex(codepoint));
            }

            for (int i = 0; i < NumCharacters; i++)
            {
                int offset = BitConverter.ToInt32(data.Skip(4 * (i + 1)).Take(4).ToArray());
                Characters.Add(new(Helpers.DecompressData(data.Skip(offset).ToArray()),
                    i, _codepointsToIndexes.Where(c => c.Value == i).Select(c => c.Key), offset));
            }

            for (int i = (NumCharacters + 1) * 4; i < ((NumCharacters + 1) * 4) + 0x40; i += 4)
            {
                UnknownInts.Add(BitConverter.ToInt32(data.Skip(i).Take(4).ToArray()));
            }
        }

        public byte[] GetBytes()
        {
            List<byte> data = new();
            List<int> pointers = new()
            {
                ((NumCharacters + 1) * 4) + (UnknownInts.Count * 4)
            };

            data.AddRange(BitConverter.GetBytes(NumCharacters));
            
            foreach (Character character in Characters)
            {
                List<byte> charData = Helpers.CompressData(character.Data.ToArray()).ToList();
                charData.Add(0x00);
                pointers.Add(pointers.Last() + charData.Count);
                data.AddRange(charData);
            }
            pointers.RemoveAt(pointers.Count - 1); // remove pointer to end of file
            data.InsertRange(4, pointers.SelectMany(p => BitConverter.GetBytes(p)));
            data.InsertRange((NumCharacters + 1) * 4, UnknownInts.SelectMany(i => BitConverter.GetBytes(i)));

            return data.ToArray();
        }

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
                Characters.First(c => c.Codepoints.Contains(codepoint)).SetFontCharacterImage(replacement.Character, skFont, fontSize, replacement.VerticalOffset);
            }
        }
    }

    public class Character : GraphicsFile
    {
        public ushort[] Codepoints { get; set; }

        public const int SCALED_WIDTH = 25;
        public const int SCALED_HEIGHT = 24;

        public Character(byte[] data, int index, IEnumerable<ushort> codepoint, int offset)
        {
            Codepoints = codepoint.ToArray();
            Index = index;
            Offset = offset;
            FileType = GraphicsFileType.FONT_CHARACTER;
            Data = data.ToList();
            Height = 24;
            Width = (int)(data.Length / Height * 2.0);
        }

        public string GetCodepointsString()
        {
            string codepointsString = "";
            foreach (ushort codepoint in Codepoints)
            {
                codepointsString += $"'{Encoding.GetEncoding("Shift-JIS").GetString(BitConverter.GetBytes(codepoint).Reverse().ToArray())}' (0x{codepoint:X4}), ";
            }
            return codepointsString;
        }

        public override string ToString()
        {
            return $"'{Encoding.GetEncoding("Shift-JIS").GetString(BitConverter.GetBytes(Codepoints.Last()).Reverse().ToArray())}' {Index:D4} {Offset:X8}";
        }

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
}
