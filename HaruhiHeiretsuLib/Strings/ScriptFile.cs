using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public List<string> Objects { get; set; } = new();

        public int NumObjectssOffset { get; set; }
        public short NumObjects { get; set; }
        public int NumScriptCommandBlocksOffset { get; set; }
        public short NumScriptCommandBlocks { get; set; }
        public int ObjectssEndOffset { get; set; }
        public int ObjectsEnd { get; set; }
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

        private static string ReadString(IEnumerable<byte> data, int currentPosition, out int newPosition)
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

            NumObjects = BitConverter.ToInt16(Data.Skip(pos).Take(2).Reverse().ToArray());
            NumObjectssOffset = pos;
            pos += 2;

            NumScriptCommandBlocks = BitConverter.ToInt16(Data.Skip(pos).Take(2).Reverse().ToArray());
            NumScriptCommandBlocksOffset = pos;
            pos += 2;

            ObjectsEnd = BitConverter.ToInt32(Data.Skip(pos).Take(4).Reverse().ToArray());
            ObjectssEndOffset = pos;
            pos += 4;

            ScriptCommandBlockDefinitionsEnd = BitConverter.ToInt32(Data.Skip(pos).Take(4).Reverse().ToArray());
            ScriptCommandBlockDefinitionsEndOffset = pos;
            pos += 4;

            for (int i = 0; i < NumObjects; i++)
            {
                Objects.Add(ReadString(Data, pos, out pos));
            }

            for (int i = ObjectsEnd; i < ScriptCommandBlockDefinitionsEnd; i += 0x08)
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
                ScriptCommandBlocks.Add(new(i, endAddress, Data, Objects));
            }

            DialogueLines = ScriptCommandBlocks
                .SelectMany(b => b.Invocations.Where(i => i.CommandCode != 0x4B) // TL_ADD (4B) does not have real dialogue, just references
                .SelectMany(i => i.Parameters.Where(p => p.Type == ScriptCommand.ParameterType.DIALOGUE)
                .Select(p => new DialogueLine()
                {
                    Line = Objects[BitConverter.ToInt16(p.Value.Reverse().ToArray())],
                    Speaker = i.CommandCode >= 0x2E && i.CommandCode <= 0x31 ? Speaker.CHOICE : (Speaker)i.CharacterEntity
                }))).ToList();
        }

        private Parameter[] GetDialogueParameters()
        {
            return ScriptCommandBlocks.SelectMany(b => b.Invocations.Where(i => i.CommandCode != 0x4B)
            .SelectMany(i => i.Parameters.Where(p => p.Type == ScriptCommand.ParameterType.DIALOGUE))).ToArray();
        }

        public override void EditDialogue(int index, string newLine)
        {
            DialogueLines[index].Line = newLine;
            Objects.Add(newLine);

            Parameter[] dialogueParams = GetDialogueParameters();
            dialogueParams[index].Value = BitConverter.GetBytes((short)(Objects.Count - 1)).Reverse().ToArray();

            Recompile();
        }

        public void Recompile()
        {
            Compile(Decompile());
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
            List<(string, int)> labels = new();
            string[] lines = code.Split('\n');
            string[] info = lines[0].Split(' ');
            InternalName = info[0];
            Room = info[1];
            Time = info[2];
            bytes.AddRange(Helpers.GetStringBytes(InternalName));
            bytes.AddRange(Helpers.GetStringBytes(Room));
            bytes.AddRange(Helpers.GetStringBytes(Time));

            ScriptCommandBlocks = new();
            Objects = new();

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
                    lineNumber = commandBlock.ParseBlock(lineNumber, lines[(lineNumber - 1)..], AvailableCommands, Objects, labels);
                    ScriptCommandBlocks.Add(commandBlock);
                }
            }

            NumObjects = (short)Objects.Count;
            NumScriptCommandBlocks = (short)ScriptCommandBlocks.Count;

            NumObjectssOffset = bytes.Count;
            bytes.AddRange(BitConverter.GetBytes(NumObjects).Reverse());
            NumScriptCommandBlocksOffset = bytes.Count;
            bytes.AddRange(BitConverter.GetBytes(NumScriptCommandBlocks).Reverse());
            ObjectssEndOffset = bytes.Count;
            bytes.AddRange(BitConverter.GetBytes(0));
            ScriptCommandBlockDefinitionsEndOffset = bytes.Count;
            bytes.AddRange(BitConverter.GetBytes(0));

            foreach (string @object in Objects)
            {
                bytes.AddRange(Helpers.GetStringBytes(@object));
            }

            ObjectsEnd = bytes.Count;
            bytes.RemoveRange(ObjectssEndOffset, 4);
            bytes.InsertRange(ObjectssEndOffset, BitConverter.GetBytes(ObjectsEnd).Reverse());

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
                    invocation.ScriptObjects = Objects;
                    invocation.AllOtherInvocations = ScriptCommandBlocks.SelectMany(b => b.Invocations).ToList();
                    if (invocation.ResolveAddresses(labels))
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
            int numLabels = 0;
            for (int i = 0; i < ScriptCommandBlocks.Count; i++)
            {
                ScriptCommandBlocks[i].PopulateCommands(AvailableCommands);
                for (int j = 0; j < ScriptCommandBlocks[i].Invocations.Count; j++)
                {
                    ScriptCommandBlocks[i].Invocations[j].AllOtherInvocations = allInvocations;
                    List<Parameter> addressParams = new();
                    addressParams.AddRange(ScriptCommandBlocks[i].Invocations[j].Parameters.Where(p => p.Type == ScriptCommand.ParameterType.ADDRESS));
                    addressParams.AddRange(ScriptCommandBlocks[i].Invocations[j].Parameters.Where(p => p.Type == ScriptCommand.ParameterType.INDEXEDADDRESS));

                    foreach (Parameter param in addressParams)
                    {
                        int address = BitConverter.ToInt32(param.Value.Take(4).Reverse().ToArray());
                        ScriptCommandInvocation referencedCommand = allInvocations.First(a => a.Address == address);
                        if (string.IsNullOrEmpty(referencedCommand.Label))
                        {
                            referencedCommand.Label = $"label{numLabels++:D3}";
                        }
                    }
                }
            }

            TagDialogueWithVjumpMetadata();
        }

        private void TagDialogueWithVjumpMetadata()
        {
            Parameter[] dialogueParams = GetDialogueParameters();
            ScriptCommandInvocation[] allInvocations = ScriptCommandBlocks.SelectMany(b => b.Invocations).ToArray();
            Dictionary<int, int> NumChoicesPerInvocationIndex = new();

            for (int i = 0; i < DialogueLines.Count; i++)
            {
                if (DialogueLines[i].Speaker == Speaker.CHOICE)
                {
                    int selectIndex = Array.IndexOf(allInvocations, allInvocations.First(v => v.LineNumber == dialogueParams[i].LineNumber));
                    if (NumChoicesPerInvocationIndex.ContainsKey(selectIndex))
                    {
                        NumChoicesPerInvocationIndex[selectIndex]++;
                    }
                    else
                    {

                        if (!allInvocations[selectIndex].Command.Name.Contains('2'))
                        {
                            NumChoicesPerInvocationIndex.Add(selectIndex, 0);
                        }
                        else
                        {
                            NumChoicesPerInvocationIndex.Add(selectIndex, 1);
                        }
                    }

                    if (NumChoicesPerInvocationIndex[selectIndex] == 0)
                    {
                        continue;
                    }

                    int nextVjumpIndex = -1;
                    for (int j = selectIndex + 1; j < allInvocations.Length; j++)
                    {
                        if (allInvocations[j].Command.Name.Equals("VJUMP", StringComparison.OrdinalIgnoreCase))
                        {
                            nextVjumpIndex = j;
                            break;
                        }
                    }

                    if (nextVjumpIndex < 0)
                    {
                        continue;
                    }

                    int currentAddressParameter = 0;
                    foreach (Parameter parameter in allInvocations[nextVjumpIndex].Parameters)
                    {
                        if (parameter.Type == ScriptCommand.ParameterType.INDEXEDADDRESS)
                        {
                            currentAddressParameter++;
                            if (currentAddressParameter < NumChoicesPerInvocationIndex[selectIndex])
                            {
                                continue;
                            }
                            else if (currentAddressParameter > NumChoicesPerInvocationIndex[selectIndex])
                            {
                                break;
                            }

                            int targetLineNumber = allInvocations.First(v => v.Address == BitConverter.ToInt32(parameter.Value.Take(4).Reverse().ToArray())).LineNumber;
                            for (int j = 0; j < dialogueParams.Length; j++)
                            {
                                if (dialogueParams[j].LineNumber >= targetLineNumber)
                                {
                                    DialogueLines[i].Metadata.Add($"VJUMPs to {j:D4}");
                                    break;
                                }
                            }
                        }
                    }
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
            VECTOR2 = 4,
            INT = 5,
            TRANSITION = 6,
            INDEXEDADDRESS = 7,
            ANGLE = 8,
            UNKNOWN09 = 9,
            BOOL = 10,
            VOLUME = 11,
            COLOR = 12,
            CHARACTER = 13,
            INT0E = 14,
            UNKNOWN0F = 15,
            PREVEC = 16,
            UNKNOWN11 = 17,
            FLOAT = 18,
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
            LIPSYNCDATA = 29,
            UNKNOWN29 = 41,
            UNKNOWN2A = 42,
        }

        public static readonly Dictionary<string, byte> ComparisonOperatorToCodeMap = new()
        {
            { "==", 0x83 },
            { "!=", 0x84 },
            { ">", 0x85 },
            { "<", 0x86 },
            { ">=", 0x87 },
            { "<=", 0x88 },
        };

        public static readonly Dictionary<string, int> TransitionToCodeMap = new()
        {
            { "HARD_CUT", 0 },
            { "CROSS_DISSOLVE", 1 },
            { "PUSH_RIGHT", 2 },
            { "PUSH_LEFT", 3 },
            { "PUSH_UP", 4 },
            { "PUSH_DOWN", 5 },
            { "WIPE_RIGHT", 6 },
            { "WIPE_LEFT", 7 },
            { "WIPE_UP", 8 },
            { "WIPE_DOWN", 9 },
            { "HORIZONTAL_BLINDS", 10 },
            { "VERTICAL_BLINDS", 11 },
            { "CENTER_OUT", 12 },
            { "RED_SETTINGS_BG", 13 },
            { "BLACK_SETTINGS_BG", 14 },
            { "FADE_TO_BLACK", 500 },
        };

        public static readonly Dictionary<byte, string> LipSyncMap = new()
        {
            { 0x01, "s" },
            { 0x02, "a" },
            { 0x03, "i" },
            { 0x04, "u" },
            { 0x05, "e" },
            { 0x06, "o" },
            { 0x07, "n" },
            { 0xF0, "N" },
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

        public ScriptCommandBlock(int address, int endAddress, IEnumerable<byte> data, List<string> objects)
        {
            DefinitionAddress = address;
            NameIndex = BitConverter.ToUInt16(data.Skip(address).Take(2).Reverse().ToArray());
            Name = objects[NameIndex];
            NumInvocations = BitConverter.ToUInt16(data.Skip(address + 2).Take(2).Reverse().ToArray());
            BlockOffset = BitConverter.ToInt32(data.Skip(address + 4).Take(4).Reverse().ToArray());

            for (int i = BlockOffset; i < endAddress - 8;)
            {
                ScriptCommandInvocation invocation = new(objects, i);
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
                    invocation.Parameters.Add(new Parameter() { Type = (ScriptCommand.ParameterType)paramTypeCode, Value = data.Skip(i).Take(paramLength).ToArray(), LineNumber = invocation.LineNumber });
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

        public int ParseBlock(int lineNumber, string[] lines, List<ScriptCommand> allCommands, List<string> objects, List<(string, int)> labels)
        {
            Regex nameRegex = new(@"== (?<name>.+) ==");
            Match nameMatch = nameRegex.Match(lines[0]);
            if (!nameMatch.Success)
            {
                throw new ArgumentException($"Name {lines[0]} not a valid block name!");
            }

            Name = nameMatch.Groups["name"].Value;
            if (!objects.Contains(Name))
            {
                objects.Add(Name);
                NameIndex = (ushort)(objects.Count - 1);
            }
            else
            {
                NameIndex = (ushort)objects.IndexOf(Name);
            }

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

                Invocations.Add(new ScriptCommandInvocation(lines[i], (short)(i + lineNumber), allCommands, objects, labels));
            }

            NumInvocations = (ushort)Invocations.Count;

            return i + lineNumber;
        }
    }

    public struct Parameter
    {
        public ScriptCommand.ParameterType Type { get; set; }
        public byte[] Value { get; set; }
        public int LineNumber { get; set; }
    }

    public class ScriptCommandInvocation
    {

        public int Address { get; set; }
        public short LineNumber { get; set; }
        public string Label { get; set; }
        public short CharacterEntity { get; set; }
        public short CommandCode { get; set; }
        public ScriptCommand Command { get; set; }
        public List<string> ScriptObjects { get; set; }
        public List<Parameter> Parameters { get; set; } = new();
        public int Length => 12 + Parameters.Sum(p => 2 + p.Value.Length);

        public List<ScriptCommandInvocation> AllOtherInvocations { get; set; }

        public ScriptCommandInvocation(List<string> scriptObjects, int address)
        {
            ScriptObjects = scriptObjects;
            Address = address;
        }

        public ScriptCommandInvocation(string invocation, short lineNumber, List<ScriptCommand> allCommands, List<string> objects, List<(string, int)> labels)
        {
            ParseInvocation(invocation, lineNumber, allCommands, objects, labels);
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
            string invocation = string.Empty;
            if (!string.IsNullOrEmpty(Label))
            {
                invocation += $"{Label}: ";
            }
            invocation += Command.Name;
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

        public void ParseInvocation(string invocation, short lineNumber, List<ScriptCommand> allCommands, List<string> objects, List<(string label, int lineNumber)> labels)
        {
            Regex labelRegex = new(@"^{(?<label>[\w\d]+)}");

            Match lineLabelMatch = Regex.Match(invocation, @"^(?<label>[\w\d]+): ");
            if (lineLabelMatch.Success)
            {
                Label = lineLabelMatch.Groups["label"].Value;
                invocation = invocation[lineLabelMatch.Length..];
                if (!labels.Any(l => l.label == Label))
                {
                    labels.Add((Label, lineNumber));
                }
                else
                {
                    labels[labels.IndexOf(labels.First(l => l.label == Label))] = (Label, lineNumber); // this is hell, i'm in hell
                }
            }

            Match match = Regex.Match(invocation, @"(?<command>[A-Z\d_]+)(?:<(?<character>[?A-Z\d_]+)>)?\((?<parameters>.+)?\)");
            if (!match.Success)
            {
                throw new ArgumentException($"Invalid invocation '{invocation}' provided!");
            }

            string command = match.Groups["command"].Value;
            string character = match.Groups["character"].Value;
            string parameters = match.Groups["parameters"].Value;

            Command = allCommands.FirstOrDefault(c => c.Name == command);
            if (Command is null)
            {
                throw new ArgumentException($"Command '{command}' is not a valid command!");
            }
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
                try
                {
                    string trimmedParameters = parameters[i..].Trim();
                    i += parameters[i..].Length - trimmedParameters.Length;

                    // Try to see if it's a line number
                    Match lineNumberMatch = Regex.Match(trimmedParameters, @"^\d+");
                    Match labelMatch = labelRegex.Match(trimmedParameters);
                    if (lineNumberMatch.Success)
                    {
                        string parameter = trimmedParameters.Split(',')[0];

                        if (int.TryParse(parameter, out int varLineNumber))
                        {
                            List<byte> bytes = new(new byte[] { 0 });
                            bytes.AddRange(BitConverter.GetBytes(varLineNumber).Reverse().Skip(1)); // skip the first byte to keep us at four bytes; this makes us a 24-bit integer :D
                            Parameters.Add(new() { Type = ScriptCommand.ParameterType.ADDRESS, Value = bytes.ToArray(), LineNumber = LineNumber });
                            i += parameter.Length + 2;
                            continue;
                        }
                    }
                    else if (labelMatch.Success)
                    {
                        List<byte> bytes = new(new byte[] { 1 });
                        if (!labels.Any(l => l.label == labelMatch.Groups["label"].Value))
                        {
                            labels.Add((labelMatch.Groups["label"].Value, 0));
                        }
                        bytes.AddRange(BitConverter.GetBytes(labels.IndexOf(labels.First(l => l.label == labelMatch.Groups["label"].Value))).Reverse().Skip(1)); // another 24-bit integer
                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.ADDRESS, Value = bytes.ToArray(), LineNumber = LineNumber });
                        i += labelMatch.Length + 1;
                        continue;
                    }

                    // See if it's a string
                    if (trimmedParameters.StartsWith("\""))
                    {
                        int firstQuote = trimmedParameters.IndexOf('"');
                        int secondQuote = Regex.Match(trimmedParameters[(firstQuote + 1)..], @"[^\\]""").Index + 1;
                        string line = trimmedParameters[(firstQuote + 1)..(secondQuote + 1)].Replace("\\n", "\n").Replace("\\\"", "\"");

                        objects.Add(line);
                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.DIALOGUE, Value = BitConverter.GetBytes((short)(objects.Count - 1)).Reverse().ToArray(), LineNumber = LineNumber });

                        i += line.Replace("\n", "\\n").Replace("\"", "\\\"").Length + 4;
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

                        byte[] firstOperand = CalculateControlStructure(components[1], components[2], objects);
                        if (components.Length > 3)
                        {
                            bytes.Add(ScriptCommand.ComparisonOperatorToCodeMap[components[3]]); // add comparator byte
                            bytes.Add(0x82); // always this for one condition
                            bytes.AddRange(firstOperand);
                            bytes.AddRange(CalculateControlStructure(components[4], components[5], objects));
                        }
                        else
                        {
                            bytes.AddRange(new byte[] { 0x89, 0x82 });
                            bytes.AddRange(firstOperand);
                            bytes.AddRange(CalculateControlStructure("lit", "1", objects));
                        }
                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.CONDITIONAL, Value = bytes.ToArray(), LineNumber = LineNumber });
                        i += parameter.Length + 5;
                        continue;
                    }

                    // See if it's a time
                    if (trimmedParameters.StartsWith("time_", StringComparison.OrdinalIgnoreCase))
                    {
                        List<byte> bytes = new();
                        string parameter = trimmedParameters.Split(',')[0];
                        string[] components = parameter.Split(' ');

                        switch (components[0])
                        {
                            case "time_f":
                                bytes.AddRange(BitConverter.GetBytes(0x200).Reverse());
                                break;
                            case "time_s":
                                bytes.AddRange(BitConverter.GetBytes(0x201).Reverse());
                                break;
                        }

                        bytes.AddRange(CalculateControlStructure(components[1], components[2], objects));
                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.TIMESPAN, Value = bytes.ToArray(), LineNumber = LineNumber });
                        i += parameter.Length + 2;
                        continue;
                    }

                    // Is it a Vector2?
                    if (trimmedParameters.StartsWith("Vector2(", StringComparison.OrdinalIgnoreCase))
                    {
                        List<byte> bytes = new();
                        string[] vectorComponents = trimmedParameters[8..].Split(')')[0].Split(',');
                        foreach (string vectorComponent in vectorComponents)
                        {
                            string trimmedVectorComponent = vectorComponent.Trim();
                            string[] parts = trimmedVectorComponent.Split(' ');
                            bytes.AddRange(CalculateControlStructure(parts[0], parts[1], objects));
                        }

                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.VECTOR2, Value = bytes.ToArray(), LineNumber = LineNumber });
                        i += vectorComponents.Sum(c => c.Length) + 11;
                        continue;
                    }

                    // Is it a transition?
                    if (trimmedParameters.StartsWith("Transition[", StringComparison.OrdinalIgnoreCase))
                    {
                        List<byte> bytes = new();
                        string parameter = trimmedParameters.Split(',')[0][11..^1].ToUpper();
                        if (ScriptCommand.TransitionToCodeMap.TryGetValue(parameter, out int transition))
                        {
                            bytes.AddRange(CalculateControlStructure("lit", $"{transition}", objects));
                        }
                        else
                        {
                            string[] controlSplit = parameter.Split(' ');
                            bytes.AddRange(CalculateControlStructure(controlSplit[0], controlSplit[1], objects));
                        }

                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.TRANSITION, Value = bytes.ToArray(), LineNumber = LineNumber });
                        i += parameter.Length + 13;
                        continue;
                    }

                    // Is it an indexed address?
                    if (trimmedParameters.StartsWith("("))
                    {
                        List<byte> bytes = new();
                        string[] components = trimmedParameters.Split(')')[0].Split(',');

                        Match indexedLabelMatch = labelRegex.Match(components[0][1..]);
                        if (indexedLabelMatch.Success)
                        {
                            bytes.Add(1);
                            if (!labels.Any(l => l.label == indexedLabelMatch.Groups["label"].Value))
                            {
                                labels.Add((indexedLabelMatch.Groups["label"].Value, 0));
                            }
                            bytes.AddRange(BitConverter.GetBytes(labels.IndexOf(labels.First(l => l.label == indexedLabelMatch.Groups["label"].Value))).Reverse().Skip(1));
                        }
                        else
                        {
                            bytes.Add(0);
                            bytes.AddRange(BitConverter.GetBytes(int.Parse(components[0].Trim())).Reverse().Skip(1));
                        }

                        bytes.AddRange(components[1].Trim() == "TRUE" ? new byte[] { 0x01, 0x00, 0x00, 0x00 } : new byte[4]); // weird little-endian looking thing is on purpose; matches game
                        string[] controlCodeComponents = components[2].Trim().Split(' ');
                        bytes.AddRange(CalculateControlStructure(controlCodeComponents[0], controlCodeComponents[1], objects));

                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.INDEXEDADDRESS, Value = bytes.ToArray(), LineNumber = LineNumber });
                        i += trimmedParameters.Split(')')[0].Length + 2;
                        continue;
                    }

                    // Is it an angle?
                    if (trimmedParameters.StartsWith("degrees ", StringComparison.OrdinalIgnoreCase))
                    {
                        string parameter = trimmedParameters.Split(',')[0];
                        string[] components = parameter.Split(' ');

                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.ANGLE, Value = CalculateControlStructure(components[1], components[2], objects), LineNumber = LineNumber });
                        i += parameter.Length + 2;
                        continue;
                    }

                    // Is it a boolean?
                    if (trimmedParameters.StartsWith("TRUE", StringComparison.OrdinalIgnoreCase) || trimmedParameters.StartsWith("FALSE", StringComparison.OrdinalIgnoreCase))
                    {
                        string parameter = trimmedParameters.Split(',')[0].ToLower();

                        bool boolean = bool.Parse(parameter);

                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.BOOL, Value = boolean ? BitConverter.GetBytes(1).Reverse().ToArray() : BitConverter.GetBytes(0).Reverse().ToArray(), LineNumber = LineNumber });
                        i += parameter.Length + 2;
                        continue;
                    }

                    // Is it a sound volume?
                    if (trimmedParameters.StartsWith("VOLUME[", StringComparison.OrdinalIgnoreCase))
                    {
                        string[] volumeComponents = trimmedParameters.Split(',')[0][7..^1].Split(' ');

                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.VOLUME, Value = CalculateControlStructure(volumeComponents[0], volumeComponents[1], objects), LineNumber = LineNumber });
                        i += volumeComponents.Sum(v => v.Length) + 10;
                        continue;
                    }

                    // Is it a color?
                    if (trimmedParameters.StartsWith("color", StringComparison.OrdinalIgnoreCase))
                    {
                        string parameter = trimmedParameters.Split(',')[0].ToLower();
                        string[] components = parameter.Split(' ');

                        int color = int.Parse(components[2], NumberStyles.HexNumber);

                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.COLOR, Value = CalculateControlStructure(components[1], $"{color}", objects), LineNumber = LineNumber });
                        i += parameter.Length + 2;
                        continue;
                    }

                    // Is it a character?
                    if (trimmedParameters.StartsWith("Character[", StringComparison.OrdinalIgnoreCase))
                    {
                        string characterName = trimmedParameters.Split(',')[0][10..^1].ToUpper();
                        int characterEntity = GetCharacterEntity(characterName);

                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.CHARACTER, Value = CalculateControlStructure("lit", $"{characterEntity}", objects), LineNumber = LineNumber });
                        i += characterName.Length + 12;
                        continue;
                    }

                    // Is it a precalculated vector?
                    if (trimmedParameters.StartsWith("PreVec[", StringComparison.OrdinalIgnoreCase))
                    {
                        string parameter = trimmedParameters.Split(',')[0][7..^1];
                        string[] components = parameter.Split(' ');

                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.PREVEC, Value = CalculateControlStructure(components[0], components[1], objects), LineNumber = LineNumber });
                        i += parameter.Length + 9;
                        continue;
                    }

                    // Is it a float?
                    if (trimmedParameters.StartsWith("float ", StringComparison.OrdinalIgnoreCase))
                    {
                        List<byte> bytes = new();
                        string[] components = trimmedParameters.Split(',')[0].Split(' ');

                        if (components[1] == "lit")
                        {
                            bytes.AddRange(CalculateControlStructure(components[1], $"{Helpers.FloatToInt(float.Parse(components[2]))}", objects));
                        }
                        else
                        {
                            bytes.AddRange(CalculateControlStructure(components[1], components[2], objects));
                        }

                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.FLOAT, Value = bytes.ToArray(), LineNumber = LineNumber });
                        i += components.Sum(c => c.Length) + 4;
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
                            bytes.AddRange(CalculateControlStructure(parts[1], $"{Helpers.FloatToInt(float.Parse(parts[2]))}", objects));
                        }

                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.VECTOR3, Value = bytes.ToArray(), LineNumber = LineNumber });
                        i += vectorComponents.Sum(c => c.Length) + 12;
                        continue;
                    }

                    // Is it a var index?
                    if (trimmedParameters.StartsWith('$'))
                    {
                        string scriptObject = trimmedParameters.Split(',')[0][1..].ToUpper();

                        if (!objects.Contains(scriptObject))
                        {
                            objects.Add(scriptObject);
                        }
                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.VARINDEX, Value = BitConverter.GetBytes((short)objects.IndexOf(scriptObject)).Reverse().ToArray(), LineNumber = LineNumber });
                        i += scriptObject.Length + 2;
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
                            bytes.AddRange(CalculateControlStructure(parts[0], parts[1], objects));
                        }

                        i += arrayStrings.Sum(a => a.Length) + 4;
                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.INTARRAY, Value = bytes.ToArray(), LineNumber = LineNumber });
                        continue;
                    }

                    // Is it lip sync data?
                    if (trimmedParameters.StartsWith("LIPSYNC[", StringComparison.OrdinalIgnoreCase))
                    {
                        List<byte> bytes = new();
                        string lipSyncString = trimmedParameters.Split(']')[0][8..];
                        Regex lipSyncRegex = new(@"(?<lipFlap>[saiueonN])(?<length>\d+)");

                        for (int j = 0; j < lipSyncString.Length;)
                        {
                            Match nextLipSync = lipSyncRegex.Match(lipSyncString[j..]);
                            bytes.Add(ScriptCommand.LipSyncMap.FirstOrDefault(l => l.Value == nextLipSync.Groups["lipFlap"].Value).Key);
                            bytes.Add(byte.Parse(nextLipSync.Groups["length"].Value));
                            j += nextLipSync.Value.Length;
                        }

                        bytes.AddRange(new byte[4]);

                        bytes.InsertRange(0, BitConverter.GetBytes(bytes.Count + 4).Reverse());

                        i += lipSyncString.Length + 11;
                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.LIPSYNCDATA, Value = bytes.ToArray(), LineNumber = LineNumber });
                        continue;
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

                        Parameters.Add(new() { Type = (ScriptCommand.ParameterType)typeCode, Value = bytes.ToArray(), LineNumber = LineNumber });
                        i += parameter.Length + 2;
                        continue;
                    }

                    // It has to be some sort of int; try to see if it's an unknown kind
                    if (int.TryParse(trimmedParameters[0..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int type))
                    {
                        string[] parameter = trimmedParameters[2..].Split(',')[0].Split(' ');

                        Parameters.Add(new() { Type = (ScriptCommand.ParameterType)type, Value = CalculateControlStructure(parameter[0], parameter[1], objects), LineNumber = LineNumber });
                        i += parameter.Sum(p => p.Length) + 4;
                        continue;
                    }

                    // It's a regular int
                    string[] intParams = trimmedParameters.Split(',')[0].Split(' ');
                    Parameters.Add(new() { Type = ScriptCommand.ParameterType.INT, Value = CalculateControlStructure(intParams[0], intParams[1], objects), LineNumber = LineNumber });
                    i += intParams.Sum(p => p.Length) + 2;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"ERROR: Failed to compile line {LineNumber} (command: {Command.Name}): first parameter of '{parameters[i..]}' threw \"{e.Message}\"");
                    return;
                }
            }
        }

        public bool ResolveAddresses(List<(string label, int lineNumber)> labels)
        {
            bool resolvedAddress = false;
            for (int i = 0; i < Parameters.Count; i++)
            {
                if (Parameters[i].Type == ScriptCommand.ParameterType.ADDRESS)
                {
                    int lineNumber;
                    if (Parameters[i].Value[0] == 0x00)
                    {
                        List<byte> tempBytes = new(new byte[] { 0 });
                        tempBytes.AddRange(Parameters[i].Value[1..]);
                        lineNumber = BitConverter.ToInt32(tempBytes.ToArray().Reverse().ToArray()); // why dear god did you do this? bc roslyn won't let me do it w/ List.Reverse() existing
                    }
                    else
                    {
                        List<byte> tempBytes = new(new byte[] { 0 });
                        tempBytes.AddRange(Parameters[i].Value[1..4]);
                        lineNumber = labels[BitConverter.ToInt32(tempBytes.ToArray().Reverse().ToArray())].lineNumber;
                    }
                    int address = AllOtherInvocations.FirstOrDefault(i => i.LineNumber == lineNumber)?.Address ?? -1;
                    if (address < 0)
                    {
                        throw new ArgumentException($"ERROR: Line {LineNumber} (command {Command.Name}) attempting to resolve address to line {lineNumber} when no such line exists.");
                    }
                    Parameters[i] = new Parameter { Type = Parameters[i].Type, Value = BitConverter.GetBytes(address).Reverse().ToArray(), LineNumber = LineNumber };
                    resolvedAddress = true;
                }
                else if (Parameters[i].Type == ScriptCommand.ParameterType.INDEXEDADDRESS)
                {
                    List<byte> parameterBytes = new(Parameters[i].Value);
                    int lineNumber;
                    if (Parameters[i].Value[0] == 0x00)
                    {
                        List<byte> tempBytes = new(new byte[] { 0 });
                        tempBytes.AddRange(Parameters[i].Value[1..4]);
                        lineNumber = BitConverter.ToInt32(tempBytes.ToArray().Reverse().ToArray());
                    }
                    else
                    {
                        List<byte> tempBytes = new(new byte[] { 0 });
                        tempBytes.AddRange(Parameters[i].Value[1..4]);
                        lineNumber = labels[BitConverter.ToInt32(tempBytes.ToArray().Reverse().ToArray())].lineNumber;
                    }
                    int address = AllOtherInvocations.FirstOrDefault(i => i.LineNumber == lineNumber)?.Address ?? -1;
                    if (address < 0)
                    {
                        throw new ArgumentException($"ERROR: Line {LineNumber} (command {Command.Name}) attempting to resolve address to line {lineNumber} when no such line exists.");
                    }
                    parameterBytes.RemoveRange(0, 4);
                    parameterBytes.InsertRange(0, BitConverter.GetBytes(address).Reverse());
                    Parameters[i] = new Parameter { Type = Parameters[i].Type, Value = parameterBytes.ToArray(), LineNumber = LineNumber };
                    resolvedAddress = true;
                }
            }
            return resolvedAddress;
        }

        public string ParseParameter(Parameter parameter)
        {
            switch (parameter.Type)
            {
                case ScriptCommand.ParameterType.ADDRESS:
                    return ParseAddress(parameter.Value);
                case ScriptCommand.ParameterType.DIALOGUE:
                    return $"\"{ScriptObjects[BitConverter.ToInt16(parameter.Value.Reverse().ToArray())].Replace("\n", "\\n").Replace("\"", "\\\"")}\"";
                case ScriptCommand.ParameterType.CONDITIONAL:
                    return ParseConditional(parameter.Value);
                case ScriptCommand.ParameterType.TIMESPAN:
                    return ParseTime(parameter.Value);
                case ScriptCommand.ParameterType.VECTOR2:
                    return ParseVector2(parameter.Value);
                case ScriptCommand.ParameterType.INT:
                    return CalculateIntParameter(Helpers.GetIntFromByteArray(parameter.Value, 0), Helpers.GetIntFromByteArray(parameter.Value, 1));
                case ScriptCommand.ParameterType.TRANSITION:
                    return ParseTransition(parameter.Value);
                case ScriptCommand.ParameterType.INDEXEDADDRESS:
                    return ParseIndexedAddress(parameter.Value);
                case ScriptCommand.ParameterType.ANGLE:
                    return $"degrees {CalculateIntParameter(Helpers.GetIntFromByteArray(parameter.Value, 0), Helpers.GetIntFromByteArray(parameter.Value, 1))}";
                case ScriptCommand.ParameterType.BOOL:
                    return ParseBoolean(parameter.Value);
                case ScriptCommand.ParameterType.VOLUME:
                    return $"VOLUME[{CalculateIntParameter(Helpers.GetIntFromByteArray(parameter.Value, 0), Helpers.GetIntFromByteArray(parameter.Value, 1))}]";
                case ScriptCommand.ParameterType.COLOR:
                    return ParseColor(CalculateIntParameter(Helpers.GetIntFromByteArray(parameter.Value, 0), Helpers.GetIntFromByteArray(parameter.Value, 1)));
                case ScriptCommand.ParameterType.CHARACTER:
                    return GetCharacter(CalculateIntParameter(Helpers.GetIntFromByteArray(parameter.Value, 0), Helpers.GetIntFromByteArray(parameter.Value, 1)));
                case ScriptCommand.ParameterType.INT0E:
                    return $"0E{CalculateIntParameter(Helpers.GetIntFromByteArray(parameter.Value, 0), Helpers.GetIntFromByteArray(parameter.Value, 1))}";
                case ScriptCommand.ParameterType.PREVEC:
                    return $"PreVec[{CalculateIntParameter(Helpers.GetIntFromByteArray(parameter.Value, 0), Helpers.GetIntFromByteArray(parameter.Value, 1))}]";
                case ScriptCommand.ParameterType.FLOAT:
                    return ParseFloat(parameter.Value);
                case ScriptCommand.ParameterType.VECTOR3:
                    return ParseVector3(parameter.Value);
                case ScriptCommand.ParameterType.VARINDEX:
                    return $"${ScriptObjects[BitConverter.ToInt16(parameter.Value.Reverse().ToArray())]}";
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
                case ScriptCommand.ParameterType.LIPSYNCDATA:
                    return ParseLipSyncData(parameter.Value);
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
                    return $"var {ScriptObjects[valueCode]}";
                default:
                    return "-1";
            }
        }

        public static byte[] CalculateControlStructure(string controlCode, string value, List<string> objects)
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
                    if (!objects.Contains(value))
                    {
                        objects.Add(value);
                    }
                    bytes.AddRange(BitConverter.GetBytes(objects.IndexOf(value)).Reverse());
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

        private static string ParseBoolean(byte[] type0AParameterData)
        {
            return Helpers.GetIntFromByteArray(type0AParameterData, 0) != 0 ? "TRUE" : "FALSE";
        }

        private static string GetCharacter(int characterCode)
        {
            if (characterCode <= 32)
            {
                return ((Speaker)characterCode).ToString();
            }
            else
            {
                return $"UNKNOWN{characterCode}";
            }
        }

        public static string ParseColor(string colorCode)
        {
            if (colorCode.StartsWith("lit"))
            {
                int color = int.Parse(colorCode[4..]);
                return $"color lit {color:X8}";
            }
            else
            {
                return $"color {colorCode}";
            }
        }

        private static short GetCharacterEntity(string character)
        {
            if (character.StartsWith("UNKNOWN"))
            {
                return short.Parse(character[7..]);
            }
            else
            {
                return (short)((int)Enum.Parse(typeof(Speaker), character));
            }
        }

        private string ParseAddress(byte[] type00Parameter)
        {
            return $"{{{AllOtherInvocations.First(i => i.Address == Helpers.GetIntFromByteArray(type00Parameter, 0)).Label}}}";
        }

        private string ParseConditional(byte[] conditionalData)
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
            string param = Helpers.GetIntFromByteArray(type03Parameter, 0) == 0x201 ? "time_s " : "time_f ";
            param += CalculateIntParameter(Helpers.GetIntFromByteArray(type03Parameter, 1), Helpers.GetIntFromByteArray(type03Parameter, 2));
            return param;
        }

        private string ParseTransition(byte[] type06Parameter)
        {
            string parameter = CalculateIntParameter(Helpers.GetIntFromByteArray(type06Parameter, 0), Helpers.GetIntFromByteArray(type06Parameter, 1));
            if (parameter.StartsWith("lit "))
            {
                int parsedInt = int.Parse(parameter[4..]);
                string transition = ScriptCommand.TransitionToCodeMap.FirstOrDefault(t => t.Value == parsedInt).Key;
                if (transition is not null)
                {
                    return $"Transition[{transition}]";
                }
                else
                {
                    return $"Transition[{parameter}]";
                }
            }
            else
            {
                return $"Transition[{parameter}]";
            }
        }

        private string ParseFloat(byte[] type12Parameter)
        {
            string startingString = CalculateIntParameter(Helpers.GetIntFromByteArray(type12Parameter, 0), Helpers.GetIntFromByteArray(type12Parameter, 1));
            if (startingString.StartsWith("lit"))
            {
                int startingInt = int.Parse(startingString.Split(' ')[1]);
                return $"float lit {Helpers.IntToFloat(startingInt)}";
            }
            else
            {
                return $"float {startingString}";
            }
        }

        private string ParseIndexedAddress(byte[] type07Parameter)
        {
            string lineNumber = ParseAddress(type07Parameter[0..4]);
            string alwaysUse = Helpers.GetIntFromByteArray(type07Parameter, 1) != 0 ? "TRUE" : "FALSE";
            string index = CalculateIntParameter(Helpers.GetIntFromByteArray(type07Parameter, 2), Helpers.GetIntFromByteArray(type07Parameter, 3));

            return $"({lineNumber}, {alwaysUse}, {index})";
        }

        private string ParseVector2(byte[] type04Parameter)
        {
            string[] coords = new string[]
            {
                CalculateIntParameter(Helpers.GetIntFromByteArray(type04Parameter, 0), Helpers.GetIntFromByteArray(type04Parameter, 1)),
                CalculateIntParameter(Helpers.GetIntFromByteArray(type04Parameter, 2), Helpers.GetIntFromByteArray(type04Parameter, 3)),
            };

            return $"Vector2({coords[0]}, {coords[1]})";
        }

        private string ParseVector3(byte[] type14Parameter)
        {
            string[] coords = new string[]
            {
                ParseFloat(type14Parameter[0..8]),
                ParseFloat(type14Parameter[8..16]),
                ParseFloat(type14Parameter[16..24]),
            };

            return $"Vector3({coords[0]}, {coords[1]}, {coords[2]})";
        }

        private static string ParseLipSyncData(byte[] type1DParameter)
        {
            string lipSyncData = "LIPSYNC[";

            for (int i = 4; i < type1DParameter.Length - 4; i += 2)
            {
                lipSyncData += $"{ScriptCommand.LipSyncMap[type1DParameter[i]]}{type1DParameter[i + 1]}";
            }

            return $"{lipSyncData}]";
        }
    }
}
