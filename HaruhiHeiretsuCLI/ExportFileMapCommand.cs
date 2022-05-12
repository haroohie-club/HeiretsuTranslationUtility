using HaruhiHeiretsuLib;
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
            Options = new()
            {
                "Export a map of all files between a specified bin and the MCB",
                "Usage: HaruhiHeiretsuCLI export-file-map -m [MCB_PATH] -b [BIN_FILE] -o [OUTPUT_FILE]",
                "",
                { "m|mcb=", "Path to mcb0.bln", m => _mcb = m },
                { "b|bin=", "Path to the bin archive", b => _binFile = b },
                { "o|output=", "Output path of file map", o => _outputFile = o },
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

            Dictionary<int, List<(short, int)>> fileMap = await mcb.GetFileMap(_binFile);
            string binIdentifier = Path.GetFileNameWithoutExtension(_binFile);
            List<string> fileNames = new();

            foreach (int binIndex in fileMap.Keys)
            {
                string fileName = "";
                foreach ((short mcbId, int child) locationPair in fileMap[binIndex])
                {
                    fileName += $"{locationPair.mcbId:X4}-{locationPair.child:D4}_";
                }
                fileName += $"{binIdentifier}-{binIndex:D4}";
                fileNames.Add(fileName);
            }

            File.WriteAllLines(_outputFile, fileNames);

            return 0;
        }
    }
}
