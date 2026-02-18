namespace SolidEdgeConfigurator.Domain.Entities
{
    /// <summary>
    /// Junction table that defines which parts belong to which modules and their quantities
    /// This is the key table for BOM generation: selected options → modules → parts
    /// </summary>
    public class ModulePart
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Foreign key to Module
        /// </summary>
        public int ModuleId { get; set; }
        
        /// <summary>
        /// Foreign key to Part
        /// </summary>
        public int PartId { get; set; }
        
        /// <summary>
        /// Quantity of this part needed in the module
        /// </summary>
        public int Quantity { get; set; } = 1;
    }
}
