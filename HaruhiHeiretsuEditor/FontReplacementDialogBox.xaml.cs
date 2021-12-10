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
    public partial class FontReplacementDialogBox : Window
    {
        public System.Drawing.FontFamily SelectedFontFamily { get; set; }
        public int SelectedFontSize { get; set; }
        public Encoding SelectedEncoding { get; set; }
        public char StartingChar { get; set; }
        public char EndingChar { get; set; }

        public FontReplacementDialogBox()
        {
            InitializeComponent();
            fontFamilyComboBox.ItemsSource = new System.Drawing.Text.InstalledFontCollection().Families;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            SelectedFontFamily = (System.Drawing.FontFamily)fontFamilyComboBox.SelectedItem;
            SelectedFontSize = int.Parse(fontSizeTextBox.Text);
            switch (((ComboBoxItem)encodingComboBox.SelectedItem).Tag)
            {
                case "latin1":
                    SelectedEncoding = Encoding.Latin1;
                    break;
                case "utf8":
                    SelectedEncoding = Encoding.UTF8;
                    break;
                case "shiftjis":
                    SelectedEncoding = Encoding.GetEncoding("Shift-JIS");
                    break;
            }
            StartingChar = startingCharacterTextBox.Text[0];
            EndingChar = endingCharacterTextBox.Text[0];
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
