namespace SolidEdgeConfigurator.Domain.Entities
{
    /// <summary>
    /// Represents a category that groups related options (e.g., "Columns", "IP Rating", "Busbar")
    /// </summary>
    public class Category
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
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
