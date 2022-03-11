using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace HaruhiHeiretsuCLI
{
    public class GeneratePatchCommand : Command
    {
        private string _outputDir;

        public GeneratePatchCommand() : base("generate-patch")
        {
            Options = new()
            {
                "Generate the base Riivolution patch",
                "Usage: HaruhiHeiretsuCLI generate-patch -o [OUTPUT_DIR]",
                "",
                { "o|output=", "Directory to place patch in", o => _outputDir = o },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);

            Directory.CreateDirectory(_outputDir);

            XmlDocument xml = new();
            XmlElement root = xml.CreateElement("wiidisc");
            root.SetAttribute("version", "1");
            xml.AppendChild(root);

            XmlElement id = xml.CreateElement("id");
            id.SetAttribute("game", "R44J8P");
            root.AppendChild(id);

            XmlElement options = xml.CreateElement("options");
            XmlElement section = xml.CreateElement("section");
            section.SetAttribute("name", "Heiretsu Translation");
            XmlElement option = xml.CreateElement("option");
            option.SetAttribute("name", "Heiretsu Translation");
            XmlElement choice = xml.CreateElement("choice");
            choice.SetAttribute("name", "Enabled");
            XmlElement choicePatch = xml.CreateElement("patch");
            choicePatch.SetAttribute("id", "HeiretsuFolder");
            choice.AppendChild(choicePatch);
            option.AppendChild(choice);
            section.AppendChild(option);
            options.AppendChild(section);
            root.AppendChild(options);

            XmlElement patch = xml.CreateElement("patch");
            patch.SetAttribute("id", "HeiretsuFolder");
            XmlElement folderRecurse = xml.CreateElement("folder");
            folderRecurse.SetAttribute("external", "/Heiretsu/files");
            folderRecurse.SetAttribute("recursive", "true");
            XmlElement folderDisc = xml.CreateElement("folder");
            folderDisc.SetAttribute("external", "/Heiretsu/files");
            folderRecurse.SetAttribute("disc", "/");
            patch.AppendChild(folderRecurse);
            patch.AppendChild(folderDisc);
            root.AppendChild(patch);

            xml.Save(Path.Combine(_outputDir, "Heiretsu_base.xml"));

            return 0;
        }
    }
}
