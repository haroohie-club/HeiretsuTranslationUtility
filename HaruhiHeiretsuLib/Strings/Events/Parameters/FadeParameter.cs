using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HaruhiHeiretsuLib.Strings.Events.Parameters
{
    public class FadeParameter : ActionParameter
    {
        public int Unknown0C { get; set; }
        public int Unknown10 { get; set; }
        public int Unknown14 { get; set; }
        public int Unknown18 { get; set; }
        public SKColor StartColor { get; set; }
        public SKColor EndColor { get; set; }
        public byte Unknown24 { get; set; }
        public byte Unknown25 { get; set; }
        public byte Unknown26 { get; set; }
        public byte Unknown27 { get; set; }

        public FadeParameter(IEnumerable<byte> data, int offset, ushort opCode) : base(data, offset, opCode)
        {
            Unknown0C = BitConverter.ToInt32(data.Skip(offset + 0x0C).Take(4).ToArray());
            Unknown10 = BitConverter.ToInt32(data.Skip(offset + 0x10).Take(4).ToArray());
            Unknown14 = BitConverter.ToInt32(data.Skip(offset + 0x14).Take(4).ToArray());
            Unknown18 = BitConverter.ToInt32(data.Skip(offset + 0x18).Take(4).ToArray());
            StartColor = new(data.ElementAt(offset + 0x1E), data.ElementAt(offset + 0x1D), data.ElementAt(offset + 0x1C), data.ElementAt(offset + 0x1F));
            EndColor = new(data.ElementAt(offset + 0x22), data.ElementAt(offset + 0x21), data.ElementAt(offset + 0x20), data.ElementAt(offset + 0x23));
            Unknown24 = data.ElementAt(offset + 0x24);
            Unknown25 = data.ElementAt(offset + 0x25);
            Unknown26 = data.ElementAt(offset + 0x26);
            Unknown27 = data.ElementAt(offset + 0x27);
        }
    }
}
