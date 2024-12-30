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
        public List<List<TimelineEntry>> TimelineSections { get; set; } = [];

        public override void Initialize(byte[] decompressedData, int offset)
        {
            base.Initialize(decompressedData, offset);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            int numSections = IO.ReadInt(decompressedData, 0x00);

            for (int i = 0; i < numSections; i++)
            {
                List<TimelineEntry> entries = [];
                int sectionStartPointer = IO.ReadInt(decompressedData, 0x0C + i * 0x08);
                int numEntries = IO.ReadInt(decompressedData, 0x0C + i * 0x08 + 0x04);

                for (int j = 0; j < numEntries; j++)
                {
                    entries.Add(new(decompressedData, sectionStartPointer + j * 0x54));
                }

                TimelineSections.Add(entries);
            }
        }

        public override byte[] GetBytes()
        {
            List<byte> bytes = [];
            List<byte> dataBytes = [];
            List<int> endPointers = [];

            bytes.AddRange(BitConverter.GetBytes(TimelineSections.Count).Reverse());
            bytes.AddRange(new byte[4]); // end pointer pointer, will be replaced later

            int startPointer = 12 + 8 * (TimelineSections.Count);
            bytes.AddRange(BitConverter.GetBytes(startPointer).Reverse());
            int currentSectionPointer = startPointer;

            for (int i = 0; i <  TimelineSections.Count; i++)
            {
                List<byte> sectionStringBytes = [];
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

            return [.. bytes];
        }

        public List<DialogueLine> GetDialogueLines()
        {
            List<DialogueLine> lines = [];

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
                            Metadata = [.. (new string[3] { $"{i}", $"{j}", "0" })],
                        });
                    }
                    lines.Add(new()
                    {
                        Speaker = $"{identifier} Description (1)",
                        Line = entry.EntryDescription,
                        Metadata = [.. (new string[3] { $"{i}", $"{j}", "1" })],
                    });
                    lines.Add(new()
                    {
                        Speaker = $"{identifier} Description (2)",
                        Line = entry.EntryDescription2,
                        Metadata = [.. (new string[3] { $"{i}", $"{j}", "2" })],
                    });
                    if (!string.IsNullOrEmpty(entry.EntryDescriptionCompleted))
                    {
                        lines.Add(new()
                        {
                            Speaker = $"{identifier} Description Completed (1)",
                            Line = entry.EntryDescriptionCompleted,
                            Metadata = [.. (new string[3] { $"{i}", $"{j}", "3" })],
                        });
                    }
                    if (!string.IsNullOrEmpty(entry.EntryDescriptionCompleted2))
                    {
                        lines.Add(new()
                        {
                            Speaker = $"{identifier} Description Completed (2)",
                            Line = entry.EntryDescriptionCompleted2,
                            Metadata = [.. (new string[3] { $"{i}", $"{j}", "4" })],
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

        public TimelineEntry(byte[] data, int offset)
        {
            Index = IO.ReadInt(data, offset + 0x00);
            int entryTitleOffset = IO.ReadInt(data, offset + 0x04);
            EntryTitle = IO.ReadShiftJisString(data, entryTitleOffset);
            Unknown08 = IO.ReadInt(data, offset + 0x08);
            int entryDescription1Offset = IO.ReadInt(data, offset + 0x0C);
            EntryDescription = IO.ReadShiftJisString(data, entryDescription1Offset);
            int entryDescription2Offset = IO.ReadInt(data, offset + 0x10);
            EntryDescription2 = IO.ReadShiftJisString(data, entryDescription2Offset);
            int entryFlag0Offset = IO.ReadInt(data, offset + 0x14);
            EntryFlag0 = Encoding.ASCII.GetString(data.Skip(entryFlag0Offset).TakeWhile(b => b != 0x00).ToArray());
            Unknown18 = IO.ReadInt(data, offset + 0x18);
            int entryDescriptionCompletedOffset = IO.ReadInt(data, offset + 0x1C);
            EntryDescriptionCompleted = IO.ReadShiftJisString(data, entryDescriptionCompletedOffset);
            int entryDescriptionCompleted2Offset = IO.ReadInt(data, offset + 0x20);
            EntryDescriptionCompleted2 = IO.ReadShiftJisString(data, entryDescriptionCompleted2Offset);
            Unknown24 = IO.ReadInt(data, offset + 0x24);
            int entryFlag1Offset = IO.ReadInt(data, offset + 0x28);
            EntryFlag1 = Encoding.ASCII.GetString(data.Skip(entryFlag1Offset).TakeWhile(b => b != 0x00).ToArray());
            int entryFlag2Offset = IO.ReadInt(data, offset + 0x2C);
            EntryFlag2 = Encoding.ASCII.GetString(data.Skip(entryFlag2Offset).TakeWhile(b => b != 0x00).ToArray());
            Unknown30 = IO.ReadInt(data, offset + 0x30);
            int entryFlag3Offset = IO.ReadInt(data, offset + 0x34);
            EntryFlag3 = Encoding.ASCII.GetString(data.Skip(entryFlag3Offset).TakeWhile(b => b != 0x00).ToArray());
            Unknown38 = IO.ReadShort(data, offset + 0x38);
            Unknown3A = IO.ReadShort(data, offset + 0x3A);
            Unknown3C = IO.ReadShort(data, offset + 0x3C);
            Unknown3E = IO.ReadShort(data, offset + 0x3E);
            int entryIdOffset = IO.ReadInt(data, offset + 0x40);
            EntryId = Encoding.ASCII.GetString(data.Skip(entryIdOffset).TakeWhile(b => b != 0x00).ToArray());
            Unknown44 = IO.ReadInt(data, offset + 0x44);
            Unknown48 = IO.ReadInt(data, offset + 0x48);
            Unknown4C = IO.ReadInt(data, offset + 0x4C);
            Unknown50 = IO.ReadInt(data, offset + 0x50);
        }

        public (List<byte> dataBytes, List<byte> stringBytes) GetBytes(int currentOffset, int currentStringsOffset, List<int> endPointers)
        {
            List<byte> dataBytes = [];
            List<byte> stringBytes = [];

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
