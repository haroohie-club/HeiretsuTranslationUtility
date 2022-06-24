using HaruhiHeiretsuLib.Archive;
using HaruhiHeiretsuLib.Strings;
using Mono.Options;
using System.Collections.Generic;
using System.IO;
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
            Options.Parse(arguments);
            McbArchive mcb = Program.GetMcbFile(_mcb);
            BinArchive<ShadeStringsFile> dat = BinArchive<ShadeStringsFile>.FromFile(_dat);
            BinArchive<EventFile> evt = BinArchive<EventFile>.FromFile(_evt);
            BinArchive<ScriptFile> scr = BinArchive<ScriptFile>.FromFile(_scr);
            Dictionary<McbArchive.ArchiveIndex, bool> archivesEdited = new() { { McbArchive.ArchiveIndex.DAT, false }, { McbArchive.ArchiveIndex.EVT, false }, { McbArchive.ArchiveIndex.SCR, false } };

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

            if (archivesEdited[McbArchive.ArchiveIndex.DAT])
            {
                File.WriteAllBytes(Path.Combine(_outputDir, "dat.bin"), dat.GetBytes(out Dictionary<int, int> offsetAdjustments));
                CommandSet.Out.WriteLine("Finished saving DAT");
                mcb.AdjustOffsets("dat.bin", offsetAdjustments);
                CommandSet.Out.WriteLine("Finished adjusting MCB offsets");
                (byte[] mcb0, byte[] mcb1) = mcb.GetBytes();
                File.WriteAllBytes(Path.Combine(_outputDir, "mcb0.bln"), mcb0);
                File.WriteAllBytes(Path.Combine(_outputDir, "mcb1.bln"), mcb1);
                CommandSet.Out.WriteLine("Finished saving MCB");
            }
            if (archivesEdited[McbArchive.ArchiveIndex.EVT])
            {
                File.WriteAllBytes(Path.Combine(_outputDir, "evt.bin"), evt.GetBytes(out Dictionary<int, int> offsetAdjustments));
                CommandSet.Out.WriteLine("Finished saving EVT");
                mcb.AdjustOffsets("evt.bin", offsetAdjustments);
                CommandSet.Out.WriteLine("Finished adjusting MCB offsets");
                (byte[] mcb0, byte[] mcb1) = mcb.GetBytes();
                File.WriteAllBytes(Path.Combine(_outputDir, "mcb0.bln"), mcb0);
                File.WriteAllBytes(Path.Combine(_outputDir, "mcb1.bln"), mcb1);
                CommandSet.Out.WriteLine("Finished saving MCB");
            }
            if (archivesEdited[McbArchive.ArchiveIndex.SCR])
            {
                File.WriteAllBytes(Path.Combine(_outputDir, "scr.bin"), scr.GetBytes(out Dictionary<int, int> offsetAdjustments));
                CommandSet.Out.WriteLine("Finished saving SCR");
                mcb.AdjustOffsets("scr.bin", offsetAdjustments);
                CommandSet.Out.WriteLine("Finished adjusting MCB offsets");
                (byte[] mcb0, byte[] mcb1) = mcb.GetBytes();
                File.WriteAllBytes(Path.Combine(_outputDir, "mcb0.bln"), mcb0);
                File.WriteAllBytes(Path.Combine(_outputDir, "mcb1.bln"), mcb1);
                CommandSet.Out.WriteLine("Finished saving MCB");
            }

            return 0;
        }
    }
}
