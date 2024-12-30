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
