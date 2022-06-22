using System;
using System.Collections.Generic;
using System.Linq;

namespace HaruhiHeiretsuLib.Data
{
    public class CameraDataFile : DataFile
    {
        public List<float> Section1 { get; set; } = new();
        public List<CameraDataEntry> CameraDataEntries { get; set; } = new();
        public short StaticCameraIndex { get; set; } = new();

        public CameraDataFile()
        {
            Name = "Camera Data";
        }

        public override void Initialize(byte[] decompressedData, int offset)
        {
            base.Initialize(decompressedData, offset);

            int section1Offset = BitConverter.ToInt32(Data.Skip(0x0C).Take(4).Reverse().ToArray());
            int numSection1Floats = BitConverter.ToInt32(Data.Skip(0x10).Take(4).Reverse().ToArray());
            for (int i = 0; i < numSection1Floats; i++)
            {
                Section1.Add(BitConverter.ToSingle(Data.Skip(section1Offset + 0x04 * i).Take(4).Reverse().ToArray()));
            }

            int cameraDataSectionOffset = BitConverter.ToInt32(Data.Skip(0x14).Take(4).Reverse().ToArray());
            int numCameraDataEntries = BitConverter.ToInt32(Data.Skip(0x18).Take(4).Reverse().ToArray());
            for (int i = 0; i < numCameraDataEntries; i++)
            {
                CameraDataEntries.Add(new CameraDataEntry(Data.Skip(cameraDataSectionOffset + 0x34 * i).Take(0x34)));
            }

            int section3Offset = BitConverter.ToInt32(Data.Skip(0x1C).Take(4).Reverse().ToArray());
            StaticCameraIndex = BitConverter.ToInt16(Data.Skip(section3Offset).Take(2).Reverse().ToArray());
        }

        public CameraDataFile(string[] csvLines)
        {
            Section1 = csvLines[1].Split(',').Where(f => f.Length > 0).Select(f => float.Parse(f)).ToList();
            IEnumerable<string> cameraDefinitionEntries = csvLines.Skip(3).TakeWhile(l => !l.StartsWith("Section3Ints"));
            CameraDataEntries = cameraDefinitionEntries.Select(e => new CameraDataEntry(e)).ToList();
            StaticCameraIndex = csvLines.Skip(3 + CameraDataEntries.Count).ElementAt(1).Split(',').Where(f => f.Length > 0).Select(f => short.Parse(f)).First();
        }

        public string GetCsv()
        {
            List<string> csvLines = new();
            csvLines.Add("Section1Floats");
            csvLines.Add(string.Join(',', Section1));
            csvLines.Add($"{nameof(CameraDataEntry.XPosition)},{nameof(CameraDataEntry.YPosition)},{nameof(CameraDataEntry.ZPosition)},{nameof(CameraDataEntry.XLook)},{nameof(CameraDataEntry.YLook)},{nameof(CameraDataEntry.ZLook)}," +
                $"{nameof(CameraDataEntry.Unknown18)},{nameof(CameraDataEntry.MinYaw)},{nameof(CameraDataEntry.MaxYaw)},{nameof(CameraDataEntry.MinPitch)},{nameof(CameraDataEntry.MaxPitch)},{nameof(CameraDataEntry.Zoom)}," +
                $"{nameof(CameraDataEntry.Unknown30)}");
            csvLines.AddRange(CameraDataEntries.Select(e => e.GetCsvLine()));
            csvLines.Add("Section3Ints");
            csvLines.Add(string.Join(',', StaticCameraIndex));

            return string.Join('\n', csvLines);
        }

        public override byte[] GetBytes()
        {
            List<byte> bytes = new();
            List<byte> sectionBytes = new();
            int startingOffset = 0x24;

            bytes.AddRange(BitConverter.GetBytes(3).Reverse());
            bytes.AddRange(new byte[4]); // will be replaced later
            bytes.AddRange(BitConverter.GetBytes(startingOffset).Reverse());

            bytes.AddRange(BitConverter.GetBytes(startingOffset).Reverse());
            bytes.AddRange(BitConverter.GetBytes(Section1.Count).Reverse());
            sectionBytes.AddRange(Section1.SelectMany(f => BitConverter.GetBytes(f).Reverse()));

            bytes.AddRange(BitConverter.GetBytes(startingOffset + sectionBytes.Count).Reverse());
            bytes.AddRange(BitConverter.GetBytes(CameraDataEntries.Count).Reverse());
            sectionBytes.AddRange(CameraDataEntries.SelectMany(e => e.GetBytes()));

            bytes.AddRange(BitConverter.GetBytes(startingOffset + sectionBytes.Count).Reverse());
            bytes.AddRange(BitConverter.GetBytes(1).Reverse());
            sectionBytes.AddRange(BitConverter.GetBytes(StaticCameraIndex).Reverse());
            sectionBytes.AddRange(new byte[2]);

            bytes.AddRange(sectionBytes);
            bytes.RemoveRange(4, 4);
            bytes.InsertRange(4, BitConverter.GetBytes(bytes.Count + 4).Reverse());

            bytes.AddRange(new byte[4]);

            return bytes.ToArray();
        }
    }

    public class CameraDataEntry
    {
        public float XPosition { get; set; }
        public float YPosition { get; set; }
        public float ZPosition { get; set; }
        public float XLook { get; set; }
        public float YLook { get; set; }
        public float ZLook { get; set; }
        public float Unknown18 { get; set; }
        public float MinYaw { get; set; }
        public float MaxYaw { get; set; }
        public float MinPitch { get; set; }
        public float MaxPitch { get; set; }
        public float Zoom { get; set; }
        public float Unknown30 { get; set; }

        public CameraDataEntry(IEnumerable<byte> bytes)
        {
            XPosition = BitConverter.ToSingle(bytes.Take(4).Reverse().ToArray());
            YPosition = BitConverter.ToSingle(bytes.Skip(0x04).Take(4).Reverse().ToArray());
            ZPosition = BitConverter.ToSingle(bytes.Skip(0x08).Take(4).Reverse().ToArray());
            XLook = BitConverter.ToSingle(bytes.Skip(0x0C).Take(4).Reverse().ToArray());
            YLook = BitConverter.ToSingle(bytes.Skip(0x10).Take(4).Reverse().ToArray());
            ZLook = BitConverter.ToSingle(bytes.Skip(0x14).Take(4).Reverse().ToArray());
            Unknown18 = BitConverter.ToSingle(bytes.Skip(0x18).Take(4).Reverse().ToArray());
            MinYaw = BitConverter.ToSingle(bytes.Skip(0x1C).Take(4).Reverse().ToArray());
            MaxYaw = BitConverter.ToSingle(bytes.Skip(0x20).Take(4).Reverse().ToArray());
            MinPitch = BitConverter.ToSingle(bytes.Skip(0x24).Take(4).Reverse().ToArray());
            MaxPitch = BitConverter.ToSingle(bytes.Skip(0x28).Take(4).Reverse().ToArray());
            Zoom = BitConverter.ToSingle(bytes.Skip(0x2C).Take(4).Reverse().ToArray());
            Unknown30 = BitConverter.ToSingle(bytes.Skip(0x30).Take(4).Reverse().ToArray());
        }

        public CameraDataEntry(string csvLine)
        {
            string[] components = csvLine.Split(',');
            XPosition = float.Parse(components[0]);
            YPosition = float.Parse(components[1]);
            ZPosition = float.Parse(components[2]);
            XLook = float.Parse(components[3]);
            YLook = float.Parse(components[4]);
            ZLook = float.Parse(components[5]);
            Unknown18 = float.Parse(components[6]);
            MinYaw = float.Parse(components[7]);
            MaxYaw = float.Parse(components[8]);
            MinPitch = float.Parse(components[9]);
            MaxPitch = float.Parse(components[10]);
            Zoom = float.Parse(components[11]);
            Unknown30 = float.Parse(components[12]);
        }

        public List<byte> GetBytes()
        {
            List<byte> bytes = new();

            bytes.AddRange(BitConverter.GetBytes(XPosition).Reverse());
            bytes.AddRange(BitConverter.GetBytes(YPosition).Reverse());
            bytes.AddRange(BitConverter.GetBytes(ZPosition).Reverse());
            bytes.AddRange(BitConverter.GetBytes(XLook).Reverse());
            bytes.AddRange(BitConverter.GetBytes(YLook).Reverse());
            bytes.AddRange(BitConverter.GetBytes(ZLook).Reverse());
            bytes.AddRange(BitConverter.GetBytes(Unknown18).Reverse());
            bytes.AddRange(BitConverter.GetBytes(MinYaw).Reverse());
            bytes.AddRange(BitConverter.GetBytes(MaxYaw).Reverse());
            bytes.AddRange(BitConverter.GetBytes(MinPitch).Reverse());
            bytes.AddRange(BitConverter.GetBytes(MaxPitch).Reverse());
            bytes.AddRange(BitConverter.GetBytes(Zoom).Reverse());
            bytes.AddRange(BitConverter.GetBytes(Unknown30).Reverse());

            return bytes;
        }

        public string GetCsvLine()
        {
            return $"{XPosition},{YPosition},{ZPosition},{XLook},{YLook},{ZLook},{Unknown18},{MinYaw},{MaxYaw},{MinPitch},{MaxPitch},{Zoom},{Unknown30}";
        }
    }
}
