﻿using HaruhiHeiretsuLib.Strings.Events.Parameters;
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
        public ushort Unknown06 { get; set; }
        public ushort ParametersCount { get; set; }
        public ushort Unknown0A { get; set; }
        public int ParametersAddress { get; set; }
        public List<ActionParameter> Parameters { get; set; } = new();

        public ActionDefinition(IEnumerable<byte> data, int offset)
        {
            ActorDefinitionAddress = BitConverter.ToInt32(data.Skip(offset).Take(4).ToArray());
            OpCode = BitConverter.ToUInt16(data.Skip(offset + 0x04).Take(2).ToArray());
            Unknown06 = BitConverter.ToUInt16(data.Skip(offset + 0x06).Take(2).ToArray());
            ParametersCount = BitConverter.ToUInt16(data.Skip(offset + 0x08).Take(2).ToArray());
            Unknown0A = BitConverter.ToUInt16(data.Skip(offset + 0x0A).Take(2).ToArray());
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
            bytes.AddRange(BitConverter.GetBytes(Unknown06));
            bytes.AddRange(BitConverter.GetBytes(ParametersCount));
            bytes.AddRange(BitConverter.GetBytes(Unknown0A));
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
                return _opCode switch
                {
                    1 or 2 or 4 or 9 or 22 or 25 or 6 => 0x40,
                    3 or 24 => 0x48,
                    5 or 8 or 19 => 0x2C,
                    7 => 0x28,
                    10 or 17 or 18 or 23 => 0x24,
                    11 => 0x4C,
                    12 => 0x38,
                    13 or 14 => 0x50,
                    15 => 0x58,
                    16 or 21 => 0x20,
                    20 => 0x250,
                    _ => 0,
                };
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
