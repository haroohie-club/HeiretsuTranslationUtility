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
        private ArchiveFile<ScriptFile> _scrFile;
        private DolFile _dolFile;
        private FontFile _fontFile;
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
                _mcb.LoadScriptFiles(File.ReadAllText("string_file_locations.csv"));
                _mcb.Load20AF30GraphicsFiles(File.ReadAllText("graphics_20AF30_locations.csv"));
                scriptsListBox.ItemsSource = _mcb.ScriptFiles;
                graphicsListBox.ItemsSource = _mcb.Graphics20AF30Files;
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

        private void OpenScrFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "SCR.BIN|scr*.bin"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                _scrFile = ArchiveFile<ScriptFile>.FromFile(openFileDialog.FileName);
                scriptsListBox.ItemsSource = _scrFile.Files;
                scriptsListBox.Items.Refresh();
            }
        }

        private void SaveScrFileButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "SCR.BIN|scr*.bin"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllBytes(saveFileDialog.FileName, _scrFile.GetBytes());
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

        // only works for chokuretsu style ones and only works for the mcb bc i'm lazy
        private void ImportEventsFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "BIN file|*.bin"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                ChokuretsuEventFile chokuretsuEventFile = new();
                chokuretsuEventFile.Initialize(File.ReadAllBytes(openFileDialog.FileName));
                chokuretsuEventFile.Location = _mcb.ScriptFiles[scriptsListBox.SelectedIndex].Location;
                _mcb.ScriptFiles[scriptsListBox.SelectedIndex] = chokuretsuEventFile;
                scriptsListBox.Items.Refresh();
            }
        }

        private void ScriptsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            scriptEditStackPanel.Children.Clear();
            if (_mcb is not null)
            {
                scriptEditStackPanel.Children.Add(new TextBlock { Text = $"{_mcb.ScriptFiles.Sum(s => s.DialogueLines.Count)}" });
            }
            else if (_scrFile is not null)
            {
                scriptEditStackPanel.Children.Add(new TextBlock { Text = $"{_scrFile.Files.Sum(s => s.DialogueLines.Count)}" });
            }
            if (scriptsListBox.SelectedIndex >= 0)
            {
                var selectedFile = (ScriptFile)scriptsListBox.SelectedItem;
                for (int i = 0; i < selectedFile.DialogueLines.Count; i++)
                {
                    StackPanel dialogueStackPanel = new() { Orientation = Orientation.Horizontal };
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

        private void SaveGrpFileButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "GRP.BIN|grp*.bin"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllBytes(saveFileDialog.FileName, _grpFile.GetBytes());
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

        private void OpenDolGraphicsFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "haruhi.dol|haruhi.dol"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                graphicsEditStackPanel.Children.Clear();
                _dolFile = new(File.ReadAllBytes(openFileDialog.FileName));
                graphicsListBox.ItemsSource = _dolFile.GraphicsFiles;
                graphicsListBox.Items.Refresh();
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
                    Filter = "PNG file|*.png"
                };
                if (saveFileDialog.ShowDialog() == true)
                {
                    Bitmap bitmap = _loadedGraphicsFile.GetImage();
                    bitmap.Save(saveFileDialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
                }
            }
        }

        private void ImportImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_loadedGraphicsFile is not null)
            {
                OpenFileDialog openFileDialog = new()
                {
                    Filter = "PNG file|*.png"
                };
                if (openFileDialog.ShowDialog() == true)
                {
                    _loadedGraphicsFile.Set20AF30Image(new Bitmap(openFileDialog.FileName));
                    graphicsEditStackPanel.Children.Clear();
                    graphicsEditStackPanel.Children.Add(new TextBlock { Text = $"20AF30: {_loadedGraphicsFile.Mode}", Background = System.Windows.Media.Brushes.White });
                    graphicsEditStackPanel.Children.Add(new System.Windows.Controls.Image { Source = GuiHelpers.GetBitmapImageFromBitmap(_loadedGraphicsFile.GetImage()), MaxWidth = _loadedGraphicsFile.Width });
                }
            }
        }

        private void OpenFontFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "BIN file|*.bin"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                _fontFile = new(File.ReadAllBytes(openFileDialog.FileName));
                fontListBox.ItemsSource = _fontFile.Characters;
            }
        }

        private void FontListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (fontListBox.SelectedIndex >= 0)
            {
                Character selectedCharacter = (Character)fontListBox.SelectedItem;
                fontEditStackPanel.Children.Clear();
                fontEditStackPanel.Background = System.Windows.Media.Brushes.Gray;
                fontEditStackPanel.Children.Add(new System.Windows.Controls.Image { Source = GuiHelpers.GetBitmapImageFromBitmap(selectedCharacter.GetImage()), MaxWidth = selectedCharacter.Width });
            }
        }
    }
}
