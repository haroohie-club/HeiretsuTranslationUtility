using HaruhiHeiretsuLib;
using Mono.Options;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace HaruhiHeiretsuCLI
{
    public class FindStringsCommand : Command
    {
        private string _mcb;

        public FindStringsCommand() : base("find-strings")
        {
            Options = new()
            {
                "Perform a search for script files in the MCB",
                "Usage: HaruhiHeiretsuCLI find-strings -m [MCB_FILE]",
                "",
                { "m|mcb=", "Path to mcb0.bln", m => _mcb = m },
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

            List<(short, int)> stringFileLocations = await mcb.FindStringFiles();
            using StreamWriter fs = File.CreateText("string_file_locations.csv");
            foreach ((short file, int subFile) in stringFileLocations)
            {
                fs.WriteLine($"{file:X4},{subFile:D3}");
            }

            return 0;
        }
    }
}
