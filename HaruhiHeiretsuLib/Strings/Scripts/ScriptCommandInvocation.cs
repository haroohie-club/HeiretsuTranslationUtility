using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HaruhiHeiretsuLib.Util;

namespace HaruhiHeiretsuLib.Strings.Scripts
{
    public class Parameter
    {
        public ScriptCommand.ParameterType Type { get; set; }
        public byte[] Value { get; set; }
        public int LineNumber { get; set; }
    }

    public class ScriptCommandInvocation
    {

        public int Address { get; set; }
        public short LineNumber { get; set; }
        public string Label { get; set; }
        public short CharacterEntity { get; set; }
        public short CommandCode { get; set; }
        public ScriptCommand Command { get; set; }
        public List<string> ScriptObjects { get; set; }
        public List<Parameter> Parameters { get; set; } = [];
        public int Length => 12 + Parameters.Sum(p => 2 + p.Value.Length);

        public List<ScriptCommandInvocation> AllOtherInvocations { get; set; }

        public ScriptCommandInvocation(List<string> scriptObjects, int address)
        {
            ScriptObjects = scriptObjects;
            Address = address;
        }

        public ScriptCommandInvocation(string invocation, short lineNumber, List<ScriptCommand> allCommands, List<string> objects, List<(string, int)> labels, FontReplacementMap fontReplacementMap = null)
        {
            ParseInvocation(invocation, lineNumber, allCommands, objects, labels, fontReplacementMap);
        }

        public override string ToString()
        {
            if (Command is not null)
            {
                return Command.ToString();
            }
            else
            {
                return $"{CommandCode:X4}";
            }
        }

        public byte[] GetBytes()
        {
            List<byte> bytes =
            [
                .. BitConverter.GetBytes(LineNumber).Reverse(),
                .. BitConverter.GetBytes(CharacterEntity).Reverse(),
                .. BitConverter.GetBytes(CommandCode).Reverse(),
                .. BitConverter.GetBytes((short)Parameters.Count).Reverse(),
                .. BitConverter.GetBytes(0),
            ];

            bytes.AddRange(Parameters.SelectMany(p =>
            {
                List<byte> bytes = [.. BitConverter.GetBytes((short)p.Type).Reverse(), .. p.Value];
                return bytes;
            }));

            return [.. bytes];
        }

        public string GetInvocation(FontReplacementMap fontReplacementMap = null)
        {
            string invocation = string.Empty;
            if (!string.IsNullOrEmpty(Label))
            {
                invocation += $"{Label}: ";
            }
            invocation += Command.Name;
            if (CharacterEntity >= 0)
            {
                invocation += $"<{GetCharacter(CharacterEntity)}>";
            }
            invocation += "(";
            for (int i = 0; i < Parameters.Count; i++)
            {
                if (i > 0)
                {
                    invocation += ", ";
                }
                invocation += ParseParameter(Parameters[i], fontReplacementMap);
            }
            return $"{invocation})";
        }

        public void ParseInvocation(string invocation, short lineNumber, List<ScriptCommand> allCommands, List<string> objects, List<(string label, int lineNumber)> labels, FontReplacementMap fontReplacementMap = null)
        {
            Regex labelRegex = new(@"^{(?<label>[\w\d]+)}");

            Match lineLabelMatch = Regex.Match(invocation, @"^(?<label>[\w\d]+): ");
            if (lineLabelMatch.Success)
            {
                Label = lineLabelMatch.Groups["label"].Value;
                invocation = invocation[lineLabelMatch.Length..];
                if (!labels.Any(l => l.label == Label))
                {
                    labels.Add((Label, lineNumber));
                }
                else
                {
                    labels[labels.IndexOf(labels.First(l => l.label == Label))] = (Label, lineNumber); // this is hell, i'm in hell
                }
            }

            Match match = Regex.Match(invocation, @"(?<command>[A-Z\d_]+)(?:<(?<character>[?A-Z\d_]+)>)?\((?<parameters>.+)?\)");
            if (!match.Success)
            {
                throw new ArgumentException($"Invalid invocation '{invocation}' provided!");
            }

            string command = match.Groups["command"].Value;
            string character = match.Groups["character"].Value;
            string parameters = match.Groups["parameters"].Value;

            Command = allCommands.FirstOrDefault(c => c.Name == command);
            if (Command is null)
            {
                throw new ArgumentException($"Command '{command}' is not a valid command!");
            }
            CommandCode = (short)allCommands.IndexOf(Command);
            LineNumber = lineNumber;
            if (character.Length > 0)
            {
                CharacterEntity = GetCharacterEntity(character);
            }
            else
            {
                CharacterEntity = -1;
            }

            Parameters = [];
            for (int i = 0; i < parameters.Length;)
            {
                try
                {
                    string trimmedParameters = parameters[i..].Trim();
                    i += parameters[i..].Length - trimmedParameters.Length;

                    // Try to see if it's a line number
                    Match lineNumberMatch = Regex.Match(trimmedParameters, @"^\d+");
                    Match labelMatch = labelRegex.Match(trimmedParameters);
                    if (lineNumberMatch.Success)
                    {
                        string parameter = trimmedParameters.Split(',')[0];

                        if (int.TryParse(parameter, out int varLineNumber))
                        {
                            List<byte> bytes = new(new byte[] { 0 });
                            bytes.AddRange(BitConverter.GetBytes(varLineNumber).Reverse().Skip(1)); // skip the first byte to keep us at four bytes; this makes us a 24-bit integer :D
                            Parameters.Add(new() { Type = ScriptCommand.ParameterType.ADDRESS, Value = [.. bytes], LineNumber = LineNumber });
                            i += parameter.Length + 2;
                            continue;
                        }
                    }
                    else if (labelMatch.Success)
                    {
                        List<byte> bytes = new(new byte[] { 1 });
                        if (!labels.Any(l => l.label == labelMatch.Groups["label"].Value))
                        {
                            labels.Add((labelMatch.Groups["label"].Value, 0));
                        }
                        bytes.AddRange(BitConverter.GetBytes(labels.IndexOf(labels.First(l => l.label == labelMatch.Groups["label"].Value))).Reverse().Skip(1)); // another 24-bit integer
                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.ADDRESS, Value = [.. bytes], LineNumber = LineNumber });
                        i += labelMatch.Length + 1;
                        continue;
                    }

                    // See if it's a string
                    if (trimmedParameters.StartsWith("\""))
                    {
                        int firstQuote = trimmedParameters.IndexOf('"');
                        int secondQuote = Regex.Match(trimmedParameters[(firstQuote + 1)..], @"[^\\]""").Index + 1;
                        string line = trimmedParameters[(firstQuote + 1)..(secondQuote + 1)];
                        int lineLength = line.Length;
                        line = line.Replace("\\n", "\n").Replace("\\\"", "\"");

                        if (fontReplacementMap is not null)
                        {
                            line = StringsFile.ProcessDialogueLineWithFontReplacement(line, fontReplacementMap, ScriptFile.DIALOGUE_LINE_LENGTHS);
                        }

                        objects.Add(line);
                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.DIALOGUE, Value = BitConverter.GetBytes((short)(objects.Count - 1)).Reverse().ToArray(), LineNumber = LineNumber });

                        i += lineLength + 4;
                        continue;
                    }

                    // See if it's a conditional (currently we only support one condition!!)
                    // TODO: Add support for more than one condition
                    if (trimmedParameters.StartsWith("if ", StringComparison.OrdinalIgnoreCase))
                    {
                        List<byte> bytes =
                        [
                            .. BitConverter.GetBytes((short)22).Reverse(), // Add length
                            .. BitConverter.GetBytes((short)1).Reverse(), // Add number of conditions
                        ];
                        string parameter = trimmedParameters.Split(',')[0];
                        string[] components = parameter.Split(' ');

                        byte[] firstOperand = CalculateControlStructure(components[1], components[2], objects);
                        if (components.Length > 3)
                        {
                            bytes.Add(ScriptCommand.ComparisonOperatorToCodeMap[components[3]]); // add comparator byte
                            bytes.Add(0x82); // always this for one condition
                            bytes.AddRange(firstOperand);
                            bytes.AddRange(CalculateControlStructure(components[4], components[5], objects));
                        }
                        else
                        {
                            bytes.AddRange(new byte[] { 0x89, 0x82 });
                            bytes.AddRange(firstOperand);
                            bytes.AddRange(CalculateControlStructure("lit", "1", objects));
                        }
                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.CONDITIONAL, Value = [.. bytes], LineNumber = LineNumber });
                        i += parameter.Length + 5;
                        continue;
                    }

                    // See if it's a time
                    if (trimmedParameters.StartsWith("time_", StringComparison.OrdinalIgnoreCase))
                    {
                        List<byte> bytes = [];
                        string parameter = trimmedParameters.Split(',')[0];
                        string[] components = parameter.Split(' ');

                        switch (components[0])
                        {
                            case "time_f":
                                bytes.AddRange(BitConverter.GetBytes(0x200).Reverse());
                                break;
                            case "time_s":
                                bytes.AddRange(BitConverter.GetBytes(0x201).Reverse());
                                break;
                        }

                        bytes.AddRange(CalculateControlStructure(components[1], components[2], objects));
                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.TIMESPAN, Value = [.. bytes], LineNumber = LineNumber });
                        i += parameter.Length + 2;
                        continue;
                    }

                    // Is it a Vector2?
                    if (trimmedParameters.StartsWith("Vector2(", StringComparison.OrdinalIgnoreCase))
                    {
                        List<byte> bytes = [];
                        string[] vectorComponents = trimmedParameters[8..].Split(')')[0].Split(',');
                        foreach (string vectorComponent in vectorComponents)
                        {
                            string trimmedVectorComponent = vectorComponent.Trim();
                            string[] parts = trimmedVectorComponent.Split(' ');
                            bytes.AddRange(CalculateControlStructure(parts[0], parts[1], objects));
                        }

                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.VECTOR2, Value = [.. bytes], LineNumber = LineNumber });
                        i += vectorComponents.Sum(c => c.Length) + 11;
                        continue;
                    }

                    // Is it a transition?
                    if (trimmedParameters.StartsWith("Transition[", StringComparison.OrdinalIgnoreCase))
                    {
                        List<byte> bytes = [];
                        string parameter = trimmedParameters.Split(',')[0][11..^1].ToUpper();
                        if (ScriptCommand.TransitionToCodeMap.TryGetValue(parameter, out int transition))
                        {
                            bytes.AddRange(CalculateControlStructure("lit", $"{transition}", objects));
                        }
                        else
                        {
                            string[] controlSplit = parameter.Split(' ');
                            bytes.AddRange(CalculateControlStructure(controlSplit[0], controlSplit[1], objects));
                        }

                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.TRANSITION, Value = [.. bytes], LineNumber = LineNumber });
                        i += parameter.Length + 13;
                        continue;
                    }

                    // Is it an indexed address?
                    if (trimmedParameters.StartsWith("("))
                    {
                        List<byte> bytes = [];
                        string[] components = trimmedParameters.Split(')')[0].Split(',');

                        Match indexedLabelMatch = labelRegex.Match(components[0][1..]);
                        if (indexedLabelMatch.Success)
                        {
                            bytes.Add(1);
                            if (!labels.Any(l => l.label == indexedLabelMatch.Groups["label"].Value))
                            {
                                labels.Add((indexedLabelMatch.Groups["label"].Value, 0));
                            }
                            bytes.AddRange(BitConverter.GetBytes(labels.IndexOf(labels.First(l => l.label == indexedLabelMatch.Groups["label"].Value))).Reverse().Skip(1));
                        }
                        else
                        {
                            bytes.Add(0);
                            bytes.AddRange(BitConverter.GetBytes(int.Parse(components[0].Trim())).Reverse().Skip(1));
                        }

                        bytes.AddRange(components[1].Trim() == "TRUE" ? new byte[] { 0x01, 0x00, 0x00, 0x00 } : new byte[4]); // weird little-endian looking thing is on purpose; matches game
                        string[] controlCodeComponents = components[2].Trim().Split(' ');
                        bytes.AddRange(CalculateControlStructure(controlCodeComponents[0], controlCodeComponents[1], objects));

                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.INDEXEDADDRESS, Value = [.. bytes], LineNumber = LineNumber });
                        i += trimmedParameters.Split(')')[0].Length + 2;
                        continue;
                    }

                    // Is it an angle?
                    if (trimmedParameters.StartsWith("degrees ", StringComparison.OrdinalIgnoreCase))
                    {
                        string parameter = trimmedParameters.Split(',')[0];
                        string[] components = parameter.Split(' ');

                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.ANGLE, Value = CalculateControlStructure(components[1], components[2], objects), LineNumber = LineNumber });
                        i += parameter.Length + 2;
                        continue;
                    }

                    // Is it a facial expression?
                    if (trimmedParameters.StartsWith("EXPRESSION[", StringComparison.OrdinalIgnoreCase))
                    {
                        string[] expressionComponents = trimmedParameters.Split(',')[0][11..^1].Split(' ');

                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.FACIALEXPRESSION, Value = CalculateControlStructure(expressionComponents[0], expressionComponents[1], objects), LineNumber = LineNumber });
                        i += expressionComponents.Sum(v => v.Length) + 14;
                        continue;
                    }

                    // Is it a boolean?
                    if (trimmedParameters.StartsWith("TRUE", StringComparison.OrdinalIgnoreCase) || trimmedParameters.StartsWith("FALSE", StringComparison.OrdinalIgnoreCase))
                    {
                        string parameter = trimmedParameters.Split(',')[0].ToLower();

                        bool boolean = bool.Parse(parameter);

                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.BOOL, Value = boolean ? BitConverter.GetBytes(1).Reverse().ToArray() : BitConverter.GetBytes(0).Reverse().ToArray(), LineNumber = LineNumber });
                        i += parameter.Length + 2;
                        continue;
                    }

                    // Is it a sound volume?
                    if (trimmedParameters.StartsWith("VOLUME[", StringComparison.OrdinalIgnoreCase))
                    {
                        string[] volumeComponents = trimmedParameters.Split(',')[0][7..^1].Split(' ');

                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.VOLUME, Value = CalculateControlStructure(volumeComponents[0], volumeComponents[1], objects), LineNumber = LineNumber });
                        i += volumeComponents.Sum(v => v.Length) + 10;
                        continue;
                    }

                    // Is it a color?
                    if (trimmedParameters.StartsWith("color", StringComparison.OrdinalIgnoreCase))
                    {
                        string parameter = trimmedParameters.Split(',')[0].ToLower();
                        string[] components = parameter.Split(' ');

                        int color = int.Parse(components[2], NumberStyles.HexNumber);

                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.COLOR, Value = CalculateControlStructure(components[1], $"{color}", objects), LineNumber = LineNumber });
                        i += parameter.Length + 2;
                        continue;
                    }

                    // Is it a character?
                    if (trimmedParameters.StartsWith("Character[", StringComparison.OrdinalIgnoreCase))
                    {
                        string characterName = trimmedParameters.Split(',')[0][10..^1].ToUpper();
                        int characterEntity = GetCharacterEntity(characterName);

                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.CHARACTER, Value = CalculateControlStructure("lit", $"{characterEntity}", objects), LineNumber = LineNumber });
                        i += characterName.Length + 12;
                        continue;
                    }

                    // Is it a precalculated vector?
                    if (trimmedParameters.StartsWith("PreVec[", StringComparison.OrdinalIgnoreCase))
                    {
                        string parameter = trimmedParameters.Split(',')[0][7..^1];
                        string[] components = parameter.Split(' ');

                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.PREVEC, Value = CalculateControlStructure(components[0], components[1], objects), LineNumber = LineNumber });
                        i += parameter.Length + 9;
                        continue;
                    }

                    // Is it a float?
                    if (trimmedParameters.StartsWith("float ", StringComparison.OrdinalIgnoreCase))
                    {
                        List<byte> bytes = [];
                        string[] components = trimmedParameters.Split(',')[0].Split(' ');

                        if (components[1] == "lit")
                        {
                            bytes.AddRange(CalculateControlStructure(components[1], $"{Helpers.FloatToInt(float.Parse(components[2]))}", objects));
                        }
                        else
                        {
                            bytes.AddRange(CalculateControlStructure(components[1], components[2], objects));
                        }

                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.FLOAT, Value = [.. bytes], LineNumber = LineNumber });
                        i += components.Sum(c => c.Length) + 4;
                        continue;
                    }

                    // Is it a Vector3?
                    if (trimmedParameters.StartsWith("Vector3(", StringComparison.OrdinalIgnoreCase))
                    {
                        List<byte> bytes = [];
                        string[] vectorComponents = trimmedParameters[8..].Split(')')[0].Split(',');
                        foreach (string vectorComponent in vectorComponents)
                        {
                            string trimmedVectorComponent = vectorComponent.Trim();
                            string[] parts = trimmedVectorComponent.Split(' ');
                            bytes.AddRange(CalculateControlStructure(parts[1], $"{Helpers.FloatToInt(float.Parse(parts[2]))}", objects));
                        }

                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.VECTOR3, Value = [.. bytes], LineNumber = LineNumber });
                        i += vectorComponents.Sum(c => c.Length) + 12;
                        continue;
                    }

                    // Is it a var index?
                    if (trimmedParameters.StartsWith('$'))
                    {
                        string scriptObject = trimmedParameters.Split(',')[0][1..].ToUpper();

                        if (!objects.Contains(scriptObject))
                        {
                            objects.Add(scriptObject);
                        }
                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.VARINDEX, Value = BitConverter.GetBytes((short)objects.IndexOf(scriptObject)).Reverse().ToArray(), LineNumber = LineNumber });
                        i += scriptObject.Length + 2;
                        continue;
                    }

                    // Is it an int array?
                    if (trimmedParameters.StartsWith('['))
                    {
                        List<byte> bytes = [];
                        string[] arrayStrings = trimmedParameters[1..].Split(']')[0].Split(',');
                        bytes.AddRange(BitConverter.GetBytes(arrayStrings.Length).Reverse());
                        foreach (string arrayString in arrayStrings)
                        {
                            string trimmedArrayString = arrayString.Trim();
                            string[] parts = trimmedArrayString.Split(' ');
                            bytes.AddRange(CalculateControlStructure(parts[0], parts[1], objects));
                        }

                        i += arrayStrings.Sum(a => a.Length) + arrayStrings.Length + 3;
                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.INTARRAY, Value = [.. bytes], LineNumber = LineNumber });
                        continue;
                    }

                    // Is it lip sync data?
                    if (trimmedParameters.StartsWith("LIPSYNC[", StringComparison.OrdinalIgnoreCase))
                    {
                        List<byte> bytes = [];
                        string lipSyncString = trimmedParameters.Split(']')[0][8..];

                        bytes.AddRange(EncodeLipSyncData(lipSyncString));

                        bytes.AddRange(new byte[4]);

                        bytes.InsertRange(0, BitConverter.GetBytes(bytes.Count + 4).Reverse());

                        i += lipSyncString.Length + 11;
                        Parameters.Add(new() { Type = ScriptCommand.ParameterType.LIPSYNCDATA, Value = [.. bytes], LineNumber = LineNumber });
                        continue;
                    }

                    // Is it unknown?
                    if (trimmedParameters.StartsWith("UNKNOWN", StringComparison.OrdinalIgnoreCase))
                    {
                        List<byte> bytes = [];
                        string parameter = trimmedParameters.Split(',')[0];
                        int typeCode = int.Parse(parameter[7..9], NumberStyles.HexNumber);
                        string[] components = parameter.Split(' ');

                        for (int j = 1; j < components.Length; j++)
                        {
                            bytes.Add(byte.Parse(components[j], NumberStyles.HexNumber));
                        }

                        Parameters.Add(new() { Type = (ScriptCommand.ParameterType)typeCode, Value = [.. bytes], LineNumber = LineNumber });
                        i += parameter.Length + 2;
                        continue;
                    }

                    // It has to be some sort of int; try to see if it's an unknown kind
                    if (int.TryParse(trimmedParameters[0..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int type))
                    {
                        string[] parameter = trimmedParameters[2..].Split(',')[0].Split(' ');

                        Parameters.Add(new() { Type = (ScriptCommand.ParameterType)type, Value = CalculateControlStructure(parameter[0], parameter[1], objects), LineNumber = LineNumber });
                        i += parameter.Sum(p => p.Length) + 4;
                        continue;
                    }

                    // It's a regular int
                    string[] intParams = trimmedParameters.Split(',')[0].Split(' ');
                    Parameters.Add(new() { Type = ScriptCommand.ParameterType.INT, Value = CalculateControlStructure(intParams[0], intParams[1], objects), LineNumber = LineNumber });
                    i += intParams.Sum(p => p.Length) + 2;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"ERROR: Failed to compile line {LineNumber} (command: {Command.Name}): first parameter of '{parameters[i..]}' threw \"{e.Message}\"");
                    return;
                }
            }
        }

        public bool ResolveAddresses(List<(string label, int lineNumber)> labels)
        {
            bool resolvedAddress = false;
            for (int i = 0; i < Parameters.Count; i++)
            {
                if (Parameters[i].Type == ScriptCommand.ParameterType.ADDRESS)
                {
                    int lineNumber;
                    if (Parameters[i].Value[0] == 0x00)
                    {
                        List<byte> tempBytes = new(new byte[] { 0 });
                        tempBytes.AddRange(Parameters[i].Value[1..]);
                        lineNumber = BitConverter.ToInt32(tempBytes.ToArray().Reverse().ToArray()); // why dear god did you do this? bc roslyn won't let me do it w/ List.Reverse() existing
                    }
                    else
                    {
                        List<byte> tempBytes = new(new byte[] { 0 });
                        tempBytes.AddRange(Parameters[i].Value[1..4]);
                        lineNumber = labels[BitConverter.ToInt32(tempBytes.ToArray().Reverse().ToArray())].lineNumber;
                    }
                    int address = AllOtherInvocations.FirstOrDefault(i => i.LineNumber == lineNumber)?.Address ?? -1;
                    if (address < 0)
                    {
                        throw new ArgumentException($"ERROR: Line {LineNumber} (command {Command.Name}) attempting to resolve address to line {lineNumber} when no such line exists.");
                    }
                    Parameters[i] = new Parameter { Type = Parameters[i].Type, Value = BitConverter.GetBytes(address).Reverse().ToArray(), LineNumber = LineNumber };
                    resolvedAddress = true;
                }
                else if (Parameters[i].Type == ScriptCommand.ParameterType.INDEXEDADDRESS)
                {
                    List<byte> parameterBytes = new(Parameters[i].Value);
                    int lineNumber;
                    if (Parameters[i].Value[0] == 0x00)
                    {
                        List<byte> tempBytes = new(new byte[] { 0 });
                        tempBytes.AddRange(Parameters[i].Value[1..4]);
                        lineNumber = BitConverter.ToInt32(tempBytes.ToArray().Reverse().ToArray());
                    }
                    else
                    {
                        List<byte> tempBytes = new(new byte[] { 0 });
                        tempBytes.AddRange(Parameters[i].Value[1..4]);
                        lineNumber = labels[BitConverter.ToInt32(tempBytes.ToArray().Reverse().ToArray())].lineNumber;
                    }
                    int address = AllOtherInvocations.FirstOrDefault(i => i.LineNumber == lineNumber)?.Address ?? -1;
                    if (address < 0)
                    {
                        throw new ArgumentException($"ERROR: Line {LineNumber} (command {Command.Name}) attempting to resolve address to line {lineNumber} when no such line exists.");
                    }
                    parameterBytes.RemoveRange(0, 4);
                    parameterBytes.InsertRange(0, BitConverter.GetBytes(address).Reverse());
                    Parameters[i] = new Parameter { Type = Parameters[i].Type, Value = [.. parameterBytes], LineNumber = LineNumber };
                    resolvedAddress = true;
                }
            }
            return resolvedAddress;
        }

        public string ParseParameter(Parameter parameter, FontReplacementMap fontReplacementMap = null)
        {
            switch (parameter.Type)
            {
                case ScriptCommand.ParameterType.ADDRESS:
                    return ParseAddress(parameter.Value);
                case ScriptCommand.ParameterType.DIALOGUE:
                    string dialogue = ScriptObjects[BitConverter.ToInt16(parameter.Value.Reverse().ToArray())];
                    if (fontReplacementMap is not null)
                    {
                        for (int i = 0; i < dialogue.Length; i++)
                        {
                            ushort character = BitConverter.ToUInt16(Encoding.GetEncoding("Shift-JIS").GetBytes($"{dialogue[i]}"));
                            if (fontReplacementMap.Map.ContainsKey(character))
                            {
                                dialogue = dialogue.Remove(i, 1);
                                dialogue = dialogue.Insert(i, fontReplacementMap.Map[character].Character);
                            }
                        }
                    }
                    return $"\"{dialogue.Replace("\n", "\\n").Replace("\"", "\\\"")}\"";
                case ScriptCommand.ParameterType.CONDITIONAL:
                    return ParseConditional(parameter.Value);
                case ScriptCommand.ParameterType.TIMESPAN:
                    return ParseTime(parameter.Value);
                case ScriptCommand.ParameterType.VECTOR2:
                    return ParseVector2(parameter.Value);
                case ScriptCommand.ParameterType.INT:
                    return CalculateIntParameter(Helpers.GetIntFromByteArray(parameter.Value, 0), Helpers.GetIntFromByteArray(parameter.Value, 1));
                case ScriptCommand.ParameterType.TRANSITION:
                    return ParseTransition(parameter.Value);
                case ScriptCommand.ParameterType.INDEXEDADDRESS:
                    return ParseIndexedAddress(parameter.Value);
                case ScriptCommand.ParameterType.ANGLE:
                    return $"degrees {CalculateIntParameter(Helpers.GetIntFromByteArray(parameter.Value, 0), Helpers.GetIntFromByteArray(parameter.Value, 1))}";
                case ScriptCommand.ParameterType.FACIALEXPRESSION:
                    return $"EXPRESSION[{CalculateIntParameter(Helpers.GetIntFromByteArray(parameter.Value, 0), Helpers.GetIntFromByteArray(parameter.Value, 1))}]";
                case ScriptCommand.ParameterType.BOOL:
                    return ParseBoolean(parameter.Value);
                case ScriptCommand.ParameterType.VOLUME:
                    return $"VOLUME[{CalculateIntParameter(Helpers.GetIntFromByteArray(parameter.Value, 0), Helpers.GetIntFromByteArray(parameter.Value, 1))}]";
                case ScriptCommand.ParameterType.COLOR:
                    return ParseColor(CalculateIntParameter(Helpers.GetIntFromByteArray(parameter.Value, 0), Helpers.GetIntFromByteArray(parameter.Value, 1)));
                case ScriptCommand.ParameterType.CHARACTER:
                    return GetCharacter(CalculateIntParameter(Helpers.GetIntFromByteArray(parameter.Value, 0), Helpers.GetIntFromByteArray(parameter.Value, 1)));
                case ScriptCommand.ParameterType.INT0E:
                    return $"0E{CalculateIntParameter(Helpers.GetIntFromByteArray(parameter.Value, 0), Helpers.GetIntFromByteArray(parameter.Value, 1))}";
                case ScriptCommand.ParameterType.PREVEC:
                    return $"PreVec[{CalculateIntParameter(Helpers.GetIntFromByteArray(parameter.Value, 0), Helpers.GetIntFromByteArray(parameter.Value, 1))}]";
                case ScriptCommand.ParameterType.FLOAT:
                    return ParseFloat(parameter.Value);
                case ScriptCommand.ParameterType.VECTOR3:
                    return ParseVector3(parameter.Value);
                case ScriptCommand.ParameterType.VARINDEX:
                    return $"${ScriptObjects[BitConverter.ToInt16(parameter.Value.Reverse().ToArray())]}";
                case ScriptCommand.ParameterType.INTARRAY:
                    List<string> values = [];
                    int numValues = Helpers.GetIntFromByteArray(parameter.Value, 0);
                    for (int i = 1; i <= numValues; i++)
                    {
                        values.Add(CalculateIntParameter(Helpers.GetIntFromByteArray(parameter.Value, i * 2 - 1), Helpers.GetIntFromByteArray(parameter.Value, i * 2)));
                    }
                    return $"[{string.Join(", ", values)}]";
                case ScriptCommand.ParameterType.INT19:
                    return $"19{CalculateIntParameter(Helpers.GetIntFromByteArray(parameter.Value, 0), Helpers.GetIntFromByteArray(parameter.Value, 1))}";
                case ScriptCommand.ParameterType.LIPSYNCDATA:
                    return ParseLipSyncData(parameter.Value);
                default:
                    return $"{parameter.Type} {string.Join(" ", parameter.Value.Select(b => $"{b:X2}"))}";
            }
        }

        public string CalculateIntParameter(int controlCode, int valueCode)
        {
            switch (controlCode)
            {
                case 0x300:
                    return $"lit {valueCode}";
                case 0x302:
                    if (valueCode - 0x124F < 0)
                    {
                        throw new ArgumentException("Illegal system flag index");
                    }
                    return $"memd {valueCode}";
                case 0x303:
                    return $"memm {valueCode}";
                case 0x304:
                    return $"var {ScriptObjects[valueCode]}";
                default:
                    return "-1";
            }
        }

        public static byte[] CalculateControlStructure(string controlCode, string value, List<string> objects)
        {
            List<byte> bytes = [];

            switch (controlCode)
            {
                case "lit":
                    bytes.AddRange(BitConverter.GetBytes(0x300).Reverse());
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(value)).Reverse());
                    break;
                case "memd":
                    bytes.AddRange(BitConverter.GetBytes(0x302).Reverse());
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(value)).Reverse());
                    break;
                case "memm":
                    bytes.AddRange(BitConverter.GetBytes(0x303).Reverse());
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(value)).Reverse());
                    break;
                case "var":
                    bytes.AddRange(BitConverter.GetBytes(0x304).Reverse());
                    if (!objects.Contains(value))
                    {
                        objects.Add(value);
                    }
                    bytes.AddRange(BitConverter.GetBytes(objects.IndexOf(value)).Reverse());
                    break;
            }

            return [.. bytes];
        }

        public static string GetCharacter(string characterInt)
        {
            if (characterInt.StartsWith("lit "))
            {
                string character = GetCharacter(int.Parse(characterInt[4..]));
                return $"CHARACTER[{character}]";
            }
            else
            {
                return $"CHARACTER[{characterInt}]";
            }
        }

        private static string ParseBoolean(byte[] type0AParameterData)
        {
            return Helpers.GetIntFromByteArray(type0AParameterData, 0) != 0 ? "TRUE" : "FALSE";
        }

        private static string GetCharacter(int characterCode)
        {
            if (characterCode <= 32)
            {
                return ((ScriptFileSpeaker)characterCode).ToString();
            }
            else
            {
                return $"UNKNOWN{characterCode}";
            }
        }

        public static string ParseColor(string colorCode)
        {
            if (colorCode.StartsWith("lit"))
            {
                int color = int.Parse(colorCode[4..]);
                return $"color lit {color:X8}";
            }
            else
            {
                return $"color {colorCode}";
            }
        }

        private static short GetCharacterEntity(string character)
        {
            if (character.StartsWith("UNKNOWN"))
            {
                return short.Parse(character[7..]);
            }
            else
            {
                return (short)((int)Enum.Parse(typeof(ScriptFileSpeaker), character));
            }
        }

        private string ParseAddress(byte[] type00Parameter)
        {
            return $"{{{AllOtherInvocations.First(i => i.Address == Helpers.GetIntFromByteArray(type00Parameter, 0)).Label}}}";
        }

        private string ParseConditional(byte[] conditionalData)
        {
            short numConditions = BitConverter.ToInt16(conditionalData.Skip(2).Take(2).Reverse().ToArray());

            string conditional = "if ";
            byte lastCombiningByte = 0;

            for (int i = 0; i < numConditions; i++)
            {
                byte comparatorByte = conditionalData[4 + i * 18];
                byte combiningByte = conditionalData[5 + i * 18];
                string value1 = CalculateIntParameter(IO.ReadInt(conditionalData, 6 + i * 18), IO.ReadInt(conditionalData, 10 + i * 18));
                string value2 = CalculateIntParameter(IO.ReadInt(conditionalData, 14 + i * 18), IO.ReadInt(conditionalData, 18 + i * 18));

                if (i > 0)
                {
                    switch (lastCombiningByte)
                    {
                        case 0x80:
                            conditional += $" && ";
                            break;
                        case 0x81:
                            conditional += $" || ";
                            break;
                        case 0x82:
                            break;
                    }
                }

                switch (comparatorByte)
                {
                    case 0x83:
                        conditional += $"{value1} == {value2}";
                        break;
                    case 0x84:
                        conditional += $"{value1} != {value2}";
                        break;
                    case 0x85:
                        conditional += $"{value1} > {value2}";
                        break;
                    case 0x86:
                        conditional += $"{value1} < {value2}";
                        break;
                    case 0x87:
                        conditional += $"{value1} >= {value2}";
                        break;
                    case 0x88:
                        conditional += $"{value1} <= {value2}";
                        break;
                    case 0x89:
                        conditional += $"{value1}";
                        break;
                    default:
                        break;
                }

                lastCombiningByte = combiningByte;
            }

            return conditional;
        }

        private string ParseTime(byte[] type03Parameter)
        {
            string param = Helpers.GetIntFromByteArray(type03Parameter, 0) == 0x201 ? "time_s " : "time_f ";
            param += CalculateIntParameter(Helpers.GetIntFromByteArray(type03Parameter, 1), Helpers.GetIntFromByteArray(type03Parameter, 2));
            return param;
        }

        private string ParseTransition(byte[] type06Parameter)
        {
            string parameter = CalculateIntParameter(Helpers.GetIntFromByteArray(type06Parameter, 0), Helpers.GetIntFromByteArray(type06Parameter, 1));
            if (parameter.StartsWith("lit "))
            {
                int parsedInt = int.Parse(parameter[4..]);
                string transition = ScriptCommand.TransitionToCodeMap.FirstOrDefault(t => t.Value == parsedInt).Key;
                if (transition is not null)
                {
                    return $"Transition[{transition}]";
                }
                else
                {
                    return $"Transition[{parameter}]";
                }
            }
            else
            {
                return $"Transition[{parameter}]";
            }
        }

        private string ParseFloat(byte[] type12Parameter)
        {
            string startingString = CalculateIntParameter(Helpers.GetIntFromByteArray(type12Parameter, 0), Helpers.GetIntFromByteArray(type12Parameter, 1));
            if (startingString.StartsWith("lit"))
            {
                int startingInt = int.Parse(startingString.Split(' ')[1]);
                return $"float lit {Helpers.IntToFloat(startingInt)}";
            }
            else
            {
                return $"float {startingString}";
            }
        }

        private string ParseIndexedAddress(byte[] type07Parameter)
        {
            string lineNumber = ParseAddress(type07Parameter[0..4]);
            string alwaysUse = Helpers.GetIntFromByteArray(type07Parameter, 1) != 0 ? "TRUE" : "FALSE";
            string index = CalculateIntParameter(Helpers.GetIntFromByteArray(type07Parameter, 2), Helpers.GetIntFromByteArray(type07Parameter, 3));

            return $"({lineNumber}, {alwaysUse}, {index})";
        }

        private string ParseVector2(byte[] type04Parameter)
        {
            string[] coords = new string[]
            {
                CalculateIntParameter(Helpers.GetIntFromByteArray(type04Parameter, 0), Helpers.GetIntFromByteArray(type04Parameter, 1)),
                CalculateIntParameter(Helpers.GetIntFromByteArray(type04Parameter, 2), Helpers.GetIntFromByteArray(type04Parameter, 3)),
            };

            return $"Vector2({coords[0]}, {coords[1]})";
        }

        private string ParseVector3(byte[] type14Parameter)
        {
            string[] coords = new string[]
            {
                ParseFloat(type14Parameter[0..8]),
                ParseFloat(type14Parameter[8..16]),
                ParseFloat(type14Parameter[16..24]),
            };

            return $"Vector3({coords[0]}, {coords[1]}, {coords[2]})";
        }

        public static List<byte> EncodeLipSyncData(string lipSyncString)
        {
            List<byte> bytes = [];
            Regex lipSyncRegex = new(@"(?<lipFlap>[saiueonN])(?<length>\d+)");

            for (int j = 0; j < lipSyncString.Length;)
            {
                Match nextLipSync = lipSyncRegex.Match(lipSyncString[j..]);
                bytes.Add(ScriptCommand.LipSyncMap.FirstOrDefault(l => l.Value == nextLipSync.Groups["lipFlap"].Value).Key);
                bytes.Add(byte.Parse(nextLipSync.Groups["length"].Value));
                j += nextLipSync.Value.Length;
            }

            return bytes;
        }
        private static string ParseLipSyncData(byte[] type1DParameter)
        {
            string lipSyncData = "LIPSYNC[";

            for (int i = 4; i < type1DParameter.Length - 4; i += 2)
            {
                lipSyncData += $"{ScriptCommand.LipSyncMap[type1DParameter[i]]}{type1DParameter[i + 1]}";
            }

            return $"{lipSyncData}]";
        }
    }
}
