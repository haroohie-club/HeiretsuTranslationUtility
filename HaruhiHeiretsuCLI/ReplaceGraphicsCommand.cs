using HaruhiHeiretsuLib;
using Mono.Options;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HaruhiHeiretsuCLI
{
    public class ReplaceGraphicsCommand : Command
    {
        private string _mcb, _grp, _replacementDir, _outputDir;

        public ReplaceGraphicsCommand() : base("replace-graphics")
        {
            Options = new()
            {
                "Replace graphics files with PNG files from a replacement folder in the MCB and grp.bin",
                "Usage: HaruhiHeiretsuCLI replace-graphics -m [MCB_PATH] -g [GRP_BIN] -r [REPLACEMENT_FOLDER] -o [OUTPUT_FOLDER]",
                "",
                { "m|mcb=", "Path to mcb0.bln", m => _mcb = m },
                { "g|grp=", "Path to grp.bin", g => _grp = g },
                { "r|replacement=", "Directory from which to pull files for replacement", r => _replacementDir = r },
                { "o|output=", "Directory where modified MCBs and grp.bin will be placed", o => _outputDir = o },
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


            Regex grpRegex = new(@"grp-(?<grpIndex>\d{4})");
            foreach (string file in Directory.GetFiles(_replacementDir))
            {
                if (file.EndsWith("png", StringComparison.OrdinalIgnoreCase))
                {
                    SKBitmap bitmap = SKBitmap.Decode(file);

                    int grpIndex = int.Parse(grpRegex.Match(file).Groups["grpIndex"].Value);
                    grp.Files.First(f => f.Index == grpIndex).Set20AF30Image(bitmap);

                    mcb.GraphicsFiles.Clear();
                    mcb.LoadGraphicsFiles(file.Split('_'));
                    foreach (GraphicsFile graphicsFile in mcb.GraphicsFiles)
                    {
                        graphicsFile.Set20AF30Image(bitmap);
                    }

                    Console.WriteLine($"Finished replacing file {file} in MCB & GRP");
                }
            }

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
