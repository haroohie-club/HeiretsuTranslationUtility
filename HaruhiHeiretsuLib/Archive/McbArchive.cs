using HaruhiHeiretsuLib.Data;
using HaruhiHeiretsuLib.Graphics;
using HaruhiHeiretsuLib.Strings.Data;
using HaruhiHeiretsuLib.Strings.Events;
using HaruhiHeiretsuLib.Strings.Scripts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HaruhiHeiretsuLib.Util;

namespace HaruhiHeiretsuLib.Archive
{
    /// <summary>
    /// Representation of the mcb0.bln and mcb1.bln files, which together make up the "MCB archive"
    /// </summary>
    public class McbArchive
    {
        /// <summary>
        /// The MCB sub archives that comprise the archive
        /// </summary>
        public List<McbSubArchive> McbSubArchives { get; set; } = [];
        /// <summary>
        /// The strings files that are contained in the archive
        /// </summary>
        public List<(int parentLoc, int childLoc)> StringsFiles { get; set; } = [];
        /// <summary>
        /// The graphics files that are contained in the archive
        /// </summary>
        public List<(int parentLoc, int childLoc)> GraphicsFiles { get; set; } = [];
        /// <summary>
        /// A set of dictionaries for the bin archives indicating the correspondence between the MCB indices and the offsets in its bin archive
        /// </summary>
        Dictionary<ArchiveIndex, Dictionary<int, int>> OffsetIndexDictionaries { get; set; } = [];
        /// <summary>
        /// The font file stored in the MCB
        /// </summary>
        public FontFile FontFile { get; set; }

        /// <summary>
        /// An enum describing the index assigned to each bin archive
        /// </summary>
        public enum ArchiveIndex
        {
            /// <summary>
            /// grp.bin
            /// </summary>
            GRP = 0,
            /// <summary>
            /// dat.bin
            /// </summary>
            DAT = 1,
            /// <summary>
            /// scr.bin
            /// </summary>
            SCR = 2,
            /// <summary>
            /// evt.bin
            /// </summary>
            EVT = 3,
        }

        /// <summary>
        /// Constructs an MCB archive from its index and data file paths
        /// </summary>
        /// <param name="indexFile">The path to mcb0.bln</param>
        /// <param name="dataFile">The path to mcb1.bln</param>
        public McbArchive(string indexFile, string dataFile)
        {
            byte[] indexFileBytes = File.ReadAllBytes(indexFile);
            byte[] dataFileBytes = File.ReadAllBytes(dataFile);

            int parentLoc = 0;
            for (int i = 0; i < indexFileBytes.Length - 12; i += 12)
            {
                ushort id = IO.ReadUShortLE(indexFileBytes, i);
                short padding = IO.ReadShortLE(indexFileBytes, i + 2);
                int offset = IO.ReadIntLE(indexFileBytes, i + 4);
                int size = IO.ReadIntLE(indexFileBytes, i + 8);

                if (id == 0)
                {
                    break;
                }

                McbSubArchives.Add(new(parentLoc++, id, padding, offset, size, dataFileBytes));
            }
        }

        /// <summary>
        /// Gets the bytes representing MCB's two file components
        /// </summary>
        /// <returns>A tuple containing binary representations of mcb0.bln and mcb1.bln</returns>
        public (byte[] mcb0Bytes, byte[] mcb1Bytes) GetBytes()
        {
            List<byte> mcb0 = [], mcb1 = [];

            if (FontFile is not null && FontFile.Edited == true)
            {
                SaveFontFile();
            }

            int offset = 0;
            foreach (McbSubArchive subArchive in McbSubArchives)
            {
                mcb0.AddRange(BitConverter.GetBytes(subArchive.Id));
                mcb0.AddRange(BitConverter.GetBytes(subArchive.Padding));
                subArchive.Offset = offset;
                mcb0.AddRange(BitConverter.GetBytes(subArchive.Offset));
                byte[] subArchiveBytes = subArchive.GetBytes();
                subArchive.Size = subArchiveBytes.Length;
                mcb0.AddRange(BitConverter.GetBytes(subArchive.Size));

                mcb1.AddRange(subArchiveBytes);
                offset += subArchive.Size;
            }

            mcb0.AddRange(new byte[0x3000 - mcb0.Count]);

            return (mcb0.ToArray(), mcb1.ToArray());
        }

        /// <summary>
        /// Adjusts the bin archive offsets stored in the mcb based on a change file produced by editing the bin archives
        /// </summary>
        /// <param name="binArchiveAdjustmentFile">The path to a bin archive adjustment file</param>
        public void AdjustOffsets(string binArchiveAdjustmentFile)
        {
            Dictionary<int, int> offsetAdjustments = [];
            string[] archiveAdjustmentFileLines = File.ReadAllLines(binArchiveAdjustmentFile);

            foreach (string line in archiveAdjustmentFileLines.Skip(1))
            {
                string[] adjustments = line.Split(',');
                offsetAdjustments.Add(int.Parse(adjustments[0]), int.Parse(adjustments[1]));
            }

            AdjustOffsets(archiveAdjustmentFileLines[0], offsetAdjustments);
        }

        /// <summary>
        /// Adjusts the bin archive offsets stored in the mcb based on a provided dictionary of offset adjustments
        /// </summary>
        /// <param name="archiveToAdjust">The name of the archive to adjust (grp.bin, dat.bin, scr.bin, or evt.bin)</param>
        /// <param name="offsetAdjustments">A dictionary keyed with the old offsets and valued with the new offsets</param>
        public void AdjustOffsets(string archiveToAdjust, Dictionary<int, int> offsetAdjustments)
        {
            int archiveIndexToAdjust;
            switch (archiveToAdjust)
            {
                case "grp.bin":
                    archiveIndexToAdjust = (int)ArchiveIndex.GRP;
                    break;

                case "dat.bin":
                    archiveIndexToAdjust = (int)ArchiveIndex.DAT;
                    break;

                case "scr.bin":
                    archiveIndexToAdjust = (int)ArchiveIndex.SCR;
                    break;

                case "evt.bin":
                    archiveIndexToAdjust = (int)ArchiveIndex.EVT;
                    break;

                default:
                    Console.WriteLine($"Invalid archive loaded: {archiveToAdjust}");
                    return;
            }

            foreach (McbSubArchive subArchive in McbSubArchives)
            {
                foreach (FileInArchive file in subArchive.Files)
                {
                    if (file.McbEntryData.ArchiveIndex == archiveIndexToAdjust)
                    {
                        if (offsetAdjustments.ContainsKey(file.McbEntryData.ArchiveOffset))
                        {
                            file.McbEntryData = (file.McbEntryData.ArchiveIndex, offsetAdjustments[file.McbEntryData.ArchiveOffset]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Resolves graphics file names in the mcb
        /// </summary>
        public void ResolveGraphicsFileNames()
        {
            byte[] graphicsFileNameMap = McbSubArchives[0].Files[57].GetBytes();
            int numGraphicsFiles = IO.ReadInt(graphicsFileNameMap, 0x10);

            Dictionary<int, string> indexToNameMap = [];
            for (int i = 0; i < numGraphicsFiles; i++)
            {
                indexToNameMap.Add(IO.ReadInt(graphicsFileNameMap, 0x14 * (i + 1)), IO.ReadAsciiString(graphicsFileNameMap, 0x14 * (i + 1) + 0x04));
            }

            foreach ((int parentLoc, int childLoc) in GraphicsFiles)
            {
                ((GraphicsFile)McbSubArchives[parentLoc].Files[childLoc]).TryResolveName(OffsetIndexDictionaries[ArchiveIndex.GRP], indexToNameMap);
            }
            foreach ((int parentLoc, int childLoc) in GraphicsFiles)
            {
                if (((GraphicsFile)McbSubArchives[parentLoc].Files[childLoc]).FileType == GraphicsFile.GraphicsFileType.SGE)
                {
                    ((GraphicsFile)McbSubArchives[parentLoc].Files[childLoc]).Sge.ResolveTextures(((GraphicsFile)McbSubArchives[parentLoc].Files[childLoc]).Name,
                        GraphicsFiles.Select(i => (GraphicsFile)McbSubArchives[i.parentLoc].Files[i.childLoc]).ToList());
                }
            }
        }

        /// <summary>
        /// Resolves script file names in the MCB
        /// </summary>
        public void ResolveScriptFileNames()
        {
            byte[] scriptNameList = McbSubArchives[75].Files[1].GetBytes();
            List<string> scriptNames = ScriptFile.ParseScriptListFile(scriptNameList);
            Dictionary<int, string> indexToNameMap = scriptNames.ToDictionary(keySelector: n => scriptNames.IndexOf(n) + 1);

            foreach ((int parentLoc, int childLoc) in StringsFiles)
            {
                if (McbSubArchives[parentLoc].Files[childLoc].GetType() == typeof(ScriptFile))
                {
                    ((ScriptFile)McbSubArchives[parentLoc].Files[childLoc]).Name = indexToNameMap[OffsetIndexDictionaries[ArchiveIndex.SCR][McbSubArchives[parentLoc].Files[childLoc].McbEntryData.ArchiveOffset]];
                }
            }
        }

        /// <summary>
        /// Constructs and stores an index/offset dictionary given a bin archive file
        /// </summary>
        /// <param name="binArchiveFile">The bin archive files whose offsets to use to construct the dictionary</param>
        public void LoadIndexOffsetDictionary(string binArchiveFile)
        {
            var archive = BinArchive<FileInArchive>.FromFile(binArchiveFile);
            switch (Path.GetFileName(binArchiveFile).ToLower())
            {
                case "grp.bin":
                    if (!OffsetIndexDictionaries.ContainsKey(ArchiveIndex.GRP))
                    {
                        OffsetIndexDictionaries.Add(ArchiveIndex.GRP, []);
                    }
                    OffsetIndexDictionaries[ArchiveIndex.GRP] = [];
                    foreach (var file in archive.Files)
                    {
                        OffsetIndexDictionaries[ArchiveIndex.GRP].Add(file.Offset, file.BinArchiveIndex);
                    }
                    ResolveGraphicsFileNames();
                    break;
                case "dat.bin":
                    if (!OffsetIndexDictionaries.ContainsKey(ArchiveIndex.DAT))
                    {
                        OffsetIndexDictionaries.Add(ArchiveIndex.DAT, []);
                    }
                    OffsetIndexDictionaries[ArchiveIndex.DAT] = [];
                    foreach (var file in archive.Files)
                    {
                        OffsetIndexDictionaries[ArchiveIndex.DAT].Add(file.Offset, file.BinArchiveIndex);
                    }
                    break;
                case "scr.bin":
                    if (!OffsetIndexDictionaries.ContainsKey(ArchiveIndex.SCR))
                    {
                        OffsetIndexDictionaries.Add(ArchiveIndex.SCR, []);
                    }
                    OffsetIndexDictionaries[ArchiveIndex.SCR] = [];
                    foreach (var file in archive.Files)
                    {
                        OffsetIndexDictionaries[ArchiveIndex.SCR].Add(file.Offset, file.BinArchiveIndex);
                    }
                    ResolveScriptFileNames();
                    break;
                case "evt.bin":
                    if (!OffsetIndexDictionaries.ContainsKey(ArchiveIndex.EVT))
                    {
                        OffsetIndexDictionaries.Add(ArchiveIndex.EVT, []);
                    }
                    OffsetIndexDictionaries[ArchiveIndex.EVT] = [];
                    foreach (var file in archive.Files)
                    {
                        OffsetIndexDictionaries[ArchiveIndex.EVT].Add(file.Offset, file.BinArchiveIndex);
                    }
                    break;
            }

        }

        /// <summary>
        /// Gets a file map
        /// </summary>
        /// <param name="binArchiveFile"></param>
        /// <returns></returns>
        public Dictionary<int, List<(int, int)>> GetFileMap(string binArchiveFile)
        {
            Dictionary<int, List<(int, int)>> fileMap = [];
            int archiveIndexToSearch;
            switch (Path.GetFileName(binArchiveFile).ToLower())
            {
                case "grp.bin":
                    archiveIndexToSearch = (int)ArchiveIndex.GRP;
                    break;

                case "dat.bin":
                    archiveIndexToSearch = (int)ArchiveIndex.DAT;
                    break;

                case "scr.bin":
                    archiveIndexToSearch = (int)ArchiveIndex.SCR;
                    break;

                case "evt.bin":
                    archiveIndexToSearch = (int)ArchiveIndex.EVT;
                    break;

                default:
                    Console.WriteLine($"Invalid archive loaded: {binArchiveFile}");
                    return fileMap;
            }

            var binArchive = BinArchive<FileInArchive>.FromFile(binArchiveFile);

            for (int i = 0; i < McbSubArchives.Count; i++)
            {
                for (int j = 0; j < McbSubArchives[i].Files.Count; j++)
                {
                    if (McbSubArchives[i].Files[j].McbEntryData.ArchiveIndex == archiveIndexToSearch)
                    {
                        int correspondingBinIndex = binArchive.Files.First(f => f.Offset == McbSubArchives[i].Files[j].McbEntryData.ArchiveOffset).BinArchiveIndex;

                        if (!fileMap.ContainsKey(correspondingBinIndex))
                        {
                            fileMap.Add(correspondingBinIndex, []);
                        }

                        fileMap[correspondingBinIndex].Add((i, j));
                        Console.WriteLine($"Mapped {correspondingBinIndex:D4} to MCB {i:D3}-{j:D3}");
                    }
                }
            }

            return fileMap;
        }

        public void LoadStringsFiles(string[] stringsFilesLocations, List<ScriptCommand> scriptCommands = null)
        {
            LoadStringsFiles(string.Join('\n', stringsFilesLocations.Where(l => Regex.IsMatch(l, @"\d{3}-\d{3}")).Select(l => Path.GetFileNameWithoutExtension(l).Replace('-', ','))), scriptCommands);
        }

        public void LoadStringsFiles(string stringFileLocations, List<ScriptCommand> scriptCommands = null)
        {
            foreach (string line in stringFileLocations.Replace("\r\n", "\n").Split("\n"))
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }
                string[] lineSplit = line.Split(',');
                int parentLoc = int.Parse(lineSplit[0]);
                int childLoc = int.Parse(lineSplit[1]);

                MapDefinitionsFile mapDefinitionsFile = McbSubArchives[0].Files[79].CastTo<MapDefinitionsFile>();
                MapDefinition mapDef;
                if (parentLoc != 0)
                {
                    mapDef = mapDefinitionsFile.Sections[((McbSubArchives[parentLoc].Id >> 8) ^ 0x40) - 2].MapDefinitions[McbSubArchives[parentLoc].Id & 0xFF];
                }
                else
                {
                    mapDef = null;
                }

                switch ((ArchiveIndex)McbSubArchives[parentLoc].Files[childLoc].McbEntryData.ArchiveIndex)
                {
                    case ArchiveIndex.DAT:
                        StringsFiles.Add((parentLoc, childLoc));
                        switch ((parentLoc, childLoc))
                        {
                            case (0, DataStringsFileLocations.MAP_DEFINITION_MCB_INDEX):
                                McbSubArchives[parentLoc].Files[childLoc] = McbSubArchives[parentLoc].Files[childLoc].CastTo<DataStringsFile<MapDefinitionsFile>>();
                                break;

                            case (0, DataStringsFileLocations.TOPICS_FLAG_MCB_INDEX):
                                McbSubArchives[parentLoc].Files[childLoc] = McbSubArchives[parentLoc].Files[childLoc].CastTo<DataStringsFile<TopicsAndFlagsFile>>();
                                break;

                            case (0, DataStringsFileLocations.NAMEPLATES_MCB_INDEX):
                                McbSubArchives[parentLoc].Files[childLoc] = McbSubArchives[parentLoc].Files[childLoc].CastTo<DataStringsFile<NameplatesFile>>();
                                break;

                            case (0, DataStringsFileLocations.TIMELINE_MCB_INDEX):
                                McbSubArchives[parentLoc].Files[childLoc] = McbSubArchives[parentLoc].Files[childLoc].CastTo<DataStringsFile<TimelineFile>>();
                                break;

                            case (0, DataStringsFileLocations.CLUBROOM_MCB_INDEX):
                                McbSubArchives[parentLoc].Files[childLoc] = McbSubArchives[parentLoc].Files[childLoc].CastTo<DataStringsFile<ClubroomKoizumiCutscenesFile>>();
                                break;

                            default:
                                McbSubArchives[parentLoc].Files[childLoc] = McbSubArchives[parentLoc].Files[childLoc].CastTo<ShadeStringsFile>();
                                break;
                        }
                        break;
                    case ArchiveIndex.SCR:
                        StringsFiles.Add((parentLoc, childLoc));
                        McbSubArchives[parentLoc].Files[childLoc] = McbSubArchives[parentLoc].Files[childLoc].CastTo<ScriptFile>();
                        ((ScriptFile)McbSubArchives[parentLoc].Files[childLoc]).AvailableCommands = scriptCommands;
                        ((ScriptFile)McbSubArchives[parentLoc].Files[childLoc]).PopulateCommandBlocks(mapDef?.Evts);
                        break;
                    case ArchiveIndex.EVT:
                        StringsFiles.Add((parentLoc, childLoc));
                        McbSubArchives[parentLoc].Files[childLoc] = McbSubArchives[parentLoc].Files[childLoc].CastTo<EventFile>();
                        break;
                }
            }
        }

        public static List<(int, int)> GetFilesToLoad(string[] graphicsFilesLocations)
        {
            List<(int, int)> locations = [];
            string[] recombined = graphicsFilesLocations.Where(l => Regex.IsMatch(l, @"\d{3}-\d{3}")).Select(l => Path.GetFileNameWithoutExtension(l).Replace('-', ',')).ToArray();

            foreach (string line in recombined)
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }
                string[] lineSplit = line.Split(',');
                int parentLoc = int.Parse(lineSplit[0]);
                int childLoc = int.Parse(lineSplit[1]);
                locations.Add((parentLoc, childLoc));
            }
            return locations;
        }

        public void LoadGraphicsFiles(string[] graphicsFilesLocations)
        {
            LoadGraphicsFiles(string.Join('\n', graphicsFilesLocations.Where(l => Regex.IsMatch(l, @"\d{3}-\d{3}")).Select(l => Path.GetFileNameWithoutExtension(l).Replace('-', ','))));
        }

        public void LoadGraphicsFiles(string graphicsFilesLocations)
        {
            foreach (string line in graphicsFilesLocations.Replace("\r\n", "\n").Split("\n"))
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }
                string[] lineSplit = line.Split(',');
                int parentLoc = int.Parse(lineSplit[0]);
                int childLoc = int.Parse(lineSplit[1]);

                GraphicsFiles.Add((parentLoc, childLoc));
            }
        }

        public void LoadGraphicsFiles()
        {
            for (int parent = 0; parent < McbSubArchives.Count; parent++)
            {
                for (int child = 0; child < McbSubArchives[parent].Files.Count; child++)
                {
                    if (McbSubArchives[parent].Files[child].McbEntryData.ArchiveIndex == (int)ArchiveIndex.GRP)
                    {
                        GraphicsFile graphicsFile = new()
                        {
                            Location = (parent, child), McbEntryData = (McbSubArchives[parent].Files[child].McbEntryData.ArchiveIndex,
                            McbSubArchives[parent].Files[child].McbEntryData.ArchiveOffset),
                            McbId = McbSubArchives[parent].Id,
                            CompressedData = McbSubArchives[parent].Files[child].CompressedData
                        };
                        graphicsFile.Initialize([.. McbSubArchives[parent].Files[child].Data], 0);
                        graphicsFile.Offset = McbSubArchives[parent].Files[child].Offset;
                        GraphicsFiles.Add((parent, child));
                        McbSubArchives[parent].Files[child] = graphicsFile;
                    }
                }
            }
        }

        public void LoadFontFile()
        {
            FontFile = new FontFile([.. McbSubArchives[0].Files[5].Data]);
        }

        public void SaveFontFile()
        {
            McbSubArchives[0].Files[5].Edited = true;
            McbSubArchives[0].Files[5].Data = [.. FontFile.GetBytes()];
        }

        public List<(int, int)> FindStringInFiles(string search)
        {
            List<(int, int)> fileLocations = [];

            foreach (McbSubArchive subArchive in McbSubArchives)
            {
                foreach (FileInArchive file in subArchive.Files)
                {
                    if (file.Data.Count > 0)
                    {
                        string idBytes = Encoding.GetEncoding("Shift-JIS").GetString(file.Data.ToArray());
                        if (Regex.IsMatch(idBytes, search, RegexOptions.IgnoreCase))
                        {
                            fileLocations.Add(file.Location);
                            Console.WriteLine($"File {file.Location.child} in archive {file.Location.parent} contains string '{search}'");
                        }
                    }
                }
                Console.WriteLine($"Finished searching {subArchive.Files.Count} files in archive {subArchive.Id:X4}");
            }

            return fileLocations;
        }

        public List<(int, int)> CheckHexInFile(byte[] search, bool fourByteAligned)
        {
            List<(int, int)> fileLocations = [];

            int increment = fourByteAligned ? 4 : 1;

            foreach (McbSubArchive subArchive in McbSubArchives)
            {
                foreach (FileInArchive file in subArchive.Files)
                {
                    if (file.Data.Count > 0)
                    {
                        for (int i = 0; i < file.Data.Count - search.Length; i += increment)
                        {
                            if (file.Data.Skip(i).Take(search.Length).SequenceEqual(search))
                            {
                                fileLocations.Add(file.Location);
                                Console.WriteLine($"File {file.Location.child} in archive {file.Location.parent} contains sequence '{string.Join(' ', search.Select(b => $"{b:X2}"))}'");
                                break;
                            }
                        }
                    }
                }
                Console.WriteLine($"Finished searching {subArchive.Files.Count} files in archive {subArchive.Id:X4}");
            }

            return fileLocations;
        }
    }
}
