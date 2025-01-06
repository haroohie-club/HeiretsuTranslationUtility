using HaruhiHeiretsuLib.Archive;
using HaruhiHeiretsuLib.Strings.Scripts;
using Mono.Options;
using System.Collections.Generic;
using System.IO;

namespace HaruhiHeiretsuCLI
{
    internal class ExportScriptsCommand : Command
    {
        private string _scr, _outputDir;
        private bool _includeIdx;
        public ExportScriptsCommand() : base("export-scripts", "Export all scripts as SWS files")
        {
            Options = new()
            {
                { "i|s|input|scr|input-scr=", "Input scr.bin file", s => _scr = s },
                { "o|output=", "Output directory", o => _outputDir = o },
                { "x|idx", "Include file indices in the outputted file names (e.g. '0 - SCRIPT.sws' vs 'SCRIPT.sws')", x => _includeIdx = true },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);

            if (!Directory.Exists(_outputDir))
            {
                Directory.CreateDirectory(_outputDir);
            }

            BinArchive<ScriptFile> scr = BinArchive<ScriptFile>.FromFile(_scr);
            List<string> scriptFileNames = ScriptFile.ParseScriptListFile([.. scr.Files[0].Data]);
            List<ScriptCommand> availableCommands = ScriptCommand.ParseScriptCommandFile([.. scr.Files[1].Data]);
            for (int i = 0; i < scr.Files.Count; i++)
            {
                if (scr.Files[i].ScriptCommandBlocks.Count > 0)
                {
                    scr.Files[i].Name = scriptFileNames[i];
                    scr.Files[i].AvailableCommands = availableCommands;
                    scr.Files[i].PopulateCommandBlocks();
                    File.WriteAllText(Path.Combine(_outputDir, $"{(_includeIdx ? $"{scr.Files[i].BinArchiveIndex:D3} - " : "")}{scr.Files[i].Name}.sws"), scr.Files[i].Decompile());
                }
            }

            return 0;
        }
    }
}
