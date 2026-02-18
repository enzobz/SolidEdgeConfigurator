using System.Collections.Generic;

namespace SolidEdgeConfigurator.Application.DTOs
{
    /// <summary>
    /// Represents a consolidated BOM line item with part details and total quantity
    /// </summary>
    public class BOMLineItem
    {
        public int PartId { get; set; }
        public string PartCode { get; set; }
        public string PartName { get; set; }
        public string PartNumber { get; set; }
        public string Description { get; set; }
        public int TotalQuantity { get; set; }
        public string Unit { get; set; }
        public double UnitPrice { get; set; }
        public double TotalPrice => UnitPrice * TotalQuantity;
        public string Supplier { get; set; }
        
        /// <summary>
        /// List of modules this part comes from (for reference)
        /// </summary>
        public List<string> SourceModules { get; set; } = new List<string>();
    }
}
