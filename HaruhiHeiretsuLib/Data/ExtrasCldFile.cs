using HaruhiHeiretsuLib.Strings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaruhiHeiretsuLib.Data
{
    public class ExtrasCldFile : DataFile, IDataStringsFile
    {
        public List<CldAbout> CldAbouts = new();

        public override void Initialize(byte[] decompressedData, int offset)
        {
            base.Initialize(decompressedData, offset);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            int startPointer = BitConverter.ToInt32(Data.Skip(0x0C).Take(4).Reverse().ToArray());
            int numCldAbouts = BitConverter.ToInt32(Data.Skip(0x10).Take(4).Reverse().ToArray());
            for (int i = 0; i < numCldAbouts; i++)
            {
                CldAbout cldAbout = new();
                int titleOffset = BitConverter.ToInt32(Data.Skip(startPointer + i * 0x10 + 0x00).Take(4).Reverse().ToArray());
                cldAbout.Title = Encoding.GetEncoding("Shift-JIS").GetString(Data.Skip(titleOffset).TakeWhile(b => b != 0x00).ToArray());
                int voiceFileOffset = BitConverter.ToInt32(Data.Skip(startPointer + i * 0x10 + 0x04).Take(4).Reverse().ToArray());
                cldAbout.VoiceFile = Encoding.GetEncoding("Shift-JIS").GetString(Data.Skip(voiceFileOffset).TakeWhile(b => b != 0x00).ToArray());
                int speakerOffset = BitConverter.ToInt32(Data.Skip(startPointer + i * 0x10 + 0x08).Take(4).Reverse().ToArray());
                cldAbout.Speaker = Encoding.GetEncoding("Shift-JIS").GetString(Data.Skip(speakerOffset).TakeWhile(b => b != 0x00).ToArray());
                int lineOffset = BitConverter.ToInt32(Data.Skip(startPointer + i * 0x10 + 0x0C).Take(4).Reverse().ToArray());
                cldAbout.Line = Encoding.GetEncoding("Shift-JIS").GetString(Data.Skip(lineOffset).TakeWhile(b => b != 0x00).ToArray());

                CldAbouts.Add(cldAbout);
            }
        }

        public override byte[] GetBytes()
        {
            List<byte> bytes = new();
            List<byte> stringBytes = new();
            List<int> endPointers = new();

            bytes.AddRange(BitConverter.GetBytes(1).Reverse());
            bytes.AddRange(new byte[4]); // end pointer pointer, will be replaced

            int startPointer = 0x14;
            bytes.AddRange(BitConverter.GetBytes(startPointer).Reverse());
            bytes.AddRange(BitConverter.GetBytes(startPointer).Reverse());
            bytes.AddRange(BitConverter.GetBytes(CldAbouts.Count).Reverse());

            int stringsPointer = startPointer + CldAbouts.Count * 0x10;
            for (int i = 0; i < CldAbouts.Count; i++)
            {
                if (!string.IsNullOrEmpty(CldAbouts[i].Title))
                {
                    endPointers.Add(bytes.Count);
                    bytes.AddRange(BitConverter.GetBytes(stringsPointer + stringBytes.Count).Reverse());
                    stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(CldAbouts[i].Title));
                }
                else
                {
                    bytes.AddRange(new byte[4]);
                }
                endPointers.Add(bytes.Count);
                bytes.AddRange(BitConverter.GetBytes(stringsPointer + stringBytes.Count).Reverse());
                stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(CldAbouts[i].VoiceFile));
                endPointers.Add(bytes.Count);
                bytes.AddRange(BitConverter.GetBytes(stringsPointer + stringBytes.Count).Reverse());
                stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(CldAbouts[i].Speaker));
                endPointers.Add(bytes.Count);
                bytes.AddRange(BitConverter.GetBytes(stringsPointer + stringBytes.Count).Reverse());
                stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(CldAbouts[i].Line));
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

            return bytes.ToArray();
        }

        public List<DialogueLine> GetDialogueLines()
        {
            List<DialogueLine> lines = new();

            for (int i = 0; i < CldAbouts.Count; i++)
            {
                if (!string.IsNullOrEmpty(CldAbouts[i].Title))
                {
                    lines.Add(new()
                    {
                        Offset = i,
                        Speaker = "Title",
                        Line = CldAbouts[i].Title,
                        Metadata = (new string[] { "0" }).ToList(),
                    });
                }
                lines.Add(new()
                {
                    Offset = i,
                    Speaker = CldAbouts[i].Speaker,
                    Line = CldAbouts[i].Line,
                    Metadata = (new string[] { CldAbouts[i].VoiceFile, $"Title: {CldAbouts[i].Title}", "1" }).ToList(),
                });
            }

            return lines;
        }

        public void ReplaceDialogueLine(DialogueLine line)
        {
            switch (line.Metadata[^1])
            {
                case "0":
                    CldAbouts[line.Offset].Title = line.Line;
                    break;

                case "1":
                    CldAbouts[line.Offset].Line = line.Line;
                    break;
            }
        }
    }

    // 0x10 bytes
    public class CldAbout
    {
        public string Title { get; set; }
        public string VoiceFile { get; set; }
        public string Speaker { get; set; }
        public string Line { get; set; }
    }
}
