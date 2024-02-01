using HaruhiHeiretsuLib.Archive;
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
        private readonly List<byte> _hexString = [];
        private bool _fourByteAligned = false;

        public HexSearchCommand() : base("hex-search")
        {
            Options =
            [
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
                { "4|four-byte-aligned", "When included, restricts search to starting on four-byte alignments", f => _fourByteAligned = true },
            ];
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);
            McbArchive mcb = Program.GetMcbFile(_mcb);

            List<(int, int)> fileLocations = mcb.CheckHexInFile([.. _hexString], _fourByteAligned);
            using StreamWriter fs = File.CreateText("search_result_locations.csv");
            foreach ((int file, int subFile) in fileLocations)
            {
                fs.WriteLine($"{file},{subFile}");
            }

            return 0;
        }
    }
}
