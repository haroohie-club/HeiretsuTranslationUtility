using HaruhiHeiretsuLib.Archive;
using Mono.Options;
using System.Collections.Generic;
using System.IO;

namespace HaruhiHeiretsuCLI
{
    public class ExtractMcbArchiveCommand : Command
    {
        private string _mcb, _outputDirectory;
        private int _archiveIndex;

        public ExtractMcbArchiveCommand() : base("extract-mcb-archive")
        {
            Options = new()
            {
                "Extract all files in a particular BlnSub archive from the MCB to a directory",
                "Usage: HaruhiHeiretsuCLI extract-archive -m [MCB_PATH] -i [ARCHIVE_INDEX] -o [OUTPUT_DIRECTORY]",
                "",
                { "m|mcb=", "Path to mcb0.bln", m => _mcb = m },
                { "i|index=", "Index of BlnSub archive to extract from MCB", i => _archiveIndex = int.Parse(i) },
                { "o|output=", "Output directory", o => _outputDirectory = o },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);

            McbArchive mcb = Program.GetMcbFile(_mcb);

            if (!Directory.Exists(_outputDirectory))
            {
                Directory.CreateDirectory(_outputDirectory);
            }

            for (int i = 0; i < mcb.McbSubArchives[_archiveIndex].Files.Count; i++)
            {
                File.WriteAllBytes(Path.Combine(_outputDirectory, $"{i:D3}.bin"), mcb.McbSubArchives[_archiveIndex].Files[i].GetBytes());
            }

            return 0;
        }
    }
}
