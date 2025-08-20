using HaruhiHeiretsuLib.Util;
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
        /// <summary>
        /// Representation of SGE data for an SGE model
        /// </summary>
        public Sge Sge { get; set; }
    }

    /// <summary>
    /// Representation of an SGE model (Shade Graphics Engine 3D model)
    /// </summary>
    public class Sge
    {
        /// <summary>
        /// The name of the model
        /// </summary>
        public string Name { get; set; }
        [JsonIgnore]
        internal int SgeStartOffset { get; set; }
        /// <summary>
        /// The SGE header data
        /// </summary>
        public SgeHeader SgeHeader { get; set; }
        /// <summary>
        /// List of SGE animations
        /// </summary>
        public List<SgeAnimation> SgeAnimations { get; set; } = [];
        /// <summary>
        /// List of all the animation translation operations
        /// </summary>
        public List<TranslateDataEntry> TranslateDataEntries { get; set; } = [];
        /// <summary>
        /// List of all the animation rotation operations
        /// </summary>
        public List<RotateDataEntry> RotateDataEntries { get; set; } = [];
        /// <summary>
        /// List of all the animation scale operations
        /// </summary>
        public List<ScaleDataEntry> ScaleDataEntries { get; set; } = [];
        /// <summary>
        /// List of all the animation keyframe definitions
        /// </summary>
        public List<KeyframeDefinition> KeyframeDefinitions { get; set; } = [];
        /// <summary>
        /// List of GX (Wii graphics) lighting data (assigned at the submesh level)
        /// </summary>
        public List<SgeGXLightingData> SgeGXLightingDataTable { get; set; } = [];
        /// <summary>
        /// List of blend data (assigned at the submesh level)
        /// </summary>
        public List<SubmeshBlendData> SubmeshBlendDataTable { get; set; } = [];
        public List<OutlineDataEntry> OutlineDataTable { get; set; } = [];
        public List<Unknown4CEntry> Unknown4CTable { get; set; } = [];
        /// <summary>
        /// List of bone animation groups (i.e. facial animation, mouth animation, various body part groups)
        /// </summary>
        public List<BoneAnimationGroup> BoneAnimationGroups { get; set; } = [];
        public List<Unknown58Entry> Unknown58Table { get; set; } = [];
        /// <summary>
        /// List of mesh objects -- technically these contain submeshes, but we store those separately
        /// </summary>
        public List<SgeMesh> SgeMeshes { get; set; } = [];
        /// <summary>
        /// List of materials (with textures)
        /// </summary>
        public List<SgeMaterial> SgeMaterials { get; set; } = [];
        /// <summary>
        /// List of bones (the armature/skeleton)
        /// </summary>
        public List<SgeBone> SgeBones { get; set; } = [];
        /// <summary>
        /// List of submeshes containing the geometry data
        /// </summary>
        public List<List<SgeSubmesh>> SgeSubmeshes { get; set; } = [];

        // AnimTransformData
        [JsonIgnore]
        internal int TranslateDataOffset { get; set; } // *AnimTransformDataOffset + 0
        [JsonIgnore]
        internal int RotateDataOffset { get; set; } // *AnimTransformDataOffset + 4
        [JsonIgnore]
        internal int ScaleDataOffset { get; set; } // *AnimTransformDataOffset + 8
        [JsonIgnore]
        internal int KeyframeDefinitionsOffset { get; set; } // *AnimTransformDataOffset + 12
        [JsonIgnore]
        internal short TranslateDataCount { get; set; } // *AnimTransformDataOffset + 16
        [JsonIgnore]
        internal short RotateDataCount { get; set; } // *AnimTransformDataOffset + 18
        [JsonIgnore]
        internal short ScaleDataCount { get; set; } // *AnimTransformDataOffset + 20
        [JsonIgnore]
        internal short NumKeyframes { get; set; } // *AnimTransformDataOffset + 22

        private readonly JsonSerializerOptions _serializerOptions = new();

        /// <summary>
        /// Blank constructor for deserialization
        /// </summary>
        public Sge()
        {
        }

        /// <summary>
        /// Constructs an SGE given binary SGE data
        /// </summary>
        /// <param name="data">The full, decompressed binary data from the SGE file in grp.bin</param>
        public Sge(byte[] data)
        {
            _serializerOptions.Converters.Add(new SgeBoneAttchedVertexConverter());
            _serializerOptions.Converters.Add(new SKColorConverter());
            _serializerOptions.MaxDepth = 100;
            _serializerOptions.IncludeFields = true;

            // SGEs are little-endian so no need for .Reverse() here
            SgeStartOffset = IO.ReadIntLE(data, 0x1C);
            byte[] sgeData = [.. data.Skip(SgeStartOffset)];
            SgeHeader = new([.. sgeData.Take(0x80)]);
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
            for (int i = 0; i < SgeHeader.SgeGXLightingDataCount; i++)
            {
                SgeGXLightingDataTable.Add(new([.. sgeData.Skip(SgeHeader.SgeGXLightingDataTableOffset + i * 0x48).Take(0x48)], SgeHeader.SgeGXLightingDataTableOffset + i * 0x48));
            }
            for (int i = 0; i < SgeHeader.SubmeshBlendDataCount; i++)
            {
                SubmeshBlendDataTable.Add(new([.. sgeData.Skip(SgeHeader.SubmeshBlendDataTableOffset + i * 0x14).Take(0x14)], SgeHeader.SubmeshBlendDataTableOffset + i * 0x14));
            }
            for (int i = 0; i < SgeHeader.OutlineDataCount; i++)
            {
                OutlineDataTable.Add(new([.. sgeData.Skip(SgeHeader.OutlineDataTableOffset + i * 0x18).Take(0x18)], SgeHeader.OutlineDataTableOffset + i * 0x18));
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
                            vertexLists.Last().Add(new([.. sgeData.Skip(vertexStartAddress + i * 0x38).Take(0x38)]));
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
                                        submesh.SubmeshFaces.Add(new(faceLists[listIdx][submesh.StartFace + i + 2], faceLists[listIdx][submesh.StartFace + i + 1], faceLists[listIdx][submesh.StartFace + i], i & 1));
                                        i++;
                                    }
                                    else
                                    {
                                        submesh.SubmeshFaces.Add(new(faceLists[listIdx][submesh.StartFace + i + 2], faceLists[listIdx][submesh.StartFace + i + 1], faceLists[listIdx][submesh.StartFace + i]));
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

            for (int i = 0; i < SgeHeader.Unknown4CCount; i++)
            {
                Unknown4CTable.Add(new([.. sgeData.Skip(SgeHeader.Unknown4CTableOffset + i * 0x18).Take(0x18)]));
            }

            for (int i = 0; i < SgeHeader.Unknown50Count; i++)
            {
                BoneAnimationGroups.Add(new(sgeData, SgeHeader.Unknown50TableOffset + i * 0x08));
            }

            for (int i = 0; i < SgeHeader.Unknown58Count; i++)
            {
                Unknown58Table.Add(new([.. sgeData.Skip(SgeHeader.Unknown58TableOffset + i * 0x20).Take(0x20)]));
            }

            for (int i = 0; i < TranslateDataCount; i++)
            {
                TranslateDataEntries.Add(new([.. sgeData.Skip(TranslateDataOffset + i * 0x0C).Take(0x0C)]));
            }

            for (int i = 0; i < RotateDataCount; i++)
            {
                RotateDataEntries.Add(new([.. sgeData.Skip(RotateDataOffset + i * 0x10).Take(0x10)]));
            }

            for (int i = 0; i < ScaleDataCount; i++)
            {
                ScaleDataEntries.Add(new([.. sgeData.Skip(ScaleDataOffset + i * 0x0C).Take(0x0C)]));
            }

            for (int i = 0; i < NumKeyframes; i++)
            {
                KeyframeDefinitions.Add(new([.. sgeData.Skip(KeyframeDefinitionsOffset + i * 0x28).Take(0x28)]));
            }
        }

        /// <summary>
        /// Finds the textures (in grp.bin) associated with each material
        /// As this needs to be called after resolving the names for every file in grp.bin,
        /// this method also will name the SGE with the provided name
        /// </summary>
        /// <param name="name">The name of this SGE (from dat.bin #008)</param>
        /// <param name="grpFiles">A list of all graphics files in grp.bin</param>
        public void ResolveTextures(string name, List<GraphicsFile> grpFiles)
        {
            Name = name;
            foreach (SgeMaterial material in SgeMaterials)
            {
                material.Texture = grpFiles.FirstOrDefault(t => t.Name == material.Name);
            }
        }

        /// <summary>
        /// Adds textures from disk to the SGE and give sthe SGE a name
        /// Note that while this creates the texture files, it does not add them to the grp/mcb
        /// You will need to loop through the materials after the fact to do this
        /// </summary>
        /// <param name="name">The name of the SGE</param>
        /// <param name="graphicsFiles">A list of paths to bitmap files -- the filenames must contain both the material name and texture format</param>
        public void AddExternalTextures(string name, string[] graphicsFiles)
        {
            Name = name;
            foreach (SgeMaterial material in SgeMaterials)
            {
                string graphicsFile = graphicsFiles.FirstOrDefault(f => f.Contains(material.Name));
                if (!string.IsNullOrEmpty(graphicsFile))
                {
                    material.Texture = new();
                    foreach (string mode in Enum.GetNames<GraphicsFile.ImageFormat>())
                    {
                        if (graphicsFile.Contains(mode))
                        {
                            material.Texture.InitializeNewTexture(material.Name, Enum.Parse<GraphicsFile.ImageFormat>(mode), SKBitmap.Decode(graphicsFile));
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Dumps this model to an SGE JSON intermediary file (and dumps all textures to a directory)
        /// </summary>
        /// <param name="texDirectory">A directory to dump textures to; if not provided, will dump them to a temp directory</param>
        /// <returns>A string of the JSON-encoded SGE</returns>
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
            serializerOptions.Converters.Add(new SKColorConverter());
            serializerOptions.MaxDepth = 100;
            serializerOptions.IncludeFields = true;
            Sge sge = JsonSerializer.Deserialize<Sge>(json, serializerOptions);
            foreach (SgeBone bone in sge.SgeBones)
            {
                bone.Parent = sge.SgeBones.FirstOrDefault(b => b.Address == bone.ParentAddress);
                bone.Child = sge.SgeBones.FirstOrDefault(b => b.Address == bone.ChildAddress);
                bone.NextSibling = sge.SgeBones.FirstOrDefault(b => b.Address == bone.NextSiblingAddress);
            }
            return sge;
        }

        /// <summary>
        /// Gets a binary representation of the SGE
        /// </summary>
        /// <returns>A byte list containing the SGE's binary data</returns>
        public List<byte> GetBytes()
        {
            List<byte> bytes = [];

            List<byte> preBytes = [];
            preBytes.AddRange(Encoding.ASCII.GetBytes($"SGE{SgeHeader.Version:D3}"));
            preBytes.AddRange(new byte[6]);

            // Calculate stuff
            SgeHeader.SgeGXLightingDataCount = SgeGXLightingDataTable.Count;
            SgeHeader.SubmeshBlendDataCount = SubmeshBlendDataTable.Count;
            SgeHeader.OutlineDataCount = OutlineDataTable.Count;
            SgeHeader.Unknown4CCount = Unknown4CTable.Count;
            SgeHeader.Unknown50Count = BoneAnimationGroups.Count;
            SgeHeader.Unknown58Count = Unknown58Table.Count;
            SgeHeader.BonesCount = SgeBones.Count;
            SgeHeader.TexturesCount = SgeMaterials.Count;
            SgeHeader.NumAnimations = SgeAnimations.Count;
            int offset = 0x80;
            if (SgeHeader.SgeGXLightingDataCount > 0)
            {
                SgeHeader.SgeGXLightingDataTableOffset = offset;
                offset += Helpers.RoundToNearest16(SgeGXLightingDataTable.Count * 0x48);
            }
            if (SgeHeader.SubmeshBlendDataCount > 0)
            {
                SgeHeader.SubmeshBlendDataTableOffset = offset;
                offset += Helpers.RoundToNearest16(SubmeshBlendDataTable.Count * 0x14);
            }
            SgeHeader.TextureTableAddress = offset;
            offset += Helpers.RoundToNearest16(SgeMaterials.Count * 0x18);
            if (SgeHeader.OutlineDataCount > 0)
            {
                SgeHeader.OutlineDataTableOffset = offset;
                offset += Helpers.RoundToNearest16(OutlineDataTable.Count * 0x18);
            }
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
                offset += Helpers.RoundToNearest16(BoneAnimationGroups.Count * 0x08 + BoneAnimationGroups.Sum(u => Helpers.RoundToNearest16(u.BoneIndices.Count * 0x02 + 2)));
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

            Dictionary<int, int> gxLightingAddresses = [];
            int currentGXLightingAddress = bytes.Count;
            foreach (SgeGXLightingData lighting in SgeGXLightingDataTable)
            {
                int oldAddress = lighting.Offset;
                lighting.Offset = currentGXLightingAddress;
                gxLightingAddresses.Add(oldAddress, lighting.Offset);
                bytes.AddRange(lighting.GetBytes());
                currentGXLightingAddress = bytes.Count;
            }
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

            Dictionary<int, int> outlineAddresses = [];
            int currentOutlineAddress = bytes.Count;
            foreach (OutlineDataEntry outline in OutlineDataTable)
            {
                int oldAddress = outline.Offset;
                outline.Offset = currentOutlineAddress;
                outlineAddresses.Add(oldAddress, outline.Offset);
                bytes.AddRange(outline.GetBytes());
                currentOutlineAddress = bytes.Count;
            }
            bytes.PadToNearest16();

            bytes.AddRange(Unknown4CTable.SelectMany(u => u.GetBytes()));
            bytes.PadToNearest16();

            int boneAddress = bytes.Count;
            int boneTableAddress = boneAddress;
            foreach (SgeBone bone in SgeBones)
            {
                bone.Address = boneAddress;
                boneAddress += 0x28;
            }
            foreach (SgeBone bone in SgeBones)
            {
                bytes.AddRange(bone.GetBytes());
            }
            bytes.PadToNearest16();

            List<byte> animGroupsBytes = [];
            int startOffset = bytes.Count + BoneAnimationGroups.Count * 8;
            foreach (BoneAnimationGroup animGroup in BoneAnimationGroups)
            {
                bytes.AddRange(BitConverter.GetBytes((long)(startOffset + animGroupsBytes.Count)));
                animGroupsBytes.AddRange(animGroup.BoneIndices.Concat([(short)-1]).SelectMany(BitConverter.GetBytes));
                animGroupsBytes.PadToNearest16();
            }
            bytes.AddRange(animGroupsBytes);
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
            bytes.AddRange(SgeSubmeshes.SelectMany(l => l.SelectMany(s => s.GetBytes(textureTable, blendAddresses, gxLightingAddresses, outlineAddresses, boneTableAddress))));
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

    /// <summary>
    /// The SGE's initial header
    /// </summary>
    public class SgeHeader
    {
        /// <summary>
        /// The version of the SGE (Heiretsu uses version 8)
        /// </summary>
        public short Version { get; set; }    // 0x00
        /// <summary>
        /// The model type for this SGE; the types appear to be as follows:
        /// 0: various object models
        /// 3: character models
        /// 4: map models (indicates tristripping)
        /// 5: unknown
        /// </summary>
        public short ModelType { get; set; }    // 0x02 -- value of 0, 3, 4 or 5; 4 is tristriped
        /// <summary>
        /// Count of GX lighting data objects
        /// </summary>
        [JsonIgnore]
        public int SgeGXLightingDataCount { get; set; }      // 0x04
        /// <summary>
        /// Count of submesh blend data objects
        /// </summary>
        [JsonIgnore]
        public int SubmeshBlendDataCount { get; set; }      // 0x08
        public int OutlineDataCount { get; set; }   // 0x0C
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
        public int OutlineDataTableOffset { get; set; }      // 0x40
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

        public SgeHeader(byte[] headerData)
        {
            Version = IO.ReadShortLE(headerData, 0x00);
            ModelType = IO.ReadShortLE(headerData, 0x02);
            SgeGXLightingDataCount = IO.ReadIntLE(headerData, 0x04);
            SubmeshBlendDataCount = IO.ReadIntLE(headerData, 0x08);
            OutlineDataCount = IO.ReadIntLE(headerData, 0x0C);
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
            OutlineDataTableOffset = IO.ReadIntLE(headerData, 0x40);
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
            bytes.AddRange(BitConverter.GetBytes(OutlineDataCount));
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
            bytes.AddRange(BitConverter.GetBytes(OutlineDataTableOffset));
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

        public SgeAnimation(byte[] data, int baseOffset, int numBones, int defOffset)
        {
            TotalFrames = IO.ReadFloatLE(data, defOffset);
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
                BoneTable.Add(new([.. data.Skip(offset).Take(6 * NumKeyframes)], offset, NumKeyframes));
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

        public BoneTableEntry(byte[] data, int offset, int numKeyframes)
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

        public TranslateDataEntry(byte[] data)
        {
            X = IO.ReadFloatLE(data, 0x00);
            Y = IO.ReadFloatLE(data, 0x04);
            Z = IO.ReadFloatLE(data, 0x08);
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

        public RotateDataEntry(byte[] data)
        {
            X = IO.ReadFloatLE(data, 0x00);
            Y = IO.ReadFloatLE(data, 0x04);
            Z = IO.ReadFloatLE(data, 0x08);
            W = IO.ReadFloatLE(data, 0x0C);
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

        public ScaleDataEntry(byte[] data)
        {
            X = IO.ReadFloatLE(data, 0x00);
            Y = IO.ReadFloatLE(data, 0x04);
            Z = IO.ReadFloatLE(data, 0x08);
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

        public KeyframeDefinition(byte[] data)
        {
            Unknown00 = IO.ReadFloatLE(data, 0x00);
            Unknown04 = IO.ReadFloatLE(data, 0x04);
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
        /// <summary>
        /// The offset this blend data is located at (used for submesh lookup)
        /// </summary>
        public int Offset { get; set; }
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

        public SgeGXLightingData(byte[] data, int offset)
        {
            Offset = offset;
            AmbientR = IO.ReadFloatLE(data, 0x00);
            AmbientG = IO.ReadFloatLE(data, 0x04);
            AmbientB = IO.ReadFloatLE(data, 0x08);
            AmbientA = IO.ReadFloatLE(data, 0x0C);
            MaterialR = IO.ReadFloatLE(data, 0x10);
            MaterialG = IO.ReadFloatLE(data, 0x14);
            MaterialB = IO.ReadFloatLE(data, 0x18);
            MaterialA = IO.ReadFloatLE(data, 0x1C);
            CombinedR = IO.ReadFloatLE(data, 0x20);
            CombinedG = IO.ReadFloatLE(data, 0x24);
            CombinedB = IO.ReadFloatLE(data, 0x28);
            CombinedA = IO.ReadFloatLE(data, 0x2C);
            Unknown30 = IO.ReadIntLE(data, 0x30);
            Unknown34 = IO.ReadIntLE(data, 0x34);
            Unknown38 = IO.ReadIntLE(data, 0x38);
            Unknown3C = IO.ReadIntLE(data, 0x3C);
            Unknown40 = IO.ReadFloatLE(data, 0x40);
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

        public SubmeshBlendData(byte[] data, int offset)
        {
            Offset = offset;
            UseCustomBlendMode = IO.ReadIntLE(data, 0x00) != 0;
            CustomBlendSrcFactor = IO.ReadIntLE(data, 0x04);
            CustomBlendDstFactor = IO.ReadIntLE(data, 0x08);
            Unknown0C = IO.ReadFloatLE(data, 0x0C);
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

    public class OutlineDataEntry
    {
        /// <summary>
        /// The offset this outline data is located at (used for submesh lookup)
        /// </summary>
        public int Offset { get; set; }
        public int Unknown00 { get; set; }
        public float Unknown04 { get; set; }
        /// <summary>
        /// The weight of the outline
        /// </summary>
        public float Weight { get; set; }
        /// <summary>
        /// The color of the outline (note that alpha is always 255)
        /// </summary>
        public SKColor Color { get; set; }
        public float Unknown10 { get; set; }
        public int Unknown14 { get; set; }

        public OutlineDataEntry()
        {
        }

        public OutlineDataEntry(byte[] data, int offset)
        {
            Offset = offset;
            Unknown00 = IO.ReadIntLE(data, 0x00);
            Unknown04 = IO.ReadFloatLE(data, 0x04);
            Weight = IO.ReadFloatLE(data, 0x08);
            Color = new(data.ElementAt(0x0C), data.ElementAt(0x0D), data.ElementAt(0x0E), 0xFF);
            Unknown10 = IO.ReadFloatLE(data, 0x10);
            Unknown14 = IO.ReadIntLE(data, 0x14);
        }

        public List<byte> GetBytes()
        {
            List<byte> bytes = [];

            bytes.AddRange(BitConverter.GetBytes(Unknown00));
            bytes.AddRange(BitConverter.GetBytes(Unknown04));
            bytes.AddRange(BitConverter.GetBytes(Weight));
            bytes.AddRange([Color.Red, Color.Green, Color.Blue, 0]);
            bytes.AddRange(BitConverter.GetBytes(Unknown10));
            bytes.AddRange(BitConverter.GetBytes(Unknown14));

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

        public Unknown4CEntry(byte[] data)
        {
            Unknown00 = IO.ReadShortLE(data, 0x00);
            Unknown02 = IO.ReadShortLE(data, 0x02);
            Unknown04 = IO.ReadIntLE(data, 0x04);
            Unknown08 = IO.ReadFloatLE(data, 0x08);
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

    public class BoneAnimationGroup
    {
        public List<short> BoneIndices { get; set; } = [];

        public BoneAnimationGroup()
        {
        }

        public BoneAnimationGroup(byte[] data, int offset)
        {
            int currentShortOffset = IO.ReadIntLE(data, offset);
            for (short unknownShort = IO.ReadShortLE(data, currentShortOffset); unknownShort > 0; unknownShort = IO.ReadShortLE(data, currentShortOffset))
            {
                BoneIndices.Add(unknownShort);
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

        public Unknown58Entry(byte[] data)
        {
            Unknown00 = IO.ReadFloatLE(data, 0x00);
            Unknown04 = IO.ReadFloatLE(data, 0x04);
            Unknown08 = IO.ReadFloatLE(data, 0x08);
            Unknown0C = IO.ReadFloatLE(data, 0x0C);
            Unknown10 = IO.ReadFloatLE(data, 0x10);
            Unknown14 = IO.ReadFloatLE(data, 0x14);
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

        public SgeMesh(byte[] data, int offset)
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
            Unknown24 = IO.ReadFloatLE(data, offset + 0x24);
            Unknown28 = IO.ReadFloatLE(data, offset + 0x28);
            Unknown2C = IO.ReadFloatLE(data, offset + 0x2C);
            Unknown30 = IO.ReadIntLE(data, offset + 0x30);
            Unknown34 = IO.ReadIntLE(data, offset + 0x34);
            Unknown38 = IO.ReadFloatLE(data, offset + 0x38);
            Unknown3C = IO.ReadFloatLE(data, offset + 0x3C);
            Unknown40 = IO.ReadFloatLE(data, offset + 0x40);
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
        /// <summary>
        /// The parent of this bone in the armature
        /// </summary>
        [JsonIgnore]
        public SgeBone Parent { get; set; }
        /// <summary>
        /// The child of this bone in the armature
        /// </summary>
        [JsonIgnore]
        public SgeBone Child { get; set; }
        /// <summary>
        /// The next sibling of this bone in the armature
        /// </summary>
        [JsonIgnore]
        public SgeBone NextSibling { get; set; }
        /// <summary>
        /// The address/offset of this bone in the SGE file's binary data.
        /// This is used to identify bones for armature connections, so it can be set arbitrarily
        /// if desired as long as the connections can be resolved.
        /// </summary>
        public int Address { get; set; }
        /// <summary>
        /// This clearly is supposed to be the offset of the tail of the bone,
        /// but using it while drawing the bones makes the animations all wonky, so we don't
        /// </summary>
        public Vector3 TailOffset { get; set; }
        /// <summary>
        /// The head position of the bone; we calculate its tail position to be a normal vector facing straight up
        /// </summary>
        public Vector3 HeadPosition { get; set; }
        /// <summary>
        /// The address of this bone's parent in the armature
        /// </summary>
        public int ParentAddress { get; set; }
        /// <summary>
        /// The address of this bone's child in the armature
        /// </summary>
        public int ChildAddress { get; set; }
        /// <summary>
        /// The address of this bone's next sibling in the armature
        /// </summary>
        public int NextSiblingAddress { get; set; }
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
        /// <summary>
        /// Unknown
        /// </summary>
        public short Unknown26 { get; set; }

        /// <summary>
        /// The vertex group associated with this bone (a dictionary of vertices and weights)
        /// This is used by the SGE blender import scripts
        /// </summary>
        public Dictionary<SgeBoneAttachedVertex, float> VertexGroup { get; set; } = [];

        /// <summary>
        /// Blank constructor for JSON deserialization
        /// </summary>
        public SgeBone()
        {
        }
        
        /// <summary>
        /// Constructs a bone from binary data
        /// </summary>
        /// <param name="data">The binary data of the bone</param>
        /// <param name="offset">The offset of the bone in the SGE file</param>
        public SgeBone(byte[] data, int offset)
        {
            Address = offset;
            TailOffset = new(
                IO.ReadFloatLE(data, offset + 0x00),
                IO.ReadFloatLE(data, offset + 0x04),
                IO.ReadFloatLE(data, offset + 0x08));
            HeadPosition = new(
                IO.ReadFloatLE(data, offset + 0x0C),
                IO.ReadFloatLE(data, offset + 0x10),
                IO.ReadFloatLE(data, offset + 0x14));
            ParentAddress = IO.ReadIntLE(data, offset + 0x18);
            ChildAddress = IO.ReadIntLE(data, offset + 0x1C);
            NextSiblingAddress = IO.ReadIntLE(data, offset + 0x20);
            BodyPart = IO.ReadShortLE(data, offset + 0x24);
            Unknown26 = IO.ReadShortLE(data, offset + 0x26);
        }

        /// <summary>
        /// Resolves this bone's connection to other bones
        /// </summary>
        /// <param name="bones">A list of bones in the armature</param>
        public void ResolveConnections(List<SgeBone> bones)
        {
            if (ParentAddress != 0)
            {
                Parent = bones.First(b => b.Address == ParentAddress);
            }
            if (ChildAddress != 0)
            {
                Child = bones.FirstOrDefault(b => b.Address == ChildAddress);
            }
            if (NextSiblingAddress != 0)
            {
                NextSibling = bones.FirstOrDefault(b => b.Address == NextSiblingAddress);
            }
        }

        /// <summary>
        /// Gets a binary representation of the bone.
        /// </summary>
        /// <remarks>
        /// Ensure the Address of all bones is set prior to calling this!
        /// </remarks>
        /// <returns>A byte array of binary data representing this bone</returns>
        public List<byte> GetBytes()
        {
            List<byte> bytes = [];

            bytes.AddRange(BitConverter.GetBytes(TailOffset.X));
            bytes.AddRange(BitConverter.GetBytes(TailOffset.Y));
            bytes.AddRange(BitConverter.GetBytes(TailOffset.Z));
            bytes.AddRange(BitConverter.GetBytes(HeadPosition.X));
            bytes.AddRange(BitConverter.GetBytes(HeadPosition.Y));
            bytes.AddRange(BitConverter.GetBytes(HeadPosition.Z));
            bytes.AddRange(BitConverter.GetBytes(Parent?.Address ?? 0));
            bytes.AddRange(BitConverter.GetBytes(Child?.Address ?? 0));
            bytes.AddRange(BitConverter.GetBytes(NextSibling?.Address ?? 0));
            bytes.AddRange(BitConverter.GetBytes(BodyPart));
            bytes.AddRange(BitConverter.GetBytes(Unknown26));

            return bytes;
        }
    }

    /// <summary>
    /// A struct representing a vertex in a bone's vertex group
    /// </summary>
    /// <param name="submeshGroup">The submesh group of the vertex</param>
    /// <param name="submesh">The submesh of the vertex</param>
    /// <param name="vertexIndex">The index of that vertex in the submesh</param>
    public struct SgeBoneAttachedVertex(int submeshGroup, int submesh, int vertexIndex)
    {
        /// <summary>
        /// The submesh group of the vertex
        /// </summary>
        public int SubmeshGroup { get; set; } = submeshGroup;
        /// <summary>
        /// The submesh of the vertex
        /// </summary>
        public int Submesh { get; set; } = submesh;
        /// <summary>
        /// The index of that vertex in the submesh
        /// </summary>
        public int VertexIndex { get; set; } = vertexIndex;

        /// <inheritdoc/>
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

        public short Unknown00 { get; set; }
        public short Unknown02 { get; set; }
        [JsonIgnore]
        public int BoneTableAddress { get; set; }
        [JsonIgnore]
        public int MaterialStringAddress { get; set; }
        public int BlendDataAddress { get; set; }
        public int GXLightingAddress { get; set; }
        public int OutlineAddress { get; set; }
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

        public SgeSubmesh(byte[] data, int offset, List<SgeMaterial> materials, List<SgeBone> bones, List<SubmeshBlendData> blendData)
        {
            Unknown00 = IO.ReadShortLE(data, offset);
            Unknown02 = IO.ReadShortLE(data, offset + 0x02);
            BoneTableAddress = IO.ReadIntLE(data, offset + 0x04);
            MaterialStringAddress = IO.ReadIntLE(data, offset + 0x08);
            string materialString = Encoding.ASCII.GetString(data.Skip(MaterialStringAddress).TakeWhile(b => b != 0x00).ToArray());
            Material = materials.FirstOrDefault(m => m.Name == materialString);
            BlendDataAddress = IO.ReadIntLE(data, offset + 0x0C);
            GXLightingAddress = IO.ReadIntLE(data, offset + 0x10);
            OutlineAddress = IO.ReadIntLE(data, offset + 0x14);
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
            Unknown54 = IO.ReadFloatLE(data, offset + 0x54);
            Unknown58 = IO.ReadFloatLE(data, offset + 0x58);
            Unknown5C = IO.ReadFloatLE(data, offset + 0x5C);
            Unknown60 = IO.ReadFloatLE(data, offset + 0x60);
        }

        public List<byte> GetBytes(Dictionary<string, int> materialAddresses, Dictionary<int, int> blendAddresses, Dictionary<int, int> gxLightingAddresses, Dictionary<int, int> outlineAddresses, int boneTableAddress)
        {
            List<byte> bytes = [];

            bytes.AddRange(BitConverter.GetBytes(Unknown00));
            bytes.AddRange(BitConverter.GetBytes(Unknown02));
            bytes.AddRange(BitConverter.GetBytes(boneTableAddress));
            if (materialAddresses.TryGetValue(Material?.Name ?? string.Empty, out int materialAddress))
            {
                bytes.AddRange(BitConverter.GetBytes(materialAddress));
            }
            else
            {
                bytes.AddRange(new byte[4]);
            }
            if (blendAddresses.TryGetValue(BlendDataAddress, out int blendAddress))
            {
                bytes.AddRange(BitConverter.GetBytes(blendAddress));
            }
            else
            {
                bytes.AddRange(new byte[4]);
            }
            if (gxLightingAddresses.TryGetValue(GXLightingAddress, out int gxLightingAddress))
            {
                bytes.AddRange(BitConverter.GetBytes(gxLightingAddress));
            }
            else
            {
                bytes.AddRange(new byte[4]);
            }
            if (outlineAddresses.TryGetValue(OutlineAddress, out int outlineAddress))
            {
                bytes.AddRange(BitConverter.GetBytes(outlineAddress));
            }
            else
            {
                bytes.AddRange(new byte[4]);
            }
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

        public SgeVertex(byte[] data)
        {
            Position = new(IO.ReadFloatLE(data, 0x00), IO.ReadFloatLE(data, 0x04), IO.ReadFloatLE(data, 0x08));
            float weight1 = IO.ReadFloatLE(data, 0x0C);
            float weight2 = IO.ReadFloatLE(data, 0x10);
            float weight3 = IO.ReadFloatLE(data, 0x14);
            Weight = [weight1, weight2, weight3, 1 - (weight1 + weight2 + weight3)];
            BoneIds = data.Skip(0x18).Take(4).ToArray();
            Normal = new(IO.ReadFloatLE(data, 0x1C), IO.ReadFloatLE(data, 0x20), IO.ReadFloatLE(data, 0x24));
            int color = IO.ReadIntLE(data, 0x28);
            Color = new(((color & 0x00FF0000) >> 16) / 255.0f, ((color & 0x0000FF00) >> 8) / 255.0f, (color & 0x000000FF) / 255.0f, ((color & 0xFF000000) >> 24) / 255.0f);
            UVCoords = new(IO.ReadFloatLE(data, 0x2C), IO.ReadFloatLE(data, 0x30));
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

        public SgeFace()
        {
        }

        public SgeFace(int first, int second, int third, int evenOdd = 0)
        {
            if (evenOdd == 0)
            {
                Polygon = [first, second, third];
            }
            else
            {
                Polygon = [second, first, third];
            }
        }

        public override string ToString()
        {
            return $"{Polygon[0]}, {Polygon[1]}, {Polygon[2]}";
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
