using HaruhiHeiretsuLib;
using Kontract.Models.Archive;
using Mono.Options;
using plugin_shade.Archives;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace HaruhiHeiretsuCLI
{
    public class ExtractArchiveCommand : Command
    {
        private string _mcb, _outputDirectory;
        private int _archiveIndex;

        public ExtractArchiveCommand() : base("extract-archive")
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
            return InvokeAsync(arguments).GetAwaiter().GetResult();
        }

        public async Task<int> InvokeAsync(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);

            McbFile mcb = Program.GetMcbFile(_mcb);

            if (!Directory.Exists(_outputDirectory))
            {
                Directory.CreateDirectory(_outputDirectory);
            }

            using Stream archiveStream = await mcb.ArchiveFiles[_archiveIndex].GetFileData();
            BlnSub blnSub = new();
            List<IArchiveFileInfo> blnSubFiles = (List<IArchiveFileInfo>)blnSub.Load(archiveStream);

            for (int i = 0; i < blnSubFiles.Count; i++)
            {
                File.WriteAllBytes(Path.Combine(_outputDirectory, $"{i:D3}.bin"), blnSubFiles[i].GetFileDataBytes());
            }

            return 0;
        }
    }
}
