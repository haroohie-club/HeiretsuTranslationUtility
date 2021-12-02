using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HaruhiHeiretsuLib
{
    public class ScriptFile
    {
        public const string VOICE_REGEX = @"V\d{3}\w{7}(?<characterCode>[A-Z]{3})";

        public bool Edited { get; set; } = false;
        public (int parent, int child) Location { get; set; }
        public List<byte> Data { get; set; }
        public List<DialogueLine> DialogueLines { get; set; } = new();

        public bool IsScript { get; set; } = true;

        public int NumCommandsOffset { get; set; }
        public short NumCommands { get; set; }
        public int NumShortPointersOffset { get; set; }
        public short NumShortPointers { get; set; }
        public int DialogueEndOffset { get; set; }
        public int DialogueEnd { get; set; }
        public int ShortPointersEndOffset { get; set; }
        public int ShortPointersEnd { get; set; }

        public List<ushort> ShortPointers { get; set; } = new();
        public List<ushort> UnknownShorts { get; set; } = new();
        public List<int> UnknownInts { get; set; } = new();

        public ScriptFile(int parent, int child, byte[] data)
        {
            Location = (parent, child);
            Data = data.ToList();

            ParseDialogue();
        }

        public void ParseDialogue()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (!Encoding.GetEncoding("Shift-JIS").GetString(Data.ToArray()).Contains("SCRIPT"))
            {
                IsScript = false;
                var matches = Regex.Matches(Encoding.ASCII.GetString(Data.ToArray()), @"V\d{3}\w{7}(?<characterCode>[A-Z]{3})");
                foreach (Match match in matches)
                {
                    Speaker speaker = DialogueLine.GetSpeaker(match.Groups[1].Value);
                    string line = Encoding.GetEncoding("Shift-JIS").GetString(Data.Skip(match.Index + 32).TakeWhile(b => b != 0x00).ToArray());
                    DialogueLines.Add(new DialogueLine { Offset = match.Index + 32, Line = line, Speaker = speaker });
                }
            }
            else
            {
                int numsToRead = 4;
                string lastLine = "";
                List<(int offset, string line)> lines = new();

                for (int i = 0; i < Data.Count;)
                {
                    int length = 0;
                    string line = "";
                    if (lastLine == "TIME" && numsToRead > 0)
                    {
                        line = "TIME";
                        switch (numsToRead)
                        {
                            case 4:
                                NumCommands = BitConverter.ToInt16(Data.Skip(i).Take(2).Reverse().ToArray());
                                NumCommandsOffset = i;
                                length = 2;
                                break;
                            case 3:
                                NumShortPointers = BitConverter.ToInt16(Data.Skip(i).Take(2).Reverse().ToArray());
                                NumShortPointersOffset = i;
                                length = 2;
                                break;
                            case 2:
                                DialogueEnd = BitConverter.ToInt32(Data.Skip(i).Take(4).Reverse().ToArray());
                                DialogueEndOffset = i;
                                length = 4;
                                break;
                            case 1:
                                ShortPointersEnd = BitConverter.ToInt32(Data.Skip(i).Take(4).Reverse().ToArray());
                                ShortPointersEndOffset = i;
                                length = 4;
                                break;
                            default:
                                break;
                        }

                        numsToRead--;
                    }
                    else
                    {
                        length = BitConverter.ToInt32(Data.Skip(i).Take(4).Reverse().ToArray()) - 1; // remove trailing 0x00
                        if (length < 0)
                        {
                            break;
                        }
                        line = Encoding.GetEncoding("Shift-JIS").GetString(Data.Skip(i + 4).Take(length).ToArray());
                        length += 5;

                        Match match = Regex.Match(line, VOICE_REGEX);
                        if (match.Success)
                        {
                            (int offset, string line) mostRecentLine = lines.Last();
                            if (Regex.IsMatch(mostRecentLine.line, @"^(\w\d{1,2})+$") && !Regex.IsMatch(lines[^2].line, @"^(\w\d{1,2})+$"))
                            {
                                mostRecentLine = lines[^2];
                            }
                            if (!Regex.IsMatch(mostRecentLine.line, VOICE_REGEX))
                            {
                                DialogueLines.Add(new DialogueLine
                                {
                                    Line = mostRecentLine.line,
                                    Offset = mostRecentLine.offset,
                                    Speaker = DialogueLine.GetSpeaker(match.Groups["characterCode"].Value)
                                });
                            }
                            lines.Add((i, line));
                        }
                        else
                        {
                            lines.Add((i, line));
                        }
                    }

                    lastLine = line;
                    i += length;
                }

                for (int i = DialogueEnd; i < ShortPointersEnd;)
                {
                    for (int j = 3; j >= 0; j--)
                    {
                        switch (j)
                        {
                            case 3:
                                ShortPointers.Add(BitConverter.ToUInt16(Data.Skip(i).Take(2).Reverse().ToArray()));
                                i += 2;
                                break;

                            case 2:
                                UnknownShorts.Add(BitConverter.ToUInt16(Data.Skip(i).Take(2).Reverse().ToArray()));
                                i += 2;
                                break;

                            case 1:
                                UnknownInts.Add(BitConverter.ToInt32(Data.Skip(i).Take(4).Reverse().ToArray()));
                                i += 4;
                                break;
                        }
                    }
                }
            }
        }

        public void EditDialogue(int index, string newLine)
        {
            Edited = true;
            string oldLine = DialogueLines[index].Line;
            DialogueLines[index].Line = newLine;
            int oldLength = Encoding.GetEncoding("Shift-JIS").GetByteCount(oldLine);
            byte[] newLineData = Encoding.GetEncoding("Shift-JIS").GetBytes(newLine);

            if (IsScript)
            {
                List<byte> newLineDataIncludingLength = new();
                newLineDataIncludingLength.AddRange(BitConverter.GetBytes(newLineData.Length + 1).Reverse());
                newLineDataIncludingLength.AddRange(newLineData);

                Data.RemoveRange(DialogueLines[index].Offset, oldLength + 4);
                Data.InsertRange(DialogueLines[index].Offset, newLineDataIncludingLength);

                int lengthDifference = newLineData.Length - oldLength;

                DialogueEnd += lengthDifference;
                Data.RemoveRange(DialogueEndOffset, 4);
                Data.InsertRange(DialogueEndOffset, BitConverter.GetBytes(DialogueEnd).Reverse());

                ShortPointersEnd = DialogueEnd + 8 * NumShortPointers;
                Data.RemoveRange(ShortPointersEndOffset, 4);
                Data.InsertRange(ShortPointersEndOffset, BitConverter.GetBytes(ShortPointersEnd).Reverse());

                for (int i = 0; i < ShortPointers.Count; i++)
                {
                    ShortPointers[i] += (ushort)lengthDifference;
                }

                for (int i = index + 1; i < DialogueLines.Count; i++)
                {
                    DialogueLines[i].Offset += lengthDifference;
                }
            }
            else
            {
                Data.RemoveRange(DialogueLines[index].Offset, newLineData.Length);
                Data.InsertRange(DialogueLines[index].Offset, newLineData);
            }
        }

        public override string ToString()
        {
            return $"{Location.parent},{Location.child}";
        }
    }

    public class DialogueLine
    {
        public string Line { get; set; }
        public Speaker Speaker { get; set; }
        public int Offset { get; set; }

        public override string ToString()
        {
            return $"{Speaker}: {Line}";
        }

        public static Speaker GetSpeaker(string code)
        {
            switch (code)
            {
                case "ANN":
                    return Speaker.ANN;
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
                    return Speaker.KYON_SIS;
                case "TAI":
                    return Speaker.TAIICHI;
                case "TAN":
                    return Speaker.TANIGUCHI;
                case "TRY":
                    return Speaker.TSURYA;
                default:
                    return Speaker.UNKNOWN;
            }
        }
    }

    public enum Speaker
    {
        ANN,
        CAPTAIN,
        CREW_F,
        CREW_M,
        GUEST_F1,
        GUEST_F2,
        GUEST_F3,
        GUEST_M1,
        GUEST_M2,
        GUEST_M3,
        HARUHI,
        KOIZUMI,
        KUNIKIDA,
        KYON,
        KYON2,
        MIKOTO,
        MIKURU,
        MONOLOGUE,
        NAGATO,
        NAGATO2,
        KYON_SIS,
        TAIICHI,
        TANIGUCHI,
        TSURYA,
        UNKNOWN,
    }
}
