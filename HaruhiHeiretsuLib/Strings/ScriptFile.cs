using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Globalization;

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

        public List<string> Variables { get; set; } = new();

        public int NumVariablesOffset { get; set; }
        public short NumVariables { get; set; }
        public int NumScriptCommandBlocksOffset { get; set; }
        public short NumScriptCommandBlocks { get; set; }
        public int VariablesEndOffset { get; set; }
        public int VariablesEnd { get; set; }
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

            NumVariables = BitConverter.ToInt16(Data.Skip(pos).Take(2).Reverse().ToArray());
            NumVariablesOffset = pos;
            pos += 2;

            NumScriptCommandBlocks = BitConverter.ToInt16(Data.Skip(pos).Take(2).Reverse().ToArray());
            NumScriptCommandBlocksOffset = pos;
            pos += 2;

            VariablesEnd = BitConverter.ToInt32(Data.Skip(pos).Take(4).Reverse().ToArray());
            VariablesEndOffset = pos;
            pos += 4;

            ScriptCommandBlockDefinitionsEnd = BitConverter.ToInt32(Data.Skip(pos).Take(4).Reverse().ToArray());
            ScriptCommandBlockDefinitionsEndOffset = pos;
            pos += 4;

            for (int i = 0; i < NumVariables; i++)
            {
                Variables.Add(ReadString(Data, pos, out pos));
            }

            for (int i = VariablesEnd; i < ScriptCommandBlockDefinitionsEnd; i += 0x08)
            {
                int endAddress;
                if (i + 8 == ScriptCommandBlockDefinitionsEnd)
                {
                    endAddress = Data.Count;
                }
                else
                {
                    endAddress = BitConverter.ToInt32(Data.Skip(i + 12).Take(4).Reverse().ToArray());
                }
                ScriptCommandBlocks.Add(new(i, endAddress, Data, Variables));
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

            VariablesEnd += lengthDifference;
            Data.RemoveRange(VariablesEndOffset, 4);
            Data.InsertRange(VariablesEndOffset, BitConverter.GetBytes(VariablesEnd).Reverse());

            ScriptCommandBlockDefinitionsEnd = VariablesEnd + 8 * NumScriptCommandBlocks;
            Data.RemoveRange(ScriptCommandBlockDefinitionsEndOffset, 4);
            Data.InsertRange(ScriptCommandBlockDefinitionsEndOffset, BitConverter.GetBytes(ScriptCommandBlockDefinitionsEnd).Reverse());

            for (int i = 0; i < ScriptCommandBlocks.Count; i++)
            {
                ScriptCommandBlocks[i].DefinitionAddress += lengthDifference;
                ScriptCommandBlocks[i].BlockOffset += lengthDifference;

                Data.RemoveRange(ScriptCommandBlocks[i].DefinitionAddress + 4, 4);
                Data.InsertRange(ScriptCommandBlocks[i].DefinitionAddress + 4, BitConverter.GetBytes(ScriptCommandBlocks[i].BlockOffset).Reverse());
            }

            for (int i = index + 1; i < DialogueLines.Count; i++)
            {
                DialogueLines[i].Offset += lengthDifference;
            }
        }

        public string Decompile()
        {
            string script = "";
            int currentLine = 1;

            script += $"{InternalName} {Room} {Time}\n";
            currentLine++;
            foreach (ScriptCommandBlock commandBlock in ScriptCommandBlocks)
            {
                script += $"== {commandBlock.Name} ==\n";
                currentLine++;

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

        public void Compile(string code)
        {
            List<byte> bytes = new();
            string[] lines = code.Split('\n');
            string[] info = lines[0].Split(' ');
            InternalName = info[0];
            Room = info[1];
            Time = info[2];
            bytes.AddRange(Helpers.GetStringBytes(InternalName));
            bytes.AddRange(Helpers.GetStringBytes(Room));
            bytes.AddRange(Helpers.GetStringBytes(Time));

            ScriptCommandBlocks = new();
            Variables = new();

            for (int lineNumber = 2; lineNumber < lines.Length;)
            {
                if (string.IsNullOrWhiteSpace(lines[lineNumber - 1]))
                {
                    lineNumber++;
                    continue;
                }
                if (lines[lineNumber - 1].StartsWith("=="))
                {
                    ScriptCommandBlock commandBlock = new();
                    lineNumber = commandBlock.ParseBlock(lineNumber, lines[(lineNumber - 1)..], AvailableCommands, Variables);
                    ScriptCommandBlocks.Add(commandBlock);
                }
            }

            NumVariables = (short)Variables.Count;
            NumScriptCommandBlocks = (short)ScriptCommandBlocks.Count;

            NumVariablesOffset = bytes.Count;
            bytes.AddRange(BitConverter.GetBytes(NumVariables).Reverse());
            NumScriptCommandBlocksOffset = bytes.Count;
            bytes.AddRange(BitConverter.GetBytes(NumScriptCommandBlocks).Reverse());
            VariablesEndOffset = bytes.Count;
            bytes.AddRange(BitConverter.GetBytes(0));
            ScriptCommandBlockDefinitionsEndOffset = bytes.Count;
            bytes.AddRange(BitConverter.GetBytes(0));

            foreach (string variable in Variables)
            {
                bytes.AddRange(Helpers.GetStringBytes(variable));
            }

            VariablesEnd = bytes.Count;
            bytes.RemoveRange(VariablesEndOffset, 4);
            bytes.InsertRange(VariablesEndOffset, BitConverter.GetBytes(VariablesEnd).Reverse());

            foreach (ScriptCommandBlock scriptCommandBlock in ScriptCommandBlocks)
            {
                scriptCommandBlock.DefinitionAddress = bytes.Count;
                bytes.AddRange(BitConverter.GetBytes(scriptCommandBlock.NameIndex).Reverse());
                bytes.AddRange(BitConverter.GetBytes(scriptCommandBlock.NumInvocations).Reverse());
                bytes.AddRange(BitConverter.GetBytes(0));
            }

            ScriptCommandBlockDefinitionsEnd = bytes.Count;
            bytes.RemoveRange(ScriptCommandBlockDefinitionsEndOffset, 4);
            bytes.InsertRange(ScriptCommandBlockDefinitionsEndOffset, BitConverter.GetBytes(ScriptCommandBlockDefinitionsEnd).Reverse());

            foreach (ScriptCommandBlock scriptCommandBlock in ScriptCommandBlocks)
            {
                scriptCommandBlock.BlockOffset = bytes.Count;
                bytes.RemoveRange(scriptCommandBlock.DefinitionAddress + 4, 4);
                bytes.InsertRange(scriptCommandBlock.DefinitionAddress + 4, BitConverter.GetBytes(scriptCommandBlock.BlockOffset).Reverse());

                foreach (ScriptCommandInvocation invocation in scriptCommandBlock.Invocations)
                {
                    invocation.Address = bytes.Count;
                    bytes.AddRange(invocation.GetBytes());
                }
            }

            foreach (ScriptCommandBlock scriptCommandBlock in ScriptCommandBlocks)
            {
                foreach (ScriptCommandInvocation invocation in scriptCommandBlock.Invocations)
                {
                    invocation.ScriptVariables = Variables;
                    invocation.AllOtherInvocations = ScriptCommandBlocks.SelectMany(b => b.Invocations).ToList();
                    if (invocation.ResolveAddresses())
                    {
                        bytes.RemoveRange(invocation.Address, invocation.Length);
                        bytes.InsertRange(invocation.Address, invocation.GetBytes());
                    }
                }
            }

            Data = bytes;
        }

        public void PopulateCommandBlocks()
        {
            List<ScriptCommandInvocation> allInvocations = ScriptCommandBlocks.SelectMany(b => b.Invocations).ToList();
            for (int i = 0; i < ScriptCommandBlocks.Count; i++)
            {
                ScriptCommandBlocks[i].PopulateCommands(AvailableCommands);
                for (int j = 0; j < ScriptCommandBlocks[i].Invocations.Count; j++)
                {
                    ScriptCommandBlocks[i].Invocations[j].AllOtherInvocations = allInvocations;
                }
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

        public enum ParameterType
        {
            ADDRESS = 0,
            DIALOGUE = 1,
            CONDITIONAL = 2,
            TIMESPAN = 3,
            UNKNOWN04 = 4,
            INT = 5,
            INT06 = 6,
            UNKNOWN07 = 7,
            INT08 = 8,
            UNKNOWN09 = 9,
            BOOL = 10,
            UNKNOWN0B = 11,
            UNKNOWN0C = 12,
            CHARACTER = 13,
            INT0E = 14,
            UNKNOWN0F = 15,
            INT10 = 16,
            UNKNOWN11 = 17,
            UNKNOWN12 = 18,
            UNKNOWN13 = 19,
            VECTOR3 = 20,
            VARINDEX = 21,
            INTARRAY = 22,
            UNKNOWN17 = 23,
            UNKNOWN18 = 24,
            INT19 = 25,
            UNKNOWN1A = 26,
            UNKNOWN1B = 27,
            UNKNOWN1C = 28,
            UNKNOWN1D = 29,
            UNKNOWN29 = 41,
            UNKNOWN2A = 42,
        }

        public static readonly Dictionary<short, string> CharacterCodeToCharacterMap = new()
        {
            { 0, "KYON" },
            { 2, "HARUHI" },
            { 3, "NAGATO" },
            { 5, "MIKURU" },
            { 7, "KOIZUMI" },
            { 10, "KYN_SIS" },
        };

        public static readonly Dictionary<string, byte> ComparisonOperatorToCodeMap = new()
        {
            { "==", 0x83 },
            { "!=", 0x84 },
            { ">", 0x85 },
            { "<", 0x86 },
            { ">=", 0x87 },
            { "<=", 0x88 },
        };
    }

    public class ScriptCommandBlock
    {
        public int DefinitionAddress { get; set; } // Location of the script command block *definition*
        public ushort NameIndex { get; set; }
        public string Name { get; set; }
        public ushort NumInvocations { get; set; }
        public int BlockOffset { get; set; } // Location of the actual script command block
        public List<ScriptCommandInvocation> Invocations { get; set; } = new();
        public int Length => Invocations.Sum(i => i.Length);

        public ScriptCommandBlock(int address, int endAddress, IEnumerable<byte> data, List<string> variables)
        {
            DefinitionAddress = address;
            NameIndex = BitConverter.ToUInt16(data.Skip(address).Take(2).Reverse().ToArray());
            Name = variables[NameIndex];
            NumInvocations = BitConverter.ToUInt16(data.Skip(address + 2).Take(2).Reverse().ToArray());
            BlockOffset = BitConverter.ToInt32(data.Skip(address + 4).Take(4).Reverse().ToArray());

            for (int i = BlockOffset; i < endAddress - 8;)
            {
                ScriptCommandInvocation invocation = new(variables, i);
                invocation.LineNumber = BitConverter.ToInt16(data.Skip(i).Take(2).Reverse().ToArray());
                i += 2;
                invocation.CharacterEntity = BitConverter.ToInt16(data.Skip(i).Take(2).Reverse().ToArray());
                i += 2;
                invocation.CommandCode = BitConverter.ToInt16(data.Skip(i).Take(2).Reverse().ToArray());
                i += 2;
                short numParams = BitConverter.ToInt16(data.Skip(i).Take(2).Reverse().ToArray());
                i += 6;
                for (int j = 0; j < numParams; j++)
                {
                    short paramTypeCode = BitConverter.ToInt16(data.Skip(i).Take(2).Reverse().ToArray());
                    i += 2;
                    int paramLength = ScriptCommand.GetParameterLength(paramTypeCode, data.Skip(i));
                    invocation.Parameters.Add(new Parameter() { Type = (ScriptCommand.ParameterType)paramTypeCode, Value = data.Skip(i).Take(paramLength).ToArray() });
                    i += paramLength;
                }
                Invocations.Add(invocation);
            }
        }

        public ScriptCommandBlock()
        {
        }

        public void PopulateCommands(List<ScriptCommand> availableCommands)
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

        public int ParseBlock(int lineNumber, string[] lines, List<ScriptCommand> allCommands, List<string> variables)
        {
            Regex nameRegex = new(@"== (?<name>.+) ==");
            Match nameMatch = nameRegex.Match(lines[0]);
            if (!nameMatch.Success)
            {
                throw new ArgumentException($"Name {lines[0]} not a valid block name!");
            }

            Name = nameMatch.Groups["name"].Value;
            variables.Add(Name);
            NameIndex = (ushort)(variables.Count - 1);

            int i = 1;
            for (; i < lines.Length; i++)
            {
                if (nameRegex.IsMatch(lines[i]))
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                Invocations.Add(new ScriptCommandInvocation(lines[i], (short)(i + lineNumber), allCommands, variables));
            }

            NumInvocations = (ushort)Invocations.Count;

            return i + lineNumber;
        }
    }

    public struct Parameter
    {
        public ScriptCommand.ParameterType Type { get; set; }
        public byte[] Value { get; set; }
    }

    public class ScriptCommandInvocation
    {
        public int Address { get; set; }
        public short LineNumber { get; set; }
        public short CharacterEntity { get; set; }
        public short CommandCode { get; set; }
        public ScriptCommand Command { get; set; }
        public List<string> ScriptVariables { get; set; }
        public List<Parameter> Parameters { get; set; } = new();
        public int Length => 12 + Parameters.Sum(p => 2 + p.Value.Length);

        public List<ScriptCommandInvocation> AllOtherInvocations { get; set; }

        public ScriptCommandInvocation(List<string> scriptVariables, int address)
        {
            ScriptVariables = scriptVariables;
            Address = address;
        }

        public ScriptCommandInvocation(string invocation, short lineNumber, List<ScriptCommand> allCommands, List<string> variables)
        {
            ParseInvocation(invocation, lineNumber, allCommands, variables);
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

        public byte[] GetBytes()
        {
            List<byte> bytes = new();

            bytes.AddRange(BitConverter.GetBytes(LineNumber).Reverse());
            bytes.AddRange(BitConverter.GetBytes(CharacterEntity).Reverse());
            bytes.AddRange(BitConverter.GetBytes(CommandCode).Reverse());
            bytes.AddRange(BitConverter.GetBytes((short)Parameters.Count).Reverse());
            bytes.AddRange(BitConverter.GetBytes(0));

            bytes.AddRange(Parameters.SelectMany(p =>
            {
                List<byte> bytes = new();
                bytes.AddRange(BitConverter.GetBytes((short)p.Type).Reverse());
                bytes.AddRange(p.Value);
                return bytes;
            }));

            return bytes.ToArray();
        }

        public string GetInvocation()
        {
            string invocation = Command.Name;
            if (CharacterEntity >= 0)
            {
                invocation += $"<{GetCharacter(CharacterEntity)}>";
            }
            invocation += "(";
            for (int i = 0; i < Parameters.Count; i++)
            {
                if (i > 0)
                {
                    invocation += ", ";
                }
                invocation += ParseParameter(Parameters[i]);
            }
            return $"{invocation})";
        }

        public void ParseInvocation(string invocation, short lineNumber, List<ScriptCommand> allCommands, List<string> variables)
        {
            Match match = Regex.Match(invocation, @"(?<command>[A-Z\d_]+)(?:<(?<character>[A-Z\d_]+)>)?\((?<parameters>.+)?\)");
            if (!match.Success)
            {
                throw new ArgumentException($"Invalid invocation '{invocation}' provided!");
            }

            string command = match.Groups["command"].Value;
            string character = match.Groups["character"].Value;
            string parameters = match.Groups["parameters"].Value;

            Command = allCommands.First(c => c.Name == command);
            CommandCode = (short)allCommands.IndexOf(Command);
            LineNumber = lineNumber;
            if (character.Length > 0)
            {
                CharacterEntity = GetCharacterEntity(character);
            }
            else
            {
                CharacterEntity = -1;
            }

            Parameters = new();
            for (int i = 0; i < parameters.Length;)
            {
                string trimmedParameters = parameters[i..].Trim();
                i += parameters[i..].Length - trimmedParameters.Length;

                // Try to see if it's a line number
                Match lineNumberMatch = Regex.Match(trimmedParameters, @"^\d+");
                if (lineNumberMatch.Success)
                {
                    string parameter = trimmedParameters.Split(',')[0];

                    if (int.TryParse(parameter, out int varLineNumber))
                    {
                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.ADDRESS, Value = BitConverter.GetBytes(varLineNumber).Reverse().ToArray() });
                        i += parameter.Length + 2;
                        continue;
                    }
                }

                // See if it's a string
                if (trimmedParameters.StartsWith("\""))
                {
                    int firstQuote = trimmedParameters.IndexOf('"');
                    int secondQuote = trimmedParameters[(firstQuote + 1)..].IndexOf('"');
                    string line = trimmedParameters[(firstQuote + 1)..(secondQuote + 1)].Replace("\\n", "\n");

                    variables.Add(line);
                    Parameters.Add(new() { Type = ScriptCommand.ParameterType.DIALOGUE, Value = BitConverter.GetBytes((short)(variables.Count - 1)).Reverse().ToArray() });

                    i += line.Replace("\n", "\\n").Length + 4;
                    continue;
                }

                // See if it's a conditional (currently we only support one condition!!)
                // TODO: Add support for more than one condition
                if (trimmedParameters.StartsWith("if ", StringComparison.OrdinalIgnoreCase))
                {
                    List<byte> bytes = new();
                    bytes.AddRange(BitConverter.GetBytes((short)22).Reverse()); // Add length
                    bytes.AddRange(BitConverter.GetBytes((short)1).Reverse()); // Add number of conditions
                    string parameter = trimmedParameters.Split(',')[0];
                    string[] components = parameter.Split(' ');

                    byte[] firstOperand = CalculateControlStructure(components[1], components[2], variables);
                    if (components.Length > 3)
                    {
                        bytes.Add(ScriptCommand.ComparisonOperatorToCodeMap[components[3]]); // add comparator byte
                        bytes.Add(0x82); // always this for one condition
                        bytes.AddRange(firstOperand);
                        bytes.AddRange(CalculateControlStructure(components[4], components[5], variables));
                    }
                    else
                    {
                        bytes.AddRange(new byte[] { 0x89, 0x82 });
                        bytes.AddRange(firstOperand);
                        bytes.AddRange(CalculateControlStructure("lit", "1", variables));
                    }
                    Parameters.Add(new() { Type = ScriptCommand.ParameterType.CONDITIONAL, Value = bytes.ToArray() });
                    i += parameter.Length + 5;
                    continue;
                }

                // See if it's a time
                if (trimmedParameters.StartsWith("time ", StringComparison.OrdinalIgnoreCase) || trimmedParameters.StartsWith("frames ", StringComparison.OrdinalIgnoreCase))
                {
                    List<byte> bytes = new();
                    string parameter = trimmedParameters.Split(',')[0];
                    string[] components = parameter.Split(' ');

                    switch (components[0])
                    {
                        case "time":
                            bytes.AddRange(BitConverter.GetBytes(0x200).Reverse());
                            break;
                        case "frames":
                            bytes.AddRange(BitConverter.GetBytes(0x201).Reverse());
                            break;
                    }

                    bytes.AddRange(CalculateControlStructure(components[1], components[2], variables));
                    Parameters.Add(new() { Type = ScriptCommand.ParameterType.TIMESPAN, Value = bytes.ToArray() });
                    i += parameter.Length + 2;
                    continue;
                }

                // Is it a boolean?
                if (trimmedParameters.StartsWith("TRUE", StringComparison.OrdinalIgnoreCase) || trimmedParameters.StartsWith("FALSE", StringComparison.OrdinalIgnoreCase))
                {
                    string parameter = trimmedParameters.Split(',')[0].ToLower();

                    bool boolean = bool.Parse(parameter);

                    Parameters.Add(new() { Type = ScriptCommand.ParameterType.BOOL, Value = boolean ? BitConverter.GetBytes(1).Reverse().ToArray() : BitConverter.GetBytes(0).Reverse().ToArray() });
                    i += parameter.Length + 2;
                    continue;
                }

                // Is it a character?
                if (trimmedParameters.StartsWith("CHARACTER[", StringComparison.OrdinalIgnoreCase))
                {
                    string characterName = trimmedParameters.Split(',')[0][10..^1].ToUpper();
                    int characterEntity = GetCharacterEntity(characterName);

                    Parameters.Add(new() { Type = ScriptCommand.ParameterType.CHARACTER, Value = CalculateControlStructure("lit", $"{characterEntity}", variables) });
                    i += characterName.Length + 12;
                    continue;
                }

                // Is it a Vector3?
                if (trimmedParameters.StartsWith("Vector3(", StringComparison.OrdinalIgnoreCase))
                {
                    List<byte> bytes = new();
                    string[] vectorComponents = trimmedParameters[8..].Split(')')[0].Split(',');
                    foreach (string vectorComponent in vectorComponents)
                    {
                        string trimmedVectorComponent = vectorComponent.Trim();
                        string[] parts = trimmedVectorComponent.Split(' ');
                        bytes.AddRange(CalculateControlStructure(parts[0], parts[1], variables));
                    }

                    Parameters.Add(new() { Type = ScriptCommand.ParameterType.VECTOR3, Value = bytes.ToArray() });
                    i += vectorComponents.Sum(c => c.Length) + 11;
                    continue;
                }

                // Is it a var index?
                if (trimmedParameters.StartsWith('$'))
                {
                    string variable = trimmedParameters.Split(',')[0][1..].ToUpper();

                    if (!variables.Contains(variable))
                    {
                        variables.Add(variable);
                    }
                    Parameters.Add(new() { Type = ScriptCommand.ParameterType.VARINDEX, Value = BitConverter.GetBytes((short)variables.IndexOf(variable)).Reverse().ToArray() });
                    i += variable.Length + 2;
                    continue;
                }

                // Is it an int array?
                if (trimmedParameters.StartsWith('['))
                {
                    List<byte> bytes = new();
                    string[] arrayStrings = trimmedParameters[1..].Split(']')[0].Split(',');
                    bytes.AddRange(BitConverter.GetBytes(arrayStrings.Length).Reverse());
                    foreach (string arrayString in arrayStrings)
                    {
                        string trimmedArrayString = arrayString.Trim();
                        string[] parts = trimmedArrayString.Split(' ');
                        bytes.AddRange(CalculateControlStructure(parts[0], parts[1], variables));
                    }

                    i += arrayStrings.Sum(a => a.Length) + 4;
                    Parameters.Add(new() { Type = ScriptCommand.ParameterType.INTARRAY, Value = bytes.ToArray() });
                }

                // Is it unknown?
                if (trimmedParameters.StartsWith("UNKNOWN", StringComparison.OrdinalIgnoreCase))
                {
                    List<byte> bytes = new();
                    string parameter = trimmedParameters.Split(',')[0];
                    int typeCode = int.Parse(parameter[7..9], NumberStyles.HexNumber);
                    string[] components = parameter.Split(' ');

                    for (int j = 1; j < components.Length; j++)
                    {
                        bytes.Add(byte.Parse(components[j], NumberStyles.HexNumber));
                    }

                    Parameters.Add(new() { Type = (ScriptCommand.ParameterType)typeCode, Value = bytes.ToArray() });
                    i += parameter.Length + 2;
                    continue;
                }

                // It has to be some sort of int; try to see if it's an unknown kind
                if (int.TryParse(trimmedParameters[0..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int type))
                {
                    string[] parameter = trimmedParameters[2..].Split(',')[0].Split(' ');

                    Parameters.Add(new() { Type = (ScriptCommand.ParameterType)type, Value = CalculateControlStructure(parameter[0], parameter[1], variables) });
                    i += parameter.Sum(p => p.Length) + 4;
                    continue;
                }

                // It's a regular int
                string[] intParams = trimmedParameters.Split(',')[0].Split(' ');
                Parameters.Add(new() { Type = ScriptCommand.ParameterType.INT, Value = CalculateControlStructure(intParams[0], intParams[1], variables) });
                i += intParams.Sum(p => p.Length) + 2;
            }
        }

        public bool ResolveAddresses()
        {
            bool resolvedAddress = false;
            for (int i = 0; i < Parameters.Count; i++)
            {
                if (Parameters[i].Type == ScriptCommand.ParameterType.ADDRESS)
                {
                    int lineNumber = BitConverter.ToInt32(Parameters[i].Value.Reverse().ToArray());
                    int address = AllOtherInvocations.First(i => i.LineNumber == lineNumber).Address;
                    Parameters[i] = new Parameter { Type = Parameters[i].Type, Value = BitConverter.GetBytes(address).Reverse().ToArray() };
                    resolvedAddress = true;
                }
            }
            return resolvedAddress;
        }

        public string DoADifferentThing()
        {
            string invocation = Command.Name;
            if (CharacterEntity >= 0)
            {
                invocation += $"<{GetCharacter(CharacterEntity)}>";
            }
            invocation += "(";
            switch (Command.Name)
            {
                case "FREE":
                    bool freeArgumentUsed = false;
                    int free19IntPos = GetParameterPosition(ScriptCommand.ParameterType.INT19, 0);
                    if (free19IntPos >= 0)
                    {
                        invocation += $"19{CalculateIntParameter(Helpers.GetIntFromByteArray(Parameters[free19IntPos].Value, 0), Helpers.GetIntFromByteArray(Parameters[free19IntPos].Value, 1))}";
                        freeArgumentUsed = true;
                    }
                    int free06IntPos = GetParameterPosition(ScriptCommand.ParameterType.INT06, 0);
                    if (free06IntPos >= 0)
                    {
                        if (freeArgumentUsed)
                        {
                            invocation += ", ";
                        }
                        invocation += $"06{CalculateIntParameter(Helpers.GetIntFromByteArray(Parameters[free06IntPos].Value, 0), Helpers.GetIntFromByteArray(Parameters[free06IntPos].Value, 1))}";
                    }
                    break;
                case "EV_MODE":
                    invocation += ParseBoolean(Parameters[GetParameterPosition(ScriptCommand.ParameterType.BOOL, 0)].Value);
                    for (int i = 0; ; i++)
                    {
                        int pos = GetParameterPosition(ScriptCommand.ParameterType.INT19, i);
                        if (pos < 0)
                        {
                            break;
                        }

                        byte[] data = Parameters[pos].Value;
                        invocation += $", 19{CalculateIntParameter(Helpers.GetIntFromByteArray(data, 0), Helpers.GetIntFromByteArray(data, 1))}";
                    }
                    break;
                case "SCENE_SET":
                    var sceneSetFirstInt = Parameters[GetParameterPosition(ScriptCommand.ParameterType.INT, 0)];
                    var sceneSetSecondInt = Parameters[GetParameterPosition(ScriptCommand.ParameterType.INT, 1)];
                    string firstFlag = CalculateIntParameter(Helpers.GetIntFromByteArray(sceneSetFirstInt.Value, 0), Helpers.GetIntFromByteArray(sceneSetFirstInt.Value, 1));
                    string secondFlag = CalculateIntParameter(Helpers.GetIntFromByteArray(sceneSetSecondInt.Value, 0), Helpers.GetIntFromByteArray(sceneSetSecondInt.Value, 1));
                    invocation += $"{firstFlag}, {secondFlag}";
                    break;
                case "WAIT":
                    int waitFirstPosition = GetParameterPosition(ScriptCommand.ParameterType.TIMESPAN, 0);
                    if (waitFirstPosition >= 0)
                    {
                        invocation += ParseTime(Parameters[waitFirstPosition].Value);
                    }
                    break;
                case "JUMP":
                    invocation += $"{AllOtherInvocations.First(i => i.Address == Helpers.GetIntFromByteArray(Parameters[GetParameterPosition(ScriptCommand.ParameterType.ADDRESS, 0)].Value, 0)).LineNumber}";
                    int jumpConditionPos = GetParameterPosition(ScriptCommand.ParameterType.CONDITIONAL, 0);
                    if (jumpConditionPos >= 0)
                    {
                        invocation += ParseConditional(Parameters[jumpConditionPos].Value);
                    }
                    break;
                case "SET":
                    var setFirstInt = Parameters[GetParameterPosition(ScriptCommand.ParameterType.INT, 0)];
                    var setSecondInt = Parameters[GetParameterPosition(ScriptCommand.ParameterType.INT, 1)];
                    string value = CalculateIntParameter(Helpers.GetIntFromByteArray(setSecondInt.Value, 0), Helpers.GetIntFromByteArray(setSecondInt.Value, 1));
                    int setCommandCode = Helpers.GetIntFromByteArray(setFirstInt.Value, 0);
                    int setVariableIndex = Helpers.GetIntFromByteArray(setFirstInt.Value, 1);

                    if (setCommandCode == 0x302 && setVariableIndex >= 0x1303 && setVariableIndex <= 0x1403)
                    {
                        invocation += $"memd {setVariableIndex}, {value}";
                    }
                    else if (setCommandCode == 0x304)
                    {
                        invocation += $"var {ScriptVariables[setVariableIndex]}, {value}";
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
                    invocation += ParseBoolean(Parameters[GetParameterPosition(ScriptCommand.ParameterType.BOOL, 0)].Value);
                    break;
                case "FI":
                    invocation += ParseTime(Parameters[GetParameterPosition(ScriptCommand.ParameterType.TIMESPAN, 0)].Value);
                    for (int i = 0; ; i++)
                    {
                        int pos = GetParameterPosition(ScriptCommand.ParameterType.INT19, i);
                        if (pos < 0)
                        {
                            break;
                        }

                        byte[] data = Parameters[pos].Value;
                        invocation += $", 19{CalculateIntParameter(Helpers.GetIntFromByteArray(data, 0), Helpers.GetIntFromByteArray(data, 1))}";
                    }
                    break;
                case "TOPIC_GET":
                    invocation += ScriptVariables[BitConverter.ToInt16(Parameters[GetParameterPosition(ScriptCommand.ParameterType.VARINDEX, 0)].Value.Reverse().ToArray())];
                    var topicGetBoolLoc = GetParameterPosition(ScriptCommand.ParameterType.BOOL, 0);
                    if (topicGetBoolLoc >= 0)
                    {
                        invocation += $", {ParseBoolean(Parameters[topicGetBoolLoc].Value)}";
                    }
                    var topicGet19IntLoc = GetParameterPosition(ScriptCommand.ParameterType.INT19, 0);
                    if (topicGet19IntLoc >= 0)
                    {
                        invocation += $", 19{CalculateIntParameter(Helpers.GetIntFromByteArray(Parameters[topicGet19IntLoc].Value, 0), Helpers.GetIntFromByteArray(Parameters[topicGet19IntLoc].Value, 1))}";
                    }
                    var topicGetIntLoc = GetParameterPosition(ScriptCommand.ParameterType.INT, 0);
                    if (topicGetIntLoc >= 0)
                    {
                        invocation += $", 19{CalculateIntParameter(Helpers.GetIntFromByteArray(Parameters[topicGetIntLoc].Value, 0), Helpers.GetIntFromByteArray(Parameters[topicGetIntLoc].Value, 1))}";
                    }
                    break;
                case "CAM_SET":
                    int camSetIntLoc = GetParameterPosition(ScriptCommand.ParameterType.INT, 0);
                    invocation += $"{CalculateIntParameter(Helpers.GetIntFromByteArray(Parameters[camSetIntLoc].Value, 0), Helpers.GetIntFromByteArray(Parameters[camSetIntLoc].Value, 1))}";
                    int camSetCharacterLoc = GetParameterPosition(ScriptCommand.ParameterType.CHARACTER, 0);
                    invocation += $", Character[{GetCharacter(CalculateIntParameter(Helpers.GetIntFromByteArray(Parameters[camSetCharacterLoc].Value, 0), Helpers.GetIntFromByteArray(Parameters[camSetCharacterLoc].Value, 1)))}]";
                    break;
                case "EV_START":
                    var evStartFirst = Parameters[GetParameterPosition(ScriptCommand.ParameterType.INT, 0)].Value;
                    string eventIndex = CalculateIntParameter(Helpers.GetIntFromByteArray(evStartFirst, 0), Helpers.GetIntFromByteArray(evStartFirst, 1));
                    invocation += eventIndex;
                    var evStartSecond = Parameters.ElementAtOrDefault(GetParameterPosition(ScriptCommand.ParameterType.INTARRAY, 0)).Value;
                    if (evStartSecond is not null)
                    {
                        List<string> values = new();
                        int numValues = Helpers.GetIntFromByteArray(evStartSecond, 0);
                        for (int i = 1; i <= numValues; i++)
                        {
                            values.Add(CalculateIntParameter(Helpers.GetIntFromByteArray(evStartSecond, i * 2 - 1), Helpers.GetIntFromByteArray(evStartSecond, i * 2)));
                        }
                        invocation += $", [{string.Join(", ", values)}]";
                    }
                    for (int i = 1; ; i++)
                    {
                        int pos = GetParameterPosition(ScriptCommand.ParameterType.INT, i);
                        if (pos < 0)
                        {
                            break;
                        }

                        byte[] data = Parameters[pos].Value;
                        invocation += $", {CalculateIntParameter(Helpers.GetIntFromByteArray(data, 0), Helpers.GetIntFromByteArray(data, 1))}";
                    }
                    break;
                case "APPEAR":
                    int appearBoolPos = GetParameterPosition(ScriptCommand.ParameterType.BOOL, 0);
                    if (appearBoolPos >= 0)
                    {
                        invocation += ParseBoolean(Parameters[appearBoolPos].Value);
                    }
                    break;
                case "GAZE":
                    bool parameterAdded = false;
                    int gazeBoolLocation = GetParameterPosition(ScriptCommand.ParameterType.BOOL, 0);
                    if (gazeBoolLocation >= 0)
                    {
                        invocation += ParseBoolean(Parameters[gazeBoolLocation].Value);
                        parameterAdded = true;
                    }
                    int gazeCharacterLocation = GetParameterPosition(ScriptCommand.ParameterType.CHARACTER, 0);
                    if (gazeCharacterLocation >= 0)
                    {
                        if (parameterAdded)
                        {
                            invocation += ", ";
                        }
                        invocation += GetCharacter(CalculateIntParameter(Helpers.GetIntFromByteArray(Parameters[gazeCharacterLocation].Value, 0), Helpers.GetIntFromByteArray(Parameters[gazeCharacterLocation].Value, 1)));
                        parameterAdded = true;
                    }
                    int gazeVectorLocation = GetParameterPosition(ScriptCommand.ParameterType.VECTOR3, 0);
                    if (gazeVectorLocation >= 0)
                    {
                        if (parameterAdded)
                        {
                            invocation += ", ";
                        }
                        invocation += ParseVector(Parameters[gazeVectorLocation].Value);
                        parameterAdded = true;
                    }
                    int gazeInt10Location = GetParameterPosition(ScriptCommand.ParameterType.INT10, 0);
                    if (gazeInt10Location >= 0)
                    {
                        if (parameterAdded)
                        {
                            invocation += ", ";
                        }
                        invocation += ParseInt10(Parameters[gazeInt10Location].Value);
                        parameterAdded = true;
                    }
                    for (int i = 0; ; i++)
                    {
                        int pos = GetParameterPosition(ScriptCommand.ParameterType.INT, i);
                        if (pos < 0)
                        {
                            break;
                        }

                        if (parameterAdded)
                        {
                            invocation += ", ";
                        }

                        byte[] data = Parameters[pos].Value;
                        invocation += $"{CalculateIntParameter(Helpers.GetIntFromByteArray(data, 0), Helpers.GetIntFromByteArray(data, 1))}";
                        parameterAdded = true;
                    }
                    int gazeInt0ELocation = GetParameterPosition(ScriptCommand.ParameterType.INT0E, 0);
                    if (gazeInt0ELocation >= 0)
                    {
                        if (parameterAdded)
                        {
                            invocation += ", ";
                        }

                        invocation += ParseInt0E(Parameters[gazeInt0ELocation].Value);
                    }
                    break;
                case "TURN":
                    bool turnParamExists = false;
                    int turnInt08 = GetParameterPosition(ScriptCommand.ParameterType.INT08, 0);
                    if (turnInt08 >= 0)
                    {
                        turnParamExists = true;
                        invocation += $"08{CalculateIntParameter(Helpers.GetIntFromByteArray(Parameters[turnInt08].Value, 0), Helpers.GetIntFromByteArray(Parameters[turnInt08].Value, 1))}";
                    }
                    int turnCharacter = GetParameterPosition(ScriptCommand.ParameterType.CHARACTER, 0);
                    if (turnCharacter >= 0)
                    {
                        if (turnParamExists)
                        {
                            invocation += ", ";
                        }
                        turnParamExists = true;
                        invocation += $"Character[{GetCharacter(CalculateIntParameter(Helpers.GetIntFromByteArray(Parameters[turnCharacter].Value, 0), Helpers.GetIntFromByteArray(Parameters[turnCharacter].Value, 1)))}]";
                    }
                    int turnVector = GetParameterPosition(ScriptCommand.ParameterType.VECTOR3, 0);
                    if (turnVector >= 0)
                    {
                        if (turnParamExists)
                        {
                            invocation += ", ";
                        }
                        turnParamExists = true;
                        invocation += ParseVector(Parameters[turnVector].Value);
                    }
                    int turnInt10 = GetParameterPosition(ScriptCommand.ParameterType.INT10, 0);
                    if (turnInt10 >= 0)
                    {
                        if (turnParamExists)
                        {
                            invocation += ", ";
                        }
                        turnParamExists = true;
                        invocation += ParseInt10(Parameters[turnInt10].Value);
                    }
                    int turnInt = GetParameterPosition(ScriptCommand.ParameterType.INT, 0);
                    if (turnInt >= 0)
                    {
                        if (turnParamExists)
                        {
                            invocation += ", ";
                        }
                        invocation += $"{CalculateIntParameter(Helpers.GetIntFromByteArray(Parameters[turnInt].Value, 0), Helpers.GetIntFromByteArray(Parameters[turnInt].Value, 1))}";
                    }
                    break;
                default:
                    return "";
            }
            return $"{invocation})";
        }

        public string ParseParameter(Parameter parameter)
        {
            switch (parameter.Type)
            {
                case ScriptCommand.ParameterType.ADDRESS:
                    return $"{AllOtherInvocations.First(i => i.Address == Helpers.GetIntFromByteArray(Parameters[GetParameterPosition(ScriptCommand.ParameterType.ADDRESS, 0)].Value, 0)).LineNumber}";
                case ScriptCommand.ParameterType.DIALOGUE:
                    return $"\"{ScriptVariables[BitConverter.ToInt16(parameter.Value.Reverse().ToArray())].Replace("\n", "\\n")}\"";
                case ScriptCommand.ParameterType.CONDITIONAL:
                    return ParseConditional(parameter.Value);
                case ScriptCommand.ParameterType.TIMESPAN:
                    return ParseTime(parameter.Value);
                case ScriptCommand.ParameterType.INT:
                    return CalculateIntParameter(Helpers.GetIntFromByteArray(parameter.Value, 0), Helpers.GetIntFromByteArray(parameter.Value, 1));
                case ScriptCommand.ParameterType.INT06:
                    return $"06{CalculateIntParameter(Helpers.GetIntFromByteArray(parameter.Value, 0), Helpers.GetIntFromByteArray(parameter.Value, 1))}";
                case ScriptCommand.ParameterType.INT08:
                    return $"08{CalculateIntParameter(Helpers.GetIntFromByteArray(parameter.Value, 0), Helpers.GetIntFromByteArray(parameter.Value, 1))}";
                case ScriptCommand.ParameterType.BOOL:
                    return ParseBoolean(parameter.Value);
                case ScriptCommand.ParameterType.CHARACTER:
                    return GetCharacter(CalculateIntParameter(Helpers.GetIntFromByteArray(parameter.Value, 0), Helpers.GetIntFromByteArray(parameter.Value, 1)));
                case ScriptCommand.ParameterType.INT0E:
                    return $"0E{CalculateIntParameter(Helpers.GetIntFromByteArray(parameter.Value, 0), Helpers.GetIntFromByteArray(parameter.Value, 1))}";
                case ScriptCommand.ParameterType.INT10:
                    return $"10{CalculateIntParameter(Helpers.GetIntFromByteArray(parameter.Value, 0), Helpers.GetIntFromByteArray(parameter.Value, 1))}";
                case ScriptCommand.ParameterType.VECTOR3:
                    return ParseVector(parameter.Value);
                case ScriptCommand.ParameterType.VARINDEX:
                    return $"${ScriptVariables[BitConverter.ToInt16(parameter.Value.Reverse().ToArray())]}";
                case ScriptCommand.ParameterType.INTARRAY:
                    List<string> values = new();
                    int numValues = Helpers.GetIntFromByteArray(parameter.Value, 0);
                    for (int i = 1; i <= numValues; i++)
                    {
                        values.Add(CalculateIntParameter(Helpers.GetIntFromByteArray(parameter.Value, i * 2 - 1), Helpers.GetIntFromByteArray(parameter.Value, i * 2)));
                    }
                    return $"[{string.Join(", ", values)}]";
                case ScriptCommand.ParameterType.INT19:
                    return $"19{CalculateIntParameter(Helpers.GetIntFromByteArray(parameter.Value, 0), Helpers.GetIntFromByteArray(parameter.Value, 1))}";
                default:
                    return $"{parameter.Type} {string.Join(" ", parameter.Value.Select(b => $"{b:X2}"))}";
            }
        }

        public string CalculateIntParameter(int controlCode, int valueCode)
        {
            switch (controlCode)
            {
                case 0x300:
                    return $"lit {valueCode}";
                case 0x302:
                    if (valueCode - 0x124F < 0)
                    {
                        throw new ArgumentException("Illegal system flag index");
                    }
                    return $"memd {valueCode}";
                case 0x303:
                    return $"memm {valueCode}";
                case 0x304:
                    return $"var {ScriptVariables[valueCode]}";
                default:
                    return "-1";
            }
        }

        public byte[] CalculateControlStructure(string controlCode, string value, List<string> variables)
        {
            List<byte> bytes = new();

            switch (controlCode)
            {
                case "lit":
                    bytes.AddRange(BitConverter.GetBytes(0x300).Reverse());
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(value)).Reverse());
                    break;
                case "memd":
                    bytes.AddRange(BitConverter.GetBytes(0x302).Reverse());
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(value)).Reverse());
                    break;
                case "memm":
                    bytes.AddRange(BitConverter.GetBytes(0x303).Reverse());
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(value)).Reverse());
                    break;
                case "var":
                    bytes.AddRange(BitConverter.GetBytes(0x304).Reverse());
                    if (!variables.Contains(value))
                    {
                        variables.Add(value);
                    }
                    bytes.AddRange(BitConverter.GetBytes(variables.IndexOf(value)).Reverse());
                    break;
            }

            return bytes.ToArray();
        }

        public static string GetCharacter(string characterInt)
        {
            if (characterInt.StartsWith("lit "))
            {
                string character = GetCharacter(int.Parse(characterInt[4..]));
                return $"CHARACTER[{character}]";
            }
            else
            {
                return $"CHARACTER[{characterInt}]";
            }
        }

        public static string ParseBoolean(byte[] type0AParameterData)
        {
            return Helpers.GetIntFromByteArray(type0AParameterData, 0) != 0 ? "TRUE" : "FALSE";
        }

        public static string GetCharacter(int characterCode)
        {
            if (ScriptCommand.CharacterCodeToCharacterMap.TryGetValue((short)characterCode, out string character))
            {
                return character;
            }
            else
            {
                return $"UNKNOWN{characterCode}";
            }
        }

        public static short GetCharacterEntity(string character)
        {
            if (character.StartsWith("UNKNOWN"))
            {
                return short.Parse(character[7..]);
            }
            else
            {
                return ScriptCommand.CharacterCodeToCharacterMap.First(c => c.Value == character).Key;
            }
        }

        public string ParseConditional(byte[] conditionalData)
        {
            short numConditions = BitConverter.ToInt16(conditionalData.Skip(2).Take(2).Reverse().ToArray());

            string conditional = "if ";
            byte lastCombiningByte = 0;

            for (int i = 0; i < numConditions; i++)
            {
                byte comparatorByte = conditionalData[4 + i * 18];
                byte combiningByte = conditionalData[5 + i * 18];
                string value1 = CalculateIntParameter(Helpers.GetIntFromByteArray(conditionalData.Skip(6 + i * 18), 0), Helpers.GetIntFromByteArray(conditionalData.Skip(6 + i * 18), 1));
                string value2 = CalculateIntParameter(Helpers.GetIntFromByteArray(conditionalData.Skip(14 + i * 18), 0), Helpers.GetIntFromByteArray(conditionalData.Skip(14 + i * 18), 1));

                if (i > 0)
                {
                    switch (lastCombiningByte)
                    {
                        case 0x80:
                            conditional += $" && ";
                            break;
                        case 0x81:
                            conditional += $" || ";
                            break;
                        case 0x82:
                            break;
                    }
                }

                switch (comparatorByte)
                {
                    case 0x83:
                        conditional += $"{value1} == {value2}";
                        break;
                    case 0x84:
                        conditional += $"{value1} != {value2}";
                        break;
                    case 0x85:
                        conditional += $"{value1} > {value2}";
                        break;
                    case 0x86:
                        conditional += $"{value1} < {value2}";
                        break;
                    case 0x87:
                        conditional += $"{value1} >= {value2}";
                        break;
                    case 0x88:
                        conditional += $"{value1} <= {value2}";
                        break;
                    case 0x89:
                        conditional += $"{value1}";
                        break;
                    default:
                        break;
                }

                lastCombiningByte = combiningByte;
            }

            return conditional;
        }

        private string ParseTime(byte[] type03Parameter)
        {
            string param = Helpers.GetIntFromByteArray(type03Parameter, 0) == 0x201 ? "frames " : "time ";
            param += CalculateIntParameter(Helpers.GetIntFromByteArray(type03Parameter, 1), Helpers.GetIntFromByteArray(type03Parameter, 2));
            return param;
        }

        private string ParseInt0E(byte[] type0EParameter)
        {
            return $"0E{CalculateIntParameter(Helpers.GetIntFromByteArray(type0EParameter, 0), Helpers.GetIntFromByteArray(type0EParameter, 1))}";
        }

        private string ParseInt10(byte[] type10Parameter)
        {
            return $"10{CalculateIntParameter(Helpers.GetIntFromByteArray(type10Parameter, 0), Helpers.GetIntFromByteArray(type10Parameter, 1))}";
        }

        private string ParseVector(byte[] type14Parameter)
        {
            string[] coords = new string[]
            {
                CalculateIntParameter(Helpers.GetIntFromByteArray(type14Parameter, 0), Helpers.GetIntFromByteArray(type14Parameter, 1)),
                CalculateIntParameter(Helpers.GetIntFromByteArray(type14Parameter, 2), Helpers.GetIntFromByteArray(type14Parameter, 3)),
                CalculateIntParameter(Helpers.GetIntFromByteArray(type14Parameter, 4), Helpers.GetIntFromByteArray(type14Parameter, 5)),
            };

            return $"Vector3({coords[0]}, {coords[1]}, {coords[2]})";
        }

        private int GetParameterPosition(ScriptCommand.ParameterType parameterType, int paramNum)
        {
            int currentParam = 0;
            for (int i = 0; i < Parameters.Count; i++)
            {
                if (Parameters[i].Type == parameterType)
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
