using Kanvas.Encoding;
using Kanvas.Encoding.PlatformSpecific;
using Kontract.Kanvas;
using Kontract.Models.IO;

namespace Kanvas
{
    public static class ImageFormats
    {
        public static WiiImageFormats Wii { get; } = new WiiImageFormats();

        public static IColorEncoding Rgba1010102(ByteOrder byteOrder = ByteOrder.LittleEndian) => new Rgba(10, 10, 10, 2, byteOrder);
        public static IColorEncoding Rgba8888(ByteOrder byteOrder = ByteOrder.LittleEndian) => new Rgba(8, 8, 8, 8, byteOrder);
        public static IColorEncoding Rgb888() => new Rgba(8, 8, 8);
        public static IColorEncoding Bgr888() => new Rgba(8, 8, 8, "BGR");
        public static IColorEncoding Rgba5551(ByteOrder byteOrder = ByteOrder.LittleEndian) => new Rgba(5, 5, 5, 1, byteOrder);
        public static IColorEncoding Rgb565(ByteOrder byteOrder = ByteOrder.LittleEndian) => new Rgba(5, 6, 5, byteOrder);
        public static IColorEncoding Rgb555(ByteOrder byteOrder = ByteOrder.LittleEndian) => new Rgba(5, 5, 5, byteOrder);
        public static IColorEncoding Rgba4444(ByteOrder byteOrder = ByteOrder.LittleEndian) => new Rgba(4, 4, 4, 4, byteOrder);
        public static IColorEncoding Rg88(ByteOrder byteOrder = ByteOrder.LittleEndian) => new Rgba(8, 8, 0, byteOrder);

        public static IColorEncoding L8() => new La(8, 0);
        public static IColorEncoding L4(BitOrder bitOrder = BitOrder.MostSignificantBitFirst) => new La(4, 0, ByteOrder.LittleEndian, bitOrder);
        public static IColorEncoding A8() => new La(0, 8);
        public static IColorEncoding A4(BitOrder bitOrder = BitOrder.MostSignificantBitFirst) => new La(0, 4, ByteOrder.LittleEndian, bitOrder);
        public static IColorEncoding La88(ByteOrder byteOrder = ByteOrder.LittleEndian) => new La(8, 8, byteOrder);
        public static IColorEncoding La44() => new La(4, 4);
        public static IColorEncoding Al44() => new La(4, 4, "AL");

        public static IIndexEncoding I2(BitOrder bitOrder = BitOrder.MostSignificantBitFirst) => new Index(2, ByteOrder.LittleEndian, bitOrder);
        public static IIndexEncoding I4(BitOrder bitOrder = BitOrder.MostSignificantBitFirst) => new Index(4, ByteOrder.LittleEndian, bitOrder);
        public static IIndexEncoding I8() => new Index(8);
        public static IIndexEncoding Ia53() => new Index(5, 3);
        public static IIndexEncoding Ia35() => new Index(3, 5);

        public static IColorEncoding Etc1(bool zOrder, ByteOrder byteOrder = ByteOrder.LittleEndian) => new Etc1(false, zOrder, byteOrder);
        public static IColorEncoding Etc1A4(bool zOrder, ByteOrder byteOrder = ByteOrder.LittleEndian) => new Etc1(true, zOrder, byteOrder);

        public static IColorEncoding Dxt1() => new Bc(BcFormat.Dxt1);
        public static IColorEncoding Dxt3() => new Bc(BcFormat.Dxt3);
        public static IColorEncoding Dxt5() => new Bc(BcFormat.Dxt5);
        public static IColorEncoding Ati1() => new Bc(BcFormat.Ati1);
        public static IColorEncoding Ati2() => new Bc(BcFormat.Ati2);
        public static IColorEncoding Ati1L() => new Bc(BcFormat.Ati1L);
        public static IColorEncoding Ati1A() => new Bc(BcFormat.Ati1A);
        public static IColorEncoding Ati2AL() => new Bc(BcFormat.Ati2AL);

        public static IColorEncoding Atc() => new Atc(AtcFormat.Atc);
        public static IColorEncoding AtcExplicit() => new Atc(AtcFormat.Atc_Explicit);
        public static IColorEncoding AtcInterpolated() => new Atc(AtcFormat.Atc_Interpolated);

        public static IColorEncoding Bc7() => new Bc(BcFormat.Bc7);
    }

    /* Formats configured as per http://wiki.tockdom.com/wiki/Image_Formats */
    public class WiiImageFormats
    {
        internal WiiImageFormats() { }

        public IColorEncoding L4() => new La(4, 0);
        public IColorEncoding L8() => new La(8, 0);
        public IColorEncoding La44() => new La(4, 4, "AL");
        public IColorEncoding La88() => new La(8, 8, "AL", ByteOrder.BigEndian);

        public IColorEncoding Rgb565() => new Rgba(5, 6, 5, ByteOrder.BigEndian);
        public IColorEncoding Rgb5A3() => new Rgb5A3();
        public IColorEncoding Rgba8888() => new Rgba8();

        public IIndexEncoding I4() => new Index(4);
        public IIndexEncoding I8() => new Index(8);
        public IIndexEncoding I14() => new Index(14, ByteOrder.BigEndian);

        public IColorEncoding Cmpr() => new Bc(BcFormat.Bc1);
    }
}
