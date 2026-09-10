using HaruhiHeiretsuLib.Strings.Events.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using HaruhiHeiretsuLib.Util;

namespace HaruhiHeiretsuLib.Strings.Events;

// 0x38 bytes
/// <summary>
/// An action that an actor can take in a cutscene
/// </summary>
public class ActionDefinition
{
    /// <summary>
    /// The address of the actor definition associated with this action
    /// </summary>
    public int ActorDefinitionAddress { get; set; }

    /// <summary>
    /// The op code for this action)
    /// </summary>
    public ActionOpCode OpCode { get; set; }

    /// <summary>
    /// Unknown
    /// </summary>
    public ushort Unknown06 { get; set; }

    /// <summary>
    /// The number of parameters
    /// </summary>
    public ushort ParametersCount { get; set; }

    /// <summary>
    /// Unknown
    /// </summary>
    public ushort Unknown0A { get; set; }

    /// <summary>
    /// The address of the parameters table
    /// </summary>
    public int ParametersAddress { get; set; }

    /// <summary>
    /// The actual list of parameters for the action
    /// </summary>
    public List<ActionParameter> Parameters { get; set; } = [];

    /// <summary>
    /// Constructs an action from binary data
    /// </summary>
    /// <param name="data">The binary event file data</param>
    /// <param name="offset">The offset of the action data into the event data</param>
    public ActionDefinition(byte[] data, int offset)
    {
        ActorDefinitionAddress = IO.ReadIntLE(data, offset + 0x00);
        OpCode = (ActionOpCode)IO.ReadUShortLE(data, offset + 0x04);
        Unknown06 = IO.ReadUShortLE(data, offset + 0x06);
        ParametersCount = IO.ReadUShortLE(data, offset + 0x08);
        Unknown0A = IO.ReadUShortLE(data, offset + 0x0A);
        ParametersAddress = IO.ReadIntLE(data, offset + 0x0C);
        
        int currentPosition = ParametersAddress;
        for (int i = 0; i < ParametersCount; i++)
        {
            switch (OpCode)
            {
                case ActionOpCode.CAMERA_POSITION:
                case ActionOpCode.CAMERA_LOOK_TO:
                case ActionOpCode.ANIMATION_PATH:
                case ActionOpCode.UNKNOWN09:
                case ActionOpCode.UNKNOWN16:
                case ActionOpCode.ZERO_MAP_TRANSFORM:
                    Parameters.Add(new SpatialParameter(data, currentPosition, OpCode));
                    break;
                case ActionOpCode.PLAY_MODEL_ANIMATION:
                case ActionOpCode.ZERO_MAP_MESH:
                    Parameters.Add(new ModelAnimationParameter(data, currentPosition, OpCode));
                    break;
                case ActionOpCode.FADE:
                    Parameters.Add(new FadeParameter(data, currentPosition, OpCode));
                    break;
                case ActionOpCode.FOG:
                    Parameters.Add(new FogParam(data, currentPosition, OpCode));
                    break;
                case ActionOpCode.CAMERA_RANGE:
                    Parameters.Add(new CameraRangeParameter(data, currentPosition, OpCode));
                    break;
                case ActionOpCode.SCREEN_FEEDBACK:
                    Parameters.Add(new ScreenFeedbackParameter(data, currentPosition, OpCode));
                    break;
                case ActionOpCode.DIALOGUE:
                    Parameters.Add(new DialogueParameter(data, currentPosition, OpCode));
                    break;
                case ActionOpCode.UNKNOWN05:
                // break
                case ActionOpCode.UNKNOWN06:
                //break
                case ActionOpCode.UNKNOWN08:
                //break
                case ActionOpCode.UNKNOWN0A:
                //break
                case ActionOpCode.UNKNOWN0D:
                //break
                case ActionOpCode.UNKNOWN0E:
                //break
                case ActionOpCode.UNKNOWN10:
                //break
                case ActionOpCode.UNKNOWN11:
                //break
                case ActionOpCode.UNKNOWN12:
                case ActionOpCode.UNKNOWN17:
                //break
                case ActionOpCode.UNKNOWN13:
                //break
                case ActionOpCode.UNKNOWN15:
                //break
                default:
                    Parameters.Add(new(data, currentPosition, OpCode));
                    break;
            }

            currentPosition += Parameters.Last().Length;
        }
    }

    /// <summary>
    /// Gets the binary data of the action
    /// </summary>
    /// <returns>A list of bytes representing the binary data</returns>
    public List<byte> GetBytes()
    {
        List<byte> bytes =
        [
            .. BitConverter.GetBytes(ActorDefinitionAddress),
            .. BitConverter.GetBytes((ushort)OpCode),
            .. BitConverter.GetBytes(Unknown06),
            .. BitConverter.GetBytes(ParametersCount),
            .. BitConverter.GetBytes(Unknown0A),
            .. BitConverter.GetBytes(ParametersAddress),
        ];

        return bytes;
    }
}

/// <summary>
/// Enum representing action op codes
/// </summary>
public enum ActionOpCode : ushort
{
    /// <summary>
    /// No action
    /// </summary>
    NONE,

    /// <summary>
    /// Position the camera in space
    /// </summary>
    CAMERA_POSITION,

    /// <summary>
    /// Set the position the camera is looking toward
    /// </summary>
    CAMERA_LOOK_TO,

    /// <summary>
    /// Play an animation on a model
    /// </summary>
    PLAY_MODEL_ANIMATION,

    /// <summary>
    /// Move an actor along a path
    /// </summary>
    ANIMATION_PATH,

    /// <summary>
    /// Unknown
    /// </summary>
    UNKNOWN05,

    /// <summary>
    /// Unknown
    /// </summary>
    UNKNOWN06,

    /// <summary>
    /// Unknown
    /// </summary>
    FADE,

    /// <summary>
    /// Unknown
    /// </summary>
    UNKNOWN08,

    /// <summary>
    /// Unknown
    /// </summary>
    UNKNOWN09,

    /// <summary>
    /// Unknown
    /// </summary>
    UNKNOWN0A,

    /// <summary>
    /// Unknown
    /// </summary>
    FOG,

    /// <summary>
    /// Unknown
    /// </summary>
    CAMERA_RANGE,

    /// <summary>
    /// Unknown
    /// </summary>
    UNKNOWN0D,

    /// <summary>
    /// Unknown
    /// </summary>
    UNKNOWN0E,

    /// <summary>
    /// Screen feedback effect (capture the framebuffer to a texture and re-render it)
    /// </summary>
    SCREEN_FEEDBACK,

    /// <summary>
    /// Unknown
    /// </summary>
    UNKNOWN10,

    /// <summary>
    /// Unknown
    /// </summary>
    UNKNOWN11,

    /// <summary>
    /// Unknown
    /// </summary>
    UNKNOWN12,

    /// <summary>
    /// Unknown
    /// </summary>
    UNKNOWN13,

    /// <summary>
    /// Display dialogue on the screen
    /// </summary>
    DIALOGUE,

    /// <summary>
    /// Unknown
    /// </summary>
    UNKNOWN15,

    /// <summary>
    /// Unknown
    /// </summary>
    UNKNOWN16,

    /// <summary>
    /// Unknown
    /// </summary>
    UNKNOWN17,

    /// <summary>
    /// Sets the mesh index of the zero map (skybox)
    /// </summary>
    ZERO_MAP_MESH,

    /// <summary>
    /// Sets the transform of the zero map (skybox)
    /// </summary>
    ZERO_MAP_TRANSFORM,
}

/// <summary>
/// Enum representing the mnemonics for action parameters
/// </summary>
public enum ActionParameterMnemonic
{
}

/// <summary>
/// A generic action parameter
/// </summary>
[JsonDerivedType(typeof(CameraRangeParameter))]
[JsonDerivedType(typeof(DialogueParameter))]
[JsonDerivedType(typeof(FadeParameter))]
[JsonDerivedType(typeof(FogParam))]
[JsonDerivedType(typeof(ModelAnimationParameter))]
[JsonDerivedType(typeof(ScreenFeedbackParameter))]
[JsonDerivedType(typeof(SpatialParameter))]
// Variable length
public class ActionParameter
{
    private readonly ActionOpCode _opCode;
    /// <summary>
    /// The action associated with this parameter
    /// </summary>
    public int ActionsTableEntryAddress { get; set; }
    /// <summary>
    /// The address of this parameter
    /// </summary>
    public int Address { get; set; }
    /// <summary>
    /// The frame at which this action starts
    /// </summary>
    public float StartFrame { get; set; }
    /// <summary>
    /// The frame at which this action ends
    /// </summary>
    public float EndFrame { get; set; }
    [JsonIgnore]
    private List<byte> Data { get; set; }

    /// <summary>
    /// The length of the action data in bytes
    /// </summary>
    public int Length
    {
        get
        {
            return _opCode switch
            {
                ActionOpCode.CAMERA_POSITION or ActionOpCode.CAMERA_LOOK_TO or ActionOpCode.ANIMATION_PATH
                    or ActionOpCode.UNKNOWN09 or ActionOpCode.UNKNOWN16 or ActionOpCode.ZERO_MAP_TRANSFORM
                    or ActionOpCode.UNKNOWN06 => 0x40,
                ActionOpCode.PLAY_MODEL_ANIMATION or ActionOpCode.ZERO_MAP_MESH => 0x48,
                ActionOpCode.UNKNOWN05 or ActionOpCode.UNKNOWN08 or ActionOpCode.UNKNOWN13 or ActionOpCode.UNKNOWN12
                    or ActionOpCode.UNKNOWN17 => 0x2C,
                ActionOpCode.FADE => 0x28,
                ActionOpCode.UNKNOWN0A or ActionOpCode.UNKNOWN11 => 0x24,
                ActionOpCode.FOG => 0x4C,
                ActionOpCode.CAMERA_RANGE => 0x38,
                ActionOpCode.UNKNOWN0D or ActionOpCode.UNKNOWN0E => 0x50,
                ActionOpCode.SCREEN_FEEDBACK => 0x58,
                ActionOpCode.UNKNOWN10 or ActionOpCode.UNKNOWN15 => 0x20,
                ActionOpCode.DIALOGUE => 0x250,
                _ => 0,
            };
        }
    }
    
    /// <summary>
    /// Constructs an action parameter from binary data
    /// </summary>
    /// <param name="data">The event file binary data</param>
    /// <param name="offset">The offset into the event file binary data at which the action parameter starts</param>
    /// <param name="opCode">The op code for the action associated with this parameter</param>
    public ActionParameter(byte[] data, int offset, ActionOpCode opCode)
    {
        Address = offset;
        _opCode = opCode;
        ActionsTableEntryAddress = BitConverter.ToInt32([.. data.Skip(offset).Take(4)]);
        StartFrame = BitConverter.ToSingle([.. data.Skip(offset + 0x04).Take(4)]);
        EndFrame = BitConverter.ToSingle([.. data.Skip(offset + 0x08).Take(4)]);
        Data = [.. data.Skip(offset + 0x0C).Take(Length - 12)];
    }

    /// <summary>
    /// Gets the binary data for the header of this action
    /// </summary>
    /// <returns>Binary data for this action parameter's header</returns>
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

    /// <summary>
    /// Gets the binary data of this action parameter
    /// </summary>
    /// <returns></returns>
    public virtual List<byte> GetBytes()
    {
        List<byte> bytes = [.. GetHeaderBytes(), .. Data];
        return bytes;
    }
}