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
            DataFile.Index = Index;
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
                int dialogueIndex = int.Parse(((string)d.Key)[0..4]);
                string dialogueText = (string)d.Value;

                dialogueText = NormalizeDialogueLine(dialogueText);

                for (int i = 0; i < dialogueText.Length; i++)
                {
                    if (dialogueText[i] == '“' && (i == dialogueText.Length - 1
                        || dialogueText[i + 1] == ' ' || dialogueText[i + 1] == '!' || dialogueText[i + 1] == '?' || dialogueText[i + 1] == '.' || dialogueText[i + 1] == '…' || dialogueText[i + 1] == '\n'))
                    {
                        dialogueText.Remove(i, 1);
                        dialogueText.Insert(i, "”");
                    }

                    if (fontReplacementMap.ContainsReplacement($"{dialogueText[i]}"))
                    {
                        dialogueText.Remove(i, 1);
                        dialogueText.Insert(i, fontReplacementMap.GetStartCharacterForReplacement($"{dialogueText[i]}"));
                    }
                }

                EditDialogue(dialogueIndex, dialogueText);
            }
        }
    }

    public static class DataStringsFileLocations
    {
        public const int TOPICS_FLAG_MCB_INDEX = 78;
        public const int TOPICS_FLAGS_INDEX = 56;

        public const int MAP_DEFINITION_MCB_INDEX = 79;
        public const int MAP_DEFINITION_INDEX = 58;

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
