﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace HaruhiHeiretsuLib.Strings.Events
{
    // 0x14 bytes
    public class ChapterDefinition
    {
        public float StartTime { get; set; }
        public float EndTime { get; set; }
        public int Unknown08 { get; set; }
        public ushort ActorDefTableEntryCount { get; set; }
        public int ActorDefTableOffset { get; set; }
        public List<ActorDefinition> ActorDefinitionTable { get; set; } = new List<ActorDefinition>();

        public ChapterDefinition(IEnumerable<byte> data, int offset)
        {
            StartTime = BitConverter.ToSingle(data.Skip(offset).Take(4).ToArray());
            EndTime = BitConverter.ToSingle(data.Skip(offset + 0x04).Take(4).ToArray());
            Unknown08 = BitConverter.ToInt32(data.Skip(offset + 0x08).Take(4).ToArray());
            ActorDefTableEntryCount = BitConverter.ToUInt16(data.Skip(offset + 0x0C).Take(2).ToArray());
            ActorDefTableOffset = BitConverter.ToInt32(data.Skip(offset + 0x10).Take(4).ToArray());

            for (int i = 0; i < ActorDefTableEntryCount; i++)
            {
                ActorDefinitionTable.Add(new ActorDefinition(data, ActorDefTableOffset + i * 0x3C));
            }
        }
    }
}
