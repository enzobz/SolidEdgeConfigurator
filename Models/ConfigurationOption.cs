using System.Collections.Generic;

namespace SolidEdgeConfigurator.Models
{
    /// <summary>
    /// Represents a mechanical configuration with all options
    /// </summary>
    public class ConfigurationOption
    {
        public int Id { get; set; }
        public string ConfigName { get; set; }
        
        // Mechanical Tab
        public string ColumnsSize { get; set; }
        public string IP { get; set; }
        public string VentilatedRoof { get; set; }
        public string HBB { get; set; }  // Horizontal Busbar
        public string VBB { get; set; }  // Vertical Busbar
        public string Earth { get; set; }
        public string Neutral { get; set; }
        
        // ES File Reference
        public string ESFile { get; set; }  // e.g., "ES00812"
        
        // Created date
        public System.DateTime CreatedDate { get; set; }
    }

    /// <summary>
    /// Stores the relationship between configuration and parts
    /// </summary>
    public class ConfigurationParts
    {
        public int Id { get; set; }
        public int ConfigurationOptionId { get; set; }
        public int PartId { get; set; }
        public string PartNumber { get; set; }
        public string PSMFileName { get; set; }
    }
}