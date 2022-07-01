using HaruhiHeiretsuLib.Data;
using HaruhiHeiretsuLib.Graphics;
using HaruhiHeiretsuLib.Strings;
using HaruhiHeiretsuLib.Strings.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace HaruhiHeiretsuEditor
{
    public class DialogueTextBox : TextBox
    {
        public StringsFile StringsFile { get; set; }
        public int DialogueLineIndex { get; set; }
    }

    public class ScriptButton : Button
    {
        public ScriptFile Script { get; set; }
    }

    public class GraphicsButton : Button
    {
        public GraphicsFile Graphic { get; set; }
    }

    public class MapDefinitionButton : Button
    {
        public MapDefinitionsFile MapDefFile { get; set; }
    }
    public class CameraDataButton : Button
    {
        public CameraDataFile CamDataFile { get; set; }
    }
}
