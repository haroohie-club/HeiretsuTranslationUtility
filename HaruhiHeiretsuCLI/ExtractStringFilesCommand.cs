using Mono.Options;
using System.Collections.Generic;
using System.Linq;

namespace HaruhiHeiretsuCLI
{
    public class ExtractStringFilesCommand : Command
    {
        public ExtractStringFilesCommand() : base("extract-string-files")
        {
            Options = new()
            {
                "Extract all string files from the MCB",
                "Usage: HaruhiHeiretsuCLI extract-string-files -m [MCB_PATH] -o [OUTPUT_DIRECTORY]",
                "",
                { "m|mcb=", "Path to mcb0.bln", m => _ = m },
                { "o|output=", "Path of directory where string files will be extracted to", o => _ = o },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            List<string> argumentsList = arguments.ToList();
            argumentsList.AddRange(new string[] { "-l", "string_file_locations.csv" });
            return new ExtractListOfFilesCommand().Invoke(argumentsList);
        }
    }
}
