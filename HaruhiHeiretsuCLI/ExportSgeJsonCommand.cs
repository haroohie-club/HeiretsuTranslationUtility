using HaruhiHeiretsuLib.Archive;
using HaruhiHeiretsuLib.Data;
using HaruhiHeiretsuLib.Graphics;
using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HaruhiHeiretsuCLI
{
    public class ExportSgeJsonCommand : Command
    {
        private string _grp, _dat, _sgeName, _outputFile;
        private int _grpIndex = 0;
        public ExportSgeJsonCommand() : base("export-sge-json")
        {
            Options = new()
            {
                "Export an SGE model's JSON data for importing into Blender; SGE can be specified by file name or by GRP index",
                "Usage: HaruhiHeiretsuCLI export-sge-json -g GRP.BIN -d DAT.BIN [-n SGE_NAME] [-i GRP_INDEX]",
                "",
                { "g|grp=", "grp.bin file", g => _grp = g },
                { "d|dat=", "dat.bin file", d => _dat = d },
                { "n|name|sge-name=", "Name of the SGE file to export", n => _sgeName = n },
                { "i|index|grp-index=", "Index of the file in grp.bin", i => _grpIndex = int.Parse(i) },
                { "o|output=", "Output file location; defaults to the name of the file in the current directory", o => _outputFile = o },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);

            CommandSet.Out.WriteLine($"Loading grp.bin from {_grp}...");
            BinArchive<GraphicsFile> grp = BinArchive<GraphicsFile>.FromFile(_grp);
            CommandSet.Out.WriteLine($"Loading dat.bin from {_dat}...");
            BinArchive<DataFile> dat = BinArchive<DataFile>.FromFile(_dat);

            byte[] graphicsFileNameMap = dat.Files.First(f => f.Index == 8).GetBytes();
            int numGraphicsFiles = BitConverter.ToInt32(graphicsFileNameMap.Skip(0x10).Take(4).Reverse().ToArray());

            Dictionary<int, string> indexToNameMap = new();
            for (int i = 0; i < numGraphicsFiles; i++)
            {
                indexToNameMap.Add(BitConverter.ToInt32(graphicsFileNameMap.Skip(0x14 * (i + 1)).Take(4).Reverse().ToArray()), Encoding.ASCII.GetString(graphicsFileNameMap.Skip(0x14 * (i + 1) + 0x04).TakeWhile(b => b != 0x00).ToArray()));
            }

            foreach (GraphicsFile file in grp.Files)
            {
                file.TryResolveName(indexToNameMap);
            }

            GraphicsFile sgeFile;
            if (!string.IsNullOrEmpty(_sgeName))
            {
                foreach (GraphicsFile file in grp.Files)
                {
                    if (file.FileType == GraphicsFile.GraphicsFileType.SGE)
                    {
                        file.Sge.ResolveTextures(file.Name, grp.Files);
                    }
                }

                sgeFile = grp.Files.First(f => f.Name == _sgeName);
            }
            else
            {
                sgeFile = grp.Files.First(f => f.Index == _grpIndex);
            }

            if (string.IsNullOrEmpty(_outputFile))
            {
                _outputFile = $"{sgeFile.Name}";
            }
            CommandSet.Out.WriteLine($"Dumping {sgeFile.Name} to {_outputFile}.sge.json...");

            File.WriteAllText($"{_outputFile}.sge.json", sgeFile.Sge.DumpJson());

            return 0;
        }
    }
}
