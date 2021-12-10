using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaruhiHeiretsuLib
{
    public class FontFile
    {
        public List<byte> Data { get; set; }
        public int NumCharacters { get; set; }
        public List<int> Pointers { get; set; } = new();
        public List<Character> Characters { get; set; } = new();

        private static Dictionary<ushort, int> _codepointsToIndexes = new();

        public FontFile(byte[] data)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Data = data.ToList();
            NumCharacters = BitConverter.ToInt32(data.Take(4).ToArray());

            for (ushort codepoint = 0x000; codepoint < 0xFFFF; codepoint++)
            {
                _codepointsToIndexes.Add(codepoint, Character.CodePointToIndex(codepoint));
            }

            for (int i = 0; i < NumCharacters; i++)
            {
                Pointers.Add(BitConverter.ToInt32(data.Skip(4 * (i + 1)).Take(4).ToArray()));
                Characters.Add(new Character(Helpers.DecompressData(data.Skip(Pointers[i]).ToArray()), Pointers[i], i, _codepointsToIndexes.Where(c => c.Value == i).Select(c => c.Key)));
            }
        }
    }

    public class Character : GraphicsFile
    {
        public ushort[] Codepoints { get; set; }

        public Character(byte[] data, int offset, int index, IEnumerable<ushort> codepoint)
        {
            Codepoints = codepoint.ToArray();
            Index = index;
            Offset = offset;
            FileType = GraphicsFileType.FONT_CHARACTER;
            Data = data.ToList();
            Height = 24;
            Width = data.Length / Height;
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
