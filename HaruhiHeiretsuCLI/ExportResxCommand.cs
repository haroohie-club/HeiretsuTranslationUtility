using HaruhiHeiretsuLib.Archive;
using HaruhiHeiretsuLib.Data;
using HaruhiHeiretsuLib.Strings.Data;
using HaruhiHeiretsuLib.Strings.Events;
using HaruhiHeiretsuLib.Strings.Scripts;
using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace HaruhiHeiretsuCLI
{
    public class ExportResxCommand : Command
    {
        private string _stringFileMap, _mcb, _dat, _evt, _scr, _outputDirectory;
        private Regex _binRegex = new Regex(@"(?<bin>dat|evt|scr)-(?<index>\-?\d{4})");
        public ExportResxCommand() : base("export-resx", "Exports RESX files from string files")
        {
            Options = new()
            {
                "Exports RESX files from a list of string files.",
                "",
                { "i|input|string-file-map=", "String file map", i => _stringFileMap = i },
                { "m|mcb=", "Input mcb0.bln", m => _mcb = m },
                { "d|dat=", "Input dat.bin", d => _dat = d },
                { "e|evt=", "Input evt.bin", e => _evt = e },
                { "s|scr=", "input scr.bin", s => _scr = s },
                { "o|output-directory=", "Output directory for resx files", o => _outputDirectory = o },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);

            string[] files = File.ReadAllLines(_stringFileMap);
            McbArchive mcb = Program.GetMcbFile(_mcb);
            BinArchive<DataFile> dat = BinArchive<DataFile>.FromFile(_dat);
            BinArchive<EventFile> evt = BinArchive<EventFile>.FromFile(_evt);
            BinArchive<ScriptFile> scr = BinArchive<ScriptFile>.FromFile(_scr);

            byte[] scriptNameList = scr.Files[0].GetBytes();
            List<string> scriptNames = ScriptFile.ParseScriptListFile(scriptNameList);
            Dictionary<int, string> indexToNameMap = scriptNames.ToDictionary(keySelector: n => scriptNames.IndexOf(n) + 1);
            List<ScriptCommand> commands = ScriptCommand.ParseScriptCommandFile(scr.Files[1].GetBytes());

            MapDefinitionsFile mapDefinitionsFile = dat.Files.First(f => f.Index == 58).CastTo<MapDefinitionsFile>();

            List<(string, string, int)> binMap = files.Select(f =>
            {
                Match match = _binRegex.Match(f);
                return (f, match.Groups["bin"].Value, int.Parse(match.Groups["index"].Value));
            }).ToList();

            foreach ((string name, string bin, int index) in binMap)
            {
                string fileName = Path.Combine(_outputDirectory, $"{name}.ja.resx");
                switch (bin.ToLower())
                {
                    case "dat":
                        switch (index)
                        {
                            case DataStringsFileLocations.MAP_DEFINITION_INDEX:
                                dat.Files.Last(f => f.Index == index).CastTo<DataStringsFile<MapDefinitionsFile>>().WriteResxFile(fileName);
                                break;
                            case DataStringsFileLocations.TOPICS_FLAGS_INDEX:
                                dat.Files.Last(f => f.Index == index).CastTo<DataStringsFile<TopicsAndFlagsFile>>().WriteResxFile(fileName);
                                break;
                            case DataStringsFileLocations.NAMEPLATES_INDEX:
                                dat.Files.Last(f => f.Index == index).CastTo<DataStringsFile<NameplatesFile>>().WriteResxFile(fileName);
                                break;
                            case DataStringsFileLocations.TIMELINE_INDEX:
                                dat.Files.Last(f => f.Index == index).CastTo<DataStringsFile<TimelineFile>>().WriteResxFile(fileName);
                                break;
                            default:
                                dat.Files.Last(f => f.Index == index).CastTo<ShadeStringsFile>().WriteResxFile(fileName);
                                break;
                        }
                        break;

                    case "evt":
                        evt.Files.First(f => f.Index == index).WriteResxFile(fileName);
                        break;

                    case "scr":
                        ScriptFile scrFile = scr.Files.First(f => f.Index == index);
                        scrFile.Name = indexToNameMap[scrFile.Index];
                        fileName = $"{name}_{scrFile.Name}.ja.resx";

                        int mapSection, mapIndex;
                        if (char.IsNumber(name[0]))
                        {
                            int mcbId = mcb.McbSubArchives[int.Parse(name[0..3])].Id;
                            mapSection = ((mcbId >> 8) ^ 0x40);
                            mapIndex = mcbId & 0xFF;
                        }
                        else
                        {
                            if (scrFile.Name == "SCRCLB99")
                            {
                                mapSection = 12;
                                mapIndex = 0;
                            }
                            else
                            {
                                mapSection = 13;
                                if (scrFile.Name.Equals("SCRSAMPLE0", StringComparison.OrdinalIgnoreCase))
                                {
                                    mapIndex = 0;
                                }
                                else if (scrFile.Name.Equals("SCRTest_Hanai", StringComparison.OrdinalIgnoreCase))
                                {
                                    mapIndex = 1;
                                }
                                else if (scrFile.Name.Equals("SCRTest_Kwmt", StringComparison.OrdinalIgnoreCase))
                                {
                                    mapIndex = 2;
                                }
                                else if (scrFile.Name.Equals("SCRSAMPLE1", StringComparison.OrdinalIgnoreCase))
                                {
                                    mapIndex = 2;
                                }
                                else if (scrFile.Name.StartsWith("SCRSCR_IR", StringComparison.OrdinalIgnoreCase))
                                {
                                    mapIndex = int.Parse(scrFile.Name[9..]) + 8;
                                }
                                else
                                {
                                    mapIndex = 0;
                                }
                            }
                        }

                        MapDefinition mapDef = mapDefinitionsFile.Sections[mapSection - 2].MapDefinitions[mapIndex];
                        scrFile.AvailableCommands = commands;
                        scrFile.PopulateCommandBlocks(mapDef.Evts);
                        scrFile.WriteResxFile(fileName);
                        break;

                    default:
                        throw new ArgumentException($"Invalid bin specified");
                }
                CommandSet.Out.WriteLine($"Wrote {name}.ja.resx to disk.");
            }

            return 0;
        }
    }
}
