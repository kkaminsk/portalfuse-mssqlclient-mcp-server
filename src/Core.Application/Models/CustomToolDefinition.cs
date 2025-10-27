namespace Core.Application.Models
{
    /// <summary>
    /// Defines a custom tool that maps to a stored procedure.
    /// </summary>
    public class CustomToolDefinition
    {
        /// <summary>
        /// The MCP tool name (e.g., "get_customer_orders")
        /// </summary>
        public string ToolName { get; set; } = string.Empty;
        
        /// <summary>
        /// Description of what the tool does
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// The stored procedure to execute (e.g., "dbo.usp_GetCustomerOrders")
        /// </summary>
        public string StoredProcedure { get; set; } = string.Empty;
        
        /// <summary>
        /// Optional database name for server mode. If null, uses current database.
        /// </summary>
        public string? DatabaseName { get; set; }
        
        /// <summary>
        /// Default timeout in seconds for this tool. If null, uses system default.
        /// </summary>
        public int? DefaultTimeoutSeconds { get; set; }
        
        /// <summary>
        /// Whether this tool is enabled. Default is true.
        /// </summary>
        public bool Enabled { get; set; } = true;
        
        /// <summary>
        /// Whether this tool requires approval if execute tools are disabled.
        /// </summary>
        public bool RequiresExecutePermission { get; set; } = true;
        
        /// <summary>
        /// List of parameters for the tool
        /// </summary>
        public List<ToolParameterDefinition> Parameters { get; set; } = new();
    }
}
