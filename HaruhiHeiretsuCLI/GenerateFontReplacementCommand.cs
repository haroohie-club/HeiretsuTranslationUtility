using HaruhiHeiretsuLib;
using Mono.Options;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HaruhiHeiretsuCLI
{
    public class GenerateFontReplacementCommand : Command
    {
        private string _fontFile, _extendedCharacters, _outputJson, _outputHack;
        private float _fontSize;

        private const string CHARACTERS = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`abcdefghijklmnopqrstuvwxyz{|}";
        private const double GAME_SCALE_FACTOR = 11.5;

        public GenerateFontReplacementCommand() : base("generate-font-replacement", "Generate a font replacement map from a font file")
        {
            Options = new()
            {
                "Generates a font replacement map from the glyph widths in a given font file",
                "Usage: HaruhiHeiretsuCLI generate-font-replacement-command -f FONT_FILE -s FONT_SIZE -j OUTPUT_FILE -c OUTPUT_HACK [-e EXTENDED_CHARACTERS]",
                "",
                { "f|font|font-file=", "The font file to generate the map from", f => _fontFile = f },
                { "s|size|font-size=", "The size of the font as rendered in-game", s => _fontSize = float.Parse(s) },
                { "j|output-json=", "The output JSON file location", j => _outputJson = j },
                { "c|h|output-c|output-hack=", "The output location of the font hack C file", h => _outputHack = h },
                { "e|extended-characters=", "A string of extended characters to replace Japanese characters with", e => _extendedCharacters = e },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            SKPaint paint = new(new(SKTypeface.FromFile(_fontFile), size: _fontSize));
            int[] widths = paint.GetGlyphWidths(CHARACTERS).Select(w => (int)Math.Round(w * GAME_SCALE_FACTOR)).ToArray();
            int[] extendedWidths = paint.GetGlyphWidths(_extendedCharacters).Select(w => (int)Math.Round(w * GAME_SCALE_FACTOR)).ToArray();

            Dictionary<string, FontReplacementCharacter> fontReplacementMap = new();
            for (int i = 0; i < CHARACTERS.Length; i++)
            {
                fontReplacementMap.Add($"{CHARACTERS[i]}", new() { Character = $"{CHARACTERS[i]}", Spacing = widths[i] });
            }
            ushort codePoint = 0x8140;
            for (int i = 0; i <  _extendedCharacters.Length; i++)
            {
                string encodedCharacter = "";
                // 0x8163 is an exception case in code and it's easier to just not use it than it is to try to hack around it (-1 is to compensate for the ++)
                // If encodedCharacter is empty, that means there is no Shift-JIS character at that codepoint
                while (codePoint - 1 == 0x8163 || encodedCharacter == "" || fontReplacementMap.ContainsKey(encodedCharacter))
                {
                    encodedCharacter = Encoding.GetEncoding("Shift-JIS").GetString(BitConverter.GetBytes(codePoint++).Reverse().ToArray()).Replace("\0", "");
                }
                fontReplacementMap.Add(encodedCharacter,
                    new() { Character = $"{_extendedCharacters[i]}", Spacing = extendedWidths[i] });
            }

            // hardcode some manual adjustments
            if (fontReplacementMap.Any(k => k.Value.Character == "…"))
            {
                fontReplacementMap[fontReplacementMap.First(k => k.Value.Character == "…").Key].VerticalOffset = 3;
            }

            FontReplacementMap map = new(fontReplacementMap);
            File.WriteAllText(_outputJson, map.GetJson());
            File.WriteAllText(_outputHack, map.GetFontHackCFile());
            
            return 0;
        }
    }
}
