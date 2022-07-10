using HaruhiHeiretsuLib;
using HaruhiHeiretsuLib.Archive;
using HaruhiHeiretsuLib.Data;
using HaruhiHeiretsuLib.Strings.Data;
using HaruhiHeiretsuLib.Strings.Events;
using HaruhiHeiretsuLib.Strings.Scripts;
using Mono.Options;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace HaruhiHeiretsuCLI
{
    public class ImportResxCommand : Command
    {
        private string _mcb, _dat, _evt, _scr, _resxDir, _langCode, _fontMap, _outputDir;
        public ImportResxCommand() : base("import-resx", "Import RESX files into the mcb and bin archives")
        {
            Options = new()
            {
                "Imports .NET resource files and uses them to replace strings in the mcb and dat/evt/scr bin archives",
                "Usage: HaruhiHeiretsuCLI import-resx -m MCB_PATH -d DAT_BIN -e EVT_BIN -s SCR_BIN -r RESX_FOLDER -l LANG_CODE -f FONT_MAP -o OUTPUT_FOLDER",
                "",
                { "m|mcb=", "Path to mcb0.bln", m => _mcb = m },
                { "d|dat=", "Path to dat.bin", d => _dat = d },
                { "e|evt=", "Path to evt.bin", e => _evt = e },
                { "s|scr=", "Path to scr.bin", s => _scr = s },
                { "r|resx=", "Path to RESX directory", r => _resxDir = r },
                { "l|lang-code=", "Language code to use during replacement", l => _langCode = l },
                { "f|font-map=", "Font replacement map (JSON) to use during replacement", f => _fontMap = f },
                { "o|output=", "Path to output directory", o => _outputDir = o },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);
            McbArchive mcb = Program.GetMcbFile(_mcb);
            BinArchive<DataFile> dat = BinArchive<DataFile>.FromFile(_dat);
            BinArchive<EventFile> evt = BinArchive<EventFile>.FromFile(_evt);
            BinArchive<ScriptFile> scr = BinArchive<ScriptFile>.FromFile(_scr);
            Dictionary<McbArchive.ArchiveIndex, bool> archivesEdited = new() { { McbArchive.ArchiveIndex.DAT, false }, { McbArchive.ArchiveIndex.EVT, false }, { McbArchive.ArchiveIndex.SCR, false } };
            FontReplacementMap fontReplacementMap = FontReplacementMap.FromJson(File.ReadAllText(_fontMap));

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
                    FileInArchive mcbFile = mcb.McbSubArchives[mcb.StringsFiles[i].parentLoc].Files[mcb.StringsFiles[i].childLoc];
                    switch ((McbArchive.ArchiveIndex)mcbFile.McbEntryData.archiveIndex)
                    {
                        case McbArchive.ArchiveIndex.DAT:
                            switch (mcb.StringsFiles[i].childLoc)
                            {
                                case DataStringsFileLocations.MAP_DEFINITION_MCB_INDEX:
                                    DataStringsFile<MapDefinitionsFile> mapDefinitionsFile = mcbFile.CastTo<DataStringsFile<MapDefinitionsFile>>();
                                    mapDefinitionsFile.ImportResxFile(file, fontReplacementMap);
                                    mcb.McbSubArchives[mcb.StringsFiles[i].parentLoc].Files[mcb.StringsFiles[i].childLoc] = mapDefinitionsFile.DataFile;
                                    break;

                                case DataStringsFileLocations.TOPICS_FLAG_MCB_INDEX:
                                    DataStringsFile<TopicsAndFlagsFile> topicsAndFlagsFile = mcbFile.CastTo<DataStringsFile<TopicsAndFlagsFile>>();
                                    topicsAndFlagsFile.ImportResxFile(file, fontReplacementMap);
                                    mcb.McbSubArchives[mcb.StringsFiles[i].parentLoc].Files[mcb.StringsFiles[i].childLoc] = topicsAndFlagsFile.DataFile;
                                    break;
                            }
                            break;

                        case McbArchive.ArchiveIndex.EVT:
                            EventFile mcbEventFile = mcbFile.CastTo<EventFile>();
                            mcbEventFile.ImportResxFile(file, fontReplacementMap);
                            mcb.McbSubArchives[mcb.StringsFiles[i].parentLoc].Files[mcb.StringsFiles[i].childLoc] = mcbEventFile;
                            break;

                        case McbArchive.ArchiveIndex.SCR:
                            ScriptFile mcbScriptFile = mcbFile.CastTo<ScriptFile>();
                            mcbScriptFile.ImportResxFile(file, fontReplacementMap);
                            mcb.McbSubArchives[mcb.StringsFiles[i].parentLoc].Files[mcb.StringsFiles[i].childLoc] = mcbScriptFile;
                            break;
                    }
                }

                switch (archive)
                {
                    case "dat":
                        switch (archiveIndex)
                        {
                            case DataStringsFileLocations.MAP_DEFINITION_INDEX:
                                DataStringsFile<MapDefinitionsFile> mapDefinitionsFile = dat.Files[archiveIndex].CastTo<DataStringsFile<MapDefinitionsFile>>();
                                mapDefinitionsFile.ImportResxFile(file, fontReplacementMap);
                                dat.Files[archiveIndex] = mapDefinitionsFile.DataFile;
                                break;

                            case DataStringsFileLocations.TOPICS_FLAGS_INDEX:
                                DataStringsFile<TopicsAndFlagsFile> topicsAndFlagsFile = dat.Files[archiveIndex].CastTo<DataStringsFile<TopicsAndFlagsFile>>();
                                topicsAndFlagsFile.ImportResxFile(file, fontReplacementMap);
                                dat.Files[archiveIndex] = topicsAndFlagsFile.DataFile;
                                break;

                            case DataStringsFileLocations.NAMEPLATES_INDEX:
                                DataStringsFile<NameplatesFile> nameplatesFile = dat.Files[archiveIndex].CastTo<DataStringsFile<NameplatesFile>>();
                                nameplatesFile.ImportResxFile(file, fontReplacementMap);
                                dat.Files[archiveIndex] = nameplatesFile.DataFile;
                                break;
                        }
                        break;

                    case "evt":
                        evt.Files[archiveIndex].ImportResxFile(file, fontReplacementMap);
                        break;

                    case "scr":
                        scr.Files[archiveIndex].ImportResxFile(file, fontReplacementMap);
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
