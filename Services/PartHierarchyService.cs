using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;
using SolidEdgeConfigurator.Models;

namespace SolidEdgeConfigurator.Services
{
    /// <summary>
    /// Service for scanning .asm files and extracting parts
    /// </summary>
    public class PartHierarchyService
    {
        private readonly string _rootPath;
        private readonly SolidEdgeService _seService;
        private readonly DatabaseService _dbService;
        private List<Part> _extractedParts;

        public PartHierarchyService(string rootPath, SolidEdgeService seService, DatabaseService dbService = null)
        {
            _rootPath = rootPath;
            _seService = seService;
            _dbService = dbService ?? new DatabaseService();
            _extractedParts = new List<Part>();
        }

        /// <summary>
        /// Scan folder for all .asm files and extract parts from each
        /// </summary>
        public List<Part> ExtractAllParts()
        {
            _extractedParts.Clear();

            try
            {
                if (!Directory.Exists(_rootPath))
                {
                    Log.Error("Root path does not exist: {RootPath}", _rootPath);
                    return _extractedParts;
                }

                Log.Information("Scanning for .asm files in: {RootPath}", _rootPath);

                // Find all .asm files recursively
                var asmFiles = Directory.GetFiles(_rootPath, "*.asm", SearchOption.AllDirectories);
                
                Log.Information("Found {Count} .asm files", asmFiles.Length);

                foreach (var asmFile in asmFiles)
                {
                    ExtractPartsFromAssembly(asmFile);
                }

                Log.Information("✓ Extraction complete: Found {PartCount} unique parts", _extractedParts.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error extracting parts: {Message}", ex.Message);
            }

            return _extractedParts;
        }

        /// <summary>
        /// Extract parts from a single .asm file
        /// </summary>
        private void ExtractPartsFromAssembly(string asmFilePath)
        {
            try
            {
                Log.Information("Opening assembly: {AsmFile}", asmFilePath);

                // Load the assembly
                if (!_seService.LoadAssembly(asmFilePath))
                {
                    Log.Warning("Failed to load assembly: {AsmFile}", asmFilePath);
                    return;
                }

                // Get components from the assembly
                var components = _seService.GetComponentDetails();
                
                string asmName = System.IO.Path.GetFileNameWithoutExtension(asmFilePath);
                string relativeFolder = GetHierarchyPath(asmFilePath);

                Log.Information("Assembly {AsmName} contains {Count} parts", asmName, components.Count);

                foreach (var component in components)
                {
                    var part = new Part
                    {
                        PartName = component.ComponentName,
                        PartNumber = component.PartNumber,
                        ComponentName = asmName,  // Store which .asm it came from
                        UnitPrice = 0.0,
                        Quantity = 1,
                        Unit = "pcs",
                        Supplier = "",
                        Description = $"{component.Description} | Folder: {relativeFolder}"
                    };

                    // Avoid duplicates by part number
                    if (!_extractedParts.Any(p => p.PartNumber == part.PartNumber))
                    {
                        _extractedParts.Add(part);
                        Log.Debug("Extracted part: {PartNumber} from {AsmName}", part.PartNumber, asmName);
                    }
                }

                // Close assembly after reading
                _seService.CloseAssembly(save: false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error extracting parts from assembly: {AsmFile}", asmFilePath);
            }
        }

        /// <summary>
        /// Get folder hierarchy path (e.g., "Columns > 700x1000")
        /// </summary>
        private string GetHierarchyPath(string filePath)
        {
            try
            {
                var relativePath = filePath.Replace(_rootPath, "").TrimStart(Path.DirectorySeparatorChar);
                var parts = relativePath.Split(Path.DirectorySeparatorChar);
                
                // Return all folders except the .asm file itself
                if (parts.Length > 1)
                    return string.Join(" > ", parts.Take(parts.Length - 1));
                
                return "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Import all extracted parts to database
        /// </summary>
        public int ImportPartsToDatabase()
        {
            int importedCount = 0;

            try
            {
                var existingParts = _dbService.GetAllParts();

                foreach (var part in _extractedParts)
                {
                    // Check if part already exists
                    if (existingParts.Any(p => p.PartNumber == part.PartNumber))
                    {
                        Log.Debug("Part already in database: {PartNumber}", part.PartNumber);
                        continue;
                    }

                    _dbService.AddPart(part);
                    importedCount++;
                    Log.Information("✓ Imported part: {PartNumber} from {Assembly}", part.PartNumber, part.ComponentName);
                }

                Log.Information("✓ Total imported: {Count} new parts", importedCount);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error importing parts to database: {Message}", ex.Message);
            }

            return importedCount;
        }

        /// <summary>
        /// Get all unique assemblies found
        /// </summary>
        public List<string> GetAllAssemblies()
        {
            return _extractedParts
                .Select(p => p.ComponentName)
                .Distinct()
                .OrderBy(a => a)
                .ToList();
        }

        /// <summary>
        /// Get parts from specific assembly
        /// </summary>
        public List<Part> GetPartsByAssembly(string assemblyName)
        {
            return _extractedParts
                .Where(p => p.ComponentName == assemblyName)
                .ToList();
        }

        /// <summary>
        /// Get summary statistics
        /// </summary>
        public (int TotalParts, int UniqueAssemblies) GetStatistics()
        {
            return (
                _extractedParts.Count,
                _extractedParts.Select(p => p.ComponentName).Distinct().Count()
            );
        }
    }
}