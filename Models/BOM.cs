using System;
using System.Collections.Generic;
using System.Linq;

namespace SolidEdgeConfigurator.Models
{
    public class BOM
    {
        public int Id { get; set; }
        public string ConfigurationName { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<Part> Parts { get; set; } = new List<Part>();

        public double TotalCost => Parts.Sum(p => p.TotalPrice);
        public int TotalItems => Parts.Sum(p => p.Quantity);
    }
}