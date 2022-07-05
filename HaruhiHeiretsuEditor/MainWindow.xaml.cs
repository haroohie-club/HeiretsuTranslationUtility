using FolderBrowserEx;
using HaruhiHeiretsuLib;
using HaruhiHeiretsuLib.Archive;
using HaruhiHeiretsuLib.Data;
using HaruhiHeiretsuLib.Graphics;
using HaruhiHeiretsuLib.Strings;
using HaruhiHeiretsuLib.Strings.Events;
using HaruhiHeiretsuLib.Strings.Scripts;
using Microsoft.Win32;
using Newtonsoft.Json;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HaruhiHeiretsuEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private McbArchive _mcb;
        private BinArchive<DataFile> _datFile;
        private BinArchive<ShadeStringsFile> _datStringsFile;
        private BinArchive<EventFile> _evtFile;
        private BinArchive<GraphicsFile> _grpFile;
        private BinArchive<ScriptFile> _scrFile;
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
                _mcb = new McbArchive(openFileDialog.FileName, openFileDialog.FileName.Replace("0", "1"));

                byte[] commandsFileData = _mcb.McbSubArchives[75].Files[2].Data.ToArray();

                _mcb.LoadStringsFiles(File.ReadAllText("string_file_locations.csv"), ScriptCommand.ParseScriptCommandFile(commandsFileData));
                _mcb.LoadGraphicsFiles();
                _mcb.LoadFontFile();
                scriptsListBox.ItemsSource = _mcb.StringsFiles.Select(f => (StringsFile)_mcb.McbSubArchives[f.parentLoc].Files[f.childLoc]);
                graphicsListBox.ItemsSource = _mcb.GraphicsFiles.Select(f => (GraphicsFile)_mcb.McbSubArchives[f.parentLoc].Files[f.childLoc]);
                fontListBox.ItemsSource = _mcb.FontFile.Characters;
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
                try
                {
                    (byte[] mcb0, byte[] mcb1) = _mcb.GetBytes();
                    File.WriteAllBytes(saveFileDialog.FileName, mcb0);
                    File.WriteAllBytes(saveFileDialog.FileName.Replace("0", "1"), mcb1);
                    MessageBox.Show("Save completed!");
                }
                catch (InvalidOperationException exc)
                {
                    MessageBox.Show($"Unable to save mcbs: {exc.Message}");
                }
            }
        }

        private void MenuAdjustMcb_Click(object sender, RoutedEventArgs e)
        {
            if (_mcb is not null)
            {
                SaveFileDialog mcbSaveFileDialog = new()
                {
                    Filter = "MCB0|mcb0.bln",
                    Title = "Save MCBs"
                };
                if (mcbSaveFileDialog.ShowDialog() == true)
                {
                    OpenFileDialog offsetOpenFileDialog = new()
                    {
                        Filter = "CSV|*.csv",
                        Title = "Open Offset Adjustments CSV"
                    };
                    if (offsetOpenFileDialog.ShowDialog() == true)
                    {
                        _mcb.AdjustOffsets(offsetOpenFileDialog.FileName);
                        (byte[] mcb0, byte[] mcb1) = _mcb.GetBytes();
                        File.WriteAllBytes(mcbSaveFileDialog.FileName, mcb0);
                        File.WriteAllBytes(mcbSaveFileDialog.FileName.Replace("0", "1"), mcb1);
                    }
                }
            }
        }

        private void LoadBinArchiveHeader_Click(object sender, RoutedEventArgs e)
        {
            if (_mcb is not null)
            {
                OpenFileDialog openFileDialog = new()
                {
                    Filter = "Any BIN file|*.bin"
                };
                if (openFileDialog.ShowDialog() == true)
                {
                    _mcb.LoadIndexOffsetDictionary(openFileDialog.FileName);
                    scriptsListBox.Items.Refresh();
                    graphicsListBox.Items.Refresh();
                }
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
                _scrFile = BinArchive<ScriptFile>.FromFile(openFileDialog.FileName);
                List<string> scriptFileNames = ScriptFile.ParseScriptListFile(_scrFile.Files[0].Data.ToArray());
                List<ScriptCommand> availableCommands = ScriptCommand.ParseScriptCommandFile(_scrFile.Files[1].Data.ToArray());
                for (int i = 0; i < _scrFile.Files.Count; i++)
                {
                    _scrFile.Files[i].Name = scriptFileNames[i];
                    _scrFile.Files[i].AvailableCommands = availableCommands;
                    _scrFile.Files[i].PopulateCommandBlocks();
                }
                scriptsListBox.ItemsSource = _scrFile.Files;
                scriptsListBox.Items.Refresh();
                foreach (ScriptCommand command in availableCommands)
                {
                    availableCommandsEditStackPanel.Children.Add(new TextBlock { Text = command.GetSignature() });
                }
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
                File.WriteAllBytes(saveFileDialog.FileName, _scrFile.GetBytes(out Dictionary<int, int> offsetAdjustments));
                string offsetAdjustmentsFile = "scr.bin";
                foreach (int originalOffset in offsetAdjustments.Keys)
                {
                    offsetAdjustmentsFile += $"\n{originalOffset},{offsetAdjustments[originalOffset]}";
                }
                File.WriteAllText($"{saveFileDialog.FileName}_offset_adjustments.csv", offsetAdjustmentsFile);
                MessageBox.Show("Save completed!");
            }
        }

        private void OpenDatFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "DAT.BIN|dat*.bin"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                _datStringsFile = BinArchive<ShadeStringsFile>.FromFile(openFileDialog.FileName);
                scriptsListBox.ItemsSource = _datStringsFile.Files;
                scriptsListBox.Items.Refresh();
            }
        }

        private void OpenEvtFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "EVT.BIN|evt*.bin"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                _evtFile = BinArchive<EventFile>.FromFile(openFileDialog.FileName);
                scriptsListBox.ItemsSource = _evtFile.Files;
                // TODO: REMOVE
                JsonSerializer serializer = JsonSerializer.Create(new() { MaxDepth = 10 });
                using FileStream fs = File.OpenWrite("evt.json");
                using StreamWriter sw = new(fs);
                serializer.Serialize(sw, _evtFile.Files.Select(f => f.CutsceneData));
                scriptsListBox.Items.Refresh();
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
                    var selectedFile = (StringsFile)scriptsListBox.SelectedItem;
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
                ShadeStringsFile shadeStringsFile = new();
                shadeStringsFile.Initialize(File.ReadAllBytes(openFileDialog.FileName));
                shadeStringsFile.Location = _mcb.StringsFiles[scriptsListBox.SelectedIndex];
                _mcb.McbSubArchives[shadeStringsFile.Location.parent].Files[shadeStringsFile.Location.child] = shadeStringsFile;
                scriptsListBox.Items.Refresh();
            }
        }

        private void ImportScriptFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "Shade Wii script file|*.sws"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                ScriptFile currentFile = (ScriptFile)scriptsListBox.SelectedItem;
                ScriptFile scriptFile = new();
                scriptFile.Location = currentFile.Location;
                scriptFile.Name = currentFile.Name;
                scriptFile.AvailableCommands = currentFile.AvailableCommands;
                scriptFile.Edited = true;
                scriptFile.Compile(File.ReadAllText(openFileDialog.FileName));
                _mcb.McbSubArchives[scriptFile.Location.parent].Files[scriptFile.Location.child] = scriptFile;
                scriptsListBox.Items.Refresh();
            }
        }

        private void ExportAllScriptsButton_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog saveFolderDialog = new()
            {
                AllowMultiSelect = false
            };
            if (saveFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                foreach (ScriptFile script in scriptsListBox.Items)
                {
                    if (script.ScriptCommandBlocks.Count > 0)
                    {
                        File.WriteAllText(Path.Combine(saveFolderDialog.SelectedFolder, $"{script.Name}.sws"), script.Decompile());
                    }
                }
            }
        }

        private void ExportCommandListButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "TXT file|*.txt"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllText(saveFileDialog.FileName, string.Join("\n", ((ScriptFile)scriptsListBox.Items[2]).AvailableCommands.Select(c => c.GetSignature())));
            }
        }

        private void ScriptsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            scriptEditStackPanel.Children.Clear();
            commandsEditStackPanel.Children.Clear();
            if (_mcb is not null)
            {
                scriptEditStackPanel.Children.Add(new TextBlock { Text = $"{_mcb.StringsFiles.Select(f => (StringsFile)_mcb.McbSubArchives[f.parentLoc].Files[f.childLoc]).Sum(s => s.DialogueLines.Count)}" });
            }
            else
            {
                if (_scrFile is not null)
                {
                    scriptEditStackPanel.Children.Add(new TextBlock { Text = $"SCR {_scrFile.Files.Sum(s => s.DialogueLines.Count)}" });
                }
                if (_evtFile is not null)
                {
                    scriptEditStackPanel.Children.Add(new TextBlock { Text = $"EVT {_evtFile.Files.Where(e => e.Index > 0xB3 || e.Index < 0xB1).Sum(e => e.DialogueLines.Count)}" });
                }
            }
            if (scriptsListBox.SelectedIndex >= 0)
            {
                var selectedFile = (StringsFile)scriptsListBox.SelectedItem;
                for (int i = 0; i < selectedFile.DialogueLines.Count; i++)
                {
                    StackPanel dialogueStackPanel = new() { Orientation = Orientation.Horizontal };
                    dialogueStackPanel.Children.Add(new TextBlock { Text = selectedFile.DialogueLines[i].Speaker.ToString() });
                    DialogueTextBox dialogueTextBox = new() { Text = selectedFile.DialogueLines[i].Line, AcceptsReturn = true, StringsFile = selectedFile, DialogueLineIndex = i };
                    dialogueTextBox.TextChanged += DialogueTextBox_TextChanged;
                    dialogueStackPanel.Children.Add(dialogueTextBox);
                    dialogueStackPanel.Children.Add(new TextBlock { Text = string.Join(", ", selectedFile.DialogueLines[i].Metadata) });
                    scriptEditStackPanel.Children.Add(dialogueStackPanel);
                }
                if (selectedFile.GetType() == typeof(ScriptFile))
                {
                    var scriptFile = (ScriptFile)selectedFile;
                    ScriptButton openInEditorButton = new() { Content = "Open in Editor", Script = scriptFile };
                    openInEditorButton.Click += OpenInEditorButton_Click;
                    commandsEditStackPanel.Children.Add(openInEditorButton);
                    foreach (ScriptCommandBlock block in scriptFile.ScriptCommandBlocks)
                    {

                        commandsEditStackPanel.Children.Add(new TextBlock() { Text = $"{block.Name} ({block.NumInvocations})", FontSize = 16 });

                        foreach (ScriptCommandInvocation invocation in block.Invocations)
                        {
                            commandsEditStackPanel.Children.Add(new TextBlock() { Text = invocation.GetInvocation() });
                        }

                        commandsEditStackPanel.Children.Add(new Separator());
                    }
                }
            }
        }

        private void OpenInEditorButton_Click(object sender, RoutedEventArgs e)
        {
            ScriptFile script = ((ScriptButton)sender).Script;
            string tempFile = Path.Combine(Path.GetTempPath(), $"{script.Name}.sws");
            File.WriteAllText(tempFile, script.Decompile());
            new Process { StartInfo = new ProcessStartInfo(tempFile) { UseShellExecute = true } }.Start();
        }

        private void DialogueTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            DialogueTextBox dialogueTextBox = (DialogueTextBox)sender;

            dialogueTextBox.StringsFile.EditDialogue(dialogueTextBox.DialogueLineIndex, dialogueTextBox.Text);
        }

        private void OpenGrpFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "GRP.BIN|grp*.bin"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                _grpFile = BinArchive<GraphicsFile>.FromFile(openFileDialog.FileName);
                graphicsListBox.ItemsSource = _grpFile.Files;
                graphicsListBox.Items.Refresh();
                _fontFile = new FontFile(_grpFile.Files[0].Data.ToArray());
                fontListBox.ItemsSource = _fontFile.Characters;
                fontListBox.Items.Refresh();
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
                if (_fontFile.Edited)
                {
                    _grpFile.Files[0].Edited = true;
                    _grpFile.Files[0].Data = _fontFile.GetBytes().ToList();
                }
                File.WriteAllBytes(saveFileDialog.FileName, _grpFile.GetBytes(out Dictionary<int, int> offsetAdjustments));
                string offsetAdjustmentsFile = "grp.bin";
                foreach (int originalOffset in offsetAdjustments.Keys)
                {
                    offsetAdjustmentsFile += $"\n{originalOffset},{offsetAdjustments[originalOffset]}";
                }
                File.WriteAllText($"{saveFileDialog.FileName}_offset_adjustments.csv", offsetAdjustmentsFile);
                MessageBox.Show("Save completed!");
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
                graphicsEditStackPanel.Children.Add(new TextBlock { Text = $"20AF30: {graphicsFile.Mode}", Background = Brushes.White });
                graphicsEditStackPanel.Children.Add(new Image { Source = GuiHelpers.GetBitmapImageFromBitmap(graphicsFile.GetImage()), MaxWidth = graphicsFile.Width });
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
                if (_grpFile is not null)
                {
                    graphicsEditStackPanel.Children.Add(new TextBlock
                    {
                        Text = $"Actual compressed length: {selectedFile.CompressedData.Length:X}; Calculated length: {selectedFile.Length:X}",
                        Background = Brushes.White
                    });
                }
                if (selectedFile.FileType == GraphicsFile.GraphicsFileType.SGE)
                {
                    graphicsEditStackPanel.Children.Add(new TextBlock { Text = $"SGE {selectedFile.Data.Count} bytes; {selectedFile.Sge.SgeHeader.BonesCount} bones and {selectedFile.Sge.SgeHeader.Unknown0C} meshes", Background = Brushes.White });
                    foreach (SgeMaterial mat in selectedFile.Sge.SgeMaterials)
                    {
                        graphicsEditStackPanel.Children.Add(new TextBlock { Text = mat.Name });
                    }
                    if (selectedFile.Sge.SgeMaterials.FirstOrDefault()?.Texture is not null)
                    {
                        GraphicsButton sgeButton = new() { Content = "Export SGE JSON", Graphic = selectedFile };
                        sgeButton.Click += SgeButton_Click;
                        graphicsEditStackPanel.Children.Add(sgeButton);

                        //GLWpfControlSettings settings = new() { MajorVersion = 4, MinorVersion = 2 };
                        //GLWpfControl glwpf = new() { Width = 480, Height = 640 };

                        //graphicsEditStackPanel.Children.Add(glwpf);
                        //glwpf.Start(settings);

                        Button statsButton = new() { Content = "SGE Stats" };
                        statsButton.Click += StatsButton_Click;
                        graphicsEditStackPanel.Children.Add(statsButton);
                    }
                }
                else if (selectedFile.FileType == GraphicsFile.GraphicsFileType.TEXTURE)
                {
                    graphicsEditStackPanel.Background = Brushes.Gray;
                    graphicsEditStackPanel.Children.Add(new TextBlock { Text = $"20AF30: {selectedFile.Mode}", Background = Brushes.White });
                    graphicsEditStackPanel.Children.Add(new Image { Source = GuiHelpers.GetBitmapImageFromBitmap(selectedFile.GetImage()), MaxWidth = selectedFile.Width });
                    _loadedGraphicsFile = selectedFile;
                }
                else if (selectedFile.FileType == GraphicsFile.GraphicsFileType.LAYOUT)
                {
                    graphicsEditStackPanel.Background = Brushes.Gray;
                    graphicsEditStackPanel.Children.Add(new TextBlock { Text = $"LAYOUT: {string.Join(' ', selectedFile.UnknownLayoutHeaderInt1.Select(b => $"{b:X2}"))}", Background = Brushes.White });
                    GraphicsButton loadButton = new() { Content = "Load Layout", Graphic = selectedFile };
                    loadButton.Click += LoadButton_Click;

                    graphicsEditStackPanel.Children.Add(loadButton);

                    Grid grid = new();
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Name = "U1" });
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Name = "IN" });
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Name = "U2" });
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Name = "CX" });
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Name = "CY" });
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Name = "CW" });
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Name = "CH" });
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Name = "IX" });
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Name = "IY" });
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Name = "IW" });
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Name = "IH" });
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Name = "U3" });
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Name = "AT" });
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Name = "RT" });
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Name = "GT" });
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Name = "BT" });

                    grid.RowDefinitions.Add(new RowDefinition() { Name = "labels" });
                    grid.Children.Add(new TextBlock { Text = "U1" });
                    grid.Children.Add(new TextBlock { Text = "IN" });
                    grid.Children.Add(new TextBlock { Text = "U2" });
                    grid.Children.Add(new TextBlock { Text = "CX" });
                    grid.Children.Add(new TextBlock { Text = "CY" });
                    grid.Children.Add(new TextBlock { Text = "CW" });
                    grid.Children.Add(new TextBlock { Text = "CH" });
                    grid.Children.Add(new TextBlock { Text = "IX" });
                    grid.Children.Add(new TextBlock { Text = "IY" });
                    grid.Children.Add(new TextBlock { Text = "IW" });
                    grid.Children.Add(new TextBlock { Text = "IH" });
                    grid.Children.Add(new TextBlock { Text = "U3" });
                    grid.Children.Add(new TextBlock { Text = "AT" });
                    grid.Children.Add(new TextBlock { Text = "RT" });
                    grid.Children.Add(new TextBlock { Text = "GT" });
                    grid.Children.Add(new TextBlock { Text = "BT" });

                    foreach (LayoutComponent mapComponent in selectedFile.LayoutComponents)
                    {
                        grid.RowDefinitions.Add(new RowDefinition());
                        grid.Children.Add(new TextBox { Text = $"{mapComponent.UnknownShort1}" });
                        grid.Children.Add(new TextBox { Text = $"{mapComponent.Index}" });
                        grid.Children.Add(new TextBox { Text = $"{mapComponent.UnknownShort2}" });
                        grid.Children.Add(new TextBox { Text = $"{mapComponent.ScreenX}" });
                        grid.Children.Add(new TextBox { Text = $"{mapComponent.ScreenY}" });
                        grid.Children.Add(new TextBox { Text = $"{mapComponent.ScreenWidth}" });
                        grid.Children.Add(new TextBox { Text = $"{mapComponent.ScreenHeight}" });
                        grid.Children.Add(new TextBox { Text = $"{mapComponent.ImageX}" });
                        grid.Children.Add(new TextBox { Text = $"{mapComponent.ImageY}" });
                        grid.Children.Add(new TextBox { Text = $"{mapComponent.ImageWidth}" });
                        grid.Children.Add(new TextBox { Text = $"{mapComponent.ImageHeight}" });
                        grid.Children.Add(new TextBox { Text = $"{mapComponent.UnknownShort3}" });
                        grid.Children.Add(new TextBox { Text = $"{mapComponent.AlphaTint}" });
                        grid.Children.Add(new TextBox { Text = $"{mapComponent.RedTint}" });
                        grid.Children.Add(new TextBox { Text = $"{mapComponent.GreenTint}" });
                        grid.Children.Add(new TextBox { Text = $"{mapComponent.BlueTint}" });
                    }

                    for (int i = 0; i < grid.Children.Count; i++)
                    {
                        Grid.SetRow(grid.Children[i], i / 16);
                        Grid.SetColumn(grid.Children[i], i % 16);
                    }

                    graphicsEditStackPanel.Children.Add(grid);
                }
                else if (selectedFile.FileType == GraphicsFile.GraphicsFileType.MAP)
                {
                    GraphicsButton mapButton = new() { Content = "Export to CSV", Graphic = selectedFile };
                    mapButton.Click += MapButton_Click;
                    graphicsEditStackPanel.Children.Add(mapButton);
                    graphicsEditStackPanel.Children.Add(new TextBlock { Text = $"Map Model: {selectedFile.MapModel}" });
                    graphicsEditStackPanel.Children.Add(new TextBlock { Text = $"Map Background Model: {selectedFile.MapBackgroundModel}" });
                }
            }
        }

        private void SgeButton_Click(object sender, RoutedEventArgs e)
        {
            GraphicsFile graphicsFile = ((GraphicsButton)sender).Graphic;

            SaveFileDialog saveFileDialog = new()
            {
                Filter = "SGE JSON file|*.sge.json"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllText(saveFileDialog.FileName, graphicsFile.Sge.DumpJson());
            }
        }

        private void StatsButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "CSV file|*.csv"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                List<SgeHeader> headers = new();
                foreach ((int parent, int child) in _mcb.GraphicsFiles)
                {
                    if (((GraphicsFile)_mcb.McbSubArchives[parent].Files[child]).FileType == GraphicsFile.GraphicsFileType.SGE)
                    {
                        headers.Add(((GraphicsFile)_mcb.McbSubArchives[parent].Files[child]).Sge.SgeHeader);
                    }
                }

                string csv = "Unknown00\n";
                string valueLine = string.Empty;
                string percentageLine = string.Empty;
                var unknown00HeaderValues = headers.GroupBy(h => h.Version).OrderByDescending(g => (double)g.Count() / headers.Count * 100);
                foreach (var header00 in unknown00HeaderValues)
                {
                    valueLine += $"{header00.Key},";
                    percentageLine += $"{(double)header00.Count() / headers.Count * 100:F2}%,";
                }
                csv += $"{valueLine}\n{percentageLine}\n\n";

                csv += "Unknown02\n";
                valueLine = string.Empty;
                percentageLine = string.Empty;
                var unknown02HeaderValues = headers.GroupBy(h => h.ModelType).OrderByDescending(g => (double)g.Count() / headers.Count * 100);
                foreach (var header02 in unknown02HeaderValues)
                {
                    valueLine += $"{header02.Key},";
                    percentageLine += $"{(double)header02.Count() / headers.Count * 100:F2}%,";
                }
                csv += $"{valueLine}\n{percentageLine}\n\n";

                File.WriteAllText(saveFileDialog.FileName, csv);

                csv += "Unknown04\n";
                valueLine = string.Empty;
                percentageLine = string.Empty;
                var unknown04HeaderValues = headers.GroupBy(h => h.Unknown04).OrderByDescending(g => (double)g.Count() / headers.Count * 100);
                foreach (var header04 in unknown04HeaderValues)
                {
                    valueLine += $"{header04.Key},";
                    percentageLine += $"{(double)header04.Count() / headers.Count * 100:F2}%,";
                }
                csv += $"{valueLine}\n{percentageLine}\n\n";

                csv += "Unknown08\n";
                valueLine = string.Empty;
                percentageLine = string.Empty;
                var unknown08HeaderValues = headers.GroupBy(h => h.Unknown08).OrderByDescending(g => (double)g.Count() / headers.Count * 100);
                foreach (var header08 in unknown08HeaderValues)
                {
                    valueLine += $"{header08.Key},";
                    percentageLine += $"{(double)header08.Count() / headers.Count * 100:F2}%,";
                }
                csv += $"{valueLine}\n{percentageLine}\n\n";

                csv += "Unknown0C\n";
                valueLine = string.Empty;
                percentageLine = string.Empty;
                var unknown0CHeaderValues = headers.GroupBy(h => h.Unknown0C).OrderByDescending(g => (double)g.Count() / headers.Count * 100);
                foreach (var header0C in unknown0CHeaderValues)
                {
                    valueLine += $"{header0C.Key},";
                    percentageLine += $"{(double)header0C.Count() / headers.Count * 100:F2}%,";
                }
                csv += $"{valueLine}\n{percentageLine}\n\n";

                File.WriteAllText(saveFileDialog.FileName, csv);
            }
        }

        private void MapButton_Click(object sender, RoutedEventArgs e)
        {
            GraphicsFile map = ((GraphicsButton)sender).Graphic;
            string csv = string.Join("\n", map.MapEntries.Select(e => e.GetCsvLine()));
            csv = $"{MapEntry.GetCsvHeader()}{csv}";
            string mapFile = Path.Combine(Path.GetTempPath(), $"{map.Location.parent:D3}-{map.Location.child:D3}-map.csv");
            File.WriteAllText(mapFile, csv);
            new Process { StartInfo = new ProcessStartInfo(mapFile) { UseShellExecute = true } }.Start();
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            GraphicsButton mapButton = (GraphicsButton)sender;
            List<GraphicsFile> archiveGraphicsFiles;
            if (_mcb is not null)
            {
                if (mapButton.Graphic.Location.parent == 58) // main title screen
                {
                    archiveGraphicsFiles = KnownLayoutGraphicsSets.TitleScreenGraphics.Select(s => (GraphicsFile)_mcb.McbSubArchives[s.McbLocation.parent].Files[s.McbLocation.child]).ToList();
                }
                else if (mapButton.Graphic.Location.parent == 69) // special version screen
                {
                    archiveGraphicsFiles = KnownLayoutGraphicsSets.SpecialVersionGraphics.Select(s => (GraphicsFile)_mcb.McbSubArchives[s.McbLocation.parent].Files[s.McbLocation.child]).ToList();
                }
                else
                {
                    archiveGraphicsFiles = null;
                }
            }
            else
            {
                archiveGraphicsFiles = null;
            }
            MapPreviewWindow mapPreviewWindow = new() { Width = 640 };
            Task.Factory.StartNew(() => mapButton.Graphic.GetLayout(archiveGraphicsFiles)).ContinueWith(task => mapPreviewWindow.CompleteLayoutLoad(task.Result));
            mapPreviewWindow.Show();
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
                    SKBitmap bitmap = _loadedGraphicsFile.GetImage();
                    using FileStream fs = new(saveFileDialog.FileName, FileMode.Create);
                    bitmap.Encode(fs, SKEncodedImageFormat.Png, 300);
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
                    _loadedGraphicsFile.Set20AF30Image(SKBitmap.Decode(openFileDialog.FileName));
                    graphicsEditStackPanel.Children.Clear();
                    graphicsEditStackPanel.Children.Add(new TextBlock { Text = $"20AF30: {_loadedGraphicsFile.Mode}", Background = Brushes.White });
                    graphicsEditStackPanel.Children.Add(new Image { Source = GuiHelpers.GetBitmapImageFromBitmap(_loadedGraphicsFile.GetImage()), MaxWidth = _loadedGraphicsFile.Width });
                }
            }
        }

        private void ExportGraphicsFileButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "BIN file|*.bin"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllBytes(saveFileDialog.FileName, ((GraphicsFile)graphicsListBox.SelectedItem).Data.ToArray());
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

        private void SaveFontFileButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "BIN file|*.bin"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllBytes(saveFileDialog.FileName, _fontFile.GetBytes());
            }
        }

        private void ImportFontToMcbButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "BIN file|*.bin"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                byte[] data = File.ReadAllBytes(openFileDialog.FileName);
                _mcb.FontFile = new(data);
                _mcb.FontFile.CompressedData = data;
                fontListBox.ItemsSource = _mcb.FontFile.Characters;
            }
        }

        private void FontListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (fontListBox.SelectedIndex >= 0)
            {
                Character selectedCharacter = (Character)fontListBox.SelectedItem;
                fontEditStackPanel.Children.Clear();
                fontEditStackPanel.Children.Add(new TextBlock() { Text = selectedCharacter.GetCodepointsString() });
                fontEditStackPanel.Background = Brushes.Gray;
                fontEditStackPanel.Children.Add(new Image { Source = GuiHelpers.GetBitmapImageFromBitmap(selectedCharacter.GetImage()), MaxWidth = Character.SCALED_WIDTH });
            }
        }

        private void ReplaceFontFileButton_Click(object sender, RoutedEventArgs e)
        {
            //FontReplacementDialogBox fontReplacementDialogBox = new();
            //if (fontReplacementDialogBox.ShowDialog() == true)
            //{
            //    if (_fontFile is not null)
            //    {
            //        _fontFile.OverwriteFont(
            //            fontReplacementDialogBox.SelectedFontFamily,
            //            fontReplacementDialogBox.SelectedFontSize,
            //            fontReplacementDialogBox.StartingChar,
            //            fontReplacementDialogBox.EndingChar,
            //            fontReplacementDialogBox.SelectedEncoding);
            //    }
            //    else if (_mcb is not null)
            //    {
            //        _mcb.FontFile.OverwriteFont(
            //            fontReplacementDialogBox.SelectedFontFamily,
            //            fontReplacementDialogBox.SelectedFontSize,
            //            fontReplacementDialogBox.StartingChar,
            //            fontReplacementDialogBox.EndingChar,
            //            fontReplacementDialogBox.SelectedEncoding);
            //    }
            //}
        }

        private void OpenDataFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "DAT file|dat*.bin"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                _datFile = BinArchive<DataFile>.FromFile(openFileDialog.FileName);

                if (Path.GetFileName(openFileDialog.FileName) == "dat.bin")
                {
                    DataFile currentMapDefFile = _datFile.Files.First(f => f.Index == 58);
                    MapDefinitionsFile mapDefFile = currentMapDefFile.CastTo<MapDefinitionsFile>();
                    _datFile.Files[_datFile.Files.IndexOf(currentMapDefFile)] = mapDefFile;

                    DataFile currentCameraDataFile = _datFile.Files.First(f => f.Index == 36);
                    CameraDataFile cameraDataFile = currentCameraDataFile.CastTo<CameraDataFile>();
                    _datFile.Files[_datFile.Files.IndexOf(currentCameraDataFile)] = cameraDataFile;
                }

                dataListBox.ItemsSource = _datFile.Files;
                dataListBox.Items.Refresh();
            }
        }

        private void SaveDataFileButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "DAT file|dat*.bin"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllBytes(saveFileDialog.FileName, _datFile.GetBytes(out Dictionary<int, int> offsetAdjustments));
                string offsetAdjustmentsFile = "dat.bin";
                foreach (int originalOffset in offsetAdjustments.Keys)
                {
                    offsetAdjustmentsFile += $"\n{originalOffset},{offsetAdjustments[originalOffset]}";
                }
                File.WriteAllText($"{saveFileDialog.FileName}_offset_adjustments.csv", offsetAdjustmentsFile);
                MessageBox.Show("Save completed!");
            }
        }

        private void ExportDataFileButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "BIN file|*.bin"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllBytes(saveFileDialog.FileName, ((DataFile)dataListBox.SelectedItem).GetBytes());
            }
        }

        private void ImportDataFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "BIN file|*.bin"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                DataFile newDataFile = new();
                newDataFile.Initialize(File.ReadAllBytes(openFileDialog.FileName), _datFile.Files[dataListBox.SelectedIndex].Offset);
                newDataFile.Index = _datFile.Files[dataListBox.SelectedIndex].Index;
                _datFile.Files[dataListBox.SelectedIndex] = newDataFile;
                _datFile.Files[dataListBox.SelectedIndex].Edited = true;
                graphicsListBox.Items.Refresh();
            }
        }

        private void DataListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dataEditStackPanel.Children.Clear();
            if (dataListBox.SelectedIndex >= 0)
            {
                DataFile selectedFile = (DataFile)dataListBox.SelectedItem;
                dataEditStackPanel.Children.Add(new TextBlock { Text = $"{selectedFile.Data.Count} bytes" });
                dataEditStackPanel.Children.Add(new TextBlock { Text = $"Actual compressed length: {selectedFile.CompressedData.Length:X}; Calculated length: {selectedFile.Length:X}" });

                if (selectedFile.GetType() == typeof(MapDefinitionsFile))
                {
                    MapDefinitionButton mapDefButton = new() { Content = "Export to CSV", MapDefFile = (MapDefinitionsFile)selectedFile };
                    mapDefButton.Click += MapDefButton_Click;
                    dataEditStackPanel.Children.Add(mapDefButton);
                }

                if (selectedFile.GetType() == typeof(CameraDataFile))
                {
                    CameraDataButton camDataButton = new() { Content = "Export to CSV", CamDataFile = (CameraDataFile)selectedFile };
                    camDataButton.Click += CamDataButton_Click;
                    dataEditStackPanel.Children.Add(camDataButton);
                }
            }
        }

        private void MapDefButton_Click(object sender, RoutedEventArgs e)
        {
            MapDefinitionsFile mapDefFile = ((MapDefinitionButton)sender).MapDefFile;

            string tempFile = Path.Combine(Path.GetTempPath(), $"MapDefinitions.csv");
            File.WriteAllText(tempFile, mapDefFile.GetCsv());
            new Process { StartInfo = new ProcessStartInfo(tempFile) { UseShellExecute = true } }.Start();
        }

        private void CamDataButton_Click(object sender, RoutedEventArgs e)
        {
            CameraDataFile cameraDataFile = ((CameraDataButton)sender).CamDataFile;

            string tempFile = Path.Combine(Path.GetTempPath(), $"CameraData.csv");
            File.WriteAllText(tempFile, cameraDataFile.GetCsv());
            new Process { StartInfo = new ProcessStartInfo(tempFile) { UseShellExecute = true } }.Start();
        }
    }
}
