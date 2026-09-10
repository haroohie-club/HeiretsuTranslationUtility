using System.Numerics;
using HaruhiHeiretsuLib.Util;
using SkiaSharp;

namespace HaruhiHeiretsuLib.Strings.Events.Parameters;

/// <summary>
/// Parameter for defining ambient and directional light
/// </summary>
public class LightParameter : ActionParameter
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
    /// Flag to enable/disable ambient light
    /// </summary>
    public byte EnableAmbientLight { get; set; }
    /// <summary>
    /// Ambient light color (beginning of animation)
    /// </summary>
    public SKColor AmbientLightStart { get; set; }
    /// <summary>
    /// Ambient light color (end of animation)
    /// </summary>
    public SKColor AmbientLightEnd { get; set; }
    /// <summary>
    /// Flag to enable/disable directional light
    /// </summary>
    public byte LightColorEnable { get; set; }
    /// <summary>
    /// Directional light color (beginning of animation)
    /// </summary>
    public SKColor LightColorStart { get; set; }
    /// <summary>
    /// Directional light color (end of animation)
    /// </summary>
    public SKColor LightColorEnd { get; set; }
    /// <summary>
    /// Flag to enable/disable directional light direction
    /// </summary>
    public byte LightDirectionEnable { get; set; }
    /// <summary>
    /// Directional light direction (beginning of animation)
    /// </summary>
    public Vector3 LightDirectionStart { get; set; }
    /// <summary>
    /// Directional light direction (end of animation)
    /// </summary>
    public Vector3 LightDirectionEnd { get; set; }
    
    /// <inheritdoc/>
    public LightParameter(byte[] data, int offset, ActionOpCode opCode) : base(data, offset, opCode)
    {
        Unknown0C = IO.ReadIntLE(data, offset + 0x0C);
        Unknown10 = IO.ReadIntLE(data, offset + 0x10);
        Unknown14 = IO.ReadIntLE(data, offset + 0x14);
        Unknown18 = IO.ReadIntLE(data, offset + 0x18);
        EnableAmbientLight = data[offset + 0x1C];
        AmbientLightStart = new(data[offset + 0x22], data[offset + 0x21], data[offset + 0x20], data[offset + 0x23]);
        AmbientLightEnd = new(data[offset + 0x26], data[offset + 0x25], data[offset + 0x24], data[offset + 0x27]);
        LightColorEnable = data[offset + 0x28];
        LightColorStart = new(data[offset + 0x2E], data[offset + 0x2D], data[offset + 0x2C], data[offset + 0x2F]);
        LightColorEnd = new(data[offset + 0x32], data[offset + 0x31], data[offset + 0x30], data[offset + 0x33]);
        LightDirectionEnable = data[offset + 0x34];
        LightDirectionStart = new(IO.ReadFloatLE(data, offset + 0x38), IO.ReadFloatLE(data, offset + 0x3C), IO.ReadFloatLE(data, offset + 0x40));
        LightDirectionEnd = new(IO.ReadFloatLE(data, offset + 0x44), IO.ReadFloatLE(data, offset + 0x48), IO.ReadFloatLE(data, offset + 0x4C));
    }
}