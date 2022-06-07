using HaruhiHeiretsuLib;
using Mono.Options;
using System.IO;

namespace HaruhiHeiretsuCLI
{
    class Program
    {
        public static int Main(string[] args)
        {
            CommandSet commands = new("HaruhiHeiretsuCLI")
            {
                "Usage: HaruhiHeiretscuCLI COMMAND [OPTIONS]",
                "",
                "Available commands:",
                new CheckBlnBinIntegrityCommand(),
                new ExportFileMapCommand(),
                new ExportResxCommand(),
                new ExtractArchiveCommand(),
                new ExtractListOfFilesCommand(),
                new ExtractSgeFilesCommand(),
                new ExtractStringFilesCommand(),
                new FindStringsCommand(),
                new HexSearchCommand(),
                new ReplaceFilesCommand(),
                new ReplaceFontCommand(),
                new StringSearchCommand(),
                new GeneratePatchCommand(),
            };

            return commands.Run(args);
        }

        public static McbFile GetMcbFile(string mcbPath)
        {
            string indexFile, dataFile;
            if (Path.GetFileName(mcbPath).Contains('0'))
            {
                indexFile = mcbPath;
                dataFile = Path.Combine(Path.GetDirectoryName(mcbPath), Path.GetFileName(mcbPath).Replace("0", "1"));
            }
            else
            {
                indexFile = Path.Combine(Path.GetDirectoryName(mcbPath), Path.GetFileName(mcbPath).Replace("1", "0"));
                dataFile = mcbPath;
            }

            return new McbFile(indexFile, dataFile);
        }
    }
}
