using HaruhiHeiretsuLib.Util;
using SkiaSharp;

namespace HaruhiHeiretsuLib.Strings.Events.Parameters;

/// <summary>
/// Unknown
/// </summary>
public class FogParam : ActionParameter
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
    /// Enable fog start distance
    /// </summary>
    public byte FogStartEnable { get; set; }

    /// <summary>
    /// Fog start distance (beginning of animation)
    /// </summary>
    public float FogStartInitial { get; set; }

    /// <summary>
    /// Fog start distance (end of animation)
    /// </summary>
    public float FogStartFinal { get; set; }

    /// <summary>
    /// Enable fog end distance
    /// </summary>
    public byte FogEndEnable { get; set; }

    /// <summary>
    /// Fog end distance (beginning of animation)
    /// </summary>
    public float FogEndInitial { get; set; }

    /// <summary>
    /// Fog end distance (end of animation)
    /// </summary>
    public float FogEndFinal { get; set; }

    /// <summary>
    /// Enable fog color
    /// </summary>
    public byte FogColorEnable { get; set; }

    /// <summary>
    /// Fog color (beginning of animation)
    /// </summary>
    public SKColor FogColorInitial { get; set; }

    /// <summary>
    /// Fog color (end of animation)
    /// </summary>
    public SKColor FogColorFinal { get; set; }

    /// <summary>
    /// Enable clear color
    /// </summary>
    public byte ClearColorEnable { get; set; }

    /// <summary>
    /// Clear color (beginning of animation)
    /// </summary>
    public SKColor ClearColorInitial { get; set; }

    /// <summary>
    /// Clear color (end of animation)
    /// </summary>
    public SKColor ClearColorFinal { get; set; }

    /// <inheritdoc/>
    public FogParam(byte[] data, int offset, ActionOpCode opCode) : base(data, offset, opCode)
    {
        Unknown0C = IO.ReadIntLE(data, offset + 0x0C);
        Unknown10 = IO.ReadIntLE(data, offset + 0x10);
        Unknown14 = IO.ReadIntLE(data, offset + 0x14);
        Unknown18 = IO.ReadIntLE(data, offset + 0x18);
        FogStartEnable = data[offset + 0x1C];
        FogStartInitial = IO.ReadFloatLE(data, offset + 0x20);
        FogStartFinal = IO.ReadFloatLE(data, offset + 0x24);
        FogEndEnable = data[offset + 0x28];
        FogEndInitial = IO.ReadFloatLE(data, offset + 0x2C);
        FogEndFinal = IO.ReadFloatLE(data, offset + 0x30);
        FogColorEnable = data[offset + 0x34];
        FogColorInitial = new(data[offset + 0x38], data[offset + 0x39], data[offset + 0x3A],
            data[offset + 0x3B]);
        FogColorFinal = new(data[offset + 0x3C], data[offset + 0x3D], data[offset + 0x3E],
            data[offset + 0x3F]);
        ClearColorEnable = data[offset + 0x40];
        ClearColorInitial = new(data[offset + 0x44], data[offset + 0x45], data[offset + 0x46],
            data[offset + 0x47]);
        ClearColorFinal = new(data[offset + 0x48], data[offset + 0x49], data[offset + 0x4A],
            data[offset + 0x4B]);
    }
}