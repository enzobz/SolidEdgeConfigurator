using System;
using System.Collections.Generic;
using System.Linq;
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
        private SolidEdgeService _solidEdgeService;
        private List<ComponentConfig> _currentComponents;
        private ComponentConfig _selectedComponent;

        public MainWindow()
        {
            InitializeComponent();
            InitializeService();
            Log("Application started");
        }

        /// <summary>
        /// Initialize the SolidEdge service
        /// </summary>
        private void InitializeService()
        {
            try
            {
                _solidEdgeService = new SolidEdgeService();
                _currentComponents = new List<ComponentConfig>();

                // Check if Solid Edge is available
                if (!_solidEdgeService.IsSolidEdgeAvailable())
                {
                    Log("WARNING: Solid Edge is not installed or not detected on this system");
                    StatusText.Text = "Solid Edge not detected";
                }
                else
                {
                    StatusText.Text = "Ready";
                }
            }
            catch (Exception ex)
            {
                Log($"ERROR: Failed to initialize service: {ex.Message}");
                StatusText.Text = "Initialization error";
            }
        }

        /// <summary>
        /// Event handler for browsing assemblies
        /// </summary>
        private void BrowseAssembliesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "Solid Edge Assembly Files (*.asm)|*.asm|All Files (*.*)|*.*",
                    Title = "Select Solid Edge Assembly Template"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string filePath = openFileDialog.FileName;
                    Log($"Selected template: {filePath}");
                    LoadAssembly(filePath);
                }
            }
            catch (Exception ex)
            {
                Log($"ERROR: Failed to browse for assembly: {ex.Message}");
                MessageBox.Show($"Error browsing for file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Load assembly and populate component list
        /// </summary>
        private void LoadAssembly(string filePath)
        {
            try
            {
                StatusText.Text = "Loading assembly...";
                Log($"Loading assembly: {filePath}");

                // Load the assembly
                bool success = _solidEdgeService.LoadAssembly(filePath);

                if (success)
                {
                    // Update UI
                    AssemblyPathTextBox.Text = filePath;

                    // Get components and populate list
                    _currentComponents = _solidEdgeService.GetComponents();
                    PopulateComponentList();

                    Log($"Assembly loaded successfully. Found {_currentComponents.Count} components");
                    StatusText.Text = $"Loaded {_currentComponents.Count} components";
                }
                else
                {
                    Log("ERROR: Failed to load assembly");
                    StatusText.Text = "Failed to load assembly";
                    MessageBox.Show("Failed to load assembly. Make sure Solid Edge is installed and the file is a valid assembly.", 
                        "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Log($"ERROR: Exception loading assembly: {ex.Message}");
                StatusText.Text = "Error loading assembly";
                MessageBox.Show($"Error loading assembly: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Populate the component list box
        /// </summary>
        private void PopulateComponentList()
        {
            ComponentListBox.Items.Clear();

            foreach (var component in _currentComponents)
            {
                ComponentListBox.Items.Add(component.ComponentName);
            }

            Log($"Component list populated with {_currentComponents.Count} items");
        }

        /// <summary>
        /// Event handler for selecting components
        /// </summary>
        private void ComponentSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ComponentListBox.SelectedItem == null)
                {
                    SelectedComponentText.Text = "";
                    VisibilityCheckBox.IsChecked = false;
                    VisibilityCheckBox.IsEnabled = false;
                    _selectedComponent = null;
                    return;
                }

                string selectedName = ComponentListBox.SelectedItem.ToString();
                _selectedComponent = _currentComponents.FirstOrDefault(c => c.ComponentName == selectedName);

                if (_selectedComponent != null)
                {
                    SelectedComponentText.Text = _selectedComponent.ComponentName;
                    VisibilityCheckBox.IsChecked = _selectedComponent.IsVisible;
                    VisibilityCheckBox.IsEnabled = true;

                    // Hook up checkbox event
                    VisibilityCheckBox.Checked -= VisibilityCheckBox_Changed;
                    VisibilityCheckBox.Unchecked -= VisibilityCheckBox_Changed;
                    VisibilityCheckBox.Checked += VisibilityCheckBox_Changed;
                    VisibilityCheckBox.Unchecked += VisibilityCheckBox_Changed;

                    Log($"Component selected: {_selectedComponent.ComponentName}");
                }
            }
            catch (Exception ex)
            {
                Log($"ERROR: Failed to handle component selection: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handler for visibility checkbox changes
        /// </summary>
        private void VisibilityCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedComponent == null)
                    return;

                bool isVisible = VisibilityCheckBox.IsChecked == true;
                
                // Update component visibility
                bool success = _solidEdgeService.ToggleComponentVisibility(_selectedComponent.ComponentName, isVisible);

                if (success)
                {
                    _selectedComponent.IsVisible = isVisible;
                    Log($"Component {_selectedComponent.ComponentName} visibility set to {isVisible}");
                    StatusText.Text = $"Component visibility updated";
                }
                else
                {
                    Log($"ERROR: Failed to update component visibility");
                    // Revert checkbox
                    VisibilityCheckBox.IsChecked = !isVisible;
                }
            }
            catch (Exception ex)
            {
                Log($"ERROR: Exception toggling visibility: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handler for generating assemblies
        /// </summary>
        private void GenerateAssembliesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate that an assembly is loaded
                if (string.IsNullOrWhiteSpace(AssemblyPathTextBox.Text))
                {
                    MessageBox.Show("Please load an assembly template first.", "No Template", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Get output path
                string outputPath = OutputPathTextBox.Text?.Trim();
                
                if (string.IsNullOrWhiteSpace(outputPath))
                {
                    // Suggest default output path
                    string templatePath = AssemblyPathTextBox.Text;
                    string directory = System.IO.Path.GetDirectoryName(templatePath);
                    string filename = System.IO.Path.GetFileNameWithoutExtension(templatePath);
                    outputPath = System.IO.Path.Combine(directory, $"{filename}_configured.asm");
                    OutputPathTextBox.Text = outputPath;
                }

                Log($"Generating assembly to: {outputPath}");
                StatusText.Text = "Generating assembly...";

                // Create configuration settings
                var settings = new ConfigurationSettings
                {
                    ConfigurationName = "Generated Configuration",
                    TemplatePath = AssemblyPathTextBox.Text,
                    OutputPath = outputPath,
                    ComponentConfigurations = _currentComponents.Select(c => new ComponentConfiguration()).ToList()
                };

                // Generate the assembly
                bool success = _solidEdgeService.GenerateAssembly(outputPath, settings);

                if (success)
                {
                    Log($"Assembly generated successfully: {outputPath}");
                    StatusText.Text = "Assembly generated successfully";
                    MessageBox.Show($"Assembly generated successfully!\n\nOutput: {outputPath}", 
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    Log("ERROR: Failed to generate assembly");
                    StatusText.Text = "Failed to generate assembly";
                    MessageBox.Show("Failed to generate assembly. Check the log for details.", 
                        "Generation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Log($"ERROR: Exception generating assembly: {ex.Message}");
                StatusText.Text = "Error generating assembly";
                MessageBox.Show($"Error generating assembly: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Logging functionality - writes to UI log list
        /// </summary>
        private void Log(string message)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                string logEntry = $"[{timestamp}] {message}";
                
                // Add to UI log
                Dispatcher.Invoke(() =>
                {
                    LogListBox.Items.Add(logEntry);
                    
                    // Auto-scroll to bottom
                    if (LogListBox.Items.Count > 0)
                    {
                        LogListBox.ScrollIntoView(LogListBox.Items[LogListBox.Items.Count - 1]);
                    }

                    // Limit log entries to prevent memory issues
                    if (LogListBox.Items.Count > 1000)
                    {
                        LogListBox.Items.RemoveAt(0);
                    }
                });

                // Also log to console/Serilog
                Console.WriteLine(logEntry);
            }
            catch (Exception ex)
            {
                // Fallback to console if UI logging fails
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
                Console.WriteLine($"Logging error: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleanup on window closing
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                Log("Application closing");
                _solidEdgeService?.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during cleanup: {ex.Message}");
            }

            base.OnClosing(e);
        }
    }
}