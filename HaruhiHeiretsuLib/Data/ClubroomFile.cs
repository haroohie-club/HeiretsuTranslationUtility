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

            foreach (ClubroomThing clubroomThing in ClubroomThings)
            {

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
    }
}
