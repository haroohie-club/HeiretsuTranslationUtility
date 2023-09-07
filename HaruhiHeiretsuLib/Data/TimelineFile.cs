using HaruhiHeiretsuLib.Strings;
using HaruhiHeiretsuLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiHeiretsuLib.Data
{
    public class TimelineFile : DataFile, IDataStringsFile
    {
        public List<List<TimelineEntry>> TimelineSections { get; set; } = new();

        public override void Initialize(byte[] decompressedData, int offset)
        {
            base.Initialize(decompressedData, offset);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            int numSections = BitConverter.ToInt32(Data.Skip(0x00).Take(4).Reverse().ToArray());

            for (int i = 0; i < numSections; i++)
            {
                List<TimelineEntry> entries = new();
                int sectionStartPointer = BitConverter.ToInt32(Data.Skip(0x0C + i * 0x08).Take(4).Reverse().ToArray());
                int numEntries = BitConverter.ToInt32(Data.Skip(0x0C + i * 0x08 + 0x04).Take(4).Reverse().ToArray());

                for (int j = 0; j < numEntries; j++)
                {
                    entries.Add(new(Data, sectionStartPointer + j * 0x54));
                }

                TimelineSections.Add(entries);
            }
        }

        public override byte[] GetBytes()
        {
            List<byte> bytes = new();
            List<byte> dataBytes = new();
            List<int> endPointers = new();

            bytes.AddRange(BitConverter.GetBytes(TimelineSections.Count).Reverse());
            bytes.AddRange(new byte[4]); // end pointer pointer, will be replaced later

            int startPointer = 12 + 8 * (TimelineSections.Count);
            bytes.AddRange(BitConverter.GetBytes(startPointer).Reverse());
            int currentSectionPointer = startPointer;

            for (int i = 0; i <  TimelineSections.Count; i++)
            {
                List<byte> sectionStringBytes = new();
                int stringsSectionPointer = currentSectionPointer + 0x54 * TimelineSections[i].Count;

                bytes.AddRange(BitConverter.GetBytes(currentSectionPointer).Reverse());
                bytes.AddRange(BitConverter.GetBytes(TimelineSections[i].Count).Reverse());

                for (int j = 0; j < TimelineSections[i].Count; j++)
                {
                    (List<byte> entryDataBytes, List<byte> entryStringBytes) = TimelineSections[i][j].GetBytes(currentSectionPointer + j * 0x54, stringsSectionPointer + sectionStringBytes.Count, endPointers);
                    dataBytes.AddRange(entryDataBytes);
                    sectionStringBytes.AddRange(entryStringBytes);
                }
                dataBytes.AddRange(sectionStringBytes);
                currentSectionPointer = startPointer + dataBytes.Count;
            }
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
            List<DialogueLine> lines = new();

            int i = 0;
            foreach (List<TimelineEntry> section in TimelineSections)
            {
                int j = 0;
                foreach (TimelineEntry entry in section)
                {
                    string identifier = string.IsNullOrEmpty(entry.EntryId) ? entry.EntryFlag0 : entry.EntryId;

                    if (!string.IsNullOrEmpty(entry.EntryTitle))
                    {
                        lines.Add(new()
                        {
                            Speaker = $"{identifier} Title",
                            Line = entry.EntryTitle,
                            Metadata = (new string[3] { $"{i}", $"{j}", "0" }).ToList(),
                        });
                    }
                    lines.Add(new()
                    {
                        Speaker = $"{identifier} Description (1)",
                        Line = entry.EntryDescription,
                        Metadata = (new string[3] { $"{i}", $"{j}", "1" }).ToList(),
                    });
                    lines.Add(new()
                    {
                        Speaker = $"{identifier} Description (2)",
                        Line = entry.EntryDescription2,
                        Metadata = (new string[3] { $"{i}", $"{j}", "2" }).ToList(),
                    });
                    if (!string.IsNullOrEmpty(entry.EntryDescriptionCompleted))
                    {
                        lines.Add(new()
                        {
                            Speaker = $"{identifier} Description Completed (1)",
                            Line = entry.EntryDescriptionCompleted,
                            Metadata = (new string[3] { $"{i}", $"{j}", "3" }).ToList(),
                        });
                    }
                    if (!string.IsNullOrEmpty(entry.EntryDescriptionCompleted2))
                    {
                        lines.Add(new()
                        {
                            Speaker = $"{identifier} Description Completed (2)",
                            Line = entry.EntryDescriptionCompleted2,
                            Metadata = (new string[3] { $"{i}", $"{j}", "4" }).ToList(),
                        });
                    }
                    j++;
                }
                i++;
            }

            return lines;
        }

        public void ReplaceDialogueLine(DialogueLine line)
        {
            int locFirst = int.Parse(line.Metadata[0]);
            int locSecond = int.Parse(line.Metadata[1]);
            int locThird = int.Parse(line.Metadata[2]);

            int i = 0;
            foreach (List<TimelineEntry> section in TimelineSections)
            {
                int j = 0;
                foreach (TimelineEntry entry in section)
                {
                    if (i == locFirst && j == locSecond)
                    {
                        switch (locThird)
                        {
                            case 0:
                                entry.EntryTitle = line.Line;
                                break;
                            case 1:
                                entry.EntryDescription = line.Line;
                                break;
                            case 2:
                                entry.EntryDescription2 = line.Line;
                                break;
                            case 3:
                                entry.EntryDescriptionCompleted = line.Line;
                                break;
                            case 4:
                                entry.EntryDescriptionCompleted2 = line.Line;
                                break;
                        }
                    }
                }
            }
        }
    }

    public class TimelineEntry
    {
        public int Index { get; set; }
        public string EntryTitle { get; set; }
        public int Unknown08 { get; set; }
        public string EntryDescription { get; set; }
        public string EntryDescription2 { get; set; }
        public string EntryFlag0 { get; set; }
        public int Unknown18 { get; set; }
        public string EntryDescriptionCompleted { get; set; }
        public string EntryDescriptionCompleted2 { get; set; }
        public int Unknown24 { get; set; }
        public string EntryFlag1 { get; set; }
        public string EntryFlag2 { get; set; }
        public int Unknown30 { get; set; }
        public string EntryFlag3 { get; set; }
        public short Unknown38 { get; set; }
        public short Unknown3A { get; set; }
        public short Unknown3C { get; set; }
        public short Unknown3E { get; set; }
        public string EntryId { get; set; }
        public int Unknown44 { get; set; }
        public int Unknown48 { get; set; }
        public int Unknown4C { get; set; }
        public int Unknown50 { get; set; }

        public TimelineEntry(IEnumerable<byte> data, int offset)
        {
            Index = BitConverter.ToInt32(data.Skip(offset + 0x00).Take(4).Reverse().ToArray());
            int entryTitleOffset = BitConverter.ToInt32(data.Skip(offset + 0x04).Take(4).Reverse().ToArray());
            EntryTitle = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(entryTitleOffset).TakeWhile(b => b != 0x00).ToArray());
            Unknown08 = BitConverter.ToInt32(data.Skip(offset + 0x08).Take(4).Reverse().ToArray());
            int entryDescription1Offset = BitConverter.ToInt32(data.Skip(offset + 0x0C).Take(4).Reverse().ToArray());
            EntryDescription = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(entryDescription1Offset).TakeWhile(b => b != 0x00).ToArray());
            int entryDescription2Offset = BitConverter.ToInt32(data.Skip(offset + 0x10).Take(4).Reverse().ToArray());
            EntryDescription2 = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(entryDescription2Offset).TakeWhile(b => b != 0x00).ToArray());
            int entryFlag0Offset = BitConverter.ToInt32(data.Skip(offset + 0x14).Take(4).Reverse().ToArray());
            EntryFlag0 = Encoding.ASCII.GetString(data.Skip(entryFlag0Offset).TakeWhile(b => b != 0x00).ToArray());
            Unknown18 = BitConverter.ToInt32(data.Skip(offset + 0x18).Take(4).Reverse().ToArray());
            int entryDescriptionCompletedOffset = BitConverter.ToInt32(data.Skip(offset + 0x1C).Take(4).Reverse().ToArray());
            EntryDescriptionCompleted = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(entryDescriptionCompletedOffset).TakeWhile(b => b != 0x00).ToArray());
            int entryDescriptionCompleted2Offset = BitConverter.ToInt32(data.Skip(offset + 0x20).Take(4).Reverse().ToArray());
            EntryDescriptionCompleted2 = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(entryDescriptionCompleted2Offset).TakeWhile(b => b != 0x00).ToArray());
            Unknown24 = BitConverter.ToInt32(data.Skip(offset + 0x24).Take(4).Reverse().ToArray());
            int entryFlag1Offset = BitConverter.ToInt32(data.Skip(offset + 0x28).Take(4).Reverse().ToArray());
            EntryFlag1 = Encoding.ASCII.GetString(data.Skip(entryFlag1Offset).TakeWhile(b => b != 0x00).ToArray());
            int entryFlag2Offset = BitConverter.ToInt32(data.Skip(offset + 0x2C).Take(4).Reverse().ToArray());
            EntryFlag2 = Encoding.ASCII.GetString(data.Skip(entryFlag2Offset).TakeWhile(b => b != 0x00).ToArray());
            Unknown30 = BitConverter.ToInt32(data.Skip(offset + 0x30).Take(4).Reverse().ToArray());
            int entryFlag3Offset = BitConverter.ToInt32(data.Skip(offset + 0x34).Take(4).Reverse().ToArray());
            EntryFlag3 = Encoding.ASCII.GetString(data.Skip(entryFlag3Offset).TakeWhile(b => b != 0x00).ToArray());
            Unknown38 = BitConverter.ToInt16(data.Skip(offset + 0x38).Take(2).Reverse().ToArray());
            Unknown3A = BitConverter.ToInt16(data.Skip(offset + 0x3A).Take(2).Reverse().ToArray());
            Unknown3C = BitConverter.ToInt16(data.Skip(offset + 0x3C).Take(2).Reverse().ToArray());
            Unknown3E = BitConverter.ToInt16(data.Skip(offset + 0x3E).Take(2).Reverse().ToArray());
            int entryIdOffset = BitConverter.ToInt32(data.Skip(offset + 0x40).Take(4).Reverse().ToArray());
            EntryId = Encoding.ASCII.GetString(data.Skip(entryIdOffset).TakeWhile(b => b != 0x00).ToArray());
            Unknown44 = BitConverter.ToInt32(data.Skip(offset + 0x44).Take(4).ToArray());
            Unknown48 = BitConverter.ToInt32(data.Skip(offset + 0x48).Take(4).ToArray());
            Unknown4C = BitConverter.ToInt32(data.Skip(offset + 0x4C).Take(4).ToArray());
            Unknown50 = BitConverter.ToInt32(data.Skip(offset + 0x50).Take(4).ToArray());
        }

        public (List<byte> dataBytes, List<byte> stringBytes) GetBytes(int currentOffset, int currentStringsOffset, List<int> endPointers)
        {
            List<byte> dataBytes = new();
            List<byte> stringBytes = new();

            dataBytes.AddRange(BitConverter.GetBytes(Index).Reverse());
            if (!string.IsNullOrEmpty(EntryTitle))
            {
                endPointers.Add(currentOffset + dataBytes.Count);
                dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
                stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(EntryTitle));
            }
            else
            {
                dataBytes.AddRange(new byte[4]);
            }
            dataBytes.AddRange(BitConverter.GetBytes(Unknown08).Reverse());
            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(EntryDescription));
            if (!string.IsNullOrEmpty(EntryDescription2))
            {
                endPointers.Add(currentOffset + dataBytes.Count);
                dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
                stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(EntryDescription2));
            }
            else
            {
                dataBytes.AddRange(new byte[4]);
            }
            if (!string.IsNullOrEmpty(EntryFlag0))
            {
                endPointers.Add(currentOffset + dataBytes.Count);
                dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
                stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(EntryFlag0));
            }
            else
            {
                dataBytes.AddRange(new byte[4]);
            }
            dataBytes.AddRange(BitConverter.GetBytes(Unknown18).Reverse());
            if (!string.IsNullOrEmpty(EntryDescriptionCompleted))
            {
                endPointers.Add(currentOffset + dataBytes.Count);
                dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
                stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(EntryDescriptionCompleted));
            }
            else
            {
                dataBytes.AddRange(new byte[4]);
            }
            if (!string.IsNullOrEmpty(EntryDescriptionCompleted2))
            {
                endPointers.Add(currentOffset + dataBytes.Count);
                dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
                stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(EntryDescriptionCompleted2));
            }
            else
            {
                dataBytes.AddRange(new byte[4]);
            }
            dataBytes.AddRange(BitConverter.GetBytes(Unknown24).Reverse());
            if (!string.IsNullOrEmpty(EntryFlag1))
            {
                endPointers.Add(currentOffset + dataBytes.Count);
                dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
                stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(EntryFlag1));
            }
            else
            {
                dataBytes.AddRange(new byte[4]);
            }
            if (!string.IsNullOrEmpty(EntryFlag2))
            {
                endPointers.Add(currentOffset + dataBytes.Count);
                dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
                stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(EntryFlag2));
            }
            else
            {
                dataBytes.AddRange(new byte[4]);
            }
            dataBytes.AddRange(BitConverter.GetBytes(Unknown30).Reverse());
            if (!string.IsNullOrEmpty(EntryFlag3))
            {
                endPointers.Add(currentOffset + dataBytes.Count);
                dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
                stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(EntryFlag3));
            }
            else
            {
                dataBytes.AddRange(new byte[4]);
            }
            dataBytes.AddRange(BitConverter.GetBytes(Unknown38).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown3A).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown3C).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown3E).Reverse());
            if (!string.IsNullOrEmpty(EntryId))
            {
                endPointers.Add(currentOffset + dataBytes.Count);
                dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
                stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(EntryId));
            }
            else
            {
                dataBytes.AddRange(new byte[4]);
            }
            dataBytes.AddRange(BitConverter.GetBytes(Unknown44).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown48).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown4C).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown50).Reverse());

            return (dataBytes, stringBytes);
        }
    }
}
