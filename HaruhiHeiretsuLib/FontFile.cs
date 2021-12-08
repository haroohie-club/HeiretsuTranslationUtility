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

        public FontFile(byte[] data)
        {
            Data = data.ToList();
            NumCharacters = BitConverter.ToInt32(data.Take(4).ToArray());

            for (int i = 0; i < NumCharacters; i++)
            {
                Pointers.Add(BitConverter.ToInt32(data.Skip(4 * (i + 1)).Take(4).ToArray()));
                Characters.Add(new Character(Helpers.DecompressData(data.Skip(Pointers[i]).ToArray()), Pointers[i]));
            }
        }
    }

    public class Character : GraphicsFile
    {
        public Character(byte[] data, int offset)
        {
            Offset = offset;
            FileType = GraphicsFileType.FONT_CHARACTER;
            Data = data.ToList();
            Height = 24;
            Width = data.Length / Height;
        }

        public override string ToString()
        {
            return $"{Offset:X8}";
        }
    }
}
