using HaruhiHeiretsuLib.Strings.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HaruhiHeiretsuLib.Util;

namespace HaruhiHeiretsuLib.Strings.Events.Parameters;

/// <summary>
/// A parameter that contains a dialogue line
/// </summary>
public class DialogueParameter : ActionParameter
{
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown0C { get; set; }
    /// <summary>
    /// Unknown pointer
    /// </summary>
    public int Unknown10 { get; set; } // pointer
    /// <summary>
    /// Unknown
    /// </summary>
    public ushort Unknown14 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public ushort Unknown16 { get; set; } // guess
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown18 { get; set; } // guess
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown1C { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown20 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public ushort Unknown24 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public ushort Unknown26 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public ushort Unknown28 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public ushort Unknown2A { get; set; }
    /// <summary>
    /// The character speaking the dialogue line
    /// </summary>
    public EventFileSpeaker SpeakingCharacter { get; set; }
    /// <summary>
    /// The voice file to play with this line
    /// </summary>
    public string VoiceFile { get; set; }
    /// <summary>
    /// The dialogue to display on the screen (subtitle)
    /// </summary>
    public string Dialogue { get; set; }
    /// <summary>
    /// The lip sync data to play on the character model
    /// </summary>
    public string LipSyncData { get; set; } = string.Empty;
    
    /// <inheritdoc/>
    public DialogueParameter(byte[] data, int offset, ActionOpCode opCode) : base(data, offset, opCode)
    {
        Unknown0C = IO.ReadIntLE(data, offset + 0x0C);
        Unknown10 = IO.ReadIntLE(data, offset + 0x10);
        Unknown14 = IO.ReadUShortLE(data, offset + 0x14);
        Unknown16 = IO.ReadUShortLE(data, offset + 0x16);
        Unknown18 = IO.ReadIntLE(data, offset + 0x18);
        Unknown1C = IO.ReadIntLE(data, offset + 0x1C);
        Unknown20 = IO.ReadIntLE(data, offset + 0x20);
        Unknown24 = IO.ReadUShortLE(data, offset + 0x24);
        Unknown26 = IO.ReadUShortLE(data, offset + 0x26);
        Unknown28 = IO.ReadUShortLE(data, offset + 0x28);
        Unknown2A = IO.ReadUShortLE(data, offset + 0x2A);
        SpeakingCharacter = (EventFileSpeaker)IO.ReadIntLE(data, offset + 0x2C);
        VoiceFile = IO.ReadAsciiString(data, offset + 0x30);
        Dialogue = IO.ReadShiftJisString(data, offset + 0x50);
        byte[] lipSyncData = data.Skip(offset + 0xD0).TakeWhile(b => b != 0x00).ToArray();
        for (int i = 0; i < lipSyncData.Length; i += 2)
        {
            LipSyncData += $"{ScriptCommand.LipSyncMap[lipSyncData[i]]}{lipSyncData[i + 1]}";
        }
    }

    /// <inheritdoc />
    public override List<byte> GetBytes()
    {
        List<byte> bytes =
        [
            .. GetHeaderBytes(),
            .. BitConverter.GetBytes(Unknown0C),
            .. BitConverter.GetBytes(Unknown10),
            .. BitConverter.GetBytes(Unknown14),
            .. BitConverter.GetBytes(Unknown16),
            .. BitConverter.GetBytes(Unknown18),
            .. BitConverter.GetBytes(Unknown1C),
            .. BitConverter.GetBytes(Unknown20),
            .. BitConverter.GetBytes(Unknown24),
            .. BitConverter.GetBytes(Unknown26),
            .. BitConverter.GetBytes(Unknown28),
            .. BitConverter.GetBytes(Unknown2A),
            .. BitConverter.GetBytes((int)SpeakingCharacter),
        ];
        byte[] voiceFileBytes = Encoding.ASCII.GetBytes(VoiceFile);
        bytes.AddRange(voiceFileBytes);
        bytes.AddRange(new byte[0x20 - voiceFileBytes.Length]);
        byte[] dialogueBytes = Encoding.GetEncoding("Shift-JIS").GetBytes(Dialogue);
        bytes.AddRange(dialogueBytes);
        bytes.AddRange(new byte[0x80 - dialogueBytes.Length]);
        List<byte> lipSyncBytes = ScriptCommandInvocation.EncodeLipSyncData(LipSyncData);
        bytes.AddRange(lipSyncBytes);
        bytes.AddRange(new byte[0x180 - lipSyncBytes.Count]);
        return bytes;
    }
}