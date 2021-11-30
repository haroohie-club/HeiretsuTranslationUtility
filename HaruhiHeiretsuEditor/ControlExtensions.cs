using HaruhiHeiretsuLib;
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
        public ScriptFile ScriptFile { get; set; }
        public int DialogueLineIndex { get; set; }
    }
}
