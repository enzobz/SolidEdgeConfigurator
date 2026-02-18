namespace SolidEdgeConfigurator.Domain.Entities
{
    /// <summary>
    /// Represents a selectable option within a category (e.g., "700x1000" for Columns, "IP54" for IP Rating)
    /// </summary>
    public class Option
    {
        /// <summary>
        /// Immutable internal ID (primary key)
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Mutable business identifier/code
        /// </summary>
        public string Code { get; set; }
        
        public string Name { get; set; }
        public string Description { get; set; }
        
        /// <summary>
        /// Foreign key to Category
        /// </summary>
        public int CategoryId { get; set; }
        
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;
    }
}
