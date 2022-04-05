using HaruhiHeiretsuLib;
using HaruhiHeiretsuLib.Graphics;
using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaruhiHeiretsuCLI
{
    public class ReplaceFontCommand : Command
    {
        private char _beginChar, _endChar;
        private int _fontSize;
        private string _mcb, _grp, _fontPath, _encoding, _outputDir;
        public ReplaceFontCommand() : base("replace-font")
        {
            Options = new()
            {
                "Replace a section of the base game font with a new one",
                "Usage: HaruhiHeiretsuCLI replace-font -m [MCP_BATH] -g [GPR_BIN_PATH] -f [FONT_FILE] -s [FONT_SIZE] -b [BEGIN_CHAR] -e [END_CHAR] [-c [ENCODING]] -o [OUTPUT_DIR]",
                "",
                { "m|mcb=", "Path to mcb0.bln", m => _mcb = m },
                { "g|grp=", "Path to grp.bin", g => _grp = g },
                { "f|font-file=", "Path to the font file", f => _fontPath = f },
                { "s|font-size=", "The font size to draw", s => _fontSize = int.Parse(s) },
                { "b|begin-char=", "The first character to replace", b => _beginChar = b[0] },
                { "e|end-char=", "The last character to replace", e => _endChar = e[0] },
                { "c|encoding=", "The name of the encoding to use (default is Latin-1)", c => _encoding = c },
                { "o|output=", "The directory to save the MCB and grp.bin to", o => _outputDir = o },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            return InvokeAsync(arguments).GetAwaiter().GetResult();
        }

        public async Task<int> InvokeAsync(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);
            McbFile mcb = Program.GetMcbFile(_mcb);
            ArchiveFile<GraphicsFile> grp = ArchiveFile<GraphicsFile>.FromFile(_grp);

            Encoding encoding;
            if (string.IsNullOrEmpty(_encoding))
            {
                encoding = Encoding.Latin1;
            }
            else
            {
                encoding = Encoding.GetEncoding(_encoding);
            }

            CommandSet.Out.WriteLine("Loading font file...");
            mcb.LoadFontFile();
            CommandSet.Out.WriteLine($"Replacing characters '{_beginChar}' through '{_endChar}' in font file with font {Path.GetFileName(_fontPath)} size {_fontSize}");
            mcb.FontFile.OverwriteFont(_fontPath, _fontSize, _beginChar, _endChar, encoding);
            grp.Files[0].Data = mcb.FontFile.GetBytes().ToList();
            grp.Files[0].Edited = true;

            await mcb.Save(Path.Combine(_outputDir, "mcb0.bln"), Path.Combine(_outputDir, "mcb1.bln"));
            CommandSet.Out.WriteLine("Finished saving MCB");
            File.WriteAllBytes(Path.Combine(_outputDir, "grp.bin"), grp.GetBytes(out Dictionary<int, int> offsetAdjustments));
            CommandSet.Out.WriteLine("Finished saving GRP");
            await mcb.AdjustOffsets(Path.Combine(_outputDir, "mcb0.bln"), Path.Combine(_outputDir, "mcb1.bln"), "grp.bin", offsetAdjustments);
            CommandSet.Out.WriteLine("Finished adjusting MCB offsets");

            return 0;
        }
    }
}
