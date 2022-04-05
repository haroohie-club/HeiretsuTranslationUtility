using HaruhiHeiretsuLib;
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
        private string _mcb, _dat, _evt, _scr, _replacementDir, _outputDir;
        public ReplaceStringsCommand() : base("replace-strings")
        {
            Options = new()
            {
                "Replaces strings in the mcb and dat/evt/scr archives",
                "Usage: HaruhiHeiretsuCLI repalce-strings -m [MCB_PATH] -d [DAT_BIN] -e [EVT_BIN] -s [SCR_BIN] -r [REPLACEMENT_FOLDER] -o [OUTPUT_FOLDER]",
                "",
                { "m|mcb=", "Path to mcb0.bln", m => _mcb = m },
                { "d|dat=", "Path to dat.bin", d => _dat = d },
                { "e|evt=", "Path to evt.bin", e => _evt = e },
                { "s|scr=", "Path to scr.bin", s => _scr = s },
                { "r|replacement=", "Path to replacement directory", r => _replacementDir = r },
                { "o|output=", "Path to output directory", o => _outputDir = o },
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
            ArchiveFile<ChokuretsuStringsFile> dat = ArchiveFile<ChokuretsuStringsFile>.FromFile(_dat);
            ArchiveFile<ScriptFile> evt = ArchiveFile<ScriptFile>.FromFile(_evt);
            ArchiveFile<ScriptFile> scr = ArchiveFile<ScriptFile>.FromFile(_scr);

            return 0;
        }
    }
}
