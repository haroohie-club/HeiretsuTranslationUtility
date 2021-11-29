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
    class Program
    {
        public enum Mode
        {
            EXTRACT_STRING_FILES,
            FIND_STRINGS,
            STRING_SEARCH,
        }

        public static void Main(string[] args)
        {
            Mode mode = Mode.EXTRACT_STRING_FILES;
            string file = "", output = "", search = "";

            OptionSet options = new()
            {
                "Usage: HaruhiHeiretsuCLI -f MCB_FILE",
                { "f|file=", f => file = f },
                { "o|output=", o => output = o },
                { "search=", s => search = s },
                { "find-strings", m => mode = Mode.FIND_STRINGS },
                { "string-search", m => mode = Mode.STRING_SEARCH },
                { "extract-string-files", m => mode = Mode.EXTRACT_STRING_FILES },
            };

            options.Parse(args);

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

            MainAsync(mode, indexFile, dataFile, output, search).GetAwaiter().GetResult();
        }

        public static async Task MainAsync(Mode mode, string indexFile, string dataFile, string output, string search)
        {
            McbFile mcb = new(indexFile, dataFile);

            if (mode == Mode.FIND_STRINGS)
            {
                await FindStrings(mcb);
            }
            else if (mode == Mode.EXTRACT_STRING_FILES)
            {
                ExtractFiles(mcb, output);
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

        public static void ExtractFiles(McbFile mcb, string outputDirectory)
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
                BlnSub blnSub = new BlnSub();
                IArchiveFileInfo blnSubFile = blnSub.GetFile(fileStream, childLoc);

                byte[] subFileData = blnSubFile.GetFileDataBytes();

                File.WriteAllBytes(Path.Combine(outputDirectory, $"{parentLoc}-{childLoc}.bin"), subFileData);
                Console.WriteLine($"Wrote file {parentLoc}-{childLoc}.bin");
            }
        }

        public static async Task StringSearch(McbFile mcb, string search)
        {
            await mcb.FindStringInFiles(search);
        }
    }
}
