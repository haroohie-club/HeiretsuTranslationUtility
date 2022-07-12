using HaruhiHeiretsuLib.Strings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaruhiHeiretsuLib.Data
{
    public class ExtrasClfClaFile : DataFile, IDataStringsFile
    {
        public List<ClfEntry> Section1 { get; set; } = new();
        public List<ClaOutfitEntry> Section2 { get; set; } = new();
        public List<ClaCharacterEntry> Section3 { get; set; } = new();

        public override void Initialize(byte[] decompressedData, int offset)
        {
            base.Initialize(decompressedData, offset);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            int section1StartPointer = BitConverter.ToInt32(Data.Skip(0x0C).Take(4).Reverse().ToArray());
            int section1ItemCount = BitConverter.ToInt32(Data.Skip(0x10).Take(4).Reverse().ToArray());
            for (int i = 0; i < section1ItemCount; i++)
            {
                Section1.Add(new(Data, section1StartPointer + i * 0x24));
            }
            int section2StartPointer = BitConverter.ToInt32(Data.Skip(0x14).Take(4).Reverse().ToArray());
            int section2ItemCount = BitConverter.ToInt32(Data.Skip(0x18).Take(4).Reverse().ToArray());
            for (int i = 0; i < section2ItemCount; i++)
            {
                Section2.Add(new(Data, section2StartPointer + i * 0x2C));
            }
            int section3StartPointer = BitConverter.ToInt32(Data.Skip(0x1C).Take(4).Reverse().ToArray());
            int section3ItemCount = BitConverter.ToInt32(Data.Skip(0x20).Take(4).Reverse().ToArray());
            for (int i = 0; i < section3ItemCount; i++)
            {
                Section3.Add(new(Data, section3StartPointer + i * 0x34));
            }
        }

        public override byte[] GetBytes()
        {
            List<byte> bytes = new();
            List<byte> dataBytes = new();
            List<int> endPointers = new();

            bytes.AddRange(BitConverter.GetBytes(3).Reverse());
            bytes.AddRange(new byte[4]); // end pointer pointer, will be replaced later

            int startPointer = 12 + 8 * 3;
            bytes.AddRange(BitConverter.GetBytes(startPointer).Reverse());

            int section1StartPointer = startPointer + 0x20 * Section3.Count; // float section
            bytes.AddRange(BitConverter.GetBytes(section1StartPointer).Reverse());
            bytes.AddRange(BitConverter.GetBytes(Section1.Count).Reverse());
            List<byte> section1StringBytes = new();
            int section1StringsOffset = section1StartPointer + Section1.Count * 0x24;
            
            for (int i = 0; i < Section1.Count; i++)
            {
                (List<byte> entryDataBytes, List<byte> entryStringBytes) = Section1[i].GetBytes(section1StartPointer + i * 0x24, section1StringsOffset + section1StringBytes.Count, endPointers);
                dataBytes.AddRange(entryDataBytes);
                section1StringBytes.AddRange(entryStringBytes);
            }
            dataBytes.AddRange(section1StringBytes);

            int section2StartPointer = section1StartPointer + dataBytes.Count;
            bytes.AddRange(BitConverter.GetBytes(section2StartPointer).Reverse());
            bytes.AddRange(BitConverter.GetBytes(Section2.Count).Reverse());
            List<byte> section2StringBytes = new();
            int section2StringsOffset = section2StartPointer + Section2.Count * 0x2C;
            for (int i = 0; i < Section2.Count; i++)
            {
                (List<byte> entryDataBytes, List<byte> entryStringBytes) = Section2[i].GetBytes(section2StartPointer + i * 0x2C, section2StringsOffset + section2StringBytes.Count, endPointers);
                dataBytes.AddRange(entryDataBytes);
                section2StringBytes.AddRange(entryStringBytes);
            }
            dataBytes.AddRange(section2StringBytes);

            int section3StartPointer = section1StartPointer + dataBytes.Count;
            bytes.AddRange(BitConverter.GetBytes(section3StartPointer).Reverse());
            bytes.AddRange(BitConverter.GetBytes(Section3.Count).Reverse());
            List<byte> section3StringBytes = new();
            int section3StringsOffset = section3StartPointer + Section3.Count * 0x34;
            for (int i = 0; i < Section3.Count; i++)
            {
                (List<byte> entryDataBytes, List<byte> entryStringBytes, List<byte> entryFloatStructBytes) =
                    Section3[i].GetBytes(section3StartPointer + i * 0x34, section3StringsOffset + section3StringBytes.Count, bytes.Count, endPointers);
                dataBytes.AddRange(entryDataBytes);
                section3StringBytes.AddRange(entryStringBytes);
                bytes.AddRange(entryFloatStructBytes);
            }
            dataBytes.AddRange(section3StringBytes);

            bytes.AddRange(dataBytes);

            bytes.RemoveRange(4, 4);
            bytes.InsertRange(4, BitConverter.GetBytes(bytes.Count + 4).Reverse());
            bytes.AddRange(BitConverter.GetBytes(endPointers.Count).Reverse());
            foreach (int endPointer in endPointers)
            {
                bytes.AddRange(BitConverter.GetBytes(endPointer).Reverse());
            }

            bytes.AddRange(new byte[bytes.Count % 16 == 0 ? 0 : 16 - bytes.Count % 16]);

            return bytes.ToArray();
        }

        public List<DialogueLine> GetDialogueLines()
        {
            List<DialogueLine> lines = new();

            for (int i = 0; i < Section1.Count; i++)
            {
                lines.Add(new()
                {
                    Speaker = Section1[i].Speaker,
                    Line = Section1[i].Line,
                    Metadata = (new string[] { Section1[i].VoiceFile, "0", $"{i}", "0" }).ToList(),
                });
            }
            for (int i = 0; i < Section2.Count; i++)
            {
                lines.Add(new()
                {
                    Speaker = $"{Section2[i].OutfitOwner}'s {Section2[i].OutfitType} Description",
                    Line = Section2[i].OutfitDescription,
                    Metadata = (new string[] { Section2[i].OutfitFlag, "1", $"{i}", "0" }).ToList(),
                });
                lines.Add(new()
                {
                    Speaker = $"{Section2[i].Speaker1} (Describing {Section2[i].OutfitOwner}'s {Section2[i].OutfitType})",
                    Line = Section2[i].Line1,
                    Metadata = (new string[] { Section2[i].OutfitFlag, Section2[i].VoiceFile1, "1", $"{i}", "1" }).ToList(),
                });
                lines.Add(new()
                {
                    Speaker = $"{Section2[i].Speaker2} (Describing {Section2[i].OutfitOwner}'s {Section2[i].OutfitType})",
                    Line = Section2[i].Line2,
                    Metadata = (new string[] { Section2[i].OutfitFlag, Section2[i].VoiceFile2, "1", $"{i}", "2" }).ToList(),
                });
            }
            for (int i = 0; i < Section3.Count; i++)
            {
                lines.Add(new()
                {
                    Speaker = $"{Section3[i].Character} Name",
                    Line = Section3[i].CharacterName,
                    Metadata = (new string[] { Section3[i].CharacterFlag, "2", $"{i}", "0" }).ToList(),
                });
                lines.Add(new()
                {
                    Speaker = $"{Section3[i].Speaker1} (Describing {Section3[i].Character})",
                    Line = Section3[i].Line1,
                    Metadata = (new string[] { Section3[i].CharacterFlag, Section3[i].VoiceFile1, "2", $"{i}", "1" }).ToList(),
                });
                lines.Add(new()
                {
                    Speaker = $"{Section3[i].Speaker2} (Describing {Section3[i].Character})",
                    Line = Section3[i].Line2,
                    Metadata = (new string[] { Section3[i].CharacterFlag, Section3[i].VoiceFile2, "2", $"{i}", "1" }).ToList(),
                });
            }

            return lines;
        }

        public void ReplaceDialogueLine(DialogueLine line)
        {
            int section = int.Parse(line.Metadata[^3]);
            int itemIndex = int.Parse(line.Metadata[^2]);
            int lineIndex = int.Parse(line.Metadata[^1]);

            switch (section)
            {
                case 0:
                    Section1[itemIndex].Line = line.Line;
                    break;

                case 1:
                    switch (lineIndex)
                    {
                        case 0:
                            Section2[itemIndex].OutfitDescription = line.Line;
                            break;
                        case 1:
                            Section2[itemIndex].Line1 = line.Line;
                            break;
                        case 2:
                            Section2[itemIndex].Line2 = line.Line;
                            break;
                    }
                    break;

                case 2:
                    switch (lineIndex)
                    {
                        case 0:
                            Section3[itemIndex].CharacterName = line.Line;
                            break;
                        case 1:
                            Section3[itemIndex].Line1 = line.Line;
                            break;
                        case 2:
                            Section3[itemIndex].Line2 = line.Line;
                            break;
                    }
                    break;
            }
        }
    }

    // 0x24 bytes
    public class ClfEntry
    {
        public string Speaker { get; set; }
        public string Line { get; set; }
        public int Unknown08 { get; set; }
        public int Unknown0C { get; set; }
        public int Unknown10 { get; set; }
        public string VoiceFile { get; set; }
        public int Unknown18 { get; set; }
        public short Unknown1C { get; set; }
        public short Unknown1E { get; set; }
        public int Unknown20 { get; set; }

        public ClfEntry(IEnumerable<byte> data, int offset)
        {
            int speakerPointer = BitConverter.ToInt32(data.Skip(offset + 0x00).Take(4).Reverse().ToArray());
            Speaker = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(speakerPointer).TakeWhile(b => b != 0x00).ToArray());
            int linePointer = BitConverter.ToInt32(data.Skip(offset + 0x04).Take(4).Reverse().ToArray());
            Line = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(linePointer).TakeWhile(b => b != 0x00).ToArray());
            Unknown08 = BitConverter.ToInt32(data.Skip(offset + 0x08).Take(4).Reverse().ToArray());
            Unknown0C = BitConverter.ToInt32(data.Skip(offset + 0x0C).Take(4).Reverse().ToArray());
            Unknown10 = BitConverter.ToInt32(data.Skip(offset + 0x10).Take(4).Reverse().ToArray());
            int voiceFilePointer = BitConverter.ToInt32(data.Skip(offset + 0x14).Take(4).Reverse().ToArray());
            VoiceFile = Encoding.ASCII.GetString(data.Skip(voiceFilePointer).TakeWhile(b => b != 0x00).ToArray());
            Unknown18 = BitConverter.ToInt32(data.Skip(offset + 0x18).Take(4).Reverse().ToArray());
            Unknown1C = BitConverter.ToInt16(data.Skip(offset + 0x1C).Take(2).Reverse().ToArray());
            Unknown1E = BitConverter.ToInt16(data.Skip(offset + 0x1E).Take(2).Reverse().ToArray());
            Unknown20 = BitConverter.ToInt32(data.Skip(offset + 0x20).Take(4).Reverse().ToArray());
        }

        public (List<byte> dataBytes, List<byte> stringBytes) GetBytes(int offset, int stringsOffset, List<int> endPointers)
        {
            List<byte> dataBytes = new();
            List<byte> stringBytes = new();

            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(stringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(Speaker));
            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(stringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(Line));
            dataBytes.AddRange(BitConverter.GetBytes(Unknown08).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown0C).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown10).Reverse());
            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(stringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(VoiceFile));
            dataBytes.AddRange(BitConverter.GetBytes(Unknown18).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown1C).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown1E).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown20).Reverse());

            return (dataBytes, stringBytes);
        }
    }

    // 0x2C bytes
    public class ClaOutfitEntry
    {
        public string OutfitOwner { get; set; }
        public string OutfitDescription { get; set; }
        public string VoiceFile1 { get; set; }
        public string Speaker1 { get; set; }
        public string Line1 { get; set; }
        public string VoiceFile2 { get; set; }
        public string Speaker2 { get; set; }
        public string Line2 { get; set; }
        public short Unknown20 { get; set; }
        public short Unknown22 { get; set; }
        public string OutfitFlag { get; set; }
        public string OutfitType { get; set; }

        public ClaOutfitEntry(IEnumerable<byte> data, int offset)
        {
            int outfitOwnerOffset = BitConverter.ToInt32(data.Skip(offset + 0x00).Take(4).Reverse().ToArray());
            OutfitOwner = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(outfitOwnerOffset).TakeWhile(b => b != 0x00).ToArray());
            int outfitDescriptionOffset = BitConverter.ToInt32(data.Skip(offset + 0x04).Take(4).Reverse().ToArray());
            OutfitDescription = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(outfitDescriptionOffset).TakeWhile(b => b != 0x00).ToArray());
            int voiceFile1Offset = BitConverter.ToInt32(data.Skip(offset + 0x08).Take(4).Reverse().ToArray());
            VoiceFile1 = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(voiceFile1Offset).TakeWhile(b => b != 0x00).ToArray());
            int speaker1Offset = BitConverter.ToInt32(data.Skip(offset + 0x0C).Take(4).Reverse().ToArray());
            Speaker1 = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(speaker1Offset).TakeWhile(b => b != 0x00).ToArray());
            int line1Offset = BitConverter.ToInt32(data.Skip(offset + 0x10).Take(4).Reverse().ToArray());
            Line1 = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(line1Offset).TakeWhile(b => b != 0x00).ToArray());
            int voiceFile2Offset = BitConverter.ToInt32(data.Skip(offset + 0x14).Take(4).Reverse().ToArray());
            VoiceFile2 = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(voiceFile2Offset).TakeWhile(b => b != 0x00).ToArray());
            int speaker2Offset = BitConverter.ToInt32(data.Skip(offset + 0x18).Take(4).Reverse().ToArray());
            Speaker2 = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(speaker2Offset).TakeWhile(b => b != 0x00).ToArray());
            int line2Offset = BitConverter.ToInt32(data.Skip(offset + 0x1C).Take(4).Reverse().ToArray());
            Line2 = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(line2Offset).TakeWhile(b => b != 0x00).ToArray());
            Unknown20 = BitConverter.ToInt16(data.Skip(offset + 0x20).Take(2).Reverse().ToArray());
            Unknown22 = BitConverter.ToInt16(data.Skip(offset + 0x22).Take(2).Reverse().ToArray());
            int outfitFlagOffset = BitConverter.ToInt32(data.Skip(offset + 0x24).Take(4).Reverse().ToArray());
            OutfitFlag = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(outfitFlagOffset).TakeWhile(b => b != 0x00).ToArray());
            int outfitTypeOffset = BitConverter.ToInt32(data.Skip(offset + 0x28).Take(4).Reverse().ToArray());
            OutfitType = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(outfitTypeOffset).TakeWhile(b => b != 0x00).ToArray());
        }

        public (List<byte> dataBytes, List<byte> stringBytes) GetBytes(int offset, int stringsOffset, List<int> endPointers)
        {
            List<byte> dataBytes = new();
            List<byte> stringBytes = new();

            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(stringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(OutfitOwner));
            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(stringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(OutfitDescription));
            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(stringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(VoiceFile1));
            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(stringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(Speaker1));
            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(stringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(Line1));
            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(stringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(VoiceFile2));
            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(stringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(Speaker2));
            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(stringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(Line2));
            dataBytes.AddRange(BitConverter.GetBytes(Unknown20).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown22).Reverse());
            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(stringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(OutfitFlag));
            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(stringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(OutfitType));

            return (dataBytes, stringBytes);
        }
    }

    // 0x34 bytes
    public class ClaCharacterEntry
    {
        // 0x20 bytes
        public struct UnknownFloatStruct
        {
            public float Unknown00 { get; set; }
            public float Unknown04 { get; set; }
            public float Unknown08 { get; set; }
            public float Unknown0C { get; set; }
            public float Unknown10 { get; set; }
            public float Unknown14 { get; set; }
            public float Unknown18 { get; set; }
            public float Unknown1C { get; set; }

            public UnknownFloatStruct(IEnumerable<byte> data)
            {
                Unknown00 = BitConverter.ToSingle(data.Skip(0x00).Take(4).Reverse().ToArray());
                Unknown04 = BitConverter.ToSingle(data.Skip(0x04).Take(4).Reverse().ToArray());
                Unknown08 = BitConverter.ToSingle(data.Skip(0x08).Take(4).Reverse().ToArray());
                Unknown0C = BitConverter.ToSingle(data.Skip(0x0C).Take(4).Reverse().ToArray());
                Unknown10 = BitConverter.ToSingle(data.Skip(0x10).Take(4).Reverse().ToArray());
                Unknown14 = BitConverter.ToSingle(data.Skip(0x14).Take(4).Reverse().ToArray());
                Unknown18 = BitConverter.ToSingle(data.Skip(0x18).Take(4).Reverse().ToArray());
                Unknown1C = BitConverter.ToSingle(data.Skip(0x1C).Take(4).Reverse().ToArray());
            }

            public List<byte> GetBytes()
            {
                List<byte> bytes = new();

                bytes.AddRange(BitConverter.GetBytes(Unknown00).Reverse());
                bytes.AddRange(BitConverter.GetBytes(Unknown04).Reverse());
                bytes.AddRange(BitConverter.GetBytes(Unknown08).Reverse());
                bytes.AddRange(BitConverter.GetBytes(Unknown0C).Reverse());
                bytes.AddRange(BitConverter.GetBytes(Unknown10).Reverse());
                bytes.AddRange(BitConverter.GetBytes(Unknown14).Reverse());
                bytes.AddRange(BitConverter.GetBytes(Unknown18).Reverse());
                bytes.AddRange(BitConverter.GetBytes(Unknown1C).Reverse());

                return bytes;
            }
        }

        public string CharacterName { get; set; }
        public string CharacterFlag { get; set; }
        public string VoiceFile1 { get; set; }
        public string Speaker1 { get; set; }
        public string Line1 { get; set; }
        public string VoiceFile2 { get; set; }
        public string Speaker2 { get; set; }
        public string Line2 { get; set; }
        public string Character { get; set; }
        public UnknownFloatStruct FloatStruct { get; set; }
        public float UnknownFloat { get; set; }
        public int Unknown2C { get; set; }
        public int Unknown30 { get; set; }

        public ClaCharacterEntry(IEnumerable<byte> data, int offset)
        {
            int characterNameOffset = BitConverter.ToInt32(data.Skip(offset + 0x00).Take(4).Reverse().ToArray());
            CharacterName = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(characterNameOffset).TakeWhile(b => b != 0x00).ToArray());
            int characterFlagOffset = BitConverter.ToInt32(data.Skip(offset + 0x04).Take(4).Reverse().ToArray());
            CharacterFlag = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(characterFlagOffset).TakeWhile(b => b != 0x00).ToArray());
            int voiceFile1Offset = BitConverter.ToInt32(data.Skip(offset + 0x08).Take(4).Reverse().ToArray());
            VoiceFile1 = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(voiceFile1Offset).TakeWhile(b => b != 0x00).ToArray());
            int speaker1Offset = BitConverter.ToInt32(data.Skip(offset + 0x0C).Take(4).Reverse().ToArray());
            Speaker1 = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(speaker1Offset).TakeWhile(b => b != 0x00).ToArray());
            int line1Offset = BitConverter.ToInt32(data.Skip(offset + 0x10).Take(4).Reverse().ToArray());
            Line1 = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(line1Offset).TakeWhile(b => b != 0x00).ToArray());
            int voiceFile2Offset = BitConverter.ToInt32(data.Skip(offset + 0x14).Take(4).Reverse().ToArray());
            VoiceFile2 = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(voiceFile2Offset).TakeWhile(b => b != 0x00).ToArray());
            int speaker2Offset = BitConverter.ToInt32(data.Skip(offset + 0x18).Take(4).Reverse().ToArray());
            Speaker2 = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(speaker2Offset).TakeWhile(b => b != 0x00).ToArray());
            int line2Offset = BitConverter.ToInt32(data.Skip(offset + 0x1C).Take(4).Reverse().ToArray());
            Line2 = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(line2Offset).TakeWhile(b => b != 0x00).ToArray());
            int characterOffset = BitConverter.ToInt32(data.Skip(offset + 0x20).Take(4).Reverse().ToArray());
            Character = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(characterOffset).TakeWhile(b => b != 0x00).ToArray());
            int floatStructOffset = BitConverter.ToInt32(data.Skip(offset + 0x24).Take(4).Reverse().ToArray());
            FloatStruct = new(data.Skip(floatStructOffset).Take(0x20));
            UnknownFloat = BitConverter.ToSingle(data.Skip(offset + 0x28).Take(4).Reverse().ToArray());
            Unknown2C = BitConverter.ToInt32(data.Skip(offset + 0x2C).Take(4).Reverse().ToArray());
            Unknown30 = BitConverter.ToInt32(data.Skip(offset + 0x30).Take(4).Reverse().ToArray());
        }

        public (List<byte> dataBytes, List<byte> stringBytes, List<byte> floatStructBytes) GetBytes(int offset, int stringsOffset, int floatOffset, List<int> endPointers)
        {
            List<byte> dataBytes = new();
            List<byte> stringBytes = new();

            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(stringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(CharacterName));
            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(stringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(CharacterFlag));
            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(stringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(VoiceFile1));
            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(stringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(Speaker1));
            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(stringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(Line1));
            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(stringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(VoiceFile2));
            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(stringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(Speaker2));
            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(stringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(Line2));
            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(stringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(Character));
            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(floatOffset).Reverse());
            List<byte> floatStructBytes = FloatStruct.GetBytes();
            dataBytes.AddRange(BitConverter.GetBytes(UnknownFloat).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown2C).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown30).Reverse());

            return (dataBytes, stringBytes, floatStructBytes);
        }
    }
}
