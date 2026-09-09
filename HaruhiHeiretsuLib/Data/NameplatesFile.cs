using HaruhiHeiretsuLib.Strings;
using HaruhiHeiretsuLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiHeiretsuLib.Data;

/// <summary>
/// Representation of the nameplates file
/// </summary>
public class NameplatesFile : DataFile, IDataStringsFile
{
    /// <summary>
    /// List of nameplates
    /// </summary>
    public List<Nameplate> Nameplates { get; set; } = [];

    /// <inheritdoc/>
    public override void Initialize(byte[] decompressedData, int offset)
    {
        base.Initialize(decompressedData, offset);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        int numNameplates = IO.ReadInt(decompressedData, 0x10);

        for (int i = 0; i < numNameplates; i++)
        {
            int internalCharacterNamePointer = IO.ReadInt(decompressedData, 0x14 + i * 0x14 + 0x00);
            int[] gameCharacterNamePointers = new int[4];
            for (int j = 0; j < gameCharacterNamePointers.Length; j++)
            {
                gameCharacterNamePointers[j] = IO.ReadInt(decompressedData, 0x14 + i * 0x14 + (j + 1) * 0x04);
            }
            string internalCharacterName = IO.ReadAsciiString(decompressedData, internalCharacterNamePointer);
            string[] gameCharacterNames = new string[4];
            for (int j = 0; j < gameCharacterNames.Length; j++)
            {
                if (gameCharacterNamePointers[j] > 0)
                {
                    gameCharacterNames[j] = IO.ReadShiftJisString(decompressedData, gameCharacterNamePointers[j]);
                }
            }

            Nameplates.Add(new()
            {
                Index = i,
                CharacterNameInternal = internalCharacterName,
                CharacterNamesGame = gameCharacterNames,
            });
        }
    }

    /// <inheritdoc/>
    public override byte[] GetBytes()
    {
        List<byte> bytes = [];
        List<byte> nameplateBytes = [];
        List<byte> stringBytes = [];
        List<int> endPointers = [];

        bytes.AddRange(BitConverter.GetBytes(1).Reverse());
        bytes.AddRange(new byte[4]); // end pointers pointer, will be replaced later
        int startPointer = 0x14;
        bytes.AddRange(BitConverter.GetBytes(startPointer).Reverse());
        bytes.AddRange(BitConverter.GetBytes(startPointer).Reverse());
        bytes.AddRange(BitConverter.GetBytes(Nameplates.Count).Reverse());
        int stringStartPointer = startPointer + Nameplates.Count * 0x14;

        foreach (Nameplate nameplate in Nameplates)
        {
            endPointers.Add(startPointer + nameplateBytes.Count);
            nameplateBytes.AddRange(BitConverter.GetBytes(stringStartPointer + stringBytes.Count).Reverse());
            stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(nameplate.CharacterNameInternal));
            for (int i = 0; i < nameplate.CharacterNamesGame.Length; i++)
            {
                if (!string.IsNullOrEmpty(nameplate.CharacterNamesGame[i]))
                {
                    endPointers.Add(startPointer + nameplateBytes.Count);
                    nameplateBytes.AddRange(BitConverter.GetBytes(stringStartPointer + stringBytes.Count).Reverse());
                    stringBytes.AddRange(Helpers.GetPaddedByteArrayFromString(nameplate.CharacterNamesGame[i]));
                }
                else
                {
                    nameplateBytes.AddRange(new byte[4]);
                }
            }
        }
        bytes.AddRange(nameplateBytes);
        bytes.AddRange(stringBytes);

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

        foreach (Nameplate nameplate in Nameplates)
        {
            for (int i = 0; i < nameplate.CharacterNamesGame.Length; i++)
            {
                if (!string.IsNullOrEmpty(nameplate.CharacterNamesGame[i]))
                {
                    dialogueLines.Add(new()
                    {
                        Speaker = $"Nameplate {i + 1} for {nameplate.CharacterNameInternal}",
                        Line = nameplate.CharacterNamesGame[i],
                        Offset = i,
                    });
                }
            }
        }

        return dialogueLines;
    }

    /// <inheritdoc/>
    public void ReplaceDialogueLine(DialogueLine line)
    {
        for (int i = 0; i < Nameplates.Count; i++)
        {
            if (i == line.Offset)
            {
                for (int j = 0; j < Nameplates[i].CharacterNamesGame.Length; j++)
                {
                    if (line.Offset == j)
                    {
                        Nameplates[i].CharacterNamesGame[j] = line.Line;
                    }
                }
            }
        }
    }
}

/// <summary>
/// Representation of a nameplate
/// </summary>
public class Nameplate
{
    /// <summary>
    /// The nameplate index
    /// </summary>
    public int Index { get; set; }
    /// <summary>
    /// The internal character name (used in scripts)
    /// </summary>
    public string CharacterNameInternal { get; set; }
    /// <summary>
    /// The name of the character as seen in game
    /// </summary>
    public string[] CharacterNamesGame { get; set; } = new string[4];
    
    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{CharacterNameInternal}: {CharacterNamesGame}";
    }
}