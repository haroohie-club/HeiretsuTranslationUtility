using System;
using System.Collections.Generic;
using HaruhiHeiretsuLib.Util;

namespace HaruhiHeiretsuLib.Strings.Events.Parameters;

/// <summary>
/// Parameter for setting the far clipping plane and billboard distance
/// </summary>
public class CameraRangeParameter : ActionParameter
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
    /// Camera far clip enabled
    /// </summary>
    public byte FarClipEnabled { get; set; }
    /// <summary>
    /// Start of far clip distance (lerped)
    /// </summary>
    public float FarClipStart { get; set; }
    /// <summary>
    /// End of far clip distance (lerped)
    /// </summary>
    public float FarClipEnd { get; set; }
    /// <summary>
    /// Billboard draw enabled
    /// </summary>
    public byte EnableBillboardDrawDistance { get; set; }
    /// <summary>
    /// Start of billboard draw distance (lerped)
    /// </summary>
    public float StartBillboardDistance { get; set; }
    /// <summary>
    /// End of billboard draw distancec (lerped)
    /// </summary>
    public float EndBillboardDistance { get; set; }
    /// <summary>
    /// Flags
    /// </summary>
    public byte Flags { get; set; }

    /// <inheritdoc/>
    public CameraRangeParameter(byte[] data, int offset, ActionOpCode opCode) : base(data, offset, opCode)
    {
        Unknown0C = IO.ReadIntLE(data, offset + 0x0C);
        Unknown10 = IO.ReadIntLE(data, offset + 0x10);
        Unknown14 = IO.ReadIntLE(data, offset + 0x14);
        Unknown18 = IO.ReadIntLE(data, offset + 0x18);
        FarClipEnabled = data[0x1C];
        FarClipStart = IO.ReadFloatLE(data, offset + 0x20);
        FarClipEnd = IO.ReadFloatLE(data, offset + 0x24);
        EnableBillboardDrawDistance = data[0x28];
        StartBillboardDistance = IO.ReadFloatLE(data, offset + 0x2C);
        EndBillboardDistance = IO.ReadFloatLE(data, offset + 0x30);
        Flags = data[0x34];
    }

    /// <inheritdoc/>
    public override List<byte> GetBytes()
    {
        return
        [
            .. GetHeaderBytes(),
            .. BitConverter.GetBytes(Unknown0C),
            .. BitConverter.GetBytes(Unknown10),
            .. BitConverter.GetBytes(Unknown14),
            .. BitConverter.GetBytes(Unknown18),
            FarClipEnabled, 0, 0, 0,
            .. BitConverter.GetBytes(FarClipStart),
            .. BitConverter.GetBytes(FarClipEnd),
            EnableBillboardDrawDistance, 0, 0, 0,
            .. BitConverter.GetBytes(StartBillboardDistance),
            .. BitConverter.GetBytes(EndBillboardDistance),
            Flags, 0, 0, 0,
        ];
    }
}