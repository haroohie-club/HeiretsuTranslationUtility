using HaruhiHeiretsuLib;
using HaruhiHeiretsuLib.Archive;
using HaruhiHeiretsuLib.Data;
using HaruhiHeiretsuLib.Graphics;
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
        private string _mcb, _dat, _evt, _grp, _scr, _fontReplacement, _replacementDir, _outputDir;
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
                { "f|font-replacement=", "Path to font replacement JSON (optional, used in script compilation)", f => _fontReplacement = f },
                { "r|replacement=", "Path to replacement directory", r => _replacementDir = r },
                { "o|output=", "Path to output directory", o => _outputDir = o },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {Options.Parse(arguments);
            McbArchive mcb = Program.GetMcbFile(_mcb);
            BinArchive<FileInArchive> dat = BinArchive<FileInArchive>.FromFile(_dat);
            BinArchive<EventFile> evt = BinArchive<EventFile>.FromFile(_evt);
            BinArchive<GraphicsFile> grp = BinArchive<GraphicsFile>.FromFile(_grp);
            BinArchive<ScriptFile> scr = BinArchive<ScriptFile>.FromFile(_scr);
            Dictionary<McbArchive.ArchiveIndex, bool> archivesEdited = new() { { McbArchive.ArchiveIndex.DAT, false }, { McbArchive.ArchiveIndex.EVT, false }, { McbArchive.ArchiveIndex.GRP, false }, { McbArchive.ArchiveIndex.SCR, false } };

            Regex archiveRegex = new(@"(?<archiveName>dat|evt|grp|scr)-(?<archiveIndex>\d{4})");
            foreach (string file in Directory.GetFiles(_replacementDir, "*", SearchOption.AllDirectories))
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

                    grp.Files.First(f => f.Index == archiveIndex).Set20AF30Image(bitmap);

                    int i = mcb.GraphicsFiles.Count;
                    mcb.LoadGraphicsFiles(file.Split('_'));
                    for (; i < mcb.GraphicsFiles.Count; i++)
                    {
                        GraphicsFile g = mcb.McbSubArchives[mcb.GraphicsFiles[i].parentLoc].Files[mcb.GraphicsFiles[i].childLoc].CastTo<GraphicsFile>();
                        g.Set20AF30Image(bitmap);
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

                    FontReplacementMap fontReplacementMap = null;
                    if (!string.IsNullOrEmpty(_fontReplacement))
                    {
                        fontReplacementMap = FontReplacementMap.FromJson(File.ReadAllText(_fontReplacement));
                    }

                    ScriptFile scriptFile = new() { AvailableCommands = ScriptCommand.ParseScriptCommandFile(scr.Files[1].Data.ToArray()) };
                    scriptFile.Compile(File.ReadAllText(file), fontReplacementMap);
                    List<byte> data = scriptFile.Data;

                    List<(int, int)> loadedFileLocations = McbArchive.GetFilesToLoad(file.Split('_'));
                    foreach((int parentLoc, int childLoc) in loadedFileLocations)
                    {
                        mcb.McbSubArchives[parentLoc].Files[childLoc].Edited = true;
                        mcb.McbSubArchives[parentLoc].Files[childLoc].Data = data.ToList();
                    }

                    archivesEdited[McbArchive.ArchiveIndex.SCR] = true;
                    scr.Files.First(f => f.Index == archiveIndex).Edited = true;
                    scr.Files.First(f => f.Index == archiveIndex).Data = data.ToList();

                    CommandSet.Out.WriteLine($"Finished replacing {Path.GetFileName(file)} in MCB & SCR");
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

                        grp.Files.First(f => f.Index == archiveIndex).SetMapData(mapEntries);

                        int i = mcb.GraphicsFiles.Count;
                        mcb.LoadGraphicsFiles(file.Split('_'));
                        for (; i < mcb.GraphicsFiles.Count; i++)
                        {
                            mcb.McbSubArchives[mcb.GraphicsFiles[i].parentLoc].Files[mcb.GraphicsFiles[i].childLoc].Data = grp.Files.First(f => f.Index == archiveIndex).Data;
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

                        FileInArchive currentFile = dat.Files.First(f => f.Index == archiveIndex);
                        List<byte> data = new();

                        if (archiveIndex == 36)
                        {
                            CameraDataFile cameraDataFile = new(File.ReadAllLines(file));
                            data = cameraDataFile.GetBytes().ToList();
                        }
                        else if (archiveIndex == 58)
                        {
                            MapDefinitionsFile mapDefinitionsFile = new(File.ReadAllLines(file), currentFile.Index, currentFile.Offset);
                            data = mapDefinitionsFile.GetBytes().ToList();
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
                            mcb.McbSubArchives[parentLoc].Files[childLoc].Data = data.ToList();
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
                        mcb.McbSubArchives[parentLoc].Files[childLoc].Data = data.ToList();
                    }

                    switch (archive)
                    {
                        case "dat":
                            archivesEdited[McbArchive.ArchiveIndex.DAT] = true;
                            dat.Files.First(f => f.Index == archiveIndex).Edited = true;
                            dat.Files.First(f => f.Index == archiveIndex).Data = data.ToList();
                            break;
                        case "evt":
                            archivesEdited[McbArchive.ArchiveIndex.EVT] = true;
                            evt.Files.First(f => f.Index == archiveIndex).Edited = true;
                            evt.Files.First(f => f.Index == archiveIndex).Data = data.ToList();
                            break;
                        case "grp":
                            archivesEdited[McbArchive.ArchiveIndex.GRP] = true;
                            grp.Files.First(f => f.Index == archiveIndex).Edited = true;
                            grp.Files.First(f => f.Index == archiveIndex).Data = data.ToList();
                            break;
                        case "scr":
                            archivesEdited[McbArchive.ArchiveIndex.SCR] = true;
                            scr.Files.First(f => f.Index == archiveIndex).Edited = true;
                            scr.Files.First(f => f.Index == archiveIndex).Data = data.ToList();
                            break;
                    }

                    CommandSet.Out.WriteLine($"Finished replacing {Path.GetFileName(file)} in MCB & {archive.ToUpper()}");
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

            return 0;
        }
    }
}
