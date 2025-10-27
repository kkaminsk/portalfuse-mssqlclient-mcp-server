using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Infrastructure.McpServer.Extensions;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Core.Infrastructure.McpServer.Tools
{
    /// <summary>
    /// A dynamically configured MCP tool that executes a stored procedure
    /// based on configuration from appsettings.json
    /// </summary>
    public class ConfigurableTool
    {
        private readonly CustomToolDefinition _definition;
        private readonly IDatabaseContext? _dbContext;
        private readonly IServerDatabase? _serverDb;
        private readonly bool _isServerMode;

        public ConfigurableTool(
            CustomToolDefinition definition,
            IDatabaseContext? dbContext,
            IServerDatabase? serverDb,
            bool isServerMode)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _dbContext = dbContext;
            _serverDb = serverDb;
            _isServerMode = isServerMode;

            if (!_isServerMode && _dbContext == null)
                throw new ArgumentException("Database context is required for database mode");
            if (_isServerMode && _serverDb == null)
                throw new ArgumentException("Server database is required for server mode");
        }

        public string ToolName => _definition.ToolName;
        public string Description => _definition.Description;

        public async Task<string> ExecuteAsync(
            Dictionary<string, object?> parameters,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate parameters
                var validationError = ValidateParameters(parameters);
                if (validationError != null)
                    return validationError;

                // Build SQL parameters dictionary
                var sqlParameters = BuildSqlParameters(parameters);

                // Create timeout context
                var timeoutContext = ToolCallTimeoutFactory.Create();

                // Execute stored procedure
                IAsyncDataReader reader;
                
                if (_isServerMode)
                {
                    var dbName = _definition.DatabaseName;
                    if (string.IsNullOrWhiteSpace(dbName))
                        return "Error: Database name is required in server mode for custom tools";

                    reader = await _serverDb!.ExecuteStoredProcedureAsync(
                        _definition.StoredProcedure,
                        sqlParameters,
                        dbName,
                        timeoutContext,
                        _definition.DefaultTimeoutSeconds,
                        cancellationToken);
                }
                else
                {
                    reader = await _dbContext!.ExecuteStoredProcedureAsync(
                        _definition.StoredProcedure,
                        sqlParameters,
                        timeoutContext,
                        _definition.DefaultTimeoutSeconds,
                        cancellationToken);
                }

                // Format results
                return await AsyncDataReaderExtensions.FormatAsTableAsync(reader);
            }
            catch (SqlException ex)
            {
                return $"Error: SQL error executing {_definition.ToolName}: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error: Failed to execute {_definition.ToolName}: {ex.Message}";
            }
        }

        private string? ValidateParameters(Dictionary<string, object?> parameters)
        {
            foreach (var paramDef in _definition.Parameters)
            {
                // Check required parameters
                if (paramDef.Required && !parameters.ContainsKey(paramDef.Name))
                {
                    return $"Error: Required parameter '{paramDef.Name}' is missing";
                }

                // Skip validation if parameter not provided and not required
                if (!parameters.ContainsKey(paramDef.Name))
                    continue;

                var value = parameters[paramDef.Name];
                
                // Validate based on type
                var error = paramDef.Type.ToLower() switch
                {
                    "string" => ValidateStringParameter(paramDef, value),
                    "int" or "integer" => ValidateIntParameter(paramDef, value),
                    "decimal" or "double" or "float" => ValidateDecimalParameter(paramDef, value),
                    "bool" or "boolean" => ValidateBoolParameter(paramDef, value),
                    "datetime" => ValidateDateTimeParameter(paramDef, value),
                    _ => null
                };

                if (error != null)
                    return error;
            }

            return null;
        }

        private string? ValidateStringParameter(ToolParameterDefinition paramDef, object? value)
        {
            if (value == null)
                return null;

            var strValue = value.ToString();
            if (strValue == null)
                return null;

            // Check max length
            if (paramDef.MaxLength.HasValue && strValue.Length > paramDef.MaxLength.Value)
            {
                return $"Error: Parameter '{paramDef.Name}' exceeds maximum length of {paramDef.MaxLength.Value}";
            }

            // Check pattern
            if (!string.IsNullOrEmpty(paramDef.Pattern))
            {
                if (!Regex.IsMatch(strValue, paramDef.Pattern))
                {
                    return $"Error: Parameter '{paramDef.Name}' does not match required pattern";
                }
            }

            // Check allowed values
            if (paramDef.AllowedValues != null && paramDef.AllowedValues.Count > 0)
            {
                if (!paramDef.AllowedValues.Contains(strValue, StringComparer.OrdinalIgnoreCase))
                {
                    return $"Error: Parameter '{paramDef.Name}' must be one of: {string.Join(", ", paramDef.AllowedValues)}";
                }
            }

            return null;
        }

        private string? ValidateIntParameter(ToolParameterDefinition paramDef, object? value)
        {
            if (value == null)
                return null;

            if (!int.TryParse(value.ToString(), out int intValue))
            {
                return $"Error: Parameter '{paramDef.Name}' must be an integer";
            }

            // Check min value
            if (paramDef.MinValue != null)
            {
                if (int.TryParse(paramDef.MinValue.ToString(), out int minValue))
                {
                    if (intValue < minValue)
                    {
                        return $"Error: Parameter '{paramDef.Name}' must be at least {minValue}";
                    }
                }
            }

            // Check max value
            if (paramDef.MaxValue != null)
            {
                if (int.TryParse(paramDef.MaxValue.ToString(), out int maxValue))
                {
                    if (intValue > maxValue)
                    {
                        return $"Error: Parameter '{paramDef.Name}' must be at most {maxValue}";
                    }
                }
            }

            return null;
        }

        private string? ValidateDecimalParameter(ToolParameterDefinition paramDef, object? value)
        {
            if (value == null)
                return null;

            if (!decimal.TryParse(value.ToString(), out decimal decValue))
            {
                return $"Error: Parameter '{paramDef.Name}' must be a number";
            }

            // Check min value
            if (paramDef.MinValue != null)
            {
                if (decimal.TryParse(paramDef.MinValue.ToString(), out decimal minValue))
                {
                    if (decValue < minValue)
                    {
                        return $"Error: Parameter '{paramDef.Name}' must be at least {minValue}";
                    }
                }
            }

            // Check max value
            if (paramDef.MaxValue != null)
            {
                if (decimal.TryParse(paramDef.MaxValue.ToString(), out decimal maxValue))
                {
                    if (decValue > maxValue)
                    {
                        return $"Error: Parameter '{paramDef.Name}' must be at most {maxValue}";
                    }
                }
            }

            return null;
        }

        private string? ValidateBoolParameter(ToolParameterDefinition paramDef, object? value)
        {
            if (value == null)
                return null;

            if (value is bool)
                return null;

            var strValue = value.ToString()?.ToLower();
            if (strValue == "true" || strValue == "false" || strValue == "0" || strValue == "1")
                return null;

            return $"Error: Parameter '{paramDef.Name}' must be a boolean (true/false)";
        }

        private string? ValidateDateTimeParameter(ToolParameterDefinition paramDef, object? value)
        {
            if (value == null)
                return null;

            if (value is DateTime)
                return null;

            if (!DateTime.TryParse(value.ToString(), out _))
            {
                return $"Error: Parameter '{paramDef.Name}' must be a valid date/time";
            }

            return null;
        }

        private Dictionary<string, object?> BuildSqlParameters(Dictionary<string, object?> parameters)
        {
            var sqlParams = new Dictionary<string, object?>();

            foreach (var paramDef in _definition.Parameters)
            {
                object? value;

                // Use provided value or default
                if (parameters.ContainsKey(paramDef.Name))
                {
                    value = parameters[paramDef.Name];
                }
                else if (!paramDef.Required)
                {
                    value = paramDef.DefaultValue;
                }
                else
                {
                    continue; // Skip required params that aren't provided (validation would have caught this)
                }

                // Convert to appropriate type
                var convertedValue = ConvertParameterValue(value, paramDef.Type);
                
                // Add to SQL parameters with proper name
                var sqlParamName = paramDef.SqlParameterName;
                if (string.IsNullOrWhiteSpace(sqlParamName))
                {
                    sqlParamName = paramDef.Name;
                }

                sqlParams[sqlParamName] = convertedValue;
            }

            return sqlParams;
        }

        private object? ConvertParameterValue(object? value, string type)
        {
            if (value == null)
                return null;

            return type.ToLower() switch
            {
                "int" or "integer" => Convert.ToInt32(value),
                "long" => Convert.ToInt64(value),
                "decimal" => Convert.ToDecimal(value),
                "double" => Convert.ToDouble(value),
                "float" => Convert.ToSingle(value),
                "bool" or "boolean" => Convert.ToBoolean(value),
                "datetime" => Convert.ToDateTime(value),
                "guid" => Guid.Parse(value.ToString()!),
                _ => value.ToString()
            };
        }
    }
}
