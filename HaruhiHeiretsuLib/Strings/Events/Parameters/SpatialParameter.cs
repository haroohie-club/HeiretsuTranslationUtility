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
    public int ParentActionOffset { get; set; } // dynamically allocated
    /// <summary>
    /// Unknown
    /// </summary>
    public short Unknown14 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public short Unknown16 { get; set; }
    /// <summary>
    /// Marks as active (used at runtime)
    /// </summary>
    public int IsActive { get; set; }
    /// <summary>
    /// Number of vertices on the spline
    /// </summary>
    public byte NumSplineKeys { get; set; }
    /// <summary>
    /// Mode for the spline (2 = timing curve)
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
    /// Position keys of the curve
    /// </summary>
    public Vector3[] SplinePositionKeys { get; set; } = [];
    /// <summary>
    /// Rotation keys of the curve
    /// </summary>
    public Vector3[] SplineRotationKeys { get; set; } = [];
    /// <summary>
    /// Scale keys of the curve
    /// </summary>
    public Vector3[] SplineScaleKeys { get; set; } = [];
    /// <summary>
    /// Coords
    /// </summary>
    public Vector2[] Coords4 { get; set; } = [];
    /// <summary>
    /// Coords
    /// </summary>
    public Vector2[] Coords5 { get; set; } = [];
    /// <summary>
    /// Number of 2D spline vertices
    /// </summary>
    public byte NumTimingSplineKeys { get; set; }
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
    public Vector2[] TimingSplineKeys { get; set; } = [];
    /// <summary>
    /// Flags (0x1, 0x2 -- auto orient; 0x4 use rotation keys; 0x20 relative to parent)
    /// </summary>
    public int Flags { get; set; }

    /// <inheritdoc/>
    public SpatialParameter(byte[] data, int offset, ActionOpCode opCode) : base(data, offset, opCode)
    {
        Unknown0C = IO.ReadIntLE(data, offset + 0x0C);
        ParentActionOffset = IO.ReadIntLE(data, offset + 0x10);
        Unknown14 = IO.ReadShortLE(data, offset + 0x14);
        Unknown16 = IO.ReadShortLE(data, offset + 0x16);
        IsActive = IO.ReadIntLE(data, offset + 0x18);
        NumSplineKeys = data[offset + 0x1C];
        SplineMode = data[offset + 0x1D];
        Unknown1E = data[offset + 0x1E];
        Unknown1F = data[offset + 0x1F];
        int coords1Ptr = IO.ReadIntLE(data, offset + 0x20);
        int coords2Ptr = IO.ReadIntLE(data, offset + 0x24);
        int coords3Ptr = IO.ReadIntLE(data, offset + 0x28);
        if (coords1Ptr > 0)
        {
            SplinePositionKeys = new Vector3[NumSplineKeys];
        }
        if (coords2Ptr > 0)
        {
            SplineRotationKeys = new Vector3[NumSplineKeys];
        }
        if (coords3Ptr > 0)
        {
            SplineScaleKeys = new Vector3[NumSplineKeys];
        }
        int coords4Ptr = IO.ReadIntLE(data, offset + 0x2C);
        int coords5Ptr = IO.ReadIntLE(data, offset + 0x30);
        if (coords4Ptr > 0)
        {
            Coords4 = new Vector2[NumSplineKeys];
        }
        if (coords5Ptr > 0)
        {
            Coords5 = new Vector2[NumSplineKeys];
        }
        for (int i = 0; i < NumSplineKeys; i++)
        {
            if (coords1Ptr > 0)
            {
                SplinePositionKeys[i] = new(
                    BitConverter.ToSingle(data.Skip(coords1Ptr + i * 12).Take(4).ToArray()),
                    BitConverter.ToSingle(data.Skip(coords1Ptr + 4 + i * 12).Take(4).ToArray()),
                    BitConverter.ToSingle(data.Skip(coords1Ptr + 8 + i * 12).Take(4).ToArray())
                );
            }
            if (coords2Ptr > 0)
            {
                SplineRotationKeys[i] = new(
                    BitConverter.ToSingle(data.Skip(coords2Ptr + i * 12).Take(4).ToArray()),
                    BitConverter.ToSingle(data.Skip(coords2Ptr + 4 + i * 12).Take(4).ToArray()),
                    BitConverter.ToSingle(data.Skip(coords2Ptr + 8 + i * 12).Take(4).ToArray())
                );
            }
            if (coords3Ptr > 0)
            {
                SplineScaleKeys[i] = new(
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
        NumTimingSplineKeys = data[offset + 0x34];
        Unknown35 = data[offset + 0x35];
        Unknown36 = data[offset + 0x36];
        Unknown37 = data[offset + 0x37];
        int secondaryCoordsPtr = IO.ReadIntLE(data, offset + 0x38);
        if (secondaryCoordsPtr > 0)
        {
            TimingSplineKeys = new Vector2[NumTimingSplineKeys];
        }
        for (int i = 0; i < NumTimingSplineKeys; i++)
        {
            if (secondaryCoordsPtr > 0)
            {
                TimingSplineKeys[i] = new(
                    BitConverter.ToSingle(data.Skip(secondaryCoordsPtr + i * 8).Take(4).ToArray()),
                    BitConverter.ToSingle(data.Skip(secondaryCoordsPtr + 4 + i * 8).Take(4).ToArray())
                );
            }
        }
        Flags = IO.ReadIntLE(data, offset + 0x3C);
    }
}