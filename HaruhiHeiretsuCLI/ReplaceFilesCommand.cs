﻿using HaruhiHeiretsuLib;
using HaruhiHeiretsuLib.Archive;
using HaruhiHeiretsuLib.Data;
using HaruhiHeiretsuLib.Graphics;
using HaruhiHeiretsuLib.Strings.Data;
using HaruhiHeiretsuLib.Strings.Events;
using HaruhiHeiretsuLib.Strings.Scripts;
using Mono.Options;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace HaruhiHeiretsuCLI
{
    public class ReplaceFilesCommand : Command
    {
        private string _mcb, _dat, _evt, _grp, _scr, _fontReplacement, _replacementDir, _resxDir, _langCode, _outputDir;
        public ReplaceFilesCommand() : base("replace-files", "Replaces arbitrary files in the mcb and archives")
        {
            Options = new()
            {
                "Replaces files in the mcb and bin archives",
                "",
                { "m|mcb=", "Path to mcb0.bln", m => _mcb = m },
                { "d|dat=", "Path to dat.bin", d => _dat = d },
                { "e|evt=", "Path to evt.bin", e => _evt = e },
                { "g|grp=", "Path to grp.bin", g => _grp = g },
                { "s|scr=", "Path to scr.bin", s => _scr = s },
                { "f|font-replacement=", "Path to font replacement JSON", f => _fontReplacement = f },
                { "r|replacement=", "Path to replacement directory", r => _replacementDir = r },
                { "x|resx=",  "Path to RESX directory", x => _resxDir = x },
                { "l|lang-code=", "Language code to use during replacement", l => _langCode = l },
                { "o|output=", "Path to output directory", o => _outputDir = o },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);

            CommandSet.Out.WriteLine("Loading archives...");
            McbArchive mcb = Program.GetMcbFile(_mcb);
            BinArchive<FileInArchive> dat = BinArchive<FileInArchive>.FromFile(_dat);
            BinArchive<EventFile> evt = BinArchive<EventFile>.FromFile(_evt);
            BinArchive<GraphicsFile> grp = BinArchive<GraphicsFile>.FromFile(_grp);
            BinArchive<ScriptFile> scr = BinArchive<ScriptFile>.FromFile(_scr);
            Dictionary<McbArchive.ArchiveIndex, bool> archivesEdited = new() { { McbArchive.ArchiveIndex.DAT, false }, { McbArchive.ArchiveIndex.EVT, false }, { McbArchive.ArchiveIndex.GRP, false }, { McbArchive.ArchiveIndex.SCR, false } };
            
            FontReplacementMap fontReplacementMap = null;
            if (!string.IsNullOrEmpty(_fontReplacement))
            {
                fontReplacementMap = FontReplacementMap.FromJson(File.ReadAllText(_fontReplacement));
            }
            Regex archiveRegex = new(@"(?<archiveName>dat|evt|grp|scr)-(?<archiveIndex>\d{4})");

            if (!string.IsNullOrEmpty(_replacementDir))
            {
                string[] files = Directory.GetFiles(_replacementDir, "*", SearchOption.AllDirectories);
                CommandSet.Out.WriteLine($"Beginning replacement of {files.Length} files...");
                foreach (string file in files)
                {
                    Match archiveRegexMatch = archiveRegex.Match(file);
                    if (!archiveRegexMatch.Success)
                    {
                        continue;
                    }
                    string archive = archiveRegexMatch.Groups["archiveName"].Value;
                    int archiveIndex = int.Parse(archiveRegexMatch.Groups["archiveIndex"].Value);

                    if (file.Contains("ignore", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    if (file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        if (archive != "grp")
                        {
                            CommandSet.Out.WriteLine($"WARNING: PNG file {file} targets {archive}.bin rather than grp.bin, skipping...");
                            continue;
                        }

                        SKBitmap bitmap = SKBitmap.Decode(file);

                        grp.Files.First(f => f.BinArchiveIndex == archiveIndex).SetTextureImage(bitmap);

                        int i = mcb.GraphicsFiles.Count;
                        mcb.LoadGraphicsFiles(file.Split('_'));
                        for (; i < mcb.GraphicsFiles.Count; i++)
                        {
                            GraphicsFile g = mcb.McbSubArchives[mcb.GraphicsFiles[i].parentLoc].Files[mcb.GraphicsFiles[i].childLoc].CastTo<GraphicsFile>();
                            g.SetTextureImage(bitmap);
                            mcb.McbSubArchives[mcb.GraphicsFiles[i].parentLoc].Files[mcb.GraphicsFiles[i].childLoc] = g;
                        }

                        archivesEdited[McbArchive.ArchiveIndex.GRP] = true;

                        CommandSet.Out.WriteLine($"Finished replacing file {Path.GetFileName(file)} in MCB & GRP");
                    }
                    else if (file.EndsWith(".sws", StringComparison.OrdinalIgnoreCase))
                    {
                        if (archive != "scr")
                        {
                            CommandSet.Out.WriteLine($"WARNING: SWS file {file} targets {archive}.bin rather than scr.bin, skipping...");
                            continue;
                        }

                        ScriptFile scriptFile = new() { AvailableCommands = ScriptCommand.ParseScriptCommandFile([.. scr.Files[1].Data]) };
                        scriptFile.Compile(File.ReadAllText(file), fontReplacementMap);
                        List<byte> data = scriptFile.Data;

                        List<(int, int)> loadedFileLocations = McbArchive.GetFilesToLoad(file.Split('_'));
                        foreach ((int parentLoc, int childLoc) in loadedFileLocations)
                        {
                            mcb.McbSubArchives[parentLoc].Files[childLoc].Edited = true;
                            mcb.McbSubArchives[parentLoc].Files[childLoc].Data = [.. data];
                        }

                        archivesEdited[McbArchive.ArchiveIndex.SCR] = true;
                        scr.Files.First(f => f.BinArchiveIndex == archiveIndex).Edited = true;
                        scr.Files.First(f => f.BinArchiveIndex == archiveIndex).Data = [.. data];

                        CommandSet.Out.WriteLine($"Finished replacing {Path.GetFileName(file)} in MCB & SCR");
                    }
                    else if (file.EndsWith(".sge.json", StringComparison.OrdinalIgnoreCase))
                    {
                        if (archive != "grp")
                        {
                            CommandSet.Out.WriteLine($"WARNING: SGE file {file} targets {archive}.bin rather than grp.bin, skipping...");
                            continue;
                        }

                        //File.WriteAllBytes(@"C:\Users\User\PortableApps\ROMHacking\WiiHacking\games\heiretsu\DATA\files\sge.bin", [.. Sge.LoadFromJson(File.ReadAllText(file)).GetBytes()]);
                        grp.Files.First(f => f.BinArchiveIndex == archiveIndex).Sge = Sge.LoadFromJson(File.ReadAllText(file));
                        grp.Files.First(f => f.BinArchiveIndex == archiveIndex).Data = grp.Files.First(f => f.BinArchiveIndex == archiveIndex).Sge.GetBytes();
                        grp.Files.First(f => f.BinArchiveIndex == archiveIndex).Edited = true;

                        int i = mcb.GraphicsFiles.Count;
                        mcb.LoadGraphicsFiles(file.Split('_'));
                        for (; i < mcb.GraphicsFiles.Count; i++)
                        {
                            mcb.McbSubArchives[mcb.GraphicsFiles[i].parentLoc].Files[mcb.GraphicsFiles[i].childLoc].Data = grp.Files.First(f => f.BinArchiveIndex == archiveIndex).Data;
                            mcb.McbSubArchives[mcb.GraphicsFiles[i].parentLoc].Files[mcb.GraphicsFiles[i].childLoc].Edited = true;
                        }

                        archivesEdited[McbArchive.ArchiveIndex.GRP] = true;

                        CommandSet.Out.WriteLine($"Finished replacing file {Path.GetFileName(file)} in MCB & GRP");
                    }
                    else if (file.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        if (archive != "grp")
                        {
                            CommandSet.Out.WriteLine($"WARNING: Layout file {file} targets {archive}.bin rather than grp.bin, skipping...");
                            continue;
                        }

                        string json = File.ReadAllText(file);

                        grp.Files.First(f => f.BinArchiveIndex == archiveIndex).ImportLayoutJson(json);
                        grp.Files.First(f => f.BinArchiveIndex == archiveIndex).SetLayoutData();

                        int i = mcb.GraphicsFiles.Count;
                        mcb.LoadGraphicsFiles(file.Split('_'));
                        for (; i < mcb.GraphicsFiles.Count; i++)
                        {
                            mcb.McbSubArchives[mcb.GraphicsFiles[i].parentLoc].Files[mcb.GraphicsFiles[i].childLoc].Data = grp.Files.First(f => f.BinArchiveIndex == archiveIndex).Data;
                            mcb.McbSubArchives[mcb.GraphicsFiles[i].parentLoc].Files[mcb.GraphicsFiles[i].childLoc].Edited = true;
                        }

                        archivesEdited[McbArchive.ArchiveIndex.GRP] = true;

                        CommandSet.Out.WriteLine($"Finished replacing file {Path.GetFileName(file)} in MCB & GRP");
                    }
                    else if (file.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        if (Path.GetFileNameWithoutExtension(file).EndsWith("map"))
                        {
                            if (archive != "grp")
                            {
                                CommandSet.Out.WriteLine($"WARNING: Map CSV file {file} targets {archive}.bin rather than grp.bin, skipping...");
                                continue;
                            }

                            string[] csvLines = File.ReadAllLines(file);
                            List<MapEntry> mapEntries = csvLines.Skip(1).Select(l => new MapEntry(l)).ToList();

                            grp.Files.First(f => f.BinArchiveIndex == archiveIndex).SetMapData(mapEntries);

                            int i = mcb.GraphicsFiles.Count;
                            mcb.LoadGraphicsFiles(file.Split('_'));
                            for (; i < mcb.GraphicsFiles.Count; i++)
                            {
                                mcb.McbSubArchives[mcb.GraphicsFiles[i].parentLoc].Files[mcb.GraphicsFiles[i].childLoc].Data = grp.Files.First(f => f.BinArchiveIndex == archiveIndex).Data;
                                mcb.McbSubArchives[mcb.GraphicsFiles[i].parentLoc].Files[mcb.GraphicsFiles[i].childLoc].Edited = true;
                            }

                            archivesEdited[McbArchive.ArchiveIndex.GRP] = true;

                            CommandSet.Out.WriteLine($"Finished replacing file {Path.GetFileName(file)} in MCB & GRP");
                        }
                        else
                        {
                            if (archive != "dat")
                            {
                                CommandSet.Out.WriteLine($"WARNING: CSV file {file} targets {archive}.bin rather than dat.bin, skipping...");
                                continue;
                            }

                            FileInArchive currentFile = dat.Files.First(f => f.BinArchiveIndex == archiveIndex);
                            List<byte> data = [];
                            
                            if (archiveIndex == 36)
                            {
                                CameraDataFile cameraDataFile = new(File.ReadAllLines(file));
                                data = [.. cameraDataFile.GetBytes()];
                            }
                            else if (archiveIndex == 58)
                            {
                                MapDefinitionsFile mapDefinitionsFile = new(File.ReadAllLines(file), currentFile.BinArchiveIndex, currentFile.Offset);
                                data = [.. mapDefinitionsFile.GetBytes()];
                            }
                            else
                            {
                                CommandSet.Out.WriteLine($"WARNING: CSV file {file} did not target a supported dat file, skipping...");
                                continue;
                            }

                            dat.Files[dat.Files.IndexOf(currentFile)].Edited = true;
                            dat.Files[dat.Files.IndexOf(currentFile)].Data = data;

                            List<(int, int)> loadedFileLocations = McbArchive.GetFilesToLoad(file.Split('_'));
                            foreach ((int parentLoc, int childLoc) in loadedFileLocations)
                            {
                                mcb.McbSubArchives[parentLoc].Files[childLoc].Edited = true;
                                mcb.McbSubArchives[parentLoc].Files[childLoc].Data = [.. data];
                            }

                            archivesEdited[McbArchive.ArchiveIndex.DAT] = true;
                            CommandSet.Out.WriteLine($"Finished replacing file {Path.GetFileName(file)} in MCB & DAT");
                        }
                    }
                    else if (file.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
                    {
                        byte[] data = File.ReadAllBytes(file);

                        List<(int, int)> loadedFileLocations = McbArchive.GetFilesToLoad(file.Split('_'));
                        foreach ((int parentLoc, int childLoc) in loadedFileLocations)
                        {
                            mcb.McbSubArchives[parentLoc].Files[childLoc].Edited = true;
                            mcb.McbSubArchives[parentLoc].Files[childLoc].Data = [.. data];
                        }

                        switch (archive)
                        {
                            case "dat":
                                archivesEdited[McbArchive.ArchiveIndex.DAT] = true;
                                dat.Files.First(f => f.BinArchiveIndex == archiveIndex).Edited = true;
                                dat.Files.First(f => f.BinArchiveIndex == archiveIndex).Data = [.. data];
                                break;
                            case "evt":
                                archivesEdited[McbArchive.ArchiveIndex.EVT] = true;
                                evt.Files.First(f => f.BinArchiveIndex == archiveIndex).Edited = true;
                                evt.Files.First(f => f.BinArchiveIndex == archiveIndex).Data = [.. data];
                                break;
                            case "grp":
                                archivesEdited[McbArchive.ArchiveIndex.GRP] = true;
                                grp.Files.First(f => f.BinArchiveIndex == archiveIndex).Edited = true;
                                grp.Files.First(f => f.BinArchiveIndex == archiveIndex).Data = [.. data];
                                break;
                            case "scr":
                                archivesEdited[McbArchive.ArchiveIndex.SCR] = true;
                                scr.Files.First(f => f.BinArchiveIndex == archiveIndex).Edited = true;
                                scr.Files.First(f => f.BinArchiveIndex == archiveIndex).Data = [.. data];
                                break;
                        }

                        CommandSet.Out.WriteLine($"Finished replacing {Path.GetFileName(file)} in MCB & {archive.ToUpper()}");
                    }
                }
            }

            if (!string.IsNullOrEmpty(_resxDir))
            {
                string[] resxFiles = Directory.GetFiles(_resxDir, $"*.{_langCode}.resx", SearchOption.AllDirectories);
                CommandSet.Out.WriteLine($"Beginning RESX import of {resxFiles.Length} files...");
                foreach (string file in resxFiles)
                {
                    Match archiveRegexMatch = archiveRegex.Match(file);
                    if (!archiveRegexMatch.Success)
                    {
                        continue;
                    }
                    string archive = archiveRegexMatch.Groups["archiveName"].Value;
                    int archiveIndex = int.Parse(archiveRegexMatch.Groups["archiveIndex"].Value);

                    int i = mcb.StringsFiles.Count;
                    mcb.LoadStringsFiles(file.Split('_'), ScriptCommand.ParseScriptCommandFile([.. scr.Files[1].Data]));
                    for (; i < mcb.StringsFiles.Count; i++)
                    {
                        FileInArchive mcbFile = mcb.McbSubArchives[mcb.StringsFiles[i].parentLoc].Files[mcb.StringsFiles[i].childLoc];
                        switch ((McbArchive.ArchiveIndex)mcbFile.McbEntryData.ArchiveIndex)
                        {
                            case McbArchive.ArchiveIndex.DAT:
                                switch (mcb.StringsFiles[i].childLoc)
                                {
                                    case DataStringsFileLocations.SYSTEM_TEXT_MCB_INDEX:
                                        break;

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

                                    case DataStringsFileLocations.NAMEPLATES_MCB_INDEX:
                                        DataStringsFile<NameplatesFile> nameplatesFile = mcbFile.CastTo<DataStringsFile<NameplatesFile>>();
                                        nameplatesFile.ImportResxFile(file, fontReplacementMap);
                                        mcb.McbSubArchives[mcb.StringsFiles[i].parentLoc].Files[mcb.StringsFiles[i].childLoc] = nameplatesFile.DataFile;
                                        break;

                                    case DataStringsFileLocations.TIMELINE_MCB_INDEX:
                                        DataStringsFile<TimelineFile> timelineFile = mcbFile.CastTo<DataStringsFile<TimelineFile>>();
                                        timelineFile.ImportResxFile(file, fontReplacementMap);
                                        mcb.McbSubArchives[mcb.StringsFiles[i].parentLoc].Files[mcb.StringsFiles[i].childLoc] = timelineFile.DataFile;
                                        break;

                                    case DataStringsFileLocations.CLUBROOM_MCB_INDEX:
                                        DataStringsFile<ClubroomKoizumiCutscenesFile> clubroomFile = mcbFile.CastTo<DataStringsFile<ClubroomKoizumiCutscenesFile>>();
                                        clubroomFile.ImportResxFile(file, fontReplacementMap);
                                        mcb.McbSubArchives[mcb.StringsFiles[i].parentLoc].Files[mcb.StringsFiles[i].childLoc] = clubroomFile.DataFile;
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
                                mcbScriptFile.AvailableCommands = ScriptCommand.ParseScriptCommandFile([.. scr.Files[1].Data]);
                                mcbScriptFile.PopulateCommandBlocks();
                                mcbScriptFile.ImportResxFile(file, fontReplacementMap);
                                mcb.McbSubArchives[mcb.StringsFiles[i].parentLoc].Files[mcb.StringsFiles[i].childLoc] = mcbScriptFile;
                                break;
                        }
                    }

                    switch (archive)
                    {
                        case "dat":
                            archivesEdited[McbArchive.ArchiveIndex.DAT] = true;
                            switch (archiveIndex)
                            {
                                case DataStringsFileLocations.MAP_DEFINITION_INDEX:
                                    DataStringsFile<MapDefinitionsFile> mapDefinitionsFile = dat.Files.First(f => f.BinArchiveIndex == archiveIndex).CastTo<DataStringsFile<MapDefinitionsFile>>();
                                    mapDefinitionsFile.ImportResxFile(file, fontReplacementMap);
                                    dat.Files[dat.Files.IndexOf(dat.Files.First(f => f.BinArchiveIndex == archiveIndex))] = mapDefinitionsFile.DataFile;
                                    break;

                                case DataStringsFileLocations.TOPICS_FLAGS_INDEX:
                                    DataStringsFile<TopicsAndFlagsFile> topicsAndFlagsFile = dat.Files.First(f => f.BinArchiveIndex == archiveIndex).CastTo<DataStringsFile<TopicsAndFlagsFile>>();
                                    topicsAndFlagsFile.ImportResxFile(file, fontReplacementMap);
                                    dat.Files[dat.Files.IndexOf(dat.Files.First(f => f.BinArchiveIndex == archiveIndex))] = topicsAndFlagsFile.DataFile;
                                    break;

                                case DataStringsFileLocations.NAMEPLATES_INDEX:
                                    DataStringsFile<NameplatesFile> nameplatesFile = dat.Files.First(f => f.BinArchiveIndex == archiveIndex).CastTo<DataStringsFile<NameplatesFile>>();
                                    nameplatesFile.ImportResxFile(file, fontReplacementMap);
                                    dat.Files[dat.Files.IndexOf(dat.Files.First(f => f.BinArchiveIndex == archiveIndex))] = nameplatesFile.DataFile;
                                    break;

                                case DataStringsFileLocations.TIMELINE_INDEX:
                                    DataStringsFile<TimelineFile> timelineFile = dat.Files.First(f => f.BinArchiveIndex == archiveIndex).CastTo<DataStringsFile<TimelineFile>>();
                                    timelineFile.ImportResxFile(file, fontReplacementMap);
                                    dat.Files[dat.Files.IndexOf(dat.Files.First(f => f.BinArchiveIndex == archiveIndex))] = timelineFile.DataFile;
                                    break;

                                case DataStringsFileLocations.CLUBROOM_INDEX:
                                    DataStringsFile<ClubroomKoizumiCutscenesFile> clubroomFile = dat.Files.First(f => f.BinArchiveIndex == archiveIndex).CastTo<DataStringsFile<ClubroomKoizumiCutscenesFile>>();
                                    clubroomFile.ImportResxFile(file, fontReplacementMap);
                                    dat.Files[dat.Files.IndexOf(dat.Files.First(f => f.BinArchiveIndex == archiveIndex))] = clubroomFile.DataFile;
                                    break;

                                case DataStringsFileLocations.EXTRAS_CLF_CLA_INDEX:
                                    DataStringsFile<ClubroomHaruhiModelsFile> extrasClfClaFile = dat.Files.First(f => f.BinArchiveIndex == archiveIndex).CastTo<DataStringsFile<ClubroomHaruhiModelsFile>>();
                                    extrasClfClaFile.ImportResxFile(file, fontReplacementMap);
                                    dat.Files[dat.Files.IndexOf(dat.Files.First(f => f.BinArchiveIndex == archiveIndex))] = extrasClfClaFile.DataFile;
                                    break;

                                case DataStringsFileLocations.EXTRAS_CLD_INDEX:
                                    DataStringsFile<ClubroomNagatoDatabaseFile> extrasCldFile = dat.Files.First(f => f.BinArchiveIndex == archiveIndex).CastTo<DataStringsFile<ClubroomNagatoDatabaseFile>>();
                                    extrasCldFile.ImportResxFile(file, fontReplacementMap);
                                    dat.Files[dat.Files.IndexOf(dat.Files.First(f => f.BinArchiveIndex == archiveIndex))] = extrasCldFile.DataFile;
                                    break;
                            }
                            break;

                        case "evt":
                            archivesEdited[McbArchive.ArchiveIndex.EVT] = true;
                            evt.Files.First(f => f.BinArchiveIndex == archiveIndex).ImportResxFile(file, fontReplacementMap);
                            break;

                        case "scr":
                            archivesEdited[McbArchive.ArchiveIndex.SCR] = true;
                            scr.Files.First(f => f.BinArchiveIndex == archiveIndex).AvailableCommands = ScriptCommand.ParseScriptCommandFile([.. scr.Files[1].Data]);
                            scr.Files.First(f => f.BinArchiveIndex == archiveIndex).PopulateCommandBlocks();
                            scr.Files.First(f => f.BinArchiveIndex == archiveIndex).ImportResxFile(file, fontReplacementMap);
                            break;

                        case "default":
                            CommandSet.Out.WriteLine($"WARNING: RESX file {file} targets {archive}.bin which is invalid, skipping...");
                            continue;
                    }
                }
            }

            if (archivesEdited[McbArchive.ArchiveIndex.DAT])
            {
                File.WriteAllBytes(Path.Combine(_outputDir, "dat.bin"), dat.GetBytes(out Dictionary<int, int> offsetAdjustments));
                CommandSet.Out.WriteLine("Finished saving DAT");
                mcb.AdjustOffsets("dat.bin", offsetAdjustments);
                CommandSet.Out.WriteLine("Finished adjusting MCB offsets");
            }
            if (archivesEdited[McbArchive.ArchiveIndex.EVT])
            {
                File.WriteAllBytes(Path.Combine(_outputDir, "evt.bin"), evt.GetBytes(out Dictionary<int, int> offsetAdjustments));
                CommandSet.Out.WriteLine("Finished saving EVT");
                mcb.AdjustOffsets("evt.bin", offsetAdjustments);
                CommandSet.Out.WriteLine("Finished adjusting MCB offsets");
            }
            if (archivesEdited[McbArchive.ArchiveIndex.GRP])
            {
                File.WriteAllBytes(Path.Combine(_outputDir, "grp.bin"), grp.GetBytes(out Dictionary<int, int> offsetAdjustments));
                CommandSet.Out.WriteLine("Finished saving GRP");
                mcb.AdjustOffsets("grp.bin", offsetAdjustments);
                CommandSet.Out.WriteLine("Finished adjusting MCB offsets");
            }
            if (archivesEdited[McbArchive.ArchiveIndex.SCR])
            {
                File.WriteAllBytes(Path.Combine(_outputDir, "scr.bin"), scr.GetBytes(out Dictionary<int, int> offsetAdjustments));
                CommandSet.Out.WriteLine("Finished saving SCR");
                mcb.AdjustOffsets("scr.bin", offsetAdjustments);
                CommandSet.Out.WriteLine("Finished adjusting MCB offsets");
            }

            (byte[] mcb0, byte[] mcb1) = mcb.GetBytes();
            File.WriteAllBytes(Path.Combine(_outputDir, "mcb0.bln"), mcb0);
            File.WriteAllBytes(Path.Combine(_outputDir, "mcb1.bln"), mcb1);
            CommandSet.Out.WriteLine("Finished saving MCB");

            CommandSet.Out.WriteLine("File replacement complete.");

            return 0;
        }
    }
}
