using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HaruhiHeiretsuLib.Util;

namespace HaruhiHeiretsuLib.Strings.Events;

// 0x3C bytes (0x20 bytes of padding)
/// <summary>
/// Definition of an actor in a cutscene
/// </summary>
public class ActorDefinition
{
    /// <summary>
    /// The offset of the associated chapter definition
    /// </summary>
    public int ChapterDefinitionOffset { get; set; }
    /// <summary>
    /// The type of actor
    /// </summary>
    public ActorType Type{ get; set; }
    /// <summary>
    /// The name of the model to use
    /// </summary>
    public string ModelName { get; set; }
    /// <summary>
    /// The number of actions
    /// </summary>
    public short ActionsCount { get; set; }
    /// <summary>
    /// The table of actions
    /// </summary>
    public int ActionsTableAddress { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown1C { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public byte Unknown20 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public byte Unknown21 { get; set; }
    /// <summary>
    /// The dat.bin index of the SGE data
    /// </summary>
    public short SgeDatIndex { get; set; }
    /// <summary>
    /// The action definitions table
    /// </summary>
    public List<ActionDefinition> ActionsTable { get; set; } = [];

    /// <summary>
    /// Constructs an actor definition from raw binary
    /// </summary>
    /// <param name="data">The cutscene file binary data</param>
    /// <param name="offset">The offset into the cutscene data of the actor definition</param>
    public ActorDefinition(byte[] data, int offset)
    {
        ChapterDefinitionOffset = IO.ReadIntLE(data, offset + 0x00);
        Type = (ActorType)IO.ReadShortLE(data, offset + 0x04);
        byte[] nameBytes = data.Skip(offset + 0x06).TakeWhile(b => b != 0x00).ToArray();
        if (nameBytes.Count() > 0x10)
        {
            ModelName = IO.ReadAsciiString(nameBytes, offset + 0x10);
        }
        else
        {
            ModelName = IO.ReadAsciiString(nameBytes, 0x00);
        }
        ActionsCount = IO.ReadShortLE(data, offset + 0x16);
        ActionsTableAddress = IO.ReadIntLE(data, offset + 0x18);

        for (int i = 0; i < ActionsCount; i++)
        {
            ActionsTable.Add(new(data, ActionsTableAddress + 0x38 * i));
        }
    }

    /// <summary>
    /// Return binary representation of the actor definition
    /// </summary>
    /// <returns>Byte array representing the actor definition</returns>
    public List<byte> GetBytes()
    {
        List<byte> bytes = [.. BitConverter.GetBytes(ChapterDefinitionOffset), .. BitConverter.GetBytes((short)Type)];
        byte[] modelNameBytes = Encoding.ASCII.GetBytes(ModelName);
        bytes.AddRange(modelNameBytes);
        bytes.AddRange(new byte[0x10 - modelNameBytes.Length]);
        bytes.AddRange(BitConverter.GetBytes(ActionsCount));
        bytes.AddRange(BitConverter.GetBytes(ActionsTableAddress));
        bytes.AddRange(BitConverter.GetBytes(Unknown1C));
        bytes.Add(Unknown20);
        bytes.Add(Unknown21);
        bytes.AddRange(BitConverter.GetBytes(SgeDatIndex));
        bytes.AddRange(new byte[0x18]);

        return bytes;
    }
}

/// <summary>
/// The different types of actor in a scene
/// </summary>
public enum ActorType : short
{
    /// <summary>
    /// None
    /// </summary>
    NONE = 0,
    /// <summary>
    /// Scene camera
    /// </summary>
    CAMERA = 1,
    /// <summary>
    /// A modeled actor
    /// </summary>
    MODEL = 2,
    /// <summary>
    /// Unknown
    /// </summary>
    UNKNOWN03 = 3,
    /// <summary>
    /// Screen (used for fade in/out)
    /// </summary>
    SCREEN = 4,
    /// <summary>
    /// Unknown
    /// </summary>
    UNKNOWN06 = 6,
    /// <summary>
    /// Unknown
    /// </summary>
    ENVIRONMENT = 7,
    /// <summary>
    /// Screen effect
    /// </summary>
    SCREEN_EFFECT = 8,
    /// <summary>
    /// Actor for managing cross-fades between scenes
    /// </summary>
    CROSS_FADE = 9,
    /// <summary>
    /// Unknown
    /// </summary>
    UNKNOWN10 = 10,
    /// <summary>
    /// Unknown
    /// </summary>
    SFX_ENGINE = 11,
    /// <summary>
    /// Unknown
    /// </summary>
    UNKNOWN12 = 12,
    /// <summary>
    /// Dialogue actor (used for displaying dialogue)
    /// </summary>
    DIALOGUE = 13,
    /// <summary>
    /// Unknown
    /// </summary>
    ZERO_MAP = 14,
}