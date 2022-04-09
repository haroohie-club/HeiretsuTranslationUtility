using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HaruhiHeiretsuLib.Strings
{
    public class ScriptFile : StringsFile
    {
        public List<string> Commands { get; set; } = new();

        public bool IsScript { get; set; } = true;

        public int NumCommandsOffset { get; set; }
        public short NumCommands { get; set; }
        public int NumShortPointersOffset { get; set; }
        public short NumShortPointers { get; set; }
        public int CommandsEndOffset { get; set; }
        public int CommandsEnd { get; set; }
        public int ShortPointersEndOffset { get; set; }
        public int ShortPointersEnd { get; set; }

        public List<int> PostCommandOffsets { get; set; } = new();
        public List<ushort> UnknownShorts1 { get; set; } = new();
        public List<ushort> UnknownShorts2 { get; set; } = new();
        public List<int> PostCommandPointers { get; set; } = new();
       

        public ScriptFile()
        {
        }

        public ScriptFile(int parent, int child, byte[] data, int mcbId = 0)
        {
            Location = (parent, child);
            McbId = mcbId;
            Data = data.ToList();

            ParseDialogue();
        }

        public override void Initialize(byte[] decompressedData, int offset)
        {
            Offset = offset;
            Data = decompressedData.ToList();

            ParseDialogue();
        }

        public override byte[] GetBytes() => Data.ToArray();

        public void ParseDialogue()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

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
                            CommandsEnd = BitConverter.ToInt32(Data.Skip(i).Take(4).Reverse().ToArray());
                            CommandsEndOffset = i;
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

                Commands.Add(line);
                lastLine = line;
                i += length;
            }

            if (Commands.Count > 7)
            {
                // remove commands that don't count
                Commands.RemoveRange(0, 7);

                for (int i = CommandsEnd; i < ShortPointersEnd;)
                {
                    PostCommandOffsets.Add(i);
                    for (int j = 3; j >= 0; j--)
                    {
                        switch (j)
                        {
                            case 3:
                                UnknownShorts1.Add(BitConverter.ToUInt16(Data.Skip(i).Take(2).Reverse().ToArray()));
                                i += 2;
                                break;

                            case 2:
                                UnknownShorts2.Add(BitConverter.ToUInt16(Data.Skip(i).Take(2).Reverse().ToArray()));
                                i += 2;
                                break;

                            case 1:
                                PostCommandPointers.Add(BitConverter.ToInt32(Data.Skip(i).Take(4).Reverse().ToArray()));
                                i += 4;
                                break;
                        }
                    }
                }
            }
        }

        public override void EditDialogue(int index, string newLine)
        {
            (int oldLength, byte[] newLineData) = DialogueEditSetUp(index, newLine);

            List<byte> newLineDataIncludingLength = new();
            newLineDataIncludingLength.AddRange(BitConverter.GetBytes(newLineData.Length + 1).Reverse());
            newLineDataIncludingLength.AddRange(newLineData);

            Data.RemoveRange(DialogueLines[index].Offset, oldLength + 4);
            Data.InsertRange(DialogueLines[index].Offset, newLineDataIncludingLength);

            int lengthDifference = newLineData.Length - oldLength;

            CommandsEnd += lengthDifference;
            Data.RemoveRange(CommandsEndOffset, 4);
            Data.InsertRange(CommandsEndOffset, BitConverter.GetBytes(CommandsEnd).Reverse());

            ShortPointersEnd = CommandsEnd + 8 * NumShortPointers;
            Data.RemoveRange(ShortPointersEndOffset, 4);
            Data.InsertRange(ShortPointersEndOffset, BitConverter.GetBytes(ShortPointersEnd).Reverse());

            for (int i = 0; i < UnknownShorts1.Count; i++)
            {
                PostCommandOffsets[i] += lengthDifference;
                PostCommandPointers[i] += lengthDifference;
                List<byte> postCommandsInsertionList = new();
                postCommandsInsertionList.AddRange(BitConverter.GetBytes(UnknownShorts1[i]).Reverse());
                postCommandsInsertionList.AddRange(BitConverter.GetBytes(UnknownShorts2[i]).Reverse());
                postCommandsInsertionList.AddRange(BitConverter.GetBytes(PostCommandPointers[i]).Reverse());

                Data.RemoveRange(PostCommandOffsets[i], 8);
                Data.InsertRange(PostCommandOffsets[i], postCommandsInsertionList);
            }

            for (int i = index + 1; i < DialogueLines.Count; i++)
            {
                DialogueLines[i].Offset += lengthDifference;
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
                    return Speaker.KYON_SIS;
                case "TAI":
                    return Speaker.TAIICHIRO;
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
        ANNOUNCEMENT,
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
        TAIICHIRO,
        TANIGUCHI,
        TSURYA,
        UNKNOWN,
    }
}
