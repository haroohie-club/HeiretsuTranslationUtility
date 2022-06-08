using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace HaruhiHeiretsuLib.Strings
{
    public class ScriptFile : StringsFile
    {
        public string Name { get; set; }
        public string InternalName { get; set; }
        public string Room { get; set; }
        public string Time { get; set; }

        public List<ScriptCommand> AvailableCommands { get; set; }
        public List<ScriptCommandBlock> ScriptCommandBlocks { get; set; } = new();
        public List<ScriptCommandInvocation> CommandInvocations { get; set; } = new();

        public List<string> Parameters { get; set; } = new();

        public int NumParametersOffset { get; set; }
        public short NumParameters { get; set; }
        public int NumScriptCommandBlocksOffset { get; set; }
        public short NumScriptCommandBlocks { get; set; }
        public int ParametersEndOffset { get; set; }
        public int ParametersEnd { get; set; }
        public int ScriptCommandBlockDefinitionsEndOffset { get; set; }
        public int ScriptCommandBlockDefinitionsEnd { get; set; }
       

        public ScriptFile()
        {
        }

        public ScriptFile(int parent, int child, byte[] data, short mcbId = 0)
        {
            Location = (parent, child);
            McbId = mcbId;
            Data = data.ToList();

            ParseScript();
        }

        public override void Initialize(byte[] decompressedData, int offset)
        {
            Offset = offset;
            Data = decompressedData.ToList();

            ParseScript();
        }

        public override byte[] GetBytes() => Data.ToArray();

        private string ReadString(IEnumerable<byte> data, int currentPosition, out int newPosition)
        {
            int stringLength = BitConverter.ToInt32(data.Skip(currentPosition).Take(4).Reverse().ToArray());
            string result = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(currentPosition + 4).Take(stringLength - 1).ToArray());
            newPosition = currentPosition + stringLength + 4;
            return result;
        }

        public void ParseScript()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            int pos = 0;
            InternalName = ReadString(Data, pos, out pos);

            if (!InternalName.Contains("script", StringComparison.OrdinalIgnoreCase) && !InternalName.Contains("タイトル"))
            {
                InternalName = "";
                return;
            }

            Room = ReadString(Data, pos, out pos);
            Time = ReadString(Data, pos, out pos);

            NumParameters = BitConverter.ToInt16(Data.Skip(pos).Take(2).Reverse().ToArray());
            NumParametersOffset = pos;
            pos += 2;

            NumScriptCommandBlocks = BitConverter.ToInt16(Data.Skip(pos).Take(2).Reverse().ToArray());
            NumScriptCommandBlocksOffset = pos;
            pos += 2;

            ParametersEnd = BitConverter.ToInt32(Data.Skip(pos).Take(4).Reverse().ToArray());
            ParametersEndOffset = pos;
            pos += 4;

            ScriptCommandBlockDefinitionsEnd = BitConverter.ToInt32(Data.Skip(pos).Take(4).Reverse().ToArray());
            ScriptCommandBlockDefinitionsEndOffset = pos;
            pos += 4;

            for (int i = 0; i < NumParameters; i++)
            {
                Parameters.Add(ReadString(Data, pos, out pos));
            }

            for (int i = ParametersEnd; i < ScriptCommandBlockDefinitionsEnd; i += 0x08)
            {
                int endAddress;
                ushort endParams;
                if (i + 8 == ScriptCommandBlockDefinitionsEnd)
                {
                    endAddress = Data.Count;
                    endParams = (ushort)Parameters.Count;
                }
                else
                {
                    endAddress = BitConverter.ToInt32(Data.Skip(i + 12).Take(4).Reverse().ToArray());
                    endParams = BitConverter.ToUInt16(Data.Skip(i + 8).Take(2).Reverse().ToArray());
                }
                ScriptCommandBlocks.Add(new(i, endAddress, endParams, Data, Parameters));
            }

            //for (int i = 0; i < Data.Count;)
            //{
            //    int length = 0;
            //    string line;
            //    if (lastLine == "TIME" && numsToRead > 0)
            //    {
            //        line = "TIME";
            //        switch (numsToRead)
            //        {
            //            case 4:
            //                NumParameters = BitConverter.ToInt16(Data.Skip(i).Take(2).Reverse().ToArray());
            //                NumParametersOffset = i;
            //                length = 2;
            //                break;
            //            case 3:
            //                NumScriptCommandBlocks = BitConverter.ToInt16(Data.Skip(i).Take(2).Reverse().ToArray());
            //                NumScriptCommandBlocksOffset = i;
            //                length = 2;
            //                break;
            //            case 2:
            //                ParametersEnd = BitConverter.ToInt32(Data.Skip(i).Take(4).Reverse().ToArray());
            //                ParametersEndOffset = i;
            //                length = 4;
            //                break;
            //            case 1:
            //                ScriptCommandBlockDefinitionsEnd = BitConverter.ToInt32(Data.Skip(i).Take(4).Reverse().ToArray());
            //                ScriptCommandBlockDefinitionsEndOffset = i;
            //                length = 4;
            //                break;
            //            default:
            //                break;
            //        }

            //        numsToRead--;
            //    }
            //    else
            //    {
            //        length = BitConverter.ToInt32(Data.Skip(i).Take(4).Reverse().ToArray()) - 1; // remove trailing 0x00
            //        if (length < 0)
            //        {
            //            break;
            //        }
            //        line = Encoding.GetEncoding("Shift-JIS").GetString(Data.Skip(i + 4).Take(length).ToArray());
            //        length += 5;

            //        Match match = Regex.Match(line, VOICE_REGEX);
            //        if (match.Success)
            //        {
            //            (int offset, string line) mostRecentLine = lines.Last();
            //            if (Regex.IsMatch(mostRecentLine.line, @"^(\w\d{1,2})+$") && !Regex.IsMatch(lines[^2].line, @"^(\w\d{1,2})+$"))
            //            {
            //                mostRecentLine = lines[^2];
            //            }
            //            if (!Regex.IsMatch(mostRecentLine.line, VOICE_REGEX))
            //            {
            //                DialogueLines.Add(new DialogueLine
            //                {
            //                    Line = mostRecentLine.line,
            //                    Offset = mostRecentLine.offset,
            //                    Speaker = DialogueLine.GetSpeaker(match.Groups["characterCode"].Value)
            //                });
            //            }
            //            lines.Add((i, line));
            //        }
            //        else
            //        {
            //            lines.Add((i, line));
            //        }
            //    }

            //    Parameters.Add(line);
            //    lastLine = line;
            //    i += length;
            //}
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

            ParametersEnd += lengthDifference;
            Data.RemoveRange(ParametersEndOffset, 4);
            Data.InsertRange(ParametersEndOffset, BitConverter.GetBytes(ParametersEnd).Reverse());

            ScriptCommandBlockDefinitionsEnd = ParametersEnd + 8 * NumScriptCommandBlocks;
            Data.RemoveRange(ScriptCommandBlockDefinitionsEndOffset, 4);
            Data.InsertRange(ScriptCommandBlockDefinitionsEndOffset, BitConverter.GetBytes(ScriptCommandBlockDefinitionsEnd).Reverse());

            for (int i = 0; i < ScriptCommandBlocks.Count; i++)
            {
                ScriptCommandBlocks[i].Address += lengthDifference;
                ScriptCommandBlocks[i].Offset += lengthDifference;

                Data.RemoveRange(ScriptCommandBlocks[i].Address + 4, 4);
                Data.InsertRange(ScriptCommandBlocks[i].Address + 4, BitConverter.GetBytes(ScriptCommandBlocks[i].Offset).Reverse());
            }

            for (int i = index + 1; i < DialogueLines.Count; i++)
            {
                DialogueLines[i].Offset += lengthDifference;
            }
        }

        public void PopulateCommandBlocks()
        {
            for (int i = 0; i < ScriptCommandBlocks.Count; i++)
            {
                ScriptCommandBlocks[i].PopulateCommands(AvailableCommands, Data);
            }
        }

        public static List<string> ParseScriptListFile(byte[] scriptListFileData)
        {
            List<string> scriptList = new();

            int numScripts = BitConverter.ToInt32(scriptListFileData.Take(4).Reverse().ToArray());

            for (int i = 0; i < numScripts; i++)
            {
                scriptList.Add(Encoding.ASCII.GetString(scriptListFileData.Skip(8 + i * 36).TakeWhile(b => b != 0).ToArray()));
            }

            return scriptList;
        }

        public override string ToString()
        {
            if (Location != (-1, -1))
            {
                return $"{McbId:X4}/{Location.parent},{Location.child} {Name}";
            }
            else
            {
                return $"{Index:X3} {Index:D4} 0x{Offset:X8} {Name}";
            }
        }
    }

    public class ScriptCommand
    {
        public short Index { get; set; }
        public ushort NumberOfParameters => (ushort)Parameters.Count;
        public string Name { get; set; }
        public List<ushort> Parameters { get; set; } = new();
        public int DefinitionLength { get; set; }

        public static ScriptCommand DeletedScriptCommand => new() { Name = "DELETED", Index = -1 };

        private ScriptCommand()
        {
        }

        public ScriptCommand(IEnumerable<byte> data)
        {
            Index = BitConverter.ToInt16(data.Take(2).Reverse().ToArray());
            ushort numParams = BitConverter.ToUInt16(data.Skip(2).Take(2).Reverse().ToArray());
            int nameLength = BitConverter.ToInt32(data.Skip(4).Take(4).Reverse().ToArray());
            Name = Encoding.ASCII.GetString(data.Skip(8).Take(nameLength - 1).ToArray()); // minus one bc of the terminal character \x00 that we want to avoid
            for (ushort i = 0; i < numParams; i++)
            {
                Parameters.Add(BitConverter.ToUInt16(data.Skip(8 + nameLength + i * 2).Take(2).Reverse().ToArray()));
            }
            DefinitionLength = 8 + nameLength + numParams * 2;
        }

        public static List<ScriptCommand> ParseScriptCommandFile(byte[] scriptCommandFileData)
        {
            int numCommands = BitConverter.ToInt32(scriptCommandFileData.Take(4).Reverse().ToArray());
            List<ScriptCommand> scriptCommands = new();

            for (int i = 4; scriptCommands.Count < numCommands;)
            {
                ScriptCommand scriptCommand = new(scriptCommandFileData.Skip(i));
                scriptCommands.Add(scriptCommand);
                i += scriptCommand.DefinitionLength;
            }

            return scriptCommands;
        }

        public override string ToString()
        {
            return Name;
        }

        public string GetSignature()
        {
            return $"{Index} {Name}({string.Join(", ", Parameters.Select(s => $"{s:X4}"))})";
        }

        public static int GetParameterLength(short paramTypeCode, IEnumerable<byte> data)
        {
            switch (paramTypeCode)
            {
                case 1:
                case 21:
                case 28:
                    return 2;
                case 0:
                case 10:
                    return 4;
                case 5:
                case 6:
                case 8:
                case 9:
                case 11:
                case 12:
                case 13:
                case 14:
                case 16:
                case 18:
                case 19:
                case 23:
                case 24:
                case 25:
                case 26:
                case 27:
                case 41:
                case 42:
                    return 8;
                case 3:
                    return 12;
                case 4:
                case 7:
                case 15:
                    return 16;
                case 20:
                    return 24;
                case 2:
                    return BitConverter.ToInt16(data.Take(2).Reverse().ToArray()); // *a2
                case 17:
                case 22:
                    return 8 * BitConverter.ToInt32(data.Take(4).Reverse().ToArray()) + 4; // 8 * *(_DWORD *)a2 + 4
                case 29:
                    return BitConverter.ToInt32(data.Take(4).Reverse().ToArray()); // *(_DWORD *)a2
                default:
                    return 0;
            }
        }
    }

    public class ScriptCommandBlock
    {
        public int Address { get; set; }
        public ushort NameIndex { get; set; }
        public string Name { get; set; }
        public ushort Unknown { get; set; }
        public int Offset { get; set; }
        public List<ScriptCommandInvocation> Invocations { get; set; } = new();
        List<string> Parameters { get; set; }

        public ScriptCommandBlock(int address, int endAddress, ushort endParams, IEnumerable<byte> data, List<string> parameters)
        {
            Address = address;
            NameIndex = BitConverter.ToUInt16(data.Skip(address).Take(2).Reverse().ToArray());
            Name = parameters[NameIndex];
            Parameters = parameters.Skip(NameIndex + 1).Take(endParams - NameIndex + 1).ToList();
            Unknown = BitConverter.ToUInt16(data.Skip(address + 2).Take(2).Reverse().ToArray());
            Offset = BitConverter.ToInt32(data.Skip(address + 4).Take(4).Reverse().ToArray());
            
            for (int i = Offset; i < endAddress - 8;)
            {
                ScriptCommandInvocation invocation = new();
                invocation.LineNumber = BitConverter.ToInt16(data.Skip(i).Take(2).Reverse().ToArray());
                i += 4;
                invocation.CommandCode = BitConverter.ToInt16(data.Skip(i).Take(2).Reverse().ToArray());
                i += 2;
                short numParams = BitConverter.ToInt16(data.Skip(i).Take(2).Reverse().ToArray());
                i += 6;
                for (int j = 0; j < numParams; j++)
                {
                    short paramTypeCode = BitConverter.ToInt16(data.Skip(i).Take(2).Reverse().ToArray());
                    i += 2;
                    int paramLength = ScriptCommand.GetParameterLength(paramTypeCode, data.Skip(i));
                    switch (paramTypeCode)
                    {
                        case 0:
                            int zeroValue = BitConverter.ToInt32(data.Skip(i).Take(4).Reverse().ToArray());
                            invocation.Parameters.Add((typeof(int), paramTypeCode, zeroValue, $"0x{zeroValue:X4}"));
                            break;
                        case 10:
                            int[] tenValue = new int[]
                            {
                                BitConverter.ToInt32(data.Skip(i).Take(4).Reverse().ToArray()),
                                BitConverter.ToInt32(data.Skip(i + 4).Take(4).Reverse().ToArray())
                            };
                            invocation.Parameters.Add((typeof(int[]), paramTypeCode, tenValue, $"[{string.Join(", ", tenValue.Select(v => $"{v:X8}"))}]"));
                            break;
                        default:
                            string defaultValue = string.Join(" ", data.Skip(i).Take(paramLength).Select(b => $"{b:X2}"));
                            invocation.Parameters.Add((typeof(string), paramTypeCode, defaultValue, defaultValue));
                            break;
                    }
                    i += paramLength;
                }
                Invocations.Add(invocation);
            }
        }

        public void PopulateCommands(List<ScriptCommand> availableCommands, IEnumerable<byte> Data)
        {
            for (int i = 0; i < Invocations.Count; i++)
            {
                if (Invocations[i].CommandCode < 0)
                {
                    Invocations[i].Command = ScriptCommand.DeletedScriptCommand;
                }
                else
                {
                    Invocations[i].Command = availableCommands[Invocations[i].CommandCode];
                }
            }
        }
    }

    public class ScriptCommandInvocation
    {
        public short LineNumber { get; set; }
        public short CommandCode { get; set; }
        public ScriptCommand Command { get; set; }
        public List<(Type type, short typeCode, object value, string display)> Parameters { get; set; } = new();

        public override string ToString()
        {
            if (Command is not null)
            {
                return Command.ToString();
            }
            else
            {
                return $"{CommandCode:X4}";
            }
        }

        public string GetInvocation()
        {
            string invocation = $"{LineNumber} {Command.Name}(";
            for (int i = 0; i < Parameters.Count; i++)
            {
                invocation += $"{Parameters[i].typeCode:X4} {Parameters[i].display}, ";
            }
            if (Parameters.Count > 0)
            {
                invocation = invocation[0..^2];
            }
            return $"{invocation})";
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
