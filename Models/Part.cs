namespace SolidEdgeConfigurator.Models
{
    public class Part
    {
        public int Id { get; set; }
        public string PartName { get; set; }
        public string PartNumber { get; set; }
        public string ComponentName { get; set; }  // Links to Solid Edge component
        public double UnitPrice { get; set; }
        public string Supplier { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; } = 1;
        public string Unit { get; set; } = "pcs";  // pieces, kg, meters, etc.
        public double TotalPrice => UnitPrice * Quantity;
    }
}