using HaruhiHeiretsuLib;
using Mono.Options;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace HaruhiHeiretsuCLI
{
    public class StringSearchCommand : Command
    {
        private string _mcb, _search;
        public StringSearchCommand() : base("string-search")
        {
            Options = new()
            {
                "Perform a search for a string anywhere in all files in the MCB",
                "Usage: HaruhiHeiretsuCLI string-search -m [MCB_FILE] -s [SEARCH]",
                "",
                { "m|mcb=", "Path to mcb0.bln", m => _mcb = m },
                { "s|search=", "String to search for", s => _search = s },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            return InvokeAsync(arguments).GetAwaiter().GetResult();
        }

        public async Task<int> InvokeAsync(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);
            McbFile mcb = Program.GetMcbFile(_mcb);
            
            List<(int, int)> fileLocations = await mcb.FindStringInFiles(_search);
            using StreamWriter fs = File.CreateText("search_result_locations.csv");
            foreach ((int file, int subFile) in fileLocations)
            {
                fs.WriteLine($"{file},{subFile}");
            }
            return 0;
        }
    }
}
