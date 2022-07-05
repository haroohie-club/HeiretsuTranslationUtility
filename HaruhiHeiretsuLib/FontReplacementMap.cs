using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaruhiHeiretsuLib
{
    public class FontReplacementMap
    {
        public Dictionary<ushort, FontReplacementCharacter> Map { get; set; } = new();

        public static FontReplacementMap FromJson(string json)
        {
            JsonSerializer serializer = new();
            using StringReader stringReader = new(json);
            using JsonTextReader jsonTextReader = new(stringReader);
            FontReplacementMap map = serializer.Deserialize<FontReplacementMap>(jsonTextReader);
            return map;
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
                foreach ((ushort start, FontReplacementCharacter replacement) in grouping)
                {
                    cFile += $"        case 0x{start:X4}:\n";
                }
                cFile += $"            return {grouping.Key};\n";
            }
            cFile += @"        default:
            return 144;
    }
}";
            return cFile;
        }

        public string GetJson()
        {
            JsonSerializer serializer = new();
            serializer.Formatting = Formatting.Indented;
            using StringWriter stringWriter = new();
            using JsonTextWriter writer = new(stringWriter);
            serializer.Serialize(writer, this);
            return stringWriter.ToString();
        }
    }

    public class FontReplacementCharacter
    {
        public string Character { get; set; }
        public int Spacing { get; set; }
    }
}
