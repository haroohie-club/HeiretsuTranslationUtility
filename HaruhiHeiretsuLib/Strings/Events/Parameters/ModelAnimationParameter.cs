using System;
using System.Collections.Generic;
using HaruhiHeiretsuLib.Util;

namespace HaruhiHeiretsuLib.Strings.Events.Parameters;

/// <summary>
/// Parameter describing animation for a model
/// </summary>
public class ModelAnimationParameter : ActionParameter
{
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown0C { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown10 { get; set; }
    /// <summary>
    /// Used to set the current model/map/etc. active/inactive
    /// </summary>
    public int IsActive { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown18 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown1C { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown20 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown24 { get; set; }
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
    /// Flag indicating the alternate animation (i.e. event-file specified animation) should be used
    /// </summary>
    public bool UseAltAnimation { get; set; }
    /// <summary>
    /// Index of the animation to play
    /// </summary>
    public short AnimationIndex { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public short Unknown36 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown38 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown3C { get; set; }
    /// <summary>
    /// Speed to play the animation
    /// </summary>
    public byte AnimationSpeed { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public byte Unknown41 { get; set; }
    /// <summary>
    /// The mesh slot of the zero map (assuming this is actor type ZERO_MAP)
    /// </summary>
    public byte ZeroMapMeshSlot { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public byte Unknown43 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public short Unknown44 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public short Unknown46 { get; set; }

    /// <inheritdoc/>
    public ModelAnimationParameter(byte[] data, int offset, ActionOpCode opCode) : base(data, offset, opCode)
    {
        Unknown0C = IO.ReadIntLE(data, offset + 0x0C);
        Unknown10 = IO.ReadIntLE(data, offset + 0x10);
        IsActive = IO.ReadIntLE(data, offset + 0x14);
        Unknown18 = IO.ReadIntLE(data, offset + 0x18);
        Unknown1C = IO.ReadIntLE(data, offset + 0x1C);
        Unknown20 = IO.ReadIntLE(data, offset + 0x20);
        Unknown24 = IO.ReadIntLE(data, offset + 0x24);
        Unknown28 = IO.ReadIntLE(data, offset + 0x28);
        Unknown2C = IO.ReadIntLE(data, offset + 0x2C);
        Unknown30 = IO.ReadIntLE(data, offset + 0x30);
        short animParam = IO.ReadShortLE(data, offset + 0x34);
        UseAltAnimation = (animParam & 0x8000) > 0;
        AnimationIndex = (short)(animParam & 0x7FFF);
        Unknown36 = IO.ReadShortLE(data, offset + 0x36);
        Unknown38 = IO.ReadIntLE(data, offset + 0x38);
        Unknown3C = IO.ReadIntLE(data, offset + 0x3C);
        AnimationSpeed = data[offset + 0x40];
        Unknown41 = data[offset + 0x41];
        ZeroMapMeshSlot = data[offset + 0x42];
        Unknown43 = data[offset + 0x43];
        Unknown44 = IO.ReadShortLE(data, offset + 0x44);
        Unknown46 = IO.ReadShortLE(data, offset + 0x46);
    }

    /// <inheritdoc />
    public override List<byte> GetBytes()
    {
        return
        [
            ..GetHeaderBytes(),
            ..BitConverter.GetBytes(Unknown0C),
            ..BitConverter.GetBytes(Unknown10),
            ..BitConverter.GetBytes(IsActive),
            ..BitConverter.GetBytes(Unknown18),
            ..BitConverter.GetBytes(Unknown1C),
            ..BitConverter.GetBytes(Unknown20),
            ..BitConverter.GetBytes(Unknown24),
            ..BitConverter.GetBytes(Unknown28),
            ..BitConverter.GetBytes(Unknown2C),
            ..BitConverter.GetBytes(Unknown30),
            ..BitConverter.GetBytes((short)((UseAltAnimation ? 0x8000 : 0) | (ushort)AnimationIndex)),
            ..BitConverter.GetBytes(Unknown36),
            ..BitConverter.GetBytes(Unknown38),
            ..BitConverter.GetBytes(Unknown3C),
            AnimationSpeed,
            Unknown41,
            ZeroMapMeshSlot,
            Unknown43,
            ..BitConverter.GetBytes(Unknown44),
            ..BitConverter.GetBytes(Unknown46),
        ];
    }
}