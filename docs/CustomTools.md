# Custom Tools Configuration Guide

## Overview

The SQL Server MCP Client supports **Configuration-Driven Tool Generation**, allowing you to define custom MCP tools in `appsettings.json` that automatically map to your stored procedures. This provides a more intuitive, domain-specific interface for AI assistants compared to using the generic `execute_stored_procedure` tool.

## Benefits

- **Domain-Specific Names**: Use meaningful tool names like `get_customer_orders` instead of generic commands
- **Type Safety**: Automatic parameter validation based on your configuration
- **Better AI Understanding**: AI assistants can better understand purpose-built tools
- **Validation**: Built-in validation for parameters (required, type, min/max, patterns, etc.)
- **Security**: Individual tool enablement control
- **No Code Changes**: Add or modify tools through configuration only

## Configuration Structure

Add custom tools to `appsettings.json` under the `CustomTools` array:

```json
{
  "CustomTools": [
    {
      "ToolName": "get_customer_orders",
      "Description": "Retrieves all orders for a specific customer",
      "StoredProcedure": "dbo.usp_GetCustomerOrders",
      "DatabaseName": null,
      "DefaultTimeoutSeconds": 60,
      "Enabled": true,
      "RequiresExecutePermission": true,
      "Parameters": [
        {
          "Name": "customerId",
          "Description": "Customer ID",
          "SqlParameterName": "CustomerId",
          "Type": "string",
          "Required": true,
          "MaxLength": 10
        }
      ]
    }
  ]
}
```

## Tool Definition Properties

### Root Level Properties

| Property | Type | Required | Default | Description |
|----------|------|----------|---------|-------------|
| `ToolName` | string | Yes | - | MCP tool name (e.g., "get_customer_orders") |
| `Description` | string | Yes | - | What the tool does (shown to AI assistants) |
| `StoredProcedure` | string | Yes | - | Stored procedure to execute (e.g., "dbo.usp_GetCustomerOrders") |
| `DatabaseName` | string | No | null | Database name (required in server mode, null = current database) |
| `DefaultTimeoutSeconds` | int | No | 30 | Timeout in seconds for this tool |
| `Enabled` | bool | No | true | Whether this tool is active |
| `RequiresExecutePermission` | bool | No | true | If true, requires `EnableExecuteStoredProcedure=true` |
| `Parameters` | array | No | [] | List of parameter definitions |

### Parameter Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Name` | string | Yes | Parameter name as seen by AI (e.g., "customerId") |
| `Description` | string | Yes | Parameter description for AI |
| `SqlParameterName` | string | Yes | SQL parameter name (e.g., "@CustomerId" or "CustomerId") |
| `Type` | string | No | Parameter type: `string`, `int`, `bool`, `decimal`, `datetime`, `guid` |
| `Required` | bool | No | Whether parameter is required (default: true) |
| `DefaultValue` | any | No | Default value for optional parameters |
| `MinValue` | number | No | Minimum value (for numeric types) |
| `MaxValue` | number | No | Maximum value (for numeric types) |
| `MaxLength` | int | No | Maximum length (for string types) |
| `Pattern` | string | No | Regular expression pattern (for string types) |
| `AllowedValues` | array | No | List of allowed values (enum-like behavior) |

## Examples

### Example 1: Simple Customer Lookup

```json
{
  "ToolName": "get_customer_orders",
  "Description": "Retrieves all orders for a specific customer",
  "StoredProcedure": "dbo.usp_GetCustomerOrders",
  "Parameters": [
    {
      "Name": "customerId",
      "Description": "Customer ID (e.g., 'ALFKI')",
      "SqlParameterName": "CustomerId",
      "Type": "string",
      "Required": true,
      "MaxLength": 10
    },
    {
      "Name": "includeDetails",
      "Description": "Include detailed line items",
      "SqlParameterName": "IncludeDetails",
      "Type": "bool",
      "Required": false,
      "DefaultValue": false
    }
  ]
}
```

**Usage by AI**:
```
