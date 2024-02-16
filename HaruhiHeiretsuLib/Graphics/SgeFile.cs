﻿using HaruhiHeiretsuLib.Util;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HaruhiHeiretsuLib.Graphics
{
    public partial class GraphicsFile
    {
        public Sge Sge { get; set; }
    }

    public class Sge
    {
        public string Name { get; set; }
        public int SgeStartOffset { get; set; }
        public SgeHeader SgeHeader { get; set; }
        public List<SgeAnimation> SgeAnimations { get; set; } = [];
        public List<TranslateDataEntry> TranslateDataEntries { get; set; } = [];
        public List<RotateDataEntry> RotateDataEntries { get; set; } = [];
        public List<ScaleDataEntry> ScaleDataEntries { get; set; } = [];
        public List<KeyframeDefinition> KeyframeDefinitions { get; set; } = [];
        public List<Unknown38Entry> Unknown38Table { get; set; } = [];
        public List<SgeMesh> SgeMeshes { get; set; } = [];
        public List<SgeMaterial> SgeMaterials { get; set; } = [];
        public List<SgeBone> SgeBones { get; set; } = [];
        public List<SgeSubmesh> SgeSubmeshes { get; set; } = [];

        // AnimTransformData
        public int TranslateDataOffset { get; set; } // *AnimTransformDataOffset + 0
        public int RotateDataOffset { get; set; } // *AnimTransformDataOffset + 4
        public int ScaleDataOffset { get; set; } // *AnimTransformDataOffset + 8
        public int KeyframeDefinitionsOffset { get; set; } // *AnimTransformDataOffset + 12
        public short TranslateDataCount { get; set; } // *AnimTransformDataOffset + 16
        public short RotateDataCount { get; set; } // *AnimTransformDataOffset + 18
        public short ScaleDataCount { get; set; } // *AnimTransformDataOffset + 20
        public short NumKeyframes { get; set; } // *AnimTransformDataOffset + 22

        private JsonSerializerOptions _serializerOptions = new();

        public Sge(IEnumerable<byte> data)
        {
            _serializerOptions.Converters.Add(new SgeBoneAttchedVertexConverter());
            _serializerOptions.MaxDepth = 100;
            _serializerOptions.IncludeFields = true;

            // SGEs are little-endian so no need for .Reverse() here
            SgeStartOffset = IO.ReadIntLE(data, 0x1C);
            IEnumerable<byte> sgeData = data.Skip(SgeStartOffset);
            SgeHeader = new(sgeData.Take(0x80));
            for (int i = 0; i < 1; i++)
            {
                SgeMeshes.Add(new(sgeData, SgeHeader.MeshTableAddress + 0x44 * i));
            }
            for (int i = 0; i < SgeHeader.TexturesCount; i++)
            {
                int nameOffset = SgeHeader.TextureTableAddress + i * 0x18;
                SgeMaterials.Add(new(i, Encoding.ASCII.GetString(sgeData.Skip(nameOffset).TakeWhile(b => b != 0x00).ToArray())));
            }
            for (int i = 0; i < SgeHeader.BonesCount; i++)
            {
                SgeBones.Add(new(sgeData, SgeHeader.BonesTableAddress + 0x28 * i));
            }
            foreach (SgeBone bone in SgeBones)
            {
                bone.ResolveConnections(SgeBones);
            }

            foreach (SgeMesh meshTableEntry in SgeMeshes)
            {
                if (meshTableEntry.SubmeshAddress > 0 && meshTableEntry.SubmeshAddress % 4 == 0 && meshTableEntry.SubmeshCount > 0 && meshTableEntry.VertexAddress > 0)
                {
                    for (int i = 0; i < meshTableEntry.SubmeshCount; i++)
                    {
                        SgeSubmeshes.Add(new(sgeData, meshTableEntry.SubmeshAddress + i * 0x64, SgeMaterials, SgeBones));
                    }

                    List<(int numVertices, int startAddress)> vertexTables = [];
                    List<(int numFaces, int startAddress)> faceTables = [];

                    for (int i = 0; i < 2; i++)
                    {
                        int vertexTableAddress = IO.ReadIntLE(sgeData, meshTableEntry.VertexAddress + i * 4);
                        int numVertices = IO.ReadIntLE(sgeData, vertexTableAddress);
                        int vertexStartAddress = IO.ReadIntLE(sgeData, vertexTableAddress + 0x04);
                        if (numVertices == vertexStartAddress) // tell tale sign that we're actually at the end of the file
                        {
                            break;
                        }
                        vertexTables.Add((numVertices, vertexStartAddress));
                        int numFaces = IO.ReadIntLE(sgeData, vertexTableAddress + 0x08);
                        int facesStartAddress = IO.ReadIntLE(sgeData, vertexTableAddress + 0x0C);
                        if (facesStartAddress == 0) // another sign that things are amiss
                        {
                            vertexTables.RemoveAt(1);
                            break;
                        }
                        faceTables.Add((numFaces, facesStartAddress));
                    }
                    List<List<SgeVertex>> vertexLists = [];
                    foreach ((int numVertices, int startAddress) in vertexTables)
                    {
                        vertexLists.Add([]);
                        for (int i = 0; i < numVertices; i++)
                        {
                            vertexLists.Last().Add(new(sgeData.Skip(startAddress + i * 0x38).Take(0x38)));
                        }
                    }

                    int count = 0;
                    int currentTable = 0;
                    foreach (SgeSubmesh submesh in SgeSubmeshes)
                    {
                        if (count > vertexTables[currentTable].numVertices)
                        {
                            count = 0;
                            currentTable++;
                        }
                        submesh.SubmeshVertices = vertexLists[currentTable].Skip(submesh.StartVertex).Take(submesh.EndVertex - submesh.StartVertex + 1).ToList();
                        count += submesh.SubmeshVertices.Count;
                    }

                    List<List<int>> faceLists = [];
                    currentTable = 0;
                    foreach ((int numFaces, int startAddress) in faceTables)
                    {
                        faceLists.Add([]);
                        for (int i = 0; i < numFaces; i++)
                        {
                            faceLists[currentTable].Add(IO.ReadIntLE(sgeData, startAddress + i * 4));
                        }
                        currentTable++;
                    }

                    bool triStripped = SgeHeader.ModelType == 4;
                    currentTable = 0;
                    count = 0;
                    int previousStartVertex = 0;
                    int currentMesh = 0;
                    foreach (SgeSubmesh submesh in SgeSubmeshes)
                    {
                        for (int i = 0; (triStripped && i < submesh.FaceCount) || (!triStripped && i < submesh.FaceCount * 3);)
                        {
                            if (previousStartVertex > submesh.StartVertex || count > faceTables[currentTable].numFaces)
                            {
                                previousStartVertex = 0;
                                count = 0;
                                currentTable++;
                            }
                            else
                            {
                                previousStartVertex = submesh.StartVertex;
                            }

                            if (triStripped)
                            {
                                submesh.SubmeshFaces.Add(new(faceLists[currentTable][submesh.StartFace + i + 2], faceLists[currentTable][submesh.StartFace + i + 1], faceLists[currentTable][submesh.StartFace + i], submesh.Material, i & 1));
                                i++;
                                count++;
                            }
                            else
                            {
                                submesh.SubmeshFaces.Add(new(faceLists[currentTable][submesh.StartFace + i + 2], faceLists[currentTable][submesh.StartFace + i + 1], faceLists[currentTable][submesh.StartFace + i], submesh.Material));
                                i += 3;
                                count += 3;
                            }

                            if (submesh.SubmeshFaces.Last().Polygon.Any(v => v >= submesh.SubmeshVertices.Count))
                            {
                                break;
                            }

                            IEnumerable<(int, SgeBone, float)> attachedBones = submesh.SubmeshFaces.Last().Polygon.SelectMany(v => submesh.SubmeshVertices[v].BoneIds.Select(b =>
                            {
                                int boneIndex = submesh.BonePalette[b];
                                if (boneIndex >= 0)
                                {
                                    return (v, SgeBones[boneIndex], submesh.SubmeshVertices[v].Weight[Array.IndexOf(submesh.SubmeshVertices[v].BoneIds, b)]);
                                }
                                return (v, null, 0);
                            }));
                            foreach ((int vertexIndex, SgeBone bone, float weight) in attachedBones)
                            {
                                if (bone is not null)
                                {
                                    SgeBoneAttachedVertex attachedVertex = new(currentMesh, vertexIndex);
                                    if (!bone.VertexGroup.ContainsKey(attachedVertex) && weight > 0)
                                    {
                                        bone.VertexGroup.Add(attachedVertex, weight);
                                    }
                                }
                            }
                        }
                        currentMesh++;
                    }
                }
            }
            
            TranslateDataOffset = IO.ReadIntLE(sgeData, SgeHeader.AnimationTransformTableAddress);
            RotateDataOffset = IO.ReadIntLE(sgeData, SgeHeader.AnimationTransformTableAddress + 0x04);
            ScaleDataOffset = IO.ReadIntLE(sgeData, SgeHeader.AnimationTransformTableAddress + 0x08);
            KeyframeDefinitionsOffset = IO.ReadIntLE(sgeData, SgeHeader.AnimationTransformTableAddress + 0x0C);
            TranslateDataCount = IO.ReadShortLE(sgeData, SgeHeader.AnimationTransformTableAddress + 0x10);
            RotateDataCount = IO.ReadShortLE(sgeData, SgeHeader.AnimationTransformTableAddress + 0x12);
            ScaleDataCount = IO.ReadShortLE(sgeData, SgeHeader.AnimationTransformTableAddress + 0x14);
            NumKeyframes = IO.ReadShortLE(sgeData, SgeHeader.AnimationTransformTableAddress + 0x16);

            for (int i = 0; i < SgeHeader.NumAnimations; i++)
            {
                SgeAnimations.Add(new(sgeData, 0, SgeHeader.BonesCount, SgeHeader.AnimationDataTableAddress + i * 0x38));
            }

            for (int i = 0; i < SgeHeader.Unknown38Count; i++)
            {
                Unknown38Table.Add(new(sgeData.Skip(SgeHeader.Unknown38TableOffset + i * 0x48).Take(0x48)));
            }

            for (int i = 0; i < TranslateDataCount; i++)
            {
                TranslateDataEntries.Add(new(sgeData.Skip(TranslateDataOffset + i * 0x0C).Take(0x0C)));
            }

            for (int i = 0; i < RotateDataCount; i++)
            {
                RotateDataEntries.Add(new(sgeData.Skip(RotateDataOffset + i * 0x10).Take(0x10)));
            }

            for (int i = 0; i < ScaleDataCount; i++)
            {
                ScaleDataEntries.Add(new(sgeData.Skip(ScaleDataOffset + i * 0x0C).Take(0x0C)));
            }

            for (int i = 0; i < NumKeyframes; i++)
            {
                KeyframeDefinitions.Add(new(sgeData.Skip(KeyframeDefinitionsOffset + i * 0x28).Take(0x28)));
            }
        }

        public void ResolveTextures(string name, List<GraphicsFile> textures)
        {
            Name = name;
            foreach (SgeMaterial material in SgeMaterials)
            {
                material.Texture = textures.FirstOrDefault(t => t.Name == material.Name);
            }
        }

        public void ResolveTextures(string name, string[] graphicsFiles)
        {
            Name = name;
            foreach (SgeMaterial material in SgeMaterials)
            {
                string graphicsFile = graphicsFiles.FirstOrDefault(f => f.Contains(material.Name));
                if (!string.IsNullOrEmpty(graphicsFile))
                {
                    material.Texture = new GraphicsFile();
                    material.Texture.Initialize(File.ReadAllBytes(graphicsFile), 0);
                }
            }
        }

        public string DumpJson(string texDirectory = "")
        {
            if (string.IsNullOrEmpty(texDirectory))
            {
                texDirectory = Path.Combine(Path.GetTempPath(), "sge-tex");
            }
            if (!Directory.Exists(texDirectory))
            {
                Directory.CreateDirectory(texDirectory);
            }

            foreach (SgeMaterial material in SgeMaterials)
            {
                if (material.Texture is not null)
                {
                    string fileName = Path.Combine(texDirectory, $"{material.Name}.png");
                    material.ExportTexture(fileName);
                    material.TexturePath = fileName;
                }
            }

            return JsonSerializer.Serialize(this, _serializerOptions);
        }
    }

    public class SgeHeader
    {
        public short Version { get; set; }    // 0x00
        public short ModelType { get; set; }    // 0x02 -- value of 0, 3, 4 or 5; of 3 gives outline on character model; 4 is tristriped
        public int Unknown38Count { get; set; }      // 0x04
        public int Unknown08 { get; set; }      // 0x08
        public int Unknown0C { get; set; }   // 0x0C
        public int BonesCount { get; set; }     // 0x10
        public int TexturesCount { get; set; }  // 0x14
        public int Unknown18 { get; set; }      // 0x18
        public int Unknown1C { get; set; }      // 0x1C
        public int Unknown20 { get; set; }      // 0x20
        public int Unknown24 { get; set; }      // 0x24
        public int Unknown28 { get; set; }      // 0x2
        public int Unknown2C { get; set; }      // 0x2C
        public int MeshTableAddress { get; set; } // 0x30
        public int Unknown34 { get; set; }      // 0x34
        public int Unknown38TableOffset { get; set; }      // 0x38
        public int Unknown3C { get; set; }      // 0x3C
        public int Unknown40 { get; set; }      // 0x40
        public int BonesTableAddress { get; set; }  // 0x44
        public int TextureTableAddress { get; set; }    // 0x48
        public int Unknown4C { get; set; }      // 0x4C
        public int Unknown50 { get; set; }      // 0x50
        public int Unknown54 { get; set; }      // 0x54
        public int Unknown58 { get; set; }      // 0x58
        public int NumAnimations { get; set; }      // 0x5C
        public int AnimationDataTableAddress { get; set; }      // 0x60
        public int AnimationTransformTableAddress { get; set; }      // 0x64
        public int NumEventAnimations { get; set; }      // 0x68
        public int EventAnimationDataTableAddress { get; set; }      // 0x6C
        public int EventAnimationTransformTableAddress { get; set; }      // 0x70
        public int Unknown74 { get; set; }      // 0x74
        public int Unknown78 { get; set; }      // 0x78
        public int Unknown7C { get; set; }      // 0x7C

        public SgeHeader(IEnumerable<byte> headerData)
        {
            Version = IO.ReadShortLE(headerData, 0x00);
            ModelType = IO.ReadShortLE(headerData, 0x02);
            Unknown38Count = IO.ReadIntLE(headerData, 0x04);
            Unknown08 = IO.ReadIntLE(headerData, 0x08);
            Unknown0C = IO.ReadIntLE(headerData, 0x0C);
            BonesCount = IO.ReadIntLE(headerData, 0x10);
            TexturesCount = IO.ReadIntLE(headerData, 0x14);
            Unknown18 = IO.ReadIntLE(headerData, 0x18);
            Unknown1C = IO.ReadIntLE(headerData, 0x1C);
            Unknown20 = IO.ReadIntLE(headerData, 0x20);
            Unknown24 = IO.ReadIntLE(headerData, 0x24);
            Unknown28 = IO.ReadIntLE(headerData, 0x28);
            Unknown2C = IO.ReadIntLE(headerData, 0x2C);
            MeshTableAddress = IO.ReadIntLE(headerData, 0x30);
            Unknown34 = IO.ReadIntLE(headerData, 0x34);
            Unknown38TableOffset = IO.ReadIntLE(headerData, 0x38);
            Unknown3C = IO.ReadIntLE(headerData, 0x3C);
            Unknown40 = IO.ReadIntLE(headerData, 0x40);
            BonesTableAddress = IO.ReadIntLE(headerData, 0x44);
            TextureTableAddress = IO.ReadIntLE(headerData, 0x48);
            Unknown4C = IO.ReadIntLE(headerData, 0x4C);
            Unknown50 = IO.ReadIntLE(headerData, 0x50);
            Unknown54 = IO.ReadIntLE(headerData, 0x54);
            Unknown58 = IO.ReadIntLE(headerData, 0x58);
            NumAnimations = IO.ReadIntLE(headerData, 0x5C);
            AnimationDataTableAddress = IO.ReadIntLE(headerData, 0x60);
            AnimationTransformTableAddress = IO.ReadIntLE(headerData, 0x64);
            NumEventAnimations = IO.ReadIntLE(headerData, 0x68);
            EventAnimationDataTableAddress = IO.ReadIntLE(headerData, 0x6C); // These tables are accessed when
            EventAnimationTransformTableAddress = IO.ReadIntLE(headerData, 0x70); // (anim number & 0x8000) is true
            Unknown74 = IO.ReadIntLE(headerData, 0x74);
            Unknown78 = IO.ReadIntLE(headerData, 0x78);
            Unknown7C = IO.ReadIntLE(headerData, 0x7C);
        }
    }

    public class SgeAnimation
    {
        public float TotalFrames { get; set; }
        public int Unknown04 { get; set; }
        public int BoneTableOffset { get; set; }
        public int NumKeyframes { get; set; }
        public int Unknown10 { get; set; }
        public int Unknown14 { get; set; }
        public int Unknown18 { get; set; }
        public int Unknown1C { get; set; }
        public int Unknown20 { get; set; }
        public int Unknown24 { get; set; }
        public int Unknown28 { get; set; }
        public int Unknown2C { get; set; }
        public int Unknown30 { get; set; }
        public int Unknown34 { get; set; }

        public List<short> UsedKeyframes { get; set; } = [];
        public List<BoneTableEntry> BoneTable { get; set; } = [];

        public SgeAnimation(IEnumerable<byte> data, int baseOffset, int numBones, int defOffset)
        {
            TotalFrames = IO.ReadFloat(data, defOffset);
            Unknown04 = IO.ReadIntLE(data, defOffset + 0x04);
            BoneTableOffset = IO.ReadIntLE(data, defOffset + 0x08);
            NumKeyframes = IO.ReadIntLE(data, defOffset + 0x0C);
            Unknown10 = IO.ReadIntLE(data, defOffset + 0x10);
            Unknown14 = IO.ReadIntLE(data, defOffset + 0x14);
            Unknown18 = IO.ReadIntLE(data, defOffset + 0x18);
            Unknown1C = IO.ReadIntLE(data, defOffset + 0x1C);
            Unknown20 = IO.ReadIntLE(data, defOffset + 0x20);
            Unknown24 = IO.ReadIntLE(data, defOffset + 0x24);
            Unknown28 = IO.ReadIntLE(data, defOffset + 0x28);
            Unknown2C = IO.ReadIntLE(data, defOffset + 0x2C);
            Unknown30 = IO.ReadIntLE(data, defOffset + 0x30);
            Unknown34 = IO.ReadIntLE(data, defOffset + 0x34);

            int usedKeyframesOffset = IO.ReadIntLE(data, baseOffset + BoneTableOffset);
            for (int i = 0; i < NumKeyframes; i++)
            {
                UsedKeyframes.Add(IO.ReadShortLE(data, baseOffset + usedKeyframesOffset + i * 6));
            }
            for (int i = 1; i < numBones; i++)
            {
                int offset = IO.ReadIntLE(data, baseOffset + BoneTableOffset + i * 4) + baseOffset;
                BoneTable.Add(new(data.Skip(offset).Take(6 * NumKeyframes), offset, NumKeyframes));
            }
        }
    }

    public class BoneTableEntry
    {
        public int Offset { get; set; }
        public List<BoneTableKeyframe> Keyframes { get; set; } = [];

        public BoneTableEntry(IEnumerable<byte> data, int offset, int numKeyframes)
        {
            for (int i = 0; i < numKeyframes; i++)
            {
                Offset = offset;
                Keyframes.Add(new(
                    IO.ReadShortLE(data, i * 6),
                    IO.ReadShortLE(data, i * 6 + 2),
                    IO.ReadShortLE(data, i * 6 + 4)
                    ));
            }
        }
    }

    public struct BoneTableKeyframe
    {
        public short TranslateIndex { get; set; }
        public short RotateIndex { get; set; }
        public short ScaleIndex { get; set; }

        public BoneTableKeyframe(short translate, short rotate, short scale)
        {
            TranslateIndex = translate;
            RotateIndex = rotate;
            ScaleIndex = scale;
        }
    }

    // 0x0C bytes
    // vector
    public class TranslateDataEntry
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public TranslateDataEntry(IEnumerable<byte> data)
        {
            X = IO.ReadFloat(data, 0x00);
            Y = IO.ReadFloat(data, 0x04);
            Z = IO.ReadFloat(data, 0x08);
        }
    }

    // 0x10 bytes
    // quaternion
    public class RotateDataEntry
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float W { get; set; }

        public RotateDataEntry(IEnumerable<byte> data)
        {
            X = IO.ReadFloat(data, 0x00);
            Y = IO.ReadFloat(data, 0x04);
            Z = IO.ReadFloat(data, 0x08);
            W = IO.ReadFloat(data, 0x0C);
        }
    }

    // 0x0C bytes
    // vector
    public class ScaleDataEntry
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public ScaleDataEntry(IEnumerable<byte> data)
        {
            X = IO.ReadFloat(data, 0x00);
            Y = IO.ReadFloat(data, 0x04);
            Z = IO.ReadFloat(data, 0x08);
        }
    }

    // 0x28 bytes
    public class KeyframeDefinition
    {
        public float Unknown00 { get; set; }
        public float Unknown04 { get; set; }
        public ushort Unknown08 { get; set; }
        public ushort Unknown0A { get; set; }
        public int Unknown0C { get; set; }
        public ushort NumFrames { get; set; }
        public ushort EndFrame { get; set; }
        public int Unknown14 { get; set; }
        public int Unknown18 { get; set; }
        public int Unknown1C { get; set; } // Pointer
        public int Unknown20 { get; set; } // Pointer
        public int Unknown24 { get; set; } // Pointer

        public KeyframeDefinition(IEnumerable<byte> data)
        {
            Unknown00 = IO.ReadFloat(data, 0x00);
            Unknown04 = IO.ReadFloat(data, 0x04);
            Unknown08 = IO.ReadUShortLE(data, 0x08);
            Unknown0A = IO.ReadUShortLE(data, 0x0A);
            Unknown0C = IO.ReadIntLE(data, 0x0C);
            NumFrames = IO.ReadUShortLE(data, 0x10);
            EndFrame = IO.ReadUShortLE(data, 0x12);
            Unknown14 = IO.ReadIntLE(data, 0x14);
            Unknown18 = IO.ReadIntLE(data, 0x18);
            Unknown1C = IO.ReadIntLE(data, 0x1C);
            Unknown20 = IO.ReadIntLE(data, 0x20);
            Unknown24 = IO.ReadIntLE(data, 0x24);
        }
    }

    public class Unknown38Entry
    {
        public float Unknown00 { get; set; }
        public float Unknown04 { get; set; }
        public float Unknown08 { get; set; }
        public float Unknown0C { get; set; }
        public float Unknown10 { get; set; }
        public float Unknown14 { get; set; }
        public float Unknown18 { get; set; }
        public float Unknown1C { get; set; }
        public float Unknown20 { get; set; }
        public float Unknown24 { get; set; }
        public float Unknown28 { get; set; }
        public float Unknown2C { get; set; }
        public int Unknown30 { get; set; }
        public int Unknown34 { get; set; }
        public int Unknown38 { get; set; }
        public int Unknown3C { get; set; }
        public float Unknown40 { get; set; }
        public int Unknown44 { get; set; }

        public Unknown38Entry(IEnumerable<byte> data)
        {
            Unknown00 = IO.ReadFloat(data, 0x00);
            Unknown04 = IO.ReadFloat(data, 0x04);
            Unknown08 = IO.ReadFloat(data, 0x08);
            Unknown0C = IO.ReadFloat(data, 0x0C);
            Unknown10 = IO.ReadFloat(data, 0x10);
            Unknown14 = IO.ReadFloat(data, 0x14);
            Unknown18 = IO.ReadFloat(data, 0x18);
            Unknown1C = IO.ReadFloat(data, 0x1C);
            Unknown20 = IO.ReadFloat(data, 0x20);
            Unknown24 = IO.ReadFloat(data, 0x24);
            Unknown28 = IO.ReadFloat(data, 0x28);
            Unknown2C = IO.ReadFloat(data, 0x2C);
            Unknown30 = IO.ReadIntLE(data, 0x30);
            Unknown34 = IO.ReadIntLE(data, 0x34);
            Unknown38 = IO.ReadIntLE(data, 0x38);
            Unknown3C = IO.ReadIntLE(data, 0x3C);
            Unknown40 = IO.ReadFloat(data, 0x3C);
            Unknown44 = IO.ReadIntLE(data, 0x3C);
        }
    }

    public class SgeMesh
    {
        public int Unknown00 { get; set; }          // 1
        public int Unknown04 { get; set; }          // 2
        public int SubmeshAddress { get; set; }            // 3
        public int SubmeshCount { get; set; }       // 4
        public int Unknown10 { get; set; }          // 5
        public int VertexAddress { get; set; }      // 6
        public int Unknown18 { get; set; }          // 7
        public int Unknown1C { get; set; }          // 8
        public int Unknown20 { get; set; }          // 9
        public float Unknown24 { get; set; }          // 10
        public float Unknown28 { get; set; }          // 11
        public float Unknown2C { get; set; }          // 12
        public int Unknown30 { get; set; }          // 13
        public int Unknown34 { get; set; }          // 14
        public float Unknown38 { get; set; }          // 15
        public float Unknown3C { get; set; }          // 16
        public float Unknown40 { get; set; }          // 17

        public SgeMesh(IEnumerable<byte> data, int offset)
        {
            Unknown00 = IO.ReadIntLE(data, offset);
            Unknown04 = IO.ReadIntLE(data, offset + 0x04);
            SubmeshAddress = IO.ReadIntLE(data, offset + 0x08);
            SubmeshCount = IO.ReadIntLE(data, offset + 0x0C);
            Unknown10 = IO.ReadIntLE(data, offset + 0x10);
            VertexAddress = IO.ReadIntLE(data, offset + 0x14);
            Unknown18 = IO.ReadIntLE(data, offset + 0x18);
            Unknown1C = IO.ReadIntLE(data, offset + 0x1C);
            Unknown20 = IO.ReadIntLE(data, offset + 0x20);
            Unknown24 = IO.ReadFloat(data, offset + 0x24);
            Unknown28 = IO.ReadFloat(data, offset + 0x28);
            Unknown2C = IO.ReadFloat(data, offset + 0x2C);
            Unknown30 = IO.ReadIntLE(data, offset + 0x30);
            Unknown34 = IO.ReadIntLE(data, offset + 0x34);
            Unknown38 = IO.ReadFloat(data, offset + 0x38);
            Unknown3C = IO.ReadFloat(data, offset + 0x3C);
            Unknown40 = IO.ReadFloat(data, offset + 0x40);
        }
    }

    public class SgeBone
    {
        [JsonIgnore]
        public SgeBone Parent { get; set; }
        public int Address { get; set; }
        public Vector3 TailOffset { get; set; }      // 1
        public Vector3 HeadPosition { get; set; }
        public int ParentAddress { get; set; }
        public int AddressToBone1 { get; set; }     // 4
        public int AddressToBone2 { get; set; }     // 5
        public int Count { get; set; }              // 6

        public Dictionary<SgeBoneAttachedVertex, float> VertexGroup { get; set; } = [];

        public SgeBone(IEnumerable<byte> data, int offset)
        {
            Address = offset;
            TailOffset = new Vector3(
                IO.ReadFloat(data, offset + 0x00),
                IO.ReadFloat(data, offset + 0x04),
                IO.ReadFloat(data, offset + 0x08));
            HeadPosition = new Vector3(
                IO.ReadFloat(data, offset + 0x0C),
                IO.ReadFloat(data, offset + 0x10),
                IO.ReadFloat(data, offset + 0x14));
            ParentAddress = IO.ReadIntLE(data, offset + 0x18);
            AddressToBone1 = IO.ReadIntLE(data, offset + 0x1C);
            AddressToBone2 = IO.ReadIntLE(data, offset + 0x20);
            Count = IO.ReadIntLE(data, offset + 0x24);
        }

        public void ResolveConnections(List<SgeBone> bones)
        {
            if (ParentAddress != 0)
            {
                Parent = bones.First(b => b.Address == ParentAddress);
            }
            //if (AddressToBone1 != 0)
            //{
            //    Bone1 = bones.First(b => b.Address == AddressToBone1);
            //}
            //if (AddressToBone2 != 0)
            //{
            //    Bone2 = bones.First(b => b.Address == AddressToBone2);
            //}
        }
    }

    public struct SgeBoneAttachedVertex
    {
        public int Mesh { get; set; }
        public int VertexIndex { get; set; }

        public SgeBoneAttachedVertex(int mesh, int vertexIndex)
        {
            Mesh = mesh;
            VertexIndex = vertexIndex;
        }

        public override string ToString()
        {
            return $"{Mesh},{VertexIndex}";
        }
    }

    public class SgeSubmesh
    {
        public List<SgeVertex> SubmeshVertices { get; set; } = [];
        public List<SgeFace> SubmeshFaces { get; set; } = [];

        public SgeMaterial Material { get; set; }
        public int Unknown00 { get; set; }          // 2
        public int Unknown04 { get; set; }          // 3
        public int MaterialStringAddress { get; set; }
        public int Unknown0C { get; set; }          // 5
        public int Unknown10 { get; set; }          // 6
        public SgeBone Bone { get; set; }
        public int BoneAddress { get; set; }        // 7
        public int Unknown18 { get; set; }          // 8
        public int Unknown1C { get; set; }          // 9
        public int Unknown20 { get; set; }              // 11
        public int StartVertex { get; set; }
        public int EndVertex { get; set; }      // 12
        public int StartFace { get; set; }             // 13
        public int FaceCount { get; set; }
        public List<short> BonePalette { get; set; } = [];
        public float Unknown54 { get; set; }          // 23
        public float Unknown58 { get; set; }          // 24
        public float Unknown5C { get; set; }          // 25
        public float Unknown60 { get; set; }          // 26

        public SgeSubmesh(IEnumerable<byte> data, int offset, List<SgeMaterial> materials, List<SgeBone> bones)
        {
            Unknown00 = IO.ReadIntLE(data, offset);
            Unknown04 = IO.ReadIntLE(data, offset + 0x04);
            MaterialStringAddress = IO.ReadIntLE(data, offset + 0x08);
            string materialString = Encoding.ASCII.GetString(data.Skip(MaterialStringAddress).TakeWhile(b => b != 0x00).ToArray());
            Material = materials.FirstOrDefault(m => m.Name == materialString);
            Unknown0C = IO.ReadIntLE(data, offset + 0x0C);
            Unknown10 = IO.ReadIntLE(data, offset + 0x10);
            BoneAddress = IO.ReadIntLE(data, offset + 0x14);
            Bone = bones.FirstOrDefault(b => b.Address == BoneAddress);
            Unknown18 = IO.ReadIntLE(data, offset + 0x18);
            Unknown1C = IO.ReadIntLE(data, offset + 0x1C);
            Unknown20 = IO.ReadIntLE(data, offset + 0x20);
            StartVertex = IO.ReadIntLE(data, offset + 0x24);
            EndVertex = IO.ReadIntLE(data, offset + 0x28);
            StartFace = IO.ReadIntLE(data, offset + 0x2C);
            FaceCount = IO.ReadIntLE(data, offset + 0x30);
            for (int i = 0; i < 16; i++)
            {
                BonePalette.Add(IO.ReadShortLE(data, offset + 0x34 + i * 2));
            }
            Unknown54 = IO.ReadFloat(data, offset + 0x54);
            Unknown58 = IO.ReadFloat(data, offset + 0x58);
            Unknown5C = IO.ReadFloat(data, offset + 0x5C);
            Unknown60 = IO.ReadFloat(data, offset + 0x60);
        }
    }

    public class SgeVertex
    {
        public Vector3 Position { get; set; }
        public float[] Weight { get; set; }
        [JsonIgnore]
        public byte[] BoneIds { get; set; }
        public int[] BoneIndices { get => BoneIds.Select(i => (int)i).ToArray(); set => BoneIds = value.Select(i => (byte)i).ToArray(); }
        public Vector3 Normal { get; set; }
        public VertexColor Color { get; set; }
        public Vector2 UVCoords { get; set; }
        public int Unknown2 { get; set; }

        public SgeVertex(IEnumerable<byte> data)
        {
            Position = new Vector3(IO.ReadFloat(data, 0x00), IO.ReadFloat(data, 0x04), IO.ReadFloat(data, 0x08));
            float weight1 = IO.ReadFloat(data, 0x0C);
            float weight2 = IO.ReadFloat(data, 0x10);
            float weight3 = IO.ReadFloat(data, 0x14);
            Weight = [weight1, weight2, weight3, 1 - (weight1 + weight2 + weight3)];
            BoneIds = [data.ElementAt(0x18), data.ElementAt(0x19), data.ElementAt(0x1A), data.ElementAt(0x1B)];
            Normal = new Vector3(IO.ReadFloat(data, 0x1C), IO.ReadFloat(data, 0x20), IO.ReadFloat(data, 0x24));
            int color = IO.ReadIntLE(data, 0x28);
            Color = new VertexColor(((color & 0x00FF0000) >> 16) / 255.0f, ((color & 0x0000FF00) >> 8) / 255.0f, (color & 0x000000FF) / 255.0f, ((color & 0xFF000000) >> 24) / 255.0f);
            UVCoords = new Vector2(IO.ReadFloat(data, 0x2C), IO.ReadFloat(data, 0x30));
            Unknown2 = IO.ReadIntLE(data, 0x34);
        }

        public override string ToString()
        {
            return $"{Position}";
        }
    }

    public class SgeFace
    {
        public List<int> Polygon { get; set; }
        public SgeMaterial Material { get; set; }

        public SgeFace(int first, int second, int third, SgeMaterial material, int evenOdd = 0)
        {
            if (evenOdd == 0)
            {
                Polygon = [first, second, third];
            }
            else
            {
                Polygon = [second, first, third];
            }
            Material = material;
        }

        public override string ToString()
        {
            return $"{Material}: {Polygon[0]}, {Polygon[1]}, {Polygon[2]}";
        }
    }

    public class SgeMaterial
    {
        public int Index { get; set; }
        public string Name { get; set; }
        [JsonIgnore]
        public GraphicsFile Texture { get; set; }
        public string TexturePath { get; set; }

        public SgeMaterial(int index, string name)
        {
            Index = index;
            Name = name;
        }

        public void ExportTexture(string fileName)
        {
            SKBitmap bitmap = Texture.GetImage().FlipBitmap();
            using FileStream fs = new(fileName, FileMode.Create);
            bitmap.Encode(fs, SKEncodedImageFormat.Png, 300);
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public struct VertexColor
    {
        public float R { get; set; }
        public float G { get; set; }
        public float B { get; set; }
        public float A { get; set; }

        public VertexColor(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public override string ToString()
        {
            return $"{R} {G} {B} {A}";
        }
    }
}
