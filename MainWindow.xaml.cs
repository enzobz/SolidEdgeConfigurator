using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SolidEdgeConfigurator.Models;
using SolidEdgeConfigurator.Services;
using Serilog;

namespace SolidEdgeConfigurator
{
    public partial class MainWindow : Window
    {
        private SolidEdgeService _seService;
        private DatabaseService _dbService;
        private PartHierarchyService _partHierarchyService;
        private List<string> _availableComponents;
        private Dictionary<string, ComponentConfig> _componentSettings;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                _availableComponents = new List<string>();
                _componentSettings = new Dictionary<string, ComponentConfig>();
                _dbService = new DatabaseService();

                StatusText.Text = "Initializing Solid Edge...";

                try
                {
                    _seService = new SolidEdgeService();
                    StatusText.Text = "✓ Ready";
                    LogMessage("Solid Edge initialized successfully", "Success");
                }
                catch (Exception ex)
                {
                    StatusText.Text = $"⚠ Solid Edge not available: {ex.Message}";
                    _seService = null;
                    LogMessage($"Solid Edge initialization warning: {ex.Message}", "Warning");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize window: {ex.Message}\n\nStack: {ex.StackTrace}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private void BrowseAssembliesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Use OpenFileDialog to browse for a folder
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Select folder containing .asm files",
                    Filter = "All Files (*.*)|*.*",
                    CheckFileExists = false,
                    CheckPathExists = true
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string selectedPath = System.IO.Path.GetDirectoryName(openFileDialog.FileName);
                    
                    // Initialize the hierarchy service
                    _partHierarchyService = new PartHierarchyService(selectedPath, _seService, _dbService);
                    
                    LogMessage("Scanning .asm files...", "Info");
                    StatusText.Text = "Scanning assembly files...";
                    
                    // Extract all parts from .asm files
                    var extractedParts = _partHierarchyService.ExtractAllParts();
                    
                    var stats = _partHierarchyService.GetStatistics();
                    
                    Log.Information("Extracted {PartCount} parts from {AssemblyCount} assemblies", 
                        stats.TotalParts, stats.UniqueAssemblies);
                    
                    StatusText.Text = $"✓ Found {stats.TotalParts} parts in {stats.UniqueAssemblies} assemblies";
                    LogMessage($"Found {stats.TotalParts} parts in {stats.UniqueAssemblies} assemblies", "Success");
                    
                    // Ask to import
                    var result = MessageBox.Show(
                        $"Found {stats.TotalParts} parts in {stats.UniqueAssemblies} assemblies.\n\n" +
                        "Would you like to import these to the Parts Database?",
                        "Import Parts",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        int imported = _partHierarchyService.ImportPartsToDatabase();
                        MessageBox.Show($"✓ Successfully imported {imported} parts to database!", 
                            "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                        StatusText.Text = $"✓ Imported {imported} parts to database";
                        LogMessage($"Imported {imported} parts to database", "Success");
                    }

                    AssemblyPathTextBox.Text = selectedPath;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                LogMessage($"Error scanning assemblies: {ex.Message}", "Error");
                Log.Error(ex, "Error in BrowseAssembliesButton_Click: {Message}", ex.Message);
            }
        }

        private void BrowseOutput_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Solid Edge Assembly (*.asm)|*.asm|All Files (*.*)|*.*",
                    Title = "Save Generated Assembly As",
                    DefaultExt = ".asm"
                };

                if (!string.IsNullOrEmpty(AssemblyPathTextBox.Text))
                {
                    var originalFilename = System.IO.Path.GetFileNameWithoutExtension(AssemblyPathTextBox.Text);
                    saveFileDialog.FileName = originalFilename + "_configured.asm";
                }

                if (saveFileDialog.ShowDialog() == true)
                {
                    OutputPathTextBox.Text = saveFileDialog.FileName;
                    LogMessage($"Output path set: {saveFileDialog.FileName}", "Info");
                    StatusText.Text = "Output path configured";
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error browsing output: {ex.Message}", "Error");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateAssembliesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_seService == null)
                {
                    MessageBox.Show("Solid Edge service is not available.", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrEmpty(AssemblyPathTextBox.Text))
                {
                    MessageBox.Show("Please load a template assembly first.", "Missing Input", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(OutputPathTextBox.Text))
                {
                    MessageBox.Show("Please specify an output file path.", "Missing Input", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                StatusText.Text = "Generating assembly...";
                LogMessage("Starting assembly generation...", "Info");

                var config = new ConfigurationSettings
                {
                    TemplatePath = AssemblyPathTextBox.Text,
                    OutputPath = OutputPathTextBox.Text,
                    ConfigurationName = System.IO.Path.GetFileNameWithoutExtension(OutputPathTextBox.Text)
                };

                foreach (var componentSetting in _componentSettings.Values)
                {
                    config.ComponentConfigs.Add(componentSetting);
                }

                _seService.ApplyConfiguration(config);
                LogMessage("Configuration applied to Solid Edge", "Success");

                _seService.SaveAssemblyAs(config.OutputPath);
                LogMessage($"Assembly saved: {config.OutputPath}", "Success");

                StatusText.Text = "✓ Assembly generated successfully!";
                MessageBox.Show($"Assembly generated successfully!\n\nLocation:\n{config.OutputPath}", 
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"Error generating assembly: {ex.Message}", "Error");
                StatusText.Text = "Error generating assembly";
                MessageBox.Show($"Error: {ex.Message}", "Generation Failed", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LogMessage(string message, string level)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string logEntry = $"[{timestamp}] [{level}] {message}";
            LogListBox.Items.Insert(0, logEntry);

            while (LogListBox.Items.Count > 100)
            {
                LogListBox.Items.RemoveAt(LogListBox.Items.Count - 1);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _seService?.Dispose();
            _dbService?.Dispose();
        }

        private void OpenPartsManagement_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var partsWindow = new PartsManagementWindow(_availableComponents, _seService);
                partsWindow.ShowDialog();
                LogMessage("Parts Management window opened", "Info");
            }
            catch (Exception ex)
            {
                LogMessage($"Error opening Parts Management: {ex.Message}", "Error");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ToggleSolidEdge_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_seService == null)
                {
                    MessageBox.Show("Solid Edge service is not available.", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _seService.ToggleSolidEdgeVisibility();
                UpdateToggleButtonText();
                LogMessage($"Solid Edge {(_seService.IsSolidEdgeVisible() ? "shown" : "hidden")}", "Info");
            }
            catch (Exception ex)
            {
                LogMessage($"Error toggling Solid Edge: {ex.Message}", "Error");
            }
        }

        private void UpdateToggleButtonText()
        {
            if (_seService != null)
            {
                ToggleSolidEdgeButton.Content = _seService.IsSolidEdgeVisible() 
                    ? "Hide Solid Edge" 
                    : "Show Solid Edge";
            }
        }

        private void OpenConfigurationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var configWindow = new ConfigurationWindow();
                if (configWindow.ShowDialog() == true)
                {
                    var config = configWindow.SelectedConfiguration;
                    MessageBox.Show($"Selected Configuration:\n{config.ConfigName}", "Configuration", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    LogMessage($"Configuration selected: {config.ConfigName}", "Success");
                    
                    // Get parts for this configuration
                    var parts = _dbService.GetPartsByConfiguration(config.ConfigName);
                    StatusText.Text = $"Configuration: {config.ConfigName} ({parts.Count} parts)";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error(ex, "Error opening configuration window");
            }
        }

        private int _currentTab = 0;

        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            int tabIndex = int.Parse(button.Tag.ToString());
            
            // Hide all tabs
            SpecsTab.Visibility = Visibility.Hidden;
            LoadTab.Visibility = Visibility.Hidden;
            ConfigTab.Visibility = Visibility.Hidden;
            GenerateTab.Visibility = Visibility.Hidden;
            LogPanel.Visibility = Visibility.Collapsed;

            // Reset tab button colors
            TabSpecs.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(102, 187, 106));
            TabLoad.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(102, 187, 106));
            TabConfig.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(102, 187, 106));
            TabGenerate.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(102, 187, 106));

            // Show selected tab
            switch (tabIndex)
            {
                case 0:
                    SpecsTab.Visibility = Visibility.Visible;
                    TabSpecs.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80));
                    break;
                case 1:
                    LoadTab.Visibility = Visibility.Visible;
                    TabLoad.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80));
                    break;
                case 2:
                    ConfigTab.Visibility = Visibility.Visible;
                    TabConfig.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80));
                    UpdateConfigSummary();
                    break;
                case 3:
                    GenerateTab.Visibility = Visibility.Visible;
                    TabGenerate.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80));
                    break;
            }

            _currentTab = tabIndex;
        }

        private void UpdateConfigSummary()
        {
            try
            {
                string summary = $"Columns: {GetComboValue(ColumnsSizeCombo)}\n" +
                                $"IP Rating: {GetComboValue(IPCombo)}\n" +
                                $"Ventilated Roof: {GetComboValue(VentilatedRoofCombo)}\n" +
                                $"HBB: {GetComboValue(HBBCombo)}\n" +
                                $"VBB: {GetComboValue(VBBCombo)}\n" +
                                $"Earth: {GetComboValue(EarthCombo)}\n" +
                                $"Neutral: {GetComboValue(NeutralCombo)}";

                ConfigSummaryText.Text = summary;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating config summary");
            }
        }

        private string GetComboValue(ComboBox combo)
        {
            return (combo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Not selected";
        }

        private void IP_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            string ip = GetComboValue(IPCombo);
            
            if (ip == "IP54" || ip == "IP42")
            {
                VentilatedRoofCombo.SelectedIndex = 0;  // Yes
                VentilatedRoofCombo.IsEnabled = false;
            }
            else
            {
                VentilatedRoofCombo.IsEnabled = true;
            }

            UpdateConfigSummary();
        }
        
        private void OnConfigurationChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateConfigSummary();
        }
    }
}