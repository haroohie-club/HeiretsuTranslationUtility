using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HaruhiHeiretsuLib.Strings
{
    public class EventFile : StringsFile
    {
        public EventFile()
        {
        }

        public override void Initialize(byte[] decompressedData, int offset)
        {
            Offset = offset;
            Data = decompressedData.ToList();

            ParseDialogue();
        }

        public EventFile(int parent, int child, byte[] data, int mcbId = 0)
        {
            Location = (parent, child);
            McbId = mcbId;
            Data = data.ToList();

            ParseDialogue();
        }

        public void ParseDialogue()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var matches = Regex.Matches(Encoding.ASCII.GetString(Data.ToArray()), VOICE_REGEX);
            foreach (Match match in matches)
            {
                Speaker speaker = DialogueLine.GetSpeaker(match.Groups["characterCode"].Value);
                string line = Encoding.GetEncoding("Shift-JIS").GetString(Data.Skip(match.Index + 32).TakeWhile(b => b != 0x00).ToArray());
                DialogueLines.Add(new DialogueLine { Offset = match.Index + 32, Line = line, Speaker = speaker });
            }
        }

        public override void EditDialogue(int index, string newLine)
        {
            (_, byte[] newLineData) = DialogueEditSetUp(index, newLine);

            Data.RemoveRange(DialogueLines[index].Offset, newLineData.Length);
            Data.InsertRange(DialogueLines[index].Offset, newLineData);
        }
    }
}
