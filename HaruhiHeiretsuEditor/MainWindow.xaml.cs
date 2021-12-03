using HaruhiHeiretsuLib;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HaruhiHeiretsuEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private McbFile _mcb;
        private ArchiveFile<GraphicsFile> _grpFile;
        private GraphicsFile _loadedGraphicsFile;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MenuOpenMcb_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "MCB0|mcb0.bln"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                _mcb = new McbFile(openFileDialog.FileName, openFileDialog.FileName.Replace("0", "1"));
                string stringFileLocations = File.ReadAllText("string_file_locations.csv");
                _mcb.LoadScriptFiles(stringFileLocations);
                scriptsListBox.ItemsSource = _mcb.ScriptFiles;
            }
        }

        private void MenuSaveMcb_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "MCB0|mcb0.bln"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                _mcb.Save(saveFileDialog.FileName, saveFileDialog.FileName.Replace("0", "1")).GetAwaiter().GetResult();
            }
        }

        private void ExportEventsFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (scriptsListBox.SelectedIndex >= 0)
            {
                SaveFileDialog saveFileDialog = new()
                {
                    Filter = "BIN file|*.bin"
                };
                if (saveFileDialog.ShowDialog() == true)
                {
                    var selectedFile = (ScriptFile)scriptsListBox.SelectedItem;
                    File.WriteAllBytes(saveFileDialog.FileName, selectedFile.Data.ToArray());
                }                
            }
        }

        private void ScriptsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            scriptEditStackPanel.Children.Clear();
            scriptEditStackPanel.Children.Add(new TextBlock { Text = $"{_mcb.ScriptFiles.Sum(s => s.DialogueLines.Count)}" });
            if (scriptsListBox.SelectedIndex >= 0)
            {
                var selectedFile = (ScriptFile)scriptsListBox.SelectedItem;
                for (int i = 0; i < selectedFile.DialogueLines.Count; i++)
                {
                    var dialogueStackPanel = new StackPanel { Orientation = Orientation.Horizontal };
                    dialogueStackPanel.Children.Add(new TextBlock { Text = selectedFile.DialogueLines[i].Speaker.ToString() });
                    DialogueTextBox dialogueTextBox = new() { Text = selectedFile.DialogueLines[i].Line, AcceptsReturn = true, ScriptFile = selectedFile, DialogueLineIndex = i };
                    dialogueTextBox.TextChanged += DialogueTextBox_TextChanged;
                    dialogueStackPanel.Children.Add(dialogueTextBox);
                    scriptEditStackPanel.Children.Add(dialogueStackPanel);
                }
            }
        }

        private void DialogueTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            DialogueTextBox dialogueTextBox = (DialogueTextBox)sender;

            dialogueTextBox.ScriptFile.EditDialogue(dialogueTextBox.DialogueLineIndex, dialogueTextBox.Text);
        }

        private void OpenGrpFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "GRP.BIN|grp*.bin"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                _grpFile = ArchiveFile<GraphicsFile>.FromFile(openFileDialog.FileName);
                graphicsListBox.ItemsSource = _grpFile.Files;
                graphicsListBox.Items.Refresh();
            }
        }

        private void OpenSingleGraphicsFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "BIN file|*.bin"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                graphicsEditStackPanel.Children.Clear();
                GraphicsFile graphicsFile = new();
                graphicsFile.Initialize(File.ReadAllBytes(openFileDialog.FileName), 0);
                graphicsEditStackPanel.Children.Add(new TextBlock { Text = $"20AF30: {graphicsFile.Mode}", Background = System.Windows.Media.Brushes.White });
                graphicsEditStackPanel.Children.Add(new System.Windows.Controls.Image { Source = GuiHelpers.GetBitmapImageFromBitmap(graphicsFile.GetImage()), MaxWidth = graphicsFile.Width });
                _loadedGraphicsFile = graphicsFile;
            }
        }

        private void GraphicsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            graphicsEditStackPanel.Children.Clear();
            _loadedGraphicsFile = null;
            if (graphicsListBox.SelectedIndex >= 0)
            {
                GraphicsFile selectedFile = (GraphicsFile)graphicsListBox.SelectedItem;
                if (selectedFile.FileType == GraphicsFile.GraphicsFileType.SGE)
                {
                    graphicsEditStackPanel.Children.Add(new TextBlock { Text = $"SGE {selectedFile.Data.Count} bytes" });
                }
                if (selectedFile.FileType == GraphicsFile.GraphicsFileType.TYPE_20AF30)
                {
                    graphicsEditStackPanel.Background = System.Windows.Media.Brushes.Gray;
                    graphicsEditStackPanel.Children.Add(new TextBlock { Text = $"20AF30: {selectedFile.Mode}", Background = System.Windows.Media.Brushes.White });
                    graphicsEditStackPanel.Children.Add(new System.Windows.Controls.Image { Source = GuiHelpers.GetBitmapImageFromBitmap(selectedFile.GetImage()), MaxWidth = selectedFile.Width });
                    _loadedGraphicsFile = selectedFile;
                }
            }
        }

        private void SaveImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_loadedGraphicsFile is not null)
            {
                SaveFileDialog saveFileDialog = new()
                {
                    Filter = "BMP file|*.bmp"
                };
                if (saveFileDialog.ShowDialog() == true)
                {
                    Bitmap bitmap = _loadedGraphicsFile.GetImage();
                    bitmap.Save(saveFileDialog.FileName, System.Drawing.Imaging.ImageFormat.Bmp);
                }
            }
        }
    }
}
