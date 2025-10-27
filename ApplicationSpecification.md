# Application Specification Document

## Project: SQL Server MCP Client

**Version:** 1.0  
**Last Updated:** October 27, 2025  
**Document Type:** Technical Specification  

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [System Overview](#system-overview)
3. [Architecture](#architecture)
4. [Core Components](#core-components)
5. [Operational Modes](#operational-modes)
6. [Features and Capabilities](#features-and-capabilities)
7. [Security Model](#security-model)
8. [Configuration System](#configuration-system)
9. [Data Models](#data-models)
10. [API Specifications](#api-specifications)
11. [Deployment Architecture](#deployment-architecture)
12. [Testing Strategy](#testing-strategy)
13. [Dependencies](#dependencies)
14. [Performance Considerations](#performance-considerations)
15. [Error Handling](#error-handling)
16. [Future Enhancements](#future-enhancements)

---

## 1. Executive Summary

The SQL Server MCP Client is a comprehensive Microsoft SQL Server database client implementing the Model Context Protocol (MCP). It provides a standardized interface for AI assistants and other MCP-compatible clients to interact with SQL Server databases through a rich set of tools for query execution, schema discovery, and stored procedure management.

### Key Objectives

- **Standardized Database Access**: Provide MCP-compliant tools for SQL Server operations
- **Two-Mode Operation**: Support both single-database and multi-database scenarios
- **Security-First Design**: Granular security controls with disabled-by-default execution tools
- **Production-Ready**: Comprehensive timeout management, session handling, and error reporting
- **Cross-Platform**: Native .NET 9.0 implementation with Docker containerization support

### Target Audience

- AI assistants (Claude, GPT, etc.) requiring database access
- Developers building MCP-enabled applications
- Database administrators needing programmatic SQL Server access
- Organizations requiring secure, auditable database interactions

---

## 2. System Overview

### 2.1 Purpose

The SQL Server MCP Client serves as a bridge between MCP-compatible clients and Microsoft SQL Server databases, enabling:

- Safe, controlled database querying and schema exploration
- Stored procedure discovery and execution with automatic type conversion
- Long-running operation management through background sessions
- Cross-database operations in server-wide scenarios

### 2.2 Technology Stack

| Component | Technology | Version |
|-----------|-----------|---------|
| Framework | .NET | 9.0 |
| Language | C# | 13 |
| Protocol | Model Context Protocol | Latest |
| Database Driver | Microsoft.Data.SqlClient | Latest |
| Testing Framework | xUnit | Latest |
| Mocking Library | Moq | Latest |
| Container Platform | Docker | Latest |
| Configuration | Microsoft.Extensions.Configuration | .NET 9.0 |
| Dependency Injection | Microsoft.Extensions.DependencyInjection | .NET 9.0 |
| Logging | Microsoft.Extensions.Logging | .NET 9.0 |

### 2.3 System Context

```
┌─────────────────────┐
│   MCP Clients       │
│  (Claude Desktop,   │
│   Custom Apps)      │
└──────────┬──────────┘
           │ MCP Protocol (stdio/SSE)
           │
┌──────────▼──────────┐
│  MCP Server         │
│  (This Application) │
└──────────┬──────────┘
           │ SQL Protocol
           │
┌──────────▼──────────┐
│  SQL Server         │
│  (Any Version/      │
│   Azure SQL)        │
└─────────────────────┘
```

---

## 3. Architecture

### 3.1 Architectural Principles

1. **Clean Architecture**: Separation of concerns with distinct layers
2. **Dependency Inversion**: Interfaces for all external dependencies
3. **Single Responsibility**: Each component has one clear purpose
4. **Open/Closed Principle**: Extensible without modification
5. **Interface Segregation**: Focused, role-specific interfaces

### 3.2 Layered Architecture

```
┌─────────────────────────────────────────────────────────┐
│                   Presentation Layer                     │
│  ┌────────────────────────────────────────────────────┐ │
│  │         MCP Tools (Infrastructure.McpServer)       │ │
│  │  - ExecuteQueryTool                                │ │
│  │  - ListTablesTool                                  │ │
│  │  - ServerCapabilitiesTool                          │ │
│  │  - SessionManagementTools                          │ │
│  │  - [Server Mode Tools]                             │ │
│  └────────────────────────────────────────────────────┘ │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│                   Application Layer                      │
│  ┌────────────────────────────────────────────────────┐ │
│  │              Core Interfaces & Models              │ │
│  │  - IDatabaseContext                                │ │
│  │  - IServerDatabase                                 │ │
│  │  - IQuerySessionManager                            │ │
│  │  - DatabaseConfiguration                           │ │
│  │  - QuerySession, TableInfo, etc.                   │ │
│  └────────────────────────────────────────────────────┘ │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│                 Infrastructure Layer                     │
│  ┌────────────────────────────────────────────────────┐ │
│  │          Service Implementations (SqlClient)       │ │
│  │  - DatabaseService (IDatabaseService)              │ │
│  │  - DatabaseContextService (IDatabaseContext)       │ │
│  │  - ServerDatabaseService (IServerDatabase)         │ │
│  │  - QuerySessionManager                             │ │
│  │  - SqlServerCapabilityDetector                     │ │
│  │  - SessionCleanupService                           │ │
│  └────────────────────────────────────────────────────┘ │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│                   Data Access Layer                      │
│  ┌────────────────────────────────────────────────────┐ │
│  │        Microsoft.Data.SqlClient                    │ │
│  │  - SqlConnection                                   │ │
│  │  - SqlCommand                                      │ │
│  │  - SqlDataReader                                   │ │
│  └────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

### 3.3 Three-Tier Interface Design

The system implements a sophisticated three-tier interface hierarchy:

#### Tier 1: IDatabaseService (Core Layer)
- **Purpose**: Low-level database operations
- **Characteristics**: 
  - Context-switching capable (optional database parameter)
  - No assumptions about operational mode
  - Direct SQL Server communication
  - Foundation for higher-level services

#### Tier 2: IDatabaseContext (Database Mode)
- **Purpose**: Single-database scoped operations
- **Characteristics**:
  - Simplified API without database parameters
  - Assumes connection to specific database
  - Timeout context management
  - Optimized for single-database scenarios

#### Tier 3: IServerDatabase (Server Mode)
- **Purpose**: Server-wide, multi-database operations
- **Characteristics**:
  - Explicit database parameters required
  - Cross-database operation support
  - Database listing and discovery
  - Timeout context management

### 3.4 Component Relationships

```
┌──────────────────────────────────────────────────────────┐
│                      Program.cs                           │
│  (Application Entry Point & Dependency Configuration)     │
└───────┬──────────────────────────────────────────────────┘
        │
        ├─────► DatabaseConfiguration
        │
        ├─────► IDatabaseService ◄────── DatabaseService
        │               │
        │               ├─────► ISqlServerCapabilityDetector
        │               │
        │               └─────► SqlConnection (ADO.NET)
        │
        ├─────► IDatabaseContext ◄────── DatabaseContextService
        │               │                        │
        │               └────────────────────────┘
        │                      (wraps IDatabaseService)
        │
        ├─────► IServerDatabase ◄────── ServerDatabaseService
        │               │                        │
        │               └────────────────────────┘
        │                      (wraps IDatabaseService)
        │
        └─────► IQuerySessionManager ◄── QuerySessionManager
                        │
                        └─────► Background Sessions
```

---

## 4. Core Components

### 4.1 Application Layer Components

#### 4.1.1 DatabaseConfiguration
**Location**: `Core.Application/Models/DatabaseConfiguration.cs`

**Purpose**: Central configuration model for all database operations

**Properties**:
```csharp
- ConnectionString: string
- EnableExecuteQuery: bool (default: false)
- EnableExecuteStoredProcedure: bool (default: false)
- EnableStartQuery: bool (default: false)
- EnableStartStoredProcedure: bool (default: false)
- DefaultCommandTimeoutSeconds: int (default: 30)
- ConnectionTimeoutSeconds: int (default: 15)
- MaxConcurrentSessions: int (default: 10)
- SessionCleanupIntervalMinutes: int (default: 60)
- TotalToolCallTimeoutSeconds: int? (default: 120)
```

#### 4.1.2 QuerySessionManager
**Location**: `Core.Application/QuerySessionManager.cs`

**Purpose**: Manages background query and stored procedure sessions

**Key Features**:
- Session lifecycle management (create, monitor, retrieve, stop)
- Thread-safe concurrent session handling
- Automatic result caching
- Status tracking and error capture
- Session cleanup coordination

#### 4.1.3 ToolCallTimeoutContext
**Location**: `Core.Application/Models/ToolCallTimeoutContext.cs`

**Purpose**: Tracks total timeout for tool call operations

**Functionality**:
- Captures start time of tool call
- Calculates remaining timeout for operations
- Ensures operations complete within total timeout limit
- Prevents timeout overruns in MCP clients

### 4.2 Infrastructure Layer Components

#### 4.2.1 DatabaseService
**Location**: `Core.Infrastructure.SqlClient/DatabaseService.cs`

**Purpose**: Core database operations implementation

**Capabilities**:
- Query execution with context switching
- Table and database enumeration
- Schema discovery
- Stored procedure operations
- Capability detection integration
- Timeout management

#### 4.2.2 SqlServerCapabilityDetector
**Location**: `Core.Infrastructure.SqlClient/SqlServerCapabilityDetector.cs`

**Purpose**: Detects SQL Server version and feature capabilities

**Detected Capabilities**:
- Version information (major, minor, build)
- Edition (Enterprise, Standard, Express, Azure)
- Feature flags:
  - Partitioning
  - Columnstore indexes
  - JSON support
  - In-Memory OLTP
  - Row-level security
  - Dynamic data masking
  - Temporal tables
  - Graph database
  - Always Encrypted
  - And more...

#### 4.2.3 SessionCleanupService
**Location**: `Core.Infrastructure.SqlClient/SessionCleanupService.cs`

**Purpose**: Background service for cleaning up completed sessions

**Features**:
- Periodic cleanup timer
- Configurable cleanup interval
- Removes completed sessions older than threshold
- Hosted service lifecycle management

#### 4.2.4 Type Conversion Utilities

##### JsonParameterConverter
**Location**: `Core.Infrastructure.SqlClient/Utilities/JsonParameterConverter.cs`

**Purpose**: Converts JSON values to SQL Server types

**Supported Conversions**:
- String types (varchar, nvarchar, char, nchar, text, ntext)
- Numeric types (int, bigint, smallint, tinyint, decimal, numeric, float, real, money, smallmoney)
- Date/Time types (datetime, datetime2, date, time, smalldatetime, datetimeoffset)
- Binary types (binary, varbinary, image)
- Boolean type (bit)
- Special types (uniqueidentifier, xml, sql_variant, geometry, geography)

##### ParameterNormalizer
**Location**: `Core.Infrastructure.SqlClient/Utilities/ParameterNormalizer.cs`

**Purpose**: Normalizes parameter names for consistency

**Features**:
- Case-insensitive matching
- @ prefix handling
- Parameter validation against metadata

##### SqlTypeMapper
**Location**: `Core.Infrastructure.SqlClient/Utilities/SqlTypeMapper.cs`

**Purpose**: Maps SQL Server types to .NET types

### 4.3 MCP Server Components

#### 4.3.1 Tool Organization

**Database Mode Tools** (`Core.Infrastructure.McpServer/Tools/`):
- `ExecuteQueryTool.cs`
- `ListTablesTool.cs`
- `GetTableSchemaTool.cs`
- `ListStoredProceduresTool.cs`
- `GetStoredProcedureDefinitionTool.cs`
- `GetStoredProcedureParametersTool.cs`
- `ExecuteStoredProcedureTool.cs`
- `StartQueryTool.cs`
- `StartStoredProcedureTool.cs`

**Server Mode Tools**:
- `ServerExecuteQueryTool.cs`
- `ServerListTablesTool.cs`
- `ServerListDatabasesTool.cs`
- `ServerGetTableSchemaTool.cs`
- `ServerListStoredProceduresTool.cs`
- `ServerGetStoredProcedureDefinitionTool.cs`
- `ServerGetStoredProcedureParametersTool.cs`
- `ServerExecuteStoredProcedureTool.cs`
- `ServerStartQueryTool.cs`
- `ServerStartStoredProcedureTool.cs`

**Common Tools**:
- `ServerCapabilitiesTool.cs`
- `SessionManagementTools.cs`
- `TimeoutManagementTools.cs`

#### 4.3.2 Tool Pattern

Each tool follows a consistent pattern:
```csharp
[McpTool]
public class ToolName(IService service) : McpToolBase
{
    [McpToolCall]
    public async Task<string> Execute(
        [McpToolParameter] string requiredParam,
        [McpToolParameter] string? optionalParam = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Create timeout context
        // 2. Validate parameters
        // 3. Call service layer
        // 4. Format results
        // 5. Handle errors
    }
}
```

---

## 5. Operational Modes

### 5.1 Mode Detection

The application automatically detects the operational mode based on the connection string:

**Detection Logic**:
```csharp
public static bool IsServerMode(string connectionString)
{
    // Parse connection string for Database or Initial Catalog parameter
    // Returns true if no database specified (Server Mode)
    // Returns false if database specified (Database Mode)
}
```

### 5.2 Database Mode

**Triggered When**: Connection string contains `Database=` or `Initial Catalog=` parameter

**Characteristics**:
- All operations scoped to the connected database
- Simplified tool parameters (no database name required)
- Optimized for single-database workflows
- Tools registered: database-scoped tools only

**Example Connection String**:
```
Server=myserver;Database=Northwind;User Id=sa;Password=pass;TrustServerCertificate=True;
```

**Available Tools**:
- `execute_query`
- `list_tables`
- `get_table_schema`
- `list_stored_procedures`
- `get_stored_procedure_definition`
- `get_stored_procedure_parameters`
- `execute_stored_procedure`
- `start_query`
- `start_stored_procedure`
- `server_capabilities`
- `get_session_status`
- `get_session_results`
- `stop_session`
- `list_sessions`
- `get_command_timeout`
- `set_command_timeout`

### 5.3 Server Mode

**Triggered When**: Connection string does NOT contain database parameter

**Characteristics**:
- All operations require explicit database name
- Server-wide database listing capability
- Cross-database operation support
- Tools registered: server-scoped tools

**Example Connection String**:
```
Server=myserver;User Id=sa;Password=pass;TrustServerCertificate=True;
```

**Available Tools**:
- `execute_query_in_database`
- `list_databases`
- `list_tables_in_database`
- `get_table_schema_in_database`
- `list_stored_procedures_in_database`
- `get_stored_procedure_definition_in_database`
- `get_stored_procedure_parameters` (with database parameter)
- `execute_stored_procedure_in_database`
- `start_query_in_database`
- `start_stored_procedure_in_database`
- `server_capabilities`
- `get_session_status`
- `get_session_results`
- `stop_session`
- `list_sessions`
- `get_command_timeout`
- `set_command_timeout`

### 5.4 Mode Comparison

| Feature | Database Mode | Server Mode |
|---------|---------------|-------------|
| Database Selection | Fixed at connection | Dynamic per operation |
| Tool Complexity | Simpler (no DB param) | More complex (DB param required) |
| Use Case | Single database apps | Multi-database management |
| Database Listing | Not available | Available |
| Performance | Optimized for one DB | Flexible across DBs |
| Security Scope | Limited to one DB | Access to all accessible DBs |

---

## 6. Features and Capabilities

### 6.1 Query Execution

#### Synchronous Query Execution
- **Tools**: `execute_query`, `execute_query_in_database`
- **Features**:
  - Direct SQL query execution
  - Table-formatted results
  - Row count reporting
  - Custom timeout support
  - Cancellation support

#### Asynchronous Query Execution
- **Tools**: `start_query`, `start_query_in_database`
- **Features**:
  - Background execution
  - Session-based tracking
  - Progress monitoring
  - Result retrieval when complete
  - Long-running query support

### 6.2 Schema Discovery

#### Table Operations
- **List Tables**: Enumerate all tables with schema, row counts
- **Get Table Schema**: Detailed column information including:
  - Column names
  - Data types
  - Maximum lengths
  - Nullable constraints
  - Primary key information
  - Index information (version-dependent)

#### Database Operations (Server Mode Only)
- **List Databases**: Enumerate all databases with:
  - Database name
  - State (ONLINE, OFFLINE, etc.)
  - Size in MB
  - Owner
  - Compatibility level

### 6.3 Stored Procedure Management

#### Discovery
- **List Stored Procedures**: All procedures with:
  - Schema and name
  - Parameter count
  - Last execution time
  - Execution count
  - Creation date

#### Metadata
- **Get Definition**: SQL source code of procedure
- **Get Parameters**: Detailed parameter information:
  - Parameter names
  - SQL types
  - Required/optional status
  - Direction (INPUT/OUTPUT)
  - Default values
  - Output formats: Table or JSON Schema

#### Execution
- **Execute Stored Procedure**: 
  - Automatic JSON-to-SQL type conversion
  - Parameter validation against metadata
  - Case-insensitive parameter matching
  - @-prefix normalization
  - Output parameter support
  - Return value handling
  - Background execution support

### 6.4 Session Management

#### Session Lifecycle
1. **Creation**: Via `start_query` or `start_stored_procedure`
2. **Monitoring**: Via `get_session_status`
3. **Retrieval**: Via `get_session_results`
4. **Cancellation**: Via `stop_session`
5. **Cleanup**: Automatic via background service

#### Session Features
- Concurrent session support (configurable limit)
- Status tracking (running, completed, failed, cancelled)
- Result caching in memory
- Error capture and reporting
- Duration tracking
- Row count tracking

### 6.5 Timeout Management

#### Multiple Timeout Levels
1. **Connection Timeout**: Database connection establishment
2. **Default Command Timeout**: Standard operations (configurable)
3. **Per-Operation Timeout**: Override via tool parameter
4. **Total Tool Call Timeout**: Maximum time for any tool call
5. **Session Timeout**: Long-running background operations

#### Dynamic Timeout Control
- **Get Current Timeout**: Query current settings
- **Set Default Timeout**: Modify default for new operations
- **Runtime Adjustment**: Change timeouts without restart

### 6.6 Capability Detection

**Server Capabilities Tool** provides:
- SQL Server version (major, minor, build)
- Edition detection (Enterprise, Standard, Express, Azure)
- Platform detection (On-Premises, Azure SQL Database, Azure VM)
- Feature availability flags
- Current operational mode (server vs database)
- Capability-based query optimization

---

## 7. Security Model

### 7.1 Security Layers

#### Layer 1: Tool Enablement
**Default State**: All execution tools disabled

**Configuration Required For**:
- Query execution tools
- Stored procedure execution tools
- Session-based execution tools

**Benefits**:
- Principle of least privilege
- Explicit opt-in for write operations
- Prevents accidental data modification
- Audit trail through configuration

#### Layer 2: SQL Injection Prevention
- All queries use parameterized commands
- No string concatenation for SQL building
- Parameter type validation
- Input sanitization through SQL Server metadata

#### Layer 3: Connection Security
- Support for all SQL Server authentication methods:
  - SQL Server authentication
  - Windows authentication (non-Docker)
  - Azure AD authentication
- TLS/SSL encryption support
- Connection string security (stored in environment/config)

#### Layer 4: Database Permissions
- Respects SQL Server user permissions
- No privilege escalation
- Database-level access control
- Schema-level access control

### 7.2 Security Configuration

#### Minimal Security (Read-Only)
```json
{
  "DatabaseConfiguration": {
    "EnableExecuteQuery": false,
    "EnableExecuteStoredProcedure": false,
    "EnableStartQuery": false,
    "EnableStartStoredProcedure": false
  }
}
```

**Available Operations**: Schema discovery, listing only

#### Standard Security (Read + Execute)
```json
{
  "DatabaseConfiguration": {
    "EnableExecuteQuery": true,
    "EnableExecuteStoredProcedure": false,
    "EnableStartQuery": false,
    "EnableStartStoredProcedure": false
  }
}
```

**Available Operations**: Read queries, schema discovery

#### Full Security (All Operations)
```json
{
  "DatabaseConfiguration": {
    "EnableExecuteQuery": true,
    "EnableExecuteStoredProcedure": true,
    "EnableStartQuery": true,
    "EnableStartStoredProcedure": true
  }
}
```

**Available Operations**: All tool capabilities

### 7.3 Audit and Monitoring

**Logging Levels**:
- All operations logged to stderr
- Tool registration logged at startup
- Database mode detection logged
- Security configuration logged
- Error conditions logged with context

**Audit Trail**:
- Connection attempts
- Query executions
- Tool invocations
- Configuration changes
- Session lifecycle events

---

## 8. Configuration System

### 8.1 Configuration Sources (Priority Order)

1. **Environment Variables** (highest priority)
2. **User Secrets** (development only)
3. **appsettings.{Environment}.json**
4. **appsettings.json** (lowest priority)

### 8.2 Configuration Schema

```json
{
  "MSSQL_CONNECTIONSTRING": "Server=...;Database=...;...",
  
  "DatabaseConfiguration": {
    "EnableExecuteQuery": false,
    "EnableExecuteStoredProcedure": false,
    "EnableStartQuery": false,
    "EnableStartStoredProcedure": false,
    "DefaultCommandTimeoutSeconds": 30,
    "ConnectionTimeoutSeconds": 15,
    "MaxConcurrentSessions": 10,
    "SessionCleanupIntervalMinutes": 60,
    "TotalToolCallTimeoutSeconds": 120
  },
  
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

### 8.3 Environment Variable Mapping

**Docker Example**:
```bash
-e "MSSQL_CONNECTIONSTRING=Server=..."
-e "DatabaseConfiguration__EnableExecuteQuery=true"
-e "DatabaseConfiguration__DefaultCommandTimeoutSeconds=60"
```

**Windows Example**:
```powershell
$env:MSSQL_CONNECTIONSTRING="Server=..."
$env:DatabaseConfiguration__EnableExecuteQuery="true"
```

### 8.4 Connection String Formats

#### Standard SQL Server
```
Server=hostname,1433;Database=dbname;User Id=username;Password=password;TrustServerCertificate=True;
```

#### Windows Authentication
```
Server=hostname;Database=dbname;Integrated Security=SSPI;TrustServerCertificate=True;
```

#### Azure SQL Database
```
Server=servername.database.windows.net;Database=dbname;User Id=username;Password=password;Encrypt=True;
```

#### Server Mode (No Database)
```
Server=hostname;User Id=username;Password=password;TrustServerCertificate=True;
```

---

## 9. Data Models

### 9.1 Core Data Models

#### TableInfo
```csharp
public record TableInfo
{
    public string SchemaName { get; init; }
    public string TableName { get; init; }
    public long RowCount { get; init; }
    public DateTime? CreateDate { get; init; }
    public DateTime? ModifyDate { get; init; }
}
```

#### TableSchemaInfo
```csharp
public record TableSchemaInfo
{
    public string TableName { get; init; }
    public string SchemaName { get; init; }
    public List<ColumnInfo> Columns { get; init; }
    public List<IndexInfo> Indexes { get; init; }
}
```

#### DatabaseInfo
```csharp
public record DatabaseInfo
{
    public string Name { get; init; }
    public string State { get; init; }
    public decimal SizeMB { get; init; }
    public string Owner { get; init; }
    public int CompatibilityLevel { get; init; }
}
```

#### StoredProcedureInfo
```csharp
public record StoredProcedureInfo
{
    public string SchemaName { get; init; }
    public string ProcedureName { get; init; }
    public int ParameterCount { get; init; }
    public DateTime? LastExecutionTime { get; init; }
    public long? ExecutionCount { get; init; }
    public DateTime CreateDate { get; init; }
    public DateTime ModifyDate { get; init; }
}
```

#### QuerySession
```csharp
public class QuerySession
{
    public int SessionId { get; set; }
    public QuerySessionType Type { get; set; }
    public string Query { get; set; }
    public string? DatabaseName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsRunning { get; set; }
    public string Status { get; set; }
    public int? RowCount { get; set; }
    public string? Error { get; set; }
    public string? Results { get; set; }
    public int TimeoutSeconds { get; set; }
    public Dictionary<string, object?>? Parameters { get; set; }
}
```

---

## 10. API Specifications

### 10.1 MCP Tool Interface

All tools follow the MCP specification and use JSON-RPC 2.0 protocol.

**Request Format**:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "tool_name",
    "arguments": {
      "parameter1": "value1",
      "parameter2": "value2"
    }
  }
}
```

**Response Format (Success)**:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "Result content here"
      }
    ]
  }
}
```

**Response Format (Error)**:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "error": {
    "code": -32000,
    "message": "Error description"
  }
}
```

### 10.2 Tool Catalog

#### Database Mode Tools

**execute_query**
- **Parameters**: `query` (string, required), `timeoutSeconds` (int, optional)
- **Returns**: Formatted table results with row count
- **Security**: Requires `EnableExecuteQuery=true`

**list_tables**
- **Parameters**: None
- **Returns**: Table list with schema, row counts
- **Security**: Always enabled

**get_table_schema**
- **Parameters**: `tableName` (string, required)
- **Returns**: Column definitions, types, constraints
- **Security**: Always enabled

**list_stored_procedures**
- **Parameters**: None
- **Returns**: Procedure list with metadata
- **Security**: Always enabled

**get_stored_procedure_definition**
- **Parameters**: `procedureName` (string, required)
- **Returns**: SQL definition text
- **Security**: Always enabled

**get_stored_procedure_parameters**
- **Parameters**: `procedureName` (string, required), `format` (string, optional: "table" or "json")
- **Returns**: Parameter metadata in requested format
- **Security**: Always enabled

**execute_stored_procedure**
- **Parameters**: `procedureName` (string, required), `parameters` (JSON string, required)
- **Returns**: Execution results
- **Security**: Requires `EnableExecuteStoredProcedure=true`

**start_query**
- **Parameters**: `query` (string, required), `timeoutSeconds` (int, optional)
- **Returns**: Session ID and status
- **Security**: Requires `EnableStartQuery=true`

**start_stored_procedure**
- **Parameters**: `procedureName` (string, required), `parameters` (JSON string, optional), `timeoutSeconds` (int, optional)
- **Returns**: Session ID and status
- **Security**: Requires `EnableStartStoredProcedure=true`

#### Server Mode Tools

All database mode tools have server mode equivalents with `_in_database` suffix and additional `databaseName` parameter.

**Additional Server Mode Tools**:

**list_databases**
- **Parameters**: None
- **Returns**: Database list with state, size, owner
- **Security**: Always enabled

#### Common Tools

**server_capabilities**
- **Parameters**: None
- **Returns**: Server version, edition, features, mode
- **Security**: Always enabled

**get_session_status**
- **Parameters**: `sessionId` (int, required)
- **Returns**: Session status and metadata
- **Security**: Enabled if any session tool enabled

**get_session_results**
- **Parameters**: `sessionId` (int, required), `maxRows` (int, optional)
- **Returns**: Session results with row limit
- **Security**: Enabled if any session tool enabled

**stop_session**
- **Parameters**: `sessionId` (int, required)
- **Returns**: Cancellation confirmation
- **Security**: Enabled if any session tool enabled

**list_sessions**
- **Parameters**: `status` (string, optional: "all", "running", "completed")
- **Returns**: List of all sessions with filters
- **Security**: Enabled if any session tool enabled

**get_command_timeout**
- **Parameters**: None
- **Returns**: Current timeout configuration
- **Security**: Always enabled

**set_command_timeout**
- **Parameters**: `timeoutSeconds` (int, required: 1-3600)
- **Returns**: Confirmation of timeout change
- **Security**: Always enabled

---

## 11. Deployment Architecture

### 11.1 Deployment Options

#### Option 1: Native .NET Deployment
**Use Case**: Direct server installation, maximum performance

**Requirements**:
- .NET 9.0 Runtime or SDK
- SQL Server connectivity
- Windows, Linux, or macOS

**Deployment Steps**:
```bash
# Build the project
dotnet build -c Release

# Run the application
dotnet run --project src/Core.Infrastructure.McpServer/Core.Infrastructure.McpServer.csproj
```

**Configuration**:
- Use appsettings.json for base configuration
- Environment variables for secrets
- User secrets for development

#### Option 2: Docker Container Deployment
**Use Case**: Isolated environments, cloud deployment, consistent runtime

**Requirements**:
- Docker Engine 20.10+
- SQL Server accessible from container network

**Deployment Steps**:
```bash
# Pull from Docker Hub
docker pull aadversteeg/mssqlclient-mcp-server:latest

# Run container
docker run -d \
  --name mssql-mcp \
  -e "MSSQL_CONNECTIONSTRING=Server=..." \
  -e "DatabaseConfiguration__EnableExecuteQuery=true" \
  aadversteeg/mssqlclient-mcp-server:latest
```

**Docker Image Details**:
- Multi-stage build for minimal size
- Based on .NET 9.0 runtime image
- Non-root user execution
- Health check endpoint support

#### Option 3: Claude Desktop Integration
**Use Case**: AI assistant database access

**Configuration File**: `claude_desktop_config.json` (macOS/Linux) or `claude_desktop_config.json` (Windows)

**Native Integration**:
```json
{
  "mcpServers": {
    "mssql": {
      "command": "dotnet",
      "args": [
        "C:\\Path\\To\\Core.Infrastructure.McpServer.dll"
      ],
      "env": {
        "MSSQL_CONNECTIONSTRING": "Server=...",
        "DatabaseConfiguration__EnableExecuteQuery": "true"
      }
    }
  }
}
```

**Docker Integration**:
```json
{
  "mcpServers": {
    "mssql": {
      "command": "docker",
      "args": [
        "run", "--rm", "-i",
        "-e", "MSSQL_CONNECTIONSTRING=Server=...",
        "aadversteeg/mssqlclient-mcp-server:latest"
      ]
    }
  }
}
```

### 11.2 Network Architecture

#### Standard Deployment
```
┌─────────────────────┐
│   MCP Client        │
│   (Claude, etc.)    │
└──────────┬──────────┘
           │ stdio
           │
┌──────────▼──────────┐
│  MCP Server         │
│  (Local Process)    │
└──────────┬──────────┘
           │ TCP 1433
           │
┌──────────▼──────────┐
│  SQL Server         │
│  (Database Server)  │
└─────────────────────┘
```

#### Docker Deployment with External SQL Server
```
┌─────────────────────┐
│   MCP Client        │
└──────────┬──────────┘
           │ stdio
┌──────────▼──────────┐
│  Docker Container   │
│  ┌────────────────┐ │
│  │  MCP Server    │ │
│  └────────┬───────┘ │
└───────────┼─────────┘
            │ TCP 1433
            │ (Docker network)
┌───────────▼─────────┐
│  SQL Server         │
│  (External)         │
└─────────────────────┘
```

### 11.3 Production Considerations

#### High Availability
- Deploy multiple instances behind load balancer
- Connection pooling for database connections
- Session state stored in database (future enhancement)
- Health monitoring and auto-restart

#### Security Hardening
- Use managed identities for Azure SQL
- Implement certificate-based authentication
- Enable SQL Server audit logging
- Restrict network access via firewall rules
- Use least-privilege SQL Server accounts

#### Monitoring and Logging
- Structured logging to stderr
- Integration with logging aggregators (Seq, ELK, Splunk)
- Performance metrics collection
- Session tracking and analytics
- Error rate monitoring

---

## 12. Testing Strategy

### 12.1 Test Project Organization

```
src/
  UnitTests.Application/           - Application layer unit tests
  UnitTests.Infrastructure.McpServer/ - MCP tool unit tests
  UnitTests.Infrastructure.SqlClient/ - Database service unit tests

tst/
  IntegrationTests/                - End-to-end integration tests
  UnitTests.Testing.ModelContextProtocol/ - Test infrastructure tests
  Ave.Testing.ModelContextProtocol/ - MCP test framework
```

### 12.2 Unit Testing

#### Test Coverage Areas
1. **Application Layer** (UnitTests.Application)
   - QuerySessionManager lifecycle
   - ToolCallTimeoutContext calculations
   - Model validation

2. **Infrastructure Layer** (UnitTests.Infrastructure.SqlClient)
   - DatabaseService operations
   - ServerDatabaseService operations
   - DatabaseContextService operations
   - SqlServerCapabilityDetector
   - Type conversion utilities
   - Parameter normalization

3. **MCP Server Layer** (UnitTests.Infrastructure.McpServer)
   - All tool implementations
   - Extension methods
   - Error handling
   - Result formatting

#### Testing Approach
- **Mocking**: Moq library for interface mocking
- **Test Data**: Realistic SQL Server metadata
- **Assertions**: xUnit assertion library
- **Coverage Target**: 80%+ code coverage

### 12.3 Integration Testing

#### Test Infrastructure
**Location**: `tst/IntegrationTests/`

**Components**:
- Docker Compose for SQL Server test instance
- MCP client test harness
- Test fixtures for lifecycle management
- Port management for parallel test execution

#### Test Scenarios
1. **End-to-End Tool Execution**
   - Query execution with real database
   - Schema discovery accuracy
   - Stored procedure execution
   - Session management

2. **Mode Switching**
   - Database mode operations
   - Server mode operations
   - Cross-database queries

3. **Timeout Handling**
   - Operation timeout enforcement
   - Total tool call timeout
   - Session timeout behavior

4. **Error Scenarios**
   - Invalid SQL queries
   - Connection failures
   - Permission errors
   - Timeout conditions

### 12.4 Test Execution

#### Local Development
```bash
# Run all unit tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportsDirectory=./coverage

# Run integration tests
cd tst/IntegrationTests
docker-compose up -d
dotnet test
docker-compose down
```

#### CI/CD Pipeline
- Automated test execution on PR
- Code coverage reporting
- Integration test environment provisioning
- Performance regression testing

---

## 13. Dependencies

### 13.1 NuGet Packages

#### Core Application
```xml
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.0" />
```

#### Database Access
```xml
<PackageReference Include="Microsoft.Data.SqlClient" Version="5.x" />
```

#### MCP Protocol
```xml
<PackageReference Include="ModelContextProtocol.NET" Version="Latest" />
```

#### Testing
```xml
<PackageReference Include="xUnit" Version="2.x" />
<PackageReference Include="Moq" Version="4.x" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.x" />
```

### 13.2 External Dependencies

#### Runtime Requirements
- .NET 9.0 Runtime
- SQL Server 2016+ or Azure SQL Database
- Network connectivity to SQL Server

#### Development Requirements
- .NET 9.0 SDK
- Docker Desktop (for containerization)
- SQL Server (local or remote)
- IDE: Visual Studio 2022, VS Code, or Rider

### 13.3 Dependency Management

#### Version Pinning Strategy
- Major versions pinned in project files
- Minor version updates allowed for patches
- Regular dependency audits for vulnerabilities
- Automated dependency updates via Dependabot

#### Security Scanning
- Regular NuGet package vulnerability scans
- OWASP dependency check integration
- Security advisory monitoring

---

## 14. Performance Considerations

### 14.1 Database Connection Management

#### Connection Pooling
- Enabled by default in Microsoft.Data.SqlClient
- Configurable pool size (default: 100)
- Connection lifetime management
- Automatic connection recycling

**Optimization**:
```
Server=...;Min Pool Size=10;Max Pool Size=100;Connection Lifetime=1800;
```

#### Connection Timeout Tuning
- Default: 15 seconds
- Increase for slow networks
- Decrease for fast failure detection

### 14.2 Query Optimization

#### Result Set Management
- Streaming results via IAsyncDataReader
- Memory-efficient row-by-row processing
- Configurable row limits for large result sets
- Automatic cleanup of completed sessions

#### Query Patterns
- Use of EXISTS instead of COUNT where appropriate
- Efficient metadata queries with system views
- Capability-based query selection
- Index hints for specific SQL Server versions

### 14.3 Session Management Performance

#### Concurrent Session Limits
- Default: 10 concurrent sessions
- Configurable based on server resources
- Session queue for overflow handling
- Background cleanup to prevent memory leaks

#### Memory Management
- Result caching in memory (configurable limit)
- Automatic cleanup of old sessions
- Garbage collection optimization
- Large object heap considerations

### 14.4 Scalability Considerations

#### Vertical Scaling
- Increase max concurrent sessions
- Larger connection pool
- More memory for result caching
- Faster CPU for complex queries

#### Horizontal Scaling
- Multiple server instances
- Load balancing across instances
- Shared configuration via environment
- Session state externalization (future)

### 14.5 Performance Monitoring

#### Key Metrics
- Query execution time
- Session queue depth
- Connection pool utilization
- Memory usage
- Error rates

#### Optimization Techniques
- Query plan caching
- Metadata caching
- Connection reuse
- Asynchronous operations throughout

---

## 15. Error Handling

### 15.1 Error Categories

#### SQL Errors
**Examples**:
- Syntax errors in queries
- Permission denied
- Table/procedure not found
- Connection failures
- Timeout errors

**Handling**:
```csharp
try
{
    // Database operation
}
catch (SqlException ex)
{
    return $"Error: SQL error while {operation}: {ex.Message}";
}
```

#### Configuration Errors
**Examples**:
- Missing connection string
- Invalid timeout values
- Malformed JSON parameters

**Handling**:
- Validation at startup
- Clear error messages
- Graceful degradation where possible

#### MCP Protocol Errors
**Examples**:
- Invalid tool parameters
- Unsupported operations
- Protocol violations

**Handling**:
- Parameter validation before execution
- Standardized error responses
- Client-friendly error messages

### 15.2 Error Format Standardization

#### Consistent Error Messages
**Pattern**: `"Error: {context} while {operation}: {details}"`

**Examples**:
```
Error: SQL error while executing query: Invalid column name 'xyz'
Error: Database error while listing tables: Database 'NotFound' does not exist
Error: Timeout while executing stored procedure: Operation exceeded 30 seconds
```

#### Error Response Structure
```json
{
  "error": {
    "code": -32000,
    "message": "Descriptive error message",
    "data": {
      "operation": "execute_query",
      "sqlError": "Detailed SQL error if applicable"
    }
  }
}
```

### 15.3 Error Recovery Strategies

#### Transient Errors
- Automatic retry for connection failures
- Exponential backoff for throttling
- Circuit breaker for persistent failures

#### Permanent Errors
- Clear error message to user
- Logging for diagnostics
- No automatic retry
- Graceful failure

### 15.4 Logging Strategy

#### Log Levels
- **Error**: Unhandled exceptions, critical failures
- **Warning**: Recoverable errors, deprecated features
- **Information**: Tool invocations, configuration
- **Debug**: Detailed execution flow (development)

#### Log Destinations
- Console (stderr) for all levels
- File logging (optional, via configuration)
- Structured logging for aggregation
- Correlation IDs for request tracking

---

## 16. Future Enhancements

### 16.1 Planned Features

#### Enhanced Session Management
- **Persistent Sessions**: Session state in database
- **Session Sharing**: Multiple clients access same session
- **Session Snapshots**: Save/restore session state
- **Session Analytics**: Historical session metrics

#### Advanced Query Features
- **Query Plans**: Return execution plans
- **Query Statistics**: Performance metrics
- **Query Hints**: Optimization hints support
- **Batch Operations**: Multiple queries in one call

#### Extended Schema Discovery
- **View Metadata**: Detailed view information
- **Function Discovery**: User-defined functions
- **Trigger Information**: Trigger metadata
- **Constraint Details**: Check constraints, foreign keys

#### Performance Improvements
- **Metadata Caching**: Cache schema information
- **Query Plan Caching**: Reuse execution plans
- **Connection Pooling Optimization**: Advanced pool management
- **Parallel Query Execution**: Multiple queries concurrently

### 16.2 Integration Enhancements

#### MCP Resources
- **Schema as Resources**: Expose table schemas as MCP resources
- **Procedure Library**: Stored procedures as callable resources
- **Query Templates**: Parameterized query templates

#### Additional Protocols
- **HTTP/REST**: REST API alongside MCP
- **WebSocket**: Real-time query updates
- **gRPC**: High-performance protocol option

### 16.3 Security Enhancements

#### Authentication
- **OAuth 2.0**: Modern authentication flow
- **API Keys**: Token-based access control
- **Certificate Auth**: Mutual TLS support

#### Authorization
- **Role-Based Access**: Fine-grained permissions
- **Row-Level Security**: Data filtering by user
- **Audit Logging**: Comprehensive audit trail
- **Data Masking**: Sensitive data protection

### 16.4 Monitoring and Observability

#### Metrics
- **Prometheus Integration**: Metrics endpoint
- **Custom Metrics**: Business-specific metrics
- **Performance Counters**: Windows performance integration

#### Tracing
- **OpenTelemetry**: Distributed tracing
- **Correlation IDs**: Request tracking
- **Span Annotations**: Detailed operation tracking

#### Health Checks
- **Liveness Probe**: Container health
- **Readiness Probe**: Database connectivity
- **Dependency Health**: SQL Server availability

### 16.5 Developer Experience

#### CLI Tools
- **Configuration Wizard**: Interactive setup
- **Schema Export**: Export database schema
- **Test Data Generator**: Sample data creation

#### Documentation
- **Interactive API Docs**: Swagger-like interface
- **Code Examples**: Language-specific samples
- **Video Tutorials**: Getting started guides

#### IDE Integration
- **VS Code Extension**: Database explorer
- **IntelliSense**: Auto-completion for queries
- **Debugging Support**: Query debugging tools

---

## Appendix A: Glossary

**MCP (Model Context Protocol)**: A standardized protocol for AI assistants to interact with external systems and data sources.

**Session**: A background execution context for long-running queries or stored procedures.

**Tool**: An MCP-callable operation that performs a specific database function.

**Database Mode**: Operational mode when connected to a specific database.

**Server Mode**: Operational mode when connected to SQL Server without a specific database.

**Timeout Context**: A tracking mechanism for total tool call timeout enforcement.

---

## Appendix B: References

- **MCP Specification**: https://modelcontextprotocol.io
- **SQL Server Documentation**: https://docs.microsoft.com/sql
- **.NET Documentation**: https://docs.microsoft.com/dotnet
- **Project Repository**: https://github.com/aadversteeg/mssqlclient-mcp-server
- **Docker Hub**: https://hub.docker.com/r/aadversteeg/mssqlclient-mcp-server

---

## Document Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-10-27 | Auto-generated | Initial reverse-engineered specification |

---

**End of Document**
