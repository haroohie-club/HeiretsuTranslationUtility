namespace HaruhiHeiretsuLib.Strings.Events.Parameters;

public class CrossFadeParam : ActionParameter
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
    /// Unknown
    /// </summary>
    internal int Unknown1C { get; set; }
    
    /// <inheritdoc/>
    public CrossFadeParam(byte[] data, int offset, ActionOpCode opCode) : base(data, offset, opCode)
    {
    }
}