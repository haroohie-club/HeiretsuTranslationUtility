using HaruhiHeiretsuLib;
using Kontract.Models.Archive;
using Mono.Options;
using plugin_shade.Archives;
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
        public enum Mode
        {
            HELP,
            EXPORT_FILE_MAP,
            EXTRACT_ARCHIVE,
            EXTRACT_LIST_OF_FILES,
            EXTRACT_SGE_FILES,
            EXTRACT_STRING_FILES,
            FIND_STRINGS,
            GENERATE_PATCH,
            HEX_SEARCH,
            REPLACE_GRAPHICS,
            REPLACE_STRINGS,
            STRING_SEARCH,
        }

        public static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        public static async Task MainAsync( string[] args)
        {
            Mode mode = Mode.HELP;
            string mcbFile = "", binFile = "", output = "", search = "", fileList = "", replacementFolder = "";
            int archiveIndex = 0;

            OptionSet options = new()
            {
                "Usage: HaruhiHeiretsuCLI -f MCB_FILE",
                { "m|mcb-file=", "MCB file (either mcb0.bln or mcb1.bln)", m => mcbFile = m },
                { "b|bin-file=", "BIN archive file (e.g. grp.bin or scr.bin)", b => binFile = b},
                { "o|output=", "Output file or directory", o => output = o },
                { "s|search=", "Query to search for in files", s => search = s },
                { "l|file-list=", "List of files to use", l => fileList = l },
                { "i|archive-index=", "Index of archive in mcb to search during search operation", i => archiveIndex = int.Parse(i) },
                { "r|replacement-folder=", "Folder to pull replacement files from during replacement operation", r => replacementFolder = r },
                { "export-file-map", "Export a map of all files between a specified bin and the MCB", m => mode = Mode.EXPORT_FILE_MAP },
                { "extract-archive", "Extract all files in a particular BlnSub archive from the MCB", m => mode = Mode.EXTRACT_ARCHIVE },
                { "extract-list-of-files", "Extract a list of files from the MCB as specified in -l|--file-list", m => mode = Mode.EXTRACT_LIST_OF_FILES },
                { "extract-sge-files", "Extract all SGE files from the MCB", m => mode = Mode.EXTRACT_SGE_FILES },
                { "extract-string-files", "Extract all string files from the MCB", m => mode = Mode.EXTRACT_STRING_FILES },
                { "hex-search", "Perform a search for a hex string at the beginning of all files in the MCB", m => mode = Mode.HEX_SEARCH },
                { "find-strings", "Perform a search for script files in the MCB", m => mode = Mode.FIND_STRINGS },
                { "replace-graphics", "Replace graphics files with PNG files from -r|--replacement-folder in the MCB and grp.bin", m => mode = Mode.REPLACE_GRAPHICS },
                { "replace-strings", "Replace script file strings with RESX files from -r|--replacement-folder in the MCB and scr.bin", m => mode = Mode.REPLACE_STRINGS },
                { "string-search", "Perform a search for a string anywhere in all files in the MCB", m => mode = Mode.STRING_SEARCH },
                { "generate-patch", "Generate the base Riivolution patch", m => mode = Mode.GENERATE_PATCH },
                { "h|help", "Display help", m => mode = Mode.HELP },
            };

            options.Parse(args);

            if (mode == Mode.HELP)
            {
                options.WriteOptionDescriptions(Console.Out);
                return;
            }
            else if (mode == Mode.GENERATE_PATCH)
            {
                GeneratePatch(output);
                return;
            }

            string indexFile = "", dataFile = "";
            if (mcbFile.Contains("0"))
            {
                indexFile = mcbFile;
                dataFile = mcbFile.Replace("0", "1");
            }
            else
            {
                indexFile = mcbFile.Replace("1", "0");
                dataFile = mcbFile;
            }

            McbFile mcb = new(indexFile, dataFile);

            ArchiveFile<GraphicsFile> grp = null;
            ArchiveFile<ScriptFile> scr = null;
            if (!string.IsNullOrEmpty(binFile))
            {
                if (binFile.EndsWith("grp.bin"))
                {
                    grp = ArchiveFile<GraphicsFile>.FromFile(binFile);
                }
                else if (binFile.EndsWith("scr.bin"))
                {
                    scr = ArchiveFile<ScriptFile>.FromFile(binFile);
                }
            }

            switch (mode)
            {
                case Mode.EXPORT_FILE_MAP:
                    await ExportFileMap(mcb, binFile, output);
                    break;

                case Mode.EXTRACT_ARCHIVE:
                    await ExtractArchive(mcb, output, archiveIndex);
                    break;

                case Mode.EXTRACT_LIST_OF_FILES:
                    ExtractListOfFiles(mcb, output, fileList);
                    break;

                case Mode.EXTRACT_SGE_FILES:
                    await ExtractSgeFiles(mcb, output);
                    break;

                case Mode.EXTRACT_STRING_FILES:
                    ExtractListOfFiles(mcb, output);
                    break;

                case Mode.FIND_STRINGS:
                    await FindStrings(mcb);
                    break;

                case Mode.HEX_SEARCH:
                    List<byte> searchBytes = new();
                    for (int i = 0; i < search.Length; i += 2)
                    {
                        searchBytes.Add(byte.Parse(search.Substring(i, 2), System.Globalization.NumberStyles.HexNumber));
                    }
                    await HexSearch(mcb, searchBytes.ToArray());
                    break;

                case Mode.REPLACE_GRAPHICS:
                    await ReplaceGraphics(mcb, grp, replacementFolder, output);
                    break;

                case Mode.STRING_SEARCH:
                    await StringSearch(mcb, search);
                    break;

                default:
                    options.WriteOptionDescriptions(Console.Out);
                    break;
            }
        }

        public static async Task ReplaceGraphics(McbFile mcb, ArchiveFile<GraphicsFile> grp, string replacementFolder, string outputFolder)
        {
            Regex grpRegex = new(@"grp-(?<grpIndex>\d{4})");
            foreach (string file in Directory.GetFiles(replacementFolder))
            {
                if (file.EndsWith("png", StringComparison.OrdinalIgnoreCase))
                {
                    var bitmap = new System.Drawing.Bitmap(file);

                    int grpIndex = int.Parse(grpRegex.Match(file).Groups["grpIndex"].Value);
                    grp.Files.First(f => f.Index == grpIndex).Set20AF30Image(bitmap);

                    mcb.LoadGraphicsFiles(file.Split('_'));
                    foreach (GraphicsFile graphicsFile in mcb.GraphicsFiles)
                    {
                        graphicsFile.Set20AF30Image(bitmap);
                    }

                    Console.WriteLine($"Finished replacing file {file} in MCB & GRP");
                }
            }

            await mcb.Save(Path.Combine(outputFolder, "mcb0.bln"), Path.Combine(outputFolder, "mcb1.bln"));
            Console.WriteLine("Finished saving MCB");
            File.WriteAllBytes(Path.Combine(outputFolder, "grp.bin"), grp.GetBytes(out Dictionary<int, int> offsetAdjustments));
            Console.WriteLine("Finished saving GRP");
            await mcb.AdjustOffsets(Path.Combine(outputFolder, "mcb0.bln"), Path.Combine(outputFolder, "mcb1.bln"), "grp.bin", offsetAdjustments);
            Console.WriteLine("Finished adjusting MCB offsets");
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

        public static async Task ExportFileMap(McbFile mcb, string binFile, string outputFile)
        {
            Dictionary<int, List<(int, int)>> fileMap = await mcb.GetFileMap(binFile);
            string binIdentifier = Path.GetFileNameWithoutExtension(binFile);
            List<string> fileNames = new();

            foreach (int binIndex in fileMap.Keys)
            {
                string fileName = "";
                foreach ((int parent, int child) locationPair in fileMap[binIndex])
                {
                    fileName += $"{locationPair.parent:D3}-{locationPair.child:D3}_";
                }
                fileName += $"{binIdentifier}-{binIndex:D4}";
                fileNames.Add(fileName);
            }

            File.WriteAllLines(outputFile, fileNames);
        }

        public static async Task ExtractArchive(McbFile mcb, string outputDirectory, int archive)
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            using Stream archiveStream = await mcb.ArchiveFiles[archive].GetFileData();
            BlnSub blnSub = new();
            List<IArchiveFileInfo> blnSubFiles = (List<IArchiveFileInfo>)blnSub.Load(archiveStream);

            for (int i = 0; i < blnSubFiles.Count; i++)
            {
                File.WriteAllBytes(Path.Combine(outputDirectory, $"{i:D3}.bin"), blnSubFiles[i].GetFileDataBytes());
            }
        }

        public static void ExtractListOfFiles(McbFile mcb, string outputDirectory, string locationsFile = "string_file_locations.csv")
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            string stringFileLocations = File.ReadAllText(locationsFile);
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
            List<(int, int)> fileLocations = await mcb.FindStringInFiles(search);
            using StreamWriter fs = File.CreateText("search_result_locations.csv");
            foreach ((int file, int subFile) in fileLocations)
            {
                fs.WriteLine($"{file},{subFile}");
            }
        }

        public static async Task HexSearch(McbFile mcb, byte[] search)
        {
            List<(int, int)> fileLocations = await mcb.CheckHexInIdentifier(search);
            using StreamWriter fs = File.CreateText("search_result_locations.csv");
            foreach ((int file, int subFile) in fileLocations)
            {
                fs.WriteLine($"{file},{subFile}");
            }
        }

        public static void GeneratePatch(string outputDir)
        {
            Directory.CreateDirectory(outputDir);

            XmlDocument xml = new();
            XmlElement root = xml.CreateElement("wiidisc");
            root.SetAttribute("version", "1");
            xml.AppendChild(root);

            XmlElement id = xml.CreateElement("id");
            id.SetAttribute("game", "R44J8P");
            root.AppendChild(id);

            XmlElement options = xml.CreateElement("options");
            XmlElement section = xml.CreateElement("section");
            section.SetAttribute("name", "Heiretsu Translation");
            XmlElement option = xml.CreateElement("option");
            option.SetAttribute("name", "Heiretsu Translation");
            XmlElement choice = xml.CreateElement("choice");
            choice.SetAttribute("name", "Enabled");
            XmlElement choicePatch = xml.CreateElement("patch");
            choicePatch.SetAttribute("id", "HeiretsuFolder");
            choice.AppendChild(choicePatch);
            option.AppendChild(choice);
            section.AppendChild(option);
            options.AppendChild(section);
            root.AppendChild(options);

            XmlElement patch = xml.CreateElement("patch");
            patch.SetAttribute("id", "HeiretsuFolder");
            XmlElement folderRecurse = xml.CreateElement("folder");
            folderRecurse.SetAttribute("external", "/Heiretsu/files");
            folderRecurse.SetAttribute("recursive", "true");
            XmlElement folderDisc = xml.CreateElement("folder");
            folderDisc.SetAttribute("external", "/Heiretsu/files");
            folderRecurse.SetAttribute("disc", "/");
            patch.AppendChild(folderRecurse);
            patch.AppendChild(folderDisc);
            root.AppendChild(patch);

            xml.Save(Path.Combine(outputDir, "Heiretsu_base.xml"));
        }
    }
}
