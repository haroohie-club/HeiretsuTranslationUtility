using SkiaSharp;
using System;
using System.Windows;
using System.Windows.Controls;

namespace HaruhiHeiretsuEditor
{
    /// <summary>
    /// Interaction logic for LanguageCodeDialogBox.xaml
    /// </summary>
    public partial class MapPreviewWindow : Window
    {
        public MapPreviewWindow(Image image)
        {
            InitializeComponent();
            mainGrid.Children.Add(image);
        }
        public MapPreviewWindow()
        {
            InitializeComponent();
        }

        public void CompleteLayoutLoad(SKBitmap bitmap)
        {
            Dispatcher.BeginInvoke(new Action<SKBitmap>(AddContent), bitmap);
        }

        private void AddContent(SKBitmap bitmap)
        {
            mainGrid.Children.Add(new Image { Source = GuiHelpers.GetBitmapImageFromBitmap(bitmap) });
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }
    }
}
