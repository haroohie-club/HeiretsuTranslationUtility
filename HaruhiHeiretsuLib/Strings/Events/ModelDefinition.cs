using HaruhiHeiretsuLib.Graphics;
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
        public int Unknown38TableCount { get; set; } // 0x04
        public int Unknown08 { get; set; } // 0x08
        public int Unknown0C { get; set; } // 0x0C
        public int NumBones { get; set; } // 0x10
        public int Unknown14 { get; set; } // 0x14
        public int Unknown18 { get; set; } // 0x18
        public int Unknown1C { get; set; } // 0x1C
        public int Unknown20 { get; set; } // 0x20
        public int Unknown24 { get; set; } // 0x24
        public int Unknown28 { get; set; } // 0x28
        public int Unknown2C { get; set; } // 0x2C
        public int Unknown38TableOffset { get; set; } // 0x38
        public int Unknown3C { get; set; } // 0x3C
        public int Unknown40 { get; set; } // 0x40
        public int Unknown44 { get; set; } // 0x40
        public int Unknown48 { get; set; } // 0x48
        public int Unknown4C { get; set; } // 0x4C
        public int Unknown50 { get; set; } // 0x4C
        public int Unknown54 { get; set; } // 0x4C
        public int Unknown58 { get; set; } // 0x4C
        public int NumAnimations { get; set; } // 0x5C
        public int AnimDefinitionsOffset { get; set; } // 0x60
        public int AnimTransformDataOffset { get; set; } // 0x64

        public int TranslateDataOffset { get; set; } // *AnimTransformDataOffset + 0
        public int RotateDataOffset { get; set; } // *AnimTransformDataOffset + 4
        public int ScaleDataOffset { get; set; } // *AnimTransformDataOffset + 8
        public int KeyframeDefinitionsOffset { get; set; } // *AnimTransformDataOffset + 12
        public short TranslateDataCount { get; set; } // *AnimTransformDataOffset + 16
        public short RotateDataCount { get; set; } // *AnimTransformDataOffset + 18
        public short ScaleDataCount { get; set; } // *AnimTransformDataOffset + 20
        public short NumKeyframes { get; set; } // *AnimTransformDataOffset + 22
        public List<SgeAnimation> AnimationDefinitions { get; set; } = [];
        public List<Unknown38Entry> Unknown38Table { get; set; } = [];
        public List<TranslateDataEntry> TranslateDataEntries { get; set; } = [];
        public List<RotateDataEntry> RotateDataEntries { get; set; } = [];
        public List<ScaleDataEntry> ScaleDataEntries { get; set; } = [];
        public List<KeyframeDefinition> KeyframeDefinitions { get; set; } = [];

        // Truncated version of an SGE header
        public ModelDefinitionDetails(IEnumerable<byte> data, int offset)
        {
            Version = BitConverter.ToInt32(data.Skip(offset).Take(4).ToArray());
            Unknown38TableCount = BitConverter.ToInt32(data.Skip(offset + 0x04).Take(4).ToArray());
            Unknown08 = BitConverter.ToInt32(data.Skip(offset + 0x08).Take(4).ToArray());
            Unknown0C = BitConverter.ToInt32(data.Skip(offset + 0x0C).Take(4).ToArray());
            NumBones = BitConverter.ToInt32(data.Skip(offset + 0x10).Take(4).ToArray());
            Unknown14 = BitConverter.ToInt32(data.Skip(offset + 0x14).Take(4).ToArray());
            Unknown18 = BitConverter.ToInt32(data.Skip(offset + 0x18).Take(4).ToArray());
            Unknown1C = BitConverter.ToInt32(data.Skip(offset + 0x1C).Take(4).ToArray());
            Unknown20 = BitConverter.ToInt32(data.Skip(offset + 0x20).Take(4).ToArray());
            Unknown24 = BitConverter.ToInt32(data.Skip(offset + 0x24).Take(4).ToArray());
            Unknown28 = BitConverter.ToInt32(data.Skip(offset + 0x28).Take(4).ToArray());
            Unknown2C = BitConverter.ToInt32(data.Skip(offset + 0x2C).Take(4).ToArray());
            Unknown38TableOffset = BitConverter.ToInt32(data.Skip(offset + 0x38).Take(4).ToArray());
            Unknown3C = BitConverter.ToInt32(data.Skip(offset + 0x3C).Take(4).ToArray());
            Unknown40 = BitConverter.ToInt32(data.Skip(offset + 0x40).Take(4).ToArray());
            Unknown44 = BitConverter.ToInt32(data.Skip(offset + 0x44).Take(4).ToArray());
            Unknown48 = BitConverter.ToInt32(data.Skip(offset + 0x48).Take(4).ToArray());
            Unknown4C = BitConverter.ToInt32(data.Skip(offset + 0x4C).Take(4).ToArray());
            Unknown50 = BitConverter.ToInt32(data.Skip(offset + 0x50).Take(4).ToArray());
            Unknown54 = BitConverter.ToInt32(data.Skip(offset + 0x54).Take(4).ToArray());
            Unknown58 = BitConverter.ToInt32(data.Skip(offset + 0x58).Take(4).ToArray());
            NumAnimations = BitConverter.ToInt32(data.Skip(offset + 0x5C).Take(4).ToArray());
            AnimDefinitionsOffset = BitConverter.ToInt32(data.Skip(offset + 0x60).Take(4).ToArray());
            AnimTransformDataOffset = BitConverter.ToInt32(data.Skip(offset + 0x64).Take(4).ToArray());

            TranslateDataOffset = BitConverter.ToInt32(data.Skip(AnimTransformDataOffset).Take(4).ToArray());
            RotateDataOffset = BitConverter.ToInt32(data.Skip(AnimTransformDataOffset + 0x04).Take(4).ToArray());
            ScaleDataOffset = BitConverter.ToInt32(data.Skip(AnimTransformDataOffset + 0x08).Take(4).ToArray());
            KeyframeDefinitionsOffset = BitConverter.ToInt32(data.Skip(AnimTransformDataOffset + 0x0C).Take(4).ToArray());
            TranslateDataCount = BitConverter.ToInt16(data.Skip(AnimTransformDataOffset + 0x10).Take(2).ToArray());
            RotateDataCount = BitConverter.ToInt16(data.Skip(AnimTransformDataOffset + 0x12).Take(2).ToArray());
            ScaleDataCount = BitConverter.ToInt16(data.Skip(AnimTransformDataOffset + 0x14).Take(2).ToArray());
            NumKeyframes = BitConverter.ToInt16(data.Skip(AnimTransformDataOffset + 0x16).Take(2).ToArray());

            for (int i = 0; i < NumAnimations; i++)
            {
                AnimationDefinitions.Add(new(data, offset, NumBones, AnimDefinitionsOffset + i * 0x38));
            }

            for (int i = 0; i < Unknown38TableCount; i++)
            {
                Unknown38Table.Add(new(data.Skip(Unknown38TableOffset + i * 0x48).Take(0x48)));
            }

            for (int i = 0; i < TranslateDataCount; i++)
            {
                TranslateDataEntries.Add(new(data.Skip(TranslateDataOffset + i * 0x0C).Take(0x0C)));
            }

            for (int i = 0; i < RotateDataCount; i++)
            {
                RotateDataEntries.Add(new(data.Skip(RotateDataOffset + i * 0x10).Take(0x10)));
            }

            for (int i = 0; i < ScaleDataCount; i++)
            {
                ScaleDataEntries.Add(new(data.Skip(ScaleDataOffset + i * 0x0C).Take(0x0C)));
            }

            for (int i = 0; i < NumKeyframes; i++)
            {
                KeyframeDefinitions.Add(new(data.Skip(KeyframeDefinitionsOffset + i * 0x28).Take(0x28)));
            }
        }
    }
}
