using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HaruhiHeiretsuLib
{
    public class ScriptFile
    {
        public const string VOICE_REGEX = @"V\d{3}\w{7}(?<characterCode>[A-Z]{3})";

        public bool Edited = false;
        public (int parent, int child) Location { get; set; }
        public List<byte> Data { get; set; }
        public List<DialogueLine> DialogueLines { get; set; } = new List<DialogueLine>();

        public int ScriptInt { get; set; }
        public int RoomInt { get; set; }
        public int TimeInt { get; set; }

        public ScriptFile(int parent, int child, byte[] data)
        {
            Location = (parent, child);
            Data = data.ToList();

            ParseDialogue();
        }

        public void ParseDialogue()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (!Encoding.GetEncoding("Shift-JIS").GetString(Data.ToArray()).Contains("SCRIPT"))
            {
                var matches = Regex.Matches(Encoding.ASCII.GetString(Data.ToArray()), @"V\d{3}\w{7}(?<characterCode>[A-Z]{3})");
                foreach (Match match in matches)
                {
                    Speaker speaker = DialogueLine.GetSpeaker(match.Groups[1].Value);
                    string line = Encoding.GetEncoding("Shift-JIS").GetString(Data.Skip(match.Index + 32).TakeWhile(b => b != 0x00).ToArray());
                    DialogueLines.Add(new DialogueLine { Offset = match.Index, Line = line, Speaker = speaker });
                }
            }
            else
            {
                int intsToRead = 0;
                string lastLine = "";
                var lines = new List<(int offset, string line)>();

                for (int i = 0; i < Data.Count;)
                {
                    int length = 0;
                    string line = "";
                    if (lastLine == "TIME" && intsToRead > 0)
                    {
                        line = "TIME";
                        length = 4;

                        switch (intsToRead)
                        {
                            case 3:
                                ScriptInt = BitConverter.ToInt32(Data.Skip(i).Take(4).Reverse().ToArray());
                                break;
                            case 2:
                                RoomInt = BitConverter.ToInt32(Data.Skip(i).Take(4).Reverse().ToArray());
                                break;
                            case 1:
                                TimeInt = BitConverter.ToInt32(Data.Skip(i).Take(4).Reverse().ToArray());
                                break;
                        }

                        intsToRead--;
                    }
                    else
                    {
                        length = BitConverter.ToInt32(Data.Skip(i).Take(4).Reverse().ToArray()) - 1; // remove trailing 0x00
                        if (length < 0)
                        {
                            break;
                        }
                        line = Encoding.GetEncoding("Shift-JIS").GetString(Data.Skip(i + 4).Take(length).ToArray());
                        length += 5;

                        if (line == "SCRIPT" || line == "ROOM" || line == "TIME")
                        {
                            intsToRead++;
                        }

                        if (lines.Count == 120)
                        {
                            Console.WriteLine("Here");
                        }

                        var match = Regex.Match(line, VOICE_REGEX);
                        if (match.Success)
                        {
                            (int offset, string line) mostRecentLine = lines.Last();
                            if (Regex.IsMatch(mostRecentLine.line, @"^(\w\d{1,2})+$") && !Regex.IsMatch(lines[^2].line, @"^(\w\d{1,2})+$"))
                            {
                                mostRecentLine = lines[^2];
                            }
                            DialogueLines.Add(new DialogueLine
                            {
                                Line = mostRecentLine.line,
                                Offset = mostRecentLine.offset,
                                Speaker = DialogueLine.GetSpeaker(match.Groups["characterCode"].Value)
                            });
                        }
                        else
                        {
                            lines.Add((i, line));
                        }
                    }

                    lastLine = line;
                    i += length;
                }
            }
        }

        public override string ToString()
        {
            return $"{Location.parent},{Location.child}";
        }
    }

    public class DialogueLine
    {
        public string Line { get; set; }
        public Speaker Speaker { get; set; }
        public int Offset { get; set; }

        public override string ToString()
        {
            return $"{Speaker}: {Line}";
        }

        public static Speaker GetSpeaker(string code)
        {
            switch (code)
            {
                case "ANN":
                    return Speaker.ANN;
                case "CAP":
                    return Speaker.CAP;
                case "CRF":
                    return Speaker.CRF;
                case "CRM":
                    return Speaker.CRM;
                case "GF1":
                    return Speaker.GF1;
                case "GF2":
                    return Speaker.GF2;
                case "GF3":
                    return Speaker.GF3;
                case "GM1":
                    return Speaker.GM1;
                case "GM2":
                    return Speaker.GM2;
                case "GM3":
                    return Speaker.GM3;
                case "HRH":
                    return Speaker.HARUHI;
                case "KZM":
                    return Speaker.KOIZUMI;
                case "KUN":
                    return Speaker.KUNIKIDA;
                case "KYN":
                    return Speaker.KYON;
                case "KY2":
                    return Speaker.KY2;
                case "MKT":
                    return Speaker.MIKOTO;
                case "MKR":
                    return Speaker.MIKURU;
                case "MNL":
                    return Speaker.MONOLOGUE;
                case "NGT":
                    return Speaker.NAGATO;
                case "NG2":
                    return Speaker.NG2;
                case "SIS":
                    return Speaker.KYON_SIS;
                case "TAI":
                    return Speaker.TAI;
                case "TAN":
                    return Speaker.TANIGUCHI;
                case "TRY":
                    return Speaker.TSURYA;
                default:
                    return Speaker.UNKNOWN;
            }
        }
    }

    public enum Speaker
    {
        ANN,
        CAP,
        CRF,
        CRM,
        GF1,
        GF2,
        GF3,
        GM1,
        GM2,
        GM3,
        HARUHI,
        KOIZUMI,
        KUNIKIDA,
        KYON,
        KY2,
        MIKOTO,
        MIKURU,
        MONOLOGUE,
        NAGATO,
        NG2,
        KYON_SIS,
        TAI,
        TANIGUCHI,
        TSURYA,
        UNKNOWN,
    }
}
