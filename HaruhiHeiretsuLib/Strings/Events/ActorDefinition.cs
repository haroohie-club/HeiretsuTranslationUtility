using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiHeiretsuLib.Strings.Events
{
    // 0x3C bytes (0x20 bytes of padding)
    public class ActorDefinition
    {
        public int ChapterDefinitionOffset { get; set; }
        public short Unknown04 { get; set; }
        public string ModelName { get; set; }
        public short ActionsCount { get; set; }
        public int ActionsTableAddress { get; set; }
        public List<ActionDefinition> ActionsTable { get; set; } = new();

        public ActorDefinition(IEnumerable<byte> data, int offset)
        {
            ChapterDefinitionOffset = BitConverter.ToInt32(data.Skip(offset).Take(4).ToArray());
            Unknown04 = BitConverter.ToInt16(data.Skip(offset + 0x04).Take(2).ToArray());
            IEnumerable<byte> nameBytes = data.Skip(offset + 0x06).TakeWhile(b => b != 0x00);
            if (nameBytes.Count() > 0x10)
            {
                ModelName = Encoding.ASCII.GetString(nameBytes.Take(offset + 0x10).ToArray());
            }
            else
            {
                ModelName = Encoding.ASCII.GetString(nameBytes.ToArray());
            }
            ActionsCount = BitConverter.ToInt16(data.Skip(offset + 0x16).Take(2).ToArray());
            ActionsTableAddress = BitConverter.ToInt32(data.Skip(offset + 0x18).Take(4).ToArray());

            for (int i = 0; i < ActionsCount; i++)
            {
                ActionsTable.Add(new(data, ActionsTableAddress + 0x38 * i));
            }
        }

        public List<byte> GetBytes()
        {
            List<byte> bytes = new();

            bytes.AddRange(BitConverter.GetBytes(ChapterDefinitionOffset));
            bytes.AddRange(BitConverter.GetBytes(Unknown04));
            byte[] modelNameBytes = Encoding.ASCII.GetBytes(ModelName);
            bytes.AddRange(modelNameBytes);
            bytes.AddRange(new byte[0x10 - modelNameBytes.Length]);
            bytes.AddRange(BitConverter.GetBytes(ActionsCount));
            bytes.AddRange(BitConverter.GetBytes(ActionsTableAddress));

            return bytes;
        }
    }
}
