using HaruhiHeiretsuLib.Strings;
using HaruhiHeiretsuLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HaruhiHeiretsuLib.Data
{
    /// <summary>
    /// Class representing sound data file (dat.bin 0x001/0x002)
    /// </summary>
    public class SoundDataFile : DataFile, IDataStringsFile
    {
        /// <summary>
        /// List of groups of sounds which are loaded by maps
        /// </summary>
        public List<string> SoundGroups { get; set; } = [];
        /// <summary>
        /// Unknown
        /// </summary>
        public List<int> Unknown02s { get; set; } = [];
        /// <summary>
        /// List of sound effects
        /// </summary>
        public List<SoundEffectEntry> SoundEffects { get; set; } = [];
        /// <summary>
        /// List of background music tracks
        /// </summary>
        public List<string> Bgms { get; set; } = [];
        /// <summary>
        /// Unknown
        /// </summary>
        public List<short> Unknown05s { get; set; } = [];
        /// <summary>
        /// Unknown
        /// </summary>
        public List<string> Unknown06s { get; set; } = [];
        /// <summary>
        /// List of BGM metadata entries describing the title, etc., for the BGMs
        /// </summary>
        public List<SoundDebugMenuEntry> BgmDebugMenu { get; set; } = [];
        /// <summary>
        /// List of SFX metadata entries describing the title, etc., for the sound effects
        /// </summary>
        public List<SoundDebugMenuEntry> SfxDebugMenu { get; set; } = [];
        /// <summary>
        /// List of ST metadata entries describing the title, etc., for the STs (whatever those are)
        /// </summary>
        public List<SoundDebugMenuEntry> StDebugMenu { get; set; } = [];



        /// <inheritdoc/>
        public override void Initialize(byte[] decompressedData, int offset)
        {
            base.Initialize(decompressedData, offset);

            int numSections = IO.ReadInt(decompressedData, 0);
            if (numSections != 9)
            {
                throw new ArgumentException($"Sound data file expects 9 sections but {numSections} were detected!");
            }

            int soundGroupsOffset = IO.ReadInt(decompressedData, 0x0C);
            int numSoundGroups = IO.ReadInt(decompressedData, 0x10);
            for (int i = 0; i < numSoundGroups; i++)
            {
                SoundGroups.Add(IO.ReadAsciiString(decompressedData, IO.ReadInt(decompressedData, soundGroupsOffset + i * 4)));
            }

            int unknown02sOffset = IO.ReadInt(decompressedData, 0x14);
            int numUnknown02s = IO.ReadInt(decompressedData, 0x18);
            for (int i = 0; i < numUnknown02s; i++)
            {
                Unknown02s.Add(IO.ReadInt(decompressedData, unknown02sOffset + i * 4));
            }

            int soundEffectsOffset = IO.ReadInt(decompressedData, 0x1C);
            int numSoundEffects = IO.ReadInt(decompressedData, 0x20);
            for (int i = 0; i < numSoundEffects; i++)
            {
                SoundEffects.Add(new(
                    IO.ReadAsciiString(decompressedData, IO.ReadInt(decompressedData, soundEffectsOffset + i * 0x0C)),
                    IO.ReadFloat(decompressedData, soundEffectsOffset + i * 0x0C + 4),
                    IO.ReadFloat(decompressedData, soundEffectsOffset + i * 0x0C + 8)));
            }

            int bgmsOffset = IO.ReadInt(decompressedData, 0x24);
            int numBgms = IO.ReadInt(decompressedData, 0x28);
            for (int i = 0; i < numBgms; i++)
            {
                Bgms.Add(IO.ReadAsciiString(decompressedData, IO.ReadInt(decompressedData, bgmsOffset + i * 4)));
            }

            int unknown05sOffset = IO.ReadInt(decompressedData, 0x2C);
            int numUnknown05s = IO.ReadInt(decompressedData, 0x30);
            for (int i = 0; i < numUnknown05s; i++)
            {
                Unknown05s.Add(IO.ReadShort(decompressedData, unknown05sOffset + i * 2));
            }

            int unknown06sOffset = IO.ReadInt(decompressedData, 0x34);
            int numUnknown06s = IO.ReadInt(decompressedData, 0x38);
            for (int i = 0; i < numUnknown06s; i++)
            {
                Unknown06s.Add(IO.ReadAsciiString(decompressedData, IO.ReadInt(decompressedData, unknown06sOffset + i * 4)));
            }

            int bgmMetadataOffset = IO.ReadInt(decompressedData, 0x3C);
            int numBgmMetadatas = IO.ReadInt(decompressedData, 0x40);
            for (int i = 0; i < numBgmMetadatas; i++)
            {
                BgmDebugMenu.Add(new(decompressedData.Skip(bgmMetadataOffset + i * 0x28).Take(0x28), IO.ReadShiftJisString(decompressedData, IO.ReadInt(decompressedData, bgmMetadataOffset + i * 0x28 + 4))));
            }

            int sfxMetadataOffset = IO.ReadInt(decompressedData, 0x44);
            int numSfxMetadatas = IO.ReadInt(decompressedData, 0x48);
            for (int i = 0; i < numSfxMetadatas; i++)
            {
                SfxDebugMenu.Add(new(decompressedData.Skip(sfxMetadataOffset + i * 0x28).Take(0x28), IO.ReadShiftJisString(decompressedData, IO.ReadInt(decompressedData, sfxMetadataOffset + i * 0x28 + 4))));
            }

            int stMetadataOffset = IO.ReadInt(decompressedData, 0x4C);
            int numStMetadatas = IO.ReadInt(decompressedData, 0x50);
            for (int i = 0; i < numStMetadatas; i++)
            {
                SfxDebugMenu.Add(new(decompressedData.Skip(stMetadataOffset + i * 0x28).Take(0x28), IO.ReadShiftJisString(decompressedData, IO.ReadInt(decompressedData, stMetadataOffset + i * 0x28 + 4))));
            }
        }

        /// <inheritdoc/>
        public override byte[] GetBytes()
        {
            return base.GetBytes();
        }

        /// <inheritdoc/>
        public List<DialogueLine> GetDialogueLines()
        {
            List<DialogueLine> lines = [];
            lines.AddRange(BgmDebugMenu.Select((b, i) => new DialogueLine() { Speaker = $"BGM Debug Menu ({i:D2})", Line = b.DebugMenuTitle }));
            lines.AddRange(SfxDebugMenu.Select((s, i) => new DialogueLine() { Speaker = $"SFX Debug Menu ({i:D2})", Line = s.DebugMenuTitle }));
            lines.AddRange(StDebugMenu.Select((s, i) => new DialogueLine() { Speaker = $"ST Debug Menu ({i:D2})", Line = s.DebugMenuTitle }));
            return lines;
        }

        /// <inheritdoc/>
        public void ReplaceDialogueLine(DialogueLine line)
        {
            if (line.Speaker.StartsWith("BGM"))
            {
                BgmDebugMenu[int.Parse(line.Speaker[^3..^1])].DebugMenuTitle = line.Line;
            }
            if (line.Speaker.StartsWith("SFX"))
            {
                SfxDebugMenu[int.Parse(line.Speaker[^3..^1])].DebugMenuTitle = line.Line;
            }
            if (line.Speaker.StartsWith("ST"))
            {
                StDebugMenu[int.Parse(line.Speaker[^3..^1])].DebugMenuTitle = line.Line;
            }
        }
    }

    /// <summary>
    /// An entry in the sound effect table in the sound data file
    /// </summary>
    /// <param name="name">Name of the sound effect</param>
    /// <param name="unknownFloat1">Unknown</param>
    /// <param name="unknownFloat2">Unknown</param>
    public class SoundEffectEntry(string name, float unknownFloat1, float unknownFloat2)
    {
        /// <summary>
        /// Name of the sound effect
        /// </summary>
        public string Name { get; set; } = name;
        /// <summary>
        /// Unknown
        /// </summary>
        public float UnknownFloat1 { get; set; } = unknownFloat1;
        /// <summary>
        /// Unknown
        /// </summary>
        public float UnknownFloat2 { get; set; } = unknownFloat2;
    }

    /// <summary>
    /// An entry in one of the sound debug menu tables in the sound data file
    /// </summary>
    public class SoundDebugMenuEntry(IEnumerable<byte> data, string idAndTitle)
    {
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown00 { get; set; } = IO.ReadInt(data, 0x00);
        /// <summary>
        /// The title as it would appear in the debug menu
        /// </summary>
        public string DebugMenuTitle { get; set; } = idAndTitle;
        /// <summary>
        /// Unknown
        /// </summary>
        public short Unknown08 { get; set; } = IO.ReadShort(data, 0x08);
        /// <summary>
        /// Unknown
        /// </summary>
        public short Unknown0A { get; set; } = IO.ReadShort(data, 0x0A);
        /// <summary>
        /// Unknown
        /// </summary>
        public short Unknown0C { get; set; } = IO.ReadShort(data, 0x0C);
        /// <summary>
        /// Unknown
        /// </summary>
        public short Unknown0E { get; set; } = IO.ReadShort(data, 0x0E);
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown10 { get; set; } = IO.ReadInt(data, 0x10);
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown14 { get; set; } = IO.ReadInt(data, 0x14);
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown18 { get; set; } = IO.ReadInt(data, 0x18);
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown1C { get; set; } = IO.ReadInt(data, 0x1C);
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown20 { get; set; } = IO.ReadInt(data, 0x20);
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown24 { get; set; } = IO.ReadInt(data, 0x24);
    }
}
