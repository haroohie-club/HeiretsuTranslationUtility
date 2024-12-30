using HaruhiHeiretsuLib.Strings;
using HaruhiHeiretsuLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiHeiretsuLib.Data
{
    /* This file is going to have some hardcoding bc it's easier that way, sue me
     * There are 9 sections, they are numbered as follows:
     * 1. Topics
     * 2. Flags
     * 3. Club flags
     * 4. Timeline flags
     * 5. Local flags
     * 6. Mpad (map) flags
     * 7. Inherit flags
     * 8. PVL flags
     * 9. Topics again, but only the character topics
    */
    public class TopicsAndFlagsFile : DataFile, IDataStringsFile
    {
        public List<Topic> Topics { get; set; } = [];
        public Dictionary<FlagType, List<Flag>> Flags { get; set; } = [];
        public List<TopicReference> TopicReferences { get; set; } = [];

        public TopicsAndFlagsFile()
        {
            Name = "Topics and Flags File";
        }
        public override void Initialize(byte[] decompressedData, int offset)
        {
            base.Initialize(decompressedData, offset);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            int numSections = IO.ReadInt(decompressedData, 0x00);

            for (int i = 0; i < numSections; i++)
            {
                int sectionPointer = IO.ReadInt(decompressedData, 0x0C + i * 8);
                int sectionItemCount = IO.ReadInt(decompressedData, 0x10 + i * 8);

                if (i == 0) // we're in the initial topics section
                {
                    for (int j = 0; j < sectionItemCount; j++)
                    {
                        Topic topic = new()
                        {
                            Index = IO.ReadInt(decompressedData, sectionPointer + j * 0x18),
                            Unknown04 = IO.ReadInt(decompressedData, sectionPointer + j * 0x18 + 0x04),
                        };
                        int idOffset = IO.ReadInt(decompressedData, sectionPointer + j * 0x18 + 0x08);
                        int nameOffset = IO.ReadInt(decompressedData, sectionPointer + j * 0x18 + 0x0C);
                        int descriptionOffset = IO.ReadInt(decompressedData, sectionPointer + j * 0x18 + 0x10);
                        topic.Unknown14 = IO.ReadShort(decompressedData, sectionPointer + j * 0x18 + 0x14);
                        topic.Unknown16 = IO.ReadShort(decompressedData, sectionPointer + j * 0x18 + 0x16);

                        topic.Id = IO.ReadAsciiString(decompressedData, idOffset);
                        topic.Name = IO.ReadShiftJisString(decompressedData, nameOffset);
                        topic.Description = IO.ReadShiftJisString(decompressedData, descriptionOffset);

                        Topics.Add(topic);
                    }
                }
                else if (i < 8)
                {
                    Flags.Add((FlagType)i, []);
                    for (int j = 0; j < sectionItemCount; j++)
                    {
                        Flag flag = new();
                        flag.Index = IO.ReadInt(decompressedData, sectionPointer + j * 0x08);
                        int nameOffset = IO.ReadInt(decompressedData, sectionPointer + j * 0x08 + 4);
                        flag.Name = IO.ReadAsciiString(decompressedData, nameOffset);
                        flag.Type = (FlagType)i;

                        Flags[flag.Type].Add(flag);
                    }
                }
                else
                {
                    for (int j = 0; j < sectionItemCount; j++)
                    {
                        TopicReferences.Add(new(decompressedData[(sectionPointer + j * 0x34)..(sectionPointer + (j + 1) * 0x34)]));
                    }
                    int pointerArrayPointer = sectionPointer + sectionItemCount * 0x34;
                    foreach (TopicReference topicReference in TopicReferences)
                    {
                        for (int j = 0; j < topicReference.TopicOffsets.Length; j++)
                        {
                            if (topicReference.TopicOffsets[j] > 0)
                            {
                                topicReference.Topics[j] = IO.ReadAsciiString(decompressedData, topicReference.TopicOffsets[j]);
                            }
                        }
                    }
                }
            }
        }

        public override byte[] GetBytes()
        {
            List<byte> bytes = [];
            List<byte> dataBytes = [];
            List<int> endPointers = [];

            bytes.AddRange(BitConverter.GetBytes(Flags.Count + 2).Reverse());
            bytes.AddRange(new byte[4]); // end pointer pointer, will be replaced later

            int startPointer = 12 + 8 * (Flags.Count + 2);
            int currentSectionPointer = startPointer;

            bytes.AddRange(BitConverter.GetBytes(startPointer).Reverse());
            bytes.AddRange(BitConverter.GetBytes(currentSectionPointer).Reverse());
            bytes.AddRange(BitConverter.GetBytes(Topics.Count).Reverse());

            List<byte> topicStringsBytes = [];
            int topicStringsSectionPointer = currentSectionPointer + 0x18 * Topics.Count;
            foreach (Topic topic in Topics)
            {
                dataBytes.AddRange(BitConverter.GetBytes(topic.Index).Reverse());
                dataBytes.AddRange(BitConverter.GetBytes(topic.Unknown04).Reverse());

                endPointers.Add(startPointer + dataBytes.Count);
                dataBytes.AddRange(BitConverter.GetBytes(topicStringsSectionPointer + topicStringsBytes.Count).Reverse());
                topicStringsBytes.AddRange(Helpers.GetPaddedByteArrayFromString(topic.Id));

                endPointers.Add(startPointer + dataBytes.Count);
                dataBytes.AddRange(BitConverter.GetBytes(topicStringsSectionPointer + topicStringsBytes.Count).Reverse());
                topicStringsBytes.AddRange(Helpers.GetPaddedByteArrayFromString(topic.Name));

                endPointers.Add(startPointer + dataBytes.Count);
                dataBytes.AddRange(BitConverter.GetBytes(topicStringsSectionPointer + topicStringsBytes.Count).Reverse());
                topicStringsBytes.AddRange(Helpers.GetPaddedByteArrayFromString(topic.Description));

                dataBytes.AddRange(BitConverter.GetBytes(topic.Unknown14).Reverse());
                dataBytes.AddRange(BitConverter.GetBytes(topic.Unknown16).Reverse());
            }
            dataBytes.AddRange(topicStringsBytes);
            currentSectionPointer = startPointer + dataBytes.Count;

            foreach (FlagType flagType in Flags.Keys)
            {
                bytes.AddRange(BitConverter.GetBytes(currentSectionPointer).Reverse());
                bytes.AddRange(BitConverter.GetBytes(Flags[flagType].Count).Reverse());

                List<byte> flagStringsBytes = [];
                int flagStringsSectionPointer = currentSectionPointer + 0x08 * Flags[flagType].Count;

                foreach (Flag flag in Flags[flagType])
                {
                    dataBytes.AddRange(BitConverter.GetBytes(flag.Index).Reverse());

                    endPointers.Add(startPointer + dataBytes.Count);
                    dataBytes.AddRange(BitConverter.GetBytes(flagStringsSectionPointer + flagStringsBytes.Count).Reverse());
                    flagStringsBytes.AddRange(Helpers.GetPaddedByteArrayFromString(flag.Name));
                }

                dataBytes.AddRange(flagStringsBytes);
                currentSectionPointer = startPointer + dataBytes.Count;
            }

            bytes.AddRange(BitConverter.GetBytes(currentSectionPointer).Reverse());
            bytes.AddRange(BitConverter.GetBytes(TopicReferences.Count).Reverse());
            List<byte> topicReferenceStringBytes = [];
            int topicReferenceStringSectionPointer = currentSectionPointer + 0x34 * TopicReferences.Count;
            foreach (TopicReference topicReference in TopicReferences)
            {
                dataBytes.AddRange(BitConverter.GetBytes(topicReference.Index).Reverse().ToArray());
                for (int i = 0; i < topicReference.Topics.Length; i++)
                {
                    if (!string.IsNullOrEmpty(topicReference.Topics[i]))
                    {
                        endPointers.Add(startPointer + dataBytes.Count);
                        topicReference.TopicOffsets[i] = topicReferenceStringSectionPointer + topicReferenceStringBytes.Count;
                        dataBytes.AddRange(BitConverter.GetBytes(topicReference.TopicOffsets[i]).Reverse().ToArray());
                        topicReferenceStringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(topicReference.Topics[i]));
                    }
                    else
                    {
                        dataBytes.AddRange(new byte[4]);
                    }
                }
                dataBytes.AddRange(BitConverter.GetBytes(topicReference.Unknown18).Reverse());
                dataBytes.AddRange(BitConverter.GetBytes(topicReference.Unknown1C).Reverse());
                dataBytes.AddRange(BitConverter.GetBytes(topicReference.Unknown20).Reverse());
                dataBytes.AddRange(BitConverter.GetBytes(topicReference.Unknown24).Reverse());
                dataBytes.AddRange(BitConverter.GetBytes(topicReference.Unknown28).Reverse());
                dataBytes.AddRange(BitConverter.GetBytes(topicReference.Unknown2C).Reverse());
                dataBytes.AddRange(BitConverter.GetBytes(topicReference.Unknown30).Reverse());
            }
            dataBytes.AddRange(topicReferenceStringBytes);
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

            foreach (Topic topic in Topics)
            {
                lines.Add(new()
                {
                    Line = topic.Name,
                    Speaker = $"{topic.Id} Name",
                });
                lines.Add(new()
                {
                    Line = topic.Description,
                    Speaker = $"{topic.Id} Description"
                });
            }

            return lines;
        }

        public void ReplaceDialogueLine(DialogueLine line)
        {
            string[] topicSplit = line.Speaker.Split(' ');
            string topicId = topicSplit[0];
            string type = topicSplit[1];

            for (int i = 0; i < Topics.Count; i++)
            {
                if (Topics[i].Id == topicId)
                {
                    if (type == "Name")
                    {
                        Topics[i].Name = line.Line;
                    }
                    else
                    {
                        Topics[i].Description = line.Line;
                    }
                }
            }
        }
    }

    public class Topic
    {
        public int Index { get; set; }
        public int Unknown04 { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public short Unknown14 { get; set; }
        public short Unknown16 { get; set; }
    }

    public class Flag
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public FlagType Type { get; set; }
    }

    public enum FlagType
    {
        FLAG = 1,
        PVL = 2,
        CLUB = 3,
        TIMELINE = 4,
        MAP = 5,
        LOCAL = 6,
        INHERIT = 7,
    }

    public class TopicReference
    {
        public string[] Topics { get; set; } = new string[5];

        public int Index { get; set; }
        public int[] TopicOffsets { get; set; } = new int[5];
        public int Unknown18 { get; set; }
        public int Unknown1C { get; set; }
        public int Unknown20 { get; set; }
        public int Unknown24 { get; set; }
        public int Unknown28 { get; set; }
        public int Unknown2C { get; set; }
        public int Unknown30 { get; set; }

        public TopicReference(byte[] data)
        {
            Index = IO.ReadInt(data, 0x00);
            TopicOffsets[0] = IO.ReadInt(data, 0x04);
            TopicOffsets[1] = IO.ReadInt(data, 0x08);
            TopicOffsets[2] = IO.ReadInt(data, 0x0C);
            TopicOffsets[3] = IO.ReadInt(data, 0x10);
            TopicOffsets[4] = IO.ReadInt(data, 0x14);
            Unknown18 = IO.ReadInt(data, 0x18);
            Unknown1C = IO.ReadInt(data, 0x1C);
            Unknown20 = IO.ReadInt(data, 0x20);
            Unknown24 = IO.ReadInt(data, 0x24);
            Unknown28 = IO.ReadInt(data, 0x28);
            Unknown2C = IO.ReadInt(data, 0x2C);
            Unknown30 = IO.ReadInt(data, 0x30);
        }
    }
}
