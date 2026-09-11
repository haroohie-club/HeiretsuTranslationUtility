using HaruhiHeiretsuLib.Util;

namespace HaruhiHeiretsuLib.Strings.Events.Parameters;

/// <summary>
/// Sound effect action parameter
/// </summary>
public class SfxParameter : ActionParameter
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
    /// Index of SFX to play
    /// </summary>
    public int SfxIndex { get; set; }
    /// <summary>
    /// Whether to start the SFX immediately (runtime, I thinK?)
    /// </summary>
    public bool StartImmediately { get; set; }
    /// <summary>
    /// Volume of SFX (0..128)
    /// </summary>
    public int Volume { get; set; }
    /// <summary>
    /// Pan (-100..100) (left to right)
    /// </summary>
    public int Pan { get; set; }
    
    /// <inheritdoc/>
    public SfxParameter(byte[] data, int offset, ActionOpCode opCode) : base(data, offset, opCode)
    {
        Unknown0C = IO.ReadIntLE(data, offset + 0x0C);
        Unknown10 = IO.ReadIntLE(data, offset + 0x10);
        Unknown14 = IO.ReadIntLE(data, offset + 0x14);
        Unknown18 = IO.ReadIntLE(data, offset + 0x18);
        SfxIndex = IO.ReadIntLE(data, offset + 0x1C);
        StartImmediately = IO.ReadIntLE(data, offset + 0x20) != 0;
        Volume = IO.ReadIntLE(data, offset + 0x24);
        Pan = IO.ReadIntLE(data, offset + 0x28);
    }
}