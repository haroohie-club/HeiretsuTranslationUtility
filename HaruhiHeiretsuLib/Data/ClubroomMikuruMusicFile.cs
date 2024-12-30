using HaruhiHeiretsuLib.Strings;
using HaruhiHeiretsuLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HaruhiHeiretsuLib.Data
{
    /// <summary>
    /// Class representing the clubroom Mikuru music file (dat.bin 73/74)
    /// </summary>
    public class ClubroomMikuruMusicFile : DataFile, IDataStringsFile
    {
        /// <summary>
        /// List of entries in the clubroom music player
        /// </summary>
        public List<ClubroomMusicPlayerEntry> MusicPlayerEntries { get; set; } = [];

        /// <inheritdoc/>
        public override void Initialize(byte[] decompressedData, int offset)
        {
            base.Initialize(decompressedData, offset);

            int numSections = IO.ReadInt(decompressedData, 0x00);
            if (numSections != 1)
            {
                throw new ArgumentException($"Incorrect number of sections in clubroom Mikuru music file: expected 1 section but {numSections} detected");
            }

            int sectionOffset = IO.ReadInt(decompressedData, 0x0C);
            int numTracks = IO.ReadInt(decompressedData, 0x10);
            for (int i = 0; i < numTracks; i++)
            {
                MusicPlayerEntries.Add(new(decompressedData, sectionOffset + i * 0x24));
            }
        }

        /// <inheritdoc/>
        public List<DialogueLine> GetDialogueLines()
        {
            List<DialogueLine> lines = [];

            lines.AddRange(MusicPlayerEntries.Select((m, i) => new DialogueLine { Speaker = $"Music Player Track Title {i:D2}", Metadata = [m.Flag], Line = m.Title }));
            lines.AddRange(MusicPlayerEntries.Select((m, i) => new DialogueLine { Speaker = $"Music Player Hover Line {i:D2}", Metadata = [m.Flag, m.HoverSpeaker, m.HoverVoiceFile], Line = m.HoverLine }));
            lines.AddRange(MusicPlayerEntries.Select((m, i) => new DialogueLine { Speaker = $"Music Player Selected Line {i:D2}", Metadata = [m.Flag, m.SelectedSpeaker, m.SelectedVoiceFile], Line = m.SelectedLine }));

            return lines;
        }

        /// <inheritdoc/>
        public void ReplaceDialogueLine(DialogueLine line)
        {
            int idx = int.Parse(line.Speaker[^2..]);
            if (line.Speaker.Contains("Title"))
            {
                MusicPlayerEntries[idx].Title = line.Line;
            }
            else if (line.Speaker.Contains("Hover"))
            {
                MusicPlayerEntries[idx].HoverLine = line.Line;
            }
            else if (line.Speaker.Contains("Selected"))
            {
                MusicPlayerEntries[idx].SelectedLine = line.Line;
            }
            else
            {
                throw new("Oh no! I couldn't find a line replacement!");
            }
        }
    }

    /// <summary>
    /// Class representing an entry in the clubroom Mikuru music file
    /// </summary>
    /// <remarks>
    /// Constructs a clubroom music player entry from binary clubroom music file data and an offset into that data
    /// </remarks>
    /// <param name="data">The clubroom Mikuru music file binary data</param>
    /// <param name="offset">The offset of this entry into that data</param>
    public class ClubroomMusicPlayerEntry(byte[] data, int offset)
    {
        /// <summary>
        /// The title of the music track
        /// </summary>
        public string Title { get; set; } = IO.ReadShiftJisString(data, IO.ReadInt(data, offset));
        /// <summary>
        /// Unknown
        /// </summary>
        public short Unknown04 { get; set; } = IO.ReadShort(data, offset + 0x04);
        /// <summary>
        /// Unknown
        /// </summary>
        public short Unknown06 { get; set; } = IO.ReadShort(data, offset + 0x06);
        /// <summary>
        /// The flag that, when set, unlocks this music track in the clubroom
        /// </summary>
        public string Flag { get; set; } = IO.ReadShiftJisString(data, IO.ReadInt(data, offset + 0x08));
        /// <summary>
        /// The voice file that plays when hovering over this track
        /// </summary>
        public string HoverVoiceFile { get; set; } = IO.ReadShiftJisString(data, IO.ReadInt(data, offset + 0x0C));
        /// <summary>
        /// The speaker of the line that's displayed when hovering over this track
        /// </summary>
        public string HoverSpeaker { get; set; } = IO.ReadShiftJisString(data, IO.ReadInt(data, offset + 0x10));
        /// <summary>
        /// The line that's displayed when hovering over this track
        /// </summary>
        public string HoverLine { get; set; } = IO.ReadShiftJisString(data, IO.ReadInt(data, offset + 0x14));
        /// <summary>
        /// The voice file that plays when this track is selected
        /// </summary>
        public string SelectedVoiceFile { get; set; } = IO.ReadShiftJisString(data, IO.ReadInt(data, offset + 0x18));
        /// <summary>
        /// The speaker of the line that's displayed when this track is selected
        /// </summary>
        public string SelectedSpeaker { get; set; } = IO.ReadShiftJisString(data, IO.ReadInt(data, offset + 0x1C));
        /// <summary>
        /// The line that's displayed when this track is selected
        /// </summary>
        public string SelectedLine { get; set; } = IO.ReadShiftJisString(data, IO.ReadInt(data, offset + 0x20));
    }
}
