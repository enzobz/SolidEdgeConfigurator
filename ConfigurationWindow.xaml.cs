using System;
using System.Windows;
using SolidEdgeConfigurator.Models;
using SolidEdgeConfigurator.Services;
using Serilog;

namespace SolidEdgeConfigurator
{
    public partial class ConfigurationWindow : Window
    {
        private DatabaseService _dbService;

        public ConfigurationOption SelectedConfiguration { get; private set; }

        public ConfigurationWindow()
        {
            InitializeComponent();
            _dbService = new DatabaseService();
            
            // Set defaults
            ColumnsSizeCombo.SelectedIndex = 0;
            IPCombo.SelectedIndex = 0;
            VentilatedRoofCombo.SelectedIndex = 1;  // Default to "No"
            HBBCombo.SelectedIndex = 0;
            VBBCombo.SelectedIndex = 0;
            EarthCombo.SelectedIndex = 0;
            NeutralCombo.SelectedIndex = 0;

            UpdateSummary();
        }

        private void IP_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Auto-set ventilated roof based on IP rating
            string ip = (IPCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString();
            
            // IP54 and above require ventilated roof
            if (ip == "IP54" || ip == "IP42")
            {
                VentilatedRoofCombo.SelectedIndex = 0;  // Yes
                VentilatedRoofCombo.IsEnabled = false;
            }
            else
            {
                VentilatedRoofCombo.IsEnabled = true;
            }

            UpdateSummary();
        }

        private void UpdateSummary()
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

                SummaryText.Text = summary;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating summary");
            }
        }

        private string GetComboValue(System.Windows.Controls.ComboBox combo)
        {
            return (combo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString() ?? "Not selected";
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SelectedConfiguration = new ConfigurationOption
                {
                    ConfigName = $"{GetComboValue(ColumnsSizeCombo)}_{GetComboValue(IPCombo)}",
                    ColumnsSize = GetComboValue(ColumnsSizeCombo),
                    IP = GetComboValue(IPCombo),
                    VentilatedRoof = GetComboValue(VentilatedRoofCombo),
                    HBB = GetComboValue(HBBCombo),
                    VBB = GetComboValue(VBBCombo),
                    Earth = GetComboValue(EarthCombo),
                    Neutral = GetComboValue(NeutralCombo),
                    CreatedDate = DateTime.Now
                };

                Log.Information("Configuration selected: {ConfigName}", SelectedConfiguration.ConfigName);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error(ex, "Error in Next_Click");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}