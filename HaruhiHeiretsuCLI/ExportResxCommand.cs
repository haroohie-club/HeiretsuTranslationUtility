using HaruhiHeiretsuLib.Archive;
using HaruhiHeiretsuLib.Strings;
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
        private string _stringFileMap, _dat, _evt, _scr, _outputDirectory;
        private Regex _binRegex = new Regex(@"(?<bin>dat|evt|scr)-(?<index>\-?\d{4})");
        public ExportResxCommand() : base("export-resx", "Exports RESX files from string files")
        {
            Options = new()
            {
                "Exports RESX files from a list of string files.",
                "",
                { "i|input|string-file-map=", "String file map", i => _stringFileMap = i },
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
            BinArchive<ShadeStringsFile> dat = BinArchive<ShadeStringsFile>.FromFile(_dat);
            BinArchive<EventFile> evt = BinArchive<EventFile>.FromFile(_evt);
            BinArchive<ScriptFile> scr = BinArchive<ScriptFile>.FromFile(_scr);

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
                        //FileInArchive originalFile = dat.Files.First(f => f.Index == index);
                        //ShadeStringsFile shadeStringsFile = new();
                        //shadeStringsFile.Initialize(originalFile.Data.ToArray(), originalFile.Offset);
                        //shadeStringsFile.WriteResxFile(fileName);
                        dat.Files.Last(f => f.Index == index).WriteResxFile(fileName);
                        break;

                    case "evt":
                        evt.Files.First(f => f.Index == index).WriteResxFile(fileName);
                        break;

                    case "scr":
                        scr.Files.First(f => f.Index == index).WriteResxFile(fileName);
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
