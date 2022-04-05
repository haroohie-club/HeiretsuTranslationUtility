using Mono.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaruhiHeiretsuCLI
{
    public class ReplaceStringsCommand : Command
    {
        public ReplaceStringsCommand() : base("replace-strings")
        {
            Options = new()
            {
                "Replaces strings in the mcb and dat/evt/scr archives",
                "Usage: HaruhiHeiretsuCLI repalce-strings -m [MCB_PATH] -d [DAT_BIN] -e [EVT_BIN] -s [SCR_BIN] -r [REPLACEMENT_FOLDER] -o [OUTPUT_FOLDER]"
            };
        }
    }
}
