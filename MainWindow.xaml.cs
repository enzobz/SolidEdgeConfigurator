using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SolidEdgeConfigurator.Models;
using SolidEdgeConfigurator.Services;

namespace SolidEdgeConfigurator
{
    public partial class MainWindow : Window
    {
        private SolidEdgeService _seService;
        private List<string> _availableComponents;
        private Dictionary<string, ComponentConfig> _componentSettings;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                _availableComponents = new List<string>();
                _componentSettings = new Dictionary<string, ComponentConfig>();

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
                if (_seService == null)
                {
                    MessageBox.Show("Solid Edge service is not available.", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Solid Edge Assembly (*.asm)|*.asm|All Files (*.*)|*.*",
                    Title = "Select Template Assembly"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    AssemblyPathTextBox.Text = openFileDialog.FileName;
                    _seService.OpenAssembly(openFileDialog.FileName);
                    _availableComponents = _seService.GetComponentList();
                    UpdateComponentList();
                    StatusText.Text = $"Loaded: {System.IO.Path.GetFileName(openFileDialog.FileName)}";
                    LogMessage($"Assembly loaded: {openFileDialog.FileName}", "Success");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error loading assembly: {ex.Message}", "Error");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

                // Set default filename if available
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

        private void UpdateComponentList()
        {
            ComponentListBox.Items.Clear();
            foreach (var component in _availableComponents)
            {
                ComponentListBox.Items.Add(component);
                if (!_componentSettings.ContainsKey(component))
                {
                    _componentSettings[component] = new ComponentConfig
                    {
                        ComponentName = component,
                        IsVisible = true
                    };
                }
            }
            LogMessage($"Found {_availableComponents.Count} components", "Info");
        }

        private void ComponentSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComponentListBox.SelectedItem is string selectedComponent)
            {
                SelectedComponentText.Text = selectedComponent;
                if (_componentSettings.ContainsKey(selectedComponent))
                {
                    VisibilityCheckBox.IsChecked = _componentSettings[selectedComponent].IsVisible;
                }
            }
        }

        private void VisibilityCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (ComponentListBox.SelectedItem is string selectedComponent && VisibilityCheckBox.IsChecked.HasValue)
            {
                _componentSettings[selectedComponent].IsVisible = VisibilityCheckBox.IsChecked.Value;
                string visibility = VisibilityCheckBox.IsChecked.Value ? "VISIBLE" : "HIDDEN";
                LogMessage($"Component '{selectedComponent}' set to {visibility}", "Info");
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

            // Keep log size manageable
            while (LogListBox.Items.Count > 100)
            {
                LogListBox.Items.RemoveAt(LogListBox.Items.Count - 1);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _seService?.Dispose();
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
    }
}