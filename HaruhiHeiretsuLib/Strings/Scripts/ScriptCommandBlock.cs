using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HaruhiHeiretsuLib.Util;

namespace HaruhiHeiretsuLib.Strings.Scripts;

/// <summary>
/// A representation of a block of script ocmmands
/// </summary>
public class ScriptCommandBlock
{
    /// <summary>
    /// Location of the script command block *definition*
    /// </summary>
    public int DefinitionAddress { get; set; }
    /// <summary>
    /// Index of the name of the block
    /// </summary>
    public ushort NameIndex { get; set; }
    /// <summary>
    /// The block's name
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// The number of invocations (script commands) in the block
    /// </summary>
    public ushort NumInvocations { get; set; }
    /// <summary>
    /// Location of the actual script command block
    /// </summary>
    public int BlockOffset { get; set; }
    /// <summary>
    /// The list of script commands in the block
    /// </summary>
    public List<ScriptCommandInvocation> Invocations { get; set; } = [];
    /// <summary>
    /// Length of the block in bytes
    /// </summary>
    public int Length => Invocations.Sum(i => i.Length);

    /// <summary>
    /// Constructs a script command block
    /// </summary>
    /// <param name="address">The start address of the block</param>
    /// <param name="endAddress">The end address of the block</param>
    /// <param name="data">The block data</param>
    /// <param name="objects">The list of all script objects</param>
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

    /// <summary>
    /// Empty constructor
    /// </summary>
    public ScriptCommandBlock()
    {
    }

    /// <summary>
    /// Populates the script block with the available command definitions
    /// </summary>
    /// <param name="availableCommands">The available commands</param>
    public void PopulateCommands(List<ScriptCommand> availableCommands)
    {
        for (int i = 0; i < Invocations.Count; i++)
        {
            Invocations[i].Command = availableCommands[Invocations[i].CommandCode];
        }
    }

    /// <summary>
    /// Parses a script block
    /// </summary>
    /// <param name="lineNumber">The line number of the block</param>
    /// <param name="lines">The dialogue lines</param>
    /// <param name="allCommands">The list of all script commands</param>
    /// <param name="objects">The list of script objects</param>
    /// <param name="labels">The list of labels</param>
    /// <param name="fontReplacementMap">The font replacement map</param>
    /// <returns>The next line number</returns>
    /// <exception cref="ArgumentException">Thrown if can't find a matching block name</exception>
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

            Invocations.Add(new(lines[i], (short)(i + lineNumber), allCommands, objects, labels, fontReplacementMap));
        }

        NumInvocations = (ushort)Invocations.Count;

        return i + lineNumber;
    }
}