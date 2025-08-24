using HaruhiHeiretsuLib.Strings.Events.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using HaruhiHeiretsuLib.Util;

namespace HaruhiHeiretsuLib.Strings.Events
{
    // 0x38 bytes
    /*
     * Op Code Table:
     * 0x01 - Camera Position
     * 0x02 - Camera Look-To
     * 0x03 - Play Model Animation
     * 0x04 - Animation Path
     * 0x05 -
     * 0x06 -
     * 0x07 -
     * 0x08 -
     * 0x09 -
     * 0x0A -
     * 0x0B -
     * 0x0C -
     * 0x0D -
     * 0x0E -
     * 0x0F -
     * 0x10 -
     * 0x11 -
     * 0x12 -
     * 0x13 -
     * 0x14 - Dialogue
     * 0x15 -
     * 0x16 -
     * 0x17 -
     * 0x18 - 
     * 0x19 - 
     */
    public class ActionDefinition
    {
        public int ActorDefinitionAddress { get; set; }
        public ushort OpCode { get; set; }
        public ushort Unknown06 { get; set; }
        public ushort ParametersCount { get; set; }
        public ushort Unknown0A { get; set; }
        public int ParametersAddress { get; set; }
        public List<ActionParameter> Parameters { get; set; } = [];

        public ActionDefinition(byte[] data, int offset)
        {
            ActorDefinitionAddress = IO.ReadIntLE(data, offset + 0x00);
            OpCode = IO.ReadUShortLE(data, offset + 0x04);
            Unknown06 = IO.ReadUShortLE(data, offset + 0x06);
            ParametersCount = IO.ReadUShortLE(data, offset + 0x08);
            Unknown0A = IO.ReadUShortLE(data, offset + 0x0A);
            ParametersAddress = IO.ReadIntLE(data, offset + 0x0C);

            int currentPosition = ParametersAddress;
            for (int i = 0; i < ParametersCount; i++)
            {
                switch (OpCode)
                {
                    case 1:
                    case 2:
                    case 4:
                    case 9:
                    case 22:
                    case 25:
                        Parameters.Add(new SpatialParameter(data, currentPosition, OpCode));
                        break;
                    case 3:
                    case 24:
                        Parameters.Add(new ModelAnimationParameter(data, currentPosition, OpCode));
                        break;
                    case 7:
                        Parameters.Add(new FadeParameter(data, currentPosition, OpCode));
                        break;
                    case 20:
                        Parameters.Add(new DialogueParameter(data, currentPosition, OpCode));
                        break;
                    case 5:
                    // break
                    case 6:
                    //break
                    case 8:
                    //break
                    case 10:
                    //break
                    case 11:
                    //break
                    case 12:
                    //break
                    case 13:
                    //break
                    case 14:
                    //break
                    case 15:
                    //break
                    case 16:
                    //break
                    case 17:
                    //break
                    case 18:
                    case 23:
                    //break
                    case 19:
                    //break
                    case 21:
                    //break
                    default:
                        Parameters.Add(new(data, currentPosition, OpCode));
                        break;
                }
                currentPosition += Parameters.Last().Length;
            }
        }

        public List<byte> GetBytes()
        {
            List<byte> bytes =
            [
                .. BitConverter.GetBytes(ActorDefinitionAddress),
                .. BitConverter.GetBytes(OpCode),
                .. BitConverter.GetBytes(Unknown06),
                .. BitConverter.GetBytes(ParametersCount),
                .. BitConverter.GetBytes(Unknown0A),
                .. BitConverter.GetBytes(ParametersAddress),
            ];

            return bytes;
        }
    }

    /// <summary>
    /// Enum representing the mnemonics for action parameters
    /// </summary>
    public enum ActionParameterMnemonic
    {
        
    }

    [JsonDerivedType(typeof(DialogueParameter))]
    [JsonDerivedType(typeof(FadeParameter))]
    [JsonDerivedType(typeof(ModelAnimationParameter))]
    [JsonDerivedType(typeof(SpatialParameter))]
    // Variable length
    public class ActionParameter
    {
        private readonly ushort _opCode;
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
                    5 or 8 or 19 or 18 or 23 => 0x2C,
                    7 => 0x28,
                    10 or 17 => 0x24,
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

        public ActionParameter(byte[] data, int offset, ushort opCode)
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
            List<byte> bytes =
            [
                .. BitConverter.GetBytes(ActionsTableEntryAddress),
                .. BitConverter.GetBytes(StartFrame),
                .. BitConverter.GetBytes(EndFrame),
            ];
            return bytes;
        }

        public virtual List<byte> GetBytes()
        {
            List<byte> bytes = [.. GetHeaderBytes(), .. Data];
            return bytes;
        }
    }
}
