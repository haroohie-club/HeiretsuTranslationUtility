using HaruhiHeiretsuLib;
using HaruhiHeiretsuLib.Graphics;
using HaruhiHeiretsuLib.Strings;
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
    public class ImportResxCommand : Command
    {
        private string _mcb, _dat, _evt, _scr, _resxDir, _langCode, _outputDir;
        public ImportResxCommand() : base("import-resx", "Import RESX files into the mcb and bin archives")
        {
            Options = new()
            {
                "Imports .NET resource files and uses them to replace strings in the mcb and dat/evt/scr bin archives",
                "Usage: HaruhiHeiretsuCLI import-resx -m [MCB_PATH] -d [DAT_BIN] -e [EVT_BIN] -s [SCR_BIN] -r [REPLACEMENT_FOLDER] -o [OUTPUT_FOLDER]",
                "",
                { "m|mcb=", "Path to mcb0.bln", m => _mcb = m },
                { "d|dat=", "Path to dat.bin", d => _dat = d },
                { "e|evt=", "Path to evt.bin", e => _evt = e },
                { "s|scr=", "Path to scr.bin", s => _scr = s },
                { "r|resx=", "Path to RESX directory", r => _resxDir = r },
                { "l|lang-code=", "Language code to use during replacement", l => _langCode = l },
                { "o|output=", "Path to output directory", o => _outputDir = o },
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
            ArchiveFile<ShadeStringsFile> dat = ArchiveFile<ShadeStringsFile>.FromFile(_dat);
            ArchiveFile<EventFile> evt = ArchiveFile<EventFile>.FromFile(_evt);
            ArchiveFile<ScriptFile> scr = ArchiveFile<ScriptFile>.FromFile(_scr);
            Dictionary<McbFile.ArchiveIndex, bool> archivesEdited = new() { { McbFile.ArchiveIndex.DAT, false }, { McbFile.ArchiveIndex.EVT, false }, { McbFile.ArchiveIndex.SCR, false } };

            Regex archiveRegex = new(@"(?<archiveName>dat|evt|grp|scr)-(?<archiveIndex>\d{4})");
            foreach (string file in Directory.GetFiles(_resxDir, $"*.{_langCode}.resx", SearchOption.AllDirectories))
            {
                Match archiveRegexMatch = archiveRegex.Match(file);
                if (!archiveRegexMatch.Success)
                {
                    continue;
                }
                string archive = archiveRegexMatch.Groups["archiveName"].Value;
                int archiveIndex = int.Parse(archiveRegexMatch.Groups["archiveIndex"].Value);

                int i = mcb.StringsFiles.Count;
                mcb.LoadStringsFiles(file.Split('_'));
                for (; i < mcb.StringsFiles.Count; i++)
                {
                    
                }

                switch (archive)
                {
                    case "dat":

                        break;

                    case "evt":
                        break;

                    case "scr":
                        break;

                    case "default":
                        CommandSet.Out.WriteLine($"WARNING: RESX file {file} targets {archive}.bin which is invalid, skipping...");
                        continue;
                }
            }

            await mcb.Save(Path.Combine(_outputDir, "mcb0.bln"), Path.Combine(_outputDir, "mcb1.bln"));
            CommandSet.Out.WriteLine("Finished saving MCB");

            if (archivesEdited[McbFile.ArchiveIndex.DAT])
            {
                File.WriteAllBytes(Path.Combine(_outputDir, "dat.bin"), dat.GetBytes(out Dictionary<int, int> offsetAdjustments));
                CommandSet.Out.WriteLine("Finished saving DAT");
                await mcb.AdjustOffsets(Path.Combine(_outputDir, "mcb0.bln"), Path.Combine(_outputDir, "mcb1.bln"), "dat.bin", offsetAdjustments);
                CommandSet.Out.WriteLine("Finished adjusting MCB offsets");
            }
            if (archivesEdited[McbFile.ArchiveIndex.EVT])
            {
                File.WriteAllBytes(Path.Combine(_outputDir, "evt.bin"), evt.GetBytes(out Dictionary<int, int> offsetAdjustments));
                CommandSet.Out.WriteLine("Finished saving EVT");
                await mcb.AdjustOffsets(Path.Combine(_outputDir, "mcb0.bln"), Path.Combine(_outputDir, "mcb1.bln"), "evt.bin", offsetAdjustments);
                CommandSet.Out.WriteLine("Finished adjusting MCB offsets");
            }
            if (archivesEdited[McbFile.ArchiveIndex.SCR])
            {
                File.WriteAllBytes(Path.Combine(_outputDir, "scr.bin"), scr.GetBytes(out Dictionary<int, int> offsetAdjustments));
                CommandSet.Out.WriteLine("Finished saving SCR");
                await mcb.AdjustOffsets(Path.Combine(_outputDir, "mcb0.bln"), Path.Combine(_outputDir, "mcb1.bln"), "scr.bin", offsetAdjustments);
                CommandSet.Out.WriteLine("Finished adjusting MCB offsets");
            }

            return 0;
        }
    }
}
