using System.Collections.Generic;
using System.IO;
using System.Resources.NetStandard;
using System.Text;

namespace HaruhiHeiretsuLib.Strings
{
    public partial class StringsFile : FileInArchive
    {
        public const string VOICE_REGEX = @"(CL|V)(\w\d{2}\w)?\w{3}\d{3}(?<characterCode>[A-Z]{3})";

        public List<DialogueLine> DialogueLines { get; set; } = new();

        public virtual void EditDialogue(int index, string newLine)
        {
        }

        protected (int oldLength, byte[] newLineData) DialogueEditSetUp(int index, string newLine)
        {
            Edited = true;
            string oldLine = DialogueLines[index].Line;
            newLine = newLine.Replace("\r\n", "\n"); // consolidate newlines
            DialogueLines[index].Line = newLine;
            int oldLength = Encoding.GetEncoding("Shift-JIS").GetByteCount(oldLine);
            byte[] newLineData = Encoding.GetEncoding("Shift-JIS").GetBytes(newLine);

            return (oldLength, newLineData);
        }

        public void WriteResxFile(string fileName)
        {
            using ResXResourceWriter resxWriter = new(fileName);
            for (int i = 0; i < DialogueLines.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(DialogueLines[i].Line) && DialogueLines[i].Length > 1)
                {
                    resxWriter.AddResource(new ResXDataNode($"{i:D4} ({Path.GetFileNameWithoutExtension(fileName)}) {DialogueLines[i].Speaker}",
                        DialogueLines[i].Line));
                }
            }
        }

        public override string ToString()
        {
            if (Location != (-1, -1))
            {
                return $"{McbId:X4}/{Location.parent},{Location.child}";
            }
            else
            {
                return $"{Index:X3} {Index:D4} 0x{Offset:X8}";
            }
        }
    }
}
