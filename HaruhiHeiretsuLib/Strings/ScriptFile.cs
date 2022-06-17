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

            ObjectsEnd += lengthDifference;
            Data.RemoveRange(ObjectssEndOffset, 4);
            Data.InsertRange(ObjectssEndOffset, BitConverter.GetBytes(ObjectsEnd).Reverse());

            ScriptCommandBlockDefinitionsEnd = ObjectsEnd + 8 * NumScriptCommandBlocks;
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
            Dictionary<string, int> labels = new();
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
            INT08 = 8,
            UNKNOWN09 = 9,
            BOOL = 10,
            UNKNOWN0B = 11,
            COLOR = 12,
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
            LIPSYNCDATA = 29,
            UNKNOWN29 = 41,
            UNKNOWN2A = 42,
        }

        public static readonly Dictionary<short, string> CharacterCodeToCharacterMap = new()
        {
            { 0, "KYON" },
            { 1, "KYON2" },
            { 2, "HARUHI" },
            { 3, "NAGATO" },
            { 4, "NAGATO2" },
            { 5, "MIKURU" },
            { 6, "MIKURU2" },
            { 7, "KOIZUMI" },
            { 8, "KOIZUMI2" },
            { 9, "TSURUYA" },
            { 10, "KYN_SIS" },
            { 11, "MIKOTO" },
            { 12, "MIKOTO2" },
            { 13, "TAIICHIRO" },
            { 15, "CAPTAIN" },
            { 16, "GUEST_M3" },
            { 17, "GUEST_F3_17" },
            { 18, "CREW_F" },
            { 19, "CREW_M" },
            { 20, "GUEST_M1_20" },
            { 21, "GUEST_M2_21" },
            { 22, "GUEST_F1" },
            { 23, "GUEST_F2" },
            { 27, "GUEST_M2_27" },
            { 24, "TANIGUCHI" },
            { 25, "KUNIKIDA" },
            { 26, "ANNOUNCER" },
            { 28, "GUEST_F3" },
            { 29, "???" },
            { 30, "TAIICHIRO_30" },
            { 31, "MIKOTO_31" },
            { 32, "GUEST_M1_32" },
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

        public int ParseBlock(int lineNumber, string[] lines, List<ScriptCommand> allCommands, List<string> objects, Dictionary<string, int> labels)
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

        public ScriptCommandInvocation(string invocation, short lineNumber, List<ScriptCommand> allCommands, List<string> objects, Dictionary<string, int> labels)
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

        public void ParseInvocation(string invocation, short lineNumber, List<ScriptCommand> allCommands, List<string> objects, Dictionary<string, int> labels)
        {
            Regex labelRegex = new(@"^{(?<label>[\w\d]+)}");

            Match lineLabelMatch = Regex.Match(invocation, @"^(?<label>[\w\d]+): ");
            if (lineLabelMatch.Success)
            {
                Label = lineLabelMatch.Groups["label"].Value;
                invocation = invocation[lineLabelMatch.Length..];
                labels.Add(Label, lineNumber);
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
                            bytes.AddRange(BitConverter.GetBytes(varLineNumber).Reverse());
                            Parameters.Add(new() { Type = ScriptCommand.ParameterType.ADDRESS, Value = bytes.ToArray() });
                            i += parameter.Length + 2;
                            continue;
                        }
                    }
                    else if (labelMatch.Success)
                    {
                        List<byte> bytes = new(new byte[] { 1 });
                        bytes.AddRange(Encoding.ASCII.GetBytes(labelMatch.Groups["label"].Value));
                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.ADDRESS, Value = bytes.ToArray() });
                        i += labelMatch.Length + 1;
                        continue;
                    }

                    // See if it's a string
                    if (trimmedParameters.StartsWith("\""))
                    {
                        int firstQuote = trimmedParameters.IndexOf('"');
                        int secondQuote = trimmedParameters[(firstQuote + 1)..].IndexOf('"');
                        string line = trimmedParameters[(firstQuote + 1)..(secondQuote + 1)].Replace("\\n", "\n");

                        objects.Add(line);
                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.DIALOGUE, Value = BitConverter.GetBytes((short)(objects.Count - 1)).Reverse().ToArray() });

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
                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.CONDITIONAL, Value = bytes.ToArray() });
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
                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.TIMESPAN, Value = bytes.ToArray() });
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

                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.VECTOR2, Value = bytes.ToArray() });
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

                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.TRANSITION, Value = bytes.ToArray() });
                        i += parameter.Length + 13;
                        continue;
                    }

                    // Is it an indexed address?
                    if (trimmedParameters.StartsWith("("))
                    {
                        List<byte> bytes = new();
                        string[] components = trimmedParameters.Split(')')[0].Split(',');

                        Match indexedLabelMatch = labelRegex.Match(components[0]);
                        if (indexedLabelMatch.Success)
                        {
                            bytes.Add(1);
                            bytes.AddRange(Encoding.ASCII.GetBytes(indexedLabelMatch.Groups["label"].Value));
                            bytes.Add(0);
                        }
                        else
                        {
                            bytes.Add(0);
                            bytes.AddRange(BitConverter.GetBytes(int.Parse(components[0].Trim())).Reverse());
                        }

                        bytes.AddRange(components[1].Trim() == "TRUE" ? new byte[] { 0x01, 0x00, 0x00, 0x00 } : new byte[4]); // weird little-endian looking thing is on purpose; matches game
                        string[] controlCodeComponents = components[2].Trim().Split(' ');
                        bytes.AddRange(CalculateControlStructure(controlCodeComponents[0], controlCodeComponents[1], objects));
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

                    // Is it a color?
                    if (trimmedParameters.StartsWith("color", StringComparison.OrdinalIgnoreCase))
                    {
                        string parameter = trimmedParameters.Split(',')[0].ToLower();
                        string[] components = parameter.Split(' ');

                        int color = int.Parse(components[2], NumberStyles.HexNumber);

                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.COLOR, Value = CalculateControlStructure(components[1], $"{color}", objects) });
                        i += parameter.Length + 2;
                        continue;
                    }

                    // Is it a character?
                    if (trimmedParameters.StartsWith("Character[", StringComparison.OrdinalIgnoreCase))
                    {
                        string characterName = trimmedParameters.Split(',')[0][10..^1].ToUpper();
                        int characterEntity = GetCharacterEntity(characterName);

                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.CHARACTER, Value = CalculateControlStructure("lit", $"{characterEntity}", objects) });
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
                            bytes.AddRange(CalculateControlStructure(parts[0], parts[1], objects));
                        }

                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.VECTOR3, Value = bytes.ToArray() });
                        i += vectorComponents.Sum(c => c.Length) + 11;
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
                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.VARINDEX, Value = BitConverter.GetBytes((short)objects.IndexOf(scriptObject)).Reverse().ToArray() });
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
                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.INTARRAY, Value = bytes.ToArray() });
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
                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.LIPSYNCDATA, Value = bytes.ToArray() });
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

                        Parameters.Add(new() { Type = (ScriptCommand.ParameterType)typeCode, Value = bytes.ToArray() });
                        i += parameter.Length + 2;
                        continue;
                    }

                    // It has to be some sort of int; try to see if it's an unknown kind
                    if (int.TryParse(trimmedParameters[0..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int type))
                    {
                        string[] parameter = trimmedParameters[2..].Split(',')[0].Split(' ');

                        Parameters.Add(new() { Type = (ScriptCommand.ParameterType)type, Value = CalculateControlStructure(parameter[0], parameter[1], objects) });
                        i += parameter.Sum(p => p.Length) + 4;
                        continue;
                    }

                    // It's a regular int
                    string[] intParams = trimmedParameters.Split(',')[0].Split(' ');
                    Parameters.Add(new() { Type = ScriptCommand.ParameterType.INT, Value = CalculateControlStructure(intParams[0], intParams[1], objects) });
                    i += intParams.Sum(p => p.Length) + 2;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"ERROR: Failed to compile line {LineNumber} (command: {Command.Name}): first parameter of '{parameters[i..]}' threw \"{e.Message}\"");
                    throw;
                }
            }
        }

        public bool ResolveAddresses(Dictionary<string, int> labels)
        {
            bool resolvedAddress = false;
            for (int i = 0; i < Parameters.Count; i++)
            {
                if (Parameters[i].Type == ScriptCommand.ParameterType.ADDRESS)
                {
                    int lineNumber;
                    if (Parameters[i].Value[0] == 0x00)
                    {
                        lineNumber = BitConverter.ToInt32(Parameters[i].Value[1..].Reverse().ToArray());
                    }
                    else
                    {
                        lineNumber = labels[Encoding.ASCII.GetString(Parameters[i].Value[1..])];
                    }
                    int address = AllOtherInvocations.FirstOrDefault(i => i.LineNumber == lineNumber)?.Address ?? -1;
                    if (address < 0)
                    {
                        throw new ArgumentException($"ERROR: Line {LineNumber} (command {Command.Name}) attempting to resolve address to line {lineNumber} when no such line exists.");
                    }
                    Parameters[i] = new Parameter { Type = Parameters[i].Type, Value = BitConverter.GetBytes(address).Reverse().ToArray() };
                    resolvedAddress = true;
                }
                else if (Parameters[i].Type == ScriptCommand.ParameterType.INDEXEDADDRESS)
                {
                    List<byte> parameterBytes = new(Parameters[i].Value);
                    int length;
                    int lineNumber;
                    if (Parameters[i].Value[0] == 0x00)
                    {
                        lineNumber = BitConverter.ToInt32(Parameters[i].Value.Skip(1).Take(4).Reverse().ToArray());
                        length = 4;
                    }
                    else
                    {
                        string label = Encoding.ASCII.GetString(Parameters[i].Value.Skip(1).TakeWhile(b => b != 0x00).ToArray());
                        lineNumber = labels[label];
                        length = label.Length + 1;
                    }
                    int address = AllOtherInvocations.FirstOrDefault(i => i.LineNumber == lineNumber)?.Address ?? -1;
                    if (address < 0)
                    {
                        throw new ArgumentException($"ERROR: Line {LineNumber} (command {Command.Name}) attempting to resolve address to line {lineNumber} when no such line exists.");
                    }
                    parameterBytes.RemoveRange(0, length + 1);
                    parameterBytes.InsertRange(0, BitConverter.GetBytes(address).Reverse());
                    Parameters[i] = new Parameter { Type = Parameters[i].Type, Value = parameterBytes.ToArray() };
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
                    return $"\"{ScriptObjects[BitConverter.ToInt16(parameter.Value.Reverse().ToArray())].Replace("\n", "\\n")}\"";
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
                case ScriptCommand.ParameterType.INT08:
                    return $"08{CalculateIntParameter(Helpers.GetIntFromByteArray(parameter.Value, 0), Helpers.GetIntFromByteArray(parameter.Value, 1))}";
                case ScriptCommand.ParameterType.BOOL:
                    return ParseBoolean(parameter.Value);
                case ScriptCommand.ParameterType.COLOR:
                    return ParseColor(CalculateIntParameter(Helpers.GetIntFromByteArray(parameter.Value, 0), Helpers.GetIntFromByteArray(parameter.Value, 1)));
                case ScriptCommand.ParameterType.CHARACTER:
                    return GetCharacter(CalculateIntParameter(Helpers.GetIntFromByteArray(parameter.Value, 0), Helpers.GetIntFromByteArray(parameter.Value, 1)));
                case ScriptCommand.ParameterType.INT0E:
                    return $"0E{CalculateIntParameter(Helpers.GetIntFromByteArray(parameter.Value, 0), Helpers.GetIntFromByteArray(parameter.Value, 1))}";
                case ScriptCommand.ParameterType.INT10:
                    return $"10{CalculateIntParameter(Helpers.GetIntFromByteArray(parameter.Value, 0), Helpers.GetIntFromByteArray(parameter.Value, 1))}";
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
            if (ScriptCommand.CharacterCodeToCharacterMap.TryGetValue((short)characterCode, out string character))
            {
                return character;
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
                return ScriptCommand.CharacterCodeToCharacterMap.First(c => c.Value == character).Key;
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
                CalculateIntParameter(Helpers.GetIntFromByteArray(type14Parameter, 0), Helpers.GetIntFromByteArray(type14Parameter, 1)),
                CalculateIntParameter(Helpers.GetIntFromByteArray(type14Parameter, 2), Helpers.GetIntFromByteArray(type14Parameter, 3)),
                CalculateIntParameter(Helpers.GetIntFromByteArray(type14Parameter, 4), Helpers.GetIntFromByteArray(type14Parameter, 5)),
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
