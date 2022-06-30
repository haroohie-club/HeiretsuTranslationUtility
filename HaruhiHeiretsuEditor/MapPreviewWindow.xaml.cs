using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
