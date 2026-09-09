using System.Collections.Generic;
using System.Linq;
using System.Text;
using HaruhiHeiretsuLib.Util;

namespace HaruhiHeiretsuLib.Strings.Scripts;

/// <summary>
/// Representation of a script command
/// </summary>
public class ScriptCommand
{
    /// <summary>
    /// Index of the script command
    /// </summary>
    public short Index { get; set; }
    /// <summary>
    /// Number of parameters
    /// </summary>
    public ushort NumberOfParameters => (ushort)Parameters.Count;
    /// <summary>
    /// Mnemonic of the script command
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// The script command's parameters
    /// </summary>
    public List<ushort> Parameters { get; set; } = [];
    /// <summary>
    /// Definition length
    /// </summary>
    public int DefinitionLength { get; set; }

    private ScriptCommand()
    {
    }

    /// <summary>
    /// Constructs a script command from binary data
    /// </summary>
    /// <param name="data">Binary data</param>
    public ScriptCommand(byte[] data)
    {
        Index = IO.ReadShort(data, 0x00);
        ushort numParams = IO.ReadUShort(data, 0x02);
        int nameLength = IO.ReadInt(data, 0x04);
        Name = Encoding.ASCII.GetString(data.Skip(8).Take(nameLength - 1).ToArray()); // minus one bc of the terminal character \x00 that we want to avoid
        for (ushort i = 0; i < numParams; i++)
        {
            Parameters.Add(IO.ReadUShort(data, 8 + nameLength + i * 2));
        }
        DefinitionLength = 8 + nameLength + numParams * 2;
    }

    /// <summary>
    /// Parses the script command file data (scr.bin 002)
    /// </summary>
    /// <param name="scriptCommandFileData">The data from the script command file</param>
    /// <returns>A list of script commands (available commands)</returns>
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

    /// <inheritdoc/>
    public override string ToString()
    {
        return Name;
    }

    /// <summary>
    /// Gets the signature of the command
    /// </summary>
    /// <returns>The command's signature</returns>
    public string GetSignature()
    {
        return $"{Index:X2} {Name}({string.Join(", ", Parameters.Select(s => $"{s:X4}"))})";
    }

    /// <summary>
    /// Gets the length of a parameter in bytes
    /// </summary>
    /// <param name="paramTypeCode">Param type</param>
    /// <param name="data">Binary command data (used for variable length params)</param>
    /// <returns>The length of a parameter</returns>
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

    /// <summary>
    /// The type of parameter
    /// </summary>
    public enum ParameterType
    {
        /// <summary>
        /// Address of another part of the script
        /// </summary>
        ADDRESS = 0,
        /// <summary>
        /// Dialogue line
        /// </summary>
        DIALOGUE = 1,
        /// <summary>
        /// A conditional
        /// </summary>
        CONDITIONAL = 2,
        /// <summary>
        /// A timespan
        /// </summary>
        TIMESPAN = 3,
        /// <summary>
        /// 2-dimensional vector
        /// </summary>
        VECTOR2 = 4,
        /// <summary>
        /// An integer control structure
        /// </summary>
        INT = 5,
        /// <summary>
        /// A transition
        /// </summary>
        TRANSITION = 6,
        /// <summary>
        /// An indexed-address
        /// </summary>
        INDEXED_ADDRESS = 7,
        /// <summary>
        /// An angle
        /// </summary>
        ANGLE = 8,
        /// <summary>
        /// A facial expression
        /// </summary>
        FACIAL_EXPRESSION = 9,
        /// <summary>
        /// A boolean
        /// </summary>
        BOOL = 10,
        /// <summary>
        /// Volume (of a sound)
        /// </summary>
        VOLUME = 11,
        /// <summary>
        /// Color
        /// </summary>
        COLOR = 12,
        /// <summary>
        /// A character
        /// </summary>
        CHARACTER = 13,
        /// <summary>
        /// A different kind of integer (0E)
        /// </summary>
        INT0E = 14,
        /// <summary>
        /// Unknown
        /// </summary>
        UNKNOWN0F = 15,
        /// <summary>
        /// A precalculated vector
        /// </summary>
        PREVEC = 16,
        /// <summary>
        /// Unknown
        /// </summary>
        UNKNOWN11 = 17,
        /// <summary>
        /// A float
        /// </summary>
        FLOAT = 18,
        /// <summary>
        /// Unknown
        /// </summary>
        UNKNOWN13 = 19,
        /// <summary>
        /// A 3D vector
        /// </summary>
        VECTOR3 = 20,
        /// <summary>
        /// An indexed variable
        /// </summary>
        VAR_INDEX = 21,
        /// <summary>
        /// An integer array
        /// </summary>
        INT_ARRAY = 22,
        /// <summary>
        /// Unknown
        /// </summary>
        UNKNOWN17 = 23,
        /// <summary>
        /// Unknown
        /// </summary>
        UNKNOWN18 = 24,
        /// <summary>
        /// Integer (19)
        /// </summary>
        INT19 = 25,
        /// <summary>
        /// Unknown
        /// </summary>
        UNKNOWN1A = 26,
        /// <summary>
        /// Unknown
        /// </summary>
        UNKNOWN1B = 27,
        /// <summary>
        /// Unknown
        /// </summary>
        UNKNOWN1C = 28,
        /// <summary>
        /// Lip sync data
        /// </summary>
        LIP_SYNC_DATA = 29,
        /// <summary>
        /// Unknown
        /// </summary>
        UNKNOWN29 = 41,
        /// <summary>
        /// Unknown
        /// </summary>
        UNKNOWN2A = 42,
    }

    /// <summary>
    /// Map of compariosn operators to their corresponding codes
    /// </summary>
    public static readonly Dictionary<string, byte> ComparisonOperatorToCodeMap = new()
    {
        { "==", 0x83 },
        { "!=", 0x84 },
        { ">", 0x85 },
        { "<", 0x86 },
        { ">=", 0x87 },
        { "<=", 0x88 },
    };

    /// <summary>
    /// Map of transition names to their corresponding codes (should be enum???)
    /// </summary>
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

    /// <summary>
    /// The map of lip sync codes to their values
    /// </summary>
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