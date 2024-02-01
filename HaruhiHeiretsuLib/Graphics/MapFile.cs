using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaruhiHeiretsuLib.Graphics
{
    public partial class GraphicsFile
    {
        public byte[] MapHeader { get; set; }
        public string MapModel { get; set; }
        public string MapBackgroundModel { get; set; }
        public List<string> MapModelNames { get; set; } = [];
        public List<MapEntry> MapEntries { get; set; } = [];
        public List<byte[]> MapFooterEntries { get; set; } = [];

        public void SetMapData(List<MapEntry> newMapEntries)
        {
            Edited = true;

            List<string> names = newMapEntries.Select(l => l.Name).Distinct().ToList();
            MapModelNames = [];
            MapEntries = [];

            List<byte> bytes = [.. MapHeader, .. Encoding.ASCII.GetBytes(MapModel)];
            bytes.AddRange(new byte[bytes.Count % 0x10 == 0 ? 0 : 0x10 - bytes.Count % 0x10]);
            bytes.AddRange(Encoding.ASCII.GetBytes(MapBackgroundModel));
            bytes.AddRange(new byte[bytes.Count % 0x10 == 0 ? 0 : 0x10 - bytes.Count % 0x10]);

            bytes.AddRange(new byte[0x100 - bytes.Count]);

            for (int i = 0; i < 256; i++)
            {
                if (i < names.Count && !string.IsNullOrEmpty(names[i]))
                {
                    MapModelNames.Add(names[i]);
                    bytes.AddRange(Encoding.ASCII.GetBytes(names[i]));
                    bytes.AddRange(new byte[bytes.Count % 0x10 == 0 ? 0 : 0x10 - bytes.Count % 0x10]);
                }
                else
                {
                    MapModelNames.Add(string.Empty);
                    bytes.AddRange(new byte[0x10]);
                }
            }

            for (int i = 0; i < 512; i++)
            {
                if (i < newMapEntries.Count)
                {
                    bytes.AddRange(newMapEntries[i].GetBytes(MapModelNames));
                }
                else
                {
                    bytes.AddRange(new byte[0x2C]);
                }
            }

            bytes.AddRange(MapFooterEntries.SelectMany(b => b));

            Data = bytes;
        }
    }

    public class MapEntry
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public short ShouldProcess { get; set; } // values of zero or one indicate this entry should be processed
        public short NameIndex { get; set; }
        public string Name { get; set; }
        public short Rotation { get; set; }
        public short Unknown12 { get; set; }
        public short Unknown14 { get; set; }
        public short Unknown16 { get; set; }
        public short Unknown18 { get; set; }
        public short Unknown1A { get; set; }
        public short Unknown1C { get; set; }
        public short Unknown1E { get; set; }
        public short Unknown20 { get; set; }
        public short Unknown22 { get; set; }
        public short Unknown24 { get; set; }
        public short Unknown26 { get; set; }
        public short Unknown28 { get; set; }
        public short Unknown2A { get; set; }

        public MapEntry(IEnumerable<byte> data, string name)
        {
            Name = name;
            Initialize(data);
        }

        public MapEntry(IEnumerable<byte> data)
        {
            Initialize(data);
        }

        private void Initialize(IEnumerable<byte> data)
        {
            X = BitConverter.ToSingle(data.Skip(0x00).Take(4).ToArray());
            Y = BitConverter.ToSingle(data.Skip(0x04).Take(4).ToArray());
            Z = BitConverter.ToSingle(data.Skip(0x08).Take(4).ToArray());
            ShouldProcess = BitConverter.ToInt16(data.Skip(0x0C).Take(2).ToArray());
            NameIndex = BitConverter.ToInt16(data.Skip(0x0C).Take(2).ToArray());
            Rotation = BitConverter.ToInt16(data.Skip(0x10).Take(2).ToArray());
            Unknown12 = BitConverter.ToInt16(data.Skip(0x12).Take(2).ToArray());
            Unknown14 = BitConverter.ToInt16(data.Skip(0x14).Take(2).ToArray());
            Unknown16 = BitConverter.ToInt16(data.Skip(0x16).Take(2).ToArray());
            Unknown18 = BitConverter.ToInt16(data.Skip(0x18).Take(2).ToArray());
            Unknown1A = BitConverter.ToInt16(data.Skip(0x1A).Take(2).ToArray());
            Unknown1C = BitConverter.ToInt16(data.Skip(0x1C).Take(2).ToArray());
            Unknown1E = BitConverter.ToInt16(data.Skip(0x1E).Take(2).ToArray());
            Unknown20 = BitConverter.ToInt16(data.Skip(0x20).Take(2).ToArray());
            Unknown22 = BitConverter.ToInt16(data.Skip(0x22).Take(2).ToArray());
            Unknown24 = BitConverter.ToInt16(data.Skip(0x24).Take(2).ToArray());
            Unknown26 = BitConverter.ToInt16(data.Skip(0x26).Take(2).ToArray());
            Unknown28 = BitConverter.ToInt16(data.Skip(0x28).Take(2).ToArray());
            Unknown2A = BitConverter.ToInt16(data.Skip(0x2A).Take(2).ToArray());
        }

        public MapEntry(string csvLine)
        {
            string[] components = csvLine.Split(',');

            Name = components[0];
            X = float.Parse(components[1]);
            Y = float.Parse(components[2]);
            Z = float.Parse(components[3]);
            ShouldProcess = short.Parse(components[4]);
            Rotation = short.Parse(components[5]);
            Unknown12 = short.Parse(components[6]);
            Unknown14 = short.Parse(components[7]);
            Unknown16 = short.Parse(components[8]);
            Unknown18 = short.Parse(components[9]);
            Unknown1A = short.Parse(components[10]);
            Unknown1C = short.Parse(components[11]);
            Unknown1E = short.Parse(components[12]);
            Unknown20 = short.Parse(components[13]);
            Unknown22 = short.Parse(components[14]);
            Unknown24 = short.Parse(components[15]);
            Unknown26 = short.Parse(components[16]);
            Unknown28 = short.Parse(components[17]);
            Unknown2A = short.Parse(components[18]);
        }

        public byte[] GetBytes(List<string> modelNames)
        {
            List<byte> bytes = [];

            short nameIndex;
            if (string.IsNullOrEmpty(Name))
            {
                if (NameIndex < 0)
                {
                    nameIndex = NameIndex;
                }
                else
                {
                    nameIndex = (short)modelNames.Count;
                }
            }
            else
            {
                nameIndex = (short)(modelNames.IndexOf(Name) + 1);
            }

            bytes.AddRange(BitConverter.GetBytes(X));
            bytes.AddRange(BitConverter.GetBytes(Y));
            bytes.AddRange(BitConverter.GetBytes(Z));
            bytes.AddRange(BitConverter.GetBytes(ShouldProcess));
            bytes.AddRange(BitConverter.GetBytes(nameIndex));
            bytes.AddRange(BitConverter.GetBytes(Rotation));
            bytes.AddRange(BitConverter.GetBytes(Unknown12));
            bytes.AddRange(BitConverter.GetBytes(Unknown14));
            bytes.AddRange(BitConverter.GetBytes(Unknown16));
            bytes.AddRange(BitConverter.GetBytes(Unknown18));
            bytes.AddRange(BitConverter.GetBytes(Unknown1A));
            bytes.AddRange(BitConverter.GetBytes(Unknown1C));
            bytes.AddRange(BitConverter.GetBytes(Unknown1E));
            bytes.AddRange(BitConverter.GetBytes(Unknown20));
            bytes.AddRange(BitConverter.GetBytes(Unknown22));
            bytes.AddRange(BitConverter.GetBytes(Unknown24));
            bytes.AddRange(BitConverter.GetBytes(Unknown26));
            bytes.AddRange(BitConverter.GetBytes(Unknown28));
            bytes.AddRange(BitConverter.GetBytes(Unknown2A));

            return [.. bytes];
        }

        public string GetCsvLine()
        {
            return $"{Name},{X},{Y},{Z},{ShouldProcess},{Rotation},{Unknown12},{Unknown14},{Unknown16},{Unknown18},{Unknown1A},{Unknown1C},{Unknown1E}," +
                $"{Unknown20},{Unknown22},{Unknown24},{Unknown26},{Unknown28},{Unknown2A}";
        }

        public static string GetCsvHeader()
        {
            return $"{nameof(Name)},{nameof(X)},{nameof(Y)},{nameof(Z)},{nameof(ShouldProcess)},{nameof(Rotation)},{nameof(Unknown12)},{nameof(Unknown14)},{nameof(Unknown16)},{nameof(Unknown18)},{nameof(Unknown1A)},{nameof(Unknown1C)}," +
                $"{nameof(Unknown1E)},{nameof(Unknown20)},{nameof(Unknown22)},{nameof(Unknown24)},{nameof(Unknown26)},{nameof(Unknown28)},{nameof(Unknown2A)}\n";
        }
    }
}
