using HaruhiHeiretsuLib.Archive;
using HaruhiHeiretsuLib.Strings.Events;
using Mono.Options;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HaruhiHeiretsuCLI
{
    public class ExportEventJsonCommand : Command
    {
        private string _evt, _output;
        private int _index;
        private bool _all, _formatted;

        public ExportEventJsonCommand() : base("export-event-json", "Export an event file to JSON")
        {
            Options = new()
            {
                { "e|evt=", "evt.bin", e => _evt = e },
                { "i|index=", "The index of the evt file to export", i => _index = int.Parse(i) },
                { "o|output=", "The location to output the JSON file", o => _output = o },
                { "a|all", "Dumps all events if set", a => _all = true },
                { "f|formatted", "Formats JSON (very large, don't do unless wanting to view directly)", f => _formatted = true },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);
            
            BinArchive<EventFile> evt = BinArchive<EventFile>.FromFile(_evt);
            JsonSerializerOptions options = new()
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                IncludeFields = true,
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
                WriteIndented = _formatted,
            };

            if (_all)
            {
                foreach (EventFile eventFile in evt.Files)
                {
                    if (eventFile.CutsceneData is null)
                    {
                        continue;
                    }
                    File.WriteAllText(Path.Combine(_output, $"{eventFile.BinArchiveIndex:000}.json"), JsonSerializer.Serialize(eventFile.CutsceneData, options));
                }
            }
            else
            {
                EventFile eventFile = evt.Files.First(f => f.BinArchiveIndex == _index);
                File.WriteAllText(_output, JsonSerializer.Serialize(eventFile.CutsceneData, options));
            }
            

            return 0;
        }
    }
}
