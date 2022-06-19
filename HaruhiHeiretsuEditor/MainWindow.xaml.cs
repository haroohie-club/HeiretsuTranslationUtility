﻿using FolderBrowserEx;
using HaruhiHeiretsuLib;
using HaruhiHeiretsuLib.Graphics;
using HaruhiHeiretsuLib.Strings;
using Microsoft.Win32;
using plugin_shade.Archives;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private McbFile _mcb;
        private ArchiveFile<DataFile> _datFile;
        private ArchiveFile<ShadeStringsFile> _datStringsFile;
        private ArchiveFile<EventFile> _evtFile;
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

                using Stream archiveStream = _mcb.ArchiveFiles[75].GetFileData().GetAwaiter().GetResult();
                BlnSub blnSub = new();
                BlnSubArchiveFileInfo blnSubFile = (BlnSubArchiveFileInfo)blnSub.GetFile(archiveStream, 2);
                byte[] commandsFileData = blnSubFile.GetFileDataBytes();

                _mcb.LoadStringsFiles(File.ReadAllText("string_file_locations.csv"), ScriptCommand.ParseScriptCommandFile(commandsFileData));
                _mcb.LoadGraphicsFiles(File.ReadAllText("graphics_locations.csv"));
                _mcb.LoadFontFile();
                scriptsListBox.ItemsSource = _mcb.StringsFiles;
                graphicsListBox.ItemsSource = _mcb.GraphicsFiles;
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
                    _mcb.Save(saveFileDialog.FileName, saveFileDialog.FileName.Replace("0", "1")).GetAwaiter().GetResult();
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
                        _mcb.AdjustOffsets(mcbSaveFileDialog.FileName, mcbSaveFileDialog.FileName.Replace("0", "1"), offsetOpenFileDialog.FileName).GetAwaiter().GetResult();
                    }
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
                _scrFile = ArchiveFile<ScriptFile>.FromFile(openFileDialog.FileName);
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
                _datStringsFile = ArchiveFile<ShadeStringsFile>.FromFile(openFileDialog.FileName);
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
                _evtFile = ArchiveFile<EventFile>.FromFile(openFileDialog.FileName);
                scriptsListBox.ItemsSource = _evtFile.Files;
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
                shadeStringsFile.Location = _mcb.StringsFiles[scriptsListBox.SelectedIndex].Location;
                _mcb.StringsFiles[scriptsListBox.SelectedIndex] = shadeStringsFile;
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
                _mcb.StringsFiles[scriptsListBox.SelectedIndex] = scriptFile;
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
                scriptEditStackPanel.Children.Add(new TextBlock { Text = $"{_mcb.StringsFiles.Sum(s => s.DialogueLines.Count)}" });
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
                _grpFile = ArchiveFile<GraphicsFile>.FromFile(openFileDialog.FileName);
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
                    graphicsEditStackPanel.Children.Add(new TextBlock { Text = $"SGE {selectedFile.Data.Count} bytes", Background = Brushes.White });
                }
                else if (selectedFile.FileType == GraphicsFile.GraphicsFileType.TILE_20AF30)
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
                    MapButton loadButton = new() { Content = "Load Layout", Map = selectedFile };
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
                        grid.Children.Add(new TextBox { Text = $"{mapComponent.RelativeFileIndex}" });
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
                else if (selectedFile.FileType == GraphicsFile.GraphicsFileType.WORLD_DATA)
                {
                    for (int i = 0; i < 256; i++)
                    {
                        StackPanel stackPanel = new() { Orientation = Orientation.Horizontal };
                        stackPanel.Children.Add(new TextBlock { Text = $"{selectedFile.WorldDataModelNames[i]}: " });
                        stackPanel.Children.Add(new TextBox { Text = $"{string.Join(' ', selectedFile.WorldDataEntries[i].Select(b => $"{b:X2}"))}" });

                        graphicsEditStackPanel.Children.Add(stackPanel);
                    }
                }
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            MapButton mapButton = (MapButton)sender;
            //Dictionary<int, GraphicsFile> archiveGraphicsFiles = _mcb.GraphicsFiles.Where(g => g.Location.parent == selectedFile.Location.parent).ToDictionary(g => g.Location.child);
            List<GraphicsFile> archiveGraphicsFiles = new();
            if (mapButton.Map.Location == (58, 57))
            {
                archiveGraphicsFiles.Add(_mcb.GraphicsFiles.First(g => g.Location == (0, 12)));
                archiveGraphicsFiles.Add(_mcb.GraphicsFiles.First(g => g.Location == (0, 12)));
                archiveGraphicsFiles.Add(_mcb.GraphicsFiles.First(g => g.Location == (0, 42)));
                archiveGraphicsFiles.Add(_mcb.GraphicsFiles.First(g => g.Location == (0, 42)));
                archiveGraphicsFiles.Add(_mcb.GraphicsFiles.First(g => g.Location == (58, 0)));
                archiveGraphicsFiles.Add(_mcb.GraphicsFiles.First(g => g.Location == (58, 55)));
                archiveGraphicsFiles.Add(_mcb.GraphicsFiles.First(g => g.Location == (69, 33)));
                archiveGraphicsFiles.Add(_mcb.GraphicsFiles.First(g => g.Location == (0, 11)));
                archiveGraphicsFiles.Add(_mcb.GraphicsFiles.First(g => g.Location == (0, 26)));
            }
            else
            {
                archiveGraphicsFiles = _mcb.GraphicsFiles.Where(g => g.Location.parent == mapButton.Map.Location.parent).ToList();
            }
            MapPreviewWindow mapPreviewWindow = new(new Image { Source = GuiHelpers.GetBitmapImageFromBitmap(mapButton.Map.GetLayout(archiveGraphicsFiles)), MaxWidth = mapButton.Map.Width });
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
            if (_loadedGraphicsFile is not null)
            {
                SaveFileDialog saveFileDialog = new()
                {
                    Filter = "BIN file|*.bin"
                };
                if (saveFileDialog.ShowDialog() == true)
                {
                    File.WriteAllBytes(saveFileDialog.FileName, _loadedGraphicsFile.Data.ToArray());
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
            FontReplacementDialogBox fontReplacementDialogBox = new();
            if (fontReplacementDialogBox.ShowDialog() == true)
            {
                if (_fontFile is not null)
                {
                    _fontFile.OverwriteFont(
                        fontReplacementDialogBox.SelectedFontFamily,
                        fontReplacementDialogBox.SelectedFontSize,
                        fontReplacementDialogBox.StartingChar,
                        fontReplacementDialogBox.EndingChar,
                        fontReplacementDialogBox.SelectedEncoding);
                }
                else if (_mcb is not null)
                {
                    _mcb.FontFile.OverwriteFont(
                        fontReplacementDialogBox.SelectedFontFamily,
                        fontReplacementDialogBox.SelectedFontSize,
                        fontReplacementDialogBox.StartingChar,
                        fontReplacementDialogBox.EndingChar,
                        fontReplacementDialogBox.SelectedEncoding);
                }
            }
        }

        private void OpenDataFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "DAT file|dat*.bin"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                _datFile = ArchiveFile<DataFile>.FromFile(openFileDialog.FileName);
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
            }
        }
    }
}
