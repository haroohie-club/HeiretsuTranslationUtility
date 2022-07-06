using HaruhiHeiretsuLib.Strings.Events.Parameters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HaruhiHeiretsuLib.Strings.Events
{
    // 0x38 bytes
    public class ActionDefinition
    {
        public int ActorDefinitionAddress { get; set; }
        public ushort OpCode { get; set; }
        public ushort ParametersCount { get; set; }
        public int ParametersAddress { get; set; }
        public List<ActionParameter> Parameters { get; set; } = new();

        public ActionDefinition(IEnumerable<byte> data, int offset)
        {
            ActorDefinitionAddress = BitConverter.ToInt32(data.Skip(offset).Take(4).ToArray());
            OpCode = BitConverter.ToUInt16(data.Skip(offset + 0x04).Take(2).ToArray());
            ParametersCount = BitConverter.ToUInt16(data.Skip(offset + 0x08).Take(2).ToArray());
            ParametersAddress = BitConverter.ToInt32(data.Skip(offset + 0x0C).Take(4).ToArray());

            int currentPosition = ParametersAddress;
            for (int i = 0; i < ParametersCount; i++)
            {
                switch (OpCode)
                {
                    case 20:
                        Parameters.Add(new DialogueParameter(data, currentPosition, OpCode));
                        break;
                    default:
                        Parameters.Add(new(data, currentPosition, OpCode));
                        break;
                }
                currentPosition += Parameters.Last().Length;
            }
        }

        public List<byte> GetBytes()
        {
            List<byte> bytes = new();

            bytes.AddRange(BitConverter.GetBytes(ActorDefinitionAddress));
            bytes.AddRange(BitConverter.GetBytes(OpCode));
            bytes.AddRange(BitConverter.GetBytes(ParametersAddress));
            bytes.AddRange(BitConverter.GetBytes(ParametersAddress));

            return bytes;
        }
    }

    // Variable length
    public class ActionParameter
    {
        private ushort _opCode;
        public int ActionsTableEntryAddress { get; set; }
        public int Address { get; set; }
        public float StartFrame { get; set; }
        public float EndFrame { get; set; }
        [JsonIgnore]
        public List<byte> Data { get; set; }
        public int Length
        {
            get
            {
                switch (_opCode)
                {
                    case 1:
                    case 2:
                    case 4:
                    case 9:
                    case 22:
                    case 25:
                    case 6:
                        return 0x40;
                    case 3:
                    case 24:
                        return 0x48;
                    case 5:
                    case 8:
                    case 19:
                        return 0x2C;
                    case 7:
                        return 0x28;
                    case 10:
                    case 17:
                    case 18:
                    case 23:
                        return 0x24;
                    case 11:
                        return 0x4C;
                    case 12:
                        return 0x38;
                    case 13:
                    case 14:
                        return 0x50;
                    case 15:
                        return 0x58;
                    case 16:
                    case 21:
                        return 0x20;
                    case 20:
                        return 0x250;
                    default:
                        return 0;
                }
            }
        }

        public ActionParameter(IEnumerable<byte> data, int offset, ushort opCode)
        {
            Address = offset;
            _opCode = opCode;
            ActionsTableEntryAddress = BitConverter.ToInt32(data.Skip(offset).Take(4).ToArray());
            StartFrame = BitConverter.ToSingle(data.Skip(offset + 0x04).Take(4).ToArray());
            EndFrame = BitConverter.ToSingle(data.Skip(offset + 0x08).Take(4).ToArray());
            Data = data.Skip(offset + 0x0C).Take(Length - 12).ToList();
        }

        protected List<byte> GetHeaderBytes()
        {
            List<byte> bytes = new();
            bytes.AddRange(BitConverter.GetBytes(ActionsTableEntryAddress));
            bytes.AddRange(BitConverter.GetBytes(StartFrame));
            bytes.AddRange(BitConverter.GetBytes(EndFrame));
            return bytes;
        }

        public virtual List<byte> GetBytes()
        {
            List<byte> bytes = new();
            bytes.AddRange(GetHeaderBytes());
            bytes.AddRange(Data);
            return bytes;
        }
    }
}
