using Kontract.Models.Archive;
using plugin_shade.Archives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaruhiHeiretsuLib
{
    public class McbFile
    {
        public Bln BlnFile { get; set; } = new Bln();
        public List<IArchiveFileInfo> ArchiveFiles { get; set; }
        private FileStream _indexFileStream { get; set; }
        private FileStream _dataFileStream { get; set; }


        public McbFile(string indexFile, string dataFile)
        {
            _indexFileStream = File.OpenRead(indexFile);
            _dataFileStream = File.OpenRead(dataFile);
            ArchiveFiles = (List<IArchiveFileInfo>)BlnFile.Load(_indexFileStream, _dataFileStream);
        }

        public void Save(string indexFile, string dataFile, string JsonFilePath)
        {
            using FileStream indexFileStream = File.OpenWrite(indexFile);
            using FileStream dataFileStream = File.OpenWrite(dataFile);
            BlnFile.Save(indexFileStream, dataFileStream, ArchiveFiles);
        }

        public async Task<Dictionary<int, int>> FindStringFiles()
        {
            int i = 0;
            var fileLocations = new Dictionary<int, int>();

            foreach (var file in ArchiveFiles)
            {
                using Stream fileStream = await file.GetFileData();

                BlnSub blnSub = new BlnSub();
                List<IArchiveFileInfo> subFiles = (List<IArchiveFileInfo>)blnSub.Load(fileStream);

                int j = 0;
                foreach (var subFile in subFiles)
                {
                    using Stream subFileStream = await subFile.GetFileData();

                    byte[] data = new byte[subFileStream.Length];
                    subFileStream.Read(data, 0, 10);

                    if (data.Length > 0)
                    {
                        string idBytes = Encoding.ASCII.GetString(data);
                        if (idBytes.Contains("SCRIPT"))
                        {
                            fileLocations.Add(i, j);
                            Console.WriteLine($"File {j} in file {i} is a string file: {subFile.FilePath}");
                        }
                    }
                    j++;
                }
                Console.WriteLine($"Finished searching {j} files in file {i}");
                i++;
            }

            return fileLocations;
        }
    }
}
