using HaruhiHeiretsuLib.Util;

namespace HaruhiHeiretsuLib.Strings.Events.Parameters;

/// <summary>
/// Cross fade
/// </summary>
public class CrossFadeParameter : ActionParameter
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
    /// Unknown
    /// </summary>
    internal int Unknown18 { get; set; }
    /// <summary>
    /// Alpha (at beginning of fade)
    /// </summary>
    public byte AlphaStart { get; set; }
    /// <summary>
    /// Alpha (at end of fade)
    /// </summary>
    public byte AlphaEnd { get; set; }
    /// <summary>
    /// Blend mode (0 normal, nonzero alternate)
    /// </summary>
    public byte BlendMode { get; set; }
    
    /// <inheritdoc/>
    public CrossFadeParameter(byte[] data, int offset, ActionOpCode opCode) : base(data, offset, opCode)
    {
        Unknown0C = IO.ReadIntLE(data, offset + 0x0C);
        Unknown10 = IO.ReadIntLE(data, offset + 0x10);
        Unknown14 = IO.ReadIntLE(data, offset + 0x14);
        Unknown18 = IO.ReadIntLE(data, offset + 0x18);
        AlphaStart = data[offset + 0x1C];
        AlphaEnd = data[offset + 0x1D];
        BlendMode = data[offset + 0x1E];
    }
}