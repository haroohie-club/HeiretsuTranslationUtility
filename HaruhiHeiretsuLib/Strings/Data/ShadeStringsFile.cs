using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HaruhiHeiretsuLib.Util;

namespace HaruhiHeiretsuLib.Strings.Data;

/// <summary>
/// Shade data file with strings -- same kind as in Chokuretsu but big-endian
/// </summary>
public class ShadeStringsFile : StringsFile
{
    /// <summary>
    /// Front pointers (section definition pointers)
    /// </summary>
    public List<int> FrontPointers { get; set; } = [];
    /// <summary>
    /// Pointer to the end pointers section
    /// </summary>
    public int EndPointersPointer { get; set; }
    /// <summary>
    /// List of "end pointers" (pointer resolution pointers)
    /// </summary>
    public List<int> EndPointers { get; set; } = [];
    /// <summary>
    /// End pointer pointers (resolved pointers)
    /// </summary>
    public List<int> EndPointerPointers { get; set; } = [];
    /// <summary>
    /// The title of the data file
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// The characters present in the file
    /// </summary>
    public Dictionary<int, string> DramatisPersonae { get; set; } = [];
    /// <summary>
    /// The dialogue section pointer
    /// </summary>
    public int DialogueSectionPointer { get; set; }

    /// <summary>
    /// List of valid indices (real string files in scr.bin?)
    /// </summary>
    private static readonly int[] ValidIndices = [2, 4, 10, 14, 20, 30, 34, 40, 42, 44, 46, 48, 56, 58, 60, 64, 66, 72, 74, 76, 78, 84, 88,
    ];

    /// <summary>
    /// Empty constructor for serialization
    /// </summary>
    public ShadeStringsFile()
    {
    }

    /// <inheritdoc/>
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

        EndPointersPointer = IO.ReadInt(decompressedData, 4);
        int numEndPointers = IO.ReadInt(decompressedData, EndPointersPointer);
        for (int i = 0; i < numEndPointers; i++)
        {
            EndPointers.Add(IO.ReadInt(decompressedData, EndPointersPointer + 0x04 * (i + 1)));
        }

        EndPointerPointers = EndPointers.Select(p => { int x = offset; return IO.ReadInt(decompressedData, p); }).ToList();

        int titlePointer = IO.ReadInt(decompressedData, 0x08);
        Title = Encoding.ASCII.GetString(decompressedData.Skip(titlePointer).TakeWhile(b => b != 0x00).ToArray());

        for (int i = 0; i < EndPointerPointers.Count; i++)
        {
            byte[] lineData = Data.Skip(EndPointerPointers[i]).TakeWhile(b => b != 0x00).ToArray();
            Strings.Add(new()
            {
                Line = Encoding.GetEncoding("Shift-JIS").GetString(lineData),
                Offset = EndPointerPointers[i],
                Speaker = ScriptFileSpeaker.ANNOUNCEMENT.ToString(),
                NumPaddingZeroes = 4 - (lineData.Length % 4),
            });
        }
    }

    /// <inheritdoc/>
    public override void EditString(int index, string newString)
    {
        Edited = true;
        int oldLength = Strings[index].Length + Strings[index].NumPaddingZeroes;
        Strings[index].Line = newString;
        Strings[index].NumPaddingZeroes = 4 - (Strings[index].Length % 4);
        int lengthDifference = Strings[index].Length + Strings[index].NumPaddingZeroes - oldLength;

        List<byte> toWrite = [.. Encoding.GetEncoding("Shift-JIS").GetBytes(Strings[index].Line)];
        for (int i = 0; i < Strings[index].NumPaddingZeroes; i++)
        {
            toWrite.Add(0);
        }

        Data.RemoveRange(Strings[index].Offset, oldLength);
        Data.InsertRange(Strings[index].Offset, toWrite);

        ShiftPointers(Strings[index].Offset, lengthDifference);
    }

    private void ShiftPointers(int shiftLocation, int shiftAmount)
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
        if (EndPointersPointer > shiftLocation)
        {
            EndPointersPointer += shiftAmount;
            Data.RemoveRange(0x04, 4);
            Data.InsertRange(0x04, BitConverter.GetBytes(EndPointersPointer).Reverse());
        }
        for (int i = 0; i < EndPointers.Count; i++)
        {
            if (EndPointers[i] > shiftLocation)
            {
                EndPointers[i] += shiftAmount;
                Data.RemoveRange(EndPointersPointer + 0x04 * (i + 1), 4);
                Data.InsertRange(EndPointersPointer + 0x04 * (i + 1), BitConverter.GetBytes(EndPointers[i]).Reverse());
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
        foreach (DialogueLine dialogueLine in Strings)
        {
            if (dialogueLine.Offset > shiftLocation)
            {
                dialogueLine.Offset += shiftAmount;
            }
        }
    }
}