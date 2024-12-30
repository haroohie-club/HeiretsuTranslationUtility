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

            int clubroomThingsPointer = IO.ReadInt(decompressedData, 0x0C);
            int clubroomThingsCount = IO.ReadInt(decompressedData, 0x10);

            for (int i = 0; i < clubroomThingsCount; i++)
            {
                ClubroomCutscenes.Add(new(decompressedData, clubroomThingsPointer + 0x60 * i));
            }

            int clubroomThing2sPointer = IO.ReadInt(decompressedData, 0x14);
            int clubroomThing2sCount = IO.ReadInt(decompressedData, 0x18);

            for (int i = 0; i < clubroomThing2sCount; i++)
            {
                ClubroomChapters.Add(new(decompressedData, clubroomThing2sPointer + 0x24 * i));
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
        public ClubroomCutscene(byte[] data, int offset)
        {
            int titleOffset = IO.ReadInt(data, offset + 0x00);
            Title = IO.ReadShiftJisString(data, titleOffset);
            int chapterOffset = IO.ReadInt(data, offset + 0x04);
            Chapter = IO.ReadShiftJisString(data, chapterOffset);
            int flagOffset = IO.ReadInt(data, offset + 0x08);
            Flag = IO.ReadAsciiString(data, flagOffset);
            int hoverVoiceFileOffset = IO.ReadInt(data, offset + 0x0C);
            HoverVoiceFile = IO.ReadAsciiString(data, hoverVoiceFileOffset);
            int hoverSpeakerOffset = IO.ReadInt(data, offset + 0x10);
            HoverSpeaker = IO.ReadShiftJisString(data, hoverSpeakerOffset);
            int hoverLineOffset = IO.ReadInt(data, offset + 0x14);
            HoverLine = IO.ReadShiftJisString(data, hoverLineOffset);
            int selectedVoiceFileOffset = IO.ReadInt(data, offset + 0x18);
            SelectedVoiceFile = IO.ReadAsciiString(data, selectedVoiceFileOffset);
            int selectedSpeakerOffset = IO.ReadInt(data, offset + 0x1C);
            SelectedSpeaker = IO.ReadShiftJisString(data, selectedSpeakerOffset);
            int selecteLineOffset = IO.ReadInt(data, offset + 0x20);
            SelectedLine = IO.ReadShiftJisString(data, selecteLineOffset);

            MapCallerParent = IO.ReadInt(data, offset + 0x24);
            MapCallerChild = IO.ReadInt(data, offset + 0x28);
            Unknown2C = IO.ReadInt(data, offset + 0x2C);
            Unknown30 = IO.ReadInt(data, offset + 0x30);
            Unknown34 = IO.ReadInt(data, offset + 0x34);
            Unknown38 = IO.ReadShort(data, offset + 0x38);
            EventIndex = IO.ReadShort(data, offset + 0x3A);
            Unknown3C = IO.ReadInt(data, offset + 0x3C);
            Unknown40 = IO.ReadInt(data, offset + 0x40);
            Unknown44 = IO.ReadInt(data, offset + 0x44);
            Unknown48 = IO.ReadInt(data, offset + 0x48);
            Unknown4C = IO.ReadInt(data, offset + 0x4C);
            Unknown50 = IO.ReadInt(data, offset + 0x50);
            Unknown54 = IO.ReadInt(data, offset + 0x54);
            Unknown58 = IO.ReadInt(data, offset + 0x58);
            Unknown5C = IO.ReadInt(data, offset + 0x5C);
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
        public ClubroomChapter(byte[] data, int offset)
        {
            int titleOffset = IO.ReadInt(data, offset + 0x00);
            Title = IO.ReadShiftJisString(data, titleOffset);
            int flagOffset = IO.ReadInt(data, offset + 0x04);
            Flag = IO.ReadAsciiString(data, flagOffset);
            int unselectedVoiceFileOffset = IO.ReadInt(data, offset + 0x08);
            HoverVoiceFile = IO.ReadAsciiString(data, unselectedVoiceFileOffset);
            int unselectedSpeakerOffset = IO.ReadInt(data, offset + 0x0C);
            HoverSpeaker = IO.ReadShiftJisString(data, unselectedSpeakerOffset);
            int unselectedLineOffset = IO.ReadInt(data, offset + 0x10);
            HoverLine = IO.ReadShiftJisString(data, unselectedLineOffset);
            int selectedVoiceFileOffset = IO.ReadInt(data, offset + 0x14);
            SelectedVoiceFile = IO.ReadAsciiString(data, selectedVoiceFileOffset);
            int selectedSpeakerOffset = IO.ReadInt(data, offset + 0x18);
            SelectedSpeaker = IO.ReadShiftJisString(data, selectedSpeakerOffset);
            int selectedLineOffset = IO.ReadInt(data, offset + 0x1C);
            SelectedLine = IO.ReadShiftJisString(data, selectedLineOffset);
            Padding = IO.ReadInt(data, offset + 0x20);
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
