using HaruhiHeiretsuLib.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiHeiretsuLib.Strings.Events;

// 0x18 bytes
/// <summary>
/// SGE model definition (used in event files)
/// </summary>
public class ModelDefinition
{
    private readonly List<byte> _data;
    
    /// <summary>
    /// Character model name
    /// </summary>
    public string CharacterModelName { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public short Unknown10 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public short Unknown12 { get; set; }
    /// <summary>
    /// Character model data entry offset
    /// </summary>
    public int CharacterModelDataEntryOffset { get; set; }
    /// <summary>
    /// Model definition details
    /// </summary>
    public ModelDefinitionDetails Details { get; set; }

    /// <summary>
    /// Constructs a model definition
    /// </summary>
    /// <param name="data"></param>
    public ModelDefinition(IEnumerable<byte> data)
    {
        _data = [.. data];
        byte[] stringData = [.. _data.TakeWhile(b => b != 0x00)];
        CharacterModelName = Encoding.ASCII.GetString(stringData.Length > 0x10 ? [.. _data.Take(0x10)] : stringData);
        Unknown10 = BitConverter.ToInt16([.. _data.Skip(0x10).Take(2)]);
        Unknown12 = BitConverter.ToInt16([.. _data.Skip(0x12).Take(2)]);
        CharacterModelDataEntryOffset = BitConverter.ToInt32([.. _data.Skip(0x14).Take(4)]);
    }

    /// <summary>
    /// Gets binary data
    /// TODO: construct rather than just returning data
    /// </summary>
    /// <returns>The list of bytes (binary data)</returns>
    public List<byte> GetBytes()
    {
        return _data;
    }
}

// Variable number of bytes
/// <summary>
/// Truncated version of an SGE header
/// </summary>
public class ModelDefinitionDetails
{
    /// <summary>
    /// Version
    /// </summary>
    public int Version { get; set; } // 0x00
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown38TableCount { get; set; } // 0x04
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown08 { get; set; } // 0x08
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown0C { get; set; } // 0x0C
    /// <summary>
    /// Number of bones
    /// </summary>
    public int NumBones { get; set; } // 0x10
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown14 { get; set; } // 0x14
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown18 { get; set; } // 0x18
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown1C { get; set; } // 0x1C
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown20 { get; set; } // 0x20
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown24 { get; set; } // 0x24
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown28 { get; set; } // 0x28
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown2C { get; set; } // 0x2C
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown38TableOffset { get; set; } // 0x38
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown3C { get; set; } // 0x3C
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown40 { get; set; } // 0x40
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown44 { get; set; } // 0x40
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown48 { get; set; } // 0x48
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown4C { get; set; } // 0x4C
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown50 { get; set; } // 0x4C
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown54 { get; set; } // 0x4C
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown58 { get; set; } // 0x4C
    /// <summary>
    /// Number of animations
    /// </summary>
    public int NumAnimations { get; set; } // 0x5C
    /// <summary>
    /// Animation definitions
    /// </summary>
    public int AnimDefinitionsOffset { get; set; } // 0x60
    /// <summary>
    /// Animation transform data offset
    /// </summary>
    public int AnimTransformDataOffset { get; set; } // 0x64

    /// <summary>
    /// Translation data offset
    /// </summary>
    public int TranslateDataOffset { get; set; } // *AnimTransformDataOffset + 0
    /// <summary>
    /// Rotation data offset
    /// </summary>
    public int RotateDataOffset { get; set; } // *AnimTransformDataOffset + 4
    /// <summary>
    /// Scale data offset
    /// </summary>
    public int ScaleDataOffset { get; set; } // *AnimTransformDataOffset + 8
    /// <summary>
    /// Keyframe definition offset
    /// </summary>
    public int KeyframeDefinitionsOffset { get; set; } // *AnimTransformDataOffset + 12
    /// <summary>
    /// Translation data table size
    /// </summary>
    public short TranslateDataCount { get; set; } // *AnimTransformDataOffset + 16
    /// <summary>
    /// Rotation data table size
    /// </summary>
    public short RotateDataCount { get; set; } // *AnimTransformDataOffset + 18
    /// <summary>
    /// Scale data table size
    /// </summary>
    public short ScaleDataCount { get; set; } // *AnimTransformDataOffset + 20
    /// <summary>
    /// Number of keyframes
    /// </summary>
    public short NumKeyframes { get; set; } // *AnimTransformDataOffset + 22
    /// <summary>
    /// Animation definitions table
    /// </summary>
    public List<SgeAnimation> AnimationDefinitions { get; set; } = [];
    /// <summary>
    /// GX lighting data table
    /// </summary>
    public List<SgeGXLightingData> GXLightingDataTable { get; set; } = [];
    /// <summary>
    /// Translation data table entries
    /// </summary>
    public List<TranslateDataEntry> TranslateDataEntries { get; set; } = [];
    /// <summary>
    /// Rotation data table entries
    /// </summary>
    public List<RotateDataEntry> RotateDataEntries { get; set; } = [];
    /// <summary>
    /// Scale data table entries
    /// </summary>
    public List<ScaleDataEntry> ScaleDataEntries { get; set; } = [];
    /// <summary>
    /// Keyframe data table entries
    /// </summary>
    public List<KeyframeDefinition> KeyframeDefinitions { get; set; } = [];

    /// <summary>
    /// Constructs model definition details from binary data
    /// </summary>
    /// <param name="data"></param>
    /// <param name="offset"></param>
    public ModelDefinitionDetails(byte[] data, int offset)
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
            GXLightingDataTable.Add(new([.. data.Skip(Unknown38TableOffset + i * 0x48).Take(0x48)], Unknown38TableOffset + i * 0x48));
        }

        for (int i = 0; i < TranslateDataCount; i++)
        {
            TranslateDataEntries.Add(new([.. data.Skip(TranslateDataOffset + i * 0x0C).Take(0x0C)]));
        }

        for (int i = 0; i < RotateDataCount; i++)
        {
            RotateDataEntries.Add(new([.. data.Skip(RotateDataOffset + i * 0x10).Take(0x10)]));
        }

        for (int i = 0; i < ScaleDataCount; i++)
        {
            ScaleDataEntries.Add(new([.. data.Skip(ScaleDataOffset + i * 0x0C).Take(0x0C)]));
        }

        for (int i = 0; i < NumKeyframes; i++)
        {
            KeyframeDefinitions.Add(new([.. data.Skip(KeyframeDefinitionsOffset + i * 0x28).Take(0x28)]));
        }
    }
}