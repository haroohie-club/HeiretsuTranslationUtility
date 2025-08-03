using HaruhiHeiretsuLib.Strings;
using HaruhiHeiretsuLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiHeiretsuLib.Data
{
    /// <summary>
    /// File which defines map loading information
    /// </summary>
    public class MapDefinitionsFile : DataFile, IDataStringsFile
    {
        /// <summary>
        /// The map definition sections
        /// </summary>
        public List<MapDefinitionSection> Sections { get; set; } = [];

        /// <summary>
        /// Empty constructor
        /// </summary>
        public MapDefinitionsFile()
        {
            Name = "Map Definitions";
        }

        /// <inheritdoc />
        public override void Initialize(byte[] decompressedData, int offset)
        {
            base.Initialize(decompressedData, offset);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            int numSections = IO.ReadInt(decompressedData, 0x00);

            for (int i = 0; i < numSections; i++)
            {
                int sectionPointer = IO.ReadInt(decompressedData, 0x0C + i * 8);
                int sectionItemCount = IO.ReadInt(decompressedData, 0x10 + i * 8);

                Sections.Add(new(decompressedData, i + 2, sectionPointer, sectionItemCount));
            }
        }

        /// <inheritdoc />
        public MapDefinitionsFile(string[] csvLines, int index, int offset)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            IEnumerable<IGrouping<string, string>> csvGroups = csvLines.Skip(1).GroupBy(l => l[0..l.IndexOf('-')]);
            Sections = csvGroups.Select(g => new MapDefinitionSection([.. g], int.Parse(g.Key))).ToList();
        }

        /// <inheritdoc />
        public override byte[] GetBytes()
        {
            List<byte> bytes = [];
            List<byte> sectionBytes = [];
            List<int> endPointers = [];
            bytes.AddRange(BitConverter.GetBytes(Sections.Count).Reverse());
            bytes.AddRange(new byte[4]); // end pointer section, will be replaced later

            int startPointer = 12 + 8 * Sections.Count;
            int currentSectionPointer = startPointer;
            bytes.AddRange(BitConverter.GetBytes(currentSectionPointer).Reverse());

            foreach (MapDefinitionSection section in Sections)
            {
                bytes.AddRange(BitConverter.GetBytes(currentSectionPointer).Reverse());
                bytes.AddRange(BitConverter.GetBytes(section.MapDefinitions.Count).Reverse());
                sectionBytes.AddRange(section.GetBytes(currentSectionPointer, endPointers));
                currentSectionPointer = startPointer + sectionBytes.Count;
            }

            bytes.RemoveRange(4, 4);
            bytes.InsertRange(4, BitConverter.GetBytes(currentSectionPointer).Reverse());

            bytes.AddRange(sectionBytes);

            bytes.AddRange(BitConverter.GetBytes(endPointers.Count).Reverse());
            foreach (int endPointer in endPointers)
            {
                bytes.AddRange(BitConverter.GetBytes(endPointer).Reverse());
            }

            return [.. bytes];
        }

        /// <summary>
        /// Gets the map definitions file as an editable CSV
        /// </summary>
        /// <returns></returns>
        public string GetCsv()
        {
            string csv = $"Caller,{nameof(MapDefinition.TimeDay)},{nameof(MapDefinition.TimeHour)},{nameof(MapDefinition.TimeMinute)},{nameof(MapDefinition.PaddingByte)},{nameof(MapDefinition.Unknown04)},{nameof(MapDefinition.ZeroMapSgeIndex)}," +
                $"{nameof(MapDefinition.LocString)},{nameof(MapDefinition.ScriptDescription)},{nameof(MapDefinition.McbChildLoc)},{nameof(MapDefinition.Ffff56EntryIndex)},{nameof(MapDefinition.MapDataIndex)},{nameof(MapDefinition.BgDataIndex)}," +
                $"{nameof(MapDefinition.CameraDataEntryIndex)},{nameof(MapDefinition.Unknown1A)},{nameof(MapDefinition.CameraRotation)},{nameof(MapDefinition.DefaultBgmIndex)},{nameof(MapDefinition.Unknown22)},{nameof(MapDefinition.SoundGroupName)}," +
                $"{nameof(MapDefinition.ScriptName)},DispFlag1,DispFlag2,DispFlag3,DispFlag4,BgModel1-1,BgModel1-2,BgModel1-3,BgModel1-4,BgModel2-1,BgModel2-2,BgModel2-3,BgModel2-4,Evt1,Evt2,Evt3,Evt4,Evt5,Evt6,Evt7,Evt8,{nameof(MapDefinition.Unknown5C)}," +
                $"{nameof(MapDefinition.MapFlag)},{nameof(MapDefinition.Unknown64)}";
            return $"{csv}\n{string.Join('\n', Sections.Select(s => s.GetCsv()))}";
        }

        /// <inheritdoc />
        public List<DialogueLine> GetDialogueLines()
        {
            List<DialogueLine> lines = [];

            foreach (MapDefinitionSection section in Sections)
            {
                foreach (MapDefinition definition in section.MapDefinitions)
                {
                    DialogueLine line = new()
                    {
                        Line = definition.ScriptDescription,
                        Speaker = $"{definition.ScriptName} at {definition.LocString}"
                    };
                    line.Metadata.Add($"{definition.ParentIndex}");
                    line.Metadata.Add($"{definition.Index}");
                    lines.Add(line);
                }
            }

            return lines;
        }

        /// <inheritdoc />
        public void ReplaceDialogueLine(DialogueLine line)
        {
            int parentIndex = int.Parse(line.Metadata[0]);
            int index = int.Parse(line.Metadata[1]);
                
            for (int i = 0; i < Sections.Count; i++)
            {
                for (int j = 0; j < Sections[i].MapDefinitions.Count; j++)
                {
                    if (Sections[i].MapDefinitions[j].ParentIndex == parentIndex && Sections[i].MapDefinitions[j].Index == index)
                    {
                        Sections[i].MapDefinitions[j].ScriptDescription = line.Line;
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Section containing map definitions
    /// </summary>
    public class MapDefinitionSection
    {
        /// <summary>
        /// The section index
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// The set of map definitions in this section
        /// </summary>
        public List<MapDefinition> MapDefinitions { get; set; } = [];

        /// <summary>
        /// Constructs a map definition section given file data, an index, and an item count
        /// </summary>
        /// <param name="data">The map definition file data</param>
        /// <param name="index">The index of the current section being parsed</param>
        /// <param name="offset">The offset to the current section in the file data</param>
        /// <param name="itemCount">The number of map definitions in the section</param>
        public MapDefinitionSection(byte[] data, int index, int offset, int itemCount)
        {
            Index = index;
            for (int i = 0; i < itemCount; i++)
            {
                MapDefinitions.Add(new(data, Index, i, offset + i * 0x68));
            }
        }

        /// <summary>
        /// Constructs a map definition section from CSV lines
        /// </summary>
        /// <param name="csvLines">The CSV lines from the map definitions CSV file</param>
        /// <param name="index">The index of the section to construct</param>
        public MapDefinitionSection(string[] csvLines, int index)
        {
            Index = index;

            for (int i = 0; i < csvLines.Length; i++)
            {
                MapDefinitions.Add(new(csvLines[i], Index, i));
            }
        }

        /// <summary>
        /// Gets a binary representation of the map definition section and appends any end pointers to the provided
        /// end pointers list
        /// </summary>
        /// <param name="mapDefinitionOffset">The offset of the map definitions section</param>
        /// <param name="endPointers">The list of end pointers</param>
        /// <returns>A binary representation of this section</returns>
        public List<byte> GetBytes(int mapDefinitionOffset, List<int> endPointers)
        {
            List<byte> bytes = [];

            int stringsOffset = mapDefinitionOffset + MapDefinitions.Count * 0x68;
            List<byte> stringsSection = [];

            foreach (MapDefinition mapDefinition in MapDefinitions)
            {
                bytes.Add(mapDefinition.TimeDay);
                bytes.Add(mapDefinition.TimeHour);
                bytes.Add(mapDefinition.TimeMinute);
                bytes.Add(mapDefinition.PaddingByte);
                bytes.AddRange(BitConverter.GetBytes(mapDefinition.Unknown04).Reverse());
                bytes.AddRange(BitConverter.GetBytes(mapDefinition.ZeroMapSgeIndex).Reverse());
                if (mapDefinition.LocString.Length > 0)
                {
                    endPointers.Add(mapDefinitionOffset + bytes.Count);
                    bytes.AddRange(BitConverter.GetBytes(stringsOffset + stringsSection.Count).Reverse());
                    stringsSection.AddRange(Helpers.GetPaddedByteArrayFromString(mapDefinition.LocString));
                }
                else
                {
                    bytes.AddRange(new byte[4]);
                }
                if (mapDefinition.ScriptDescription.Length > 0)
                {
                    endPointers.Add(mapDefinitionOffset + bytes.Count);
                    bytes.AddRange(BitConverter.GetBytes(stringsOffset + stringsSection.Count).Reverse());
                    stringsSection.AddRange(Helpers.GetPaddedByteArrayFromString(mapDefinition.ScriptDescription));
                }
                else
                {
                    bytes.AddRange(new byte[4]);
                }
                bytes.AddRange(BitConverter.GetBytes(mapDefinition.McbChildLoc).Reverse());
                bytes.AddRange(BitConverter.GetBytes(mapDefinition.Ffff56EntryIndex).Reverse());
                bytes.AddRange(BitConverter.GetBytes(mapDefinition.MapDataIndex).Reverse());
                bytes.AddRange(BitConverter.GetBytes(mapDefinition.BgDataIndex).Reverse());
                bytes.AddRange(BitConverter.GetBytes(mapDefinition.CameraDataEntryIndex).Reverse());
                bytes.AddRange(BitConverter.GetBytes(mapDefinition.Unknown1A).Reverse());
                bytes.AddRange(BitConverter.GetBytes(mapDefinition.CameraRotation).Reverse());
                bytes.AddRange(BitConverter.GetBytes(mapDefinition.DefaultBgmIndex).Reverse());
                bytes.AddRange(BitConverter.GetBytes(mapDefinition.Unknown22).Reverse());
                if (mapDefinition.SoundGroupName.Length > 0)
                {
                    endPointers.Add(mapDefinitionOffset + bytes.Count);
                    bytes.AddRange(BitConverter.GetBytes(stringsOffset + stringsSection.Count).Reverse());
                    stringsSection.AddRange(Helpers.GetPaddedByteArrayFromString(mapDefinition.SoundGroupName));
                }
                else
                {
                    bytes.AddRange(new byte[4]);
                }
                if (mapDefinition.ScriptName.Length > 0)
                {
                    endPointers.Add(mapDefinitionOffset + bytes.Count);
                    bytes.AddRange(BitConverter.GetBytes(stringsOffset + stringsSection.Count).Reverse());
                    stringsSection.AddRange(Helpers.GetPaddedByteArrayFromString(mapDefinition.ScriptName));
                }
                else
                {
                    bytes.AddRange(new byte[4]);
                }
                for (int i = 0; i < mapDefinition.DispFlags.Length; i++)
                {
                    if (mapDefinition.DispFlags[i].Length > 0)
                    {
                        endPointers.Add(mapDefinitionOffset + bytes.Count);
                        bytes.AddRange(BitConverter.GetBytes(stringsOffset + stringsSection.Count).Reverse());
                        stringsSection.AddRange(Helpers.GetPaddedByteArrayFromString(mapDefinition.DispFlags[i]));
                    }
                    else
                    {
                        bytes.AddRange(new byte[4]);
                    }
                }
                for (int i = 0; i < mapDefinition.BgModels1.Length; i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(mapDefinition.BgModels1[i]).Reverse());
                }
                for (int i = 0; i < mapDefinition.BgModels2.Length; i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(mapDefinition.BgModels2[i]).Reverse());
                }
                for (int i = 0; i < mapDefinition.Evts.Length; i++)
                {
                    short evt;
                    if (mapDefinition.Evts[i] < 0)
                    {
                        evt = mapDefinition.Evts[i];
                    }
                    else
                    {
                        evt = (short)(mapDefinition.Evts[i] + MapDefinition.EVT_SHIFT);
                    }
                    bytes.AddRange(BitConverter.GetBytes(evt).Reverse());
                }
                bytes.AddRange(BitConverter.GetBytes(mapDefinition.Unknown5C).Reverse());
                if (mapDefinition.MapFlag.Length > 0)
                {
                    endPointers.Add(mapDefinitionOffset + bytes.Count);
                    bytes.AddRange(BitConverter.GetBytes(stringsOffset + stringsSection.Count).Reverse());
                    stringsSection.AddRange(Helpers.GetPaddedByteArrayFromString(mapDefinition.MapFlag));
                }
                else
                {
                    bytes.AddRange(new byte[4]);
                }
                bytes.AddRange(BitConverter.GetBytes(mapDefinition.Unknown64).Reverse());
            }
            bytes.AddRange(stringsSection);

            return bytes;
        }

        /// <summary>
        /// Gets CSV lines representing this section
        /// </summary>
        /// <returns>CSV lines representing this section</returns>
        public string GetCsv()
        {
            return string.Join('\n', MapDefinitions.Select(m => m.GetCsvLine()));
        }
    }

    /// <summary>
    /// A map definition
    /// </summary>
    public class MapDefinition
    {
        /// <summary>
        /// A constant shift value that shifts evt indices in some cases
        /// </summary>
        public const short EVT_SHIFT = 0x2710;

        /// <summary>
        /// The index of the map definition section
        /// </summary>
        public int ParentIndex { get; set; }
        /// <summary>
        /// The index of this definition
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// The day component of the time in which the map should be loaded
        /// </summary>
        public byte TimeDay { get; set; }
        /// <summary>
        /// The hour component of the time in which the map should be loaded
        /// </summary>
        public byte TimeHour { get; set; }
        /// <summary>
        /// The minute component of the time in which the map should be loaded
        /// </summary>
        public byte TimeMinute { get; set; }
        /// <summary>
        /// Padding
        /// </summary>
        public byte PaddingByte { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public short Unknown04 { get; set; }
        /// <summary>
        /// The SGE index of the zero map
        /// </summary>
        public short ZeroMapSgeIndex { get; set; }
        /// <summary>
        /// A string describing the location to load
        /// </summary>
        public string LocString { get; set; }
        /// <summary>
        /// The description of the script as seen in-game
        /// </summary>
        public string ScriptDescription { get; set; }
        /// <summary>
        /// The MCB child location
        /// </summary>
        public short McbChildLoc { get; set; }
        /// <summary>
        /// The index into MCB(FFFF, 56) that's referenced here
        /// </summary>
        public short Ffff56EntryIndex { get; set; }
        /// <summary>
        /// Index of map data
        /// </summary>
        public short MapDataIndex { get; set; }
        /// <summary>
        /// Index of background data
        /// </summary>
        public short BgDataIndex { get; set; }
        /// <summary>
        /// Index of camera data
        /// </summary>
        public short CameraDataEntryIndex { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public short Unknown1A { get; set; }
        /// <summary>
        /// Value describing the camera's rotation
        /// </summary>
        public float CameraRotation { get; set; }
        /// <summary>
        /// The default starting BGM to play when loading the map
        /// </summary>
        public short DefaultBgmIndex { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public short Unknown22 { get; set; }
        /// <summary>
        /// The sound group to load
        /// </summary>
        public string SoundGroupName { get; set; }
        /// <summary>
        /// Name of the script
        /// </summary>
        public string ScriptName { get; set; }
        /// <summary>
        /// Display flags
        /// </summary>
        public string[] DispFlags { get; set; }
        /// <summary>
        /// The list of BG model indices
        /// </summary>
        public short[] BgModels1 { get; set; }
        /// <summary>
        /// A second list of BG model indices
        /// </summary>
        public short[] BgModels2 { get; set; }
        /// <summary>
        /// Event file cutscenes to load
        /// </summary>
        public short[] Evts { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown5C { get; set; }
        /// <summary>
        /// Pointer to map flag
        /// </summary>
        public int MapFlagPointer { get; set; }
        /// <summary>
        /// Map flag
        /// </summary>
        public string MapFlag { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown64 { get; set; }

        /// <summary>
        /// Constructs map definition from data
        /// </summary>
        /// <param name="data">The binary data of the map file</param>
        /// <param name="parentIndex">Parent index</param>
        /// <param name="index">Index</param>
        /// <param name="definitionOffset">The offset of the current definition in the file</param>
        public MapDefinition(byte[] data, int parentIndex, int index, int definitionOffset)
        {
            ParentIndex = parentIndex;
            Index = index;

            TimeDay = data[definitionOffset];
            TimeHour = data[definitionOffset + 0x01];
            TimeMinute = data[definitionOffset + 0x02];
            PaddingByte = data[definitionOffset + 0x03];
            Unknown04 = IO.ReadShort(data, definitionOffset + 0x04);
            ZeroMapSgeIndex = IO.ReadShort(data, definitionOffset + 0x06);
            int locStringPointer = IO.ReadInt(data, definitionOffset + 0x08);
            LocString = IO.ReadShiftJisString(data, locStringPointer);
            int scriptDescriptionPointer = IO.ReadInt(data, definitionOffset + 0x0C);
            ScriptDescription = IO.ReadShiftJisString(data, scriptDescriptionPointer);
            McbChildLoc = IO.ReadShort(data, definitionOffset + 0x10);
            Ffff56EntryIndex = IO.ReadShort(data, definitionOffset + 0x12);
            MapDataIndex = IO.ReadShort(data, definitionOffset + 0x14);
            BgDataIndex = IO.ReadShort(data, definitionOffset + 0x16);
            CameraDataEntryIndex = IO.ReadShort(data, definitionOffset + 0x18);
            Unknown1A = IO.ReadShort(data, definitionOffset + 0x1A);
            CameraRotation = IO.ReadFloat(data, definitionOffset + 0x1C);
            DefaultBgmIndex = IO.ReadShort(data, definitionOffset + 0x20);
            Unknown22 = IO.ReadShort(data, definitionOffset + 0x22);
            int soundGroupNamePointer = IO.ReadInt(data, definitionOffset + 0x24);
            SoundGroupName = IO.ReadAsciiString(data, soundGroupNamePointer);
            int scriptNamePointer = IO.ReadInt(data, definitionOffset + 0x28);
            ScriptName = IO.ReadAsciiString(data, scriptNamePointer);
            DispFlags = new string[4];
            for (int i = 0; i < DispFlags.Length; i++)
            {
                int dispFlagPointer = IO.ReadInt(data, definitionOffset + 0x2C + i * 4);
                DispFlags[i] = IO.ReadAsciiString(data, dispFlagPointer);
            }
            BgModels1 = new short[4];
            for (int i = 0; i < BgModels1.Length; i++)
            {
                BgModels1[i] = IO.ReadShort(data, definitionOffset + 0x3C + i * 2);
            }
            BgModels2 = new short[4];
            for (int i = 0; i < BgModels2.Length; i++)
            {
                BgModels2[i] = IO.ReadShort(data, definitionOffset + 0x44 + i * 2);
            }
            Evts = new short[8];
            for (int i = 0; i < Evts.Length; i++)
            {
                short evtId = IO.ReadShort(data, definitionOffset + 0x4C + i * 2);
                if (evtId < 0)
                {
                    Evts[i] = evtId;
                }
                else
                {
                    Evts[i] = (short)(evtId - EVT_SHIFT);
                }
            }
            Unknown5C = IO.ReadInt(data, definitionOffset + 0x5C);
            MapFlagPointer = IO.ReadInt(data, definitionOffset + 0x60);
            MapFlag = IO.ReadAsciiString(data, MapFlagPointer);
            Unknown64 = IO.ReadInt(data, definitionOffset + 0x64);
        }

        /// <summary>
        /// Constructs map data from a CSV line
        /// </summary>
        /// <param name="csvLine">The CSV line</param>
        /// <param name="parentIndex">The parent index</param>
        /// <param name="index">The index</param>
        public MapDefinition(string csvLine, int parentIndex, int index)
        {
            ParentIndex = parentIndex;
            Index = index;

            string[] components = csvLine.Split(',');
            TimeDay = byte.Parse(components[1]);
            TimeHour = byte.Parse(components[2]);
            TimeMinute = byte.Parse(components[3]);
            PaddingByte = byte.Parse(components[4]);
            Unknown04 = short.Parse(components[5]);
            ZeroMapSgeIndex = short.Parse(components[6]);
            LocString = components[7];
            ScriptDescription = components[8];
            McbChildLoc = short.Parse(components[9]);
            Ffff56EntryIndex = short.Parse(components[10]);
            MapDataIndex = short.Parse(components[11]);
            BgDataIndex = short.Parse(components[12]);
            CameraDataEntryIndex = short.Parse(components[13]);
            Unknown1A = short.Parse(components[14]);
            CameraRotation = float.Parse(components[15]);
            DefaultBgmIndex = short.Parse(components[16]);
            Unknown22 = short.Parse(components[17]);
            SoundGroupName = components[18];
            ScriptName = components[19];
            DispFlags = new string[4];
            for (int i = 0; i < DispFlags.Length; i++)
            {
                DispFlags[i] = components[20 + i];
            }
            BgModels1 = new short[4];
            for (int i = 0; i < BgModels1.Length; i++)
            {
                BgModels1[i] = short.Parse(components[24 + i]);
            }
            BgModels2 = new short[4];
            for (int i = 0; i < BgModels2.Length; i++)
            {
                BgModels2[i] = short.Parse(components[28 + i]);
            }
            Evts = new short[8];
            for (int i = 0; i < Evts.Length; i++)
            {
                Evts[i] = short.Parse(components[32 + i]);
            }
            Unknown5C = int.Parse(components[40]);
            MapFlag = components[41];
            Unknown64 = int.Parse(components[42]);
        }

        /// <summary>
        /// Gets the map data as a CSV line
        /// </summary>
        /// <returns>This map definition as a CSV line</returns>
        public string GetCsvLine()
        {
            return $"{ParentIndex}-{Index},{TimeDay},{TimeHour},{TimeMinute},{PaddingByte},{Unknown04},{ZeroMapSgeIndex},{LocString},{ScriptDescription},{McbChildLoc},{Ffff56EntryIndex},{MapDataIndex},{BgDataIndex},{CameraDataEntryIndex}," +
                $"{Unknown1A},{CameraRotation},{DefaultBgmIndex},{Unknown22},{SoundGroupName},{ScriptName},{string.Join(',', DispFlags)},{string.Join(',', BgModels1)},{string.Join(',', BgModels2)}," +
                $"{string.Join(',', Evts)},{Unknown5C},{MapFlag},{Unknown64}";

        }
    }
}
