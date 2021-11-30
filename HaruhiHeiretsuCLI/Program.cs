using HaruhiHeiretsuLib;
using Kontract.Models.Archive;
using Mono.Options;
using plugin_shade.Archives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaruhiHeiretsuCLI
{
    class Program
    {
        public enum Mode
        {
            HELP,
            EXTRACT_SGE_FILES,
            EXTRACT_STRING_FILES,
            FIND_STRINGS,
            STRING_SEARCH,
        }

        public static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        public static async Task MainAsync( string[] args)
        {
            Mode mode = Mode.HELP;
            string file = "", output = "", search = "";

            OptionSet options = new()
            {
                "Usage: HaruhiHeiretsuCLI -f MCB_FILE",
                { "f|file=", f => file = f },
                { "o|output=", o => output = o },
                { "search=", s => search = s },
                { "find-strings", m => mode = Mode.FIND_STRINGS },
                { "string-search", m => mode = Mode.STRING_SEARCH },
                { "extract-sge-files", m => mode = Mode.EXTRACT_SGE_FILES },
                { "extract-string-files", m => mode = Mode.EXTRACT_STRING_FILES },
                { "h|help", m => mode = Mode.HELP },
            };

            options.Parse(args);

            if (mode == Mode.HELP)
            {
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            string indexFile = "", dataFile = "";
            if (file.Contains("0"))
            {
                indexFile = file;
                dataFile = file.Replace("0", "1");
            }
            else
            {
                indexFile = file.Replace("1", "0");
                dataFile = file;
            }

            McbFile mcb = new(indexFile, dataFile);

            if (mode == Mode.EXTRACT_SGE_FILES)
            {
                await ExtractSgeFiles(mcb, output);
            }
            else if (mode == Mode.EXTRACT_STRING_FILES)
            {
                ExtractStringFiles(mcb, output);
            }
            else if (mode == Mode.FIND_STRINGS)
            {
                await FindStrings(mcb);
            }
            else if (mode == Mode.STRING_SEARCH)
            {
                await StringSearch(mcb, search);
            }
        }

        public static async Task FindStrings(McbFile mcb)
        {
            List<(int, int)> stringFileLocations = await mcb.FindStringFiles();
            using StreamWriter fs = File.CreateText("string_file_locations.csv");
            foreach ((int file, int subFile) in stringFileLocations)
            {
                fs.WriteLine($"{file},{subFile}");
            }
        }

        public static void ExtractStringFiles(McbFile mcb, string outputDirectory)
        {
            string stringFileLocations = File.ReadAllText("string_file_locations.csv");
            foreach (string line in stringFileLocations.Split("\r\n"))
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

                File.WriteAllBytes(Path.Combine(outputDirectory, $"{parentLoc}-{childLoc}.bin"), subFileData);
                Console.WriteLine($"Wrote file {parentLoc}-{childLoc}.bin");
            }
        }

        public static async Task ExtractSgeFiles(McbFile mcb, string outputDirectory)
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            for ( int i = 0; i < mcb.ArchiveFiles.Count; i++)
            {
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
                        if (!Directory.Exists(Path.Combine(outputDirectory, $"{i:D3}")))
                        {
                            Directory.CreateDirectory(Path.Combine(outputDirectory, $"{i:D3}"));
                        }

                        byte[] data = blnSubFiles[j].GetFileDataBytes();

                        // get (first) model name
                        string name = Encoding.ASCII.GetString(data.Skip(0xF0)
                            .SkipWhile(b => (b < '0' || b > '9') && (b < 'A' || b > 'Z'))
                            .TakeWhile(b => (b >= '0' && b <= '9') || (b >= 'A' && b <= 'Z') || b == '_').ToArray());

                        File.WriteAllBytes(Path.Combine(outputDirectory, $"{i:D3}", $"{j:D3}-{name}.sge"), data);
                        Console.WriteLine($"Extracted SGE file {i:D3}/{j:D3}-{name}.sge");
                    }
                }
                Console.WriteLine($"Finished searching {blnSubFiles.Count} files in {i:D3}.bin");
            }
        }

        public static async Task StringSearch(McbFile mcb, string search)
        {
            await mcb.FindStringInFiles(search);
        }
    }
}
