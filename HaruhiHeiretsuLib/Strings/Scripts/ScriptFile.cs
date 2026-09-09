using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources.NetStandard;
using System.Text;
using HaruhiHeiretsuLib.Util;

namespace HaruhiHeiretsuLib.Strings.Scripts;

/// <summary>
/// Represents a script file in scr.bin
/// </summary>
public class ScriptFile : StringsFile
{
    /// <summary>
    /// Name of the script file
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Internal name (i.e. within the file) of the script file
    /// </summary>
    public string InternalName { get; set; }
    /// <summary>
    /// "Room" (map) that the script uses
    /// </summary>
    public string Room { get; set; }
    /// <summary>
    /// Time of day in which the script takes place
    /// </summary>
    public string Time { get; set; }

    /// <summary>
    /// List of all available commands
    /// </summary>
    public List<ScriptCommand> AvailableCommands { get; set; }
    /// <summary>
    /// List of script command blocks within the script
    /// </summary>
    public List<ScriptCommandBlock> ScriptCommandBlocks { get; set; } = [];

    /// <summary>
    /// List of objects in the script
    /// </summary>
    public List<string> Objects { get; set; } = [];

    /// <summary>
    /// Offset where the number of objects is stored
    /// </summary>
    public int NumObjectsOffset { get; set; }
    /// <summary>
    /// Number of objects
    /// </summary>
    public short NumObjects { get; set; }
    /// <summary>
    /// Offset to the number of script command blocks
    /// </summary>
    public int NumScriptCommandBlocksOffset { get; set; }
    /// <summary>
    /// Number of script command blocks
    /// </summary>
    public short NumScriptCommandBlocks { get; set; }
    /// <summary>
    /// End offset of the objects
    /// </summary>
    public int ObjectsEndOffset { get; set; }
    /// <summary>
    /// End of objects
    /// </summary>
    public int ObjectsEnd { get; set; }
    /// <summary>
    /// Script command block definitions end offset
    /// </summary>
    public int ScriptCommandBlockDefinitionsEndOffset { get; set; }
    /// <summary>
    /// End of the script command block definitions
    /// </summary>
    public int ScriptCommandBlockDefinitionsEnd { get; set; }

    /// <summary>
    /// Length of dialogue lines
    /// </summary>
    public static readonly int[] DialogueLineLengths = [9368, 9368, 9000, 9000];

    /// <summary>
    /// Empty constructor for serialization (?)
    /// </summary>
    public ScriptFile()
    {
    }

    /// <summary>
    /// Constructs and parses a script file from MCB and data
    /// </summary>
    /// <param name="parent">MCB parent archive</param>
    /// <param name="child">MCB child archive</param>
    /// <param name="data">Binary script file data</param>
    /// <param name="mcbId">The ID of the MCB archive</param>
    public ScriptFile(int parent, int child, byte[] data, ushort mcbId = 0)
    {
        Location = (parent, child);
        McbId = mcbId;
        Data = [.. data];

        ParseScript();
    }

    /// <inheritdoc/>
    public override void Initialize(byte[] decompressedData, int offset)
    {
        Offset = offset;
        Data = [.. decompressedData];

        ParseScript();
    }

    /// <inheritdoc/>
    public override byte[] GetBytes() => Data.ToArray();

    private static string ReadString(byte[] data, int currentPosition, out int newPosition)
    {
        int stringLength = IO.ReadInt(data, currentPosition);
        string result = IO.ReadShiftJisStringOfLength(data, currentPosition + 4, stringLength - 1);
        newPosition = currentPosition + stringLength + 4;
        return result;
    }

    private void ParseScript()
    {
        byte[] quickData = Data.ToArray();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        int pos = 0;
        InternalName = ReadString(quickData, pos, out pos);

        if (InternalName.Length > 20)
        {
            InternalName = "";
            return;
        }

        Room = ReadString(quickData, pos, out pos);
        Time = ReadString(quickData, pos, out pos);

        NumObjects = IO.ReadShort(quickData, pos);
        NumObjectsOffset = pos;
        pos += 2;

        NumScriptCommandBlocks = IO.ReadShort(quickData, pos);
        NumScriptCommandBlocksOffset = pos;
        pos += 2;

        ObjectsEnd = IO.ReadInt(quickData, pos);
        ObjectsEndOffset = pos;
        pos += 4;

        ScriptCommandBlockDefinitionsEnd = IO.ReadInt(quickData, pos);
        ScriptCommandBlockDefinitionsEndOffset = pos;
        pos += 4;

        for (int i = 0; i < NumObjects; i++)
        {
            Objects.Add(ReadString(quickData, pos, out pos));
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
                endAddress = IO.ReadInt(quickData, i + 12);
            }
            ScriptCommandBlocks.Add(new(i, endAddress, quickData, Objects));
        }

        (ScriptCommandBlock commandBlock, DialogueLine[] dialogue)[] dialogueLines = ScriptCommandBlocks
            .Select(b => (b, b.Invocations.Where(i => i.CommandCode != 0x4B) // TL_ADD (4B) does not have real dialogue, just references
                .SelectMany(i => i.Parameters.Where(p => p.Type == ScriptCommand.ParameterType.DIALOGUE)
                    .Select(p => new DialogueLine()
                    {
                        Line = Objects[IO.ReadShort(p.Value, 0)],
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
                    int idx = i;
                    int jdx = j;
                    string voiceFile = Objects.ElementAtOrDefault(Helpers.ToShortOrDefault([
                        .. ScriptCommandBlocks
                            .SelectMany(b => (b.Invocations
                                                  .FirstOrDefault(inv =>
                                                      (inv?.Address ?? -1) == dialogueLines[idx].dialogue[jdx].Offset)
                                                  ?.Parameters ??
                                              [])
                                .FirstOrDefault(p => p.Type == ScriptCommand.ParameterType.VAR_INDEX)?.Value ?? [])
                    ]) ?? -1);
                    if (!string.IsNullOrEmpty(voiceFile))
                    {
                        dialogueLines[i].dialogue[j].Metadata.Add(voiceFile);
                    }
                }
            }
        }

        Strings = dialogueLines.SelectMany(k => k.dialogue).ToList();
    }

    private Parameter[] GetDialogueParameters()
    {
        return ScriptCommandBlocks.SelectMany(b => b.Invocations.Where(i => i.CommandCode != 0x4B)
            .SelectMany(i => i.Parameters.Where(p => p.Type == ScriptCommand.ParameterType.DIALOGUE))).ToArray();
    }

    /// <inheritdoc/>
    public override void EditString(int index, string newString)
    {
        Strings[index].Line = newString;
        Objects.Add(newString); // add new line to the script objects collection; when recompiling, the old line will be removed if it is not referenced elsewhere in the script

        Parameter[] dialogueParams = GetDialogueParameters();
        dialogueParams[index].Value = BitConverter.GetBytes((short)(Objects.Count - 1)).Reverse().ToArray(); // change the dialogue pointer to the new script object

        Recompile();
    }

    /// <inheritdoc/>
    public override void ImportResxFile(string fileName, FontReplacementMap fontReplacementMap)
    {
        base.ImportResxFile(fileName, fontReplacementMap);

        TextReader textReader = GetResxReader(fileName);

        using ResXResourceReader resxReader = new(textReader);
        foreach (DictionaryEntry d in resxReader)
        {
            int dialogueIndex = int.Parse(((string)d.Key)[..4]);
            string dialogueText = ProcessDialogueLineWithFontReplacement(NormalizeDialogueLine((string)d.Value), fontReplacementMap, DialogueLineLengths);

            if (dialogueText.Count(c => c == '\n') > 3 && BinArchiveIndex > 0)
            {
                Console.WriteLine($"Warning: file scr-{BinArchiveIndex:D4} has line too long: {dialogueIndex} (starts with '{dialogueText[..30]}')");
            }

            EditString(dialogueIndex, dialogueText);
        }
    }

    private void Recompile()
    {
        Compile(Decompile());
    }

    /// <summary>
    /// Decompiles a script binary file to an approximation of its original format
    /// </summary>
    /// <param name="fontReplacementMap">Font replacement map (used for converting lines)</param>
    /// <returns></returns>
    public string Decompile(FontReplacementMap fontReplacementMap = null)
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

                script += $"{invocation.GetInvocation(fontReplacementMap)}\n";
                currentLine++;
            }
        }

        return script;
    }

    /// <summary>
    /// Compiles a script from its decompiled form into binary
    /// </summary>
    /// <param name="code">The decompiled script code</param>
    /// <param name="fontReplacementMap">The font replacement map for strings</param>
    public void Compile(string code, FontReplacementMap fontReplacementMap = null)
    {
        List<byte> bytes = [];
        List<(string, int)> labels = [];
        string[] lines = code.Split('\n');
        string[] info = lines[0].Split(' ');
        InternalName = info[0];
        Room = info[1];
        Time = info[2];
        bytes.AddRange(Helpers.GetStringBytes(InternalName));
        bytes.AddRange(Helpers.GetStringBytes(Room));
        bytes.AddRange(Helpers.GetStringBytes(Time));

        ScriptCommandBlocks = [];
        Objects = [];

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
                lineNumber = commandBlock.ParseBlock(lineNumber, lines[(lineNumber - 1)..], AvailableCommands, Objects, labels, fontReplacementMap);
                ScriptCommandBlocks.Add(commandBlock);
            }
        }

        NumObjects = (short)Objects.Count;
        NumScriptCommandBlocks = (short)ScriptCommandBlocks.Count;

        NumObjectsOffset = bytes.Count;
        bytes.AddRange(BitConverter.GetBytes(NumObjects).Reverse());
        NumScriptCommandBlocksOffset = bytes.Count;
        bytes.AddRange(BitConverter.GetBytes(NumScriptCommandBlocks).Reverse());
        ObjectsEndOffset = bytes.Count;
        bytes.AddRange(BitConverter.GetBytes(0));
        ScriptCommandBlockDefinitionsEndOffset = bytes.Count;
        bytes.AddRange(BitConverter.GetBytes(0));

        foreach (string @object in Objects)
        {
            bytes.AddRange(Helpers.GetStringBytes(@object));
        }

        ObjectsEnd = bytes.Count;
        bytes.RemoveRange(ObjectsEndOffset, 4);
        bytes.InsertRange(ObjectsEndOffset, BitConverter.GetBytes(ObjectsEnd).Reverse());

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

    /// <summary>
    /// Populates command blocks with external data
    /// </summary>
    /// <param name="eventFileIndices">The indices of the event files</param>
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
                List<Parameter> addressParams =
                [
                    .. ScriptCommandBlocks[i].Invocations[j].Parameters.Where(p => p.Type == ScriptCommand.ParameterType.ADDRESS),
                    .. ScriptCommandBlocks[i].Invocations[j].Parameters.Where(p => p.Type == ScriptCommand.ParameterType.INDEXED_ADDRESS),
                ];

                foreach (Parameter param in addressParams)
                {
                    int address = IO.ReadInt(param.Value, 0);
                    ScriptCommandInvocation referencedCommand = allInvocations.First(a => a.Address == address);
                    if (string.IsNullOrEmpty(referencedCommand.Label))
                    {
                        referencedCommand.Label = $"label{numLabels++:D3}";
                    }
                }
            }
        }
        if (Strings.Count > 0)
        {
            TagDialogueWithVjumpMetadata();
            if (eventFileIndices is not null)
            {
                TagDialogueWithEventFileMetadata(eventFileIndices);
            }
            TagDialogueWithTopicMetadata();
        }
    }

    private void TagDialogueWithVjumpMetadata()
    {
        Parameter[] dialogueParams = GetDialogueParameters();
        ScriptCommandInvocation[] allInvocations = ScriptCommandBlocks.SelectMany(b => b.Invocations).ToArray();
        Dictionary<int, int> numChoicesPerInvocationIndex = [];

        for (int i = 0; i < Strings.Count; i++)
        {
            if (Strings[i].Speaker == ScriptFileSpeaker.CHOICE.ToString())
            {
                int selectIndex = Array.IndexOf(allInvocations, allInvocations.First(v => v.LineNumber == dialogueParams[i].LineNumber));
                if (numChoicesPerInvocationIndex.ContainsKey(selectIndex))
                {
                    numChoicesPerInvocationIndex[selectIndex]++;
                }
                else
                {

                    if (!allInvocations[selectIndex].Command.Name.Contains('2'))
                    {
                        numChoicesPerInvocationIndex.Add(selectIndex, 0);
                    }
                    else
                    {
                        numChoicesPerInvocationIndex.Add(selectIndex, 1);
                    }
                }

                if (numChoicesPerInvocationIndex[selectIndex] == 0)
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
                    if (parameter.Type == ScriptCommand.ParameterType.INDEXED_ADDRESS)
                    {
                        currentAddressParameter++;
                        if (currentAddressParameter < numChoicesPerInvocationIndex[selectIndex])
                        {
                            continue;
                        }
                        else if (currentAddressParameter > numChoicesPerInvocationIndex[selectIndex])
                        {
                            break;
                        }

                        int targetLineNumber = allInvocations.First(v => v.Address == IO.ReadInt(parameter.Value, 0)).LineNumber;
                        for (int j = 0; j < dialogueParams.Length; j++)
                        {
                            if (dialogueParams[j].LineNumber >= targetLineNumber)
                            {
                                Strings[i].Metadata.Add($"VJUMPs to {j:D4}");
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    private void TagDialogueWithEventFileMetadata(short[] scriptEventFiles)
    {
        ScriptCommandInvocation[] allInvocations = ScriptCommandBlocks.SelectMany(b => b.Invocations).ToArray();

        for (int i = 0; i < allInvocations.Length; i++)
        {
            if (allInvocations[i].Command.Name == "EV_START")
            {
                int eventId = scriptEventFiles[int.Parse(allInvocations[i].CalculateIntParameter(Helpers.GetIntFromByteArray(allInvocations[i].Parameters.First(p => p.Type == ScriptCommand.ParameterType.INT).Value, 0),
                    Helpers.GetIntFromByteArray(allInvocations[i].Parameters.First(p => p.Type == ScriptCommand.ParameterType.INT).Value, 1))[4..])];
                List<int> chapters = [];
                byte[] chaptersParam = allInvocations[i].Parameters.FirstOrDefault(p => p.Type == ScriptCommand.ParameterType.INT_ARRAY)?.Value;
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
                for (int j = 0; j < Strings.Count; j++)
                {
                    int distanceBetweenDialogueLineAndEventStart = allInvocations.Where(inv => inv.Address == Strings[j].Offset)
                        .Select(inv => Math.Abs(inv.LineNumber - allInvocations[i].LineNumber)).FirstOrDefault();
                    if (distanceBetweenDialogueLineAndEventStart < minDistance)
                    {
                        minDistance = distanceBetweenDialogueLineAndEventStart;
                        minDistanceLine = j;
                    }
                }

                string beforeAfter = Strings[minDistanceLine].Offset > allInvocations[i].Address ? "before" : "after";
                string chaptersString = chapters.Count > 0 ? $" (ch {string.Join(", ", chapters)})" : "";
                Strings[minDistanceLine].Metadata.Add($"Event evt-{eventId:D4}{chaptersString} starts {beforeAfter}");
            }
        }
    }

    private void TagDialogueWithTopicMetadata()
    {
        ScriptCommandInvocation[] allInvocations = ScriptCommandBlocks.SelectMany(b => b.Invocations).ToArray();

        for (int i = 0; i < allInvocations.Length; i++)
        {
            if (allInvocations[i].Command.Name == "TOPIC" || allInvocations[i].Command.Name == "TOPIC_GET"
                                                          || allInvocations[i].Command.Name == "TOPIC_CHANGE" || allInvocations[i].Command.Name == "TOPIC_VANISH"
                                                          || allInvocations[i].Command.Name == "TOPIC_USING")
            {
                string topicName = Objects[IO.ReadShort(allInvocations[i].Parameters[0].Value, 0)];

                int minDistance = int.MaxValue;
                int minDistanceLine = 0;
                for (int j = 0; j < Strings.Count; j++)
                {
                    int distanceBetweenDialogueLineAndEventStart = allInvocations.Where(inv => inv.Address == Strings[j].Offset)
                        .Select(inv => Math.Abs(inv.LineNumber - allInvocations[i].LineNumber)).FirstOrDefault();
                    if (distanceBetweenDialogueLineAndEventStart < minDistance)
                    {
                        minDistance = distanceBetweenDialogueLineAndEventStart;
                        minDistanceLine = j;
                    }
                }

                string beforeAfter = Strings[minDistanceLine].Offset > allInvocations[i].Address ? "before" : "after";
                Strings[minDistanceLine].Metadata.Add($"{allInvocations[i].Command.Name}({topicName}) {beforeAfter} this line");
            }
        }
    }

    /// <summary>
    /// Parses the list of script files
    /// </summary>
    /// <param name="scriptListFileData">Script file list data</param>
    /// <returns></returns>
    public static List<string> ParseScriptListFile(byte[] scriptListFileData)
    {
        List<string> scriptList = [];

        int numScripts = IO.ReadInt(scriptListFileData, 0);

        for (int i = 0; i < numScripts; i++)
        {
            scriptList.Add(Encoding.ASCII.GetString(scriptListFileData.Skip(8 + i * 36).TakeWhile(b => b != 0).ToArray()));
        }

        return scriptList;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        if (Location != (-1, -1))
        {
            return $"{McbId:X4}/{Location.parent},{Location.child} {Name}";
        }
        else
        {
            return $"{BinArchiveIndex:X3} {BinArchiveIndex:D4} 0x{Offset:X8} {Name}";
        }
    }
}