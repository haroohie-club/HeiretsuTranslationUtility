using System.Collections.Generic;
using System.Linq;
using System.Text;
using HaruhiHeiretsuLib.Util;

namespace HaruhiHeiretsuLib.Strings.Scripts
{
    public class ScriptCommand
    {
        public short Index { get; set; }
        public ushort NumberOfParameters => (ushort)Parameters.Count;
        public string Name { get; set; }
        public List<ushort> Parameters { get; set; } = [];
        public int DefinitionLength { get; set; }

        private ScriptCommand()
        {
        }

        public ScriptCommand(byte[] data)
        {
            Index = IO.ReadShort(data, 2);
            ushort numParams = IO.ReadUShort(data, 0x02);
            int nameLength = IO.ReadInt(data, 0x04);
            Name = Encoding.ASCII.GetString(data.Skip(8).Take(nameLength - 1).ToArray()); // minus one bc of the terminal character \x00 that we want to avoid
            for (ushort i = 0; i < numParams; i++)
            {
                Parameters.Add(IO.ReadUShort(data, 8 + nameLength + i * 2));
            }
            DefinitionLength = 8 + nameLength + numParams * 2;
        }

        public static List<ScriptCommand> ParseScriptCommandFile(byte[] scriptCommandFileData)
        {
            int numCommands = IO.ReadInt(scriptCommandFileData, 0x00);
            List<ScriptCommand> scriptCommands = [];

            for (int i = 4; scriptCommands.Count < numCommands;)
            {
                ScriptCommand scriptCommand = new(scriptCommandFileData[i..]);
                scriptCommands.Add(scriptCommand);
                i += scriptCommand.DefinitionLength;
            }

            return scriptCommands;
        }

        public override string ToString()
        {
            return Name;
        }

        public string GetSignature()
        {
            return $"{Index:X2} {Name}({string.Join(", ", Parameters.Select(s => $"{s:X4}"))})";
        }

        public static int GetParameterLength(short paramTypeCode, byte[] data)
        {
            switch (paramTypeCode)
            {
                case 1:
                case 21:
                case 28:
                    return 2;
                case 0:
                case 10:
                    return 4;
                case 5:
                case 6:
                case 8:
                case 9:
                case 11:
                case 12:
                case 13:
                case 14:
                case 16:
                case 18:
                case 19:
                case 23:
                case 24:
                case 25:
                case 26:
                case 27:
                case 41:
                case 42:
                    return 8;
                case 3:
                    return 12;
                case 4:
                case 7:
                case 15:
                    return 16;
                case 20:
                    return 24;
                case 2:
                    return IO.ReadShort(data, 0); // *a2
                case 17:
                case 22:
                    return 8 * IO.ReadInt(data, 0) + 4; // 8 * *(_DWORD *)a2 + 4
                case 29:
                    return IO.ReadInt(data, 0); // *(_DWORD *)a2
                default:
                    return 0;
            }
        }

        public enum ParameterType
        {
            ADDRESS = 0,
            DIALOGUE = 1,
            CONDITIONAL = 2,
            TIMESPAN = 3,
            VECTOR2 = 4,
            INT = 5,
            TRANSITION = 6,
            INDEXEDADDRESS = 7,
            ANGLE = 8,
            FACIALEXPRESSION = 9,
            BOOL = 10,
            VOLUME = 11,
            COLOR = 12,
            CHARACTER = 13,
            INT0E = 14,
            UNKNOWN0F = 15,
            PREVEC = 16,
            UNKNOWN11 = 17,
            FLOAT = 18,
            UNKNOWN13 = 19,
            VECTOR3 = 20,
            VARINDEX = 21,
            INTARRAY = 22,
            UNKNOWN17 = 23,
            UNKNOWN18 = 24,
            INT19 = 25,
            UNKNOWN1A = 26,
            UNKNOWN1B = 27,
            UNKNOWN1C = 28,
            LIPSYNCDATA = 29,
            UNKNOWN29 = 41,
            UNKNOWN2A = 42,
        }

        public static readonly Dictionary<string, byte> ComparisonOperatorToCodeMap = new()
        {
            { "==", 0x83 },
            { "!=", 0x84 },
            { ">", 0x85 },
            { "<", 0x86 },
            { ">=", 0x87 },
            { "<=", 0x88 },
        };

        public static readonly Dictionary<string, int> TransitionToCodeMap = new()
        {
            { "HARD_CUT", 0 },
            { "CROSS_DISSOLVE", 1 },
            { "PUSH_RIGHT", 2 },
            { "PUSH_LEFT", 3 },
            { "PUSH_UP", 4 },
            { "PUSH_DOWN", 5 },
            { "WIPE_RIGHT", 6 },
            { "WIPE_LEFT", 7 },
            { "WIPE_UP", 8 },
            { "WIPE_DOWN", 9 },
            { "HORIZONTAL_BLINDS", 10 },
            { "VERTICAL_BLINDS", 11 },
            { "CENTER_OUT", 12 },
            { "RED_SETTINGS_BG", 13 },
            { "BLACK_SETTINGS_BG", 14 },
            { "FADE_TO_BLACK", 500 },
        };

        public static readonly Dictionary<byte, string> LipSyncMap = new()
        {
            { 0x01, "s" },
            { 0x02, "a" },
            { 0x03, "i" },
            { 0x04, "u" },
            { 0x05, "e" },
            { 0x06, "o" },
            { 0x07, "n" },
            { 0xF0, "N" },
        };
    }
}
