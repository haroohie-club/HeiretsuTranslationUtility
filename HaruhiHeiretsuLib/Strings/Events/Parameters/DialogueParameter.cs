using HaruhiHeiretsuLib.Strings.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiHeiretsuLib.Strings.Events.Parameters
{
    public class DialogueParameter : ActionParameter
    {
        public int Unknown0C { get; set; }
        public int Unknown10 { get; set; } // pointer
        public ushort Unknown14 { get; set; }
        public int Unknown1C { get; set; }
        public int Unknown20 { get; set; }
        public ushort Unknown24 { get; set; }
        public ushort Unknown26 { get; set; }
        public ushort Unknown28 { get; set; }
        public ushort Unknown2A { get; set; }
        public EventFileSpeaker SpeakingCharacter { get; set; }
        public string VoiceFile { get; set; }
        public string Dialogue { get; set; }
        public string LipSyncData { get; set; } = string.Empty;
        public DialogueParameter(IEnumerable<byte> data, int offset, ushort opCode) : base(data, offset, opCode)
        {
            Unknown0C = BitConverter.ToInt32(data.Skip(offset + 0x0C).Take(4).ToArray());
            Unknown10 = BitConverter.ToInt32(data.Skip(offset + 0x10).Take(4).ToArray());
            Unknown14 = BitConverter.ToUInt16(data.Skip(offset + 0x14).Take(2).ToArray());
            Unknown1C = BitConverter.ToInt32(data.Skip(offset + 0x1C).Take(4).ToArray());
            Unknown20 = BitConverter.ToInt32(data.Skip(offset + 0x20).Take(4).ToArray());
            Unknown24 = BitConverter.ToUInt16(data.Skip(offset + 0x24).Take(2).ToArray());
            Unknown26 = BitConverter.ToUInt16(data.Skip(offset + 0x26).Take(2).ToArray());
            Unknown28 = BitConverter.ToUInt16(data.Skip(offset + 0x28).Take(2).ToArray());
            Unknown2A = BitConverter.ToUInt16(data.Skip(offset + 0x2A).Take(2).ToArray());
            SpeakingCharacter = (EventFileSpeaker)BitConverter.ToInt32(data.Skip(offset + 0x2C).Take(4).ToArray());
            VoiceFile = Encoding.ASCII.GetString(data.Skip(offset + 0x30).TakeWhile(b => b != 0x00).ToArray());
            Dialogue = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(offset + 0x50).TakeWhile(b => b != 0x00).ToArray());
            byte[] lipSyncData = data.Skip(offset + 0xD0).TakeWhile(b => b != 0x00).ToArray();
            for (int i = 0; i < lipSyncData.Length; i += 2)
            {
                LipSyncData += $"{ScriptCommand.LipSyncMap[lipSyncData[i]]}{lipSyncData[i + 1]}";
            }
        }

        public override List<byte> GetBytes()
        {
            List<byte> bytes = new();
            bytes.AddRange(GetHeaderBytes());
            bytes.AddRange(BitConverter.GetBytes(Unknown0C));
            bytes.AddRange(BitConverter.GetBytes(Unknown10));
            bytes.AddRange(BitConverter.GetBytes(Unknown14));
            bytes.AddRange(new byte[6]);
            bytes.AddRange(BitConverter.GetBytes(Unknown20));
            bytes.AddRange(BitConverter.GetBytes(Unknown24));
            bytes.AddRange(BitConverter.GetBytes(Unknown26));
            bytes.AddRange(BitConverter.GetBytes(Unknown28));
            bytes.AddRange(BitConverter.GetBytes(Unknown2A));
            bytes.AddRange(BitConverter.GetBytes((int)SpeakingCharacter));
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
}
