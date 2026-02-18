namespace SolidEdgeConfigurator.Domain.Entities
{
    /// <summary>
    /// Represents a module (assembly/subassembly) that can be included in a configuration
    /// A module is activated when specific options are selected
    /// </summary>
    public class Module
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
        /// Path to the master assembly file (.ASM) for this module
        /// Used for generating the physical assembly in CAD
        /// </summary>
        public string MasterAssemblyPath { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
}
