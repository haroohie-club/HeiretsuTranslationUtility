using System;
using System.Collections.Generic;
using System.Linq;

namespace HaruhiHeiretsuLib.Data
{
    // dat 004
    public class SgeIndexFile : DataFile
    {
        public List<SgeIndexFileEntry> Section1Entries { get; set; } = new();
        public List<SgeIndexFileSection2Entry> Section2Entries { get; set; } = new();
        public List<short> Section3Entries { get; set; } = new();

        public SgeIndexFile()
        {
            Name = "SGE Indices";
        }

        public override void Initialize(byte[] decompressedData, int offset)
        {
            base.Initialize(decompressedData, offset);

            int section1Offset = BitConverter.ToInt32(Data.Skip(0x0C).Take(4).Reverse().ToArray());
            int numSection1Entries = BitConverter.ToInt32(Data.Skip(0x10).Take(4).Reverse().ToArray());
            for (int i = 0; i < numSection1Entries; i++)
            {
                Section1Entries.Add(new(Data.Skip(section1Offset + i * 0x24).Take(0x24)));
            }

            int section2Offset = BitConverter.ToInt32(Data.Skip(0x14).Take(4).Reverse().ToArray());
            int numSection2Entries = BitConverter.ToInt32(Data.Skip(0x18).Take(4).Reverse().ToArray());
            for (int i = 0; i < numSection2Entries; i++)
            {
                Section2Entries.Add(new(Data.Skip(section2Offset + i * 8).Take(8)));
            }

            int section3Offset = BitConverter.ToInt32(Data.Skip(0x1C).Take(4).Reverse().ToArray());
            int numSection3Entries = BitConverter.ToInt32(Data.Skip(0x20).Take(4).Reverse().ToArray());
            for (int i = 0; i < numSection3Entries; i++)
            {
                Section3Entries.Add(BitConverter.ToInt16(Data.Skip(section3Offset + i * 2).Take(2).Reverse().ToArray()));
            }
        }
    }

    public class SgeIndexFileEntry
    {
        public short Index { get; set; }
        public short Unknown02 { get; set; }
        public int Unknown04 { get; set; }
        public int Unknown08 { get; set; }
        public int Unknown0C { get; set; }
        public short Unknown10 { get; set; }
        public int Unknown12 { get; set; }
        public short ModelGrpIndex { get; set; }
        public short Unknown18 { get; set; }
        public short Unknown1A { get; set; }
        public int Unknown1C { get; set; }
        public int Unknown20 { get; set; }

        public SgeIndexFileEntry(IEnumerable<byte> data)
        {
            Index = BitConverter.ToInt16(data.Take(2).Reverse().ToArray());
            Unknown02 = BitConverter.ToInt16(data.Skip(0x02).Take(2).Reverse().ToArray());
            Unknown04 = BitConverter.ToInt32(data.Skip(0x04).Take(4).Reverse().ToArray());
            Unknown08 = BitConverter.ToInt32(data.Skip(0x08).Take(4).Reverse().ToArray());
            Unknown0C = BitConverter.ToInt32(data.Skip(0x0C).Take(4).Reverse().ToArray());
            Unknown10 = BitConverter.ToInt16(data.Skip(0x10).Take(2).Reverse().ToArray());
            Unknown12 = BitConverter.ToInt16(data.Skip(0x12).Take(4).Reverse().ToArray());
            ModelGrpIndex = BitConverter.ToInt16(data.Skip(0x16).Take(2).Reverse().ToArray());
            Unknown18 = BitConverter.ToInt16(data.Skip(0x18).Take(2).Reverse().ToArray());
            Unknown1A = BitConverter.ToInt16(data.Skip(0x1A).Take(2).Reverse().ToArray());
            Unknown1C = BitConverter.ToInt16(data.Skip(0x1C).Take(4).Reverse().ToArray());
            Unknown20 = BitConverter.ToInt16(data.Skip(0x20).Take(4).Reverse().ToArray());
        }
    }
    public class SgeIndexFileSection2Entry
    {
        public int Unknown00 { get; set; }
        public int Unknown04 { get; set; }

        public SgeIndexFileSection2Entry(IEnumerable<byte> data)
        {
            Unknown00 = BitConverter.ToInt32(data.Take(4).Reverse().ToArray());
            Unknown04 = BitConverter.ToInt32(data.Skip(0x04).Take(4).Reverse().ToArray());
        }
    }
}
