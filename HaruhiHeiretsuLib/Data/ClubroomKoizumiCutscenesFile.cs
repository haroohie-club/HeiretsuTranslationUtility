using HaruhiHeiretsuLib.Strings;
using HaruhiHeiretsuLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiHeiretsuLib.Data
{
    /// <summary>
    /// A representation of the Koizumi clubroom cutscene file file (dat.bin 71/72)
    /// </summary>
    public class ClubroomKoizumiCutscenesFile : DataFile, IDataStringsFile
    {
        /// <summary>
        /// A list of clubroom cutscenes
        /// </summary>
        public List<ClubroomCutscene> ClubroomCutscenes { get; set; } = [];
        /// <summary>
        /// A list representing the story chapter list you are initially presented with when talking to Koizumi in the clubroom
        /// </summary>
        public List<ClubroomChapter> ClubroomChapters { get; set; } = [];

        /// <summary>
        /// Simple constructor
        /// </summary>
        public ClubroomKoizumiCutscenesFile()
        {
            Name = "Clubroom Koizumi Cutscene File";
        }

        /// <inheritdoc/>
        public override void Initialize(byte[] decompressedData, int offset)
        {
            base.Initialize(decompressedData, offset);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            int clubroomThingsPointer = BitConverter.ToInt32(Data.Skip(0x0C).Take(4).Reverse().ToArray());
            int clubroomThingsCount = BitConverter.ToInt32(Data.Skip(0x10).Take(4).Reverse().ToArray());

            for (int i = 0; i < clubroomThingsCount; i++)
            {
                ClubroomCutscenes.Add(new(Data, clubroomThingsPointer + 0x60 * i));
            }

            int clubroomThing2sPointer = BitConverter.ToInt32(Data.Skip(0x14).Take(4).Reverse().ToArray());
            int clubroomThing2sCount = BitConverter.ToInt32(Data.Skip(0x18).Take(4).Reverse().ToArray());

            for (int i = 0; i < clubroomThing2sCount; i++)
            {
                ClubroomChapters.Add(new(Data, clubroomThing2sPointer + 0x24 * i));
            }
        }

        /// <inheritdoc/>
        public override byte[] GetBytes()
        {
            List<byte> bytes = [];
            List<byte> dataBytes = [];
            List<int> endPointers = [];

            bytes.AddRange(BitConverter.GetBytes(2).Reverse());
            bytes.AddRange(new byte[4]); // end pointer pointer, will be replaced later

            int startPointer = 12 + 8 * 2;
            bytes.AddRange(BitConverter.GetBytes(startPointer).Reverse());
            bytes.AddRange(BitConverter.GetBytes(startPointer).Reverse());
            bytes.AddRange(BitConverter.GetBytes(ClubroomCutscenes.Count).Reverse());

            List<byte> clubroomThingStringBytes = [];
            int clubroomStringsStartPointer = startPointer + ClubroomCutscenes.Count * 0x60;
            for (int i = 0; i < ClubroomCutscenes.Count; i++)
            {
                (List<byte> thingDataBytes, List<byte> thingStringBytes) = ClubroomCutscenes[i].GetBytes(startPointer + dataBytes.Count, clubroomStringsStartPointer + clubroomThingStringBytes.Count, endPointers);
                dataBytes.AddRange(thingDataBytes);
                clubroomThingStringBytes.AddRange(thingStringBytes);
            }
            dataBytes.AddRange(clubroomThingStringBytes);

            int clubroomThings2StartPointer = startPointer + dataBytes.Count;
            bytes.AddRange(BitConverter.GetBytes(clubroomThings2StartPointer).Reverse());
            bytes.AddRange(BitConverter.GetBytes(ClubroomChapters.Count).Reverse());

            List<byte> clubroomThing2StringBytes = [];
            int clubroom2StringsStartPointer = clubroomThings2StartPointer + ClubroomChapters.Count * 0x24;
            for (int i = 0; i < ClubroomChapters.Count; i++)
            {
                (List<byte> thing2DataBytes, List<byte> thing2StringBytes) = ClubroomChapters[i].GetBytes(startPointer + dataBytes.Count, clubroom2StringsStartPointer + clubroomThing2StringBytes.Count, endPointers);
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

            return [.. bytes];
        }

        /// <inheritdoc/>
        public List<DialogueLine> GetDialogueLines()
        {
            List<DialogueLine> dialogueLines = [];

            for (int i = 0; i < ClubroomCutscenes.Count; i++)
            {
                dialogueLines.Add(new()
                {
                    Speaker = $"{ClubroomCutscenes[i].Flag} (EVT {ClubroomCutscenes[i].EventIndex}) Title",
                    Line = ClubroomCutscenes[i].Title,
                    Offset = i,
                });
                dialogueLines.Add(new()
                {
                    Speaker = $"{ClubroomCutscenes[i].Flag} (EVT {ClubroomCutscenes[i].EventIndex}) Chapter",
                    Line = ClubroomCutscenes[i].Chapter,
                    Offset = i,
                });
                dialogueLines.Add(new()
                {
                    Speaker = $"{ClubroomCutscenes[i].HoverSpeaker} (EVT {ClubroomCutscenes[i].EventIndex}, {ClubroomCutscenes[i].Flag} Hover Description)",
                    Line = ClubroomCutscenes[i].HoverLine,
                    Offset = i,
                    Metadata = [.. (new string[] { ClubroomCutscenes[i].HoverVoiceFile })],
                });
                dialogueLines.Add(new()
                {
                    Speaker = $"{ClubroomCutscenes[i].SelectedSpeaker} (EVT {ClubroomCutscenes[i].EventIndex}, {ClubroomCutscenes[i].Flag} Selected Description)",
                    Line = ClubroomCutscenes[i].SelectedLine,
                    Offset = i,
                    Metadata = [.. (new string[] { ClubroomCutscenes[i].SelectedVoiceFile })],
                });
            }
            for (int i = 0; i < ClubroomChapters.Count; i++)
            {
                dialogueLines.Add(new()
                {
                    Speaker = $"{ClubroomChapters[i].Flag} Title",
                    Line = ClubroomChapters[i].Title,
                    Offset = ClubroomCutscenes.Count + i,
                });
                dialogueLines.Add(new()
                {
                    Speaker = $"{ClubroomChapters[i].HoverSpeaker} ({ClubroomChapters[i].Flag} Hover Description)",
                    Line = ClubroomChapters[i].HoverLine,
                    Offset = ClubroomCutscenes.Count + i,
                    Metadata = [.. (new string[] { ClubroomChapters[i].HoverVoiceFile })],
                });
                dialogueLines.Add(new()
                {
                    Speaker = $"{ClubroomChapters[i].SelectedSpeaker} ({ClubroomChapters[i].Flag} Selected Description)",
                    Line = ClubroomChapters[i].SelectedLine,
                    Offset = ClubroomCutscenes.Count + i,
                    Metadata = [.. (new string[] { ClubroomChapters[i].SelectedVoiceFile })],
                });
            }

            return dialogueLines;
        }

        /// <inheritdoc/>
        public void ReplaceDialogueLine(DialogueLine line)
        {
            if (line.Offset < ClubroomCutscenes.Count)
            {
                if (line.Speaker.EndsWith("Title"))
                {
                    ClubroomCutscenes[line.Offset].Title = line.Line;
                }
                else if (line.Speaker.EndsWith("Chapter"))
                {
                    ClubroomCutscenes[line.Offset].Chapter = line.Line;
                }
                else if (line.Speaker.EndsWith("Hover Description)"))
                {
                    ClubroomCutscenes[line.Offset].HoverLine = line.Line;
                }
                else if (line.Speaker.EndsWith("Selected Description)"))
                {
                    ClubroomCutscenes[line.Offset].SelectedLine = line.Line;
                }
            }
            else
            {
                if (line.Speaker.EndsWith("Title"))
                {
                    ClubroomChapters[line.Offset].Title = line.Line;
                }
                else if (line.Speaker.EndsWith("Hover Description)"))
                {
                    ClubroomChapters[line.Offset].HoverLine = line.Line;
                }
                else if (line.Speaker.EndsWith("Selected Description)"))
                {
                    ClubroomChapters[line.Offset].SelectedLine = line.Line;
                }
            }
        }
    }

    /// <summary>
    /// An object representing a watchable cutscene in the clubroom when talking to Koizumi
    /// </summary>
    public class ClubroomCutscene
    {
        /// <summary>
        /// The title of the cutscene as displayed in the selection menu
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// The chapter that contains this cutscene
        /// </summary>
        public string Chapter { get; set; }
        /// <summary>
        /// The flag that, when set, unlocks viewing this cutscene
        /// </summary>
        public string Flag { get; set; }
        /// <summary>
        /// The voice file that plays when hovering over this cutscene in the selection menu
        /// </summary>
        public string HoverVoiceFile { get; set; }
        /// <summary>
        /// The speaker of the line displayed when hovering over this cutscene in the selection menu
        /// </summary>
        public string HoverSpeaker { get; set; }
        /// <summary>
        /// The line that is displayed when hovering over this cutscene in the selection menu
        /// </summary>
        public string HoverLine { get; set; }
        /// <summary>
        /// The voice file that plays when this cutscene is selected in the selection menu
        /// </summary>
        public string SelectedVoiceFile { get; set; }
        /// <summary>
        /// The speaker of the line displayed when this cutscene is selected in the selection menu
        /// </summary>
        public string SelectedSpeaker { get; set; }
        /// <summary>
        /// The line that is displayed when this cutscene is selected in the selection menu
        /// </summary>
        public string SelectedLine { get; set; }
        /// <summary>
        /// The caller parent of the cutscene's map as defined in the map definitions file
        /// </summary>
        public int MapCallerParent { get; set; }
        /// <summary>
        /// The caller child of the cutscene's map as defined in the map defintions file
        /// </summary>
        public int MapCallerChild { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown2C { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown30 { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown34 { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public short Unknown38 { get; set; }
        /// <summary>
        /// The index of the event (cutscene) to play
        /// </summary>
        public short EventIndex { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown3C { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown40 { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown44 { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown48 { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown4C { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown50 { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown54 { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown58 { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown5C { get; set; }

        /// <summary>
        /// Creates a cutscene object from the Koizumi clubroom cutscene file data and the offset of the current cutscene
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        public ClubroomCutscene(IEnumerable<byte> data, int offset)
        {
            int titleOffset = BitConverter.ToInt32(data.Skip(offset + 0x00).Take(4).Reverse().ToArray());
            Title = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(titleOffset).TakeWhile(b => b != 0x00).ToArray());
            int chapterOffset = BitConverter.ToInt32(data.Skip(offset + 0x04).Take(4).Reverse().ToArray());
            Chapter = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(chapterOffset).TakeWhile(b => b != 0x00).ToArray());
            int flagOffset = BitConverter.ToInt32(data.Skip(offset + 0x08).Take(4).Reverse().ToArray());
            Flag = Encoding.ASCII.GetString(data.Skip(flagOffset).TakeWhile(b => b != 0x00).ToArray());
            int hoverVoiceFileOffset = BitConverter.ToInt32(data.Skip(offset + 0x0C).Take(4).Reverse().ToArray());
            HoverVoiceFile = Encoding.ASCII.GetString(data.Skip(hoverVoiceFileOffset).TakeWhile(b => b != 0x00).ToArray());
            int hoverSpeakerOffset = BitConverter.ToInt32(data.Skip(offset + 0x10).Take(4).Reverse().ToArray());
            HoverSpeaker = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(hoverSpeakerOffset).TakeWhile(b => b != 0x00).ToArray());
            int hoverLineOffset = BitConverter.ToInt32(data.Skip(offset + 0x14).Take(4).Reverse().ToArray());
            HoverLine = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(hoverLineOffset).TakeWhile(b => b != 0x00).ToArray());
            int selectedVoiceFileOffset = BitConverter.ToInt32(data.Skip(offset + 0x18).Take(4).Reverse().ToArray());
            SelectedVoiceFile = Encoding.ASCII.GetString(data.Skip(selectedVoiceFileOffset).TakeWhile(b => b != 0x00).ToArray());
            int selectedSpeakerOffset = BitConverter.ToInt32(data.Skip(offset + 0x1C).Take(4).Reverse().ToArray());
            SelectedSpeaker = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(selectedSpeakerOffset).TakeWhile(b => b != 0x00).ToArray());
            int selecteLineOffset = BitConverter.ToInt32(data.Skip(offset + 0x20).Take(4).Reverse().ToArray());
            SelectedLine = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(selecteLineOffset).TakeWhile(b => b != 0x00).ToArray());

            MapCallerParent = BitConverter.ToInt32(data.Skip(offset + 0x24).Take(4).Reverse().ToArray());
            MapCallerChild = BitConverter.ToInt32(data.Skip(offset + 0x28).Take(4).Reverse().ToArray());
            Unknown2C = BitConverter.ToInt32(data.Skip(offset + 0x2C).Take(4).Reverse().ToArray());
            Unknown30 = BitConverter.ToInt32(data.Skip(offset + 0x30).Take(4).Reverse().ToArray());
            Unknown34 = BitConverter.ToInt32(data.Skip(offset + 0x34).Take(4).Reverse().ToArray());
            Unknown38 = BitConverter.ToInt16(data.Skip(offset + 0x34).Take(2).Reverse().ToArray());
            EventIndex = BitConverter.ToInt16(data.Skip(offset + 0x38).Take(2).Reverse().ToArray());
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

        /// <summary>
        /// Gets a binary data representation of the clubroom cutscene object
        /// </summary>
        /// <param name="currentOffset">The current data offset in the clubroom cutscene file</param>
        /// <param name="currentStringsOffset">The current strings offset in the clubroom cutscene file</param>
        /// <param name="endPointers">The current end pointers list to append to</param>
        /// <returns>A tuple containing a byte list of the binary pointer and other data of the object and a byte list of the string binary data</returns>
        public (List<byte> dataBytes, List<byte> stringBytes) GetBytes(int currentOffset, int currentStringsOffset, List<int> endPointers)
        {
            List<byte> dataBytes = [];
            List<byte> stringBytes = [];

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
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(HoverVoiceFile));
            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(HoverSpeaker));
            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(HoverLine));
            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(SelectedVoiceFile));
            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(SelectedSpeaker));
            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(SelectedLine));

            dataBytes.AddRange(BitConverter.GetBytes(MapCallerParent).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(MapCallerChild).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown2C).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown30).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(Unknown34).Reverse());
            dataBytes.AddRange(BitConverter.GetBytes(EventIndex).Reverse());
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

    /// <summary>
    /// An object representing the initial chapters you are presented with when talking to Koizumi
    /// </summary>
    public class ClubroomChapter
    {
        /// <summary>
        /// The title of the chapter
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// The flag that, when set, unlocks this chapter in Koizumi's menu
        /// </summary>
        public string Flag { get; set; }
        /// <summary>
        /// The voice file that plays when the chapter is hovered over in the selection menu
        /// </summary>
        public string HoverVoiceFile { get; set; }
        /// <summary>
        /// The speaker of the line that is displayed when the chapter is hovered over in the selection menu
        /// </summary>
        public string HoverSpeaker { get; set; }
        /// <summary>
        /// The line that is displayed when the chapter is hovered over in the selection menu
        /// </summary>
        public string HoverLine { get; set; }
        /// <summary>
        /// The voice file that plays when the chapter is selected in the selection menu
        /// </summary>
        public string SelectedVoiceFile { get; set; }
        /// <summary>
        /// The speaker of the line that is displayed when the chapter is selected in the selection menu
        /// </summary>
        public string SelectedSpeaker { get; set; }
        /// <summary>
        /// The line that plays when the chapter is selected in the selection menu
        /// </summary>
        public string SelectedLine { get; set; }
        /// <summary>
        /// Padding
        /// </summary>
        public int Padding { get; set; }

        /// <summary>
        /// Creates a clubroom mode chapter from the file's data and the offset of the chapter
        /// </summary>
        /// <param name="data">The binary data representing the clubroom Koizumi cutscene file</param>
        /// <param name="offset">The offset of this chapter in that data</param>
        public ClubroomChapter(IEnumerable<byte> data, int offset)
        {
            int titleOffset = BitConverter.ToInt32(data.Skip(offset + 0x00).Take(4).Reverse().ToArray());
            Title = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(titleOffset).TakeWhile(b => b != 0x00).ToArray());
            int flagOffset = BitConverter.ToInt32(data.Skip(offset + 0x04).Take(4).Reverse().ToArray());
            Flag = Encoding.ASCII.GetString(data.Skip(flagOffset).TakeWhile(b => b != 0x00).ToArray());
            int unselectedVoiceFileOffset = BitConverter.ToInt32(data.Skip(offset + 0x08).Take(4).Reverse().ToArray());
            HoverVoiceFile = Encoding.ASCII.GetString(data.Skip(unselectedVoiceFileOffset).TakeWhile(b => b != 0x00).ToArray());
            int unselectedSpeakerOffset = BitConverter.ToInt32(data.Skip(offset + 0x0C).Take(4).Reverse().ToArray());
            HoverSpeaker = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(unselectedSpeakerOffset).TakeWhile(b => b != 0x00).ToArray());
            int unselectedLineOffset = BitConverter.ToInt32(data.Skip(offset + 0x10).Take(4).Reverse().ToArray());
            HoverLine = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(unselectedLineOffset).TakeWhile(b => b != 0x00).ToArray());
            int selectedVoiceFileOffset = BitConverter.ToInt32(data.Skip(offset + 0x14).Take(4).Reverse().ToArray());
            SelectedVoiceFile = Encoding.ASCII.GetString(data.Skip(selectedVoiceFileOffset).TakeWhile(b => b != 0x00).ToArray());
            int selectedSpeakerOffset = BitConverter.ToInt32(data.Skip(offset + 0x18).Take(4).Reverse().ToArray());
            SelectedSpeaker = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(selectedSpeakerOffset).TakeWhile(b => b != 0x00).ToArray());
            int selectedLineOffset = BitConverter.ToInt32(data.Skip(offset + 0x1C).Take(4).Reverse().ToArray());
            SelectedLine = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(selectedLineOffset).TakeWhile(b => b != 0x00).ToArray());
            Padding = BitConverter.ToInt32(data.Skip(offset + 0x20).Take(4).Reverse().ToArray());
        }

        /// <summary>
        /// Gets the data and string bytes of the clubroom chapter
        /// </summary>
        /// <param name="currentOffset">The current data offset in the file</param>
        /// <param name="currentStringsOffset">The current offset of strings in the file</param>
        /// <param name="endPointers">The current list of end pointers to append to</param>
        /// <returns>A tuple of a byte list of binary data representing the pointer dat and a byte list of binary data representing the Shift-JIS encoded strings</returns>
        public (List<byte> dataBytes, List<byte> stringBytes) GetBytes(int currentOffset, int currentStringsOffset, List<int> endPointers)
        {
            List<byte> dataBytes = [];
            List<byte> stringBytes = [];

            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(Title));
            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(Flag));
            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(HoverVoiceFile));
            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(HoverSpeaker));
            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(HoverLine));
            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(SelectedVoiceFile));
            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(SelectedSpeaker));
            endPointers.Add(currentOffset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(currentStringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(SelectedLine));
            dataBytes.AddRange(BitConverter.GetBytes(Padding).Reverse());

            return (dataBytes, stringBytes);
        }
    }
}
