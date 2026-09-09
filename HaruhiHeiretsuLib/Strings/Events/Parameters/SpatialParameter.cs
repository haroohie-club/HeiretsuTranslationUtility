using System;
using System.Linq;
using System.Numerics;
using HaruhiHeiretsuLib.Util;

namespace HaruhiHeiretsuLib.Strings.Events.Parameters;

/// <summary>
/// A spatial parameter, used to describe movement through 3D space
/// </summary>
public class SpatialParameter : ActionParameter
{
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown0C { get; set; }
    /// <summary>
    /// Pointer to a spline (dynamically allocated)
    /// </summary>
    public int SplinePointer { get; set; } // dynamically allocated
    /// <summary>
    /// Unknown
    /// </summary>
    public short Unknown14 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public short Unknown16 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown18 { get; set; }
    /// <summary>
    /// Number of vertices on the spline
    /// </summary>
    public byte NumSplineVerts { get; set; }
    /// <summary>
    /// Mode for the spline
    /// </summary>
    public byte SplineMode { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public byte Unknown1E { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public byte Unknown1F { get; set; }
    /// <summary>
    /// First set of spline verts
    /// </summary>
    public Vector3[] SplineVertices1 { get; set; }
    /// <summary>
    /// Second set of spline verts
    /// </summary>
    public Vector3[] SplineVertices2 { get; set; }
    /// <summary>
    /// Third set of spline verts
    /// </summary>
    public Vector3[] SplineVertices3 { get; set; }
    /// <summary>
    /// Coords
    /// </summary>
    public Vector2[] Coords4 { get; set; }
    /// <summary>
    /// Coords
    /// </summary>
    public Vector2[] Coords5 { get; set; }
    /// <summary>
    /// Number of 2D spline vertices
    /// </summary>
    public byte NumSpline2DVerts { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public byte Unknown35 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public byte Unknown36 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public byte Unknown37 { get; set; }
    /// <summary>
    /// 2D spline vert set
    /// </summary>
    public Vector2[] Spline2DVertices { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown3C { get; set; }

    /// <inheritdoc/>
    public SpatialParameter(byte[] data, int offset, ActionOpCode opCode) : base(data, offset, opCode)
    {
        Unknown0C = IO.ReadIntLE(data, offset + 0x0C);
        SplinePointer = IO.ReadIntLE(data, offset + 0x10);
        Unknown14 = IO.ReadShortLE(data, offset + 0x14);
        Unknown16 = IO.ReadShortLE(data, offset + 0x16);
        Unknown18 = IO.ReadIntLE(data, offset + 0x18);
        NumSplineVerts = data[offset + 0x1C];
        SplineMode = data[offset + 0x1D];
        Unknown1E = data[offset + 0x1E];
        Unknown1F = data[offset + 0x1F];
        int coords1Ptr = IO.ReadIntLE(data, offset + 0x20);
        int coords2Ptr = IO.ReadIntLE(data, offset + 0x24);
        int coords3Ptr = IO.ReadIntLE(data, offset + 0x28);
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
        int coords4Ptr = IO.ReadIntLE(data, offset + 0x2C);
        int coords5Ptr = IO.ReadIntLE(data, offset + 0x30);
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
        NumSpline2DVerts = data[offset + 0x34];
        Unknown35 = data[offset + 0x35];
        Unknown36 = data[offset + 0x36];
        Unknown37 = data[offset + 0x37];
        int secondaryCoordsPtr = IO.ReadIntLE(data, offset + 0x38);
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
        Unknown3C = IO.ReadIntLE(data, offset + 0x3C);
    }
}