using Kontract.Models.Archive;
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
        public List<ScriptFile> ScriptFiles { get; set; } = new List<ScriptFile>();

        private FileStream _indexFileStream { get; set; }
        private FileStream _dataFileStream { get; set; }


        public McbFile(string indexFile, string dataFile)
        {
            _indexFileStream = File.OpenRead(indexFile);
            _dataFileStream = File.OpenRead(dataFile);
            ArchiveFiles = (List<IArchiveFileInfo>)BlnFile.Load(_indexFileStream, _dataFileStream);
        }

        public void Save(string indexFile, string dataFile)
        {
            using FileStream indexFileStream = File.OpenWrite(indexFile);
            using FileStream dataFileStream = File.OpenWrite(dataFile);
            BlnFile.Save(indexFileStream, dataFileStream, ArchiveFiles);
        }

        public void LoadScriptFiles(string stringFileLocations)
        {
            foreach (string line in stringFileLocations.Split("\r\n"))
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }
                string[] lineSplit = line.Split(',');
                int parentLoc = int.Parse(lineSplit[0]);
                int childLoc = int.Parse(lineSplit[1]);

                using Stream fileStream = ArchiveFiles[parentLoc].GetFileData().GetAwaiter().GetResult();
                BlnSub blnSub = new BlnSub();
                IArchiveFileInfo blnSubFile = blnSub.GetFile(fileStream, childLoc);

                byte[] subFileData = blnSubFile.GetFileDataBytes();

                ScriptFiles.Add(new ScriptFile(parentLoc, childLoc, subFileData));
            }
        }

        public async Task<List<(int, int)>> FindStringFiles()
        {
            var fileLocations = new List<(int, int)>();

            for (int i = 75; i < ArchiveFiles.Count; i++)
            {
                using Stream fileStream = await ArchiveFiles[i].GetFileData();

                BlnSub blnSub = new BlnSub();
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
                            Console.WriteLine($"File {j} in file {i} contains voiced lines");
                        }
                    }
                }
                Console.WriteLine($"Finished searching {subFiles.Count} files in file {i}");
            }

            return fileLocations;
        }

        public async Task<List<(int, int)>> FindStringInFiles(string search)
        {
            var fileLocations = new List<(int, int)>();

            for (int i = 0; i < ArchiveFiles.Count; i++)
            {
                using Stream fileStream = await ArchiveFiles[i].GetFileData();

                BlnSub blnSub = new BlnSub();
                List<IArchiveFileInfo> subFiles = (List<IArchiveFileInfo>)blnSub.Load(fileStream);

                for (int j = 0; j < subFiles.Count; j++)
                {
                    byte[] data = subFiles[j].GetFileDataBytes();

                    if (data.Length > 0)
                    {
                        string idBytes = Encoding.GetEncoding("Shift-JIS").GetString(data);
                        if (Regex.IsMatch(idBytes, search))
                        {
                            fileLocations.Add((i, j));
                            Console.WriteLine($"File {j} in file {i} contains string '{search}'");
                        }
                    }
                }
                Console.WriteLine($"Finished searching {subFiles.Count} files in file {i}");
            }

            return fileLocations;
        }
    }
}
