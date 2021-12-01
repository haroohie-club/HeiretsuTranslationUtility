﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace HaruhiHeiretsuEditor
{
    public static class GuiHelpers
    {
        public static BitmapImage GetBitmapImageFromBitmap(Bitmap bitmap)
        {
            BitmapImage bitmapImage = new BitmapImage();
            if (bitmap is not null)
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    bitmap.Save(memoryStream, ImageFormat.Png);
                    memoryStream.Position = 0;
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memoryStream;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                }
            }
            return bitmapImage;
        }
    }
}
