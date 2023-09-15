using System;
using System.Collections.Generic;
using System.Linq;

namespace HaruhiHeiretsuLib.Strings.Events.Parameters
{
    public class ModelAnimationParameter : ActionParameter
    {
        public int Unknown0C { get; set; }
        public int Unknown10 { get; set; }
        public int Unknown14 { get; set; }
        public int Unknown18 { get; set; }
        public int Unknown1C { get; set; }
        public int Unknown20 { get; set; }
        public int Unknown24 { get; set; }
        public int Unknown28 { get; set; }
        public int Unknown2C { get; set; }
        public int Unknown30 { get; set; }
        public bool UseAltAnimation { get; set; }
        public short AnimationIndex { get; set; }
        public short Unknown36 { get; set; }
        public int Unknown38 { get; set; }
        public int Unknown3C { get; set; }
        public byte AnimationSpeed { get; set; }
        public byte Unknown41 { get; set; }
        public byte Unknown42 { get; set; }
        public byte Unknown43 { get; set; }
        public short Unknown44 { get; set; }
        public short Unknown46 { get; set; }

        public ModelAnimationParameter(IEnumerable<byte> data, int offset, ushort opCode) : base(data, offset, opCode)
        {
            Unknown0C = BitConverter.ToInt32(data.Skip(offset + 0x0C).Take(4).ToArray());
            Unknown10 = BitConverter.ToInt32(data.Skip(offset + 0x10).Take(4).ToArray());
            Unknown14 = BitConverter.ToInt32(data.Skip(offset + 0x14).Take(4).ToArray());
            Unknown18 = BitConverter.ToInt32(data.Skip(offset + 0x18).Take(4).ToArray());
            Unknown1C = BitConverter.ToInt32(data.Skip(offset + 0x1C).Take(4).ToArray());
            Unknown20 = BitConverter.ToInt32(data.Skip(offset + 0x20).Take(4).ToArray());
            Unknown24 = BitConverter.ToInt32(data.Skip(offset + 0x24).Take(4).ToArray());
            Unknown28 = BitConverter.ToInt32(data.Skip(offset + 0x28).Take(4).ToArray());
            Unknown2C = BitConverter.ToInt32(data.Skip(offset + 0x2C).Take(4).ToArray());
            Unknown30 = BitConverter.ToInt32(data.Skip(offset + 0x30).Take(4).ToArray());
            short animParam = BitConverter.ToInt16(data.Skip(offset + 0x34).Take(2).ToArray());
            UseAltAnimation = (animParam & 0x8000) > 0;
            AnimationIndex = (short)(animParam & 0x7FFF);
            Unknown36 = BitConverter.ToInt16(data.Skip(offset + 0x36).Take(2).ToArray());
            Unknown38 = BitConverter.ToInt32(data.Skip(offset + 0x38).Take(4).ToArray());
            Unknown3C = BitConverter.ToInt32(data.Skip(offset + 0x3C).Take(4).ToArray());
            AnimationSpeed = data.ElementAt(offset + 0x40);
            Unknown41 = data.ElementAt(offset + 0x41);
            Unknown42 = data.ElementAt(offset + 0x42);
            Unknown43 = data.ElementAt(offset + 0x43);
            Unknown44 = BitConverter.ToInt16(data.Skip(offset + 0x44).Take(2).ToArray());
            Unknown46 = BitConverter.ToInt16(data.Skip(offset + 0x46).Take(2).ToArray());
        }
    }
}
