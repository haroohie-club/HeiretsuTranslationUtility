using HaruhiHeiretsuLib.Strings.Events.Parameters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources.NetStandard;
using System.Text;

namespace HaruhiHeiretsuLib.Strings.Events
{
    public class EventFile : StringsFile
    {
        public CutsceneData CutsceneData { get; set; }

        public EventFile()
        {
        }

        public static int[] DIALOGUE_LINE_LENGTH = new int[] { 7038, 7038 };

        public override void Initialize(byte[] decompressedData, int offset)
        {
            Offset = offset;
            Data = [.. decompressedData];

            InitializeInternal();
        }

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
                CutsceneData = new(Data);

                ParseDialogue();
            }
        }

        public void ParseDialogue()
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
                    DialogueLines.Add(new() { Line = parameter.Dialogue.Replace("\\n", "\n"), Speaker = parameter.SpeakingCharacter.ToString(), Offset = parameter.Address + 0x50 });
                    if (j == 0)
                    {
                        DialogueLines.Last().Metadata.Add($"Chapter {i} Start");
                    }
                    DialogueLines.Last().Metadata.Add(parameter.VoiceFile);
                    j++;
                }
                i++;
            }
        }

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

        public override void EditDialogue(int index, string newLine)
        {
            newLine = newLine.Replace("\n", "\\n");
            (_, byte[] newLineData) = DialogueEditSetUp(index, newLine);

            if (newLineData.Length < DialogueLines[index].Length)
            {
                List<byte> temp = [.. newLineData, .. new byte[DialogueLines[index].Length - newLineData.Length]];
                newLineData = [.. temp];
            }

            if (newLineData.Length > 0x80)
            {
                newLineData = newLineData.Take(0x80).ToArray();
            }

            Data.RemoveRange(DialogueLines[index].Offset, newLineData.Length);
            Data.InsertRange(DialogueLines[index].Offset, newLineData);
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

                if (dialogueText.Count(c => c == '\n') > 1)
                {
                    Console.WriteLine($"Warning: file evt-{Index} has line too long: {dialogueIndex} (starts with '{dialogueText[0..30]}')");
                }

                EditDialogue(dialogueIndex, dialogueText);
            }
        }
    }

    public class CutsceneData
    {
        public EventFileHeader Header { get; set; }
        public List<ModelDefinition> CharacterModelDefinitionTable { get; set; } = [];
        public List<ChapterDefinition> ChapterDefinitionTable { get; set; } = [];

        public CutsceneData(IEnumerable<byte> data)
        {
            Header = new(data.Take(0x40));
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

        public List<byte> GetBytes()
        {
            List<byte> bytes = [.. Header.GetBytes()];

            return bytes;
        }
    }

    // 0x40 bytes
    public class EventFileHeader
    {
        public int Version { get; set; }
        public float TotalRuntimeInFrames { get; set; }
        public float CurrentFrame { get; set; }
        public short CurrentChapter { get; set; } // with two bytes of padding
        public float Unknown10 { get; set; }
        public short ChaptersCount { get; set; }
        public short Padding16 { get; set; }
        public int ChapterDefTableOffset { get; set; }
        public short NumActors { get; set; } // with two bytes of padding
        public int ActorModelDefinitionOffset { get; set; }
        public short Unknown24 { get; set; }
        public short Unknown26 { get; set; }
        public int Unknown28 { get; set; }
        public int Unknown2C { get; set; }
        public int Unknown30 { get; set; }
        public int Unknown34 { get; set; }
        public int Unknown38 { get; set; }
        public int Unknown3C { get; set; }

        public EventFileHeader(IEnumerable<byte> data)
        {
            // Event files are little-endian, so no need to .Reverse
            Version = BitConverter.ToInt32(data.Take(4).ToArray());
            TotalRuntimeInFrames = BitConverter.ToSingle(data.Skip(0x04).Take(4).ToArray());
            CurrentFrame = BitConverter.ToSingle(data.Skip(0x08).Take(4).ToArray());
            CurrentChapter = BitConverter.ToInt16(data.Skip(0x0C).Take(4).ToArray()); // With two bytes of padding
            Unknown10 = BitConverter.ToSingle(data.Skip(0x10).Take(4).ToArray());
            ChaptersCount = BitConverter.ToInt16(data.Skip(0x14).Take(2).ToArray());
            Padding16 = BitConverter.ToInt16(data.Skip(0x16).Take(2).ToArray());
            ChapterDefTableOffset = BitConverter.ToInt32(data.Skip(0x18).Take(4).ToArray());
            NumActors = BitConverter.ToInt16(data.Skip(0x1C).Take(2).ToArray());
            ActorModelDefinitionOffset = BitConverter.ToInt32(data.Skip(0x20).Take(4).ToArray());
            Unknown24 = BitConverter.ToInt16(data.Skip(0x24).Take(2).ToArray());
            Unknown26 = BitConverter.ToInt16(data.Skip(0x26).Take(2).ToArray());
            Unknown28 = BitConverter.ToInt32(data.Skip(0x28).Take(4).ToArray());
            Unknown2C = BitConverter.ToInt32(data.Skip(0x2C).Take(4).ToArray());
        }

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
}
