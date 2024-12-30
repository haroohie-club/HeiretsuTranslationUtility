using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace HaruhiHeiretsuEditor
{
    public static class GuiHelpers
    {
        public static BitmapImage GetBitmapImageFromBitmap(SKBitmap bitmap)
        {
            BitmapImage bitmapImage = new();
            if (bitmap is not null)
            {
                using MemoryStream memoryStream = new();
                bitmap.Encode(memoryStream, SKEncodedImageFormat.Png, 300);
                memoryStream.Position = 0;
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
            }
            return bitmapImage;
        }
    }
}
