using HaruhiHeiretsuLib;
using HaruhiHeiretsuLib.Graphics;
using HaruhiHeiretsuLib.Strings;
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

    public class MapButton : Button
    {
        public GraphicsFile Map { get; set; }
    }
}
