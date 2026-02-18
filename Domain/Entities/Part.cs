namespace SolidEdgeConfigurator.Domain.Entities
{
    /// <summary>
    /// Represents a physical part/component with pricing and supplier information
    /// This is the new Part entity following the modular approach
    /// </summary>
    public class Part
    {
        /// <summary>
        /// Immutable internal ID (primary key)
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Mutable business identifier/code (e.g., "PART-001", "BOLT-M8")
        /// </summary>
        public string Code { get; set; }
        
        public string Name { get; set; }
        public string Description { get; set; }
        
        /// <summary>
        /// Part number from supplier or internal system
        /// </summary>
        public string PartNumber { get; set; }
        
        public double UnitPrice { get; set; }
        public string Supplier { get; set; }
        public string Unit { get; set; } = "pcs";  // pieces, kg, meters, etc.
        
        /// <summary>
        /// Optional: Reference to CAD component name for assembly generation
        /// This is separate from BOM generation which is 100% DB-based
        /// </summary>
        public string ComponentName { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
}
