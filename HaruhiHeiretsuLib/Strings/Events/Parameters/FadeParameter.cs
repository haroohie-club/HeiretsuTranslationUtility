using SkiaSharp;
using System;
using System.Collections.Generic;
using HaruhiHeiretsuLib.Util;

namespace HaruhiHeiretsuLib.Strings.Events.Parameters;

/// <summary>
/// A parameter describing a fade in/out
/// </summary>
public class FadeParameter : ActionParameter
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
    /// Unknown
    /// </summary>
    public int Unknown14 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown18 { get; set; }
    /// <summary>
    /// Start fade color
    /// </summary>
    public SKColor StartColor { get; set; }
    /// <summary>
    /// End fade color
    /// </summary>
    public SKColor EndColor { get; set; }
    /// <summary>
    /// Flag indicating the fade should be quick
    /// </summary>
    public byte FadeFast { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public byte Unknown25 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public byte Unknown26 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public byte Unknown27 { get; set; }

    /// <inheritdoc/>
    public FadeParameter(byte[] data, int offset, ActionOpCode opCode) : base(data, offset, opCode)
    {
        Unknown0C = IO.ReadIntLE(data, offset + 0x0C);
        Unknown10 = IO.ReadIntLE(data, offset + 0x10);
        Unknown14 = IO.ReadIntLE(data, offset + 0x14);
        Unknown18 = IO.ReadIntLE(data, offset + 0x18);
        StartColor = new(data[offset + 0x1E], data[offset + 0x1D], data[offset + 0x1C], data[offset + 0x1F]);
        EndColor = new(data[offset + 0x22], data[offset + 0x21], data[offset + 0x20], data[offset + 0x23]);
        FadeFast = data[offset + 0x24];
        Unknown25 = data[offset + 0x25];
        Unknown26 = data[offset + 0x26];
        Unknown27 = data[offset + 0x27];
    }

    /// <inheritdoc />
    public override List<byte> GetBytes()
    {
        return
        [
            ..GetHeaderBytes(),
            ..BitConverter.GetBytes(Unknown0C),
            ..BitConverter.GetBytes(Unknown10),
            ..BitConverter.GetBytes(Unknown14),
            ..BitConverter.GetBytes(Unknown18),
            StartColor.Red,
            StartColor.Green,
            StartColor.Blue,
            StartColor.Alpha,
            EndColor.Red,
            EndColor.Green,
            EndColor.Blue,
            EndColor.Alpha,
            FadeFast,
            Unknown25,
            Unknown26,
            Unknown27,
        ];
    }
}