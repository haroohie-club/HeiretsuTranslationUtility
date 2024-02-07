using HaruhiHeiretsuLib.Strings;
using System.Collections.Generic;

namespace HaruhiHeiretsuLib.Data
{
    /// <summary>
    /// An interface representing data files with strings in them
    /// </summary>
    public interface IDataStringsFile
    {
        /// <summary>
        /// Gets the strings that can be translated
        /// </summary>
        /// <returns>A list of DialogueLine objects representing strings to be translated in the file</returns>
        public List<DialogueLine> GetDialogueLines();
        /// <summary>
        /// Replaces a particular DialogueLine object with the provided string
        /// </summary>
        /// <param name="line">A DialogueLine object that should be repleaced</param>
        public void ReplaceDialogueLine(DialogueLine line);
    }
}
