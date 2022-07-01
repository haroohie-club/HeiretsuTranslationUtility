using Newtonsoft.Json;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace HaruhiHeiretsuLib.Graphics
{
    public partial class GraphicsFile
    {
        public Sge Sge { get; set; }
    }

    public class Sge
    {
        public const float MODEL_SCALE = 25.4f;
        public string Name { get; set; }
        public int SgeStartOffset { get; set; }
        public SgeHeader SgeHeader { get; set; }
        public List<SgeMesh> SgeMeshes { get; set; } = new();
        public List<SgeMaterial> SgeMaterials { get; set; } = new();
        public List<SgeBone> SgeBones { get; set; } = new();
        public List<SgeVertex> SgeVertices { get; set; } = new();
        public List<SgeSubmesh> SgeSubmeshes { get; set; } = new();
        public List<SgeFace> SgeFaces { get; set; } = new();

        public Sge(IEnumerable<byte> data)
        {
            // SGEs are little-endian so no need for .Reverse() here
            SgeStartOffset = BitConverter.ToInt32(data.Skip(0x1C).Take(4).ToArray());
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

                    List<(int numVertices, int startAddress)> vertexTables = new();
                    List<(int numFaces, int startAddress)> faceTables = new();

                    for (int i = 0; i < 2; i++)
                    {
                        int vertexTableAddress = BitConverter.ToInt32(sgeData.Skip(meshTableEntry.VertexAddress + i * 4).Take(4).ToArray());
                        int numVertices = BitConverter.ToInt32(sgeData.Skip(vertexTableAddress).Take(4).ToArray());
                        int vertexStartAddress = BitConverter.ToInt32(sgeData.Skip(vertexTableAddress + 0x04).Take(4).ToArray());
                        if (numVertices == vertexStartAddress) // tell tale sign that we're actually at the end of the file
                        {
                            break;
                        }
                        vertexTables.Add((numVertices, vertexStartAddress));
                        int numFaces = BitConverter.ToInt32(sgeData.Skip(vertexTableAddress + 0x08).Take(4).ToArray());
                        int facesStartAddress = BitConverter.ToInt32(sgeData.Skip(vertexTableAddress + 0x0C).Take(4).ToArray());
                        if (facesStartAddress == 0) // another sign that things are amiss
                        {
                            vertexTables.RemoveAt(1);
                            break;
                        }
                        faceTables.Add((numFaces, facesStartAddress));
                    }
                    foreach ((int numVertices, int startAddress) in vertexTables)
                    {
                        for (int i = 0; i < numVertices; i++)
                        {
                            SgeVertices.Add(new(sgeData.Skip(startAddress + i * 0x38).Take(0x38)));
                        }
                    }
                    List<int> combinedFaceTable = new List<int>();
                    foreach ((int numFaces, int startAddress) in faceTables)
                    {
                        for (int i = 0; i < numFaces; i++)
                        {
                            combinedFaceTable.Add(BitConverter.ToInt32(sgeData.Skip(startAddress + i * 4).Take(4).ToArray()));
                        }
                    }

                    bool triStripped = SgeHeader.ModelType == 4;
                    int faceIndex = 0;
                    foreach (SgeSubmesh submesh in SgeSubmeshes)
                    {
                        for (int i = 0; i < submesh.EndFace;)
                        {
                            if (triStripped)
                            {
                                SgeFaces.Add(new(combinedFaceTable[faceIndex], combinedFaceTable[faceIndex + 1], combinedFaceTable[faceIndex + 2], submesh.Material, submesh.StartVertex, faceIndex & 1));
                                i++;
                                faceIndex++;
                            }
                            else
                            {
                                SgeFaces.Add(new(combinedFaceTable[faceIndex + 2], combinedFaceTable[faceIndex + 1], combinedFaceTable[faceIndex], submesh.Material, submesh.StartVertex));
                                i += 3;
                                faceIndex += 3;
                            }

                            IEnumerable<(int, SgeBone, float)> attachedBones = SgeFaces.Last().Polygon.SelectMany(v => SgeVertices[v].BoneIds.Select(b =>
                            {
                                int boneIndex = submesh.BonePalette[b];
                                if (boneIndex >= 0)
                                {
                                    return (v, SgeBones[boneIndex], SgeVertices[v].Weight[Array.IndexOf(SgeVertices[v].BoneIds, b)]);
                                }
                                return (v, null, 0);
                            }));
                            foreach ((int vertexIndex, SgeBone bone, float weight) in attachedBones)
                            {
                                if (bone is not null)
                                {
                                    if (!bone.VertexGroup.ContainsKey(vertexIndex) && weight > 0)
                                    {
                                        bone.VertexGroup.Add(vertexIndex, weight);
                                    }
                                }
                            }
                        }
                        faceIndex += 2; // Increment to avoid reusing indices from the previous submesh
                    }
                }
            }
        }

        public void ResolveTextures(string name, List<GraphicsFile> textures)
        {
            Name = name;
            foreach (SgeMaterial texture in SgeMaterials)
            {
                texture.Texture = textures.FirstOrDefault(t => t.Name == texture.Name);
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

            using StringWriter stringWriter = new();
            JsonSerializer serializer = new();
            serializer.Serialize(stringWriter, this);

            return stringWriter.ToString();
        }
    }

    public class SgeHeader
    {
        public short Version { get; set; }    // 1
        public short ModelType { get; set; }    // 2 -- value of 0, 3, 4 or 5; of 3 gives outline on character model
        public int Unknown04 { get; set; }      // 3
        public int Unknown08 { get; set; }      // 4
        public int Unknown0C { get; set; }   // 5
        public int BonesCount { get; set; }     // 6
        public int TexturesCount { get; set; }  // 7
        public int Unknown18 { get; set; }      // 8
        public int Unknown1C { get; set; }      // 9
        public int Unknown20 { get; set; }      // 10
        public int Unknown24 { get; set; }      // 11
        public int Unknown28 { get; set; }      // 12
        public int Unknown2C { get; set; }      // 13
        public int MeshTableAddress { get; set; } // 14
        public int Unknown34 { get; set; }      // 15
        public int Unknown38 { get; set; }      // 16
        public int Unknown3C { get; set; }      // 17
        public int Unknown40 { get; set; }      // 18
        public int BonesTableAddress { get; set; }  // 19
        public int TextureTableAddress { get; set; }    // 20
        public int Unknown4C { get; set; }      // 21
        public int Unknown50 { get; set; }      // 22
        public int Unknown54 { get; set; }      // 23
        public int Unknown58 { get; set; }      // 24
        public int Unknown5C { get; set; }      // 25
        public int AnimationDataTableAddress { get; set; }      // 26
        public int AnimationActionTableAddress { get; set; }      // 27
        public int Unknown68 { get; set; }      // 28
        public int Unknown6C { get; set; }      // 29
        public int Unknown70 { get; set; }      // 30
        public int Unknown74 { get; set; }      // 31
        public int Unknown78 { get; set; }      // 32
        public int Unknown7C { get; set; }      // 33

        public SgeHeader(IEnumerable<byte> headerData)
        {
            Version = BitConverter.ToInt16(headerData.Take(2).ToArray());
            ModelType = BitConverter.ToInt16(headerData.Skip(0x02).Take(2).ToArray());
            Unknown04 = BitConverter.ToInt32(headerData.Skip(0x04).Take(4).ToArray());
            Unknown08 = BitConverter.ToInt32(headerData.Skip(0x08).Take(4).ToArray());
            Unknown0C = BitConverter.ToInt32(headerData.Skip(0x0C).Take(4).ToArray());
            BonesCount = BitConverter.ToInt32(headerData.Skip(0x10).Take(4).ToArray());
            TexturesCount = BitConverter.ToInt32(headerData.Skip(0x14).Take(4).ToArray());
            Unknown18 = BitConverter.ToInt32(headerData.Skip(0x18).Take(4).ToArray());
            Unknown1C = BitConverter.ToInt32(headerData.Skip(0x1C).Take(4).ToArray());
            Unknown20 = BitConverter.ToInt32(headerData.Skip(0x20).Take(4).ToArray());
            Unknown24 = BitConverter.ToInt32(headerData.Skip(0x24).Take(4).ToArray());
            Unknown28 = BitConverter.ToInt32(headerData.Skip(0x28).Take(4).ToArray());
            Unknown2C = BitConverter.ToInt32(headerData.Skip(0x2C).Take(4).ToArray());
            MeshTableAddress = BitConverter.ToInt32(headerData.Skip(0x30).Take(4).ToArray());
            Unknown34 = BitConverter.ToInt32(headerData.Skip(0x34).Take(4).ToArray());
            Unknown38 = BitConverter.ToInt32(headerData.Skip(0x38).Take(4).ToArray());
            Unknown3C = BitConverter.ToInt32(headerData.Skip(0x3C).Take(4).ToArray());
            Unknown40 = BitConverter.ToInt32(headerData.Skip(0x40).Take(4).ToArray());
            BonesTableAddress = BitConverter.ToInt32(headerData.Skip(0x44).Take(4).ToArray());
            TextureTableAddress = BitConverter.ToInt32(headerData.Skip(0x48).Take(4).ToArray());
            Unknown4C = BitConverter.ToInt32(headerData.Skip(0x4C).Take(4).ToArray());
            Unknown50 = BitConverter.ToInt32(headerData.Skip(0x50).Take(4).ToArray());
            Unknown54 = BitConverter.ToInt32(headerData.Skip(0x54).Take(4).ToArray());
            Unknown58 = BitConverter.ToInt32(headerData.Skip(0x58).Take(4).ToArray());
            Unknown5C = BitConverter.ToInt32(headerData.Skip(0x5C).Take(4).ToArray());
            AnimationDataTableAddress = BitConverter.ToInt32(headerData.Skip(0x60).Take(4).ToArray());
            AnimationActionTableAddress = BitConverter.ToInt32(headerData.Skip(0x64).Take(4).ToArray());
            Unknown68 = BitConverter.ToInt32(headerData.Skip(0x68).Take(4).ToArray());
            Unknown6C = BitConverter.ToInt32(headerData.Skip(0x6C).Take(4).ToArray());
            Unknown70 = BitConverter.ToInt32(headerData.Skip(0x70).Take(4).ToArray());
            Unknown74 = BitConverter.ToInt32(headerData.Skip(0x74).Take(4).ToArray());
            Unknown78 = BitConverter.ToInt32(headerData.Skip(0x78).Take(4).ToArray());
            Unknown7C = BitConverter.ToInt32(headerData.Skip(0x7C).Take(4).ToArray());
        }
    }

    public class AnimationTableEntry
    {
        public float Unknown00 { get; set; }
        public int Unknown04 { get; set; }
        public int AnimationAddress { get; set; }
        public AnimationData Animation { get; set; }
        public int Unknown0C { get; set; }
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

        public AnimationTableEntry(IEnumerable<byte> data, int offset)
        {
            Unknown00 = BitConverter.ToSingle(data.Skip(0x00 + offset).Take(4).ToArray());
            Unknown04 = BitConverter.ToInt32(data.Skip(0x04 + offset).Take(4).ToArray());
            AnimationAddress = BitConverter.ToInt32(data.Skip(0x08 + offset).Take(4).ToArray());

            Unknown0C = BitConverter.ToInt32(data.Skip(0x0C + offset).Take(4).ToArray());
            Unknown10 = BitConverter.ToInt32(data.Skip(0x10 + offset).Take(4).ToArray());
            Unknown14 = BitConverter.ToInt32(data.Skip(0x14 + offset).Take(4).ToArray());
            Unknown18 = BitConverter.ToInt32(data.Skip(0x18 + offset).Take(4).ToArray());
            Unknown1C = BitConverter.ToInt32(data.Skip(0x1C + offset).Take(4).ToArray());
            Unknown20 = BitConverter.ToInt32(data.Skip(0x20 + offset).Take(4).ToArray());
            Unknown24 = BitConverter.ToInt32(data.Skip(0x24 + offset).Take(4).ToArray());
            Unknown28 = BitConverter.ToInt32(data.Skip(0x28 + offset).Take(4).ToArray());
            Unknown2C = BitConverter.ToInt32(data.Skip(0x2C + offset).Take(4).ToArray());
            Unknown30 = BitConverter.ToInt32(data.Skip(0x30 + offset).Take(4).ToArray());
            Unknown34 = BitConverter.ToInt32(data.Skip(0x34 + offset).Take(4).ToArray());
        }
    }

    public class AnimationData
    {

    }

    public class AnimationAction
    {

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
            Unknown00 = BitConverter.ToInt32(data.Skip(offset).Take(4).ToArray());
            Unknown04 = BitConverter.ToInt32(data.Skip(offset + 0x04).Take(4).ToArray());
            SubmeshAddress = BitConverter.ToInt32(data.Skip(offset + 0x08).Take(4).ToArray());
            SubmeshCount = BitConverter.ToInt32(data.Skip(offset + 0x0C).Take(4).ToArray());
            Unknown10 = BitConverter.ToInt32(data.Skip(offset + 0x10).Take(4).ToArray());
            VertexAddress = BitConverter.ToInt32(data.Skip(offset + 0x14).Take(4).ToArray());
            Unknown18 = BitConverter.ToInt32(data.Skip(offset + 0x18).Take(4).ToArray());
            Unknown1C = BitConverter.ToInt32(data.Skip(offset + 0x1C).Take(4).ToArray());
            Unknown20 = BitConverter.ToInt32(data.Skip(offset + 0x20).Take(4).ToArray());
            Unknown24 = BitConverter.ToSingle(data.Skip(offset + 0x24).Take(4).ToArray());
            Unknown28 = BitConverter.ToSingle(data.Skip(offset + 0x28).Take(4).ToArray());
            Unknown2C = BitConverter.ToSingle(data.Skip(offset + 0x2C).Take(4).ToArray());
            Unknown30 = BitConverter.ToInt32(data.Skip(offset + 0x30).Take(4).ToArray());
            Unknown34 = BitConverter.ToInt32(data.Skip(offset + 0x34).Take(4).ToArray());
            Unknown38 = BitConverter.ToSingle(data.Skip(offset + 0x38).Take(4).ToArray());
            Unknown3C = BitConverter.ToSingle(data.Skip(offset + 0x3C).Take(4).ToArray());
            Unknown40 = BitConverter.ToSingle(data.Skip(offset + 0x40).Take(4).ToArray());
        }
    }

    public class SgeBone
    {
        [JsonIgnore]
        public SgeBone Parent { get; set; }
        public int Address { get; set; }
        public Vector3 Unknown00 { get; set; }      // 1
        public Vector3 Position { get; set; } 
        public int ParentAddress { get; set; }
        public int AddressToBone1 { get; set; }     // 4
        public int AddressToBone2 { get; set; }     // 5
        public int Count { get; set; }              // 6

        public Dictionary<int, float> VertexGroup { get; set; } = new();

        public SgeBone(IEnumerable<byte> data, int offset)
        {
            Address = offset;
            Unknown00 = new Vector3(
                BitConverter.ToSingle(data.Skip(offset).Take(4).ToArray()),
                BitConverter.ToSingle(data.Skip(offset + 0x04).Take(4).ToArray()),
                BitConverter.ToSingle(data.Skip(offset + 0x08).Take(4).ToArray()));
            Position = new Vector3(
                BitConverter.ToSingle(data.Skip(offset + 0x0C).Take(4).ToArray()),
                BitConverter.ToSingle(data.Skip(offset + 0x10).Take(4).ToArray()),
                BitConverter.ToSingle(data.Skip(offset + 0x14).Take(4).ToArray())) * Sge.MODEL_SCALE;
            ParentAddress = BitConverter.ToInt32(data.Skip(offset + 0x18).Take(4).ToArray());
            AddressToBone1 = BitConverter.ToInt32(data.Skip(offset + 0x1C).Take(4).ToArray());
            AddressToBone2 = BitConverter.ToInt32(data.Skip(offset + 0x20).Take(4).ToArray());
            Count = BitConverter.ToInt32(data.Skip(offset + 0x24).Take(4).ToArray());
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

    public class SgeSubmesh
    {
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
        public int EndFace { get; set; }
        public List<short> BonePalette { get; set; } = new();
        public float Unknown54 { get; set; }          // 23
        public float Unknown58 { get; set; }          // 24
        public float Unknown5C { get; set; }          // 25
        public float Unknown60 { get; set; }          // 26

        public SgeSubmesh(IEnumerable<byte> data, int offset, List<SgeMaterial> materials, List<SgeBone> bones)
        {
            Unknown00 = BitConverter.ToInt32(data.Skip(offset).Take(4).ToArray());
            Unknown04 = BitConverter.ToInt32(data.Skip(offset + 0x04).Take(4).ToArray());
            MaterialStringAddress = BitConverter.ToInt32(data.Skip(offset + 0x08).Take(4).ToArray());
            string materialString = Encoding.ASCII.GetString(data.Skip(MaterialStringAddress).TakeWhile(b => b != 0x00).ToArray());
            Material = materials.FirstOrDefault(m => m.Name == materialString);
            Unknown0C = BitConverter.ToInt32(data.Skip(offset + 0x0C).Take(4).ToArray());
            Unknown10 = BitConverter.ToInt32(data.Skip(offset + 0x10).Take(4).ToArray());
            BoneAddress = BitConverter.ToInt32(data.Skip(offset + 0x14).Take(4).ToArray());
            Bone = bones.FirstOrDefault(b => b.Address == BoneAddress);
            Unknown18 = BitConverter.ToInt32(data.Skip(offset + 0x18).Take(4).ToArray());
            Unknown1C = BitConverter.ToInt32(data.Skip(offset + 0x1C).Take(4).ToArray());
            Unknown20 = BitConverter.ToInt32(data.Skip(offset + 0x20).Take(4).ToArray());
            StartVertex = BitConverter.ToInt32(data.Skip(offset + 0x24).Take(4).ToArray());
            EndVertex = BitConverter.ToInt32(data.Skip(offset + 0x28).Take(4).ToArray());
            StartFace = BitConverter.ToInt32(data.Skip(offset + 0x2C).Take(4).ToArray());
            EndFace = BitConverter.ToInt32(data.Skip(offset + 0x30).Take(4).ToArray());
            for (int i = 0; i < 16; i++)
            {
                BonePalette.Add(BitConverter.ToInt16(data.Skip(offset + 0x34 + i * 2).Take(2).ToArray()));
            }
            Unknown54 = BitConverter.ToSingle(data.Skip(offset + 0x54).Take(4).ToArray());
            Unknown58 = BitConverter.ToSingle(data.Skip(offset + 0x58).Take(4).ToArray());
            Unknown5C = BitConverter.ToSingle(data.Skip(offset + 0x5C).Take(4).ToArray());
            Unknown60 = BitConverter.ToSingle(data.Skip(offset + 0x60).Take(4).ToArray());
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
            Position = new Vector3(BitConverter.ToSingle(data.Skip(0x00).Take(4).ToArray()), BitConverter.ToSingle(data.Skip(0x04).Take(4).ToArray()), BitConverter.ToSingle(data.Skip(0x08).Take(4).ToArray())) * Sge.MODEL_SCALE;
            float weight1 = BitConverter.ToSingle(data.Skip(0x0C).Take(4).ToArray());
            float weight2 = BitConverter.ToSingle(data.Skip(0x10).Take(4).ToArray());
            float weight3 = BitConverter.ToSingle(data.Skip(0x14).Take(4).ToArray());
            Weight = new float[] { weight1, weight2, weight3, 1 - (weight1 + weight2 + weight3) };
            BoneIds = new byte[] { data.ElementAt(0x18), data.ElementAt(0x19), data.ElementAt(0x1A), data.ElementAt(0x1B) };
            Normal = new Vector3(BitConverter.ToSingle(data.Skip(0x1C).Take(4).ToArray()), BitConverter.ToSingle(data.Skip(0x20).Take(4).ToArray()), BitConverter.ToSingle(data.Skip(0x24).Take(4).ToArray()));
            int color = BitConverter.ToInt32(data.Skip(0x28).Take(4).ToArray());
            Color = new VertexColor(((color & 0x00FF0000) >> 16) / 255.0f, ((color & 0x0000FF00) >> 8) / 255.0f, (color & 0x000000FF) / 255.0f, ((color & 0xFF000000) >> 24) / 255.0f);
            UVCoords = new Vector2(BitConverter.ToSingle(data.Skip(0x2C).Take(4).ToArray()), BitConverter.ToSingle(data.Skip(0x30).Take(4).ToArray()));
            Unknown2 = BitConverter.ToInt32(data.Skip(0x34).Take(4).ToArray());
        }
    }

    public class SgeFace
    {
        public List<int> Polygon { get; set; }
        public SgeMaterial Material { get; set; }
        
        public SgeFace(int first, int second, int third, SgeMaterial material, int faceOffset, int evenOdd = 0)
        {
            if (evenOdd == 0)
            {
                Polygon = new List<int> { first + faceOffset, second + faceOffset, third + faceOffset };
            }
            else
            {
                Polygon = new List<int> { second + faceOffset, first + faceOffset, third + faceOffset };
            }
            Material = material;
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
    }
}
