using Core.Application.Interfaces;
using Core.Application.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.NET;

namespace Core.Infrastructure.McpServer.Tools
{
    /// <summary>
    /// Factory for creating custom MCP tools from configuration
    /// </summary>
    public static class CustomToolFactory
    {
        /// <summary>
        /// Registers all custom tools defined in configuration
        /// </summary>
        public static void RegisterCustomTools(
            IMcpServerBuilder builder,
            IConfiguration configuration,
            IDatabaseContext? dbContext,
            IServerDatabase? serverDb,
            bool isServerMode,
            DatabaseConfiguration dbConfig,
            ILogger logger)
        {
            var customTools = configuration
                .GetSection("CustomTools")
                .Get<List<CustomToolDefinition>>();

            if (customTools == null || customTools.Count == 0)
            {
                logger.LogInformation("No custom tools defined in configuration");
                return;
            }

            logger.LogInformation($"Found {customTools.Count} custom tool definition(s) in configuration");

            int registeredCount = 0;
            foreach (var toolDef in customTools)
            {
                try
                {
                    // Validate tool definition
                    if (string.IsNullOrWhiteSpace(toolDef.ToolName))
                    {
                        logger.LogWarning("Skipping custom tool with empty name");
                        continue;
                    }

                    // Check if tool is enabled
                    if (!toolDef.Enabled)
                    {
                        logger.LogInformation($"Custom tool '{toolDef.ToolName}' is disabled");
                        continue;
                    }

                    // Check if execution permission is required and granted
                    if (toolDef.RequiresExecutePermission && !dbConfig.EnableExecuteStoredProcedure)
                    {
                        logger.LogWarning($"Custom tool '{toolDef.ToolName}' requires EnableExecuteStoredProcedure=true");
                        continue;
                    }

                    // Validate stored procedure name
                    if (string.IsNullOrWhiteSpace(toolDef.StoredProcedure))
                    {
                        logger.LogWarning($"Custom tool '{toolDef.ToolName}' has no stored procedure defined");
                        continue;
                    }

                    // In server mode, database name is required
                    if (isServerMode && string.IsNullOrWhiteSpace(toolDef.DatabaseName))
                    {
                        logger.LogWarning($"Custom tool '{toolDef.ToolName}' requires DatabaseName in server mode");
                        continue;
                    }

                    // Create the configurable tool
                    var configurableTool = new ConfigurableTool(
                        toolDef,
                        dbContext,
                        serverDb,
                        isServerMode);

                    // Create MCP tool wrapper
                    var mcpTool = new DynamicMcpToolWrapper(configurableTool, toolDef);

                    // Register the tool
                    builder.WithTool(mcpTool);

                    registeredCount++;
                    logger.LogInformation($"Registered custom tool: {toolDef.ToolName} -> {toolDef.StoredProcedure}");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Failed to register custom tool '{toolDef.ToolName}': {ex.Message}");
                }
            }

            logger.LogInformation($"Successfully registered {registeredCount} custom tool(s)");
        }
    }

    /// <summary>
    /// Wraps a ConfigurableTool as an MCP tool with dynamic parameter handling
    /// </summary>
    internal class DynamicMcpToolWrapper : IMcpTool
    {
        private readonly ConfigurableTool _tool;
        private readonly CustomToolDefinition _definition;

        public DynamicMcpToolWrapper(ConfigurableTool tool, CustomToolDefinition definition)
        {
            _tool = tool;
            _definition = definition;
        }

        public string Name => _definition.ToolName;
        
        public string Description => _definition.Description;

        public Task<object> InvokeAsync(Dictionary<string, object?> parameters, CancellationToken cancellationToken = default)
        {
            return _tool.ExecuteAsync(parameters, cancellationToken)
                .ContinueWith(t => (object)t.Result, cancellationToken);
        }

        public Dictionary<string, ParameterDefinition> GetParameters()
        {
            var paramDict = new Dictionary<string, ParameterDefinition>();

            foreach (var param in _definition.Parameters)
            {
                var paramDef = new ParameterDefinition
                {
                    Description = param.Description,
                    Required = param.Required,
                    Type = MapTypeToJsonSchemaType(param.Type)
                };

                // Add constraints as additional properties
                if (param.DefaultValue != null)
                {
                    paramDef.DefaultValue = param.DefaultValue;
                }

                if (param.AllowedValues != null && param.AllowedValues.Count > 0)
                {
                    paramDef.Enum = param.AllowedValues.ToArray();
                }

                if (param.MinValue != null || param.MaxValue != null)
                {
                    if (param.MinValue != null)
                        paramDef.Minimum = Convert.ToDouble(param.MinValue);
                    if (param.MaxValue != null)
                        paramDef.Maximum = Convert.ToDouble(param.MaxValue);
                }

                if (param.MaxLength.HasValue)
                {
                    paramDef.MaxLength = param.MaxLength.Value;
                }

                if (!string.IsNullOrEmpty(param.Pattern))
                {
                    paramDef.Pattern = param.Pattern;
                }

                paramDict[param.Name] = paramDef;
            }

            return paramDict;
        }

        private string MapTypeToJsonSchemaType(string type)
        {
            return type.ToLower() switch
            {
                "string" => "string",
                "int" or "integer" or "long" => "integer",
                "decimal" or "double" or "float" => "number",
                "bool" or "boolean" => "boolean",
                "datetime" => "string", // ISO 8601 format
                "guid" => "string",     // UUID format
                _ => "string"
            };
        }
    }

    /// <summary>
    /// Represents a parameter definition for MCP tools
    /// </summary>
    public class ParameterDefinition
    {
        public string Description { get; set; } = string.Empty;
        public bool Required { get; set; }
        public string Type { get; set; } = "string";
        public object? DefaultValue { get; set; }
        public string[]? Enum { get; set; }
        public double? Minimum { get; set; }
        public double? Maximum { get; set; }
        public int? MaxLength { get; set; }
        public string? Pattern { get; set; }
    }
}
