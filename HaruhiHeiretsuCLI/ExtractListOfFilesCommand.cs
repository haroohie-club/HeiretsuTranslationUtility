using HaruhiHeiretsuLib;
using Kontract.Models.Archive;
using Mono.Options;
using plugin_shade.Archives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace HaruhiHeiretsuCLI
{
    public class ExtractListOfFilesCommand : Command
    {
        private string _mcb, _locationsFile, _outputDirectory;

        public ExtractListOfFilesCommand() : base("extract-list-of-files")
        {
            Options = new()
            {
                "Extract a list of files from the MCB as specified in an input list",
                "Usage: HaruhiHeiretsuCLI extract-list-of-files -m [MCB_PATH] -l [LIST_FILE] -o [OUTPUT_DIRECTORY]",
                "",
                { "m|mcb=", "Path to mcb0.bln", m => _mcb = m },
                { "l|file-list=", "Path to a file containing a list of files to extract", l => _locationsFile = l },
                { "o|output=", "Path of directory where files will be extracted to", o => _outputDirectory = o },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);

            McbFile mcb = Program.GetMcbFile(_mcb);

            if (!Directory.Exists(_outputDirectory))
            {
                Directory.CreateDirectory(_outputDirectory);
            }

            string fileLocations = File.ReadAllText(_locationsFile);
            foreach (string line in fileLocations.Split("\r\n"))
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }
                string[] lineSplit = line.Split(',');
                int parentLoc = int.Parse(lineSplit[0]);
                int childLoc = int.Parse(lineSplit[1]);

                using Stream fileStream = mcb.ArchiveFiles[parentLoc].GetFileData().GetAwaiter().GetResult();
                BlnSub blnSub = new();
                IArchiveFileInfo blnSubFile = blnSub.GetFile(fileStream, childLoc);

                byte[] subFileData = blnSubFile.GetFileDataBytes();

                File.WriteAllBytes(Path.Combine(_outputDirectory, $"{parentLoc}-{childLoc}.bin"), subFileData);
                CommandSet.Out.WriteLine($"Wrote file {parentLoc}-{childLoc}.bin");
            }

            return 0;
        }
    }
}
