using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiHeiretsuLib.Graphics
{
    public partial class GraphicsFile
    {
        public SgeModel SgeModel { get; set; }
    }

    public class SgeModel
    {
        public int SgeStartOffset { get; set; }
        public SgeHeader SgeHeader { get; set; }
        public List<SgeTexture> SgeTextures { get; set; } = new();

        public SgeModel(IEnumerable<byte> data)
        {
            // SGEs are little-endian so no need for .Reverse() here
            SgeStartOffset = BitConverter.ToInt32(data.Skip(0x1C).Take(4).ToArray());
            IEnumerable<byte> sgeData = data.Skip(SgeStartOffset);
            SgeHeader = new(sgeData.Take(0x80));
            for (int i = 0; i < SgeHeader.TexturesCount; i++)
            {
                int nameOffset = SgeHeader.TextureTableAddress + i * 0x18;
                SgeTextures.Add(new(i, Encoding.ASCII.GetString(sgeData.Skip(nameOffset).TakeWhile(b => b != 0x00).ToArray())));
            }
        }

        public void ResolveTextures(List<GraphicsFile> textures)
        {
            foreach (SgeTexture texture in SgeTextures)
            {
                texture.File = textures.FirstOrDefault(t => t.Name == texture.Name);
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
        public int BonesCount { get; set; }       // 6
        public int TexturesCount { get; set; }    // 7
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
            Unknown04 = BitConverter.ToInt16(headerData.Skip(0x04).Take(4).ToArray());
            Unknown08 = BitConverter.ToInt16(headerData.Skip(0x08).Take(4).ToArray());
            SubmeshCount = BitConverter.ToInt16(headerData.Skip(0x0C).Take(4).ToArray());
            BonesCount = BitConverter.ToInt16(headerData.Skip(0x10).Take(4).ToArray());
            TexturesCount = BitConverter.ToInt16(headerData.Skip(0x14).Take(4).ToArray());
            Unknown18 = BitConverter.ToInt16(headerData.Skip(0x18).Take(4).ToArray());
            Unknown1C = BitConverter.ToInt16(headerData.Skip(0x1C).Take(4).ToArray());
            Unknown20 = BitConverter.ToInt16(headerData.Skip(0x20).Take(4).ToArray());
            Unknown24 = BitConverter.ToInt16(headerData.Skip(0x24).Take(4).ToArray());
            Unknown2C = BitConverter.ToInt16(headerData.Skip(0x2C).Take(4).ToArray());
            SubmeshTableAddress = BitConverter.ToInt16(headerData.Skip(0x30).Take(4).ToArray());
            Unknown34 = BitConverter.ToInt16(headerData.Skip(0x34).Take(4).ToArray());
            Unknown38 = BitConverter.ToInt16(headerData.Skip(0x38).Take(4).ToArray());
            Unknown3C = BitConverter.ToInt16(headerData.Skip(0x3C).Take(4).ToArray());
            Unknown40 = BitConverter.ToInt16(headerData.Skip(0x40).Take(4).ToArray());
            BonesTableAddress = BitConverter.ToInt16(headerData.Skip(0x44).Take(4).ToArray());
            TextureTableAddress = BitConverter.ToInt16(headerData.Skip(0x48).Take(4).ToArray());
            Unknown4C = BitConverter.ToInt16(headerData.Skip(0x4C).Take(4).ToArray());
            Unknown50 = BitConverter.ToInt16(headerData.Skip(0x50).Take(4).ToArray());
            Unknown54 = BitConverter.ToInt16(headerData.Skip(0x54).Take(4).ToArray());
            Unknown58 = BitConverter.ToInt16(headerData.Skip(0x58).Take(4).ToArray());
            Unknown5C = BitConverter.ToInt16(headerData.Skip(0x5C).Take(4).ToArray());
            Unknown60 = BitConverter.ToInt16(headerData.Skip(0x60).Take(4).ToArray());
            Unknown64 = BitConverter.ToInt16(headerData.Skip(0x64).Take(4).ToArray());
            Unknown68 = BitConverter.ToInt16(headerData.Skip(0x68).Take(4).ToArray());
            Unknown6C = BitConverter.ToInt16(headerData.Skip(0x6C).Take(4).ToArray());
            Unknown70 = BitConverter.ToInt16(headerData.Skip(0x70).Take(4).ToArray());
            Unknown74 = BitConverter.ToInt16(headerData.Skip(0x74).Take(4).ToArray());
            Unknown78 = BitConverter.ToInt16(headerData.Skip(0x78).Take(4).ToArray());
            Unknown7C = BitConverter.ToInt16(headerData.Skip(0x7C).Take(4).ToArray());
        }
    }

    public class SgeBone
    {

    }

    public class SgeSubmesh
    {

    }

    public class SgeTexture
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public GraphicsFile File { get; set; }

        public SgeTexture(int index, string name)
        {
            Index = index;
            Name = name;
        }
    }
}
