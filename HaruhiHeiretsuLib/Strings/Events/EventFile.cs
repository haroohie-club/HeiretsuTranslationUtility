using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HaruhiHeiretsuLib.Strings.Events
{
    public class EventFile : StringsFile
    {
        public CutsceneData CutsceneData { get; set; }

        public EventFile()
        {
        }

        public override void Initialize(byte[] decompressedData, int offset)
        {
            Offset = offset;
            Data = decompressedData.ToList();

            InitializeInternal();
        }

        public EventFile(int parent, int child, byte[] data, ushort mcbId = 0)
        {
            Location = (parent, child);
            McbId = mcbId;
            Data = data.ToList();

            InitializeInternal();
        }

        private void InitializeInternal()
        {
            if (BitConverter.ToInt32(Data.Take(4).ToArray()) == 6)
            {
                CutsceneData = new CutsceneData(Data);

                ParseDialogue();
            }
        }

        public void ParseDialogue()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var matches = Regex.Matches(Encoding.ASCII.GetString(Data.ToArray()), VOICE_REGEX);
            foreach (Match match in matches)
            {
                Speaker speaker = DialogueLine.GetSpeaker(match.Groups["characterCode"].Value);
                string line = Encoding.GetEncoding("Shift-JIS").GetString(Data.Skip(match.Index + 32).TakeWhile(b => b != 0x00).ToArray());
                DialogueLines.Add(new DialogueLine { Offset = match.Index + 32, Line = line, Speaker = speaker });
            }
        }

        public override void EditDialogue(int index, string newLine)
        {
            (_, byte[] newLineData) = DialogueEditSetUp(index, newLine);

            Data.RemoveRange(DialogueLines[index].Offset, newLineData.Length);
            Data.InsertRange(DialogueLines[index].Offset, newLineData);
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
    }

    public class CutsceneData
    {
        public EventFileHeader Header { get; set; }
        public List<ModelDefinition> CharacterModelDefinitionTable { get; set; } = new List<ModelDefinition>();
        public List<ChapterDefinition> ChapterDefinitionTable { get; set; } = new List<ChapterDefinition>();

        public CutsceneData(IEnumerable<byte> data)
        {
            Header = new EventFileHeader(data.Take(0x40));
            for (int i = 0; i < Header.NumCharacters; i++)
            {
                CharacterModelDefinitionTable.Add(new ModelDefinition(data.Skip(Header.CharacterModelDefinitionOffset + i * 0x18).Take(0x18)));
                CharacterModelDefinitionTable.Last().Details = new ModelDefinitionDetails(data, CharacterModelDefinitionTable.Last().CharacterModelDataEntryOffset);
            }

            for (int i = 0; i < Header.ChaptersCount; i++)
            {
                ChapterDefinitionTable.Add(new ChapterDefinition(data, Header.ChapterDefTableOffset + i * 0x14));
            }
        }

        public List<byte> GetBytes()
        {
            var bytes = new List<byte>();

            bytes.AddRange(Header.GetBytes());

            return bytes;
        }
    }

    // 0x30 bytes
    public class EventFileHeader
    {
        public int Version { get; set; }
        public float TotalRuntimeInFrames { get; set; }
        public float Unknown08 { get; set; }
        public short Unknown0C { get; set; } // with two bytes of padding, though
        public float Unknown10 { get; set; }
        public short ChaptersCount { get; set; }
        public short Unknown16 { get; set; }
        public int ChapterDefTableOffset { get; set; }
        public int NumCharacters { get; set; }
        public int CharacterModelDefinitionOffset { get; set; }
        public short Unknown24 { get; set; }
        public short Unknown26 { get; set; }
        public int Unknown28 { get; set; }
        public int Unknown2C { get; set; }

        public EventFileHeader(IEnumerable<byte> data)
        {
            // Event files are little-endian, so no need to .Reverse
            Version = BitConverter.ToInt32(data.Take(4).ToArray());
            TotalRuntimeInFrames = BitConverter.ToSingle(data.Skip(0x04).Take(4).ToArray());
            Unknown08 = BitConverter.ToSingle(data.Skip(0x08).Take(4).ToArray());
            Unknown0C = BitConverter.ToInt16(data.Skip(0x0C).Take(4).ToArray());
            Unknown10 = BitConverter.ToSingle(data.Skip(0x10).Take(4).ToArray());
            ChaptersCount = BitConverter.ToInt16(data.Skip(0x14).Take(2).ToArray());
            Unknown16 = BitConverter.ToInt16(data.Skip(0x16).Take(2).ToArray());
            ChapterDefTableOffset = BitConverter.ToInt32(data.Skip(0x18).Take(4).ToArray());
            NumCharacters = BitConverter.ToInt32(data.Skip(0x1C).Take(4).ToArray());
            CharacterModelDefinitionOffset = BitConverter.ToInt32(data.Skip(0x20).Take(4).ToArray());
            Unknown24 = BitConverter.ToInt16(data.Skip(0x24).Take(2).ToArray());
            Unknown26 = BitConverter.ToInt16(data.Skip(0x26).Take(2).ToArray());
            Unknown28 = BitConverter.ToInt32(data.Skip(0x28).Take(4).ToArray());
            Unknown2C = BitConverter.ToInt32(data.Skip(0x2C).Take(4).ToArray());
        }

        public List<byte> GetBytes()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(Version));
            bytes.AddRange(BitConverter.GetBytes(TotalRuntimeInFrames));
            bytes.AddRange(BitConverter.GetBytes(Unknown08));
            bytes.AddRange(BitConverter.GetBytes(Unknown0C));
            bytes.AddRange(BitConverter.GetBytes(Unknown10));
            bytes.AddRange(BitConverter.GetBytes(ChaptersCount));
            bytes.AddRange(BitConverter.GetBytes(Unknown16));
            bytes.AddRange(BitConverter.GetBytes(ChapterDefTableOffset));
            bytes.AddRange(BitConverter.GetBytes(NumCharacters));
            bytes.AddRange(BitConverter.GetBytes(CharacterModelDefinitionOffset));
            bytes.AddRange(BitConverter.GetBytes(Unknown24));
            bytes.AddRange(BitConverter.GetBytes(Unknown26));
            bytes.AddRange(BitConverter.GetBytes(Unknown28));
            bytes.AddRange(BitConverter.GetBytes(Unknown2C));

            return bytes;
        }
    }
}
