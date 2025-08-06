using HaruhiHeiretsuLib.Data;
using System.Collections;
using System.IO;
using System.Resources.NetStandard;

namespace HaruhiHeiretsuLib.Strings.Data
{
    public class DataStringsFile<T> : ShadeStringsFile
        where T : DataFile, IDataStringsFile, new()
    {
        public T DataFile { get; set; }

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
            DialogueLines = DataFile.GetDialogueLines();
        }

        public override byte[] GetBytes()
        {
            return DataFile.GetBytes();
        }

        public override void EditDialogue(int index, string newLine)
        {
            DataFile.Edited = true;
            DialogueLine newDialogueLine = new()
            {
                Line = newLine,
                Speaker = DialogueLines[index].Speaker,
                NumPaddingZeroes = DialogueLines[index].NumPaddingZeroes,
                Offset = DialogueLines[index].Offset,
                Metadata = DialogueLines[index].Metadata,
            };

            DataFile.ReplaceDialogueLine(newDialogueLine);
            DialogueLines = DataFile.GetDialogueLines();
        }

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

                EditDialogue(dialogueIndex, dialogueText);
            }
        }
    }

    public static class DataStringsFileLocations
    {
        public const int SYSTEM_TEXT_MCB_INDEX = 58;
        public const int SYSTEM_TEXT_INDEX = 10;

        public const int MESSAGE_BOX_TEXT_MCB_INDEX = 60;
        public const int MESSAGE_BOX_TEXT_INDEX = 14;

        // mcb 63 dat 20

        public const int TIMELINE_TEXT_MCB_INDEX = 68;
        public const int TIMELINE_TEXT_INDEX = 30;

        public const int MENU_TEXT_MCB_INDEX = 70;
        public const int MENU_TEXT_INDEX = 34;

        // mcb 73 dat 40

        // dat 42

        // dat 44

        public const int CLUBROOM_TEXT_INDEX = 46;

        public const int TOPICS_FLAG_MCB_INDEX = 78;
        public const int TOPICS_FLAGS_INDEX = 56;

        public const int MAP_DEFINITION_MCB_INDEX = 79;
        public const int MAP_DEFINITION_INDEX = 58;

        public const int LOCATIONS_MCB_INDEX = 80;
        public const int LOCATIONS_INDEX = 60;

        public const int NAMEPLATES_MCB_INDEX = 82;
        public const int NAMEPLATES_INDEX = 64;

        public const int TIMELINE_MCB_INDEX = 83;
        public const int TIMELINE_INDEX = 66;

        public const int CLUBROOM_MCB_INDEX = 86;
        public const int CLUBROOM_INDEX = 72;

        public const int EXTRAS_CLF_CLA_INDEX = 76;

        public const int EXTRAS_CLD_INDEX = 78;
    }
}
