using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaruhiHeiretsuLib
{
    public class GraphicsFile : FileInArchive
    {
        public GraphicsFileType FileType { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public ImageMode Mode  { get; set; }

        public int PointerPointer { get; set; }
        public int SizePointer { get; set; }
        public int DataPointer { get; set; }

        public GraphicsFile()
        {

        }

        public override void Initialize(byte[] compressedData, int offset)
        {
            Data = compressedData.ToList();
            Offset = offset;
            if (Encoding.ASCII.GetString(Data.Take(3).ToArray()) == "SGE")
            {
                FileType = GraphicsFileType.SGE;
            }
            if (Data[0] == 0x00 && Data[1] == 0x20 && Data[2] == 0xAF && Data[3] == 0x30)
            {
                FileType = GraphicsFileType.TYPE_20AF30;
                PointerPointer = BitConverter.ToInt32(Data.Skip(0x08).Take(4).Reverse().ToArray());
                SizePointer = BitConverter.ToInt32(Data.Skip(PointerPointer).Take(4).Reverse().ToArray());
                Height = BitConverter.ToInt16(Data.Skip(SizePointer).Take(2).Reverse().ToArray());
                Width = BitConverter.ToInt16(Data.Skip(SizePointer + 2).Take(2).Reverse().ToArray());
                Mode = (ImageMode)BitConverter.ToInt32(Data.Skip(SizePointer + 4).Take(4).Reverse().ToArray());
                DataPointer = BitConverter.ToInt32(Data.Skip(SizePointer + 8).Take(4).Reverse().ToArray());
            }
            else
            {
                FileType = GraphicsFileType.UNKNOWN;
            }
        }

        public Bitmap GetImage()
        {
            if (FileType == GraphicsFileType.TYPE_20AF30)
            {
                Bitmap bitmap = new(Width, Height);

                switch (Mode)
                {
                    case ImageMode.RGBA32:

                        for (int h = 0; h < Height; h += 4)
                        {
                            for (int w = 0; w < Width; w += 4)
                            {
                                for (int bh = 0; bh < 4; bh++)
                                {
                                    for (int bw = 0; bw < 4; bw++)
                                    {
                                        if (h * Width * 4 + w * 16 + bw * 2 + bh * 8 + 17 + DataPointer >= Data.Count || w + bw >= Width || h + bh >= Height)
                                        {
                                            break;
                                        }
                                        Color color = Color.FromArgb(
                                            Data[h * Width * 4 + w * 16 + bw * 2 + bh * 8 + DataPointer],
                                            Data[h * Width * 4 + w * 16 + bw * 2 + bh * 8 + 1 + DataPointer],
                                            Data[h * Width * 4 + w * 16 + bw * 2 + bh * 8 + 16 + DataPointer],
                                            Data[h * Width * 4 + w * 16 + bw * 2 + bh * 8 + 17 + DataPointer]);

                                        bitmap.SetPixel(w + bw, h + bh, color);
                                    }
                                }
                            }
                        }
                        break;
                }
                return bitmap;
            }
            return null;
        }

        public override string ToString()
        {
            return $"{Index:X3} {Index:D4} 0x{Offset:X8}";
        }

        public enum GraphicsFileType
        {
            SGE,
            TYPE_20AF30,
            UNKNOWN
        }

        public enum ImageMode
        {
            I4 = 0x00,
            I8 = 0x01,
            IA4 = 0x02,
            IA8 = 0x03,
            RGB565 = 0x04,
            RGB6A3 = 0x05,
            RGBA32 = 0x06,
            CI4 = 0x08,
            CI8 = 0x09,
            CI14X2 = 0x0A,
            CMPR = 0x0E,
        }
    }
}
