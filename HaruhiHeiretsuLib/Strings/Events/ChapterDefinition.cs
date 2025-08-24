using System;
using System.Collections.Generic;
using System.Linq;
using HaruhiHeiretsuLib.Util;

namespace HaruhiHeiretsuLib.Strings.Events
{
    // 0x14 bytes
    public class ChapterDefinition
    {
        public float StartTime { get; set; }
        public float EndTime { get; set; }
        public int CurrentTime { get; set; }
        public ushort ActorDefTableEntryCount { get; set; }
        public int ActorDefTableOffset { get; set; }
        public List<ActorDefinition> ActorDefinitionTable { get; set; } = [];

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
}
