using HaruhiHeiretsuLib;
using Mono.Options;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace HaruhiHeiretsuCLI
{
    public class HexSearchCommand : Command
    {
        private string _mcb;
        private List<byte> _hexString = new();

        public HexSearchCommand() : base("hex-search")
        {
            Options = new()
            {
                "Searches the decompressed files in the MCB for a particular hex string and returns files/locations where it is found",
                "Usage: HaruhiHeiretsuCLI hex-search -m [MCB_FILE] -s [HEX_STRING]",
                "",
                { "m|mcb=", "MCB file", m => _mcb = m },
                {
                    "s|search=",
                    "Hex string to search for",
                    s =>
                    {
                        for (int i = 0; i < s.Length; i += 2)
                        {
                            _hexString.Add(byte.Parse(s.Substring(i, 2), NumberStyles.HexNumber));
                        }
                    }
                },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            return base.Invoke(arguments);
        }

        public async Task<int> InvokeAsync(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);
            McbFile mcb = Program.GetMcbFile(_mcb);

            List<(int, int)> fileLocations = await mcb.CheckHexInFile(_hexString.ToArray());
            using StreamWriter fs = File.CreateText("search_result_locations.csv");
            foreach ((int file, int subFile) in fileLocations)
            {
                fs.WriteLine($"{file},{subFile}");
            }

            return 0;
        }
    }
}
