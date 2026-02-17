using System.Collections.Generic;

namespace SolidEdgeConfigurator.Models
{
    /// <summary>
    /// Configuration settings for assembly generation
    /// </summary>
    public class ConfigurationSettings
    {
        public string ConfigurationName { get; set; }
        public string TemplatePath { get; set; }
        public string OutputPath { get; set; }
        public List<ComponentConfiguration> ComponentConfigurations { get; set; }

        public ConfigurationSettings()
        {
            ComponentConfigurations = new List<ComponentConfiguration>();
        }
    }

    /// <summary>
    /// Configuration for individual component
    /// </summary>
    public class ComponentConfiguration
    {
        public string ComponentName { get; set; }
        public bool IsVisible { get; set; }
        public string ConfigurationOption { get; set; }
    }
}