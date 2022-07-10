using HaruhiHeiretsuLib.Strings;
using System.Collections.Generic;

namespace HaruhiHeiretsuLib.Data
{
    public interface IDataStringsFile
    {
        public List<DialogueLine> GetDialogueLines();
        public void ReplaceDialogueLine(DialogueLine line);
    }
}
