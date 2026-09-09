using HaruhiHeiretsuLib.Archive;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources.NetStandard;
using System.Text;

namespace HaruhiHeiretsuLib.Strings;

/// <summary>
/// A generic representation of a file which contains strings
/// </summary>
public partial class StringsFile : FileInArchive
{
    /// <summary>
    /// Regex for matching voice files
    /// </summary>
    public const string VoiceRegex = @"(CL|V)(\w\d{2}\w)?\w{3}\d{3}(?<characterCode>[A-Z]{3})";

    /// <summary>
    /// List of strings (or specifically dialogue lines) in the file
    /// </summary>
    public List<DialogueLine> Strings { get; set; } = [];

    /// <summary>
    /// Edits a string (or dialogue line) in a strings file
    /// </summary>
    /// <param name="index">The index of the string in the file</param>
    /// <param name="newString">The string to replace it with</param>
    public virtual void EditString(int index, string newString)
    {
    }

    /// <summary>
    /// Common function called by classes inheriting this one when editing strings
    /// </summary>
    /// <param name="index">The string to prep</param>
    /// <param name="newLine">The new line</param>
    /// <returns></returns>
    protected (int oldLength, byte[] newLineData) StringEditSetUp(int index, string newLine)
    {
        Edited = true;
        string oldLine = Strings[index].Line;
        newLine = newLine.Replace("\r\n", "\n"); // consolidate newlines
        Strings[index].Line = newLine;
        int oldLength = Encoding.GetEncoding("Shift-JIS").GetByteCount(oldLine);
        byte[] newLineData = Encoding.GetEncoding("Shift-JIS").GetBytes(newLine);

        return (oldLength, newLineData);
    }

    /// <summary>
    /// Imports a RESX to replace all strings in the file
    /// </summary>
    /// <param name="fileName">The RESX file to load</param>
    /// <param name="fontReplacementMap">The font replacement map to use when replacing strings</param>
    public virtual void ImportResxFile(string fileName, FontReplacementMap fontReplacementMap)
    {
        Edited = true;
    }

    /// <summary>
    /// Writes the file's strings to a RESX
    /// </summary>
    /// <param name="fileName">The output RESX file location</param>
    public void WriteResxFile(string fileName)
    {
        using ResXResourceWriter resxWriter = new(fileName);
        for (int i = 0; i < Strings.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(Strings[i].Line) && Strings[i].Length > 1)
            {
                resxWriter.AddResource(new($"{i:D4} ({Path.GetFileNameWithoutExtension(fileName)}) {Strings[i].Speaker}{(Strings[i].Metadata.Count > 0 ? $" - {string.Join(", ", Strings[i].Metadata)}" : "")}",
                    Strings[i].Line));
            }
        }
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        if (Location != (-1, -1))
        {
            return $"{McbId:X4}/{Location.parent},{Location.child}";
        }
        else
        {
            return $"{BinArchiveIndex:X3} {BinArchiveIndex:D4} 0x{Offset:X8}";
        }
    }

    /// <summary>
    /// Gets a RESX reader that has the appropriate parameters to read Weblate-generated RESXs
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns>A RESX text reader</returns>
    public static TextReader GetResxReader(string fileName)
    {
        string resxContents = File.ReadAllText(fileName);
        resxContents = resxContents.Replace("System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
            "System.Resources.NetStandard.ResXResourceWriter, System.Resources.NetStandard, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
        resxContents = resxContents.Replace("System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
            "System.Resources.NetStandard.ResXResourceReader, System.Resources.NetStandard, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
        return new StringReader(resxContents);
    }

    /// <summary>
    /// Normalize the dialogue line
    /// </summary>
    /// <param name="dialogueText">Dialogue line</param>
    /// <returns>A normalized dialogue line</returns>
    protected static string NormalizeDialogueLine(string dialogueText)
    {
        // Replace all faux-ellipses with an ellipsis character
        dialogueText = dialogueText.Replace("...", "…");
        // Replace all faux-em-dashes with actual em-dash characters
        dialogueText = dialogueText.Replace("--", "—");
        // Consolidate Unix/Windows newlines to just \n
        dialogueText = dialogueText.Replace("\r\n", "\n");
        // We start by replacing all quotes with the open quotes, then will replace them with closed quotes as we go
        return dialogueText.Replace("\"", "“");
    }

    /// <summary>
    /// Processes a dialogue line, including replacing characters from a font replacement map
    /// </summary>
    /// <param name="dialogueText">The dialogue line</param>
    /// <param name="fontReplacementMap">The font replacement map to use</param>
    /// <param name="dialogueLineLengths">The valid lengths of lines of dialogue</param>
    /// <returns>A processed string to use for dialogue</returns>
    public static string ProcessDialogueLineWithFontReplacement(string dialogueText, FontReplacementMap fontReplacementMap, int[] dialogueLineLengths)
    {
        int lineLength = 0;
        int currentLine = 0;
        for (int i = 0; i < dialogueText.Length; i++)
        {
            if (dialogueText[i] == '#' && dialogueText.Length - i >= 3)
            {
                // skip replacement/line length increment for operators
                if (dialogueText[i + 1] == 'b' && dialogueText[i + 2] == 'w' || dialogueText[i + 1] == 'F')
                {
                    i += 2;
                    continue;
                }
                else if (dialogueText.Length - i >= 5 && dialogueText[i + 1] == 'b' && dialogueText[i + 2] == 't')
                {
                    i += 2 + dialogueText.Skip(i + 2).TakeWhile(c => char.IsNumber(c)).Count();
                    continue;
                }
            }

            if (dialogueText[i] == '“' && (i == dialogueText.Length - 1
                                           || dialogueText[i + 1] == ' ' || dialogueText[i + 1] == '!' || dialogueText[i + 1] == '?' || dialogueText[i + 1] == '.' || dialogueText[i + 1] == '…' || dialogueText[i + 1] == '\n'))
            {
                dialogueText = dialogueText.Remove(i, 1);
                dialogueText = dialogueText.Insert(i, "”");
            }

            if (fontReplacementMap.ContainsReplacement($"{dialogueText[i]}"))
            {
                lineLength += fontReplacementMap.GetReplacementCharacterWidth($"{dialogueText[i]}");
                string replacement = $"{dialogueText[i]}";
                dialogueText = dialogueText.Remove(i, 1);
                dialogueText = dialogueText.Insert(i, fontReplacementMap.GetStartCharacterForReplacement(replacement));
            }

            if (dialogueText[i] == '\n')
            {
                lineLength = 0;
                currentLine++;
            }

            int maxLineLength;
            if (currentLine >= dialogueLineLengths.Length)
            {
                maxLineLength = dialogueLineLengths.Last();
            }
            else
            {
                maxLineLength = dialogueLineLengths[currentLine];
            }

            if (dialogueText[i] != ' ' && lineLength > maxLineLength)
            {
                int indexOfMostRecentSpace = dialogueText[..i].LastIndexOf(' ');
                dialogueText = dialogueText.Remove(indexOfMostRecentSpace, 1);
                dialogueText = dialogueText.Insert(indexOfMostRecentSpace, "\n");
                lineLength = dialogueText[(indexOfMostRecentSpace + 1)..(i + 1)].Sum(c => fontReplacementMap.GetReplacementCharacterWidth($"{dialogueText[i]}"));
                currentLine++;
            }
        }

        return dialogueText;
    }
}

/// <summary>
/// A representation of a string in a strings file
/// Specifically tailored for dialogue, but used in all strings files
/// </summary>
public class DialogueLine
{
    /// <summary>
    /// The string or dialogue line in question
    /// </summary>
    public string Line { get; set; }
    /// <summary>
    /// The speaker of the line of dialogue
    /// </summary>
    public string Speaker { get; set; }
    /// <summary>
    /// The offset of the string or struct
    /// </summary>
    public int Offset { get; set; }
    /// <summary>
    /// The length of the string (Shift-JIS encoded)
    /// </summary>
    public int Length => Encoding.GetEncoding("Shift-JIS").GetByteCount(Line);
    /// <summary>
    /// The number of zeroes its padded tdo
    /// </summary>
    public int NumPaddingZeroes { get; set; } = 1;

    /// <summary>
    /// Any extra metadata to be included when exporting to RESX
    /// </summary>
    public List<string> Metadata { get; set; } = [];

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Speaker}: {Line}";
    }

    /// <summary>
    /// Gets the speaker from game codes
    /// </summary>
    /// <param name="code">The game code</param>
    /// <returns>A script file speaker</returns>
    public static ScriptFileSpeaker GetSpeaker(string code)
    {
        return code switch
        {
            "ANN" => ScriptFileSpeaker.ANNOUNCEMENT,
            "CAP" => ScriptFileSpeaker.CAPTAIN,
            "CRF" => ScriptFileSpeaker.CREW_F,
            "CRM" => ScriptFileSpeaker.CREW_M,
            "GF1" => ScriptFileSpeaker.GUEST_F1,
            "GF2" => ScriptFileSpeaker.GUEST_F2,
            "GF3" => ScriptFileSpeaker.GUEST_F3,
            "GM1" => ScriptFileSpeaker.GUEST_M1,
            "GM2" => ScriptFileSpeaker.GUEST_M2,
            "GM3" => ScriptFileSpeaker.GUEST_M3,
            "HRH" => ScriptFileSpeaker.HARUHI,
            "KZM" => ScriptFileSpeaker.KOIZUMI,
            "KUN" => ScriptFileSpeaker.KUNIKIDA,
            "KYN" => ScriptFileSpeaker.KYON,
            "KY2" => ScriptFileSpeaker.KYON2,
            "MKT" => ScriptFileSpeaker.MIKOTO,
            "MKR" => ScriptFileSpeaker.MIKURU,
            "MNL" => ScriptFileSpeaker.MONOLOGUE,
            "NGT" => ScriptFileSpeaker.NAGATO,
            "NG2" => ScriptFileSpeaker.NAGATO2,
            "SIS" => ScriptFileSpeaker.KYNSIS,
            "TAI" => ScriptFileSpeaker.TAIICHIRO,
            "TAN" => ScriptFileSpeaker.TANIGUCHI,
            "TRY" => ScriptFileSpeaker.TSURUYA,
            _ => ScriptFileSpeaker.UNKNOWN,
        };
    }
}

/// <summary>
/// Script file speakers
/// </summary>
public enum ScriptFileSpeaker
{
    /// <summary>
    /// Speaker unknown
    /// </summary>
    UNKNOWN = -3,
    /// <summary>
    /// A choice in a select
    /// </summary>
    CHOICE = -2,
    /// <summary>
    /// Kyon's monologue
    /// </summary>
    MONOLOGUE = -1,
    /// <summary>
    /// Kyon
    /// </summary>
    KYON,
    /// <summary>
    /// Kyon's copy
    /// </summary>
    KYON2,
    /// <summary>
    /// Haruhi Suzumiya
    /// </summary>
    HARUHI,
    /// <summary>
    /// Yuki Nagato
    /// </summary>
    NAGATO,
    /// <summary>
    /// Nagato's copy
    /// </summary>
    NAGATO2,
    /// <summary>
    /// Mikuru Asahina
    /// </summary>
    MIKURU,
    /// <summary>
    /// Mikuru's double
    /// </summary>
    MIKURU2,
    /// <summary>
    /// Itsuki Koizumi
    /// </summary>
    KOIZUMI,
    /// <summary>
    /// Koizumi's double
    /// </summary>
    KOIZUMI2,
    /// <summary>
    /// Tsuruya
    /// </summary>
    TSURUYA,
    /// <summary>
    /// Kyon's sister
    /// </summary>
    KYNSIS,
    /// <summary>
    /// Mikoto Misumaru
    /// </summary>
    MIKOTO,
    /// <summary>
    /// Mikoto's double
    /// </summary>
    MIKOTO2,
    /// <summary>
    /// Taiichiro
    /// </summary>
    TAIICHIRO,
    /// <summary>
    /// Taiichiro's double
    /// </summary>
    TAIICHIRO2,
    /// <summary>
    /// Captain of the ship
    /// </summary>
    CAPTAIN,
    /// <summary>
    /// Male guest (3)
    /// </summary>
    GUEST_M3,
    /// <summary>
    /// Female guest (3)
    /// </summary>
    GUEST_F3,
    /// <summary>
    /// Female crewmember
    /// </summary>
    CREW_F,
    /// <summary>
    /// Male crewmember
    /// </summary>
    CREW_M,
    /// <summary>
    /// Male guest (1)
    /// </summary>
    GUEST_M1,
    /// <summary>
    /// Male guest (2)
    /// </summary>
    GUEST_M2,
    /// <summary>
    /// Female guest (1)
    /// </summary>
    GUEST_F1,
    /// <summary>
    /// Female guest (2)
    /// </summary>
    GUEST_F2,
    /// <summary>
    /// Taniguchi
    /// </summary>
    TANIGUCHI,
    /// <summary>
    /// Kunikida
    /// </summary>
    KUNIKIDA,
    /// <summary>
    /// Shipwide announcement
    /// </summary>
    ANNOUNCEMENT,
    /// <summary>
    /// Chariman
    /// </summary>
    CHAIRMAN,
    /// <summary>
    /// A shop employee
    /// </summary>
    SHOP_EMPLOYEE,
    /// <summary>
    /// ???
    /// </summary>
    THREE_QUESTIONS,
    /// <summary>
    /// Another One
    /// </summary>
    ANOTHER_ONE,
    /// <summary>
    /// Another Two
    /// </summary>
    ANOTHER_TWO,
    /// <summary>
    /// A crew member
    /// </summary>
    CREW_32,
}

/// <summary>
/// Event file speaker
/// </summary>
public enum EventFileSpeaker
{
    /// <summary>
    /// Kyon's monologue
    /// </summary>
    MONOLOGUE = -1,
    /// <summary>
    /// Kyon
    /// </summary>
    KYON = 0,
    /// <summary>
    /// Haruhi Suzumiya
    /// </summary>
    HARUHI = 1,
    /// <summary>
    /// Yuki Nagato
    /// </summary>
    NAGATO = 2,
    /// <summary>
    /// Mikuru Asahina
    /// </summary>
    MIKURU = 3,
    /// <summary>
    /// Itsuki Koizumi
    /// </summary>
    KOIZUMI = 4,
    /// <summary>
    /// Tsuruya
    /// </summary>
    TSURUYA = 5,
    /// <summary>
    /// Kyon's sister
    /// </summary>
    KYNSIS = 6,
    /// <summary>
    /// Mikoto Misumaru
    /// </summary>
    MIKOTO = 7,
    /// <summary>
    /// Taiichiro
    /// </summary>
    TAIICHIRO = 8,
    /// <summary>
    /// Kyon's double
    /// </summary>
    KYON2 = 9,
    /// <summary>
    /// Nagato's double
    /// </summary>
    NAGATO2 = 10,
    /// <summary>
    /// Mikuru's double
    /// </summary>
    MIKURU2 = 11, // guess
    /// <summary>
    /// Koizumi's double
    /// </summary>
    KOIZUMI2 = 12, // guess
    /// <summary>
    /// Captain
    /// </summary>
    CAPTAIN = 14,
    /// <summary>
    /// Female crewmember
    /// </summary>
    CREW_FEMALE = 17,
    /// <summary>
    /// Male crewmember
    /// </summary>
    CREW_MALE = 18,
    /// <summary>
    /// Male guest
    /// </summary>
    GUEST_MALE2 = 20,
    /// <summary>
    /// Female guest
    /// </summary>
    GUEST_FEMALE1 = 21,
    /// <summary>
    /// Taniguchi
    /// </summary>
    TANIGUCHI = 23,
    /// <summary>
    /// Kunikida
    /// </summary>
    KUNIKIDA = 24,
}