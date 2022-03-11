using HaruhiHeiretsuLib;
using Kontract.Models.Archive;
using Mono.Options;
using plugin_shade.Archives;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

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
                new ExportFileMapCommand(),
                new ExtractArchiveCommand(),
                new ExtractListOfFilesCommand(),
                new ExtractSgeFilesCommand(),
                new ExtractStringFilesCommand(),
                new FindStringsCommand(),
                new HexSearchCommand(),
                new ReplaceGraphicsCommand(),
                new StringSearchCommand(),
                new GeneratePatchCommand()
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
