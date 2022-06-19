using System.Collections;
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

        public void ImportResxFile(string fileName)
        {
            Edited = true;
            string resxContents = File.ReadAllText(fileName);
            resxContents = resxContents.Replace("System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                "System.Resources.NetStandard.ResXResourceWriter, System.Resources.NetStandard, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
            resxContents = resxContents.Replace("System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                "System.Resources.NetStandard.ResXResourceReader, System.Resources.NetStandard, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
            TextReader textReader = new StringReader(resxContents);

            using ResXResourceReader resxReader = new(textReader);
            foreach (DictionaryEntry d in resxReader)
            {
                int dialogueIndex = int.Parse(((string)d.Key)[0..4]);
                string dialogueText = (string)d.Value;

                // Replace all faux-ellipses with an ellipsis character
                dialogueText = dialogueText.Replace("...", "…");
                // Replace all faux-em-dashes with actual em-dash characters
                dialogueText = dialogueText.Replace("--", "—");
                // Consolidate Unix/Windows newlines to just \n
                dialogueText = dialogueText.Replace("\r\n", "\n");

                int lineLength = 0;
            }
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

    public class DialogueLine
    {
        public string Line { get; set; }
        public Speaker Speaker { get; set; }
        public int Offset { get; set; }
        public int Length => Encoding.GetEncoding("Shift-JIS").GetByteCount(Line);
        public int NumPaddingZeroes { get; set; } = 1;

        public List<string> Metadata { get; set; } = new();

        public override string ToString()
        {
            return $"{Speaker}: {Line}";
        }

        public static Speaker GetSpeaker(string code)
        {
            switch (code)
            {
                case "ANN":
                    return Speaker.ANNOUNCEMENT;
                case "CAP":
                    return Speaker.CAPTAIN;
                case "CRF":
                    return Speaker.CREW_F;
                case "CRM":
                    return Speaker.CREW_M;
                case "GF1":
                    return Speaker.GUEST_F1;
                case "GF2":
                    return Speaker.GUEST_F2;
                case "GF3":
                    return Speaker.GUEST_F3;
                case "GM1":
                    return Speaker.GUEST_M1;
                case "GM2":
                    return Speaker.GUEST_M2;
                case "GM3":
                    return Speaker.GUEST_M3;
                case "HRH":
                    return Speaker.HARUHI;
                case "KZM":
                    return Speaker.KOIZUMI;
                case "KUN":
                    return Speaker.KUNIKIDA;
                case "KYN":
                    return Speaker.KYON;
                case "KY2":
                    return Speaker.KYON2;
                case "MKT":
                    return Speaker.MIKOTO;
                case "MKR":
                    return Speaker.MIKURU;
                case "MNL":
                    return Speaker.MONOLOGUE;
                case "NGT":
                    return Speaker.NAGATO;
                case "NG2":
                    return Speaker.NAGATO2;
                case "SIS":
                    return Speaker.KYN_SIS;
                case "TAI":
                    return Speaker.TAIICHIRO;
                case "TAN":
                    return Speaker.TANIGUCHI;
                case "TRY":
                    return Speaker.TSURUYA;
                default:
                    return Speaker.UNKNOWN;
            }
        }
    }

    public enum Speaker
    {
        UNKNOWN = -3,
        CHOICE = -2,
        MONOLOGUE = -1,
        KYON,
        KYON2,
        HARUHI,
        NAGATO,
        NAGATO2,
        MIKURU,
        MIKURU2,
        KOIZUMI,
        KOIZUMI2,
        TSURUYA,
        KYN_SIS,
        MIKOTO,
        MIKOTO2,
        TAIICHIRO,
        TAIICHIRO2,
        CAPTAIN,
        GUEST_M3,
        GUEST_F3,
        CREW_F,
        CREW_M,
        GUEST_M1,
        GUEST_M2,
        GUEST_F1,
        GUEST_F2,
        TANIGUCHI,
        KUNIKIDA,
        ANNOUNCEMENT,
        CHAIRMAN,
        SHOP_EMPLOYEE,
        THREE_QUESTIONS,
        ANOTHER_ONE,
        ANOTHER_TWO,
        CREW_32,
    }
}
