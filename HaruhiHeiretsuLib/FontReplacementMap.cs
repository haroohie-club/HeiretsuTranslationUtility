using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace HaruhiHeiretsuLib;

/// <summary>
/// A font replacement map
/// </summary>
public class FontReplacementMap
{
    /// <summary>
    /// The map of font replacement characters
    /// </summary>
    public Dictionary<ushort, FontReplacementCharacter> Map { get; set; } = [];

    /// <summary>
    /// Deserializes the font replacement map from JSON
    /// </summary>
    /// <param name="json">The JSON string</param>
    /// <returns>A font replacement map</returns>
    public static FontReplacementMap FromJson(string json)
    {
        return JsonSerializer.Deserialize<FontReplacementMap>(json);
    }

    /// <summary>
    /// Empty constructor for serialization
    /// </summary>
    public FontReplacementMap()
    {
    }
    
    /// <summary>
    /// Constructs a font replacement map from a dictionary
    /// </summary>
    /// <param name="map">The dictionaryt hat is the map basically</param>
    public FontReplacementMap(Dictionary<string, FontReplacementCharacter> map)
    {
        Map = map.ToDictionary(kv =>
            {
                List<byte> bytes = Encoding.GetEncoding("Shift-JIS").GetBytes(kv.Key).Reverse().ToList();
                if (bytes.Count == 1)
                {
                    bytes.Add(0);
                }

                return BitConverter.ToUInt16(bytes.ToArray());
            },
            kv => kv.Value);
    }

    /// <summary>
    /// Check if a replacement character is contained in the map
    /// </summary>
    /// <param name="replacement">The replacement character</param>
    /// <returns>True if the character is contained</returns>
    public bool ContainsReplacement(string replacement)
    {
        return Map.Any(kv => kv.Value.Character == replacement);
    }

    /// <summary>
    /// Gets the original character for a replacement
    /// </summary>
    /// <param name="replacement">The replacement character</param>
    /// <returns>The starting character</returns>
    public string GetStartCharacterForReplacement(string replacement)
    {
        return Encoding.GetEncoding("Shift-JIS").GetString(BitConverter.GetBytes(Map.First(kv => kv.Value.Character == replacement).Key).Reverse().ToArray()).Replace("\0", "");
    }

    /// <summary>
    /// Gets the width of a replacement character
    /// </summary>
    /// <param name="replacement">The character</param>
    /// <returns>The width of the character</returns>
    public int GetReplacementCharacterWidth(string replacement)
    {
        return Map.FirstOrDefault(kv => kv.Value.Character == replacement).Value?.Spacing ?? 354;
    }

    /// <summary>
    /// Constructs a C file for font replacement
    /// </summary>
    /// <returns>A C file containing the font hack</returns>
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

    /// <summary>
    /// Gets a JSON representation fo the font map
    /// </summary>
    /// <returns>The JSON of the font map</returns>
    public string GetJson()
    {
        return JsonSerializer.Serialize(this);
    }
}

/// <summary>
/// A font replacement character
/// </summary>
public class FontReplacementCharacter
{
    /// <summary>
    /// The replacement character
    /// </summary>
    public string Character { get; set; }
    /// <summary>
    /// The spacing width
    /// </summary>
    public int Spacing { get; set; }
    /// <summary>
    /// The vertical offset
    /// </summary>
    public int VerticalOffset { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Character} ({Spacing} units)";
    }
}