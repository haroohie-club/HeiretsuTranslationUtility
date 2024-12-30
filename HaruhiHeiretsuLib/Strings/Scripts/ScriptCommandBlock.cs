using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HaruhiHeiretsuLib.Util;

namespace HaruhiHeiretsuLib.Strings.Scripts
{
    public class ScriptCommandBlock
    {
        public int DefinitionAddress { get; set; } // Location of the script command block *definition*
        public ushort NameIndex { get; set; }
        public string Name { get; set; }
        public ushort NumInvocations { get; set; }
        public int BlockOffset { get; set; } // Location of the actual script command block
        public List<ScriptCommandInvocation> Invocations { get; set; } = [];
        public int Length => Invocations.Sum(i => i.Length);

        public ScriptCommandBlock(int address, int endAddress, byte[] data, List<string> objects)
        {
            DefinitionAddress = address;
            NameIndex = IO.ReadUShort(data, address);
            Name = objects[NameIndex];
            NumInvocations = IO.ReadUShort(data, address + 0x02);
            BlockOffset = IO.ReadInt(data, address + 0x04);

            for (int i = BlockOffset; i < endAddress - 8;)
            {
                ScriptCommandInvocation invocation = new(objects, i);
                invocation.LineNumber = IO.ReadShort(data, i);
                i += 2;
                invocation.CharacterEntity = IO.ReadShort(data, i);
                i += 2;
                invocation.CommandCode = IO.ReadShort(data, i);
                i += 2;
                short numParams = IO.ReadShort(data, i);
                i += 6;
                for (int j = 0; j < numParams; j++)
                {
                    short paramTypeCode = IO.ReadShort(data, i);
                    i += 2;
                    int paramLength = ScriptCommand.GetParameterLength(paramTypeCode, data[i..]);
                    invocation.Parameters.Add(new() { Type = (ScriptCommand.ParameterType)paramTypeCode, Value = data.Skip(i).Take(paramLength).ToArray(), LineNumber = invocation.LineNumber });
                    i += paramLength;
                }
                Invocations.Add(invocation);
            }
        }

        public ScriptCommandBlock()
        {
        }

        public void PopulateCommands(List<ScriptCommand> availableCommands)
        {
            for (int i = 0; i < Invocations.Count; i++)
            {
                Invocations[i].Command = availableCommands[Invocations[i].CommandCode];
            }
        }

        public int ParseBlock(int lineNumber, string[] lines, List<ScriptCommand> allCommands, List<string> objects, List<(string, int)> labels, FontReplacementMap fontReplacementMap = null)
        {
            Regex nameRegex = new(@"== (?<name>.+) ==");
            Match nameMatch = nameRegex.Match(lines[0]);
            if (!nameMatch.Success)
            {
                throw new ArgumentException($"Name {lines[0]} not a valid block name!");
            }

            Name = nameMatch.Groups["name"].Value;
            if (!objects.Contains(Name))
            {
                objects.Add(Name);
                NameIndex = (ushort)(objects.Count - 1);
            }
            else
            {
                NameIndex = (ushort)objects.IndexOf(Name);
            }

            int i = 1;
            for (; i < lines.Length; i++)
            {
                if (nameRegex.IsMatch(lines[i]))
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                Invocations.Add(new ScriptCommandInvocation(lines[i], (short)(i + lineNumber), allCommands, objects, labels, fontReplacementMap));
            }

            NumInvocations = (ushort)Invocations.Count;

            return i + lineNumber;
        }
    }
}
