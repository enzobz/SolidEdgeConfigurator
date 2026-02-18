using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using SolidEdgeConfigurator.Models;
using Serilog;

namespace SolidEdgeConfigurator.Services
{
    /// <summary>
    /// Service for interacting with Solid Edge assemblies
    /// </summary>
    public class SolidEdgeService : IDisposable
    {
        /// <summary>
        /// Reference to the Solid Edge Application COM object
        /// </summary>
        private dynamic _application;
        
        /// <summary>
        /// Reference to the current AssemblyDocument COM object
        /// </summary>
        private dynamic _assemblyDocument;
        
        private string _currentAssemblyPath;
        private List<ComponentConfig> _components; 

        public SolidEdgeService()
        {
            _components = new List<ComponentConfig>();
            InitializeSolidEdge();
        }

        /// <summary>
        /// Initialize Solid Edge application (hidden by default)
        /// </summary>
        private void InitializeSolidEdge()
        {
            try
            {
                var seType = Type.GetTypeFromProgID("SolidEdge.Application", throwOnError: false);
                if (seType == null)
                {
                    throw new Exception("Solid Edge is not installed or COM registration failed.");
                }

                _application = Activator.CreateInstance(seType);
                _application.Visible = false;  // Start hidden
                Log.Information("✓ Solid Edge initialized (hidden)");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "✗ Failed to initialize Solid Edge: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Connect to Solid Edge application
        /// </summary>
        public bool ConnectToSolidEdge()
        {
            try
            {
                if (_application != null)
                {
                    Log.Information("Connected to Solid Edge instance");
                    return true;
                }

                InitializeSolidEdge();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to connect to Solid Edge");
                return false;
            }
        }

        /// <summary>
        /// Load an assembly file
        /// </summary>
        public bool LoadAssembly(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Log.Error("Assembly file not found: {FilePath}", filePath);
                    return false;
                }

                if (!ConnectToSolidEdge())
                {
                    return false;
                }

                // Close existing document if any
                if (_assemblyDocument != null)
                {
                    try
                    {
                        _assemblyDocument.Close(false);
                    }
                    catch (Exception closeEx)
                    {
                        Log.Debug(closeEx, "Exception while closing previous document");
                    }
                }

                // Open the assembly document
                _assemblyDocument = _application.Documents.Open(filePath);
                _currentAssemblyPath = filePath;
                
                Log.Information("Assembly loaded: {FilePath}", filePath);

                // Load components from assembly
                LoadComponentsFromAssembly();

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load assembly: {FilePath}", filePath);
                return false;
            }
        }

        /// <summary>
        /// Get list of components from loaded assembly
        /// </summary>
        private void LoadComponentsFromAssembly()
        {
            _components.Clear();

            try
            {
                if (_assemblyDocument == null)
                {
                    Log.Warning("No assembly document loaded");
                    return;
                }

                // Get occurrences (component instances) from assembly
                dynamic occurrences = _assemblyDocument.Occurrences;
                int count = occurrences.Count;

                Log.Information("Found {Count} components in assembly", count);

                // Note: COM collections are 1-indexed, not 0-indexed
                for (int i = 1; i <= count; i++)
                {
                    try
                    {
                        dynamic occurrence = occurrences.Item(i);
                        string componentName = occurrence.Name;
                        _components.Add(new ComponentConfig
                        {
                            ComponentName = componentName,
                            IsVisible = true
                        });
                        Log.Debug("Found component: {ComponentName}", componentName);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Error processing component {Index}", i);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load components from assembly");
            }
        }

        /// <summary>
        /// Get list of available components
        /// </summary>
        public List<string> GetComponentList()
        {
            return _components.Select(c => c.ComponentName).ToList();
        }

        /// <summary>
        /// Apply configuration to assembly components
        /// </summary>
        public void ApplyConfiguration(ConfigurationSettings config)
        {
            try
            {
                if (_assemblyDocument == null)
                {
                    throw new InvalidOperationException("No assembly is currently open");
                }

                Log.Information("Applying configuration with {Count} component settings...", config.ComponentConfigs.Count);

                foreach (var componentConfig in config.ComponentConfigs)
                {
                    SetComponentVisibility(componentConfig);
                }

                Log.Information("✓ Configuration applied successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "✗ Failed to apply configuration: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Set visibility of a component
        /// </summary>
        private void SetComponentVisibility(ComponentConfig componentConfig)
        {
            try
            {
                var occurrences = _assemblyDocument.Occurrences;

                for (int i = 1; i <= occurrences.Count; i++)
                {
                    dynamic occurrence = occurrences.Item(i);
                    
                    if (occurrence.Name.Equals(componentConfig.ComponentName, StringComparison.OrdinalIgnoreCase))
                    {
                        occurrence.Visible = componentConfig.IsVisible;
                        string state = componentConfig.IsVisible ? "VISIBLE" : "HIDDEN";
                        Log.Information("✓ Component '{ComponentName}' set to {State}", componentConfig.ComponentName, state);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error setting visibility for component: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Save assembly with new configuration
        /// </summary>
        public void SaveAssemblyAs(string outputPath)
        {
            try
            {
                if (_assemblyDocument == null)
                {
                    throw new InvalidOperationException("No assembly is currently open");
                }

                _assemblyDocument.SaveAs(outputPath);
                Log.Information("✓ Assembly saved to: {OutputPath}", outputPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "✗ Failed to save assembly: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Open assembly file
        /// </summary>
        public void OpenAssembly(string asmFilePath)
        {
            try
            {
                if (!File.Exists(asmFilePath))
                {
                    throw new FileNotFoundException($"Assembly file not found: {asmFilePath}");
                }

                LoadAssembly(asmFilePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to open assembly");
                throw;
            }
        }

        /// <summary>
        /// Toggle Solid Edge visibility
        /// </summary>
        public bool ToggleSolidEdgeVisibility()
        {
            try
            {
                if (_application == null)
                {
                    return false;
                }

                _application.Visible = !_application.Visible;
                string state = _application.Visible ? "SHOWN" : "HIDDEN";
                Log.Information("✓ Solid Edge {State}", state);
                return _application.Visible;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error toggling Solid Edge: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Check if Solid Edge window is visible
        /// </summary>
        public bool IsSolidEdgeVisible()
        {
            try
            {
                return _application != null && _application.Visible;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Show Solid Edge window
        /// </summary>
        public void ShowSolidEdge()
        {
            try
            {
                if (_application != null)
                {
                    _application.Visible = true;
                    Log.Information("✓ Solid Edge shown");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error showing Solid Edge: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Hide Solid Edge window
        /// </summary>
        public void HideSolidEdge()
        {
            try
            {
                if (_application != null)
                {
                    _application.Visible = false;
                    Log.Information("✓ Solid Edge hidden");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error hiding Solid Edge: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Close assembly document
        /// </summary>
        public void CloseAssembly(bool save = false)
        {
            try
            {
                if (_assemblyDocument != null)
                {
                    _assemblyDocument.Close(!save);
                    _assemblyDocument = null;
                    Log.Information("✓ Assembly closed");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error closing assembly: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Dispose and cleanup
        /// </summary>
        public void Dispose()
        {
            try
            {
                CloseAssembly(save: false);
                
                if (_application != null)
                {
                    _application.Quit();
                    Marshal.ReleaseComObject(_application);
                    _application = null;
                }
                
                Log.Information("✓ Solid Edge service disposed");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during disposal: {Message}", ex.Message);
            }

            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Get all components with their details (for parts import)
        /// </summary>
        public List<ComponentDetail> GetComponentDetails()
        {
            var components = new List<ComponentDetail>();

            try
            {
                if (_assemblyDocument == null)
                    return components;

                // Get all occurrence objects (parts/components)
                dynamic occurrences = _assemblyDocument.Occurrences;

                for (int i = 1; i <= occurrences.Count; i++)
                {
                    try
                    {
                        dynamic occurrence = occurrences.Item(i);
                        
                        // Get the referenced document (the actual .psm file)
                        dynamic refDoc = occurrence.ReferencedDocument;
                        
                        // Get the full path and extract filename
                        string fullPath = refDoc.FullFileName;
                        string fileName = System.IO.Path.GetFileNameWithoutExtension(fullPath);
                        string fileExtension = System.IO.Path.GetExtension(fullPath);

                        var component = new ComponentDetail
                        {
                            ComponentName = fileName,
                            PartNumber = fileName,  // Use filename as part number
                            Description = $"Part: {fileName} | Type: {fileExtension}"
                        };

                        // Avoid duplicates
                        if (!components.Any(c => c.PartNumber == component.PartNumber))
                        {
                            components.Add(component);
                            Log.Information("Found part: {PartNumber} ({FileName})", component.PartNumber, fileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Error processing occurrence {Index}", i);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting component details");
            }

            return components;
        }
        
        private string ExtractPartNumber(string filename)
        {
            // Extract part number from filename if it follows a pattern
            // e.g., "ES00812_400x600_IP31" -> "ES00812"
            var parts = filename.Split('_');
            return parts.Length > 0 ? parts[0] : filename;
        }
    }
}