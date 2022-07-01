using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HaruhiHeiretsuLib.Strings
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
                CutsceneData = new(Data);

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
                return CutsceneData.GetBytes().ToArray();
            }
        }
    }

    public class CutsceneData
    {
        public EventFileHeader Header { get; set; }
        public List<CharacterModelDefinitionEntry> CharacterModelDefinitionTable { get; set; } = new();
        public List<ChapterDefinition> ChapterDefinitionTable { get; set; } = new();

        public CutsceneData(IEnumerable<byte> data)
        {
            Header = new(data.Take(0x40));
            for (int i = 0; i < Header.NumCharacters; i++)
            {
                CharacterModelDefinitionTable.Add(new(data.Skip(Header.CharacterModelDefinitionOffset + i * 0x18).Take(0x18)));
                CharacterModelDefinitionTable.Last().Details = new(data, CharacterModelDefinitionTable.Last().CharacterModelDataEntryOffset);
            }

            for (int i = 0; i < Header.ChaptersCount; i++)
            {
                ChapterDefinitionTable.Add(new(data, Header.ChapterDefTableOffset + i * 0x14));
            }
        }

        public List<byte> GetBytes()
        {
            List<byte> bytes = new();

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
            List<byte> bytes = new();

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

    // 0x18 bytes
    public class CharacterModelDefinitionEntry
    {
        public string CharacterModelName { get; set; }
        public short Unknown10 { get; set; }
        public short Unknown12 { get; set; }
        public int CharacterModelDataEntryOffset { get; set; }
        public CharacterModelDefinitionDetailsEntry Details { get; set; }

        public CharacterModelDefinitionEntry(IEnumerable<byte> data)
        {
            byte[] stringData = data.TakeWhile(b => b != 0x00).ToArray();
            if (stringData.Length > 0x10)
            {
                CharacterModelName = Encoding.ASCII.GetString(data.Take(0x10).ToArray());
            }
            else
            {
                CharacterModelName = Encoding.ASCII.GetString(stringData);
            }
            Unknown10 = BitConverter.ToInt16(data.Skip(0x10).Take(2).ToArray());
            Unknown12 = BitConverter.ToInt16(data.Skip(0x12).Take(2).ToArray());
            CharacterModelDataEntryOffset = BitConverter.ToInt32(data.Skip(0x14).Take(4).ToArray());
        }
    }

    // Variable number of bytes
    public class CharacterModelDefinitionDetailsEntry
    {
        public int Version { get; set; } // 0x00
        public int Unknown10 { get; set; } // 0x10
        public int Unknown5C { get; set; } // 0x5C
        public int Unknown60 { get; set; } // 0x60
        public int Unknown64 { get; set; } // 0x64

        public int UnknownPointer1 { get; set; } // *Unknown64 + 0
        public int UnknownPointer2 { get; set; } // *Unknown64 + 4
        public int UnknownPointer3 { get; set; } // *Unknown64 + 8
        public int CharacterDetailsSubEntriesOffset { get; set; } // *Unknown64 + 12
        public short UnknownShort1 { get; set; } // *Unknown64 + 16
        public short UnknownShort2 { get; set; } // *Unknown64 + 18
        public short UnknownShort3 { get; set; } // *Unknown64 + 20
        public short CharacterDetailsSubEntryCount { get; set; } // *Unknown64 + 22
        public List<CharacterDetailsSubEntry> CharacterDetailsSubEntries { get; set; } = new();

        public CharacterModelDefinitionDetailsEntry(IEnumerable<byte> data, int offset)
        {
            Version = BitConverter.ToInt32(data.Skip(offset).Take(4).ToArray());
            Unknown10 = BitConverter.ToInt32(data.Skip(offset + 0x10).Take(4).ToArray());
            Unknown5C = BitConverter.ToInt32(data.Skip(offset + 0x5C).Take(4).ToArray());
            Unknown60 = BitConverter.ToInt32(data.Skip(offset + 0x60).Take(4).ToArray());
            Unknown64 = BitConverter.ToInt32(data.Skip(offset + 0x64).Take(4).ToArray());

            UnknownPointer1 = BitConverter.ToInt32(data.Skip(Unknown64).Take(4).ToArray());
            UnknownPointer2 = BitConverter.ToInt32(data.Skip(Unknown64 + 0x04).Take(4).ToArray());
            UnknownPointer3 = BitConverter.ToInt32(data.Skip(Unknown64 + 0x08).Take(4).ToArray());
            CharacterDetailsSubEntriesOffset = BitConverter.ToInt32(data.Skip(Unknown64 + 0x0C).Take(4).ToArray());
            UnknownShort1 = BitConverter.ToInt16(data.Skip(Unknown64 + 0x10).Take(2).ToArray());
            UnknownShort2 = BitConverter.ToInt16(data.Skip(Unknown64 + 0x12).Take(2).ToArray());
            UnknownShort3 = BitConverter.ToInt16(data.Skip(Unknown64 + 0x14).Take(2).ToArray());
            CharacterDetailsSubEntryCount = BitConverter.ToInt16(data.Skip(Unknown64 + 0x16).Take(2).ToArray());

            for (int i = 0; i < CharacterDetailsSubEntryCount; i++)
            {
                CharacterDetailsSubEntries.Add(new(data.Skip(CharacterDetailsSubEntriesOffset + i * 0x28).Take(0x28)));
            }
        }

        // 0x28 bytes
        public class CharacterDetailsSubEntry
        {
            public float Unknown00 { get; set; }
            public float Unknown04 { get; set; }
            public ushort Unknown08 { get; set; }
            public ushort Unknown0A { get; set; }
            public int Unknown0C { get; set; }
            public ushort Unknown10 { get; set; }
            public ushort Unknown12 { get; set; }
            public int Unknown14 { get; set; }
            public int Unknown18 { get; set; }
            public int Unknown1C { get; set; } // Pointer
            public int Unknown20 { get; set; } // Pointer
            public int Unknown24 { get; set; } // Pointer

            public CharacterDetailsSubEntry(IEnumerable<byte> data)
            {
                Unknown00 = BitConverter.ToSingle(data.Take(4).ToArray());
                Unknown04 = BitConverter.ToSingle(data.Skip(0x04).Take(4).ToArray());
                Unknown08 = BitConverter.ToUInt16(data.Skip(0x08).Take(2).ToArray());
                Unknown0A = BitConverter.ToUInt16(data.Skip(0x0A).Take(2).ToArray());
                Unknown0C = BitConverter.ToInt32(data.Skip(0x0C).Take(4).ToArray());
                Unknown10 = BitConverter.ToUInt16(data.Skip(0x10).Take(2).ToArray());
                Unknown12 = BitConverter.ToUInt16(data.Skip(0x12).Take(2).ToArray());
                Unknown14 = BitConverter.ToInt32(data.Skip(0x14).Take(4).ToArray());
                Unknown18 = BitConverter.ToInt32(data.Skip(0x18).Take(4).ToArray());
                Unknown1C = BitConverter.ToInt32(data.Skip(0x1C).Take(4).ToArray());
                Unknown20 = BitConverter.ToInt32(data.Skip(0x20).Take(4).ToArray());
                Unknown24 = BitConverter.ToInt32(data.Skip(0x24).Take(4).ToArray());
            }
        }
    }

    // 0x14 bytes
    public class ChapterDefinition
    {
        public float StartTime { get; set; }
        public float EndTime { get; set; }
        public int Unknown08 { get; set; }
        public ushort ActorDefTableEntryCount { get; set; }
        public int ActorDefTableOffset { get; set; }
        public List<ActorDefinition> ActorDefinitionTable { get; set; } = new();

        public ChapterDefinition(IEnumerable<byte> data, int offset)
        {
            StartTime = BitConverter.ToSingle(data.Skip(offset).Take(4).ToArray());
            EndTime = BitConverter.ToSingle(data.Skip(offset + 0x04).Take(4).ToArray());
            Unknown08 = BitConverter.ToInt32(data.Skip(offset + 0x08).Take(4).ToArray());
            ActorDefTableEntryCount = BitConverter.ToUInt16(data.Skip(offset + 0x0C).Take(2).ToArray());
            ActorDefTableOffset = BitConverter.ToInt32(data.Skip(offset + 0x10).Take(4).ToArray());

            for (int i = 0; i < ActorDefTableEntryCount; i++)
            {
                ActorDefinitionTable.Add(new(data, ActorDefTableOffset + i * 0x3C));
            }
        }
    }

    // 0x3C bytes (0x20 bytes of padding)
    public class ActorDefinition
    {
        public int ChapterDefinitionOffset { get; set; }
        public short Unknown04 { get; set; }
        public string ModelName { get; set; }
        public short ActionsCount { get; set; }
        public int ActionsTableAddress { get; set; }
        public List<ActionsTableEntry> ActionsTable { get; set; } = new();

        public ActorDefinition(IEnumerable<byte> data, int offset)
        {
            ChapterDefinitionOffset = BitConverter.ToInt32(data.Skip(offset).Take(4).ToArray());
            Unknown04 = BitConverter.ToInt16(data.Skip(offset + 0x04).Take(2).ToArray());
            IEnumerable<byte> nameBytes = data.Skip(offset + 0x06).TakeWhile(b => b != 0x00);
            if (nameBytes.Count() > 0x10)
            {
                ModelName = Encoding.ASCII.GetString(nameBytes.Take(offset + 0x10).ToArray());
            }
            else
            {
                ModelName = Encoding.ASCII.GetString(nameBytes.ToArray());
            }
            ActionsCount = BitConverter.ToInt16(data.Skip(offset + 0x16).Take(2).ToArray());
            ActionsTableAddress = BitConverter.ToInt32(data.Skip(offset + 0x18).Take(4).ToArray());

            for (int i = 0; i < ActionsCount; i++)
            {
                ActionsTable.Add(new(data, ActionsTableAddress + 0x38 * i));
            }
        }
    }

    public class ActionsTableEntry
    {
        public int ActorDefinitionAddress { get; set; }
        public ushort OpCode { get; set; }
        public ushort ParametersCount { get; set; }
        public int ParametersAddress { get; set; }
        List<ActionParameter> Parameters { get; set; } = new();

        public ActionsTableEntry(IEnumerable<byte> data, int offset)
        {
            ActorDefinitionAddress = BitConverter.ToInt32(data.Skip(offset).Take(4).ToArray());
            OpCode = BitConverter.ToUInt16(data.Skip(offset + 0x04).Take(2).ToArray());
            ParametersCount = BitConverter.ToUInt16(data.Skip(offset + 0x08).Take(2).ToArray());
            ParametersAddress = BitConverter.ToInt32(data.Skip(offset + 0x0C).Take(4).ToArray());

        }
    }

    public class ActionParameter
    {

    }
}
