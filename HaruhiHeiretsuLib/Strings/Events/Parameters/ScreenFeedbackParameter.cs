using System;
using System.Collections.Generic;
using HaruhiHeiretsuLib.Util;

namespace HaruhiHeiretsuLib.Strings.Events.Parameters;

/// <summary>
/// Parameter for describing camera distortion effects
/// </summary>
public class ScreenFeedbackParameter : ActionParameter
{
    /// <summary>
    /// Unknown
    /// </summary>
    internal int Unknown0C { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    internal int Unknown10 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    internal int Unknown14 { get; set; }
    /// <summary>
    /// Pointer to this parameter (runtime)
    /// </summary>
    internal uint ParamPointer { get; set; }
    /// <summary>
    /// Flag to enable alpha
    /// </summary>
    public byte EnableAlpha { get; set; }
    /// <summary>
    /// Start alpha percentage (128 * A / 100)
    /// </summary>
    public byte StartAlpha { get; set; }
    /// <summary>
    /// End alpha percentage (128 * A / 100)
    /// </summary>
    public byte EndAlpha { get; set; }
    /// <summary>
    /// Flag to enable offsetting on the X-axis
    /// </summary>
    public byte EnableXOffset { get; set; }
    /// <summary>
    /// Start X offset (pixels)
    /// </summary>
    public short StartXOffset { get; set; }
    /// <summary>
    /// Padding
    /// </summary>
    internal short Padding22 { get; set; }
    /// <summary>
    /// End X offset (pixels)
    /// </summary>
    public short EndXOffset { get; set; }
    /// <summary>
    /// Padding
    /// </summary>
    internal short Padding26 { get; set; }
    /// <summary>
    /// Flag to enable Y offset
    /// </summary>
    public byte EnableYOffset { get; set; }
    /// <summary>
    /// Padding
    /// </summary>
    internal byte Padding29 { get; set; }
    /// <summary>
    /// Padding
    /// </summary>
    internal byte Padding2A { get; set; }
    /// <summary>
    /// Padding
    /// </summary>
    internal byte Padding2B { get; set; }
    /// <summary>
    /// Start Y offset (pixels)
    /// </summary>
    public short StartYOffset { get; set; }
    /// <summary>
    /// Padding
    /// </summary>
    internal short Padding2E { get; set; }
    /// <summary>
    /// End Y offset (pixels)
    /// </summary>
    public short EndYOffset { get; set; }
    /// <summary>
    /// Padding
    /// </summary>
    internal short Padding32 { get; set; }
    /// <summary>
    /// Padding
    /// </summary>
    public byte EnableXScale { get; set; }
    /// <summary>
    /// Flag to enable scaling on the X axis
    /// </summary>
    internal byte Padding35 { get; set; }
    /// <summary>
    /// Padding
    /// </summary>
    internal byte Padding36 { get; set; }
    /// <summary>
    /// Padding
    /// </summary>
    internal byte Padding37 { get; set; }
    /// <summary>
    /// Padding
    /// </summary>
    public float StartXScale { get; set; }
    /// <summary>
    /// Start X-axis scale multiplier
    /// </summary>
    public float EndXScale { get; set; }
    /// <summary>
    /// End X-axis scale multiplier
    /// </summary>
    public byte EnableYScale { get; set; }
    /// <summary>
    /// Flag to enable scaling on the Y axis
    /// </summary>
    internal byte Padding41 { get; set; }
    /// <summary>
    /// Padding
    /// </summary>
    internal byte Padding42 { get; set; }
    /// <summary>
    /// Padding
    /// </summary>
    internal byte Padding43 { get; set; }
    /// <summary>
    /// Padding
    /// </summary>
    public float StartYScale { get; set; }
    /// <summary>
    /// Start Y-axis scale multiplier
    /// </summary>
    public float EndYScale { get; set; }
    /// <summary>
    /// End Y-axis scale multiplier
    /// </summary>
    public byte EnableRotation { get; set; }
    /// <summary>
    /// Flag to enable rotation
    /// </summary>
    internal byte Padding4D { get; set; }
    /// <summary>
    /// Padding
    /// </summary>
    internal byte Padding4E { get; set; }
    /// <summary>
    /// Padding
    /// </summary>
    internal byte Padding4F { get; set; }
    /// <summary>
    /// Start rotation (in degrees)
    /// </summary>
    public int RotationStart { get; set; }
    /// <summary>
    /// End rotation (in degrees)
    /// </summary>
    public int RotationEnd { get; set; }

    /// <summary>
    /// Constructs a distortion parameter from binary data
    /// </summary>
    /// <param name="data">Binary data of the event file</param>
    /// <param name="offset">Offset into the binary data where the param starts</param>
    /// <param name="opCode">Action op code</param>
    public ScreenFeedbackParameter(byte[] data, int offset, ActionOpCode opCode) : base(data, offset, opCode)
    {
        Unknown0C = IO.ReadIntLE(data, offset + 0x0C);
        Unknown10 = IO.ReadIntLE(data, offset + 0x10);
        Unknown14 = IO.ReadIntLE(data, offset + 0x14);
        ParamPointer = IO.ReadUIntLE(data, offset + 0x18);
        EnableAlpha = data[0x1C];
        StartAlpha = data[0x1D];
        EndAlpha = data[0x1E];
        EnableXOffset = data[0x1F];
        StartXOffset = IO.ReadShortLE(data, offset + 0x20);
        Padding22 = IO.ReadShortLE(data, offset + 0x22);
        EndXOffset = IO.ReadShortLE(data, offset + 0x24);
        Padding26 = IO.ReadShortLE(data, offset + 0x26);
        EnableYOffset = data[0x28];
        Padding29 = data[0x29];
        Padding2A = data[0x2A];
        Padding2B = data[0x2B];
        StartYOffset = IO.ReadShortLE(data, offset + 0x2C);
        Padding2E = IO.ReadShortLE(data, offset + 0x2E);
        EndYOffset = IO.ReadShortLE(data, offset + 0x30);
        Padding32 = IO.ReadShortLE(data, offset + 0x32);
        EnableXScale = data[0x34];
        Padding35 = data[0x35];
        Padding36 = data[0x36];
        Padding37 = data[0x37];
        StartXScale = IO.ReadFloatLE(data, offset + 0x38);
        EndXScale = IO.ReadFloatLE(data, offset + 0x3C);
        EnableYScale = data[0x40];
        Padding41 = data[0x41];
        Padding42 = data[0x42];
        Padding43 = data[0x43];
        StartYScale = IO.ReadFloatLE(data, offset + 0x44);
        EndYScale = IO.ReadFloatLE(data, offset + 0x48);
        EnableRotation = data[0x4C];
        Padding4D = data[0x4D];
        Padding4E = data[0x4E];
        Padding4F = data[0x4F];
        RotationStart = IO.ReadIntLE(data, offset + 0x50);
        RotationEnd = IO.ReadIntLE(data, offset + 0x54);
    }

    /// <inheritdoc/>
    public override List<byte> GetBytes()
    {
        return
        [
            ..GetHeaderBytes(),
            ..BitConverter.GetBytes(Unknown0C),
            ..BitConverter.GetBytes(Unknown10),
            ..BitConverter.GetBytes(Unknown14),
            ..BitConverter.GetBytes(ParamPointer),
            EnableAlpha,
            StartAlpha,
            EndAlpha,
            EnableXOffset,
            ..BitConverter.GetBytes(StartXOffset),
            ..BitConverter.GetBytes(Padding22),
            ..BitConverter.GetBytes(EndXOffset),
            ..BitConverter.GetBytes(Padding26),
            EnableYOffset,
            Padding29,
            Padding2A,
            Padding2B,
            ..BitConverter.GetBytes(StartYOffset),
            ..BitConverter.GetBytes(Padding2E),
            ..BitConverter.GetBytes(EndYOffset),
            ..BitConverter.GetBytes(Padding32),
            EnableXScale,
            Padding35,
            Padding36,
            Padding37,
            ..BitConverter.GetBytes(StartXScale),
            ..BitConverter.GetBytes(EndXScale),
            EnableYScale,
            Padding41,
            Padding42,
            Padding43,
            ..BitConverter.GetBytes(StartYScale),
            ..BitConverter.GetBytes(EndYScale),
            EnableRotation,
            Padding4D,
            Padding4E,
            Padding4F,
            ..BitConverter.GetBytes(RotationStart),
            ..BitConverter.GetBytes(RotationEnd),
        ];
    }
}