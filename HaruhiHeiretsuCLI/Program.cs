using HaruhiHeiretsuLib;
using Mono.Options;
using System;
using System.Threading.Tasks;

namespace HaruhiHeiretsuCLI
{
    class Program
    {
        public enum Mode
        {
            FIND_STRINGS
        }

        static void Main(string[] args)
        {

            Mode mode = Mode.FIND_STRINGS;
            string file = "";

            OptionSet options = new()
            {
                "Usage: HaruhiHeiretsuCLI -f MCB_FILE",
                { "f|file=", f => file = f },
                { "find-strings", m => mode = Mode.FIND_STRINGS }
            };

            options.Parse(args);

            string indexFile = "", dataFile = "";
            if (file.Contains("0"))
            {
                indexFile = file;
                dataFile = file.Replace("0", "1");
            }
            else
            {
                indexFile = file.Replace("1", "0");
                dataFile = file;
            }

            MainAsync(mode, indexFile, dataFile).GetAwaiter().GetResult();
        }

        public static async Task MainAsync(Mode mode, string indexFile, string dataFile)
        {
            McbFile mcb = new(indexFile, dataFile);

            if (mode == Mode.FIND_STRINGS)
            {
                await mcb.FindStringFiles();
            }
        }
    }
}
