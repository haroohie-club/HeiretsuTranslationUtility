﻿using HarfBuzzSharp;
using HaruhiHeiretsuLib.Util;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
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
        [JsonIgnore]
        public int SgeStartOffset { get; set; }
        public SgeHeader SgeHeader { get; set; }
        public List<SgeAnimation> SgeAnimations { get; set; } = [];
        public List<TranslateDataEntry> TranslateDataEntries { get; set; } = [];
        public List<RotateDataEntry> RotateDataEntries { get; set; } = [];
        public List<ScaleDataEntry> ScaleDataEntries { get; set; } = [];
        public List<KeyframeDefinition> KeyframeDefinitions { get; set; } = [];
        public List<SgeGXLightingData> SgeGXLightingDataTable { get; set; } = [];
        public List<SubmeshBlendData> SubmeshBlendDataTable { get; set; } = [];
        public List<Unknown40Entry> Unknown40Table { get; set; } = [];
        public List<Unknown4CEntry> Unknown4CTable { get; set; } = [];
        public List<Unknown50Entry> Unknown50Table { get; set; } = [];
        public List<Unknown58Entry> Unknown58Table { get; set; } = [];
        public List<SgeMesh> SgeMeshes { get; set; } = [];
        public List<SgeMaterial> SgeMaterials { get; set; } = [];
        public List<SgeBone> SgeBones { get; set; } = [];
        public List<List<SgeSubmesh>> SgeSubmeshes { get; set; } = [];

        // AnimTransformData
        [JsonIgnore]
        public int TranslateDataOffset { get; set; } // *AnimTransformDataOffset + 0
        [JsonIgnore]
        public int RotateDataOffset { get; set; } // *AnimTransformDataOffset + 4
        [JsonIgnore]
        public int ScaleDataOffset { get; set; } // *AnimTransformDataOffset + 8
        [JsonIgnore]
        public int KeyframeDefinitionsOffset { get; set; } // *AnimTransformDataOffset + 12
        [JsonIgnore]
        public short TranslateDataCount { get; set; } // *AnimTransformDataOffset + 16
        [JsonIgnore]
        public short RotateDataCount { get; set; } // *AnimTransformDataOffset + 18
        [JsonIgnore]
        public short ScaleDataCount { get; set; } // *AnimTransformDataOffset + 20
        [JsonIgnore]
        public short NumKeyframes { get; set; } // *AnimTransformDataOffset + 22

        private readonly JsonSerializerOptions _serializerOptions = new();

        public Sge()
        {
        }

        public Sge(IEnumerable<byte> data)
        {
            _serializerOptions.Converters.Add(new SgeBoneAttchedVertexConverter());
            _serializerOptions.MaxDepth = 100;
            _serializerOptions.IncludeFields = true;

            // SGEs are little-endian so no need for .Reverse() here
            SgeStartOffset = IO.ReadIntLE(data, 0x1C);
            IEnumerable<byte> sgeData = data.Skip(SgeStartOffset);
            SgeHeader = new(sgeData.Take(0x80));
            for (int i = 0; i < 10; i++) // There's always ten meshes
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
            for (int i = 0; i < SgeHeader.SubmeshBlendDataCount; i++)
            {
                SubmeshBlendDataTable.Add(new(sgeData.Skip(SgeHeader.SubmeshBlendDataTableOffset + i * 0x14).Take(0x14), SgeHeader.SubmeshBlendDataTableOffset + i * 0x14));
            }

            foreach (SgeMesh meshTableEntry in SgeMeshes.Take(1)) // but only the first mesh ever seems to matter
            {
                if (meshTableEntry.SubmeshAddress > 0 && meshTableEntry.SubmeshAddress % 4 == 0 && meshTableEntry.SubmeshCount > 0 && meshTableEntry.VertexAddress > 0)
                {
                    SgeSubmeshes.Add([]);
                    int prevStartVertex = 0;
                    if (meshTableEntry.SubmeshCount == 6)
                    {
                    }
                    for (int i = 0; i < meshTableEntry.SubmeshCount; i++)
                    {
                        SgeSubmesh nextSubmesh = new(sgeData, meshTableEntry.SubmeshAddress + i * 0x64, SgeMaterials, SgeBones, SubmeshBlendDataTable);
                        if (nextSubmesh.StartVertex < prevStartVertex)
                        {
                            SgeSubmeshes.Add([]);
                        }
                        SgeSubmeshes.Last().Add(nextSubmesh);
                        prevStartVertex = nextSubmesh.StartVertex;
                    }

                    List<(int numFaces, int startAddress)> faceTables = [];

                    List<List<SgeVertex>> vertexLists = [];
                    List<int> vertexStartAddresses = [];
                    for (int j = 0; j <  SgeSubmeshes.Count; j++)
                    {
                        vertexLists.Add([]);
                        vertexStartAddresses.Add(IO.ReadIntLE(sgeData, meshTableEntry.VertexAddress + j * 0x04));
                        int numVertices = IO.ReadIntLE(sgeData, vertexStartAddresses[j]);
                        int vertexStartAddress = IO.ReadIntLE(sgeData, vertexStartAddresses[j] + 0x04);
                        for (int i = 0; i < numVertices; i++)
                        {
                            vertexLists.Last().Add(new(sgeData.Skip(vertexStartAddress + i * 0x38).Take(0x38)));
                        }
                    }

                    for (int i = 0; i < SgeSubmeshes.Count; i++)
                    {
                        foreach (SgeSubmesh submesh in SgeSubmeshes[i])
                        {
                            submesh.SubmeshVertices = vertexLists[i].Skip(submesh.StartVertex).Take(submesh.EndVertex - submesh.StartVertex + 1).ToList();
                        }
                    }

                    bool triStripped = SgeHeader.ModelType == 4;
                    List<List<int>> faceLists = [];
                    for (int j = 0; j < vertexStartAddresses.Count; j++)
                    {
                        faceLists.Add([]);
                        int numFaces = IO.ReadIntLE(sgeData, vertexStartAddresses[j] + 0x08);
                        int faceStartAddress = IO.ReadIntLE(sgeData, vertexStartAddresses[j] + 0x0C);
                        for (int i = 0; i < numFaces; i++)
                        {
                            faceLists[j].Add(IO.ReadIntLE(sgeData, faceStartAddress + i * 4));
                        }
                    }

                    try
                    {
                        for (int listIdx = 0; listIdx < SgeSubmeshes.Count; listIdx++)
                        {
                            int currentMesh = 0;
                            foreach (SgeSubmesh submesh in SgeSubmeshes[listIdx])
                            {
                                for (int i = 0; (triStripped && i < submesh.FaceCount) || (!triStripped && i < submesh.FaceCount * 3);)
                                {
                                    if (triStripped)
                                    {
                                        submesh.SubmeshFaces.Add(new(faceLists[listIdx][submesh.StartFace + i + 2], faceLists[listIdx][submesh.StartFace + i + 1], faceLists[listIdx][submesh.StartFace + i], submesh.Material, i & 1));
                                        i++;
                                    }
                                    else
                                    {
                                        submesh.SubmeshFaces.Add(new(faceLists[listIdx][submesh.StartFace + i + 2], faceLists[listIdx][submesh.StartFace + i + 1], faceLists[listIdx][submesh.StartFace + i], submesh.Material));
                                        i += 3;
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
                                            SgeBoneAttachedVertex attachedVertex = new(listIdx, currentMesh, vertexIndex);
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
                    catch
                    {
                        Console.WriteLine($"Failed to create SGE faces: {SgeMaterials.FirstOrDefault(m => !string.IsNullOrEmpty(m.Name))?.Name ?? string.Empty}");
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

            for (int i = 0; i < SgeHeader.SgeGXLightingDataCount; i++)
            {
                SgeGXLightingDataTable.Add(new(sgeData.Skip(SgeHeader.SgeGXLightingDataTableOffset + i * 0x48).Take(0x48)));
            }

            for (int i = 0; i < SgeHeader.Unknown40Count; i++)
            {
                Unknown40Table.Add(new(sgeData.Skip(SgeHeader.Unknown40TableOffset + i * 0x18).Take(0x18)));
            }

            for (int i = 0; i < SgeHeader.Unknown4CCount; i++)
            {
                Unknown4CTable.Add(new(sgeData.Skip(SgeHeader.Unknown4CTableOffset + i * 0x18).Take(0x18)));
            }

            for (int i = 0; i < SgeHeader.Unknown50Count; i++)
            {
                Unknown50Table.Add(new(sgeData, SgeHeader.Unknown50TableOffset + i * 0x08));
            }

            for (int i = 0; i < SgeHeader.Unknown58Count; i++)
            {
                Unknown58Table.Add(new(sgeData.Skip(SgeHeader.Unknown58TableOffset + i * 0x20).Take(0x20)));
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

        /// <summary>
        /// Deserializes an SGE from JSON data
        /// </summary>
        /// <param name="json">A string containing a JSON-encoded SGE</param>
        /// <returns>An SGE file represented by that JSON</returns>
        public static Sge LoadFromJson(string json)
        {
            JsonSerializerOptions serializerOptions = new();
            serializerOptions.Converters.Add(new SgeBoneAttchedVertexConverter());
            serializerOptions.MaxDepth = 100;
            serializerOptions.IncludeFields = true;
            Sge sge = JsonSerializer.Deserialize<Sge>(json, serializerOptions);
            foreach (SgeBone bone in sge.SgeBones)
            {
                bone.Parent = sge.SgeBones.FirstOrDefault(b => b.Address == bone.ParentAddress);
            }
            return sge;
        }

        public List<byte> GetBytes()
        {
            List<byte> bytes = [];

            List<byte> preBytes = [];
            preBytes.AddRange(Encoding.ASCII.GetBytes($"SGE{SgeHeader.Version:D3}"));
            preBytes.AddRange(new byte[6]);

            // Calculate stuff
            SgeHeader.SgeGXLightingDataCount = SgeGXLightingDataTable.Count;
            SgeHeader.SubmeshBlendDataCount = SubmeshBlendDataTable.Count;
            SgeHeader.Unknown40Count = Unknown40Table.Count;
            SgeHeader.Unknown4CCount = Unknown4CTable.Count;
            SgeHeader.Unknown50Count = Unknown50Table.Count;
            SgeHeader.Unknown58Count = Unknown58Table.Count;
            SgeHeader.BonesCount = SgeBones.Count;
            SgeHeader.TexturesCount = SgeMaterials.Count;
            SgeHeader.NumAnimations = SgeAnimations.Count;
            int offset = 0x80;
            SgeHeader.SgeGXLightingDataTableOffset = offset;
            offset += Helpers.RoundToNearest16(SgeGXLightingDataTable.Count * 0x48);
            SgeHeader.SubmeshBlendDataTableOffset = offset;
            offset += Helpers.RoundToNearest16(SubmeshBlendDataTable.Count * 0x14);
            SgeHeader.TextureTableAddress = offset;
            offset += Helpers.RoundToNearest16(SgeMaterials.Count * 0x18);
            SgeHeader.Unknown40TableOffset = offset;
            offset += Helpers.RoundToNearest16(Unknown40Table.Count * 0x18);
            if (SgeHeader.Unknown4CCount > 0)
            {
                SgeHeader.Unknown4CTableOffset = offset;
                offset += Helpers.RoundToNearest16(Unknown4CTable.Count * 0x18);
            }
            SgeHeader.BonesTableAddress = offset;
            offset += Helpers.RoundToNearest16(SgeBones.Count * 0x28);
            if (SgeHeader.Unknown50Count > 0)
            {
                SgeHeader.Unknown50TableOffset = offset;
                offset += Helpers.RoundToNearest16(Unknown50Table.Count * 0x08 + Unknown50Table.Sum(u => Helpers.RoundToNearest16(u.UnknownShorts.Count * 0x02 + 2)));
            }
            if (SgeHeader.Unknown58Count > 0)
            {
                SgeHeader.Unknown58TableOffset = offset;
                offset += Helpers.RoundToNearest16(Unknown58Table.Count * 0x20);
            }
            SgeHeader.Unknown34Offset = offset;
            offset += 0x40;
            if (SgeHeader.NumAnimations > 0)
            {
                SgeHeader.AnimationDataTableAddress = offset;
                offset += Helpers.RoundToNearest16(SgeAnimations.Count * 0x38); // anim entries
            }
            int animBoneTableOffset = offset;
            offset += SgeAnimations.Sum(a => a.UsedKeyframes.Count == 0 ? 0 : Helpers.RoundToNearest16(SgeBones.Count * 4) + Helpers.RoundToNearest16(a.UsedKeyframes.Count * 6) + a.BoneTable.Sum(b => Helpers.RoundToNearest16(b.Keyframes.Count * 6))); // bone tables
            SgeHeader.AnimationTransformTableAddress = offset;
            offset += 0x20 + Helpers.RoundToNearest16(TranslateDataEntries.Count * 0x0C) + RotateDataEntries.Count * 0x10 + Helpers.RoundToNearest16(ScaleDataEntries.Count * 0x0C) + Helpers.RoundToNearest16(KeyframeDefinitions.Count * 0x28);
            SgeHeader.MeshTableAddress = offset;

            preBytes.AddRange(BitConverter.GetBytes(SgeHeader.MeshTableAddress));
            preBytes.AddRange(BitConverter.GetBytes(SgeHeader.MeshTableAddress + 0x3A7));

            bytes.AddRange(SgeHeader.GetBytes());
            bytes.AddRange(SgeGXLightingDataTable.SelectMany(u => u.GetBytes()));
            bytes.PadToNearest16();

            Dictionary<int, int> blendAddresses = [];
            int currentBlendAddress = bytes.Count;
            foreach (SubmeshBlendData blend in SubmeshBlendDataTable)
            {
                int oldAddress = blend.Offset;
                blend.Offset = currentBlendAddress;
                blendAddresses.Add(oldAddress, blend.Offset);
                bytes.AddRange(blend.GetBytes());
                currentBlendAddress = bytes.Count;
            }
            bytes.PadToNearest16();
            Dictionary<string, int> textureTable = [];
            foreach (SgeMaterial material in SgeMaterials)
            {
                textureTable.Add(material.Name, bytes.Count);
                bytes.AddRange(material.GetBytes());
            }
            bytes.PadToNearest16();
            bytes.AddRange(Unknown40Table.SelectMany(u => u.GetBytes()));
            bytes.PadToNearest16();
            bytes.AddRange(Unknown4CTable.SelectMany(u => u.GetBytes()));
            bytes.PadToNearest16();
            foreach (SgeBone bone in SgeBones)
            {
                bytes.AddRange(bone.GetBytes(bytes.Count));
            }
            bytes.PadToNearest16();

            List<byte> unknown50TableBytes = [];
            int startOffset = bytes.Count + Unknown50Table.Count * 8;
            foreach (Unknown50Entry unknown50 in Unknown50Table)
            {
                bytes.AddRange(BitConverter.GetBytes((long)(startOffset + unknown50TableBytes.Count)));
                unknown50TableBytes.AddRange(unknown50.UnknownShorts.Concat([(short)-1]).SelectMany(BitConverter.GetBytes));
                unknown50TableBytes.PadToNearest16();
            }
            bytes.AddRange(unknown50TableBytes);
            bytes.PadToNearest16();
            bytes.AddRange(Unknown58Table.SelectMany(u => u.GetBytes()));
            bytes.PadToNearest16();
            bytes.AddRange(new byte[] { 1 }.Concat(new byte[0x3F]));

            List<byte> boneTableBytes = [];
            foreach (SgeAnimation animation in SgeAnimations)
            {
                if (animation.UsedKeyframes.Count == 0)
                {
                    bytes.AddRange(new byte[0x38]);
                }
                else
                {
                    bytes.AddRange(animation.GetEntryBytes(animBoneTableOffset));
                    List<byte> animBoneTableBytes = animation.GetBoneTableBytes(animBoneTableOffset);
                    boneTableBytes.AddRange(animBoneTableBytes);
                    boneTableBytes.PadToNearest16();
                    animBoneTableOffset += animBoneTableBytes.Count;
                }
            }
            bytes.PadToNearest16();
            bytes.AddRange(boneTableBytes);
            int animTransformOffset = SgeHeader.AnimationTransformTableAddress + 0x20 + Helpers.RoundToNearest16(KeyframeDefinitions.Count * 0x28);
            bytes.AddRange(BitConverter.GetBytes(animTransformOffset)); // translate
            animTransformOffset += Helpers.RoundToNearest16(TranslateDataEntries.Count * 0x0C);
            bytes.AddRange(BitConverter.GetBytes(animTransformOffset)); // rot
            animTransformOffset += RotateDataEntries.Count * 0x10;
            bytes.AddRange(BitConverter.GetBytes(animTransformOffset)); // scale
            bytes.AddRange(BitConverter.GetBytes(SgeHeader.AnimationTransformTableAddress + 0x20)); // keyframes
            bytes.AddRange(BitConverter.GetBytes((short)TranslateDataEntries.Count));
            bytes.AddRange(BitConverter.GetBytes((short)RotateDataEntries.Count));
            bytes.AddRange(BitConverter.GetBytes((short)ScaleDataEntries.Count));
            bytes.AddRange(BitConverter.GetBytes((short)KeyframeDefinitions.Count));
            bytes.PadToNearest16();
            bytes.AddRange(KeyframeDefinitions.SelectMany(k => k.GetBytes()));
            bytes.PadToNearest16();
            bytes.AddRange(TranslateDataEntries.SelectMany(t => t.GetBytes()));
            bytes.PadToNearest16();
            bytes.AddRange(RotateDataEntries.SelectMany(r => r.GetBytes()));
            bytes.PadToNearest16();
            bytes.AddRange(ScaleDataEntries.SelectMany(s => s.GetBytes()));
            bytes.PadToNearest16();

            bool triStripped = SgeHeader.ModelType == 4;

            SgeMeshes[0].SubmeshCount = SgeSubmeshes.Sum(s => s.Count);
            SgeMeshes[0].SubmeshAddress = Helpers.RoundToNearest16(SgeHeader.MeshTableAddress + SgeMeshes.Count * 0x44);
            SgeMeshes[0].VertexAddress = Helpers.RoundToNearest16(SgeMeshes[0].SubmeshAddress + SgeSubmeshes.Sum(s => s.Count * 0x64));
            int previousVertexEndAddress = SgeMeshes[0].VertexAddress + SgeSubmeshes.Count * 0x10 + SgeSubmeshes.Sum(l => Helpers.RoundToNearest16(l.Sum(m => m.SubmeshVertices.Count) * 0x38)) + SgeSubmeshes.Sum(l => Helpers.RoundToNearest16(l.Sum(m => (m.SubmeshFaces.Count + (triStripped ? 2 : 0)) * (triStripped ? 4 : 12))));
            foreach (SgeMesh mesh in SgeMeshes.Skip(1))
            {
                mesh.VertexAddress = previousVertexEndAddress + 0x10;
                previousVertexEndAddress += 0x10;
            }
            foreach (SgeMesh mesh in SgeMeshes)
            {
                bytes.AddRange(mesh.GetBytes());
            }
            bytes.PadToNearest16();
            bytes.AddRange(SgeSubmeshes.SelectMany(l => l.SelectMany(s => s.GetBytes(textureTable, blendAddresses))));
            bytes.PadToNearest16();
            bytes.AddRange(BitConverter.GetBytes(bytes.Count + 0x10).Concat(BitConverter.GetBytes(bytes.Count + SgeSubmeshes.Count * 0x10 + Helpers.RoundToNearest16(SgeSubmeshes.First().Sum(s => s.SubmeshVertices.Count) * 0x38) + Helpers.RoundToNearest16(SgeSubmeshes.First().Sum(s => (s.SubmeshFaces.Count + (triStripped ? 2 : 0)) * (triStripped ? 4 : 12))))));
            bytes.AddRange(BitConverter.GetBytes(1).Concat(BitConverter.GetBytes(SgeSubmeshes.Count > 1 ? 1 : 0)));
            foreach (List<SgeSubmesh> submeshList in SgeSubmeshes)
            {
                bytes.AddRange(BitConverter.GetBytes(submeshList.Sum(s => s.SubmeshVertices.Count)).Concat(BitConverter.GetBytes(bytes.Count + 0x10))
                    .Concat(BitConverter.GetBytes(submeshList.Sum(s => s.SubmeshFaces.Count * (triStripped ? 1 : 3) + (triStripped ? 2 : 0)))).Concat(BitConverter.GetBytes(bytes.Count + 0x10 + Helpers.RoundToNearest16(submeshList.Sum(s => s.SubmeshVertices.Count) * 0x38))));
                bytes.PadToNearest16();
                bytes.AddRange(submeshList.SelectMany(s => s.SubmeshVertices.SelectMany(v => v.GetBytes())));
                bytes.PadToNearest16();
                if (triStripped)
                {
                    foreach (SgeSubmesh submesh in submeshList)
                    {
                        SgeFace firstFace = submesh.SubmeshFaces.First();
                        foreach (SgeFace face in submesh.SubmeshFaces)
                        {
                            bytes.AddRange(BitConverter.GetBytes(face.Polygon[2]));
                        }
                        SgeFace lastFace = submesh.SubmeshFaces.Last();
                        if (submesh.SubmeshFaces.Count % 2 == 0)
                        {
                            bytes.AddRange(BitConverter.GetBytes(lastFace.Polygon[0]));
                            bytes.AddRange(BitConverter.GetBytes(lastFace.Polygon[1]));
                        }
                        else
                        {
                            bytes.AddRange(BitConverter.GetBytes(lastFace.Polygon[1]));
                            bytes.AddRange(BitConverter.GetBytes(lastFace.Polygon[0]));
                        }
                    }
                }
                else
                {
                    bytes.AddRange(submeshList.SelectMany(s => s.SubmeshFaces.SelectMany(f => BitConverter.GetBytes(f.Polygon[2]).Concat(BitConverter.GetBytes(f.Polygon[1])).Concat(BitConverter.GetBytes(f.Polygon[0])))));
                }
                bytes.PadToNearest16();
            }
            bytes.PadToNearest16();
            foreach (SgeMesh mesh in SgeMeshes.Skip(1))
            {
                bytes.AddRange(BitConverter.GetBytes(bytes.Count + 0x10).Concat(BitConverter.GetBytes(bytes.Count + 0x10)));
                bytes.PadToNearest16();
            }

            preBytes.AddRange(BitConverter.GetBytes(bytes.Count + 0x5555));
            preBytes.AddRange(BitConverter.GetBytes(SgeHeader.MeshTableAddress + 0x20));
            preBytes.AddRange(BitConverter.GetBytes(0x20));
            return [.. preBytes, .. bytes];
        }
    }

    public class SgeHeader
    {
        public short Version { get; set; }    // 0x00
        public short ModelType { get; set; }    // 0x02 -- value of 0, 3, 4 or 5; of 3 gives outline on character model; 4 is tristriped
        [JsonIgnore]
        public int SgeGXLightingDataCount { get; set; }      // 0x04
        [JsonIgnore]
        public int SubmeshBlendDataCount { get; set; }      // 0x08
        public int Unknown40Count { get; set; }   // 0x0C
        [JsonIgnore]
        public int BonesCount { get; set; }     // 0x10
        [JsonIgnore]
        public int TexturesCount { get; set; }  // 0x14
        [JsonIgnore]
        public int Unknown4CCount { get; set; }      // 0x18
        public int Unknown1C { get; set; }      // 0x1C
        public int Unknown20 { get; set; }      // 0x20
        public int Unknown58Count { get; set; }      // 0x24
        [JsonIgnore]
        public int Unknown50Count { get; set; }      // 0x28
        public int Unknown2C { get; set; }      // 0x2C
        public int MeshTableAddress { get; set; } // 0x30
        public int Unknown34Offset { get; set; }      // 0x34
        public int SgeGXLightingDataTableOffset { get; set; }      // 0x38
        public int SubmeshBlendDataTableOffset { get; set; }      // 0x3C
        public int Unknown40TableOffset { get; set; }      // 0x40
        public int BonesTableAddress { get; set; }  // 0x44
        public int TextureTableAddress { get; set; }    // 0x48
        public int Unknown4CTableOffset { get; set; }      // 0x4C
        public int Unknown50TableOffset { get; set; }      // 0x50
        public int Unknown54 { get; set; }      // 0x54
        public int Unknown58TableOffset { get; set; }      // 0x58
        [JsonIgnore]
        public int NumAnimations { get; set; }      // 0x5C
        public int AnimationDataTableAddress { get; set; }      // 0x60
        public int AnimationTransformTableAddress { get; set; }      // 0x64
        [JsonIgnore]
        public int NumEventAnimations { get; set; }      // 0x68
        public int EventAnimationDataTableAddress { get; set; }      // 0x6C
        public int EventAnimationTransformTableAddress { get; set; }      // 0x70
        public int Unknown74 { get; set; }      // 0x74
        public int Unknown78 { get; set; }      // 0x78
        public int Unknown7C { get; set; }      // 0x7C

        public SgeHeader()
        {
        }

        public SgeHeader(IEnumerable<byte> headerData)
        {
            Version = IO.ReadShortLE(headerData, 0x00);
            ModelType = IO.ReadShortLE(headerData, 0x02);
            SgeGXLightingDataCount = IO.ReadIntLE(headerData, 0x04);
            SubmeshBlendDataCount = IO.ReadIntLE(headerData, 0x08);
            Unknown40Count = IO.ReadIntLE(headerData, 0x0C);
            BonesCount = IO.ReadIntLE(headerData, 0x10);
            TexturesCount = IO.ReadIntLE(headerData, 0x14);
            Unknown4CCount = IO.ReadIntLE(headerData, 0x18);
            Unknown1C = IO.ReadIntLE(headerData, 0x1C);
            Unknown20 = IO.ReadIntLE(headerData, 0x20);
            Unknown58Count = IO.ReadIntLE(headerData, 0x24);
            Unknown50Count = IO.ReadIntLE(headerData, 0x28);
            Unknown2C = IO.ReadIntLE(headerData, 0x2C);
            MeshTableAddress = IO.ReadIntLE(headerData, 0x30);
            Unknown34Offset = IO.ReadIntLE(headerData, 0x34);
            SgeGXLightingDataTableOffset = IO.ReadIntLE(headerData, 0x38);
            SubmeshBlendDataTableOffset = IO.ReadIntLE(headerData, 0x3C);
            Unknown40TableOffset = IO.ReadIntLE(headerData, 0x40);
            BonesTableAddress = IO.ReadIntLE(headerData, 0x44);
            TextureTableAddress = IO.ReadIntLE(headerData, 0x48);
            Unknown4CTableOffset = IO.ReadIntLE(headerData, 0x4C);
            Unknown50TableOffset = IO.ReadIntLE(headerData, 0x50);
            Unknown54 = IO.ReadIntLE(headerData, 0x54);
            Unknown58TableOffset = IO.ReadIntLE(headerData, 0x58);
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

        public List<byte> GetBytes()
        {
            List<byte> bytes = [];

            bytes.AddRange(BitConverter.GetBytes(Version));
            bytes.AddRange(BitConverter.GetBytes(ModelType));
            bytes.AddRange(BitConverter.GetBytes(SgeGXLightingDataCount));
            bytes.AddRange(BitConverter.GetBytes(SubmeshBlendDataCount));
            bytes.AddRange(BitConverter.GetBytes(Unknown40Count));
            bytes.AddRange(BitConverter.GetBytes(BonesCount));
            bytes.AddRange(BitConverter.GetBytes(TexturesCount));
            bytes.AddRange(BitConverter.GetBytes(Unknown4CCount));
            bytes.AddRange(BitConverter.GetBytes(Unknown1C));
            bytes.AddRange(BitConverter.GetBytes(Unknown20));
            bytes.AddRange(BitConverter.GetBytes(Unknown58Count));
            bytes.AddRange(BitConverter.GetBytes(Unknown50Count));
            bytes.AddRange(BitConverter.GetBytes(Unknown2C));
            bytes.AddRange(BitConverter.GetBytes(MeshTableAddress));
            bytes.AddRange(BitConverter.GetBytes(Unknown34Offset));
            bytes.AddRange(BitConverter.GetBytes(SgeGXLightingDataTableOffset));
            bytes.AddRange(BitConverter.GetBytes(SubmeshBlendDataTableOffset));
            bytes.AddRange(BitConverter.GetBytes(Unknown40TableOffset));
            bytes.AddRange(BitConverter.GetBytes(BonesTableAddress));
            bytes.AddRange(BitConverter.GetBytes(TextureTableAddress));
            bytes.AddRange(BitConverter.GetBytes(Unknown4CTableOffset));
            bytes.AddRange(BitConverter.GetBytes(Unknown50TableOffset));
            bytes.AddRange(BitConverter.GetBytes(Unknown54));
            bytes.AddRange(BitConverter.GetBytes(Unknown58TableOffset));
            bytes.AddRange(BitConverter.GetBytes(NumAnimations));
            bytes.AddRange(BitConverter.GetBytes(AnimationDataTableAddress));
            bytes.AddRange(BitConverter.GetBytes(AnimationTransformTableAddress));
            bytes.AddRange(BitConverter.GetBytes(NumEventAnimations));
            bytes.AddRange(BitConverter.GetBytes(EventAnimationDataTableAddress));
            bytes.AddRange(BitConverter.GetBytes(EventAnimationTransformTableAddress));
            bytes.AddRange(BitConverter.GetBytes(Unknown74));
            bytes.AddRange(BitConverter.GetBytes(Unknown78));
            bytes.AddRange(BitConverter.GetBytes(Unknown7C));

            return bytes;
        }
    }

    public class SgeAnimation
    {
        public float TotalFrames { get; set; }
        public int Unknown04 { get; set; }
        [JsonIgnore]
        public int BoneTableOffset { get; set; }
        [JsonIgnore]
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

        public SgeAnimation()
        {
        }

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

        public List<byte> GetEntryBytes(int boneTableOffset)
        {
            List<byte> entryBytes = [];
            BoneTableOffset = boneTableOffset;

            entryBytes.AddRange(BitConverter.GetBytes(TotalFrames));
            entryBytes.AddRange(BitConverter.GetBytes(Unknown04));
            entryBytes.AddRange(BitConverter.GetBytes(BoneTableOffset));
            entryBytes.AddRange(BitConverter.GetBytes(UsedKeyframes.Count));
            entryBytes.AddRange(BitConverter.GetBytes(Unknown10));
            entryBytes.AddRange(BitConverter.GetBytes(Unknown14));
            entryBytes.AddRange(BitConverter.GetBytes(Unknown18));
            entryBytes.AddRange(BitConverter.GetBytes(Unknown1C));
            entryBytes.AddRange(BitConverter.GetBytes(Unknown20));
            entryBytes.AddRange(BitConverter.GetBytes(Unknown24));
            entryBytes.AddRange(BitConverter.GetBytes(Unknown28));
            entryBytes.AddRange(BitConverter.GetBytes(Unknown2C));
            entryBytes.AddRange(BitConverter.GetBytes(Unknown30));
            entryBytes.AddRange(BitConverter.GetBytes(Unknown34));

            return entryBytes;
        }

        public List<byte> GetBoneTableBytes(int overallOffset)
        {
            List<byte> tableBytes = [];
            List<byte> entryBytes = [];
            int currentEntryOffset = Helpers.RoundToNearest16((BoneTable.Count + 1) * 0x04 + overallOffset);

            tableBytes.AddRange(BitConverter.GetBytes(currentEntryOffset));
            entryBytes.AddRange(UsedKeyframes.SelectMany(k => BitConverter.GetBytes(k).Concat(BitConverter.GetBytes(-1))));
            entryBytes.PadToNearest16();
            currentEntryOffset += entryBytes.Count;

            foreach (BoneTableEntry entry in BoneTable)
            {
                tableBytes.AddRange(BitConverter.GetBytes(currentEntryOffset));
                entryBytes.AddRange(entry.Keyframes.SelectMany(k => BitConverter.GetBytes(k.TranslateIndex).Concat(BitConverter.GetBytes(k.RotateIndex).Concat(BitConverter.GetBytes(k.ScaleIndex)))));
                entryBytes.PadToNearest16();
                currentEntryOffset = Helpers.RoundToNearest16((BoneTable.Count + 1) * 0x04 + overallOffset + entryBytes.Count);
            }

            tableBytes.PadToNearest16();
            return [.. tableBytes, .. entryBytes];
        }
    }

    public class BoneTableEntry
    {
        [JsonIgnore]
        public int Offset { get; set; }
        public List<BoneTableKeyframe> Keyframes { get; set; } = [];

        public BoneTableEntry()
        {
        }

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

        public TranslateDataEntry()
        {
        }

        public TranslateDataEntry(IEnumerable<byte> data)
        {
            X = IO.ReadFloat(data, 0x00);
            Y = IO.ReadFloat(data, 0x04);
            Z = IO.ReadFloat(data, 0x08);
        }

        public List<byte> GetBytes()
        {
            return [.. BitConverter.GetBytes(X), .. BitConverter.GetBytes(Y), .. BitConverter.GetBytes(Z)];
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

        public RotateDataEntry()
        {
        }

        public RotateDataEntry(IEnumerable<byte> data)
        {
            X = IO.ReadFloat(data, 0x00);
            Y = IO.ReadFloat(data, 0x04);
            Z = IO.ReadFloat(data, 0x08);
            W = IO.ReadFloat(data, 0x0C);
        }

        public List<byte> GetBytes()
        {
            return [.. BitConverter.GetBytes(X), .. BitConverter.GetBytes(Y), .. BitConverter.GetBytes(Z), .. BitConverter.GetBytes(W)];
        }
    }

    // 0x0C bytes
    // vector
    public class ScaleDataEntry
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public ScaleDataEntry()
        {
        }

        public ScaleDataEntry(IEnumerable<byte> data)
        {
            X = IO.ReadFloat(data, 0x00);
            Y = IO.ReadFloat(data, 0x04);
            Z = IO.ReadFloat(data, 0x08);
        }

        public List<byte> GetBytes()
        {
            return [.. BitConverter.GetBytes(X), .. BitConverter.GetBytes(Y), .. BitConverter.GetBytes(Z)];
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

        public KeyframeDefinition()
        {
        }

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

        public List<byte> GetBytes()
        {
            List<byte> bytes = [];

            bytes.AddRange(BitConverter.GetBytes(Unknown00));
            bytes.AddRange(BitConverter.GetBytes(Unknown04));
            bytes.AddRange(BitConverter.GetBytes(Unknown08));
            bytes.AddRange(BitConverter.GetBytes(Unknown0A));
            bytes.AddRange(BitConverter.GetBytes(Unknown0C));
            bytes.AddRange(BitConverter.GetBytes(NumFrames));
            bytes.AddRange(BitConverter.GetBytes(EndFrame));
            bytes.AddRange(BitConverter.GetBytes(Unknown14));
            bytes.AddRange(BitConverter.GetBytes(Unknown18));
            bytes.AddRange(BitConverter.GetBytes(Unknown1C));
            bytes.AddRange(BitConverter.GetBytes(Unknown20));
            bytes.AddRange(BitConverter.GetBytes(Unknown24));

            return bytes;
        }
    }

    public class SgeGXLightingData
    {
        public float AmbientR { get; set; }
        public float AmbientG { get; set; }
        public float AmbientB { get; set; }
        public float AmbientA { get; set; }
        public float MaterialR { get; set; }
        public float MaterialG { get; set; }
        public float MaterialB { get; set; }
        public float MaterialA { get; set; }
        public float CombinedR { get; set; }
        public float CombinedG { get; set; }
        public float CombinedB { get; set; }
        public float CombinedA { get; set; }
        public int Unknown30 { get; set; }
        public int Unknown34 { get; set; }
        public int Unknown38 { get; set; }
        public int Unknown3C { get; set; }
        public float Unknown40 { get; set; }
        public bool DefaultLightingEnabled { get; set; }

        public SgeGXLightingData()
        {
        }

        public SgeGXLightingData(IEnumerable<byte> data)
        {
            AmbientR = IO.ReadFloat(data, 0x00);
            AmbientG = IO.ReadFloat(data, 0x04);
            AmbientB = IO.ReadFloat(data, 0x08);
            AmbientA = IO.ReadFloat(data, 0x0C);
            MaterialR = IO.ReadFloat(data, 0x10);
            MaterialG = IO.ReadFloat(data, 0x14);
            MaterialB = IO.ReadFloat(data, 0x18);
            MaterialA = IO.ReadFloat(data, 0x1C);
            CombinedR = IO.ReadFloat(data, 0x20);
            CombinedG = IO.ReadFloat(data, 0x24);
            CombinedB = IO.ReadFloat(data, 0x28);
            CombinedA = IO.ReadFloat(data, 0x2C);
            Unknown30 = IO.ReadIntLE(data, 0x30);
            Unknown34 = IO.ReadIntLE(data, 0x34);
            Unknown38 = IO.ReadIntLE(data, 0x38);
            Unknown3C = IO.ReadIntLE(data, 0x3C);
            Unknown40 = IO.ReadFloat(data, 0x40);
            DefaultLightingEnabled = (IO.ReadIntLE(data, 0x44) & 1) != 0;
        }

        public List<byte> GetBytes()
        {
            List<byte> bytes = [];

            bytes.AddRange(BitConverter.GetBytes(AmbientR));
            bytes.AddRange(BitConverter.GetBytes(AmbientG));
            bytes.AddRange(BitConverter.GetBytes(AmbientB));
            bytes.AddRange(BitConverter.GetBytes(AmbientA));
            bytes.AddRange(BitConverter.GetBytes(MaterialR));
            bytes.AddRange(BitConverter.GetBytes(MaterialG));
            bytes.AddRange(BitConverter.GetBytes(MaterialB));
            bytes.AddRange(BitConverter.GetBytes(MaterialA));
            bytes.AddRange(BitConverter.GetBytes(CombinedR));
            bytes.AddRange(BitConverter.GetBytes(CombinedG));
            bytes.AddRange(BitConverter.GetBytes(CombinedB));
            bytes.AddRange(BitConverter.GetBytes(CombinedA));
            bytes.AddRange(BitConverter.GetBytes(Unknown30));
            bytes.AddRange(BitConverter.GetBytes(Unknown34));
            bytes.AddRange(BitConverter.GetBytes(Unknown38));
            bytes.AddRange(BitConverter.GetBytes(Unknown3C));
            bytes.AddRange(BitConverter.GetBytes(Unknown40));
            bytes.AddRange(BitConverter.GetBytes(DefaultLightingEnabled ? 1 : 0));

            return bytes;
        }
    }

    /// <summary>
    /// Data for applying GX blend to the first few submeshes of the model
    /// </summary>
    public class SubmeshBlendData
    {
        /// <summary>
        /// The offset this blend data is located at (used for submesh lookup)
        /// </summary>
        public int Offset { get; set; }
        /// <summary>
        /// When false, the game will ignore the custom blend factors for this object and use the default
        /// blend mode (src_factor = GX_BL_DSTCLR, dst_factor = GX_BL_INVDSTCLR)
        /// </summary>
        public bool UseCustomBlendMode { get; set; }
        /// <summary>
        /// The blend src_factor to pass to GXSetBlendMode for this object
        /// The options are:
        /// * 0 (GX_BL_ZERO)        0.0
        /// * 1 (GX_BL_ONE)         1.0
        /// * 4 (GX_BL_DSTCLR)      Frame buffer color
        /// * 5 (GX_BL_INVDSTCLR)   1.0 - (frame buffer color)
        /// * 6 (GX_BL_SRCALPHA)    Source alpha
        /// * 7 (GX_BL_INVSRCALPHA) 1.0 - (source alpha)
        /// * 8 (GX_BL_DSTALPHA)    Frame buffer alpha
        /// * 9 (GX_BL_INVDSTALPHA) 1.0 - (frame buffer alpha)
        /// </summary>
        public int CustomBlendSrcFactor { get; set; }
        /// <summary>
        /// The blend src_factor to pass to GXSetBlendMode for this object
        /// The options are:
        /// * 0 (GX_BL_ZERO)        0.0
        /// * 1 (GX_BL_ONE)         1.0
        /// * 2 (GX_BL_SRCCLR)      Source color
        /// * 3 (GX_BL_INVSRCCLR)   1.0 - (source color)
        /// * 6 (GX_BL_SRCALPHA)    Source alpha
        /// * 7 (GX_BL_INVSRCALPHA) 1.0 - (source alpha)
        /// * 8 (GX_BL_DSTALPHA)    Frame buffer alpha
        /// * 9 (GX_BL_INVDSTALPHA) 1.0 - (frame buffer alpha)
        /// </summary>
        public int CustomBlendDstFactor { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public float Unknown0C { get; set; }
        public int AlphaCompareAndZMode { get; set; }

        public SubmeshBlendData()
        {
        }

        public SubmeshBlendData(IEnumerable<byte> data, int offset)
        {
            Offset = offset;
            UseCustomBlendMode = IO.ReadIntLE(data, 0x00) != 0;
            CustomBlendSrcFactor = IO.ReadIntLE(data, 0x04);
            CustomBlendDstFactor = IO.ReadIntLE(data, 0x08);
            Unknown0C = IO.ReadFloat(data, 0x0C);
            AlphaCompareAndZMode = IO.ReadIntLE(data, 0x10);
        }

        public List<byte> GetBytes()
        {
            List<byte> bytes = [];

            bytes.AddRange(BitConverter.GetBytes(UseCustomBlendMode ? 1 : 0));
            bytes.AddRange(BitConverter.GetBytes(CustomBlendSrcFactor));
            bytes.AddRange(BitConverter.GetBytes(CustomBlendDstFactor));
            bytes.AddRange(BitConverter.GetBytes(Unknown0C));
            bytes.AddRange(BitConverter.GetBytes(AlphaCompareAndZMode));

            return bytes;
        }
    }

    public class Unknown40Entry
    {
        public int Unknown00 { get; set; }
        public float Unknown04 { get; set; }
        public float Unknown08 { get; set; }
        public float Unknown0C { get; set; }
        public float Unknown10 { get; set; }
        public int Unknown14 { get; set; }

        public Unknown40Entry()
        {
        }

        public Unknown40Entry(IEnumerable<byte> data)
        {
            Unknown00 = IO.ReadIntLE(data, 0x00);
            Unknown04 = IO.ReadFloat(data, 0x04);
            Unknown08 = IO.ReadFloat(data, 0x08);
            Unknown0C = IO.ReadFloat(data, 0x0C);
            Unknown10 = IO.ReadFloat(data, 0x10);
            Unknown14 = IO.ReadInt(data, 0x14);
        }

        public List<byte> GetBytes()
        {
            List<byte> bytes = [];

            bytes.AddRange(BitConverter.GetBytes(Unknown00));
            bytes.AddRange(BitConverter.GetBytes(Unknown04));
            bytes.AddRange(BitConverter.GetBytes(Unknown08));
            bytes.AddRange(BitConverter.GetBytes(Unknown0C));
            bytes.AddRange(BitConverter.GetBytes(Unknown10));
            bytes.AddRange(BitConverter.GetBytes(Unknown14).Reverse());

            return bytes;
        }
    }

    public class Unknown4CEntry
    {
        public short Unknown00 { get; set; }
        public short Unknown02 { get; set; }
        public int Unknown04 { get; set; }
        public float Unknown08 { get; set; }
        public short Unknown0C { get; set; }
        public short Unknown0E { get; set; }
        public short Unknown10 { get; set; }
        public short Unknown12 { get; set; }
        public int Unknown14 { get; set; }

        public Unknown4CEntry()
        {
        }

        public Unknown4CEntry(IEnumerable<byte> data)
        {
            Unknown00 = IO.ReadShortLE(data, 0x00);
            Unknown02 = IO.ReadShortLE(data, 0x02);
            Unknown04 = IO.ReadIntLE(data, 0x04);
            Unknown08 = IO.ReadFloat(data, 0x08);
            Unknown0C = IO.ReadShortLE(data, 0x0C);
            Unknown0E = IO.ReadShortLE(data, 0x0E);
            Unknown10 = IO.ReadShortLE(data, 0x10);
            Unknown12 = IO.ReadShortLE(data, 0x12);
            Unknown14 = IO.ReadIntLE(data, 0x14);
        }

        public List<byte> GetBytes()
        {
            List<byte> bytes = [];

            bytes.AddRange(BitConverter.GetBytes(Unknown00));
            bytes.AddRange(BitConverter.GetBytes(Unknown02));
            bytes.AddRange(BitConverter.GetBytes(Unknown04));
            bytes.AddRange(BitConverter.GetBytes(Unknown08));
            bytes.AddRange(BitConverter.GetBytes(Unknown0C));
            bytes.AddRange(BitConverter.GetBytes(Unknown0E));
            bytes.AddRange(BitConverter.GetBytes(Unknown10));
            bytes.AddRange(BitConverter.GetBytes(Unknown12));
            bytes.AddRange(BitConverter.GetBytes(Unknown14));

            return bytes;
        }
    }

    public class Unknown50Entry
    {
        public List<short> UnknownShorts { get; set; } = [];

        public Unknown50Entry()
        {
        }

        public Unknown50Entry(IEnumerable<byte> data, int offset)
        {
            int currentShortOffset = IO.ReadIntLE(data, offset);
            for (short unknownShort = IO.ReadShortLE(data, currentShortOffset); unknownShort > 0; unknownShort = IO.ReadShortLE(data, currentShortOffset))
            {
                UnknownShorts.Add(unknownShort);
                currentShortOffset += 2;
            }
        }
    }

    public class Unknown58Entry
    {
        public float Unknown00 { get; set; }
        public float Unknown04 { get; set; }
        public float Unknown08 { get; set; }
        public float Unknown0C { get; set; }
        public float Unknown10 { get; set; }
        public float Unknown14 { get; set; }
        public int Unknown18 { get; set; }
        public int Unknown1C { get; set; }

        public Unknown58Entry()
        {
        }

        public Unknown58Entry(IEnumerable<byte> data)
        {
            Unknown00 = IO.ReadFloat(data, 0x00);
            Unknown04 = IO.ReadFloat(data, 0x04);
            Unknown08 = IO.ReadFloat(data, 0x08);
            Unknown0C = IO.ReadFloat(data, 0x0C);
            Unknown10 = IO.ReadFloat(data, 0x10);
            Unknown14 = IO.ReadFloat(data, 0x14);
            Unknown18 = IO.ReadIntLE(data, 0x18);
            Unknown1C = IO.ReadIntLE(data, 0x1C);
        }

        public List<byte> GetBytes()
        {
            List<byte> bytes = [];

            bytes.AddRange(BitConverter.GetBytes(Unknown00));
            bytes.AddRange(BitConverter.GetBytes(Unknown04));
            bytes.AddRange(BitConverter.GetBytes(Unknown08));
            bytes.AddRange(BitConverter.GetBytes(Unknown0C));
            bytes.AddRange(BitConverter.GetBytes(Unknown10));
            bytes.AddRange(BitConverter.GetBytes(Unknown14));
            bytes.AddRange(BitConverter.GetBytes(Unknown18));
            bytes.AddRange(BitConverter.GetBytes(Unknown1C));

            return bytes;
        }
    }

    public class SgeMesh
    {
        public int Unknown00 { get; set; }
        public int Unknown04 { get; set; }
        public int SubmeshAddress { get; set; }
        public int SubmeshCount { get; set; }
        public int Unknown10 { get; set; }
        public int VertexAddress { get; set; }
        public int Unknown18 { get; set; }
        public int Unknown1C { get; set; }
        public int Unknown20 { get; set; }
        public float Unknown24 { get; set; }
        public float Unknown28 { get; set; }
        public float Unknown2C { get; set; }
        public int Unknown30 { get; set; }
        public int Unknown34 { get; set; }
        public float Unknown38 { get; set; }
        public float Unknown3C { get; set; }
        public float Unknown40 { get; set; }

        public SgeMesh()
        {
        }

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

        public List<byte> GetBytes()
        {
            List<byte> bytes = [];

            bytes.AddRange(BitConverter.GetBytes(Unknown00));
            bytes.AddRange(BitConverter.GetBytes(Unknown04));
            bytes.AddRange(BitConverter.GetBytes(SubmeshAddress));
            bytes.AddRange(BitConverter.GetBytes(SubmeshCount));
            bytes.AddRange(BitConverter.GetBytes(Unknown10));
            bytes.AddRange(BitConverter.GetBytes(VertexAddress));
            bytes.AddRange(BitConverter.GetBytes(Unknown18));
            bytes.AddRange(BitConverter.GetBytes(Unknown1C));
            bytes.AddRange(BitConverter.GetBytes(Unknown20));
            bytes.AddRange(BitConverter.GetBytes(Unknown24));
            bytes.AddRange(BitConverter.GetBytes(Unknown28));
            bytes.AddRange(BitConverter.GetBytes(Unknown2C));
            bytes.AddRange(BitConverter.GetBytes(Unknown30));
            bytes.AddRange(BitConverter.GetBytes(Unknown34));
            bytes.AddRange(BitConverter.GetBytes(Unknown38));
            bytes.AddRange(BitConverter.GetBytes(Unknown3C));
            bytes.AddRange(BitConverter.GetBytes(Unknown40));

            return bytes;
        }
    }

    public class SgeBone
    {
        [JsonIgnore]
        public SgeBone Parent { get; set; }
        public int Address { get; set; }
        public Vector3 Unknown00 { get; set; }
        public Vector3 HeadPosition { get; set; }
        public int ParentAddress { get; set; }
        public int AddressToBone1 { get; set; }
        public int AddressToBone2 { get; set; }
        /// <summary>
        /// A flag indicating which part of the body this bone belongs to on a character model
        /// 0x0002 - Neck bone
        /// 0x0004 - Face bone (Camera will focus on this bone when this model is the gazing point)
        /// 0x0008 - Chest bones
        /// 0x0010 - Stomach bone
        /// 0x0020 - Right hand bone
        /// 0x0040 - Left hand bone
        /// 0x0080 - Unknown
        /// 0x0100 - Unknown
        /// 0x0200 - Right foot bone (creates shadow)
        /// 0x0400 - Left foot bone (creates shadow)
        /// 0x0800 - Eyebrow bones
        /// 0x1000 - Right leg bone
        /// 0x2000 - Left leg bone
        /// 0x4000 - Right cheek bone
        /// 0x8000 - Left cheek bone
        /// </summary>
        public short BodyPart { get; set; }
        public short Unknown26 { get; set; }

        public Dictionary<SgeBoneAttachedVertex, float> VertexGroup { get; set; } = [];

        public SgeBone()
        {
        }

        public SgeBone(IEnumerable<byte> data, int offset)
        {
            Address = offset;
            Unknown00 = new Vector3(
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
            BodyPart = IO.ReadShortLE(data, offset + 0x24);
            Unknown26 = IO.ReadShortLE(data, offset + 0x26);
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

        public List<byte> GetBytes(int address)
        {
            List<byte> bytes = [];

            Address = address;
            bytes.AddRange(BitConverter.GetBytes(Unknown00.X));
            bytes.AddRange(BitConverter.GetBytes(Unknown00.Y));
            bytes.AddRange(BitConverter.GetBytes(Unknown00.Z));
            bytes.AddRange(BitConverter.GetBytes(HeadPosition.X));
            bytes.AddRange(BitConverter.GetBytes(HeadPosition.Y));
            bytes.AddRange(BitConverter.GetBytes(HeadPosition.Z));
            bytes.AddRange(BitConverter.GetBytes(Parent?.Address ?? 0));
            bytes.AddRange(BitConverter.GetBytes(AddressToBone1));
            bytes.AddRange(BitConverter.GetBytes(AddressToBone2));
            bytes.AddRange(BitConverter.GetBytes(BodyPart));
            bytes.AddRange(BitConverter.GetBytes(Unknown26));

            return bytes;
        }
    }

    public struct SgeBoneAttachedVertex(int submeshGroup, int mesh, int vertexIndex)
    {
        public int SubmeshGroup { get; set; } = submeshGroup;
        public int Submesh { get; set; } = mesh;
        public int VertexIndex { get; set; } = vertexIndex;

        public override string ToString()
        {
            return $"{SubmeshGroup},{Submesh},{VertexIndex}";
        }
    }

    public class SgeSubmesh
    {
        public List<SgeVertex> SubmeshVertices { get; set; } = [];
        public List<SgeFace> SubmeshFaces { get; set; } = [];

        public SgeMaterial Material { get; set; }
        public SubmeshBlendData BlendData { get; set; }

        public short Unknown00 { get; set; }
        public short Unknown02 { get; set; }
        public int Unknown04 { get; set; }
        [JsonIgnore]
        public int MaterialStringAddress { get; set; }
        [JsonIgnore]
        public int BlendDataAddress { get; set; }
        public int Unknown10 { get; set; }
        public SgeBone Bone { get; set; }
        public int BoneAddress { get; set; }
        public int Unknown18 { get; set; }
        public int Unknown1C { get; set; }
        public int Unknown20 { get; set; }
        public int StartVertex { get; set; }
        public int EndVertex { get; set; }
        public int StartFace { get; set; }
        public int FaceCount { get; set; }
        public List<short> BonePalette { get; set; } = [];
        public float Unknown54 { get; set; }
        public float Unknown58 { get; set; }
        public float Unknown5C { get; set; }
        public float Unknown60 { get; set; }

        public SgeSubmesh()
        {
        }

        public SgeSubmesh(IEnumerable<byte> data, int offset, List<SgeMaterial> materials, List<SgeBone> bones, List<SubmeshBlendData> blendData)
        {
            Unknown00 = IO.ReadShortLE(data, offset);
            Unknown02 = IO.ReadShortLE(data, offset + 0x02);
            Unknown04 = IO.ReadIntLE(data, offset + 0x04);
            MaterialStringAddress = IO.ReadIntLE(data, offset + 0x08);
            string materialString = Encoding.ASCII.GetString(data.Skip(MaterialStringAddress).TakeWhile(b => b != 0x00).ToArray());
            Material = materials.FirstOrDefault(m => m.Name == materialString);
            BlendDataAddress = IO.ReadIntLE(data, offset + 0x0C);
            BlendData = blendData.FirstOrDefault(b => b.Offset == BlendDataAddress);
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

        public List<byte> GetBytes(Dictionary<string, int> materialAddresses, Dictionary<int, int> blendAddresses)
        {
            List<byte> bytes = [];

            bytes.AddRange(BitConverter.GetBytes(Unknown00));
            bytes.AddRange(BitConverter.GetBytes(Unknown02));
            bytes.AddRange(BitConverter.GetBytes(Unknown04));
            if (materialAddresses.TryGetValue(Material?.Name ?? string.Empty, out int materialAddress))
            {
                bytes.AddRange(BitConverter.GetBytes(materialAddress));
            }
            else
            {
                bytes.AddRange(new byte[4]);
            }
            if (blendAddresses.TryGetValue(BlendData?.Offset ?? 0, out int blendAddress))
            {
                bytes.AddRange(BitConverter.GetBytes(blendAddress));
            }
            else
            {
                bytes.AddRange(new byte[4]);
            }
            bytes.AddRange(BitConverter.GetBytes(Unknown10));
            bytes.AddRange(BitConverter.GetBytes(BoneAddress));
            bytes.AddRange(BitConverter.GetBytes(Unknown18));
            bytes.AddRange(BitConverter.GetBytes(Unknown1C));
            bytes.AddRange(BitConverter.GetBytes(Unknown20));
            bytes.AddRange(BitConverter.GetBytes(StartVertex));
            bytes.AddRange(BitConverter.GetBytes(EndVertex));
            bytes.AddRange(BitConverter.GetBytes(StartFace));
            bytes.AddRange(BitConverter.GetBytes(FaceCount));
            bytes.AddRange(BonePalette.SelectMany(BitConverter.GetBytes));
            bytes.AddRange(BitConverter.GetBytes(Unknown54));
            bytes.AddRange(BitConverter.GetBytes(Unknown58));
            bytes.AddRange(BitConverter.GetBytes(Unknown5C));
            bytes.AddRange(BitConverter.GetBytes(Unknown60));

            return bytes;
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

        public SgeVertex()
        {
        }

        public SgeVertex(IEnumerable<byte> data)
        {
            Position = new Vector3(IO.ReadFloat(data, 0x00), IO.ReadFloat(data, 0x04), IO.ReadFloat(data, 0x08));
            float weight1 = IO.ReadFloat(data, 0x0C);
            float weight2 = IO.ReadFloat(data, 0x10);
            float weight3 = IO.ReadFloat(data, 0x14);
            Weight = [weight1, weight2, weight3, 1 - (weight1 + weight2 + weight3)];
            BoneIds = data.Skip(0x18).Take(4).ToArray();
            Normal = new Vector3(IO.ReadFloat(data, 0x1C), IO.ReadFloat(data, 0x20), IO.ReadFloat(data, 0x24));
            int color = IO.ReadIntLE(data, 0x28);
            Color = new VertexColor(((color & 0x00FF0000) >> 16) / 255.0f, ((color & 0x0000FF00) >> 8) / 255.0f, (color & 0x000000FF) / 255.0f, ((color & 0xFF000000) >> 24) / 255.0f);
            UVCoords = new Vector2(IO.ReadFloat(data, 0x2C), IO.ReadFloat(data, 0x30));
            Unknown2 = IO.ReadIntLE(data, 0x34);
        }

        public List<byte> GetBytes()
        {
            List<byte> bytes = [];

            bytes.AddRange(BitConverter.GetBytes(Position.X));
            bytes.AddRange(BitConverter.GetBytes(Position.Y));
            bytes.AddRange(BitConverter.GetBytes(Position.Z));
            bytes.AddRange(BitConverter.GetBytes(Weight[0]));
            bytes.AddRange(BitConverter.GetBytes(Weight[1]));
            bytes.AddRange(BitConverter.GetBytes(Weight[2]));
            bytes.AddRange(BoneIds);
            bytes.AddRange(BitConverter.GetBytes(Normal.X));
            bytes.AddRange(BitConverter.GetBytes(Normal.Y));
            bytes.AddRange(BitConverter.GetBytes(Normal.Z));
            int color = ((int)(Color.A * 255) << 24) | ((int)(Color.R * 255) << 16) | ((int)(Color.G * 255) << 8) | ((int)(Color.B * 255));
            bytes.AddRange(BitConverter.GetBytes(color));
            bytes.AddRange(BitConverter.GetBytes(UVCoords.X));
            bytes.AddRange(BitConverter.GetBytes(UVCoords.Y));
            bytes.AddRange(BitConverter.GetBytes(Unknown2));

            return bytes;
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

        public SgeFace()
        {
        }
        public SgeFace(List<int> polygon, SgeMaterial material)
        {
            Polygon = polygon;
            Material = material;
        }

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

        public SgeMaterial()
        {
        }

        public void ExportTexture(string fileName)
        {
            SKBitmap bitmap = Texture.GetImage().FlipBitmap();
            using FileStream fs = new(fileName, FileMode.Create);
            bitmap.Encode(fs, SKEncodedImageFormat.Png, 300);
        }

        public List<byte> GetBytes()
        {
            List<byte> bytes = [];

            bytes.AddRange(Encoding.ASCII.GetBytes(Name));
            bytes.AddRange(new byte[0x18 - bytes.Count]);

            return bytes;
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
