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

            List<(string, List<IArchiveFileInfo>)> allSubFiles = (await Task.WhenAll(ArchiveFiles.Select(f => GetSubFiles(f)))).ToList();
            List<(string, string, IArchiveFileInfo)> allStringFiles = (await Task.WhenAll(allSubFiles
                .SelectMany(p => p.Item2.Select(f => GetStringFilesFromSubFile(p.Item1, f)))))
                .Where(f => !string.IsNullOrEmpty(f.Item1)).ToList();

            return fileLocations;
        }

        private async Task<(string, List<IArchiveFileInfo>)> GetSubFiles(IArchiveFileInfo file)
        {
            using Stream fileStream = await file.GetFileData();

            BlnSub blnSub = new BlnSub();
            return ($"{file.FilePath}", (List<IArchiveFileInfo>)blnSub.Load(fileStream));
        }

        private async Task<(string, string, IArchiveFileInfo)> GetStringFilesFromSubFile(string parentFileName, IArchiveFileInfo file)
        {
            var stringFiles = new List<IArchiveFileInfo>();
            using Stream subFileStream = await file.GetFileData();

            byte[] data = new byte[subFileStream.Length];
            subFileStream.Read(data, 0, 10);

            string idBytes = Encoding.ASCII.GetString(data);
            if (idBytes.Contains("SCRIPT"))
            {
                Console.WriteLine($"File {file.FilePath} in file {parentFileName} is a string file");
                return (parentFileName, $"{file.FilePath}", file);
            }
            else
            {
                return ("", "", file);
            }
        }
    }
}
