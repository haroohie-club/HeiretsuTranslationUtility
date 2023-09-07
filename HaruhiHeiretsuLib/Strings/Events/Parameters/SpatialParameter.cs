using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace HaruhiHeiretsuLib.Strings.Events.Parameters
{
    public class SpatialParameter : ActionParameter
    {
        public int Unknown0C { get; set; }
        public int SplinePointer { get; set; } // dynamically allocated
        public short Unknown14 { get; set; }
        public short Unknown16 { get; set; }
        public int Unknown18 { get; set; }
        public byte NumSplineVerts { get; set; }
        public byte SplineMode { get; set; }
        public byte Unknown1E { get; set; }
        public byte Unknown1F { get; set; }
        public Vector3[] SplineVertices1 { get; set; }
        public Vector3[] SplineVertices2 { get; set; }
        public Vector3[] SplineVertices3 { get; set; }
        public Vector2[] Coords4 { get; set; }
        public Vector2[] Coords5 { get; set; }
        public byte NumSpline2DVerts { get; set; }
        public byte Unknown35 { get; set; }
        public byte Unknown36 { get; set; }
        public byte Unknown37 { get; set; }
        public Vector2[] Spline2DVertices { get; set; }
        public int Unknown3C { get; set; }

        public SpatialParameter(IEnumerable<byte> data, int offset, ushort opCode) : base(data, offset, opCode)
        {
            Unknown0C = BitConverter.ToInt32(data.Skip(offset + 0x0C).Take(4).ToArray());
            SplinePointer = BitConverter.ToInt32(data.Skip(offset + 0x10).Take(4).ToArray());
            Unknown14 = BitConverter.ToInt16(data.Skip(offset + 0x14).Take(2).ToArray());
            Unknown16 = BitConverter.ToInt16(data.Skip(offset + 0x16).Take(2).ToArray());
            Unknown18 = BitConverter.ToInt16(data.Skip(offset + 0x18).Take(4).ToArray());
            NumSplineVerts = data.ElementAt(offset + 0x1C);
            SplineMode = data.ElementAt(offset + 0x1D);
            Unknown1E = data.ElementAt(offset + 0x1E);
            Unknown1F = data.ElementAt(offset + 0x1F);
            int coords1Ptr = BitConverter.ToInt32(data.Skip(offset + 0x20).Take(4).ToArray());
            int coords2Ptr = BitConverter.ToInt32(data.Skip(offset + 0x24).Take(4).ToArray());
            int coords3Ptr = BitConverter.ToInt32(data.Skip(offset + 0x28).Take(4).ToArray());
            if (coords1Ptr > 0)
            {
                SplineVertices1 = new Vector3[NumSplineVerts];
            }
            if (coords2Ptr > 0)
            {
                SplineVertices2 = new Vector3[NumSplineVerts];
            }
            if (coords3Ptr > 0)
            {
                SplineVertices3 = new Vector3[NumSplineVerts];
            }
            int coords4Ptr = BitConverter.ToInt32(data.Skip(offset + 0x2C).Take(4).ToArray());
            int coords5Ptr = BitConverter.ToInt32(data.Skip(offset + 0x30).Take(4).ToArray());
            if (coords4Ptr > 0)
            {
                Coords4 = new Vector2[NumSplineVerts];
            }
            if (coords5Ptr > 0)
            {
                Coords5 = new Vector2[NumSplineVerts];
            }
            for (int i = 0; i < NumSplineVerts; i++)
            {
                if (coords1Ptr > 0)
                {
                    SplineVertices1[i] = new(
                        BitConverter.ToSingle(data.Skip(coords1Ptr + i * 12).Take(4).ToArray()),
                        BitConverter.ToSingle(data.Skip(coords1Ptr + 4 + i * 12).Take(4).ToArray()),
                        BitConverter.ToSingle(data.Skip(coords1Ptr + 8 + i * 12).Take(4).ToArray())
                        );
                }
                if (coords2Ptr > 0)
                {
                    SplineVertices2[i] = new(
                        BitConverter.ToSingle(data.Skip(coords2Ptr + i * 12).Take(4).ToArray()),
                        BitConverter.ToSingle(data.Skip(coords2Ptr + 4 + i * 12).Take(4).ToArray()),
                        BitConverter.ToSingle(data.Skip(coords2Ptr + 8 + i * 12).Take(4).ToArray())
                        );
                }
                if (coords3Ptr > 0)
                {
                    SplineVertices3[i] = new(
                        BitConverter.ToSingle(data.Skip(coords3Ptr + i * 12).Take(4).ToArray()),
                        BitConverter.ToSingle(data.Skip(coords3Ptr + 4 + i * 12).Take(4).ToArray()),
                        BitConverter.ToSingle(data.Skip(coords3Ptr + 8 + i * 12).Take(4).ToArray())
                        );
                }
                if (coords4Ptr > 0)
                {
                    Coords4[i] = new(
                        BitConverter.ToSingle(data.Skip(coords4Ptr + i * 8).Take(4).ToArray()),
                        BitConverter.ToSingle(data.Skip(coords4Ptr + 4 + i * 8).Take(4).ToArray())
                        );
                }
                if (coords5Ptr > 0)
                {
                    Coords5[i] = new(
                        BitConverter.ToSingle(data.Skip(coords5Ptr + i * 8).Take(4).ToArray()),
                        BitConverter.ToSingle(data.Skip(coords5Ptr + 4 + i * 8).Take(4).ToArray())
                        );
                }
            }
            NumSpline2DVerts = data.ElementAt(offset + 0x34);
            Unknown35 = data.ElementAt(offset + 0x35);
            Unknown36 = data.ElementAt(offset + 0x36);
            Unknown37 = data.ElementAt(offset + 0x37);
            int secondaryCoordsPtr = BitConverter.ToInt32(data.Skip(offset + 0x38).Take(4).ToArray());
            if (secondaryCoordsPtr > 0)
            {
                Spline2DVertices = new Vector2[NumSpline2DVerts];
            }
            for (int i = 0; i < NumSpline2DVerts; i++)
            {
                if (secondaryCoordsPtr > 0)
                {
                    Spline2DVertices[i] = new(
                        BitConverter.ToSingle(data.Skip(secondaryCoordsPtr + i * 8).Take(4).ToArray()),
                        BitConverter.ToSingle(data.Skip(secondaryCoordsPtr + 4 + i * 8).Take(4).ToArray())
                        );
                }
            }
            Unknown3C = BitConverter.ToInt32(data.Skip(offset + 0x3C).Take(4).ToArray());
        }
    }
}
