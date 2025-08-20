using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HaruhiHeiretsuLib.Util;

namespace HaruhiHeiretsuLib.Strings.Data
{
    // The same kind of string files found in Chokuretsu except big-endian
    public class ShadeStringsFile : StringsFile
    {
        public List<int> FrontPointers { get; set; } = [];
        public int PointerToNumEndPointers { get; set; }
        public List<int> EndPointers { get; set; } = [];
        public List<int> EndPointerPointers { get; set; } = [];
        public string Title { get; set; }

        public Dictionary<int, string> DramatisPersonae { get; set; } = [];
        public int DialogueSectionPointer { get; set; }

        private static int[] ValidIndices = [2, 4, 10, 14, 20, 30, 34, 40, 42, 44, 46, 48, 56, 58, 60, 64, 66, 72, 74, 76, 78, 84, 88,
        ];

        public ShadeStringsFile()
        {
        }

        public override void Initialize(byte[] decompressedData, int offset = 0)
        {
            Data = [.. decompressedData];

            if ((!ValidIndices.Contains(BinArchiveIndex) || offset == 0x800) && (Location.parent < 0 && Location.child < 0))
            {
                return;
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Offset = offset;
            Data = [.. decompressedData];

            int numFrontPointers = IO.ReadInt(decompressedData, 0);
            bool reachedDramatisPersonae = false;
            for (int i = 0; i < numFrontPointers; i++)
            {
                FrontPointers.Add(IO.ReadInt(decompressedData, 0x0C + 0x08 * i));
                uint pointerValue = IO.ReadUInt(decompressedData, FrontPointers[i]);
                if (pointerValue > 0x10000000 || pointerValue == 0x8596) // 8596 is 妹 which is a valid character name, sadly lol
                {
                    reachedDramatisPersonae = true;
                    DramatisPersonae.Add(FrontPointers[i],
                        Encoding.GetEncoding("Shift-JIS").GetString(decompressedData.Skip(FrontPointers[i]).TakeWhile(b => b != 0x00).ToArray()));
                }
                else if (reachedDramatisPersonae)
                {
                    reachedDramatisPersonae = false;
                    DialogueSectionPointer = FrontPointers[i];
                }
            }

            PointerToNumEndPointers = IO.ReadInt(decompressedData, 4);
            int numEndPointers = IO.ReadInt(decompressedData, PointerToNumEndPointers);
            for (int i = 0; i < numEndPointers; i++)
            {
                EndPointers.Add(IO.ReadInt(decompressedData, PointerToNumEndPointers + 0x04 * (i + 1)));
            }

            EndPointerPointers = EndPointers.Select(p => { int x = offset; return IO.ReadInt(decompressedData, p); }).ToList();

            int titlePointer = IO.ReadInt(decompressedData, 0x08);
            Title = Encoding.ASCII.GetString(decompressedData.Skip(titlePointer).TakeWhile(b => b != 0x00).ToArray());

            for (int i = 0; i < EndPointerPointers.Count; i++)
            {
                byte[] lineData = Data.Skip(EndPointerPointers[i]).TakeWhile(b => b != 0x00).ToArray();
                DialogueLines.Add(new DialogueLine
                {
                    Line = Encoding.GetEncoding("Shift-JIS").GetString(lineData),
                    Offset = EndPointerPointers[i],
                    Speaker = ScriptFileSpeaker.ANNOUNCEMENT.ToString(),
                    NumPaddingZeroes = 4 - (lineData.Length % 4),
                });
            }
        }

        public override void EditDialogue(int index, string newLine)
        {
            Edited = true;
            int oldLength = DialogueLines[index].Length + DialogueLines[index].NumPaddingZeroes;
            DialogueLines[index].Line = newLine;
            DialogueLines[index].NumPaddingZeroes = 4 - (DialogueLines[index].Length % 4);
            int lengthDifference = DialogueLines[index].Length + DialogueLines[index].NumPaddingZeroes - oldLength;

            List<byte> toWrite = [.. Encoding.GetEncoding("Shift-JIS").GetBytes(DialogueLines[index].Line)];
            for (int i = 0; i < DialogueLines[index].NumPaddingZeroes; i++)
            {
                toWrite.Add(0);
            }

            Data.RemoveRange(DialogueLines[index].Offset, oldLength);
            Data.InsertRange(DialogueLines[index].Offset, toWrite);

            ShiftPointers(DialogueLines[index].Offset, lengthDifference);
        }

        public void ShiftPointers(int shiftLocation, int shiftAmount)
        {
            for (int i = 0; i < FrontPointers.Count; i++)
            {
                if (FrontPointers[i] > shiftLocation)
                {
                    FrontPointers[i] += shiftAmount;
                    Data.RemoveRange(0x0C + (0x08 * i), 4);
                    Data.InsertRange(0x0C + (0x08 * i), BitConverter.GetBytes(FrontPointers[i]).Reverse());
                }
            }
            if (PointerToNumEndPointers > shiftLocation)
            {
                PointerToNumEndPointers += shiftAmount;
                Data.RemoveRange(0x04, 4);
                Data.InsertRange(0x04, BitConverter.GetBytes(PointerToNumEndPointers).Reverse());
            }
            for (int i = 0; i < EndPointers.Count; i++)
            {
                if (EndPointers[i] > shiftLocation)
                {
                    EndPointers[i] += shiftAmount;
                    Data.RemoveRange(PointerToNumEndPointers + 0x04 * (i + 1), 4);
                    Data.InsertRange(PointerToNumEndPointers + 0x04 * (i + 1), BitConverter.GetBytes(EndPointers[i]).Reverse());
                }
            }
            for (int i = 0; i < EndPointerPointers.Count; i++)
            {
                if (EndPointerPointers[i] > shiftLocation)
                {
                    EndPointerPointers[i] += shiftAmount;
                    Data.RemoveRange(EndPointers[i], 4);
                    Data.InsertRange(EndPointers[i], BitConverter.GetBytes(EndPointerPointers[i]).Reverse());
                }
            }
            foreach (DialogueLine dialogueLine in DialogueLines)
            {
                if (dialogueLine.Offset > shiftLocation)
                {
                    dialogueLine.Offset += shiftAmount;
                }
            }
        }
    }
}
