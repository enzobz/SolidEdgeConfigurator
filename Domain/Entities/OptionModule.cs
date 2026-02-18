namespace SolidEdgeConfigurator.Domain.Entities
{
    /// <summary>
    /// Junction table that defines which modules are activated by which options
    /// Multiple options can activate the same module, and one option can activate multiple modules
    /// </summary>
    public class OptionModule
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Foreign key to Option
        /// </summary>
        public int OptionId { get; set; }
        
        /// <summary>
        /// Foreign key to Module
        /// </summary>
        public int ModuleId { get; set; }
        
        /// <summary>
        /// Quantity multiplier for this module when the option is selected
        /// </summary>
        public int Quantity { get; set; } = 1;
    }
}
