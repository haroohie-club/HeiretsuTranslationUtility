using HaruhiHeiretsuLib;
using Kontract.Models.Archive;
using Mono.Options;
using plugin_shade.Archives;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaruhiHeiretsuCLI
{
    public class ExtractSgeFilesCommand : Command
    {
        private string _mcb, _outputDirectory;

        public ExtractSgeFilesCommand() : base("extract-sge-files")
        {
            Options = new()
            {
                "Extract all SGE files from the MCB",
                "Usage: HaruhiHeiretsuCLI extract-sge-files -m [MCB_FILE] -o [OUTPUT_DIRECTORY]",
                "",
                { "m|mcb=", "Path to mcb0.bln", m => _mcb = m },
                { "o|output=", "Path of directory where files will be extracted to", o => _outputDirectory = o },
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

            for (int i = 0; i < mcb.ArchiveFiles.Count; i++)
            {
                short mcbId = ((BlnArchiveFileInfo)mcb.ArchiveFiles[i]).Entry.id;
                using Stream archiveStream = await mcb.ArchiveFiles[i].GetFileData();
                BlnSub blnSub = new();
                List<IArchiveFileInfo> blnSubFiles = (List<IArchiveFileInfo>)blnSub.Load(archiveStream);

                for (int j = 0; j < blnSubFiles.Count; j++)
                {
                    using Stream fileStream = await blnSubFiles[j].GetFileData();
                    byte[] initBytes = new byte[3];
                    fileStream.Read(initBytes, 0, initBytes.Length);
                    fileStream.Position = 0;

                    if (Encoding.ASCII.GetString(initBytes) == "SGE")
                    {
                        if (!Directory.Exists(Path.Combine(_outputDirectory, $"{mcbId:X4}")))
                        {
                            Directory.CreateDirectory(Path.Combine(_outputDirectory, $"{mcbId:X4}"));
                        }

                        byte[] data = blnSubFiles[j].GetFileDataBytes();

                        // get (first) model name
                        string name = Encoding.ASCII.GetString(data.Skip(0xF0)
                            .SkipWhile(b => (b < '0' || b > '9') && (b < 'A' || b > 'Z'))
                            .TakeWhile(b => (b >= '0' && b <= '9') || (b >= 'A' && b <= 'Z') || b == '_').ToArray());

                        File.WriteAllBytes(Path.Combine(_outputDirectory, $"{mcbId:X4}", $"{j:D4}-{name}.sge"), data);
                        CommandSet.Out.WriteLine($"Extracted SGE file {mcbId:X4}/{j:D4}-{name}.sge");
                    }
                }
                CommandSet.Out.WriteLine($"Finished searching {blnSubFiles.Count} files in {mcbId:X4}.bin");
            }

            return 0;
        }
    }
}
