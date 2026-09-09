using HaruhiHeiretsuLib.Strings.Events.Parameters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources.NetStandard;
using System.Text;
using HaruhiHeiretsuLib.Util;

namespace HaruhiHeiretsuLib.Strings.Events;

/// <summary>
/// A cutscene (event) file in evt.bin
/// </summary>
public class EventFile : StringsFile
{
    /// <summary>
    /// The cutscene data associated with the event
    /// </summary>
    public CutsceneData CutsceneData { get; set; }

    /// <summary>
    /// Empty constructor for serialization
    /// </summary>
    public EventFile()
    {
    }

    /// <summary>
    /// Maximum dialogue line length
    /// </summary>
    public static readonly int[] DialogueLineLength = [7038, 7038];

    /// <inheritdoc/>
    public override void Initialize(byte[] decompressedData, int offset)
    {
        Offset = offset;
        Data = [.. decompressedData];

        InitializeInternal();
    }

    /// <summary>
    /// Constructs an event file from binary MCB data
    /// </summary>
    /// <param name="parent">The MCB parent archive</param>
    /// <param name="child">The MCB child archive</param>
    /// <param name="data">The binary data of the file</param>
    /// <param name="mcbId">The ID of the archive</param>
    public EventFile(int parent, int child, byte[] data, ushort mcbId = 0)
    {
        Location = (parent, child);
        McbId = mcbId;
        Data = [.. data];

        InitializeInternal();
    }

    private void InitializeInternal()
    {
        if (BitConverter.ToInt32(Data.Take(4).ToArray()) == 6)
        {
            CutsceneData = new(Data.ToArray());

            ParseDialogue();
        }
    }

    private void ParseDialogue()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        IEnumerable<(ChapterDefinition, IEnumerable<DialogueParameter>)> parameters = CutsceneData.ChapterDefinitionTable
            .Select(c => (c, c.ActorDefinitionTable
                .SelectMany(d => d.ActionsTable
                    .SelectMany(a => a.Parameters
                        .Where(p => p.GetType() == typeof(DialogueParameter))
                        .Select(p => (DialogueParameter)p)))));

        int i = 0;
        foreach ((ChapterDefinition chapter, IEnumerable<DialogueParameter> dialogue) in parameters)
        {
            int j = 0;
            foreach (DialogueParameter parameter in dialogue)
            {
                Strings.Add(new() { Line = parameter.Dialogue.Replace("\\n", "\n"), Speaker = parameter.SpeakingCharacter.ToString(), Offset = parameter.Address + 0x50 });
                if (j == 0)
                {
                    Strings.Last().Metadata.Add($"Chapter {i} Start");
                }
                Strings.Last().Metadata.Add(parameter.VoiceFile);
                j++;
            }
            i++;
        }
    }

    // TODO: Return real cutscene data
    /// <inheritdoc/>
    public override byte[] GetBytes()
    {
        if (CutsceneData is null)
        {
            return base.GetBytes();
        }
        else
        {
            return base.GetBytes();
            //return CutsceneData.GetBytes().ToArray();
        }
    }

    /// <inheritdoc/>
    public override void EditString(int index, string newString)
    {
        newString = newString.Replace("\n", "\\n");
        (_, byte[] newLineData) = StringEditSetUp(index, newString);

        if (newLineData.Length < Strings[index].Length)
        {
            List<byte> temp = [.. newLineData, .. new byte[Strings[index].Length - newLineData.Length]];
            newLineData = [.. temp];
        }

        if (newLineData.Length > 0x80)
        {
            newLineData = newLineData.Take(0x80).ToArray();
        }

        Data.RemoveRange(Strings[index].Offset, newLineData.Length);
        Data.InsertRange(Strings[index].Offset, newLineData);
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
            string dialogueText = ProcessDialogueLineWithFontReplacement(NormalizeDialogueLine((string)d.Value), fontReplacementMap, DialogueLineLength);

            if (dialogueText.Count(c => c == '\n') > 1)
            {
                Console.WriteLine($"Warning: file evt-{BinArchiveIndex} has line too long: {dialogueIndex} (starts with '{dialogueText[..30]}')");
            }

            EditString(dialogueIndex, dialogueText);
        }
    }
}

/// <summary>
/// Cutscene data in an event file
/// </summary>
public class CutsceneData
{
    /// <summary>
    /// Event file header data
    /// </summary>
    public EventFileHeader Header { get; set; }
    /// <summary>
    /// Character model definitions
    /// </summary>
    public List<ModelDefinition> CharacterModelDefinitionTable { get; set; } = [];
    /// <summary>
    /// Chapter definitions
    /// </summary>
    public List<ChapterDefinition> ChapterDefinitionTable { get; set; } = [];

    /// <summary>
    /// Constructs cutscene data from raw binary
    /// </summary>
    /// <param name="data">The full raw binary contained in the event file</param>
    public CutsceneData(byte[] data)
    {
        Header = new( data[..0x40]);
        for (int i = 0; i < Header.NumActors; i++)
        {
            CharacterModelDefinitionTable.Add(new(data.Skip(Header.ActorModelDefinitionOffset + i * 0x18).Take(0x18)));
            if (CharacterModelDefinitionTable.Last().CharacterModelDataEntryOffset > 0)
            {
                CharacterModelDefinitionTable.Last().Details = new(data, CharacterModelDefinitionTable.Last().CharacterModelDataEntryOffset);
            }
        }

        for (int i = 0; i < Header.ChaptersCount; i++)
        {
            ChapterDefinitionTable.Add(new(data, Header.ChapterDefTableOffset + i * 0x14));
        }
    }

    /// <summary>
    /// Gets raw binary data of the cutscene
    /// </summary>
    /// <returns>A list of bytes (the raw binary)</returns>
    public List<byte> GetBytes()
    {
        List<byte> bytes = [.. Header.GetBytes()];

        return bytes;
    }
}

// 0x40 bytes
/// <summary>
/// The event file header
/// </summary>
public class EventFileHeader
{
    /// <summary>
    /// Version (always 6)
    /// </summary>
    public int Version { get; set; }
    /// <summary>
    /// Total runtime in frames (assumes 60fps)
    /// </summary>
    public float TotalRuntimeInFrames { get; set; }
    /// <summary>
    /// The current frame when the cutscene is running
    /// </summary>
    public float CurrentFrame { get; set; }
    /// <summary>
    /// The current chapter when the cutscene is running
    /// </summary>
    public short CurrentChapter { get; set; } // with two bytes of padding
    /// <summary>
    /// Unknown
    /// </summary>
    public float Unknown10 { get; set; }
    /// <summary>
    /// Number of chapters in the cutscene
    /// </summary>
    public short ChaptersCount { get; set; }
    /// <summary>
    /// Padding
    /// </summary>
    public short Padding16 { get; set; }
    /// <summary>
    /// Offset of the chapter definition table
    /// </summary>
    public int ChapterDefTableOffset { get; set; }
    /// <summary>
    /// Number of actors in the scene
    /// </summary>
    public short NumActors { get; set; } // with two bytes of padding
    /// <summary>
    /// Offset of the actor model definitions
    /// </summary>
    public int ActorModelDefinitionOffset { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public short Unknown24 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public short Unknown26 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown28 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown2C { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown30 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown34 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown38 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown3C { get; set; }

    /// <summary>
    /// Constructs an event file header from binary data
    /// </summary>
    /// <param name="data">Binary data representation of the event file header</param>
    public EventFileHeader(byte[] data)
    {
        // Event files are little-endian, so no need to .Reverse
        Version = IO.ReadIntLE(data, 0);
        TotalRuntimeInFrames = IO.ReadFloatLE(data, 0x04);
        CurrentFrame = IO.ReadFloatLE(data, 0x08);
        CurrentChapter = IO.ReadShortLE(data, 0x0E); // With two bytes of padding
        Unknown10 = IO.ReadFloatLE(data, 0x10);
        ChaptersCount = IO.ReadShortLE(data, 0x14);
        Padding16 = IO.ReadShortLE(data, 0x16);
        ChapterDefTableOffset = IO.ReadIntLE(data, 0x18);
        NumActors = IO.ReadShortLE(data, 0x1C);
        ActorModelDefinitionOffset = IO.ReadIntLE(data, 0x20);
        Unknown24 = IO.ReadShortLE(data, 0x24);
        Unknown26 = IO.ReadShortLE(data, 0x26);
        Unknown28 = IO.ReadIntLE(data, 0x28);
        Unknown2C = IO.ReadIntLE(data, 0x2C);
    }

    /// <summary>
    /// Gets the binary representation of the cutscene header
    /// </summary>
    /// <returns>A list of bytes (the binary data)</returns>
    public List<byte> GetBytes()
    {
        List<byte> bytes =
        [
            .. BitConverter.GetBytes(Version),
            .. BitConverter.GetBytes(TotalRuntimeInFrames),
            .. BitConverter.GetBytes(CurrentFrame),
            .. BitConverter.GetBytes(CurrentChapter),
            .. BitConverter.GetBytes((short)0),
            .. BitConverter.GetBytes(Unknown10),
            .. BitConverter.GetBytes(ChaptersCount),
            .. BitConverter.GetBytes(Padding16),
            .. BitConverter.GetBytes(ChapterDefTableOffset),
            .. BitConverter.GetBytes(NumActors),
            .. BitConverter.GetBytes((short)0),
            .. BitConverter.GetBytes(ActorModelDefinitionOffset),
            .. BitConverter.GetBytes(Unknown24),
            .. BitConverter.GetBytes(Unknown26),
            .. BitConverter.GetBytes(Unknown28),
            .. BitConverter.GetBytes(Unknown2C),
            .. BitConverter.GetBytes(Unknown30),
            .. BitConverter.GetBytes(Unknown34),
            .. BitConverter.GetBytes(Unknown38),
            .. BitConverter.GetBytes(Unknown3C),
        ];

        return bytes;
    }
}