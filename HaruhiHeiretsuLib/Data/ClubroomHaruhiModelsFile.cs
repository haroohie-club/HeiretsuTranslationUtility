using HaruhiHeiretsuLib.Strings;
using HaruhiHeiretsuLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiHeiretsuLib.Data
{
    /// <summary>
    /// Class representing the clubroom Haruhi models file (dat.bin 75/76)
    /// </summary>
    public class ClubroomHaruhiModelsFile : DataFile, IDataStringsFile
    {
        /// <summary>
        /// A list of animations available for each character once the model viewer is entered
        /// </summary>
        public List<ModelViewerCharacterAnimation> Animations { get; set; } = [];
        /// <summary>
        /// A list of outfits available to each character through the second menu of the model viewer
        /// </summary>
        public List<ModelViewerCharacterOutfit> Outfits { get; set; } = [];
        /// <summary>
        /// A list of characters available in the first menu of the model viewer
        /// </summary>
        public List<ModelViewerCharacter> Characters { get; set; } = [];

        /// <inheritdoc/>
        public override void Initialize(byte[] decompressedData, int offset)
        {
            base.Initialize(decompressedData, offset);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            int animationSectionStartPointer = BitConverter.ToInt32(Data.Skip(0x0C).Take(4).Reverse().ToArray());
            int animationCount = BitConverter.ToInt32(Data.Skip(0x10).Take(4).Reverse().ToArray());
            for (int i = 0; i < animationCount; i++)
            {
                Animations.Add(new(Data, animationSectionStartPointer + i * 0x24));
            }
            int outfitStartPointer = BitConverter.ToInt32(Data.Skip(0x14).Take(4).Reverse().ToArray());
            int outfitCount = BitConverter.ToInt32(Data.Skip(0x18).Take(4).Reverse().ToArray());
            for (int i = 0; i < outfitCount; i++)
            {
                Outfits.Add(new(Data, outfitStartPointer + i * 0x2C));
            }
            int characterStartPointer = BitConverter.ToInt32(Data.Skip(0x1C).Take(4).Reverse().ToArray());
            int characterCount = BitConverter.ToInt32(Data.Skip(0x20).Take(4).Reverse().ToArray());
            for (int i = 0; i < characterCount; i++)
            {
                Characters.Add(new(Data, characterStartPointer + i * 0x34));
            }
        }

        /// <inheritdoc/>
        public override byte[] GetBytes()
        {
            List<byte> bytes = [];
            List<byte> dataBytes = [];
            List<int> endPointers = [];

            bytes.AddRange(BitConverter.GetBytes(3).Reverse());
            bytes.AddRange(new byte[4]); // end pointer pointer, will be replaced later

            int startPointer = 12 + 8 * 3;
            bytes.AddRange(BitConverter.GetBytes(startPointer).Reverse());

            int section1StartPointer = startPointer + 0x20 * Characters.Count; // float section
            bytes.AddRange(BitConverter.GetBytes(section1StartPointer).Reverse());
            bytes.AddRange(BitConverter.GetBytes(Animations.Count).Reverse());
            List<byte> section1StringBytes = [];
            int section1StringsOffset = section1StartPointer + Animations.Count * 0x24;
            
            for (int i = 0; i < Animations.Count; i++)
            {
                (List<byte> entryDataBytes, List<byte> entryStringBytes) = Animations[i].GetBytes(section1StartPointer + i * 0x24, section1StringsOffset + section1StringBytes.Count, endPointers);
                dataBytes.AddRange(entryDataBytes);
                section1StringBytes.AddRange(entryStringBytes);
            }
            dataBytes.AddRange(section1StringBytes);

            int section2StartPointer = section1StartPointer + dataBytes.Count;
            bytes.AddRange(BitConverter.GetBytes(section2StartPointer).Reverse());
            bytes.AddRange(BitConverter.GetBytes(Outfits.Count).Reverse());
            List<byte> section2StringBytes = [];
            int section2StringsOffset = section2StartPointer + Outfits.Count * 0x2C;
            for (int i = 0; i < Outfits.Count; i++)
            {
                (List<byte> entryDataBytes, List<byte> entryStringBytes) = Outfits[i].GetBytes(section2StartPointer + i * 0x2C, section2StringsOffset + section2StringBytes.Count, endPointers);
                dataBytes.AddRange(entryDataBytes);
                section2StringBytes.AddRange(entryStringBytes);
            }
            dataBytes.AddRange(section2StringBytes);

            int section3StartPointer = section1StartPointer + dataBytes.Count;
            bytes.AddRange(BitConverter.GetBytes(section3StartPointer).Reverse());
            bytes.AddRange(BitConverter.GetBytes(Characters.Count).Reverse());
            List<byte> section3StringBytes = [];
            int section3StringsOffset = section3StartPointer + Characters.Count * 0x34;
            for (int i = 0; i < Characters.Count; i++)
            {
                (List<byte> entryDataBytes, List<byte> entryStringBytes, List<byte> entryFloatStructBytes) =
                    Characters[i].GetBytes(section3StartPointer + i * 0x34, section3StringsOffset + section3StringBytes.Count, bytes.Count, endPointers);
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

            return [.. bytes];
        }

        /// <inheritdoc/>
        public List<DialogueLine> GetDialogueLines()
        {
            List<DialogueLine> lines = [];

            for (int i = 0; i < Animations.Count; i++)
            {
                lines.Add(new()
                {
                    Speaker = Animations[i].Speaker,
                    Line = Animations[i].Line,
                    Metadata = [.. (new string[] { Animations[i].VoiceFile, "0", $"{i}", "0" })],
                });
            }
            for (int i = 0; i < Outfits.Count; i++)
            {
                lines.Add(new()
                {
                    Speaker = $"{Outfits[i].OutfitOwner}'s {Outfits[i].OutfitType} Description",
                    Line = Outfits[i].OutfitDescription,
                    Metadata = [.. (new string[] { Outfits[i].OutfitFlag, "1", $"{i}", "0" })],
                });
                lines.Add(new()
                {
                    Speaker = $"{Outfits[i].Speaker1} (Describing {Outfits[i].OutfitOwner}'s {Outfits[i].OutfitType})",
                    Line = Outfits[i].Line1,
                    Metadata = [.. (new string[] { Outfits[i].OutfitFlag, Outfits[i].VoiceFile1, "1", $"{i}", "1" })],
                });
                lines.Add(new()
                {
                    Speaker = $"{Outfits[i].Speaker2} (Describing {Outfits[i].OutfitOwner}'s {Outfits[i].OutfitType})",
                    Line = Outfits[i].Line2,
                    Metadata = [.. (new string[] { Outfits[i].OutfitFlag, Outfits[i].VoiceFile2, "1", $"{i}", "2" })],
                });
            }
            for (int i = 0; i < Characters.Count; i++)
            {
                lines.Add(new()
                {
                    Speaker = $"{Characters[i].Character} Name",
                    Line = Characters[i].CharacterName,
                    Metadata = [.. (new string[] { Characters[i].CharacterUnlockFlag, "2", $"{i}", "0" })],
                });
                lines.Add(new()
                {
                    Speaker = $"{Characters[i].HoverSpeaker} (Describing {Characters[i].Character})",
                    Line = Characters[i].HoverLine,
                    Metadata = [.. (new string[] { Characters[i].CharacterUnlockFlag, Characters[i].HoverVoiceFile, "2", $"{i}", "1" })],
                });
                lines.Add(new()
                {
                    Speaker = $"{Characters[i].SelectSpeaker} (Describing {Characters[i].Character})",
                    Line = Characters[i].SelectLine,
                    Metadata = [.. (new string[] { Characters[i].CharacterUnlockFlag, Characters[i].SelectVoiceFile, "2", $"{i}", "1" })],
                });
            }

            return lines;
        }

        /// <inheritdoc/>
        public void ReplaceDialogueLine(DialogueLine line)
        {
            int section = int.Parse(line.Metadata[^3]);
            int itemIndex = int.Parse(line.Metadata[^2]);
            int lineIndex = int.Parse(line.Metadata[^1]);

            switch (section)
            {
                case 0:
                    Animations[itemIndex].Line = line.Line;
                    break;

                case 1:
                    switch (lineIndex)
                    {
                        case 0:
                            Outfits[itemIndex].OutfitDescription = line.Line;
                            break;
                        case 1:
                            Outfits[itemIndex].Line1 = line.Line;
                            break;
                        case 2:
                            Outfits[itemIndex].Line2 = line.Line;
                            break;
                    }
                    break;

                case 2:
                    switch (lineIndex)
                    {
                        case 0:
                            Characters[itemIndex].CharacterName = line.Line;
                            break;
                        case 1:
                            Characters[itemIndex].HoverLine = line.Line;
                            break;
                        case 2:
                            Characters[itemIndex].SelectLine = line.Line;
                            break;
                    }
                    break;
            }
        }
    }

    // 0x24 bytes
    public class ModelViewerCharacterAnimation
    {
        ///
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

        public ModelViewerCharacterAnimation(IEnumerable<byte> data, int offset)
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
            List<byte> dataBytes = [];
            List<byte> stringBytes = [];

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
    public class ModelViewerCharacterOutfit
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

        public ModelViewerCharacterOutfit(IEnumerable<byte> data, int offset)
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
            List<byte> dataBytes = [];
            List<byte> stringBytes = [];

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
    /// <summary>
    /// A class representing a character viewable in the clubroom's model viewer
    /// </summary>
    public class ModelViewerCharacter
    {
        // 0x20 bytes
        /// <summary>
        /// Unknown
        /// </summary>
        /// <remarks>
        /// Simple constructor from raw binary data
        /// </remarks>
        public struct UnknownFloatStruct(IEnumerable<byte> data)
        {
            /// <summary>
            /// Unknown
            /// </summary>
            public float Unknown00 { get; set; } = BitConverter.ToSingle(data.Skip(0x00).Take(4).Reverse().ToArray());
            /// <summary>
            /// Unknown
            /// </summary>
            public float Unknown04 { get; set; } = BitConverter.ToSingle(data.Skip(0x04).Take(4).Reverse().ToArray());
            /// <summary>
            /// Unknown
            /// </summary>
            public float Unknown08 { get; set; } = BitConverter.ToSingle(data.Skip(0x08).Take(4).Reverse().ToArray());
            /// <summary>
            /// Unknown
            /// </summary>
            public float Unknown0C { get; set; } = BitConverter.ToSingle(data.Skip(0x0C).Take(4).Reverse().ToArray());
            /// <summary>
            /// Unknown
            /// </summary>
            public float Unknown10 { get; set; } = BitConverter.ToSingle(data.Skip(0x10).Take(4).Reverse().ToArray());
            /// <summary>
            /// Unknown
            /// </summary>
            public float Unknown14 { get; set; } = BitConverter.ToSingle(data.Skip(0x14).Take(4).Reverse().ToArray());
            /// <summary>
            /// Unknown
            /// </summary>
            public float Unknown18 { get; set; } = BitConverter.ToSingle(data.Skip(0x18).Take(4).Reverse().ToArray());
            /// <summary>
            /// Unknown
            /// </summary>
            public float Unknown1C { get; set; } = BitConverter.ToSingle(data.Skip(0x1C).Take(4).Reverse().ToArray());

            /// <summary>
            /// Returns binary data representation of the float struct
            /// </summary>
            /// <returns>A list of bytes representing the float struct's binary data</returns>
            public List<byte> GetBytes()
            {
                List<byte> bytes =
                [
                    .. BitConverter.GetBytes(Unknown00).Reverse(),
                    .. BitConverter.GetBytes(Unknown04).Reverse(),
                    .. BitConverter.GetBytes(Unknown08).Reverse(),
                    .. BitConverter.GetBytes(Unknown0C).Reverse(),
                    .. BitConverter.GetBytes(Unknown10).Reverse(),
                    .. BitConverter.GetBytes(Unknown14).Reverse(),
                    .. BitConverter.GetBytes(Unknown18).Reverse(),
                    .. BitConverter.GetBytes(Unknown1C).Reverse(),
                ];

                return bytes;
            }
        }

        /// <summary>
        /// The name of the character as displayed in the model viewer
        /// </summary>
        public string CharacterName { get; set; }
        /// <summary>
        /// The flag which, when set, unlocks this character in the model viewer
        /// </summary>
        public string CharacterUnlockFlag { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string HoverVoiceFile { get; set; }
        public string HoverSpeaker { get; set; }
        public string HoverLine { get; set; }
        public string SelectVoiceFile { get; set; }
        public string SelectSpeaker { get; set; }
        public string SelectLine { get; set; }
        /// <summary>
        /// The name of the character as will be referenced by other classes
        /// </summary>
        public string Character { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public UnknownFloatStruct FloatStruct { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public float UnknownFloat { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown2C { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown30 { get; set; }

        public ModelViewerCharacter(IEnumerable<byte> data, int offset)
        {
            int characterNameOffset = BitConverter.ToInt32(data.Skip(offset + 0x00).Take(4).Reverse().ToArray());
            CharacterName = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(characterNameOffset).TakeWhile(b => b != 0x00).ToArray());
            int characterFlagOffset = BitConverter.ToInt32(data.Skip(offset + 0x04).Take(4).Reverse().ToArray());
            CharacterUnlockFlag = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(characterFlagOffset).TakeWhile(b => b != 0x00).ToArray());
            int voiceFile1Offset = BitConverter.ToInt32(data.Skip(offset + 0x08).Take(4).Reverse().ToArray());
            HoverVoiceFile = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(voiceFile1Offset).TakeWhile(b => b != 0x00).ToArray());
            int speaker1Offset = BitConverter.ToInt32(data.Skip(offset + 0x0C).Take(4).Reverse().ToArray());
            HoverSpeaker = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(speaker1Offset).TakeWhile(b => b != 0x00).ToArray());
            int line1Offset = BitConverter.ToInt32(data.Skip(offset + 0x10).Take(4).Reverse().ToArray());
            HoverLine = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(line1Offset).TakeWhile(b => b != 0x00).ToArray());
            int voiceFile2Offset = BitConverter.ToInt32(data.Skip(offset + 0x14).Take(4).Reverse().ToArray());
            SelectVoiceFile = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(voiceFile2Offset).TakeWhile(b => b != 0x00).ToArray());
            int speaker2Offset = BitConverter.ToInt32(data.Skip(offset + 0x18).Take(4).Reverse().ToArray());
            SelectSpeaker = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(speaker2Offset).TakeWhile(b => b != 0x00).ToArray());
            int line2Offset = BitConverter.ToInt32(data.Skip(offset + 0x1C).Take(4).Reverse().ToArray());
            SelectLine = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(line2Offset).TakeWhile(b => b != 0x00).ToArray());
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
            List<byte> dataBytes = [];
            List<byte> stringBytes = [];

            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(stringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(CharacterName));
            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(stringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(CharacterUnlockFlag));
            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(stringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(HoverVoiceFile));
            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(stringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(HoverSpeaker));
            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(stringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(HoverLine));
            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(stringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(SelectVoiceFile));
            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(stringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(SelectSpeaker));
            endPointers.Add(offset + dataBytes.Count);
            dataBytes.AddRange(BitConverter.GetBytes(stringsOffset + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(SelectLine));
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
