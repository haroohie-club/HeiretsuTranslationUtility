using System.Collections.Generic;
using HaruhiHeiretsuLib.Util;

namespace HaruhiHeiretsuLib.Data
{
    // dat 004
    public class SgeIndexFile : DataFile
    {
        public List<SgeIndexFileEntry> Section1Entries { get; set; } = [];
        public List<SgeIndexFileSection2Entry> Section2Entries { get; set; } = [];
        public List<short> Section3Entries { get; set; } = [];

        public SgeIndexFile()
        {
            Name = "SGE Indices";
        }

        public override void Initialize(byte[] decompressedData, int offset)
        {
            base.Initialize(decompressedData, offset);

            int section1Offset = IO.ReadInt(decompressedData, 0x0C);
            int numSection1Entries = IO.ReadInt(decompressedData, 0x10);
            for (int i = 0; i < numSection1Entries; i++)
            {
                Section1Entries.Add(new(decompressedData[(section1Offset + i * 0x24)..(section1Offset + (i + 1) * 0x24)]));
            }

            int section2Offset = IO.ReadInt(decompressedData, 0x14);
            int numSection2Entries = IO.ReadInt(decompressedData, 0x18);
            for (int i = 0; i < numSection2Entries; i++)
            {
                Section2Entries.Add(new(decompressedData[(section2Offset + i * 8)..(section2Offset + (i + 1) * 8)]));
            }

            int section3Offset = IO.ReadInt(decompressedData, 0x1C);
            int numSection3Entries = IO.ReadInt(decompressedData, 0x20);
            for (int i = 0; i < numSection3Entries; i++)
            {
                Section3Entries.Add(IO.ReadShort(decompressedData, section3Offset + i * 2));
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

        public SgeIndexFileEntry(byte[] data)
        {
            Index = IO.ReadShort(data, 0x00);
            Unknown02 = IO.ReadShort(data, 0x02);
            Unknown04 = IO.ReadInt(data, 0x04);
            Unknown08 = IO.ReadInt(data, 0x08);
            Unknown0C = IO.ReadInt(data, 0x0C);
            Unknown10 = IO.ReadShort(data, 0x10);
            Unknown12 = IO.ReadShort(data, 0x12);
            ModelGrpIndex = IO.ReadShort(data, 0x16);
            Unknown18 = IO.ReadShort(data, 0x18);
            Unknown1A = IO.ReadShort(data, 0x1A);
            Unknown1C = IO.ReadShort(data, 0x1C);
            Unknown20 = IO.ReadShort(data, 0x20);
        }
    }
    public class SgeIndexFileSection2Entry
    {
        public int Unknown00 { get; set; }
        public int Unknown04 { get; set; }

        public SgeIndexFileSection2Entry(byte[] data)
        {
            Unknown00 = IO.ReadInt(data, 0x00);
            Unknown04 = IO.ReadInt(data, 0x04);
        }
    }
}
