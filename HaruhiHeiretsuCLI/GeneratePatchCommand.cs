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

            XmlElement tlSection = xml.CreateElement("section");
            tlSection.SetAttribute("name", "Haroohie Translation Club Translation Patch");
            XmlElement tlOption = xml.CreateElement("option");
            tlOption.SetAttribute("name", "Full Game Translation");
            XmlElement tlChoice = xml.CreateElement("choice");
            tlChoice.SetAttribute("name", "Enabled");
            XmlElement tlChoicePatch = xml.CreateElement("patch");
            tlChoicePatch.SetAttribute("id", "HeiretsuTranslation");
            tlChoice.AppendChild(tlChoicePatch);
            tlOption.AppendChild(tlChoice);
            tlSection.AppendChild(tlOption);
            options.AppendChild(tlSection);

            XmlElement restorationSection = xml.CreateElement("section");
            restorationSection.SetAttribute("name", "Feature Restoration");

            XmlElement spVerOption = xml.CreateElement("option");
            spVerOption.SetAttribute("name", "Restore Special Version");
            XmlElement spVerChoice = xml.CreateElement("choice");
            spVerChoice.SetAttribute("name", "Enabled");
            XmlElement spVerChoicePatch = xml.CreateElement("patch");
            spVerChoicePatch.SetAttribute("id", "RestoreSpecialVersion");
            spVerChoice.AppendChild(spVerChoicePatch);
            spVerOption.AppendChild(spVerChoice);
            restorationSection.AppendChild(spVerOption);

            XmlElement commandOption = xml.CreateElement("option");
            commandOption.SetAttribute("name", "Restore Some Commands");
            XmlElement commandChoice = xml.CreateElement("choice");
            commandChoice.SetAttribute("name", "Enabled");
            XmlElement commandChoicePatch = xml.CreateElement("patch");
            commandChoicePatch.SetAttribute("id", "RestoreCommands");
            commandChoice.AppendChild(commandChoicePatch);
            commandOption.AppendChild(commandChoice);
            restorationSection.AppendChild(commandOption);

            options.AppendChild(restorationSection);

            root.AppendChild(options);

            XmlElement patch = xml.CreateElement("patch");
            patch.SetAttribute("id", "HeiretsuTranslation");
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
