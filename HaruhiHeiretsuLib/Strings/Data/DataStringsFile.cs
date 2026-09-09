using HaruhiHeiretsuLib.Data;
using System.Collections;
using System.IO;
using System.Resources.NetStandard;

namespace HaruhiHeiretsuLib.Strings.Data;

/// <summary>
/// A wrapper of a data file to enable replacing strings
/// </summary>
/// <typeparam name="T">The data file type</typeparam>
public class DataStringsFile<T> : ShadeStringsFile
    where T : DataFile, IDataStringsFile, new()
{
    /// <summary>
    /// The data file
    /// </summary>
    public T DataFile { get; set; }

    /// <inheritdoc/>
    public override void Initialize(byte[] decompressedData, int offset = 0)
    {
        DataFile = new();
        DataFile.Initialize(decompressedData, offset);
        DataFile.McbId = McbId;
        DataFile.Location = Location;
        DataFile.McbEntryData = McbEntryData;
        DataFile.MagicInteger = MagicInteger;
        DataFile.BinArchiveIndex = BinArchiveIndex;
        DataFile.Offset = Offset;
        DataFile.Length = Length;
        DataFile.CompressedData = CompressedData;
        Strings = DataFile.GetDialogueLines();
    }

    /// <inheritdoc/>
    public override byte[] GetBytes()
    {
        return DataFile.GetBytes();
    }
    
    /// <inheritdoc/>
    public override void EditString(int index, string newString)
    {
        DataFile.Edited = true;
        DialogueLine newDialogueLine = new()
        {
            Line = newString,
            Speaker = Strings[index].Speaker,
            NumPaddingZeroes = Strings[index].NumPaddingZeroes,
            Offset = Strings[index].Offset,
            Metadata = Strings[index].Metadata,
        };

        DataFile.ReplaceDialogueLine(newDialogueLine);
        Strings = DataFile.GetDialogueLines();
    }

    /// <inheritdoc/>
    public override void ImportResxFile(string fileName, FontReplacementMap fontReplacementMap)
    {
        DataFile.Edited = true;

        TextReader textReader = GetResxReader(fileName);

        using ResXResourceReader resxReader = new(textReader);
        foreach (DictionaryEntry d in resxReader)
        {
            int dialogueIndex = int.Parse(((string)d.Key)[..4]);
            string dialogueText = (string)d.Value;

            dialogueText = NormalizeDialogueLine(dialogueText);

            for (int i = 0; i < dialogueText.Length; i++)
            {
                if (dialogueText[i] == '“' && (i == dialogueText.Length - 1
                                               || dialogueText[i + 1] == ' ' || dialogueText[i + 1] == '!' || dialogueText[i + 1] == '?' || dialogueText[i + 1] == '.' || dialogueText[i + 1] == '…' || dialogueText[i + 1] == '\n'))
                {
                    dialogueText = dialogueText.Remove(i, 1);
                    dialogueText = dialogueText.Insert(i, "”");
                }

                if (fontReplacementMap.ContainsReplacement($"{dialogueText[i]}"))
                {
                    string replacement = fontReplacementMap.GetStartCharacterForReplacement($"{dialogueText[i]}");
                    dialogueText = dialogueText.Remove(i, 1);
                    dialogueText = dialogueText.Insert(i, replacement);
                }
            }

            EditString(dialogueIndex, dialogueText);
        }
    }
}

/// <summary>
/// Hardcoded indices of the data file locations
/// </summary>
public static class DataStringsFileLocations
{
    /// <summary>
    /// MCB index of system text file
    /// </summary>
    public const int SystemTextMcbIndex = 58;
    /// <summary>
    /// dat.bin index of system text file
    /// </summary>
    public const int SystemTextIndex = 10;

    /// <summary>
    /// MCB index of message box text file
    /// </summary>
    public const int MessageBoxTextMcbIndex = 60;
    /// <summary>
    /// dat.bin index of message box text file
    /// </summary>
    public const int MessageBoxTextIndex = 14;

    // mcb 63 dat 20
    /// <summary>
    /// MCB index of timeline text file
    /// </summary>
    public const int TimelineTextMcbIndex = 68;
    /// <summary>
    /// dat.bin index of timeline text file
    /// </summary>
    public const int TimelineTextIndex = 30;

    /// <summary>
    /// MCB index of menu text file
    /// </summary>
    public const int MenuTextMcbIndex = 70;
    /// <summary>
    /// dat.bin index of menu text file
    /// </summary>
    public const int MenuTextIndex = 34;

    // mcb 73 dat 40

    // dat 42

    // dat 44

    /// <summary>
    /// dat.bin index of clubroom text file
    /// </summary>
    public const int ClubroomTextIndex = 46;

    /// <summary>
    /// MCB index of topics flags file
    /// </summary>
    public const int TopicsFlagsMcbIndex = 78;
    /// <summary>
    /// dat.bin index of topics flags file
    /// </summary>
    public const int TopicsFlagsIndex = 56;

    /// <summary>
    /// MCB index of map definitions file
    /// </summary>
    public const int MapDefinitionMcbIndex = 79;
    /// <summary>
    /// dat.bin index of map definitions file
    /// </summary>
    public const int MapDefinitionIndex = 58;

    /// <summary>
    /// MCB index of locations file
    /// </summary>
    public const int LocationsMcbIndex = 80;
    /// <summary>
    /// dat.bin index of locations file
    /// </summary>
    public const int LocationsIndex = 60;

    /// <summary>
    /// MCB index of nameplates file
    /// </summary>
    public const int NameplatesMcbIndex = 82;
    /// <summary>
    /// dat.bin index of nameplates file
    /// </summary>
    public const int NameplatesIndex = 64;

    /// <summary>
    /// MCB index of timeline file
    /// </summary>
    public const int TimelineMcbIndex = 83;
    /// <summary>
    /// dat.bin index of timeline file
    /// </summary>
    public const int TimelineIndex = 66;

    /// <summary>
    /// MCB index of clubroom file
    /// </summary>
    public const int ClubroomMcbIndex = 86;
    /// <summary>
    /// dat.bin index of clubroom file
    /// </summary>
    public const int ClubroomIndex = 72;

    /// <summary>
    /// dat.bin index of extras clf/cla file
    /// </summary>
    public const int ExtrasClfClaIndex = 76;

    /// <summary>
    /// dat.bin index of extras cld file
    /// </summary>
    public const int ExtrasCldIndex = 78;
}