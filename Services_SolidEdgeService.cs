using SolidEdgeFramework;
using SolidEdgeAssembly;
using System;
using System.Collections.Generic;
using System.IO;
using SolidEdgeConfigurator.Models;

namespace SolidEdgeConfigurator.Services
{
    public class SolidEdgeService : IDisposable
    {
        private Application _seApp;
        private AssemblyDocument _currentDocument;

        public SolidEdgeService()
        {
            InitializeSolidEdge();
        }

        private void InitializeSolidEdge()
        {
            try
            {
                var seType = Type.GetTypeFromProgID("SolidEdge.Application", throwOnError: false);
                if (seType == null)
                    throw new Exception("Solid Edge is not installed or COM registration failed.");

                _seApp = (Application)Activator.CreateInstance(seType);
                _seApp.Visible = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize Solid Edge: {ex.Message}");
                throw;
            }
        }

        public void OpenAssembly(string asmFilePath)
        {
            if (!File.Exists(asmFilePath))
                throw new FileNotFoundException($"Assembly file not found: {asmFilePath}");

            _currentDocument = (AssemblyDocument)_seApp.Documents.Open(asmFilePath, Type.Missing, Type.Missing);
        }

        public void ApplyConfiguration(ConfigurationSettings config)
        {
            if (_currentDocument == null)
                throw new InvalidOperationException("No assembly is currently open");

            foreach (var componentConfig in config.ComponentConfigs)
                SetComponentVisibility(componentConfig);
        }

        private void SetComponentVisibility(ComponentConfig componentConfig)
        {
            var occurrences = _currentDocument.Occurrences;
            foreach (Occurrence occurrence in occurrences)
            {
                if (occurrence.Name.Equals(componentConfig.ComponentName, StringComparison.OrdinalIgnoreCase))
                {
                    occurrence.Visible = componentConfig.IsVisible;
                    break;
                }
            }
        }

        public void SaveAssemblyAs(string outputPath)
        {
            if (_currentDocument == null)
                throw new InvalidOperationException("No assembly is currently open");

            _currentDocument.SaveAs(outputPath);
        }

        public List<string> GetComponentList()
        {
            var components = new List<string>();
            if (_currentDocument == null)
                return components;

            foreach (Occurrence occurrence in _currentDocument.Occurrences)
                components.Add(occurrence.Name);

            return components;
        }

        public void CloseAssembly(bool save = false)
        {
            if (_currentDocument != null)
            {
                _currentDocument.Close(!save);
                _currentDocument = null;
            }
        }

        public void Dispose()
        {
            CloseAssembly(save: false);
            _seApp?.Quit();
        }
    }
}