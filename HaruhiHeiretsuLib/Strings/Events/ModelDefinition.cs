using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiHeiretsuLib.Strings.Events
{
    // 0x18 bytes
    public class ModelDefinition
    {
        public string CharacterModelName { get; set; }
        public short Unknown10 { get; set; }
        public short Unknown12 { get; set; }
        public int CharacterModelDataEntryOffset { get; set; }
        public ModelDefinitionDetails Details { get; set; }

        public ModelDefinition(IEnumerable<byte> data)
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
    public class ModelDefinitionDetails
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
        public List<ModelDetailsSubEntry> CharacterDetailsSubEntries { get; set; } = new();

        public ModelDefinitionDetails(IEnumerable<byte> data, int offset)
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
        public class ModelDetailsSubEntry
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

            public ModelDetailsSubEntry(IEnumerable<byte> data)
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
}
