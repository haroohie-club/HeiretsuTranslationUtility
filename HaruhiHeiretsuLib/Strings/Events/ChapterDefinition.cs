using System.Collections.Generic;
using HaruhiHeiretsuLib.Util;

namespace HaruhiHeiretsuLib.Strings.Events;

// 0x14 bytes
/// <summary>
/// A chapter definition in a cutscene
/// </summary>
public class ChapterDefinition
{
    /// <summary>
    /// The start time (in frames) of the chapter
    /// </summary>
    public float StartTime { get; set; }
    /// <summary>
    /// The end time (in frames) of the chapter
    /// </summary>
    public float EndTime { get; set; }
    /// <summary>
    /// The current time (in frames) while the cutscene is playing
    /// </summary>
    public int CurrentTime { get; set; }
    /// <summary>
    /// The number of actor definitions
    /// </summary>
    public ushort ActorDefTableEntryCount { get; set; }
    /// <summary>
    /// The actor definition table offset
    /// </summary>
    public int ActorDefTableOffset { get; set; }
    /// <summary>
    /// The actual list of actor definitions in the cutscene data
    /// </summary>
    public List<ActorDefinition> ActorDefinitionTable { get; set; } = [];

    /// <summary>
    /// Constructs a chapter definition given binary data
    /// </summary>
    /// <param name="data">The binary cutscene data</param>
    /// <param name="offset">The offset into the cutscene file</param>
    public ChapterDefinition(byte[] data, int offset)
    {
        StartTime = IO.ReadFloatLE(data,offset);
        EndTime = IO.ReadFloatLE(data, offset + 0x04);
        CurrentTime = IO.ReadIntLE(data, offset + 0x08);
        ActorDefTableEntryCount = IO.ReadUShortLE(data, offset + 0x0C);
        ActorDefTableOffset = IO.ReadIntLE(data, offset + 0x10);

        for (int i = 0; i < ActorDefTableEntryCount; i++)
        {
            ActorDefinitionTable.Add(new(data, ActorDefTableOffset + i * 0x3C));
        }
    }
}