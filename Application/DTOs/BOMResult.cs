using System;
using System.Collections.Generic;
using System.Linq;

namespace SolidEdgeConfigurator.Application.DTOs
{
    /// <summary>
    /// Represents a complete Bill of Materials generated from selected options
    /// </summary>
    public class BOMResult
    {
        public string ConfigurationName { get; set; }
        public DateTime GeneratedDate { get; set; }
        public List<BOMLineItem> LineItems { get; set; } = new List<BOMLineItem>();
        public List<string> SelectedOptions { get; set; } = new List<string>();
        public List<string> ActivatedModules { get; set; } = new List<string>();
        
        public double TotalCost => LineItems.Sum(item => item.TotalPrice);
        public int TotalItems => LineItems.Sum(item => item.TotalQuantity);
        public int UniquePartCount => LineItems.Count;
    }
}
