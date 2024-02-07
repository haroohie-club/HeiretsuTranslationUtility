using HaruhiHeiretsuLib.Strings;
using HaruhiHeiretsuLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiHeiretsuLib.Data
{
    /// <summary>
    /// A representation of the Database menus in the clubroom when talking to Nagato
    /// </summary>
    public class ClubroomNagatoDatabaseFile : DataFile, IDataStringsFile
    {
        /// <summary>
        /// List of clubroom database cards
        /// </summary>
        public List<ClubroomDatabaseCard> ClubroomDatabaseCards = [];

        /// <summary>
        /// Simple constructor
        /// </summary>
        public ClubroomNagatoDatabaseFile()
        {
            Name = "Clubroom Nagato Database File";
        }

        /// <inheritdoc/>
        public override void Initialize(byte[] decompressedData, int offset)
        {
            base.Initialize(decompressedData, offset);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            int startPointer = BitConverter.ToInt32(Data.Skip(0x0C).Take(4).Reverse().ToArray());
            int numCldAbouts = BitConverter.ToInt32(Data.Skip(0x10).Take(4).Reverse().ToArray());
            for (int i = 0; i < numCldAbouts; i++)
            {
                ClubroomDatabaseCard cldAbout = new();
                int titleOffset = BitConverter.ToInt32(Data.Skip(startPointer + i * 0x10 + 0x00).Take(4).Reverse().ToArray());
                cldAbout.Title = Encoding.GetEncoding("Shift-JIS").GetString(Data.Skip(titleOffset).TakeWhile(b => b != 0x00).ToArray());
                int voiceFileOffset = BitConverter.ToInt32(Data.Skip(startPointer + i * 0x10 + 0x04).Take(4).Reverse().ToArray());
                cldAbout.VoiceFile = Encoding.GetEncoding("Shift-JIS").GetString(Data.Skip(voiceFileOffset).TakeWhile(b => b != 0x00).ToArray());
                int speakerOffset = BitConverter.ToInt32(Data.Skip(startPointer + i * 0x10 + 0x08).Take(4).Reverse().ToArray());
                cldAbout.Speaker = Encoding.GetEncoding("Shift-JIS").GetString(Data.Skip(speakerOffset).TakeWhile(b => b != 0x00).ToArray());
                int lineOffset = BitConverter.ToInt32(Data.Skip(startPointer + i * 0x10 + 0x0C).Take(4).Reverse().ToArray());
                cldAbout.Line = Encoding.GetEncoding("Shift-JIS").GetString(Data.Skip(lineOffset).TakeWhile(b => b != 0x00).ToArray());

                ClubroomDatabaseCards.Add(cldAbout);
            }
        }

        /// <inheritdoc/>
        public override byte[] GetBytes()
        {
            List<byte> bytes = [];
            List<byte> stringBytes = [];
            List<int> endPointers = [];

            bytes.AddRange(BitConverter.GetBytes(1).Reverse());
            bytes.AddRange(new byte[4]); // end pointer pointer, will be replaced

            int startPointer = 0x14;
            bytes.AddRange(BitConverter.GetBytes(startPointer).Reverse());
            bytes.AddRange(BitConverter.GetBytes(startPointer).Reverse());
            bytes.AddRange(BitConverter.GetBytes(ClubroomDatabaseCards.Count).Reverse());

            int stringsPointer = startPointer + ClubroomDatabaseCards.Count * 0x10;
            for (int i = 0; i < ClubroomDatabaseCards.Count; i++)
            {
                if (!string.IsNullOrEmpty(ClubroomDatabaseCards[i].Title))
                {
                    endPointers.Add(bytes.Count);
                    bytes.AddRange(BitConverter.GetBytes(stringsPointer + stringBytes.Count).Reverse());
                    stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(ClubroomDatabaseCards[i].Title));
                }
                else
                {
                    bytes.AddRange(new byte[4]);
                }
                endPointers.Add(bytes.Count);
                bytes.AddRange(BitConverter.GetBytes(stringsPointer + stringBytes.Count).Reverse());
                stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(ClubroomDatabaseCards[i].VoiceFile));
                endPointers.Add(bytes.Count);
                bytes.AddRange(BitConverter.GetBytes(stringsPointer + stringBytes.Count).Reverse());
                stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(ClubroomDatabaseCards[i].Speaker));
                endPointers.Add(bytes.Count);
                bytes.AddRange(BitConverter.GetBytes(stringsPointer + stringBytes.Count).Reverse());
                stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(ClubroomDatabaseCards[i].Line));
            }
            bytes.AddRange(stringBytes);

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

            for (int i = 0; i < ClubroomDatabaseCards.Count; i++)
            {
                if (!string.IsNullOrEmpty(ClubroomDatabaseCards[i].Title))
                {
                    lines.Add(new()
                    {
                        Offset = i,
                        Speaker = "Title",
                        Line = ClubroomDatabaseCards[i].Title,
                        Metadata = [.. (new string[] { "0" })],
                    });
                }
                lines.Add(new()
                {
                    Offset = i,
                    Speaker = ClubroomDatabaseCards[i].Speaker,
                    Line = ClubroomDatabaseCards[i].Line,
                    Metadata = [.. (new string[] { ClubroomDatabaseCards[i].VoiceFile, $"Title: {ClubroomDatabaseCards[i].Title}", "1" })],
                });
            }

            return lines;
        }

        /// <inheritdoc/>
        public void ReplaceDialogueLine(DialogueLine line)
        {
            switch (line.Metadata[^1])
            {
                case "0":
                    ClubroomDatabaseCards[line.Offset].Title = line.Line;
                    break;

                case "1":
                    ClubroomDatabaseCards[line.Offset].Line = line.Line;
                    break;
            }
        }
    }

    // 0x10 bytes
    /// <summary>
    /// Representation of a menu card in the clubroom database when talking to Nagato
    /// </summary>
    public class ClubroomDatabaseCard
    {
        /// <summary>
        /// The title of the menu card
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// The voice file that plays when hovering over the card
        /// </summary>
        public string VoiceFile { get; set; }
        /// <summary>
        /// The speaker of the line that is displayed when hovering over the card
        /// </summary>
        public string Speaker { get; set; }
        /// <summary>
        /// The line that is displayed when hovering over the card
        /// </summary>
        public string Line { get; set; }
    }
}
