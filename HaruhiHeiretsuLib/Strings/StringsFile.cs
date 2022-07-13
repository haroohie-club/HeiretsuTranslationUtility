﻿using HaruhiHeiretsuLib.Archive;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public virtual void ImportResxFile(string fileName, FontReplacementMap fontReplacementMap)
        {
            Edited = true;
        }

        public void WriteResxFile(string fileName)
        {
            using ResXResourceWriter resxWriter = new(fileName);
            for (int i = 0; i < DialogueLines.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(DialogueLines[i].Line) && DialogueLines[i].Length > 1)
                {
                    resxWriter.AddResource(new ResXDataNode($"{i:D4} ({Path.GetFileNameWithoutExtension(fileName)}) {DialogueLines[i].Speaker}{(DialogueLines[i].Metadata.Count > 0 ? $" - {string.Join(", ", DialogueLines[i].Metadata)}" : "")}",
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

        public static TextReader GetResxReader(string fileName)
        {
            string resxContents = File.ReadAllText(fileName);
            resxContents = resxContents.Replace("System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                "System.Resources.NetStandard.ResXResourceWriter, System.Resources.NetStandard, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
            resxContents = resxContents.Replace("System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                "System.Resources.NetStandard.ResXResourceReader, System.Resources.NetStandard, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
            return new StringReader(resxContents);
        }

        public static string NormalizeDialogueLine(string dialogueText)
        {
            // Replace all faux-ellipses with an ellipsis character
            dialogueText = dialogueText.Replace("...", "…");
            // Replace all faux-em-dashes with actual em-dash characters
            dialogueText = dialogueText.Replace("--", "—");
            // Consolidate Unix/Windows newlines to just \n
            dialogueText = dialogueText.Replace("\r\n", "\n");
            // We start by replacing all quotes with the open quotes, then will replace them with closed quotes as we go
            return dialogueText.Replace("\"", "“");
        }

        public static string ProcessDialogueLineWithFontReplacement(string dialogueText, FontReplacementMap fontReplacementMap, int dialogueLineLength)
        {
            int lineLength = 0;
            for (int i = 0; i < dialogueText.Length; i++)
            {
                if (dialogueText[i] == '#' && dialogueText.Length - i >= 3)
                {
                    // skip replacement/line length increment for operators
                    if (dialogueText[i + 1] == 'b' && dialogueText[i + 2] == 'w' || dialogueText[i + 1] == 'F')
                    {
                        i += 2;
                        continue;
                    }
                    else if (dialogueText.Length - i >= 5 && dialogueText[i + 1] == 'b' && dialogueText[i + 2] == 't')
                    {
                        i += 2 + dialogueText.Skip(i + 2).TakeWhile(c => char.IsNumber(c)).Count();
                        continue;
                    }
                }

                if (dialogueText[i] == '“' && (i == dialogueText.Length - 1
                    || dialogueText[i + 1] == ' ' || dialogueText[i + 1] == '!' || dialogueText[i + 1] == '?' || dialogueText[i + 1] == '.' || dialogueText[i + 1] == '…' || dialogueText[i + 1] == '\n'))
                {
                    dialogueText = dialogueText.Remove(i, 1);
                    dialogueText = dialogueText.Insert(i, "”");
                }

                if (fontReplacementMap.ContainsReplacement($"{dialogueText[i]}"))
                {
                    lineLength += fontReplacementMap.GetReplacementCharacterWidth($"{dialogueText[i]}");
                    string replacement = $"{dialogueText[i]}";
                    dialogueText = dialogueText.Remove(i, 1);
                    dialogueText = dialogueText.Insert(i, fontReplacementMap.GetStartCharacterForReplacement(replacement));
                }

                if (dialogueText[i] == '\n')
                {
                    lineLength = 0;
                }

                if (dialogueText[i] != ' ' && lineLength > dialogueLineLength)
                {
                    int indexOfMostRecentSpace = dialogueText[0..i].LastIndexOf(' ');
                    dialogueText = dialogueText.Remove(indexOfMostRecentSpace, 1);
                    dialogueText = dialogueText.Insert(indexOfMostRecentSpace, "\n");
                    lineLength = 0;
                }
            }

            return dialogueText;
        }
    }

    public class DialogueLine
    {
        public string Line { get; set; }
        public string Speaker { get; set; }
        public int Offset { get; set; }
        public int Length => Encoding.GetEncoding("Shift-JIS").GetByteCount(Line);
        public int NumPaddingZeroes { get; set; } = 1;

        public List<string> Metadata { get; set; } = new();

        public override string ToString()
        {
            return $"{Speaker}: {Line}";
        }

        public static ScriptFileSpeaker GetSpeaker(string code)
        {
            switch (code)
            {
                case "ANN":
                    return ScriptFileSpeaker.ANNOUNCEMENT;
                case "CAP":
                    return ScriptFileSpeaker.CAPTAIN;
                case "CRF":
                    return ScriptFileSpeaker.CREW_F;
                case "CRM":
                    return ScriptFileSpeaker.CREW_M;
                case "GF1":
                    return ScriptFileSpeaker.GUEST_F1;
                case "GF2":
                    return ScriptFileSpeaker.GUEST_F2;
                case "GF3":
                    return ScriptFileSpeaker.GUEST_F3;
                case "GM1":
                    return ScriptFileSpeaker.GUEST_M1;
                case "GM2":
                    return ScriptFileSpeaker.GUEST_M2;
                case "GM3":
                    return ScriptFileSpeaker.GUEST_M3;
                case "HRH":
                    return ScriptFileSpeaker.HARUHI;
                case "KZM":
                    return ScriptFileSpeaker.KOIZUMI;
                case "KUN":
                    return ScriptFileSpeaker.KUNIKIDA;
                case "KYN":
                    return ScriptFileSpeaker.KYON;
                case "KY2":
                    return ScriptFileSpeaker.KYON2;
                case "MKT":
                    return ScriptFileSpeaker.MIKOTO;
                case "MKR":
                    return ScriptFileSpeaker.MIKURU;
                case "MNL":
                    return ScriptFileSpeaker.MONOLOGUE;
                case "NGT":
                    return ScriptFileSpeaker.NAGATO;
                case "NG2":
                    return ScriptFileSpeaker.NAGATO2;
                case "SIS":
                    return ScriptFileSpeaker.KYNSIS;
                case "TAI":
                    return ScriptFileSpeaker.TAIICHIRO;
                case "TAN":
                    return ScriptFileSpeaker.TANIGUCHI;
                case "TRY":
                    return ScriptFileSpeaker.TSURUYA;
                default:
                    return ScriptFileSpeaker.UNKNOWN;
            }
        }
    }

    public enum ScriptFileSpeaker
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
        KYNSIS,
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

    public enum EventFileSpeaker
    {
        MONOLOGUE = -1,
        KYON = 0,
        HARUHI = 1,
        NAGATO = 2,
        MIKURU = 3,
        KOIZUMI = 4,
        TSURUYA = 5,
        KYNSIS = 6,
        MIKOTO = 7,
        TAIICHIRO = 8,
        KYON2 = 9,
        NAGATO2 = 10,
        MIKURU2 = 11, // guess
        KOIZUMI2 = 12, // guess
        CAPTAIN = 14,
        CREW_FEMALE = 17,
        CREW_MALE = 18,
        GUEST_MALE2 = 20,
        GUEST_FEMALE1 = 21,
        TANIGUCHI = 23,
        KUNIKIDA = 24,
    }
}
