using HaruhiHeiretsuLib.Archive;
using Mono.Options;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace HaruhiHeiretsuCLI
{
    internal class ExportFileMapCommand : Command
    {
        private string _mcb, _binFile, _outputFile;

        public ExportFileMapCommand() : base("export-file-map")
        {
            Options =
            [
                "Export a map of all files between a specified bin and the MCB",
                "Usage: HaruhiHeiretsuCLI export-file-map -m [MCB_PATH] -b [BIN_FILE] -o [OUTPUT_FILE]",
                "",
                { "m|mcb=", "Path to mcb0.bln", m => _mcb = m },
                { "b|bin=", "Path to the bin archive", b => _binFile = b },
                { "o|output=", "Output path of file map", o => _outputFile = o },
            ];
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);

            McbArchive mcb = Program.GetMcbFile(_mcb);

            Dictionary<int, List<(int, int)>> fileMap = mcb.GetFileMap(_binFile);
            string binIdentifier = Path.GetFileNameWithoutExtension(_binFile);
            List<string> fileNames = [];

            foreach (int binIndex in fileMap.Keys)
            {
                string fileName = "";
                foreach ((int parent, int child) locationPair in fileMap[binIndex])
                {
                    fileName += $"{locationPair.parent:D3}-{locationPair.child:D3}_";
                }
                fileName += $"{binIdentifier}-{binIndex:D4}";
                fileNames.Add(fileName);
            }

            File.WriteAllLines(_outputFile, fileNames);

            return 0;
        }
    }
}
