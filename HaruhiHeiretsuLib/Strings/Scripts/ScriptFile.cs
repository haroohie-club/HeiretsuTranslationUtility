using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources.NetStandard;
using System.Text;

namespace HaruhiHeiretsuLib.Strings.Scripts
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

        public const int DIALOGUE_LINE_LENGTH = int.MaxValue;

        public ScriptFile()
        {
        }

        public ScriptFile(int parent, int child, byte[] data, ushort mcbId = 0)
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

            (ScriptCommandBlock commandBlock, DialogueLine[] dialogue)[] dialogueLines = ScriptCommandBlocks
                .Select(b => (b, b.Invocations.Where(i => i.CommandCode != 0x4B) // TL_ADD (4B) does not have real dialogue, just references
                .SelectMany(i => i.Parameters.Where(p => p.Type == ScriptCommand.ParameterType.DIALOGUE)
                .Select(p => new DialogueLine()
                {
                    Line = Objects[BitConverter.ToInt16(p.Value.Reverse().ToArray())],
                    Speaker = (i.CommandCode >= 0x2E && i.CommandCode <= 0x31 ? ScriptFileSpeaker.CHOICE : (ScriptFileSpeaker)i.CharacterEntity).ToString(),
                    Offset = i.Address,
                })).ToArray())).ToArray();

            // Add voice file metadata
            for (int i = 0; i < dialogueLines.Length; i++)
            {
                if (dialogueLines[i].dialogue.Length > 0)
                {
                    dialogueLines[i].dialogue[0].Metadata.Add($"Block '{dialogueLines[i].commandBlock.Name}' Start");

                    for (int j = 0; j < dialogueLines[i].dialogue.Length; j++)
                    {
                        string voiceFile = Objects.ElementAtOrDefault(Helpers.ToShortOrDefault(ScriptCommandBlocks
                            .SelectMany(b => (b.Invocations
                            .FirstOrDefault(inv => (inv?.Address ?? -1) == dialogueLines[i].dialogue[j].Offset)?.Parameters ?? new List<Parameter>())
                            .FirstOrDefault(p => p.Type == ScriptCommand.ParameterType.VARINDEX)?.Value ?? Array.Empty<byte>())) ?? -1);
                        if (!string.IsNullOrEmpty(voiceFile))
                        {
                            dialogueLines[i].dialogue[j].Metadata.Add(voiceFile);
                        }
                    }
                }
            }

            DialogueLines = dialogueLines.SelectMany(k => k.dialogue).ToList();
        }

        private Parameter[] GetDialogueParameters()
        {
            return ScriptCommandBlocks.SelectMany(b => b.Invocations.Where(i => i.CommandCode != 0x4B)
                .SelectMany(i => i.Parameters.Where(p => p.Type == ScriptCommand.ParameterType.DIALOGUE))).ToArray();
        }

        public override void EditDialogue(int index, string newLine)
        {
            DialogueLines[index].Line = newLine;
            Objects.Add(newLine); // add new line to the script objects collection; when recompiling, the old line will be removed if it is not referenced elsewhere in the script

            Parameter[] dialogueParams = GetDialogueParameters();
            dialogueParams[index].Value = BitConverter.GetBytes((short)(Objects.Count - 1)).Reverse().ToArray(); // change the dialogue pointer to the new script object

            Recompile();
        }

        public override void ImportResxFile(string fileName, FontReplacementMap fontReplacementMap)
        {
            base.ImportResxFile(fileName, fontReplacementMap);

            TextReader textReader = GetResxReader(fileName);

            using ResXResourceReader resxReader = new(textReader);
            foreach (DictionaryEntry d in resxReader)
            {
                int dialogueIndex = int.Parse(((string)d.Key)[0..4]);
                string dialogueText = ProcessDialogueLineWithFontReplacement(NormalizeDialogueLine((string)d.Value), fontReplacementMap, DIALOGUE_LINE_LENGTH);

                EditDialogue(dialogueIndex, dialogueText);
            }
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

        public void PopulateCommandBlocks(short[] eventFileIndices = null)
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
            if (DialogueLines.Count > 0)
            {
                TagDialogueWithVjumpMetadata();
                if (eventFileIndices is not null)
                {
                    TagDialogueWithEventFileMetadata(eventFileIndices);
                }
            }
        }

        private void TagDialogueWithVjumpMetadata()
        {
            Parameter[] dialogueParams = GetDialogueParameters();
            ScriptCommandInvocation[] allInvocations = ScriptCommandBlocks.SelectMany(b => b.Invocations).ToArray();
            Dictionary<int, int> NumChoicesPerInvocationIndex = new();

            for (int i = 0; i < DialogueLines.Count; i++)
            {
                if (DialogueLines[i].Speaker == ScriptFileSpeaker.CHOICE.ToString())
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

        public void TagDialogueWithEventFileMetadata(short[] scriptEventFiles)
        {
            ScriptCommandInvocation[] allInvocations = ScriptCommandBlocks.SelectMany(b => b.Invocations).ToArray();

            for (int i = 0; i < allInvocations.Length; i++)
            {
                if (allInvocations[i].Command.Name == "EV_START")
                {
                    int eventId = scriptEventFiles[int.Parse(allInvocations[i].CalculateIntParameter(Helpers.GetIntFromByteArray(allInvocations[i].Parameters.First(p => p.Type == ScriptCommand.ParameterType.INT).Value, 0),
                        Helpers.GetIntFromByteArray(allInvocations[i].Parameters.First(p => p.Type == ScriptCommand.ParameterType.INT).Value, 1))[4..])];
                    List<int> chapters = new();
                    byte[] chaptersParam = allInvocations[i].Parameters.FirstOrDefault(p => p.Type == ScriptCommand.ParameterType.INTARRAY)?.Value;
                    if (chaptersParam is not null)
                    {
                        int numValues = Helpers.GetIntFromByteArray(chaptersParam, 0);
                        for (int j = 1; j <= numValues; j++)
                        {
                            chapters.Add(int.Parse(allInvocations[i].CalculateIntParameter(Helpers.GetIntFromByteArray(chaptersParam, j * 2 - 1), Helpers.GetIntFromByteArray(chaptersParam, j * 2))[4..]));
                        }
                    }

                    int minDistance = int.MaxValue;
                    int minDistanceLine = 0;
                    for (int j = 0; j < DialogueLines.Count; j++)
                    {
                        int distanceBetweenDialogueLineAndEventStart = allInvocations.Where(inv => inv.Address == DialogueLines[j].Offset)
                            .Select(inv => Math.Abs(inv.LineNumber - allInvocations[i].LineNumber)).FirstOrDefault();
                        if (distanceBetweenDialogueLineAndEventStart < minDistance)
                        {
                            minDistance = distanceBetweenDialogueLineAndEventStart;
                            minDistanceLine = j;
                        }
                    }

                    string beforeAfter = DialogueLines[minDistanceLine].Offset > allInvocations[i].Address ? "before" : "after";
                    string chaptersString = chapters.Count > 0 ? $" (ch {string.Join(", ", chapters)})" : "";
                    DialogueLines[minDistanceLine].Metadata.Add($"Event evt-{eventId:D4}{chaptersString} starts {beforeAfter}");
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
}
