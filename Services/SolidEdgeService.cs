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
    public class SolidEdgeService
    {
        private dynamic _application;
        private dynamic _assemblyDocument;
        private string _currentAssemblyPath;
        private List<ComponentConfig> _components;

        public SolidEdgeService()
        {
            _components = new List<ComponentConfig>();
            
            // Initialize logger if not already configured
            if (Log.Logger == null || Log.Logger.GetType().Name == "SilentLogger")
            {
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console()
                    .CreateLogger();
            }
        }

        /// <summary>
        /// Connect to Solid Edge application
        /// </summary>
        public bool ConnectToSolidEdge()
        {
            try
            {
                // Try to get running instance
                try
                {
                    _application = Marshal.GetActiveObject("SolidEdge.Application");
                    Log.Information("Connected to running Solid Edge instance");
                }
                catch
                {
                    // If no instance running, create new one
                    Type type = Type.GetTypeFromProgID("SolidEdge.Application");
                    if (type != null)
                    {
                        _application = Activator.CreateInstance(type);
                        _application.Visible = true;
                        Log.Information("Started new Solid Edge instance");
                    }
                    else
                    {
                        throw new Exception("Solid Edge is not installed or not registered");
                    }
                }
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
                        // Document may already be closed or in an invalid state
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

                for (int i = 1; i <= count; i++)
                {
                    try
                    {
                        dynamic occurrence = occurrences.Item(i);
                        string name = occurrence.Name;
                        bool visible = !occurrence.Suppressed;

                        _components.Add(new ComponentConfig
                        {
                            ComponentName = name,
                            IsVisible = visible,
                            Description = $"Component {i}"
                        });

                        Log.Debug("Component loaded: {Name}, Visible: {Visible}", name, visible);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to load component {Index}", i);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load components from assembly");
            }
        }

        /// <summary>
        /// Get all components from loaded assembly
        /// </summary>
        public List<ComponentConfig> GetComponents()
        {
            return new List<ComponentConfig>(_components);
        }

        /// <summary>
        /// Toggle visibility of a component
        /// </summary>
        public bool ToggleComponentVisibility(string componentName, bool isVisible)
        {
            try
            {
                if (_assemblyDocument == null)
                {
                    Log.Warning("No assembly document loaded");
                    return false;
                }

                dynamic occurrences = _assemblyDocument.Occurrences;
                int count = occurrences.Count;

                for (int i = 1; i <= count; i++)
                {
                    dynamic occurrence = occurrences.Item(i);
                    if (occurrence.Name == componentName)
                    {
                        occurrence.Suppressed = !isVisible;
                        
                        // Update local cache
                        var component = _components.FirstOrDefault(c => c.ComponentName == componentName);
                        if (component != null)
                        {
                            component.IsVisible = isVisible;
                        }

                        Log.Information("Component {Name} visibility set to {Visible}", componentName, isVisible);
                        return true;
                    }
                }

                Log.Warning("Component not found: {Name}", componentName);
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to toggle visibility for component: {Name}", componentName);
                return false;
            }
        }

        /// <summary>
        /// Generate a new assembly file with current configuration
        /// </summary>
        public bool GenerateAssembly(string outputPath, ConfigurationSettings settings)
        {
            try
            {
                if (_assemblyDocument == null)
                {
                    Log.Warning("No assembly document loaded");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(outputPath))
                {
                    Log.Error("Output path is empty");
                    return false;
                }

                // Ensure output directory exists
                string directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Apply component configurations if provided
                if (settings?.ComponentConfigurations != null && settings.ComponentConfigurations.Any())
                {
                    foreach (var config in settings.ComponentConfigurations)
                    {
                        if (!string.IsNullOrEmpty(config.ComponentName))
                        {
                            ToggleComponentVisibility(config.ComponentName, config.IsVisible);
                        }
                    }
                }

                // Save assembly to new location
                _assemblyDocument.SaveAs(outputPath);
                
                Log.Information("Assembly generated successfully: {OutputPath}", outputPath);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to generate assembly: {OutputPath}", outputPath);
                return false;
            }
        }

        /// <summary>
        /// Close current assembly and cleanup
        /// </summary>
        public void Close()
        {
            try
            {
                if (_assemblyDocument != null)
                {
                    _assemblyDocument.Close(false);
                    _assemblyDocument = null;
                }

                _components.Clear();
                _currentAssemblyPath = null;

                Log.Information("Assembly closed");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error closing assembly");
            }
        }

        /// <summary>
        /// Get current assembly path
        /// </summary>
        public string GetCurrentAssemblyPath()
        {
            return _currentAssemblyPath;
        }

        /// <summary>
        /// Check if Solid Edge is available
        /// </summary>
        public bool IsSolidEdgeAvailable()
        {
            try
            {
                Type type = Type.GetTypeFromProgID("SolidEdge.Application");
                return type != null;
            }
            catch (Exception ex)
            {
                // COM registration or type resolution may fail if Solid Edge is not installed
                Log.Debug(ex, "Solid Edge availability check failed");
                return false;
            }
        }
    }
}
