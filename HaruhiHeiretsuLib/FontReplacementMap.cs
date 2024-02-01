using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace HaruhiHeiretsuLib
{
    public class FontReplacementMap
    {
        public Dictionary<ushort, FontReplacementCharacter> Map { get; set; } = [];

        public static FontReplacementMap FromJson(string json)
        {
            return JsonSerializer.Deserialize<FontReplacementMap>(json);
        }

        public FontReplacementMap()
        {
        }
        public FontReplacementMap(Dictionary<string, FontReplacementCharacter> map)
        {
            int count = 0;
            Map = map.ToDictionary(kv =>
            {
                List<byte> bytes = Encoding.GetEncoding("Shift-JIS").GetBytes(kv.Key).Reverse().ToList();
                if (bytes.Count == 1)
                {
                    bytes.Add(0);
                }
                count++;
                return BitConverter.ToUInt16(bytes.ToArray());
            },
            kv => kv.Value);
        }

        public bool ContainsReplacement(string replacement)
        {
            return Map.Any(kv => kv.Value.Character == replacement);
        }

        public string GetStartCharacterForReplacement(string replacement)
        {
            return Encoding.GetEncoding("Shift-JIS").GetString(BitConverter.GetBytes(Map.First(kv => kv.Value.Character == replacement).Key).Reverse().ToArray()).Replace("\0", "");
        }

        public int GetReplacementCharacterWidth(string replacement)
        {
            return Map.FirstOrDefault(kv => kv.Value.Character == replacement).Value?.Spacing ?? 354;
        }

        public string GetFontHackCFile()
        {
            string cFile = @"int font_offset(unsigned short character)
{
    switch (character)
    {
";

            IEnumerable<IGrouping<int, KeyValuePair<ushort, FontReplacementCharacter>>> switchGroup = Map.GroupBy(kv => kv.Value.Spacing).OrderBy(g => g.Key);

            foreach (IGrouping<int, KeyValuePair<ushort, FontReplacementCharacter>> grouping in switchGroup)
            {
                foreach ((ushort startingValue, _) in grouping)
                {
                    cFile += $"        case 0x{startingValue:X4}:\n";
                }
                cFile += $"            return {grouping.Key};\n";
            }
            cFile += @"        default:
            return 354;
    }
}";
            return cFile;
        }

        public string GetJson()
        {
            return JsonSerializer.Serialize(this);
        }
    }

    public class FontReplacementCharacter
    {
        public string Character { get; set; }
        public int Spacing { get; set; }
        public int VerticalOffset { get; set; }

        public override string ToString()
        {
            return $"{Character} ({Spacing} units)";
        }
    }
}
