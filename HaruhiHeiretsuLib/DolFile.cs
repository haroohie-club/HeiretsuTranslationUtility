using HaruhiHeiretsuLib.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HaruhiHeiretsuLib
{
    public class DolFile
    {
        public List<GraphicsFile> GraphicsFiles { get; set; } = [];

        public DolFile(byte[] dolBytes)
        {
            for (int i = 0; i < dolBytes.Length - 4; i++)
            {
                if (dolBytes.Skip(i).Take(4).SequenceEqual(new byte[] { 0x00, 0x20, 0xAF, 0x30 }))
                {
                    int pointerPointer = BitConverter.ToInt32(dolBytes.Skip(i + 0x08).Take(4).Reverse().ToArray()) + i;
                    int sizePointer = BitConverter.ToInt32(dolBytes.Skip(pointerPointer).Take(4).Reverse().ToArray()) + i;
                    int height = BitConverter.ToUInt16(dolBytes.Skip(sizePointer).Take(2).Reverse().ToArray());
                    int width = BitConverter.ToUInt16(dolBytes.Skip(sizePointer + 2).Take(2).Reverse().ToArray());
                    GraphicsFile.ImageFormat mode = (GraphicsFile.ImageFormat)BitConverter.ToInt32(dolBytes.Skip(sizePointer + 4).Take(4).Reverse().ToArray());

                    int numPixels = height * width;
                    int numBytes = 0;
                    switch (mode)
                    {
                        case GraphicsFile.ImageFormat.CMPR:
                            numBytes = numPixels / 2;
                            break;

                        case GraphicsFile.ImageFormat.IA4:
                            numBytes = numPixels;
                            break;

                        case GraphicsFile.ImageFormat.IA8:
                        case GraphicsFile.ImageFormat.RGB565:
                        case GraphicsFile.ImageFormat.RGB5A3:
                            numBytes = numPixels * 2;
                            break;

                        case GraphicsFile.ImageFormat.RGBA8:
                            numBytes = numPixels * 4;
                            break;
                    }

                    GraphicsFile graphicsFile = new();
                    graphicsFile.Initialize(dolBytes.Skip(i).Take(numBytes).ToArray(), i);
                    GraphicsFiles.Add(graphicsFile);
                }
            }
        }
    }
}
