using HaruhiHeiretsuLib;
using HaruhiHeiretsuLib.Archive;
using HaruhiHeiretsuLib.Graphics;
using Mono.Options;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HaruhiHeiretsuCLI
{
    public class ReplaceFontCommand : Command
    {
        private float _fontSize;
        private string _mcb, _grp, _fontPath, _fontMap, _outputDir;
        public ReplaceFontCommand() : base("replace-font")
        {
            Options =
            [
                "Replace a section of the base game font with a new one",
                "Usage: HaruhiHeiretsuCLI replace-font -m [MCP_BATH] -g [GPR_BIN_PATH] -f [FONT_FILE] -s [FONT_SIZE] -b [BEGIN_CHAR] -e [END_CHAR] [-c [ENCODING]] -o [OUTPUT_DIR]",
                "",
                { "m|mcb=", "Path to mcb0.bln", m => _mcb = m },
                { "g|grp=", "Path to grp.bin", g => _grp = g },
                { "f|font-file=", "Path to the font file", f => _fontPath = f },
                { "s|font-size=", "The font size to draw", s => _fontSize = float.Parse(s) },
                { "r|replacement|font-replacement-map=", "The font replacement map JSON file", r => _fontMap = r },
                { "o|output=", "The directory to save the MCB and grp.bin to", o => _outputDir = o },
            ];
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);

            CommandSet.Out.WriteLine("Loading archives...");
            McbArchive mcb = Program.GetMcbFile(_mcb);
            BinArchive<GraphicsFile> grp = BinArchive<GraphicsFile>.FromFile(_grp);

            CommandSet.Out.WriteLine("Loading font file...");
            mcb.LoadFontFile();
            CommandSet.Out.WriteLine($"Replacing characters from {Path.GetFileName(_fontMap)} in font file with font {SkiaSharp.SKTypeface.FromFile(_fontPath).FamilyName} size {_fontSize}...");
            mcb.FontFile.OverwriteFont(_fontPath, _fontSize, FontReplacementMap.FromJson(File.ReadAllText(_fontMap)));
            grp.Files[0].Data = [.. mcb.FontFile.GetBytes()];
            grp.Files[0].Edited = true;

            File.WriteAllBytes(Path.Combine(_outputDir, "grp.bin"), grp.GetBytes(out Dictionary<int, int> offsetAdjustments));
            CommandSet.Out.WriteLine("Finished saving GRP");
            mcb.AdjustOffsets("grp.bin", offsetAdjustments);
            CommandSet.Out.WriteLine("Finished adjusting MCB offsets");
            (byte[] mcb0, byte[] mcb1) = mcb.GetBytes();
            File.WriteAllBytes(Path.Combine(_outputDir, "mcb0.bln"), mcb0);
            File.WriteAllBytes(Path.Combine(_outputDir, "mcb1.bln"), mcb1);
            CommandSet.Out.WriteLine("Finished saving MCB");

            return 0;
        }
    }
}
