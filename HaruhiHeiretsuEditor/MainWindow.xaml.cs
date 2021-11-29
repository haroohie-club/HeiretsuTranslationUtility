using HaruhiHeiretsuLib;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
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

        private void ScriptsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            scriptEditStackPanel.Children.Clear();
            scriptEditStackPanel.Children.Add(new TextBlock { Text = $"{_mcb.ScriptFiles.Sum(s => s.DialogueLines.Count)}" });
            if (scriptsListBox.SelectedIndex >= 0)
            {
                var selectedFile = (ScriptFile)scriptsListBox.SelectedItem;
                foreach (DialogueLine dialogueLine in selectedFile.DialogueLines)
                {
                    var dialogueStackPanel = new StackPanel { Orientation = Orientation.Horizontal };
                    dialogueStackPanel.Children.Add(new TextBlock { Text = dialogueLine.Speaker.ToString() });
                    dialogueStackPanel.Children.Add(new TextBox { Text = dialogueLine.Line });
                    scriptEditStackPanel.Children.Add(dialogueStackPanel);
                }
            }
        }
    }
}
