using HaruhiHeiretsuLib.Archive;
using Mono.Options;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HaruhiHeiretsuCLI
{
    public class StringSearchCommand : Command
    {
        private string _mcb, _bin, _search;
        public StringSearchCommand() : base("string-search")
        {
            Options = new()
            {
                "Perform a search for a string anywhere in all files in the MCB",
                "Usage: HaruhiHeiretsuCLI string-search -m [MCB_FILE] -s [SEARCH]",
                "",
                { "m|mcb=", "Path to mcb0.bln", m => _mcb = m },
                { "b|bin=", "Path to bin archive", b => _bin = b },
                { "s|search=", "String to search for", s => _search = s },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            if (!string.IsNullOrEmpty(_mcb))
            {
                McbArchive mcb = Program.GetMcbFile(_mcb);

                List<(int, int)> fileLocations = mcb.FindStringInFiles(_search);
                using StreamWriter fs = File.CreateText("search_result_locations.csv");
                foreach ((int file, int subFile) in fileLocations)
                {
                    fs.WriteLine($"{file},{subFile}");
                }
            }
            else if (!string.IsNullOrEmpty(_bin))
            {
                BinArchive<FileInArchive> bin = BinArchive<FileInArchive>.FromFile(_bin);
                File.WriteAllLines("search_result_locations.csv", bin.Files.Where(f => Encoding.GetEncoding("Shift-JIS").GetString(f.Data.ToArray()).Contains(_search)).Select(f => $"{f.BinArchiveIndex}"));
            }
            return 0;
        }
    }
}
