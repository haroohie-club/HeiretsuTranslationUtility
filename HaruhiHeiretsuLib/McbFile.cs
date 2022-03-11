﻿using Kontract.Models.Archive;
using plugin_shade.Archives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HaruhiHeiretsuLib
{
    public class McbFile
    {
        public Bln BlnFile { get; set; } = new Bln();
        public List<IArchiveFileInfo> ArchiveFiles { get; set; }
        public List<ScriptFile> ScriptFiles { get; set; } = new();
        public List<GraphicsFile> GraphicsFiles { get; set; } = new();
        public FontFile FontFile { get; set; }

        private MemoryStream _indexFileStream;
        private MemoryStream _dataFileStream;


        public McbFile(string indexFile, string dataFile)
        {
            _indexFileStream = new MemoryStream(File.ReadAllBytes(indexFile));
            _dataFileStream = new MemoryStream(File.ReadAllBytes(dataFile));
            ArchiveFiles = (List<IArchiveFileInfo>)BlnFile.Load(_indexFileStream, _dataFileStream);
        }

        public async Task Save(string indexFile, string dataFile)
        {
            foreach (ScriptFile scriptFile in ScriptFiles)
            {
                if (scriptFile.Edited)
                {
                    using Stream archiveStream = await ArchiveFiles[scriptFile.Location.parent].GetFileData();
                    BlnSub blnSub = new();
                    List<IArchiveFileInfo> blnSubFiles = (List<IArchiveFileInfo>)blnSub.Load(archiveStream);
                    MemoryStream childFileStream = new(scriptFile.Data.ToArray());
                    blnSubFiles[scriptFile.Location.child].SetFileData(childFileStream);

                    MemoryStream parentFileStream = new();
                    blnSub.Save(parentFileStream, blnSubFiles, leaveOpen: true);
                    ArchiveFiles[scriptFile.Location.parent].SetFileData(parentFileStream);
                }
            }

            foreach (GraphicsFile graphicsFile in GraphicsFiles)
            {
                if (graphicsFile.Edited)
                {
                    using Stream archiveStream = await ArchiveFiles[graphicsFile.Location.parent].GetFileData();
                    BlnSub blnSub = new();
                    List<IArchiveFileInfo> blnSubFiles = (List<IArchiveFileInfo>)blnSub.Load(archiveStream);
                    MemoryStream childFileStream = new(graphicsFile.Data.ToArray());
                    blnSubFiles[graphicsFile.Location.child].SetFileData(childFileStream);

                    MemoryStream parentFileStream = new();
                    blnSub.Save(parentFileStream, blnSubFiles, leaveOpen: true);
                    ArchiveFiles[graphicsFile.Location.parent].SetFileData(parentFileStream);
                }
            }

            if (FontFile is not null && (FontFile.Edited || FontFile.CompressedData is not null))
            {
                byte[] data;
                if (FontFile.CompressedData is not null)
                {
                    data = FontFile.CompressedData;
                }
                else
                {
                    data = FontFile.GetBytes();
                }
                using Stream fontParentStream = await ArchiveFiles[0].GetFileData();
                BlnSub fontBlnSub = new();
                List<IArchiveFileInfo> fontParentSubFiles = (List<IArchiveFileInfo>)fontBlnSub.Load(fontParentStream);
                MemoryStream fontFileStream = new(data);
                fontParentSubFiles[5].SetFileData(fontFileStream);

                MemoryStream fontParentFileStream = new();
                fontBlnSub.Save(fontParentFileStream, fontParentSubFiles, leaveOpen: true);
                ArchiveFiles[0].SetFileData(fontParentFileStream);
            }

            using FileStream indexFileStream = File.OpenWrite(indexFile);
            using FileStream dataFileStream = File.OpenWrite(dataFile);
            BlnFile.Save(indexFileStream, dataFileStream, ArchiveFiles);
        }

        public async Task AdjustOffsets(string indexFile, string dataFile, string binArchiveAdjustmentFile)
        {
            Dictionary<int, int> offsetAdjustments = new();
            string[] archiveAdjustmentFileLines = File.ReadAllLines(binArchiveAdjustmentFile);

            foreach (string line in archiveAdjustmentFileLines.Skip(1))
            {
                string[] adjustments = line.Split(',');
                offsetAdjustments.Add(int.Parse(adjustments[0]), int.Parse(adjustments[1]));
            }

            await AdjustOffsets(indexFile, dataFile, archiveAdjustmentFileLines[0], offsetAdjustments);
        }

        public async Task AdjustOffsets(string indexFile, string dataFile, string archiveToAdjust, Dictionary<int, int> offsetAdjustments)
        {
            int archiveIndexToAdjust;
            switch (archiveToAdjust)
            {
                case "grp.bin":
                    archiveIndexToAdjust = 0;
                    break;

                case "dat.bin":
                    archiveIndexToAdjust = 1;
                    break;

                case "scr.bin":
                    archiveIndexToAdjust = 2;
                    break;

                default:
                    Console.WriteLine($"Invalid archive loaded: {archiveToAdjust}");
                    return;
            }

            foreach (IArchiveFileInfo archiveFile in ArchiveFiles)
            {
                using Stream archiveInputStream = await archiveFile.GetFileData();
                BlnSub blnSub = new();
                List<IArchiveFileInfo> blnSubFiles = (List<IArchiveFileInfo>)blnSub.Load(archiveInputStream);

                MemoryStream archiveOutputStream = new();
                blnSub.Save(archiveOutputStream, blnSubFiles, archiveIndexToAdjust: archiveIndexToAdjust, offsetAdjustments: offsetAdjustments, leaveOpen: true);
                archiveFile.SetFileData(archiveOutputStream);
            }

            using FileStream indexFileStream = File.OpenWrite(indexFile);
            using FileStream dataFileStream = File.OpenWrite(dataFile);
            BlnFile.Save(indexFileStream, dataFileStream, ArchiveFiles);
        }

        public async Task<Dictionary<int, List<(int, int)>>> GetFileMap(string binArchiveFile)
        {
            Dictionary<int, List<(int, int)>> fileMap = new();
            int archiveIndexToSearch;
            switch (Path.GetFileName(binArchiveFile))
            {
                case "grp.bin":
                    archiveIndexToSearch = 0;
                    break;

                case "dat.bin":
                    archiveIndexToSearch = 1;
                    break;

                case "scr.bin":
                    archiveIndexToSearch = 2;
                    break;

                default:
                    Console.WriteLine($"Invalid archive loaded: {binArchiveFile}");
                    return fileMap;
            }

            var binArchive = ArchiveFile<FileInArchive>.FromFile(binArchiveFile);

            for (int i = 0; i < ArchiveFiles.Count; i++)
            {
                Stream fileData = await ArchiveFiles[i].GetFileData();
                BlnSub blnSub = new();
                List<BlnSubArchiveFileInfo> blnSubAfis = blnSub.Load(fileData).Cast<BlnSubArchiveFileInfo>().ToList();
                
                for (int j = 0; j < blnSubAfis.Count; j++)
                {
                    if (blnSubAfis[j].Entry.archiveIndex == archiveIndexToSearch)
                    {
                        int correspondingBinIndex = binArchive.Files.First(f => f.Offset == blnSubAfis[j].Entry.archiveOffset).Index;

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

        public void LoadScriptFiles(string stringFileLocations)
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

                using Stream archiveStream = ArchiveFiles[parentLoc].GetFileData().GetAwaiter().GetResult();
                BlnSub blnSub = new();
                IArchiveFileInfo blnSubFile = blnSub.GetFile(archiveStream, childLoc);

                byte[] subFileData = blnSubFile.GetFileDataBytes();

                // archive 0 contains a lot of chokuretsu-style script files
                if (parentLoc == 0)
                {
                    ChokuretsuEventFile eventFile = new();
                    eventFile.Initialize(subFileData);
                    eventFile.Location = (parentLoc, childLoc);
                    ScriptFiles.Add(eventFile);
                }
                else
                {
                    ScriptFiles.Add(new ScriptFile(parentLoc, childLoc, subFileData));
                }
            }
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

                using Stream archiveStream = ArchiveFiles[parentLoc].GetFileData().GetAwaiter().GetResult();
                BlnSub blnSub = new();
                IArchiveFileInfo blnSubFile = blnSub.GetFile(archiveStream, childLoc);

                byte[] subFileData = blnSubFile.GetFileDataBytes();

                GraphicsFile graphicsFile = new() { Location = (parentLoc, childLoc) };
                graphicsFile.Initialize(subFileData, 0);
                GraphicsFiles.Add(graphicsFile);
            }
        }

        public void LoadFontFile()
        {
            using Stream archiveStream = ArchiveFiles[0].GetFileData().GetAwaiter().GetResult();
            BlnSub blnSub = new();
            IArchiveFileInfo blnSubFile = blnSub.GetFile(archiveStream, 5);

            FontFile = new FontFile(blnSubFile.GetFileDataBytes());
        }

        public async Task<List<(int, int)>> FindStringFiles()
        {
            List<(int, int)> fileLocations = new();

            for (int i = 75; i < ArchiveFiles.Count; i++)
            {
                using Stream fileStream = await ArchiveFiles[i].GetFileData();

                BlnSub blnSub = new();
                List<IArchiveFileInfo> subFiles = (List<IArchiveFileInfo>)blnSub.Load(fileStream);

                for (int j = 0; j < subFiles.Count; j++)
                {
                    byte[] data = subFiles[j].GetFileDataBytes();

                    if (data.Length > 0)
                    {
                        string idBytes = Encoding.ASCII.GetString(data);
                        if (Regex.IsMatch(idBytes, @"V\d{3}\w{7}(?<characterCode>[A-Z]{3})"))
                        {
                            fileLocations.Add((i, j));
                            Console.WriteLine($"File {j} in archive {i} contains voiced lines");
                        }
                    }
                }
                Console.WriteLine($"Finished searching {subFiles.Count} files in file {i}");
            }

            return fileLocations;
        }

        public async Task<List<(int, int)>> FindStringInFiles(string search)
        {
            List<(int, int)> fileLocations = new();

            for (int i = 0; i < ArchiveFiles.Count; i++)
            {
                using Stream fileStream = await ArchiveFiles[i].GetFileData();

                BlnSub blnSub = new();
                List<IArchiveFileInfo> subFiles = (List<IArchiveFileInfo>)blnSub.Load(fileStream);

                for (int j = 0; j < subFiles.Count; j++)
                {
                    byte[] data = subFiles[j].GetFileDataBytes();

                    if (data.Length > 0)
                    {
                        string idBytes = Encoding.GetEncoding("Shift-JIS").GetString(data);
                        if (Regex.IsMatch(idBytes, search, RegexOptions.IgnoreCase))
                        {
                            fileLocations.Add((i, j));
                            Console.WriteLine($"File {j} in archive {i} contains string '{search}'");
                        }
                    }
                }
                Console.WriteLine($"Finished searching {subFiles.Count} files in file {i}");
            }

            return fileLocations;
        }

        public async Task<List<(int, int)>> CheckHexInFile(byte[] search)
        {
            List<(int, int)> fileLocations = new();

            for (int i = 0; i < ArchiveFiles.Count; i++)
            {
                using Stream fileStream = await ArchiveFiles[i].GetFileData();

                BlnSub blnSub = new();
                List<IArchiveFileInfo> subFiles = (List<IArchiveFileInfo>)blnSub.Load(fileStream);

                for (int j = 0; j < subFiles.Count; j++)
                {
                    byte[] data = subFiles[j].GetFileDataBytes();

                    if (data.Length > 0)
                    {
                        for (int k = 0; i < data.Length - search.Length; k++)
                        {
                            if (data.Skip(k).Take(search.Length).SequenceEqual(search))
                            {
                                fileLocations.Add((i, j));
                                Console.WriteLine($"File {j} in archive {i} contains sequence '{string.Join(' ', search.Select(b => $"{b:X2}"))}'");
                                break;
                            }
                        }
                    }
                }
                Console.WriteLine($"Finished searching {subFiles.Count} files in file {i}");
            }

            return fileLocations;
        }
    }
}
