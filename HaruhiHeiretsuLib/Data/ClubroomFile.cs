using HaruhiHeiretsuLib.Strings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaruhiHeiretsuLib.Data
{
    public class ClubroomFile : DataFile, IDataStringsFile
    {
        public List<ClubroomThing> ClubroomThings { get; set; } = new();
        public List<ClubroomThing2> ClubroomThing2s { get; set; } = new();

        public override void Initialize(byte[] decompressedData, int offset)
        {
            base.Initialize(decompressedData, offset);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            int clubroomThingsPointer = BitConverter.ToInt32(Data.Skip(0x0C).Take(4).Reverse().ToArray());
            int clubroomThingsCount = BitConverter.ToInt32(Data.Skip(0x10).Take(4).Reverse().ToArray());

            for (int i = 0; i < clubroomThingsCount; i++)
            {
                ClubroomThings.Add(new(Data, clubroomThingsPointer + 0x60 * i));
            }

            int clubroomThing2sPointer = BitConverter.ToInt32(Data.Skip(0x14).Take(4).Reverse().ToArray());
            int clubroomThing2sCount = BitConverter.ToInt32(Data.Skip(0x18).Take(4).Reverse().ToArray());

            for (int i = 0; i < clubroomThing2sCount; i++)
            {
                ClubroomThing2s.Add(new(Data, clubroomThing2sPointer + 0x24 * i));
            }
        }

        public override byte[] GetBytes()
        {
            List<byte> bytes = new();
            List<byte> dataBytes = new();
            List<int> endPointers = new();

            bytes.AddRange(BitConverter.GetBytes(2).Reverse());
            bytes.AddRange(new byte[4]); // end pointer pointer, will be replaced later

            int startPointer = 12 + 8 * 2;
            bytes.AddRange(BitConverter.GetBytes(startPointer).Reverse());
            bytes.AddRange(BitConverter.GetBytes(startPointer).Reverse());
            bytes.AddRange(BitConverter.GetBytes(ClubroomThings.Count).Reverse());

            List<byte> clubroomThingStringBytes = new();
            int clubroomStringsStartPointer = startPointer + ClubroomThings.Count * 0x60;
            for (int i = 0; i < ClubroomThings.Count; i++)
            {
                (List<byte> thingDataBytes, List<byte> thingStringBytes) = ClubroomThings[i].GetBytes(startPointer + dataBytes.Count, clubroomStringsStartPointer + clubroomThingStringBytes.Count, endPointers);
                dataBytes.AddRange(thingDataBytes);
                clubroomThingStringBytes.AddRange(thingStringBytes);
            }
            dataBytes.AddRange(clubroomThingStringBytes);

            int clubroomThings2StartPointer = startPointer + dataBytes.Count;
            bytes.AddRange(BitConverter.GetBytes(clubroomThings2StartPointer).Reverse());
            bytes.AddRange(BitConverter.GetBytes(ClubroomThing2s.Count).Reverse());

            List<byte> clubroomThing2StringBytes = new();
            int clubroom2StringsStartPointer = clubroomThings2StartPointer + ClubroomThing2s.Count * 0x24;
            for (int i = 0; i < ClubroomThing2s.Count; i++)
            {
                (List<byte> thing2DataBytes, List<byte> thing2StringBytes) = ClubroomThing2s[i].GetBytes(startPointer + dataBytes.Count, clubroom2StringsStartPointer + clubroomThing2StringBytes.Count, endPointers);
                dataBytes.AddRange(thing2DataBytes);
                clubroomThing2StringBytes.AddRange(thing2StringBytes);
            }
            dataBytes.AddRange(clubroomThing2StringBytes);

            bytes.AddRange(dataBytes);

            bytes.RemoveRange(4, 4);
            bytes.InsertRange(4, BitConverter.GetBytes(bytes.Count + 4).Reverse());

            bytes.AddRange(BitConverter.GetBytes(endPointers.Count).Reverse());
            foreach (int endPointer in endPointers)
            {
                bytes.AddRange(BitConverter.GetBytes(endPointer).Reverse());
            }

            return bytes.ToArray();
        }

        public List<DialogueLine> GetDialogueLines()
        {
            List<DialogueLine> dialogueLines = new();

            for (int i = 0; i < ClubroomThings.Count; i++)
            {
                dialogueLines.Add(new()
                {
                    Speaker = $"{ClubroomThings[i].Flag} Title",
                    Line = ClubroomThings[i].Title,
                    Offset = i,
                });
                dialogueLines.Add(new()
                {
                    Speaker = $"{ClubroomThings[i].Flag} Chapter",
                    Line = ClubroomThings[i].Chapter,
                    Offset = i,
                });
                dialogueLines.Add(new()
                {
                    Speaker = $"{ClubroomThings[i].Speaker1} ({ClubroomThings[i].Flag} 1)",
                    Line = ClubroomThings[i].Line1,
                    Offset = i,
                    Metadata = (new string[] { ClubroomThings[i].VoiceFile1 }).ToList(),
                });
                dialogueLines.Add(new()
                {
                    Speaker = $"{ClubroomThings[i].Speaker2} ({ClubroomThings[i].Flag} 2)",
                    Line = ClubroomThings[i].Line2,
                    Offset = i,
                    Metadata = (new string[] { ClubroomThings[i].VoiceFile2 }).ToList(),
                });
            }
            for (int i = 0; i < ClubroomThing2s.Count; i++)
            {
                dialogueLines.Add(new()
                {
                    Speaker = $"{ClubroomThing2s[i].Flag} Title",
                    Line = ClubroomThing2s[i].Title,
                    Offset = ClubroomThings.Count + i,
                });
                dialogueLines.Add(new()
                {
                    Speaker = $"{ClubroomThing2s[i].Speaker1} ({ClubroomThing2s[i].Flag} 1)",
                    Line = ClubroomThing2s[i].Line1,
                    Offset = ClubroomThings.Count + i,
                    Metadata = (new string[] { ClubroomThing2s[i].VoiceFile1 }).ToList(),
                });
                dialogueLines.Add(new()
                {
                    Speaker = $"{ClubroomThing2s[i].Speaker2} ({ClubroomThing2s[i].Flag} 2)",
                    Line = ClubroomThing2s[i].Line2,
                    Offset = ClubroomThings.Count + i,
                    Metadata = (new string[] { ClubroomThing2s[i].VoiceFile2 }).ToList(),
                });
            }

            return dialogueLines;
        }

        public void ReplaceDialogueLine(DialogueLine line)
        {
            if (line.Offset < ClubroomThings.Count)
            {
                if (line.Speaker.EndsWith("Title"))
                {
                    ClubroomThings[line.Offset].Title = line.Line;
                }
                else if (line.Speaker.EndsWith("Chapter"))
                {
                    ClubroomThings[line.Offset].Chapter = line.Line;
                }
                else if (line.Speaker.EndsWith("1)"))
                {
                    ClubroomThings[line.Offset].Line1 = line.Line;
                }
                else if (line.Speaker.EndsWith("2)"))
                {
                    ClubroomThings[line.Offset].Line2 = line.Line;
                }
            }
            else
            {
                if (line.Speaker.EndsWith("Title"))
                {
                    ClubroomThing2s[line.Offset].Title = line.Line;
                }
                else if (line.Speaker.EndsWith("1)"))
                {
                    ClubroomThing2s[line.Offset].Line1 = line.Line;
                }
                else if (line.Speaker.EndsWith("2)"))
                {
                    ClubroomThing2s[line.Offset].Line2 = line.Line;
                }
            }
        }
    }

    public class ClubroomThing
    {
        public string Title { get; set; }
        public string Chapter { get; set; }
        public string Flag { get; set; }
        public string VoiceFile1 { get; set; }
        public string Speaker1 { get; set; }
        public string Line1 { get; set; }
        public string VoiceFile2 { get; set; }
        public string Speaker2 { get; set; }
        public string Line2 { get; set; }
        public int Unknown24 { get; set; }
        public int Unknown28 { get; set; }
        public int Unknown2C { get; set; }
        public int Unknown30 { get; set; }
        public int Unknown34 { get; set; }
        public int Unknown38 { get; set; }
        public int Unknown3C { get; set; }
        public int Unknown40 { get; set; }
        public int Unknown44 { get; set; }
        public int Unknown48 { get; set; }
        public int Unknown4C { get; set; }
        public int Unknown50 { get; set; }
        public int Unknown54 { get; set; }
        public int Unknown58 { get; set; }
        public int Unknown5C { get; set; }

        public ClubroomThing(IEnumerable<byte> data, int offset)
        {
            int titleOffset = BitConverter.ToInt32(data.Skip(offset + 0x00).Take(4).Reverse().ToArray());
            Title = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(titleOffset).TakeWhile(b => b != 0x00).ToArray());
            int chapterOffset = BitConverter.ToInt32(data.Skip(offset + 0x04).Take(4).Reverse().ToArray());
            Chapter = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(chapterOffset).TakeWhile(b => b != 0x00).ToArray());
            int flagOffset = BitConverter.ToInt32(data.Skip(offset + 0x08).Take(4).Reverse().ToArray());
            Flag = Encoding.ASCII.GetString(data.Skip(flagOffset).TakeWhile(b => b != 0x00).ToArray());
            int voiceFile1Offset = BitConverter.ToInt32(data.Skip(offset + 0x0C).Take(4).Reverse().ToArray());
            VoiceFile1 = Encoding.ASCII.GetString(data.Skip(voiceFile1Offset).TakeWhile(b => b != 0x00).ToArray());
            int speaker1Offset = BitConverter.ToInt32(data.Skip(offset + 0x10).Take(4).Reverse().ToArray());
            Speaker1 = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(speaker1Offset).TakeWhile(b => b != 0x00).ToArray());
            int line1Offset = BitConverter.ToInt32(data.Skip(offset + 0x14).Take(4).Reverse().ToArray());
            Line1 = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(line1Offset).TakeWhile(b => b != 0x00).ToArray());
            int voiceFile2Offset = BitConverter.ToInt32(data.Skip(offset + 0x18).Take(4).Reverse().ToArray());
            VoiceFile2 = Encoding.ASCII.GetString(data.Skip(voiceFile2Offset).TakeWhile(b => b != 0x00).ToArray());
            int speaker2Offset = BitConverter.ToInt32(data.Skip(offset + 0x1C).Take(4).Reverse().ToArray());
            Speaker2 = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(speaker2Offset).TakeWhile(b => b != 0x00).ToArray());
            int line2Offset = BitConverter.ToInt32(data.Skip(offset + 0x20).Take(4).Reverse().ToArray());
            Line2 = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(line2Offset).TakeWhile(b => b != 0x00).ToArray());

            Unknown24 = BitConverter.ToInt32(data.Skip(offset + 0x24).Take(4).Reverse().ToArray());
            Unknown28 = BitConverter.ToInt32(data.Skip(offset + 0x28).Take(4).Reverse().ToArray());
            Unknown2C = BitConverter.ToInt32(data.Skip(offset + 0x2C).Take(4).Reverse().ToArray());
            Unknown30 = BitConverter.ToInt32(data.Skip(offset + 0x30).Take(4).Reverse().ToArray());
            Unknown34 = BitConverter.ToInt32(data.Skip(offset + 0x34).Take(4).Reverse().ToArray());
            Unknown38 = BitConverter.ToInt32(data.Skip(offset + 0x38).Take(4).Reverse().ToArray());
            Unknown3C = BitConverter.ToInt32(data.Skip(offset + 0x3C).Take(4).Reverse().ToArray());
            Unknown40 = BitConverter.ToInt32(data.Skip(offset + 0x40).Take(4).Reverse().ToArray());
            Unknown44 = BitConverter.ToInt32(data.Skip(offset + 0x44).Take(4).Reverse().ToArray());
            Unknown48 = BitConverter.ToInt32(data.Skip(offset + 0x48).Take(4).Reverse().ToArray());
            Unknown4C = BitConverter.ToInt32(data.Skip(offset + 0x4C).Take(4).Reverse().ToArray());
            Unknown50 = BitConverter.ToInt32(data.Skip(offset + 0x50).Take(4).Reverse().ToArray());
            Unknown54 = BitConverter.ToInt32(data.Skip(offset + 0x54).Take(4).Reverse().ToArray());
            Unknown58 = BitConverter.ToInt32(data.Skip(offset + 0x58).Take(4).Reverse().ToArray());
            Unknown5C = BitConverter.ToInt32(data.Skip(offset + 0x5C).Take(4).Reverse().ToArray());
        }

        public (List<byte> dataBytes, List<byte> stringBytes) GetBytes(int currentOffset, int currentStringsOffset, List<int> endPointers)
        {
            List<byte> dataBytes = new();
            List<byte> stringBytes = new();

            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(Title));
            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(Chapter));
            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(Flag));
            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(VoiceFile1));
            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(Speaker1));
            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(Line1));
            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(VoiceFile2));
            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(Speaker2));
            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(Line2));

            dataBytes.AddRange(BitConverter.GetBytes(Unknown24).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown28).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown2C).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown30).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown34).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown38).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown3C).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown40).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown44).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown48).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown4C).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown50).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown54).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown58).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown5C).Reverse());

            return (dataBytes, stringBytes);
        }
    }

    public class ClubroomThing2
    {
        public string Title { get; set; }
        public string Flag { get; set; }
        public string VoiceFile1 { get; set; }
        public string Speaker1 { get; set; }
        public string Line1 { get; set; }
        public string VoiceFile2 { get; set; }
        public string Speaker2 { get; set; }
        public string Line2 { get; set; }
        public int Unknown { get; set; }

        public ClubroomThing2(IEnumerable<byte> data, int offset)
        {
            int titleOffset = BitConverter.ToInt32(data.Skip(offset + 0x00).Take(4).Reverse().ToArray());
            Title = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(titleOffset).TakeWhile(b => b != 0x00).ToArray());
            int flagOffset = BitConverter.ToInt32(data.Skip(offset + 0x04).Take(4).Reverse().ToArray());
            Flag = Encoding.ASCII.GetString(data.Skip(flagOffset).TakeWhile(b => b != 0x00).ToArray());
            int voiceFile1Offset = BitConverter.ToInt32(data.Skip(offset + 0x08).Take(4).Reverse().ToArray());
            VoiceFile1 = Encoding.ASCII.GetString(data.Skip(voiceFile1Offset).TakeWhile(b => b != 0x00).ToArray());
            int speaker1Offset = BitConverter.ToInt32(data.Skip(offset + 0x0C).Take(4).Reverse().ToArray());
            Speaker1 = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(speaker1Offset).TakeWhile(b => b != 0x00).ToArray());
            int line1Offset = BitConverter.ToInt32(data.Skip(offset + 0x10).Take(4).Reverse().ToArray());
            Line1 = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(line1Offset).TakeWhile(b => b != 0x00).ToArray());
            int voiceFile2Offset = BitConverter.ToInt32(data.Skip(offset + 0x14).Take(4).Reverse().ToArray());
            VoiceFile2 = Encoding.ASCII.GetString(data.Skip(voiceFile2Offset).TakeWhile(b => b != 0x00).ToArray());
            int speaker2Offset = BitConverter.ToInt32(data.Skip(offset + 0x18).Take(4).Reverse().ToArray());
            Speaker2 = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(speaker2Offset).TakeWhile(b => b != 0x00).ToArray());
            int line2Offset = BitConverter.ToInt32(data.Skip(offset + 0x1C).Take(4).Reverse().ToArray());
            Line2 = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(line2Offset).TakeWhile(b => b != 0x00).ToArray());
            Unknown = BitConverter.ToInt32(data.Skip(offset + 0x20).Take(4).Reverse().ToArray());
        }

        public (List<byte> dataBytes, List<byte> stringBytes) GetBytes(int currentOffset, int currentStringsOffset, List<int> endPointers)
        {
            List<byte> dataBytes = new();
            List<byte> stringBytes = new();

            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(Title));
            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(Flag));
            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(VoiceFile1));
            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(Speaker1));
            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(Line1));
            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(VoiceFile2));
            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(Speaker2));
            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(Line2));
            dataBytes.AddRange(BitConverter.GetBytes(Unknown).Reverse());

            return (dataBytes, stringBytes);
        }
    }
}
