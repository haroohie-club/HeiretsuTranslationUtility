using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HaruhiHeiretsuLib
{
    public class ScriptFile : FileInArchive
    {
        public const string VOICE_REGEX = @"V\d{3}\w{7}(?<characterCode>[A-Z]{3})";

        public (int parent, int child) Location { get; set; } = (-1, -1);
        public List<DialogueLine> DialogueLines { get; set; } = new();
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

        public ScriptFile(int parent, int child, byte[] data, bool chokuretsu = false)
        {
            Location = (parent, child);
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

        public virtual void EditDialogue(int index, string newLine)
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
            else
            {
                Data.RemoveRange(DialogueLines[index].Offset, newLineData.Length);
                Data.InsertRange(DialogueLines[index].Offset, newLineData);
            }
        }

        public override string ToString()
        {
            if (Location != (-1, -1))
            {
                return $"{Location.parent},{Location.child}";
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

    public class ChokuretsuEventFile : ScriptFile
    {
        public List<int> FrontPointers { get; set; } = new();
        public int PointerToNumEndPointers { get; set; }
        public List<int> EndPointers { get; set; } = new();
        public List<int> EndPointerPointers { get; set; } = new();
        public string Title { get; set; }

        public Dictionary<int, string> DramatisPersonae { get; set; } = new();
        public int DialogueSectionPointer { get; set; }

        public ChokuretsuEventFile()
        {
        }

        public override void Initialize(byte[] decompressedData, int offset = 0)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Offset = offset;
            Data = decompressedData.ToList();

            int numFrontPointers = BitConverter.ToInt32(decompressedData.Take(4).Reverse().ToArray());
            bool reachedDramatisPersonae = false;
            for (int i = 0; i < numFrontPointers; i++)
            {
                FrontPointers.Add(BitConverter.ToInt32(decompressedData.Skip(0x0C + (0x08 * i)).Take(4).Reverse().ToArray()));
                uint pointerValue = BitConverter.ToUInt32(decompressedData.Skip(FrontPointers[i]).Take(4).Reverse().ToArray());
                if (pointerValue > 0x10000000 || pointerValue == 0x8596) // 8596 is 妹 which is a valid character name, sadly lol
                {
                    reachedDramatisPersonae = true;
                    DramatisPersonae.Add(FrontPointers[i],
                        Encoding.GetEncoding("Shift-JIS").GetString(decompressedData.Skip(FrontPointers[i]).TakeWhile(b => b != 0x00).ToArray()));
                }
                else if (reachedDramatisPersonae)
                {
                    reachedDramatisPersonae = false;
                    DialogueSectionPointer = FrontPointers[i];
                }
            }

            PointerToNumEndPointers = BitConverter.ToInt32(decompressedData.Skip(4).Take(4).Reverse().ToArray());
            int numEndPointers = BitConverter.ToInt32(decompressedData.Skip(PointerToNumEndPointers).Take(4).Reverse().ToArray());
            for (int i = 0; i < numEndPointers; i++)
            {
                EndPointers.Add(BitConverter.ToInt32(decompressedData.Skip(PointerToNumEndPointers + (0x04 * (i + 1))).Take(4).Reverse().ToArray()));
            }

            EndPointerPointers = EndPointers.Select(p => { int x = offset; return BitConverter.ToInt32(decompressedData.Skip(p).Take(4).Reverse().ToArray()); }).ToList();

            int titlePointer = BitConverter.ToInt32(decompressedData.Skip(0x08).Take(4).Reverse().ToArray());
            Title = Encoding.ASCII.GetString(decompressedData.Skip(titlePointer).TakeWhile(b => b != 0x00).ToArray());

            for (int i = 0; i < EndPointerPointers.Count; i++)
            {
                byte[] lineData = Data.Skip(EndPointerPointers[i]).TakeWhile(b => b != 0x00).ToArray();
                DialogueLines.Add(new DialogueLine
                {
                    Line = Encoding.GetEncoding("Shift-JIS").GetString(lineData),
                    Offset = EndPointerPointers[i],
                    Speaker = Speaker.ANNOUNCEMENT,
                    NumPaddingZeroes = 4 - (lineData.Length % 4),
            });
            }
        }

        public override void EditDialogue(int index, string newLine)
        {
            Edited = true;
            int oldLength = DialogueLines[index].Length + DialogueLines[index].NumPaddingZeroes;
            DialogueLines[index].Line = newLine;
            DialogueLines[index].NumPaddingZeroes = 4 - (DialogueLines[index].Length % 4);
            int lengthDifference = DialogueLines[index].Length + DialogueLines[index].NumPaddingZeroes - oldLength;

            List<byte> toWrite = new();
            toWrite.AddRange(Encoding.GetEncoding("Shift-JIS").GetBytes(DialogueLines[index].Line));
            for (int i = 0; i < DialogueLines[index].NumPaddingZeroes; i++)
            {
                toWrite.Add(0);
            }

            Data.RemoveRange(DialogueLines[index].Offset, oldLength);
            Data.InsertRange(DialogueLines[index].Offset, toWrite);

            ShiftPointers(DialogueLines[index].Offset, lengthDifference);
        }

        public void ShiftPointers(int shiftLocation, int shiftAmount)
        {
            for (int i = 0; i < FrontPointers.Count; i++)
            {
                if (FrontPointers[i] > shiftLocation)
                {
                    FrontPointers[i] += shiftAmount;
                    Data.RemoveRange(0x0C + (0x08 * i), 4);
                    Data.InsertRange(0x0C + (0x08 * i), BitConverter.GetBytes(FrontPointers[i]).Reverse());
                }
            }
            if (PointerToNumEndPointers > shiftLocation)
            {
                PointerToNumEndPointers += shiftAmount;
                Data.RemoveRange(0x04, 4);
                Data.InsertRange(0x04, BitConverter.GetBytes(PointerToNumEndPointers).Reverse());
            }
            for (int i = 0; i < EndPointers.Count; i++)
            {
                if (EndPointers[i] > shiftLocation)
                {
                    EndPointers[i] += shiftAmount;
                    Data.RemoveRange(PointerToNumEndPointers + 0x04 * (i + 1), 4);
                    Data.InsertRange(PointerToNumEndPointers + 0x04 * (i + 1), BitConverter.GetBytes(EndPointers[i]).Reverse());
                }
            }
            for (int i = 0; i < EndPointerPointers.Count; i++)
            {
                if (EndPointerPointers[i] > shiftLocation)
                {
                    EndPointerPointers[i] += shiftAmount;
                    Data.RemoveRange(EndPointers[i], 4);
                    Data.InsertRange(EndPointers[i], BitConverter.GetBytes(EndPointerPointers[i]).Reverse());
                }
            }
            foreach (DialogueLine dialogueLine in DialogueLines)
            {
                if (dialogueLine.Offset > shiftLocation)
                {
                    dialogueLine.Offset += shiftAmount;
                }
            }
        }
    }
}
