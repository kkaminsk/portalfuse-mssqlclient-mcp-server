namespace Core.Application.Models
{
    /// <summary>
    /// Defines a parameter for a custom tool.
    /// </summary>
    public class ToolParameterDefinition
    {
        /// <summary>
        /// The parameter name as it appears in the MCP tool (e.g., "customerId")
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Description of the parameter for documentation
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// The SQL parameter name (e.g., "@CustomerId" or "CustomerId")
        /// </summary>
        public string SqlParameterName { get; set; } = string.Empty;
        
        /// <summary>
        /// The parameter type: string, int, bool, decimal, datetime, etc.
        /// </summary>
        public string Type { get; set; } = "string";
        
        /// <summary>
        /// Whether this parameter is required
        /// </summary>
        public bool Required { get; set; } = true;
        
        /// <summary>
        /// Default value if parameter is not provided (for optional parameters)
        /// </summary>
        public object? DefaultValue { get; set; }
        
        /// <summary>
        /// Minimum value for numeric parameters
        /// </summary>
        public object? MinValue { get; set; }
        
        /// <summary>
        /// Maximum value for numeric parameters
        /// </summary>
        public object? MaxValue { get; set; }
        
        /// <summary>
        /// Maximum length for string parameters
        /// </summary>
        public int? MaxLength { get; set; }
        
        /// <summary>
        /// Regular expression pattern for string validation
        /// </summary>
        public string? Pattern { get; set; }
        
        /// <summary>
        /// Array of allowed values for enum-like parameters
        /// </summary>
        public List<string>? AllowedValues { get; set; }
    }
}
