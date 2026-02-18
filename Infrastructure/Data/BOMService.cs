using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;
using SolidEdgeConfigurator.Application.DTOs;
using SolidEdgeConfigurator.Application.UseCases;
using SolidEdgeConfigurator.Domain.Interfaces;

namespace SolidEdgeConfigurator.Infrastructure.Data
{
    /// <summary>
    /// Service for BOM generation and export (100% database-based, no CAD dependency)
    /// </summary>
    public class BOMService
    {
        private readonly GenerateBOMUseCase _generateBOMUseCase;

        public BOMService(
            IOptionRepository optionRepository,
            IModuleRepository moduleRepository,
            IPartRepository partRepository,
            IOptionModuleRepository optionModuleRepository,
            IModulePartRepository modulePartRepository)
        {
            _generateBOMUseCase = new GenerateBOMUseCase(
                optionRepository,
                moduleRepository,
                partRepository,
                optionModuleRepository,
                modulePartRepository);
        }

        /// <summary>
        /// Generate BOM from selected options
        /// </summary>
        public BOMResult GenerateBOM(List<int> selectedOptionIds, string configurationName)
        {
            return _generateBOMUseCase.Execute(selectedOptionIds, configurationName);
        }

        /// <summary>
        /// Export BOM to CSV file
        /// </summary>
        public void ExportToCSV(BOMResult bom, string filePath)
        {
            try
            {
                using (var writer = new StreamWriter(filePath))
                {
                    // Header
                    writer.WriteLine($"Bill of Materials - {bom.ConfigurationName}");
                    writer.WriteLine($"Generated: {bom.GeneratedDate:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine();
                    writer.WriteLine($"Selected Options: {string.Join(", ", bom.SelectedOptions)}");
                    writer.WriteLine($"Activated Modules: {string.Join(", ", bom.ActivatedModules)}");
                    writer.WriteLine();

                    // Column headers
                    writer.WriteLine("Part Code,Part Name,Part Number,Description,Quantity,Unit,Unit Price,Total Price,Supplier,Source Modules");

                    // Line items
                    foreach (var item in bom.LineItems)
                    {
                        writer.WriteLine($"\"{item.PartCode}\",\"{item.PartName}\",\"{item.PartNumber}\",\"{item.Description}\",{item.TotalQuantity},{item.Unit},{item.UnitPrice:F2},{item.TotalPrice:F2},\"{item.Supplier}\",\"{string.Join("; ", item.SourceModules)}\"");
                    }

                    // Summary
                    writer.WriteLine();
                    writer.WriteLine($"Summary:");
                    writer.WriteLine($"Unique Parts: {bom.UniquePartCount}");
                    writer.WriteLine($"Total Items: {bom.TotalItems}");
                    writer.WriteLine($"Total Cost: ${bom.TotalCost:F2}");
                }

                Log.Information("✓ BOM exported to CSV: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error exporting BOM to CSV: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Export BOM to HTML file (for better visualization)
        /// </summary>
        public void ExportToHTML(BOMResult bom, string filePath)
        {
            try
            {
                using (var writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("<!DOCTYPE html>");
                    writer.WriteLine("<html>");
                    writer.WriteLine("<head>");
                    writer.WriteLine("<title>BOM - " + bom.ConfigurationName + "</title>");
                    writer.WriteLine("<style>");
                    writer.WriteLine("body { font-family: Arial, sans-serif; margin: 20px; }");
                    writer.WriteLine("h1 { color: #333; }");
                    writer.WriteLine("table { border-collapse: collapse; width: 100%; margin-top: 20px; }");
                    writer.WriteLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
                    writer.WriteLine("th { background-color: #4CAF50; color: white; }");
                    writer.WriteLine("tr:nth-child(even) { background-color: #f2f2f2; }");
                    writer.WriteLine(".summary { margin-top: 20px; padding: 15px; background-color: #f9f9f9; border-left: 4px solid #4CAF50; }");
                    writer.WriteLine("</style>");
                    writer.WriteLine("</head>");
                    writer.WriteLine("<body>");
                    
                    writer.WriteLine($"<h1>Bill of Materials - {bom.ConfigurationName}</h1>");
                    writer.WriteLine($"<p><strong>Generated:</strong> {bom.GeneratedDate:yyyy-MM-dd HH:mm:ss}</p>");
                    writer.WriteLine($"<p><strong>Selected Options:</strong> {string.Join(", ", bom.SelectedOptions)}</p>");
                    writer.WriteLine($"<p><strong>Activated Modules:</strong> {string.Join(", ", bom.ActivatedModules)}</p>");

                    writer.WriteLine("<table>");
                    writer.WriteLine("<tr><th>Part Code</th><th>Part Name</th><th>Part Number</th><th>Description</th><th>Qty</th><th>Unit</th><th>Unit Price</th><th>Total Price</th><th>Supplier</th><th>Source Modules</th></tr>");

                    foreach (var item in bom.LineItems)
                    {
                        writer.WriteLine($"<tr><td>{item.PartCode}</td><td>{item.PartName}</td><td>{item.PartNumber}</td><td>{item.Description}</td><td>{item.TotalQuantity}</td><td>{item.Unit}</td><td>${item.UnitPrice:F2}</td><td>${item.TotalPrice:F2}</td><td>{item.Supplier}</td><td>{string.Join(", ", item.SourceModules)}</td></tr>");
                    }

                    writer.WriteLine("</table>");

                    writer.WriteLine("<div class='summary'>");
                    writer.WriteLine($"<h3>Summary</h3>");
                    writer.WriteLine($"<p><strong>Unique Parts:</strong> {bom.UniquePartCount}</p>");
                    writer.WriteLine($"<p><strong>Total Items:</strong> {bom.TotalItems}</p>");
                    writer.WriteLine($"<p><strong>Total Cost:</strong> ${bom.TotalCost:F2}</p>");
                    writer.WriteLine("</div>");

                    writer.WriteLine("</body>");
                    writer.WriteLine("</html>");
                }

                Log.Information("✓ BOM exported to HTML: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error exporting BOM to HTML: {Message}", ex.Message);
                throw;
            }
        }
    }
}
