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
using System.Threading.Tasks;

namespace HaruhiHeiretsuLib.Archive
{
    public class McbArchive
    {
        public List<McbSubArchive> McbSubArchives { get; set; } = new();
        public List<(int parentLoc, int childLoc)> StringsFiles { get; set; } = new();
        public List<(int parentLoc, int childLoc)> GraphicsFiles { get; set; } = new();
        Dictionary<ArchiveIndex, Dictionary<int, int>> OffsetIndexDictionaries { get; set; } = new();
        public FontFile FontFile { get; set; }

        public enum ArchiveIndex
        {
            GRP = 0,
            DAT = 1,
            SCR = 2,
            EVT = 3,
        }

        public McbArchive(string indexFile, string dataFile)
        {
            byte[] indexFileBytes = File.ReadAllBytes(indexFile);
            byte[] dataFileBytes = File.ReadAllBytes(dataFile);

            int parentLoc = 0;
            for (int i = 0; i < indexFileBytes.Length - 12; i += 12)
            {
                ushort id = BitConverter.ToUInt16(indexFileBytes.Skip(i).Take(2).ToArray());
                short padding = BitConverter.ToInt16(indexFileBytes.Skip(i + 2).Take(2).ToArray());
                int offset = BitConverter.ToInt32(indexFileBytes.Skip(i + 4).Take(4).ToArray());
                int size = BitConverter.ToInt32(indexFileBytes.Skip(i + 8).Take(4).ToArray());

                if (id == 0)
                {
                    break;
                }

                McbSubArchives.Add(new(parentLoc++, id, padding, offset, size, dataFileBytes));
            }
        }

        public (byte[] mcb0Bytes, byte[] mcb1Bytes) GetBytes()
        {
            List<byte> mcb0 = new(), mcb1 = new();

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

        public void AdjustOffsets(string binArchiveAdjustmentFile)
        {
            Dictionary<int, int> offsetAdjustments = new();
            string[] archiveAdjustmentFileLines = File.ReadAllLines(binArchiveAdjustmentFile);

            foreach (string line in archiveAdjustmentFileLines.Skip(1))
            {
                string[] adjustments = line.Split(',');
                offsetAdjustments.Add(int.Parse(adjustments[0]), int.Parse(adjustments[1]));
            }

            AdjustOffsets(archiveAdjustmentFileLines[0], offsetAdjustments);
        }

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
                    if (file.McbEntryData.archiveIndex == archiveIndexToAdjust)
                    {
                        if (offsetAdjustments.ContainsKey(file.McbEntryData.archiveOffset))
                        {
                            file.McbEntryData = (file.McbEntryData.archiveIndex, offsetAdjustments[file.McbEntryData.archiveOffset]);
                        }
                    }
                }
            }
        }

        public void ResolveGraphicsFileNames()
        {
            byte[] graphicsFileNameMap = McbSubArchives[0].Files[57].GetBytes();
            int numGraphicsFiles = BitConverter.ToInt32(graphicsFileNameMap.Skip(0x10).Take(4).Reverse().ToArray());

            Dictionary<int, string> indexToNameMap = new();
            for (int i = 0; i < numGraphicsFiles; i++)
            {
                indexToNameMap.Add(BitConverter.ToInt32(graphicsFileNameMap.Skip(0x14 * (i + 1)).Take(4).Reverse().ToArray()), Encoding.ASCII.GetString(graphicsFileNameMap.Skip(0x14 * (i + 1) + 0x04).TakeWhile(b => b != 0x00).ToArray()));
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

        public void ResolveScriptFileName()
        {
            byte[] scriptNameList = McbSubArchives[75].Files[1].GetBytes();
            List<string> scriptNames = ScriptFile.ParseScriptListFile(scriptNameList);
            Dictionary<int, string> indexToNameMap = scriptNames.ToDictionary(keySelector: n => scriptNames.IndexOf(n) + 1);

            foreach ((int parentLoc, int childLoc) in StringsFiles)
            {
                if (McbSubArchives[parentLoc].Files[childLoc].GetType() == typeof(ScriptFile))
                {
                    ((ScriptFile)McbSubArchives[parentLoc].Files[childLoc]).Name = indexToNameMap[OffsetIndexDictionaries[ArchiveIndex.SCR][McbSubArchives[parentLoc].Files[childLoc].McbEntryData.archiveOffset]];
                }
            }
        }

        public void LoadIndexOffsetDictionary(string binArchiveFile)
        {
            var archive = BinArchive<FileInArchive>.FromFile(binArchiveFile);
            switch (Path.GetFileName(binArchiveFile).ToLower())
            {
                case "grp.bin":
                    if (!OffsetIndexDictionaries.ContainsKey(ArchiveIndex.GRP))
                    {
                        OffsetIndexDictionaries.Add(ArchiveIndex.GRP, new());
                    }
                    OffsetIndexDictionaries[ArchiveIndex.GRP] = new();
                    foreach (var file in archive.Files)
                    {
                        OffsetIndexDictionaries[ArchiveIndex.GRP].Add(file.Offset, file.Index);
                    }
                    ResolveGraphicsFileNames();
                    break;
                case "dat.bin":
                    if (!OffsetIndexDictionaries.ContainsKey(ArchiveIndex.DAT))
                    {
                        OffsetIndexDictionaries.Add(ArchiveIndex.DAT, new());
                    }
                    OffsetIndexDictionaries[ArchiveIndex.DAT] = new();
                    foreach (var file in archive.Files)
                    {
                        OffsetIndexDictionaries[ArchiveIndex.DAT].Add(file.Offset, file.Index);
                    }
                    break;
                case "scr.bin":
                    if (!OffsetIndexDictionaries.ContainsKey(ArchiveIndex.SCR))
                    {
                        OffsetIndexDictionaries.Add(ArchiveIndex.SCR, new());
                    }
                    OffsetIndexDictionaries[ArchiveIndex.SCR] = new();
                    foreach (var file in archive.Files)
                    {
                        OffsetIndexDictionaries[ArchiveIndex.SCR].Add(file.Offset, file.Index);
                    }
                    ResolveScriptFileName();
                    break;
                case "evt.bin":
                    if (!OffsetIndexDictionaries.ContainsKey(ArchiveIndex.EVT))
                    {
                        OffsetIndexDictionaries.Add(ArchiveIndex.EVT, new());
                    }
                    OffsetIndexDictionaries[ArchiveIndex.EVT] = new();
                    foreach (var file in archive.Files)
                    {
                        OffsetIndexDictionaries[ArchiveIndex.EVT].Add(file.Offset, file.Index);
                    }
                    break;
            }

        }

        public Dictionary<int, List<(int, int)>> GetFileMap(string binArchiveFile)
        {
            Dictionary<int, List<(int, int)>> fileMap = new();
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
                    if (McbSubArchives[i].Files[j].McbEntryData.archiveIndex == archiveIndexToSearch)
                    {
                        int correspondingBinIndex = binArchive.Files.First(f => f.Offset == McbSubArchives[i].Files[j].McbEntryData.archiveOffset).Index;

                        if (!fileMap.ContainsKey(correspondingBinIndex))
                        {
                            fileMap.Add(correspondingBinIndex, new List<(int, int)>());
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

                switch ((ArchiveIndex)McbSubArchives[parentLoc].Files[childLoc].McbEntryData.archiveIndex)
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
                                McbSubArchives[parentLoc].Files[childLoc] = McbSubArchives[parentLoc].Files[childLoc].CastTo<DataStringsFile<ClubroomFile>>();
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
            List<(int, int)> locations = new();
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
                    if (McbSubArchives[parent].Files[child].McbEntryData.archiveIndex == (int)ArchiveIndex.GRP)
                    {
                        GraphicsFile graphicsFile = new()
                        {
                            Location = (parent, child), McbEntryData = (McbSubArchives[parent].Files[child].McbEntryData.archiveIndex,
                            McbSubArchives[parent].Files[child].McbEntryData.archiveOffset),
                            McbId = McbSubArchives[parent].Id,
                            CompressedData = McbSubArchives[parent].Files[child].CompressedData
                        };
                        graphicsFile.Initialize(McbSubArchives[parent].Files[child].Data.ToArray(), 0);
                        graphicsFile.Offset = McbSubArchives[parent].Files[child].Offset;
                        GraphicsFiles.Add((parent, child));
                        McbSubArchives[parent].Files[child] = graphicsFile;
                    }
                }
            }
        }

        public void LoadFontFile()
        {
            FontFile = new FontFile(McbSubArchives[0].Files[5].Data.ToArray());
        }

        public void SaveFontFile()
        {
            McbSubArchives[0].Files[5].Data = FontFile.GetBytes().ToList();
        }

        public List<(int, int)> FindStringInFiles(string search)
        {
            List<(int, int)> fileLocations = new();

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

        public List<(int, int)> CheckHexInFile(byte[] search)
        {
            List<(int, int)> fileLocations = new();

            foreach (McbSubArchive subArchive in McbSubArchives)
            {
                foreach (FileInArchive file in subArchive.Files)
                {
                    if (file.Data.Count > 0)
                    {
                        for (int i = 0; i < file.Data.Count - search.Length; i++)
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
