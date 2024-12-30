using System;
using System.Collections.Generic;
using System.Linq;

namespace HaruhiHeiretsuLib.Data
{
    /// <summary>
    /// A file representing the camera data file (dat.bin 36)
    /// </summary>
    public class CameraDataFile : DataFile
    {
        /// <summary>
        /// Unknown
        /// </summary>
        public List<float> Section1 { get; set; } = [];
        /// <summary>
        /// List of cameras
        /// </summary>
        public List<CameraDataEntry> CameraDataEntries { get; set; } = [];
        /// <summary>
        /// Unknown
        /// </summary>
        public short StaticCameraIndex { get; set; }

        /// <summary>
        /// Simple constructor
        /// </summary>
        public CameraDataFile()
        {
            Name = "Camera Data";
        }

        /// <inheritdoc/>
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

        /// <summary>
        /// Creates a camera data file from a CSV file
        /// </summary>
        /// <param name="csvLines">A list of CSV lines (as with File.ReadAllLines)</param>
        public CameraDataFile(string[] csvLines)
        {
            Section1 = csvLines[1].Split(',').Where(f => f.Length > 0).Select(f => float.Parse(f)).ToList();
            IEnumerable<string> cameraDefinitionEntries = csvLines.Skip(3).TakeWhile(l => !l.StartsWith("Section3Ints"));
            CameraDataEntries = cameraDefinitionEntries.Select(e => new CameraDataEntry(e)).ToList();
            StaticCameraIndex = csvLines.Skip(3 + CameraDataEntries.Count).ElementAt(1).Split(',').Where(f => f.Length > 0).Select(f => short.Parse(f)).First();
        }

        /// <summary>
        /// Gets a CSV representation of the camera data file
        /// </summary>
        /// <returns>A string CSV representation of the camera data file</returns>
        public string GetCsv()
        {
            List<string> csvLines =
            [
                "Section1Floats",
                string.Join(',', Section1),
                $"{nameof(CameraDataEntry.XPosition)},{nameof(CameraDataEntry.YPosition)},{nameof(CameraDataEntry.ZPosition)},{nameof(CameraDataEntry.XLook)},{nameof(CameraDataEntry.YLook)},{nameof(CameraDataEntry.ZLook)}," +
                    $"{nameof(CameraDataEntry.Unknown18)},{nameof(CameraDataEntry.MinYaw)},{nameof(CameraDataEntry.MaxYaw)},{nameof(CameraDataEntry.MinPitch)},{nameof(CameraDataEntry.MaxPitch)},{nameof(CameraDataEntry.Zoom)}," +
                    $"{nameof(CameraDataEntry.Unknown30)}",
                .. CameraDataEntries.Select(e => e.GetCsvLine()),
                "Section3Ints",
                string.Join(',', StaticCameraIndex),
            ];

            return string.Join('\n', csvLines);
        }

        /// <inheritdoc/>
        public override byte[] GetBytes()
        {
            List<byte> bytes = [];
            List<byte> sectionBytes = [];
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

            return [.. bytes];
        }
    }

    /// <summary>
    /// An entry in the CameraDataFile
    /// </summary>
    public class CameraDataEntry
    {
        /// <summary>
        /// The camera's position along the X axis of the scene
        /// </summary>
        public float XPosition { get; set; }
        /// <summary>
        /// The camera's position along the Y axis of the scene
        /// </summary>
        public float YPosition { get; set; }
        /// <summary>
        /// The camera's position along the Z axis of the scene
        /// </summary>
        public float ZPosition { get; set; }
        /// <summary>
        /// The X component of the point the camera is looking at
        /// </summary>
        public float XLook { get; set; }
        /// <summary>
        /// The Y component of the point the camera is looking at
        /// </summary>
        public float YLook { get; set; }
        /// <summary>
        /// The Z component of the point the camera is looking at
        /// </summary>
        public float ZLook { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public float Unknown18 { get; set; }
        /// <summary>
        /// The minimum yaw for the camera if it can be moved
        /// </summary>
        public float MinYaw { get; set; }
        /// <summary>
        /// The maximum yaw for the camera if it can be moved
        /// </summary>
        public float MaxYaw { get; set; }
        /// <summary>
        /// The minimum pitch for the camera if it can be moved
        /// </summary>
        public float MinPitch { get; set; }
        /// <summary>
        /// The maximum pitch for the camera if it can be moved
        /// </summary>
        public float MaxPitch { get; set; }
        /// <summary>
        /// The amount the camera is zoomed in (1.0 default; higher values = greater zoom)
        /// </summary>
        public float Zoom { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public float Unknown30 { get; set; }

        /// <summary>
        /// Creates a camera entry from binary data
        /// </summary>
        /// <param name="bytes">The binary data representing the camera data (0x34 bytes)</param>
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
        
        /// <summary>
        /// Creates camera data entry from a line of comma-separated values
        /// </summary>
        /// <param name="csvLine">The CSV line string</param>
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

        /// <summary>
        /// Gets the camera's binary data
        /// </summary>
        /// <returns>List of bytes representing the camera's binary data</returns>
        public List<byte> GetBytes()
        {
            List<byte> bytes =
            [
                .. BitConverter.GetBytes(XPosition).Reverse(),
                .. BitConverter.GetBytes(YPosition).Reverse(),
                .. BitConverter.GetBytes(ZPosition).Reverse(),
                .. BitConverter.GetBytes(XLook).Reverse(),
                .. BitConverter.GetBytes(YLook).Reverse(),
                .. BitConverter.GetBytes(ZLook).Reverse(),
                .. BitConverter.GetBytes(Unknown18).Reverse(),
                .. BitConverter.GetBytes(MinYaw).Reverse(),
                .. BitConverter.GetBytes(MaxYaw).Reverse(),
                .. BitConverter.GetBytes(MinPitch).Reverse(),
                .. BitConverter.GetBytes(MaxPitch).Reverse(),
                .. BitConverter.GetBytes(Zoom).Reverse(),
                .. BitConverter.GetBytes(Unknown30).Reverse(),
            ];

            return bytes;
        }

        /// <summary>
        /// Gets a CSV line representing the camera's properties
        /// </summary>
        /// <returns>A string CSV line representing the camera's properties</returns>
        public string GetCsvLine()
        {
            return $"{XPosition},{YPosition},{ZPosition},{XLook},{YLook},{ZLook},{Unknown18},{MinYaw},{MaxYaw},{MinPitch},{MaxPitch},{Zoom},{Unknown30}";
        }
    }
}
