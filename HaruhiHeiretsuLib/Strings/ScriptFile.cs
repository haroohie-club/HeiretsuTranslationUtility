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

            if (InternalName.Length > 20)
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

        public string GetScript()
        {
            string script = "";
            int currentLine = 1;

            foreach (ScriptCommandBlock commandBlock in ScriptCommandBlocks)
            {
                script += $"== {commandBlock.Name} ==\n";
                currentLine++;

                if (commandBlock.Invocations.Count == 0)
                {
                    script += "\n";
                    currentLine++;
                }
                
                foreach (ScriptCommandInvocation invocation in commandBlock.Invocations)
                {
                    while (currentLine < invocation.LineNumber)
                    {
                        script += "\n";
                        currentLine++;
                    }

                    script += $"{invocation.GetInvocation()}\n";
                    currentLine++;
                }
            }

            return script;
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
            return $"{Index:X2} {Name}({string.Join(", ", Parameters.Select(s => $"{s:X4}"))})";
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
        public ushort NumInvocations { get; set; }
        public int Offset { get; set; }
        public List<ScriptCommandInvocation> Invocations { get; set; } = new();

        public ScriptCommandBlock(int address, int endAddress, ushort endParams, IEnumerable<byte> data, List<string> parameters)
        {
            Address = address;
            NameIndex = BitConverter.ToUInt16(data.Skip(address).Take(2).Reverse().ToArray());
            Name = parameters[NameIndex];
            NumInvocations = BitConverter.ToUInt16(data.Skip(address + 2).Take(2).Reverse().ToArray());
            Offset = BitConverter.ToInt32(data.Skip(address + 4).Take(4).Reverse().ToArray());
            
            for (int i = Offset; i < endAddress - 8;)
            {
                ScriptCommandInvocation invocation = new(parameters);
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
                    invocation.Parameters.Add((paramTypeCode, data.Skip(i).Take(paramLength).ToArray()));
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
        private List<string> _scriptParams;

        public short LineNumber { get; set; }
        public short CommandCode { get; set; }
        public ScriptCommand Command { get; set; }
        public List<(short typeCode, byte[] value)> Parameters { get; set; } = new();

        public ScriptCommandInvocation(List<string> scriptParams)
        {
            _scriptParams = scriptParams;
        }

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
            string invocation = $"{Command.Name}(";
            switch (Command.Name)
            {
                case "EV_MODE":
                    invocation += Helpers.GetIntFromByteArray(Parameters[GetParameterPosition(10, 0)].value, 0) != 0 ? "TRUE" : "FALSE";
                    for (int i = 0; ; i++)
                    {
                        int pos = GetParameterPosition(25, i);
                        if (pos < 0)
                        {
                            break;
                        }

                        byte[] data = Parameters[pos].value;
                        invocation += $", {Calculate5TypeParameter(Helpers.GetIntFromByteArray(data, 0), Helpers.GetIntFromByteArray(data, 1))}";
                    }
                    break;
                case "SCENE_SET":
                    var sceneSetFirst = Parameters[GetParameterPosition(5, 0)];
                    var sceneSetSecond = Parameters[GetParameterPosition(5, 1)];
                    string firstFlag = Calculate5TypeParameter(Helpers.GetIntFromByteArray(sceneSetFirst.value, 0), Helpers.GetIntFromByteArray(sceneSetFirst.value, 1));
                    string secondFlag = Calculate5TypeParameter(Helpers.GetIntFromByteArray(sceneSetSecond.value, 0), Helpers.GetIntFromByteArray(sceneSetSecond.value, 1));
                    invocation += $"{firstFlag}, {secondFlag}";
                    break;
                case "WAIT":
                    int waitFirstPosition = GetParameterPosition(3, 0);
                    if (waitFirstPosition >= 0)
                    {
                        invocation += Calculate5TypeParameter(Helpers.GetIntFromByteArray(Parameters[waitFirstPosition].value, 1), Helpers.GetIntFromByteArray(Parameters[waitFirstPosition].value, 2));
                    }
                    break;
                case "SET":
                    var setFirst = Parameters[GetParameterPosition(5, 0)];
                    var setSecond = Parameters[GetParameterPosition(5, 1)];
                    string value = Calculate5TypeParameter(Helpers.GetIntFromByteArray(setSecond.value, 0), Helpers.GetIntFromByteArray(setSecond.value, 1));
                    int setCommandCode = Helpers.GetIntFromByteArray(setFirst.value, 0);
                    int setVariableIndex = Helpers.GetIntFromByteArray(setFirst.value, 1);

                    if (setCommandCode == 0x302 && setVariableIndex >= 0x1303 && setVariableIndex <= 0x1403)
                    {
                        invocation += $"(int)MEMORY[{0x801FBA00 + (setVariableIndex - 0x2E3E) * 4:X8}], {value}";
                    }
                    else if (setCommandCode == 0x304)
                    {
                        invocation += $"{_scriptParams[setVariableIndex]}, {value}";
                    }
                    else
                    {
                        invocation += $"Unknown";
                    }
                    break;
                case "LOG":
                case "MW":
                case "UI_DATEPLACE":
                case "SET_EAR":
                case "MENU_LOCK":
                case "SMENU_LOCK":
                case "POINT_INVALID":
                case "SET_DV":
                case "UI_PLACE":
                    invocation += Helpers.GetIntFromByteArray(Parameters[GetParameterPosition(10, 0)].value, 0) != 0 ? "TRUE" : "FALSE";
                    break;
                case "EV_START":
                    var evStartFirst = Parameters[GetParameterPosition(5, 0)].value;
                    string eventIndex = Calculate5TypeParameter(Helpers.GetIntFromByteArray(evStartFirst, 0), Helpers.GetIntFromByteArray(evStartFirst, 1));
                    invocation += eventIndex;
                    var evStartSecond = Parameters.ElementAtOrDefault(GetParameterPosition(22, 0)).value;
                    if (evStartSecond is not null)
                    {
                        List<string> values = new();
                        int numValues = Helpers.GetIntFromByteArray(evStartSecond, 0);
                        for (int i = 1; i <= numValues; i++)
                        {
                            values.Add(Calculate5TypeParameter(Helpers.GetIntFromByteArray(evStartSecond, i * 2 - 1), Helpers.GetIntFromByteArray(evStartSecond, i * 2)));
                        }
                        invocation += $", [{string.Join(", ", values)}]";
                    }
                    for (int i = 1; ; i++)
                    {
                        int pos = GetParameterPosition(5, i);
                        if (pos < 0)
                        {
                            break;
                        }

                        byte[] data = Parameters[pos].value;
                        invocation += $", {Calculate5TypeParameter(Helpers.GetIntFromByteArray(data, 0), Helpers.GetIntFromByteArray(data, 1))}";
                    }
                    break;
                case "APPEAR":
                    int appearFirstPosition = GetParameterPosition(10, 0);
                    if (appearFirstPosition >= 0)
                    {
                        invocation += Helpers.GetIntFromByteArray(Parameters[GetParameterPosition(10, 0)].value, 0) != 0 ? "TRUE" : "FALSE";
                    }
                    break;
                default:
                    for (int i = 0; i < Parameters.Count; i++)
                    {
                        invocation += $"{Parameters[i].typeCode:X4} {string.Join(" ", Parameters[i].value.Select(b => $"{b:X2}"))}, ";
                    }
                    if (Parameters.Count > 0)
                    {
                        invocation = invocation[0..^2];
                    }
                    break;
            }
            return $"{invocation})";
        }

        public string Calculate5TypeParameter(int controlCode, int valueCode)
        {
            switch (controlCode)
            {
                case 0x300:
                    return $"{valueCode}";
                case 0x302:
                    if (valueCode - 0x124F < 0)
                    {
                        throw new ArgumentException("Illegal system flag index");
                    }
                    return $"MEMORY[{0x801FBA00 + 4 * (valueCode - 0x2E3E):X8}]";
                case 0x303:
                    if (valueCode >= 0x1000 && valueCode <= 0x1020)
                    {
                        switch (valueCode)
                        {
                            case 0x1000:
                                return "MEMORY[801F2C0B]";
                            case 0x1001:
                                return "MEMORY[801FBE30]";
                            case 0x1002:
                                return "(int)MEMORY[r13 - 0x73E0] + 0x1620";
                            case 0x1003:
                                return "(int)MEMORY[r13 - 0x73E0] + 0x161C";
                            case 0x1004:
                                return "MEMORY[801F2C0C]";
                            case 0x1005:
                                return "(ushort)MEMORY[801F2C14]";
                            default:
                                return "-1";
                        }
                    }
                    if (valueCode >= 0x1100 && valueCode <= 0x1200)
                    {
                        int temp = valueCode - 0x1100;
                        if (valueCode >= 0x280 && valueCode <= 0x47F)
                        {
                            return $"MEMORY[{0x801FBA00 + (temp + 0x7504) * 4}:X8]";
                        }
                    }
                    if (valueCode >= 0x1201 && valueCode <= 0x4865)
                    {
                        return "-1";
                    }
                    return "-1";
                case 0x304:
                    return _scriptParams[valueCode];
                default:
                    return "-1";
            }
        }

        public int GetParameterPosition(int targetCommandCode, int paramNum)
        {
            int currentParam = 0;
            for (int i = 0; i < Parameters.Count; i++)
            {
                if (Parameters[i].typeCode == targetCommandCode)
                {
                    if (currentParam == paramNum)
                    {
                        return i;
                    }

                    currentParam++;
                }
            }
            return -1;
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
