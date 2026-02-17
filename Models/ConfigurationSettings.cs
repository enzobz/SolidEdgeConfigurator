using System.Collections.Generic;

namespace SolidEdgeConfigurator.Models
{
    public class ConfigurationSettings
    {
        public string ConfigurationName { get; set; }
        public string TemplatePath { get; set; }
        public string OutputPath { get; set; }
        public List<ComponentConfig> ComponentConfigs { get; set; } = new List<ComponentConfig>();
    }
}