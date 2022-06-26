using Assimp;
using HaruhiHeiretsuLib.Graphics.Renderer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace HaruhiHeiretsuLib.Graphics
{
    public partial class GraphicsFile
    {
        public SgeModel SgeModel { get; set; }
    }

    public class SgeModel
    {
        public const float MODEL_SCALE = 25.4f;
        public string Name { get; set; }
        public int SgeStartOffset { get; set; }
        public SgeHeader SgeHeader { get; set; }
        public List<SgeSubmeshTableEntry> SubmeshTableEntries { get; set; } = new();
        public List<SgeMaterial> SgeMaterials { get; set; } = new();
        public List<SgeBone> SgeBones { get; set; } = new();
        public List<SgeVertex> SgeVertices { get; set; } = new();
        public List<SgeMesh> SgeMeshes { get; set; } = new();
        public List<SgeFace> SgeFaces { get; set; } = new();

        public SgeModel(IEnumerable<byte> data)
        {
            // SGEs are little-endian so no need for .Reverse() here
            SgeStartOffset = BitConverter.ToInt32(data.Skip(0x1C).Take(4).ToArray());
            IEnumerable<byte> sgeData = data.Skip(SgeStartOffset);
            SgeHeader = new(sgeData.Take(0x80));
            for (int i = 0; i < SgeHeader.SubmeshCount; i++)
            {
                SubmeshTableEntries.Add(new(sgeData, SgeHeader.SubmeshTableAddress + 0x44 * i));
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
            foreach (SgeSubmeshTableEntry submeshTableEntry in SubmeshTableEntries)
            {
                if (submeshTableEntry.Address > 0 && submeshTableEntry.Address % 4 == 0 && submeshTableEntry.SubmeshCount > 0 && submeshTableEntry.VertexAddress > 0)
                {
                    for (int i = 0; i < submeshTableEntry.SubmeshCount; i++)
                    {
                        SgeMeshes.Add(new(sgeData, submeshTableEntry.Address + i * 0x64, SgeMaterials, SgeBones));
                    }

                    int vertexTableAddress = BitConverter.ToInt32(sgeData.Skip(submeshTableEntry.VertexAddress).Take(4).ToArray());
                    int numVertices = BitConverter.ToInt32(sgeData.Skip(vertexTableAddress).Take(4).ToArray());
                    int vertexStartAddress = BitConverter.ToInt32(sgeData.Skip(vertexTableAddress + 0x04).Take(4).ToArray());
                    int numFaces = BitConverter.ToInt32(sgeData.Skip(vertexTableAddress + 0x08).Take(4).ToArray());
                    int facesStartAddress = BitConverter.ToInt32(sgeData.Skip(vertexTableAddress + 0x0C).Take(4).ToArray());

                    for (int i = 0; i < numVertices; i++)
                    {
                        SgeVertices.Add(new(sgeData.Skip(vertexStartAddress + i * 0x38).Take(0x38)));
                    }

                    int faceIndex = 0;
                    foreach (SgeMesh mesh in SgeMeshes)
                    {
                        for (int i = 0; i < mesh.NumFaces && faceIndex < numFaces / 3; i++)
                        {
                            SgeFaces.Add(new(sgeData.Skip(facesStartAddress + faceIndex++ * 0x0C).Take(0x0C), mesh.Material));
                        }
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

        public string GetCollada()
        {
            SgeColladaRenderer renderer = new() {  Sge = this };
            using MemoryStream stream = renderer.Render();
            string text = Encoding.UTF8.GetString(stream.ToArray());
            return text;
        }

        public void Test(string file)
        {
            AssimpContext context = new();
            if (file.EndsWith("fbx"))
            {
                context.ConvertFromFileToFile(file, $"{file}.dae", "collada");
            }
            else
            {
                string fileText = Regex.Replace(File.ReadAllText(file), @"<((\w+)( .+)?)/>", "<$1></$2>");
                string tempFile = Path.GetTempFileName();
                File.WriteAllText(tempFile, fileText);
                try
                {
                    Scene scene = context.ImportFile(tempFile);
                    context.ConvertFromFileToFile(tempFile, $"{file}.fbx", "fbx");
                }
                finally
                {
                    File.Delete(tempFile);
                }
            }
        }
    }

    public class SgeHeader
    {
        public short Unknown00 { get; set; }    // 1
        public short Unknown02 { get; set; }    // 2
        public int Unknown04 { get; set; }      // 3
        public int Unknown08 { get; set; }      // 4
        public int SubmeshCount { get; set; }   // 5
        public int BonesCount { get; set; }     // 6
        public int TexturesCount { get; set; }  // 7
        public int Unknown18 { get; set; }      // 8
        public int Unknown1C { get; set; }      // 9
        public int Unknown20 { get; set; }      // 10
        public int Unknown24 { get; set; }      // 11
        public int Unknown28 { get; set; }      // 12
        public int Unknown2C { get; set; }      // 13
        public int SubmeshTableAddress { get; set; } // 14
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
        public int Unknown60 { get; set; }      // 26
        public int Unknown64 { get; set; }      // 27
        public int Unknown68 { get; set; }      // 28
        public int Unknown6C { get; set; }      // 29
        public int Unknown70 { get; set; }      // 30
        public int Unknown74 { get; set; }      // 31
        public int Unknown78 { get; set; }      // 32
        public int Unknown7C { get; set; }      // 33

        public SgeHeader(IEnumerable<byte> headerData)
        {
            Unknown00 = BitConverter.ToInt16(headerData.Take(2).ToArray());
            Unknown02 = BitConverter.ToInt16(headerData.Skip(0x02).Take(2).ToArray());
            Unknown04 = BitConverter.ToInt32(headerData.Skip(0x04).Take(4).ToArray());
            Unknown08 = BitConverter.ToInt32(headerData.Skip(0x08).Take(4).ToArray());
            SubmeshCount = BitConverter.ToInt32(headerData.Skip(0x0C).Take(4).ToArray());
            BonesCount = BitConverter.ToInt32(headerData.Skip(0x10).Take(4).ToArray());
            TexturesCount = BitConverter.ToInt32(headerData.Skip(0x14).Take(4).ToArray());
            Unknown18 = BitConverter.ToInt32(headerData.Skip(0x18).Take(4).ToArray());
            Unknown1C = BitConverter.ToInt32(headerData.Skip(0x1C).Take(4).ToArray());
            Unknown20 = BitConverter.ToInt32(headerData.Skip(0x20).Take(4).ToArray());
            Unknown24 = BitConverter.ToInt32(headerData.Skip(0x24).Take(4).ToArray());
            Unknown28 = BitConverter.ToInt32(headerData.Skip(0x28).Take(4).ToArray());
            Unknown2C = BitConverter.ToInt32(headerData.Skip(0x2C).Take(4).ToArray());
            SubmeshTableAddress = BitConverter.ToInt32(headerData.Skip(0x30).Take(4).ToArray());
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
            Unknown60 = BitConverter.ToInt32(headerData.Skip(0x60).Take(4).ToArray());
            Unknown64 = BitConverter.ToInt32(headerData.Skip(0x64).Take(4).ToArray());
            Unknown68 = BitConverter.ToInt32(headerData.Skip(0x68).Take(4).ToArray());
            Unknown6C = BitConverter.ToInt32(headerData.Skip(0x6C).Take(4).ToArray());
            Unknown70 = BitConverter.ToInt32(headerData.Skip(0x70).Take(4).ToArray());
            Unknown74 = BitConverter.ToInt32(headerData.Skip(0x74).Take(4).ToArray());
            Unknown78 = BitConverter.ToInt32(headerData.Skip(0x78).Take(4).ToArray());
            Unknown7C = BitConverter.ToInt32(headerData.Skip(0x7C).Take(4).ToArray());
        }
    }

    public class SgeSubmeshTableEntry
    {
        public int Unknown00 { get; set; }          // 1
        public int Unknown04 { get; set; }          // 2
        public int Address { get; set; }            // 3
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

        public SgeSubmeshTableEntry(IEnumerable<byte> data, int offset)
        {
            Unknown00 = BitConverter.ToInt32(data.Skip(offset).Take(4).ToArray());
            Unknown04 = BitConverter.ToInt32(data.Skip(offset + 0x04).Take(4).ToArray());
            Address = BitConverter.ToInt32(data.Skip(offset + 0x08).Take(4).ToArray());
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
        public SgeBone Parent { get; set; }
        public int Address { get; set; }
        public Vector3 Unknown00 { get; set; }      // 1
        public Vector3 Position { get; set; } 
        public int ParentAddress { get; set; }
        public SgeBone Bone1 { get; set; }
        public int AddressToBone1 { get; set; }     // 4
        public SgeBone Bone2 { get; set; }
        public int AddressToBone2 { get; set; }     // 5
        public int Count { get; set; }              // 6

        public SgeBone(IEnumerable<byte> data, int offset)
        {
            Address = offset;
            Unknown00 = new Vector3(
                BitConverter.ToSingle(data.Skip(offset).Take(4).ToArray()),
                BitConverter.ToSingle(data.Skip(offset + 0x04).Take(4).ToArray()),
                BitConverter.ToSingle(data.Skip(offset + 0x08).Take(4).ToArray()));
            Position = new Vector3(
                BitConverter.ToSingle(data.Skip(offset + 0x0C).Take(4).ToArray()) * SgeModel.MODEL_SCALE,
                BitConverter.ToSingle(data.Skip(offset + 0x10).Take(4).ToArray()) * SgeModel.MODEL_SCALE,
                BitConverter.ToSingle(data.Skip(offset + 0x14).Take(4).ToArray())) * SgeModel.MODEL_SCALE;
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
            if (AddressToBone1 != 0)
            {
                Bone1 = bones.First(b => b.Address == AddressToBone1);
            }
            if (AddressToBone2 != 0)
            {
                Bone2 = bones.First(b => b.Address == AddressToBone2);
            }
        }
    }

    public class SgeMesh
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
        public int Count { get; set; }              // 11
        public int FaceOffset { get; set; }
        public int VerticesCount { get; set; }      // 12
        public int Count2 { get; set; }             // 13
        public int NumFaces { get; set; }
        public List<short> BonePalette { get; set; } = new();
        public int Unknown54 { get; set; }          // 23
        public int Unknown58 { get; set; }          // 24
        public int Unknown5C { get; set; }          // 25
        public int Unknown60 { get; set; }          // 26

        public SgeMesh(IEnumerable<byte> data, int offset, List<SgeMaterial> materials, List<SgeBone> bones)
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
            Count = BitConverter.ToInt32(data.Skip(offset + 0x20).Take(4).ToArray());
            FaceOffset = BitConverter.ToInt32(data.Skip(offset + 0x24).Take(4).ToArray());
            VerticesCount = BitConverter.ToInt32(data.Skip(offset + 0x28).Take(4).ToArray());
            Count2 = BitConverter.ToInt32(data.Skip(offset + 0x2C).Take(4).ToArray());
            NumFaces = BitConverter.ToInt32(data.Skip(offset + 0x30).Take(4).ToArray());
            for (int i = 0; i < 16; i++)
            {
                BonePalette.Add(BitConverter.ToInt16(data.Skip(offset + 0x34 + i * 2).Take(2).ToArray()));
            }
            Unknown54 = BitConverter.ToInt32(data.Skip(offset + 0x54).Take(4).ToArray());
            Unknown5C = BitConverter.ToInt32(data.Skip(offset + 0x58).Take(4).ToArray());
            Unknown60 = BitConverter.ToInt32(data.Skip(offset + 0x5C).Take(4).ToArray());
        }
    }

    public class SgeVertex
    {
        public Vector3 Position { get; set; }
        public Vector4 Weight { get; set; }
        public byte[] BoneIds { get; set; }
        public Vector3 Normal { get; set; }
        public int Unknown { get; set; } // color maybe
        public Vector2 UVCoords { get; set; }
        public int Unknown2 { get; set; }

        public SgeVertex(IEnumerable<byte> data)
        {
            Position = new Vector3(BitConverter.ToSingle(data.Skip(0x00).Take(4).ToArray()), BitConverter.ToSingle(data.Skip(0x04).Take(4).ToArray()), BitConverter.ToSingle(data.Skip(0x08).Take(4).ToArray())) * SgeModel.MODEL_SCALE;
            float weightX = BitConverter.ToSingle(data.Skip(0x0C).Take(4).ToArray());
            float weightY = BitConverter.ToSingle(data.Skip(0x10).Take(4).ToArray());
            float weightZ = BitConverter.ToSingle(data.Skip(0x14).Take(4).ToArray());
            Weight = new Vector4(weightX, weightY, weightZ, 1 - (weightX + weightY + weightZ));
            BoneIds = new byte[] { data.ElementAt(0x18), data.ElementAt(0x19), data.ElementAt(0x1A), data.ElementAt(0x1B) };
            Normal = new Vector3(BitConverter.ToSingle(data.Skip(0x1C).Take(4).ToArray()), BitConverter.ToSingle(data.Skip(0x20).Take(4).ToArray()), BitConverter.ToSingle(data.Skip(0x24).Take(4).ToArray()));
            Unknown = BitConverter.ToInt32(data.Skip(0x28).Take(4).ToArray());
            UVCoords = new Vector2(BitConverter.ToSingle(data.Skip(0x2C).Take(4).ToArray()), BitConverter.ToSingle(data.Skip(0x30).Take(4).ToArray()));
            Unknown2 = BitConverter.ToInt32(data.Skip(0x34).Take(4).ToArray());
        }
    }

    public class SgeFace
    {
        public List<int> Polygon { get; set; }
        public SgeMaterial Material { get; set; }
        
        public SgeFace(IEnumerable<byte> data, SgeMaterial material)
        {
            Polygon = new List<int> { BitConverter.ToInt32(data.Skip(0x00).Take(4).ToArray()), BitConverter.ToInt32(data.Skip(0x04).Take(4).ToArray()), BitConverter.ToInt32(data.Skip(0x08).Take(4).ToArray()) };
            Material = material;
        }
    }

    public class SgeMaterial
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public GraphicsFile Texture { get; set; }

        public SgeMaterial(int index, string name)
        {
            Index = index;
            Name = name;
        }

        public Material GetMaterial(string texturesDirectory)
        {
            string textureFileName = Path.Combine(texturesDirectory, $"{Name}.png");
            if (!File.Exists(textureFileName))
            {
                File.WriteAllBytes(textureFileName, Texture.GetImage().Bytes);
            }
            Material material = new() { TextureDiffuse = new TextureSlot(textureFileName, TextureType.Diffuse, Index, TextureMapping.FromUV, Index, 1.0f, TextureOperation.Multiply, TextureWrapMode.Wrap, TextureWrapMode.Wrap, 0) };

            return material;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
