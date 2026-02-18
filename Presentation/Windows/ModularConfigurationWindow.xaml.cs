using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Serilog;
using SolidEdgeConfigurator.Application.DTOs;
using SolidEdgeConfigurator.Application.UseCases;
using SolidEdgeConfigurator.Infrastructure.Data;
using SolidEdgeConfigurator.Infrastructure.Repositories;
using SolidEdgeConfigurator.Services;

namespace SolidEdgeConfigurator.Presentation.Windows
{
    public partial class ModularConfigurationWindow : Window
    {
        private readonly DatabaseService _dbService;
        private readonly GetCategoriesWithOptionsUseCase _getCategoriesUseCase;
        private readonly BOMService _bomService;
        private List<CategoryDTO> _categories;
        private Dictionary<int, int> _selectedOptions; // CategoryId -> OptionId

        public BOMResult GeneratedBOM { get; private set; }

        public ModularConfigurationWindow()
        {
            InitializeComponent();
            
            _dbService = new DatabaseService();
            
            // Initialize repositories
            var categoryRepo = new CategoryRepository(_dbService.ConnectionString);
            var optionRepo = new OptionRepository(_dbService.ConnectionString);
            var moduleRepo = new ModuleRepository(_dbService.ConnectionString);
            var partRepo = new PartRepository(_dbService.ConnectionString);
            var optionModuleRepo = new OptionModuleRepository(_dbService.ConnectionString);
            var modulePartRepo = new ModulePartRepository(_dbService.ConnectionString);

            // Initialize use cases
            _getCategoriesUseCase = new GetCategoriesWithOptionsUseCase(categoryRepo, optionRepo);
            _bomService = new BOMService(optionRepo, moduleRepo, partRepo, optionModuleRepo, modulePartRepo);

            _selectedOptions = new Dictionary<int, int>();

            LoadCategories();
        }

        private void LoadCategories()
        {
            try
            {
                _categories = _getCategoriesUseCase.Execute();

                if (_categories.Count == 0)
                {
                    MessageBox.Show(
                        "No categories found in the database. Please seed sample data first.\n\n" +
                        "You can do this by clicking 'Seed Sample Data' in the main window.",
                        "No Data",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    this.Close();
                    return;
                }

                CategoriesPanel.Children.Clear();

                foreach (var category in _categories)
                {
                    // Create a group box for each category
                    var groupBox = new GroupBox
                    {
                        Header = category.Name,
                        Margin = new Thickness(0, 0, 0, 15),
                        Background = System.Windows.Media.Brushes.White,
                        Padding = new Thickness(10)
                    };

                    var stackPanel = new StackPanel();

                    // Add description if available
                    if (!string.IsNullOrEmpty(category.Description))
                    {
                        var descText = new TextBlock
                        {
                            Text = category.Description,
                            FontStyle = FontStyles.Italic,
                            Foreground = System.Windows.Media.Brushes.Gray,
                            Margin = new Thickness(0, 0, 0, 10),
                            TextWrapping = TextWrapping.Wrap
                        };
                        stackPanel.Children.Add(descText);
                    }

                    // Create radio buttons for options
                    foreach (var option in category.Options)
                    {
                        var radioButton = new RadioButton
                        {
                            Content = $"{option.Name} - {option.Description}",
                            GroupName = $"Category_{category.Id}",
                            Tag = new { CategoryId = category.Id, OptionId = option.Id },
                            Margin = new Thickness(0, 5, 0, 5),
                            IsChecked = option.IsDefault
                        };

                        radioButton.Checked += Option_Checked;

                        // Pre-select default option
                        if (option.IsDefault)
                        {
                            _selectedOptions[category.Id] = option.Id;
                        }

                        stackPanel.Children.Add(radioButton);
                    }

                    groupBox.Content = stackPanel;
                    CategoriesPanel.Children.Add(groupBox);
                }

                UpdateSummary();
                Log.Information("Loaded {CategoryCount} categories with options", _categories.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading categories");
                MessageBox.Show($"Error loading configuration data: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Option_Checked(object sender, RoutedEventArgs e)
        {
            var radioButton = sender as RadioButton;
            if (radioButton?.Tag != null)
            {
                dynamic tag = radioButton.Tag;
                int categoryId = tag.CategoryId;
                int optionId = tag.OptionId;

                _selectedOptions[categoryId] = optionId;
                UpdateSummary();
            }
        }

        private void UpdateSummary()
        {
            try
            {
                if (_categories == null || _categories.Count == 0)
                {
                    SummaryText.Text = "No configuration loaded";
                    return;
                }

                var summary = "";
                foreach (var category in _categories)
                {
                    if (_selectedOptions.ContainsKey(category.Id))
                    {
                        var optionId = _selectedOptions[category.Id];
                        var option = category.Options.FirstOrDefault(o => o.Id == optionId);
                        if (option != null)
                        {
                            summary += $"{category.Name}: {option.Name}\n";
                        }
                    }
                }

                SummaryText.Text = string.IsNullOrEmpty(summary) ? "No options selected" : summary;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating summary");
            }
        }

        private void GenerateBOM_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedOptions.Count == 0)
                {
                    MessageBox.Show("Please select at least one option.", "No Selection", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Generate configuration name
                var configName = $"Config_{DateTime.Now:yyyyMMdd_HHmmss}";

                // Get selected option IDs
                var selectedOptionIds = _selectedOptions.Values.ToList();

                // Generate BOM
                StatusText.Text = "Generating BOM...";
                Log.Information("Generating BOM with {OptionCount} selected options", selectedOptionIds.Count);

                GeneratedBOM = _bomService.GenerateBOM(selectedOptionIds, configName);

                // Display results
                StatusText.Text = $"BOM generated: {GeneratedBOM.UniquePartCount} unique parts, " +
                                 $"{GeneratedBOM.TotalItems} total items, ${GeneratedBOM.TotalCost:F2}";

                // Update BOM display
                DisplayBOM(GeneratedBOM);

                Log.Information("BOM generated successfully: {ConfigName}", configName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating BOM");
                MessageBox.Show($"Error generating BOM: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Error generating BOM";
            }
        }

        private void DisplayBOM(BOMResult bom)
        {
            BOMDataGrid.ItemsSource = bom.LineItems;
            
            // Show activated modules
            ModulesText.Text = $"Activated Modules:\n{string.Join("\n", bom.ActivatedModules)}";
        }

        private void ExportBOM_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (GeneratedBOM == null)
                {
                    MessageBox.Show("Please generate a BOM first.", "No BOM", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|HTML files (*.html)|*.html|All files (*.*)|*.*",
                    FileName = $"BOM_{GeneratedBOM.ConfigurationName}.csv",
                    DefaultExt = ".csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var extension = System.IO.Path.GetExtension(saveDialog.FileName).ToLower();
                    
                    if (extension == ".html")
                    {
                        _bomService.ExportToHTML(GeneratedBOM, saveDialog.FileName);
                    }
                    else
                    {
                        _bomService.ExportToCSV(GeneratedBOM, saveDialog.FileName);
                    }

                    MessageBox.Show($"BOM exported successfully to:\n{saveDialog.FileName}", 
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    Log.Information("BOM exported to: {FilePath}", saveDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error exporting BOM");
                MessageBox.Show($"Error exporting BOM: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
