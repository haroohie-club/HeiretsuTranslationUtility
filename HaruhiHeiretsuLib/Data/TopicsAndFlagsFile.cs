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
        public List<Topic> Topics { get; set; } = new();
        public Dictionary<FlagType, List<Flag>> Flags { get; set; } = new();
        public List<TopicReference> TopicReferences { get; set; } = new();

        public TopicsAndFlagsFile()
        {
            Name = "Topics and Flags File";
        }
        public override void Initialize(byte[] decompressedData, int offset)
        {
            base.Initialize(decompressedData, offset);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            int numSections = BitConverter.ToInt32(Data.Take(4).Reverse().ToArray());

            for (int i = 0; i < numSections; i++)
            {
                int sectionPointer = BitConverter.ToInt32(Data.Skip(0x0C + i * 8).Take(4).Reverse().ToArray());
                int sectionItemCount = BitConverter.ToInt32(Data.Skip(0x10 + i * 8).Take(4).Reverse().ToArray());

                if (i == 0) // we're in the initial topics section
                {
                    for (int j = 0; j < sectionItemCount; j++)
                    {
                        Topic topic = new();
                        topic.Index = BitConverter.ToInt32(Data.Skip(sectionPointer + j * 0x18).Take(4).Reverse().ToArray());
                        topic.Unknown04 = BitConverter.ToInt32(Data.Skip(sectionPointer + j * 0x18 + 0x04).Take(4).Reverse().ToArray());
                        int idOffset = BitConverter.ToInt32(Data.Skip(sectionPointer + j * 0x18 + 0x08).Take(4).Reverse().ToArray());
                        int nameOffset = BitConverter.ToInt32(Data.Skip(sectionPointer + j * 0x18 + 0x0C).Take(4).Reverse().ToArray());
                        int descriptionOffset = BitConverter.ToInt32(Data.Skip(sectionPointer + j * 0x18 + 0x10).Take(4).Reverse().ToArray());
                        topic.Unknown14 = BitConverter.ToInt16(Data.Skip(sectionPointer + j * 0x18 + 0x14).Take(2).Reverse().ToArray());
                        topic.Unknown16 = BitConverter.ToInt16(Data.Skip(sectionPointer + j * 0x18 + 0x16).Take(2).Reverse().ToArray());

                        topic.Id = Encoding.ASCII.GetString(Data.Skip(idOffset).TakeWhile(b => b != 0).ToArray());
                        topic.Name = Encoding.GetEncoding("Shift-JIS").GetString(Data.Skip(nameOffset).TakeWhile(b => b != 0).ToArray());
                        topic.Description = Encoding.GetEncoding("Shift-JIS").GetString(Data.Skip(descriptionOffset).TakeWhile(b => b != 0).ToArray());

                        Topics.Add(topic);
                    }
                }
                else if (i < 8)
                {
                    Flags.Add((FlagType)i, new());
                    for (int j = 0; j < sectionItemCount; j++)
                    {
                        Flag flag = new();
                        flag.Index = BitConverter.ToInt32(Data.Skip(sectionPointer + j * 0x08).Take(4).Reverse().ToArray());
                        int nameOffset = BitConverter.ToInt32(Data.Skip(sectionPointer + j * 0x08 + 4).Take(4).Reverse().ToArray());
                        flag.Name = Encoding.ASCII.GetString(Data.Skip(nameOffset).TakeWhile(b => b != 0).ToArray());
                        flag.Type = (FlagType)i;

                        Flags[flag.Type].Add(flag);
                    }
                }
                else
                {
                    for (int j = 0; j < sectionItemCount; j++)
                    {
                        TopicReferences.Add(new(Data.Skip(sectionPointer + j * 0x34).Take(0x34)));
                    }
                    int pointerArrayPointer = sectionPointer + sectionItemCount * 0x34;
                    foreach (TopicReference topicReference in TopicReferences)
                    {
                        for (int j = 0; j < topicReference.TopicOffsets.Length; j++)
                        {
                            if (topicReference.TopicOffsets[j] > 0)
                            {
                                topicReference.Topics[j] = Encoding.ASCII.GetString(Data.Skip(topicReference.TopicOffsets[j]).TakeWhile(b => b != 0).ToArray());
                            }
                        }
                    }
                }
            }
        }

        public override byte[] GetBytes()
        {
            List<byte> bytes = new();
            List<byte> dataBytes = new();
            List<int> endPointers = new();

            bytes.AddRange(BitConverter.GetBytes(Flags.Count + 2).Reverse());
            bytes.AddRange(new byte[4]); // end pointer pointer, will be replaced later

            int startPointer = 12 + 8 * (Flags.Count + 2);
            int currentSectionPointer = startPointer;

            bytes.AddRange(BitConverter.GetBytes(startPointer).Reverse());
            bytes.AddRange(BitConverter.GetBytes(currentSectionPointer).Reverse());
            bytes.AddRange(BitConverter.GetBytes(Topics.Count).Reverse());

            List<byte> topicStringsBytes = new();
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

                List<byte> flagStringsBytes = new();
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
            List<byte> topicReferenceStringBytes = new();
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
                dataBytes.AddRange(BitConverter.GetBytes(topicReference.Unknown18).Reverse().ToArray());
                dataBytes.AddRange(BitConverter.GetBytes(topicReference.Unknown1C).Reverse().ToArray());
                dataBytes.AddRange(BitConverter.GetBytes(topicReference.Unknown20).Reverse().ToArray());
                dataBytes.AddRange(BitConverter.GetBytes(topicReference.Unknown24).Reverse().ToArray());
                dataBytes.AddRange(BitConverter.GetBytes(topicReference.Unknown28).Reverse().ToArray());
                dataBytes.AddRange(BitConverter.GetBytes(topicReference.Unknown2C).Reverse().ToArray());
                dataBytes.AddRange(BitConverter.GetBytes(topicReference.Unknown30).Reverse().ToArray());
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

            return bytes.ToArray();
        }

        public List<DialogueLine> GetDialogueLines()
        {
            List<DialogueLine> lines = new();

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

        public TopicReference(IEnumerable<byte> data)
        {
            Index = BitConverter.ToInt32(data.Skip(0x00).Take(4).Reverse().ToArray());
            TopicOffsets[0] = BitConverter.ToInt32(data.Skip(0x04).Take(4).Reverse().ToArray());
            TopicOffsets[1] = BitConverter.ToInt32(data.Skip(0x08).Take(4).Reverse().ToArray());
            TopicOffsets[2] = BitConverter.ToInt32(data.Skip(0x0C).Take(4).Reverse().ToArray());
            TopicOffsets[3] = BitConverter.ToInt32(data.Skip(0x10).Take(4).Reverse().ToArray());
            TopicOffsets[4] = BitConverter.ToInt32(data.Skip(0x14).Take(4).Reverse().ToArray());
            Unknown18 = BitConverter.ToInt32(data.Skip(0x18).Take(4).Reverse().ToArray());
            Unknown1C = BitConverter.ToInt32(data.Skip(0x1C).Take(4).Reverse().ToArray());
            Unknown20 = BitConverter.ToInt32(data.Skip(0x20).Take(4).Reverse().ToArray());
            Unknown24 = BitConverter.ToInt32(data.Skip(0x24).Take(4).Reverse().ToArray());
            Unknown28 = BitConverter.ToInt32(data.Skip(0x28).Take(4).Reverse().ToArray());
            Unknown2C = BitConverter.ToInt32(data.Skip(0x2C).Take(4).Reverse().ToArray());
            Unknown30 = BitConverter.ToInt32(data.Skip(0x30).Take(4).Reverse().ToArray());
        }
    }
}
