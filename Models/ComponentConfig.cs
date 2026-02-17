using System;

namespace SolidEdgeConfigurator.Models
{
    /// <summary>
    /// Configuration for a component in the assembly
    /// </summary>
    public class ComponentConfig
    {
        public string ComponentName { get; set; }
        public bool IsVisible { get; set; }
        public string ConfigurationName { get; set; }
        public string Description { get; set; }

        public ComponentConfig()
        {
            IsVisible = true;
        }

        public override string ToString()
        {
            return ComponentName;
        }
    }
}