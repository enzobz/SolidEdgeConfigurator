using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using SolidEdgeConfigurator.Models;
using SolidEdgeConfigurator.Services;
using Serilog;

namespace SolidEdgeConfigurator
{
    public partial class PartsManagementWindow : Window
    {
        private DatabaseService _dbService;
        private SolidEdgeService _seService;
        private ObservableCollection<Part> _parts;

        public PartsManagementWindow(List<string> availableComponents = null, SolidEdgeService seService = null)
        {
            InitializeComponent();
            _dbService = new DatabaseService();
            _seService = seService;
            _parts = new ObservableCollection<Part>();
            
            // Bind to DataGrid
            PartsDataGrid.ItemsSource = _parts;
            
            // Populate component input with available components if provided
            if (availableComponents != null && availableComponents.Count > 0)
            {
                ComponentInput.Text = availableComponents[0];
            }

            // Load and display parts
            RefreshPartsList();
        }

        /// <summary>
        /// Refresh the parts list display
        /// </summary>
        private void RefreshPartsList()
        {
            try
            {
                _parts.Clear();
                var allParts = _dbService.GetAllParts();
                
                Log.Information("RefreshPartsList: Retrieved {Count} parts from database", allParts.Count);
                
                foreach (var part in allParts)
                {
                    _parts.Add(part);
                }

                StatusText.Text = $"✓ Loaded {_parts.Count} parts from database";
                Log.Information("Parts list refreshed: {Count} items", _parts.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading parts: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "✗ Error loading parts";
                Log.Error(ex, "Error in RefreshPartsList: {Message}", ex.Message);
            }
        }

        private void AddPart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(PartNameInput.Text))
                {
                    MessageBox.Show("Please enter a part name", "Validation", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!double.TryParse(UnitPriceInput.Text, out double price))
                {
                    price = 0.0;
                }

                if (!int.TryParse(QuantityInput.Text, out int quantity))
                {
                    quantity = 1;
                }

                var part = new Part
                {
                    PartName = PartNameInput.Text,
                    PartNumber = PartNumberInput.Text,
                    ComponentName = ComponentInput.Text,
                    UnitPrice = price,
                    Quantity = quantity,
                    Unit = UnitInput.Text,
                    Supplier = SupplierInput.Text,
                    Description = DescriptionInput.Text
                };

                _dbService.AddPart(part);
                Log.Information("Part added to database: {PartName}", part.PartName);
                
                // Add to observable collection immediately
                _parts.Add(part);
                
                // Clear inputs
                PartNameInput.Clear();
                PartNumberInput.Clear();
                ComponentInput.Clear();
                UnitPriceInput.Clear();
                QuantityInput.Text = "1";
                UnitInput.Text = "pcs";
                SupplierInput.Clear();
                DescriptionInput.Clear();

                StatusText.Text = $"✓ Part '{part.PartName}' added - Total: {_parts.Count} parts";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding part: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = $"✗ Error: {ex.Message}";
                Log.Error(ex, "Error in AddPart_Click");
            }
        }

        private void DeletePart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (PartsDataGrid.SelectedItem is Part selectedPart)
                {
                    var result = MessageBox.Show($"Delete '{selectedPart.PartName}'?", "Confirm", 
                        MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        _dbService.DeletePart(selectedPart.Id);
                        _parts.Remove(selectedPart);
                        StatusText.Text = $"✓ Part deleted - Total: {_parts.Count} parts";
                        Log.Information("Part deleted: {PartName}", selectedPart.PartName);
                    }
                }
                else
                {
                    MessageBox.Show("Please select a part to delete", "Selection", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting part: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error(ex, "Error in DeletePart_Click");
            }
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int updatedCount = 0;

                foreach (var part in _parts)
                {
                    if (part.Id > 0)  // Only save existing parts
                    {
                        _dbService.UpdatePart(part);
                        updatedCount++;
                    }
                }

                StatusText.Text = $"✓ Saved {updatedCount} changes to database";
                MessageBox.Show($"Successfully saved {updatedCount} part(s) changes!", 
                    "Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                Log.Information("Saved {UpdatedCount} part changes", updatedCount);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving changes: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error(ex, "Error in SaveChanges_Click");
            }
        }

        private void ImportFromAssembly_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_seService == null)
                {
                    MessageBox.Show("No Solid Edge service available. Please load an assembly first.", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var componentDetails = _seService.GetComponentDetails();
                Log.Information("ImportFromAssembly: Retrieved {Count} components", componentDetails.Count);

                if (componentDetails.Count == 0)
                {
                    MessageBox.Show("No components found in the assembly.", 
                        "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Get all existing parts from database for lookup
                var existingParts = _dbService.GetAllParts();
                int importedCount = 0;
                int matchedCount = 0;

                foreach (var component in componentDetails)
                {
                    try
                    {
                        // Check if component already exists in current list
                        if (_parts.Any(p => p.ComponentName == component.ComponentName))
                        {
                            Log.Information("Component already in current list: {ComponentName}", component.ComponentName);
                            continue;
                        }

                        var part = new Part
                        {
                            PartName = component.ComponentName,
                            PartNumber = component.PartNumber,
                            ComponentName = component.ComponentName,
                            UnitPrice = 0.0,
                            Quantity = 1,
                            Unit = "pcs",
                            Supplier = "",
                            Description = component.Description
                        };

                        // 🔍 Try to find matching part in database by name
                        var matchingPart = existingParts.FirstOrDefault(p => 
                            p.PartName.Equals(component.ComponentName, StringComparison.OrdinalIgnoreCase) ||
                            p.ComponentName.Equals(component.ComponentName, StringComparison.OrdinalIgnoreCase));

                        if (matchingPart != null)
                        {
                            // Found a match! Copy information from database
                            part.PartNumber = !string.IsNullOrEmpty(matchingPart.PartNumber) ? matchingPart.PartNumber : part.PartNumber;
                            part.UnitPrice = matchingPart.UnitPrice;
                            part.Quantity = matchingPart.Quantity;
                            part.Unit = matchingPart.Unit;
                            part.Supplier = matchingPart.Supplier;
                            part.Description = matchingPart.Description;
                            
                            matchedCount++;
                            Log.Information("Matched component from database: {ComponentName} - Price: ${Price}, Supplier: {Supplier}", 
                                component.ComponentName, matchingPart.UnitPrice, matchingPart.Supplier);
                        }
                        else
                        {
                            Log.Information("No matching part in database for: {ComponentName}", component.ComponentName);
                        }

                        // Add to database
                        _dbService.AddPart(part);
                        // Add to UI collection
                        _parts.Add(part);
                        importedCount++;
                        
                        Log.Information("Imported component: {PartName} (Matched: {IsMatched})", 
                            part.PartName, matchingPart != null ? "Yes" : "No");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error importing component: {Message}", ex.Message);
                    }
                }

                StatusText.Text = $"✓ Imported {importedCount} components ({matchedCount} matched from database) - Total: {_parts.Count} parts";
                
                string message = $"Successfully imported {importedCount} components!\n\n";
                message += $"📊 Matched from database: {matchedCount}\n";
                message += $"🆕 New parts: {importedCount - matchedCount}\n\n";
                message += "✏️ Edit missing information in the table\n";
                message += "💾 Click 'Save Changes' when done";
                
                MessageBox.Show(message, "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                Log.Information("Import completed: {ImportedCount} parts imported, {MatchedCount} matched from database", importedCount, matchedCount);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing components: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = $"✗ Error: {ex.Message}";
                Log.Error(ex, "Error in ImportFromAssembly_Click");
            }
        }

        private void ExportBOM_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_parts.Count == 0)
                {
                    MessageBox.Show("No parts to export. Please add parts first.", 
                        "Empty BOM", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv",
                    DefaultExt = ".csv",
                    FileName = $"BOM_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var bom = new BOM
                    {
                        ConfigurationName = "Parts Export",
                        CreatedDate = DateTime.Now,
                        Parts = _parts.ToList()
                    };

                    _dbService.ExportBOMToCSV(bom, saveFileDialog.FileName);
                    StatusText.Text = $"✓ BOM exported successfully";
                    MessageBox.Show($"BOM exported successfully!\n\nLocation:\n{saveFileDialog.FileName}\n\nTotal Parts: {_parts.Count}\nTotal Cost: ${bom.TotalCost:F2}", 
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    Log.Information("BOM exported: {FilePath} - {PartCount} parts - ${TotalCost}", 
                        saveFileDialog.FileName, _parts.Count, bom.TotalCost);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting BOM: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error(ex, "Error in ExportBOM_Click");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                Log.Information("Parts Management window closing - {PartCount} parts in database", _parts.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during window closing");
            }
        }

        /// <summary>
        /// Match parts with existing database entries and fill missing information
        /// </summary>
        private void MatchAndFill_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var existingParts = _dbService.GetAllParts();
                int filledCount = 0;

                foreach (var currentPart in _parts)
                {
                    // Skip if already has price
                    if (currentPart.UnitPrice > 0)
                        continue;

                    // Try to match by part name
                    var matchingPart = existingParts.FirstOrDefault(p =>
                        p.PartName.Equals(currentPart.PartName, StringComparison.OrdinalIgnoreCase) ||
                        p.ComponentName.Equals(currentPart.ComponentName, StringComparison.OrdinalIgnoreCase));

                    if (matchingPart != null && matchingPart.Id != currentPart.Id)
                    {
                        // Fill in missing information
                        if (currentPart.UnitPrice == 0)
                            currentPart.UnitPrice = matchingPart.UnitPrice;
                        
                        if (string.IsNullOrEmpty(currentPart.Supplier))
                            currentPart.Supplier = matchingPart.Supplier;
                        
                        if (string.IsNullOrEmpty(currentPart.PartNumber))
                            currentPart.PartNumber = matchingPart.PartNumber;
                        
                        if (string.IsNullOrEmpty(currentPart.Description))
                            currentPart.Description = matchingPart.Description;

                        filledCount++;
                        Log.Information("Matched and filled info for: {PartName}", currentPart.PartName);
                    }
                }

                // Refresh DataGrid
                PartsDataGrid.Items.Refresh();

                StatusText.Text = $"✓ Matched and filled information for {filledCount} parts";
                MessageBox.Show($"Successfully matched and filled information for {filledCount} parts!\n\nReview the changes and click 'Save Changes'",
                    "Match Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                Log.Information("Match and fill completed: {FilledCount} parts updated", filledCount);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error matching parts: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error(ex, "Error in MatchAndFill_Click");
            }
        }
    }
}