using HaruhiHeiretsuLib.Archive;
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
                new CalculatorCommand(),
                new CheckBlnBinIntegrityCommand(),
                new ExportFileMapCommand(),
                new ExportResxCommand(),
                new ExportSgeJsonCommand(),
                new ExtractArchiveCommand(),
                new GenerateFontReplacementCommand(),
                new GeneratePatchCommand(),
                new HexSearchCommand(),
                new ReplaceFilesCommand(),
                new ReplaceFontCommand(),
                new StringSearchCommand(),
            };

            return commands.Run(args);
        }

        public static McbArchive GetMcbFile(string mcbPath)
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

            return new McbArchive(indexFile, dataFile);
        }
    }
}
