using System.Collections.Generic;

namespace SolidEdgeConfigurator.Application.DTOs
{
    /// <summary>
    /// Represents a category with its available options for UI display
    /// </summary>
    public class CategoryDTO
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int DisplayOrder { get; set; }
        public List<OptionDTO> Options { get; set; } = new List<OptionDTO>();
    }
    
    /// <summary>
    /// Represents an option within a category
    /// </summary>
    public class OptionDTO
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsDefault { get; set; }
    }
}
