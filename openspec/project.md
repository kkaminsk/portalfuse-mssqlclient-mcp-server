# Project Context

## Purpose
The SQL Server MCP Client is a Microsoft SQL Server database client that implements the Model Context Protocol (MCP). It provides a standardized, secure set of MCP tools for query execution, schema discovery, and stored procedure management. The system supports two operational modes (Database Mode and Server Mode), emphasizes security-first defaults (execution tools disabled by default), and targets reliable, production-ready operation with comprehensive timeout and session management.

## Tech Stack
- .NET 9.0 (C# 13)
- ModelContextProtocol.NET (MCP)
- Microsoft.Data.SqlClient (ADO.NET)
- Microsoft.Extensions.* (Configuration, DependencyInjection, Logging, Hosting)
- xUnit (tests)
- Moq (mocking)
- Docker (containerization)

## Project Conventions

### Code Style
Idiomatic modern C# 13 with asynchronous patterns.
- Interfaces for all external dependencies (Dependency Inversion)
- Records for immutable models where applicable (e.g., TableInfo, DatabaseInfo)
- Async methods with `Task` and `CancellationToken`
- Logging via `Microsoft.Extensions.Logging` to stderr
- Configuration via `Microsoft.Extensions.Configuration` (env, user secrets, appsettings)

### Architecture Patterns
Clean Architecture with layered design and a three-tier interface hierarchy.
- Layers: Presentation (MCP tools), Application (interfaces/models), Infrastructure (SqlClient services), Data Access (ADO.NET)
- Three tiers: `IDatabaseService` (core), `IDatabaseContext` (database-scoped), `IServerDatabase` (server-wide)
- Principles: Clean Architecture, Dependency Inversion, Single Responsibility, Open/Closed, Interface Segregation

### Testing Strategy
Multi-layered testing with unit and integration coverage.
- Unit tests: Application, MCP tools, SqlClient services, utilities (xUnit + Moq)
- Integration tests: End-to-end against real SQL Server (Docker Compose), covering mode switching, timeouts, and error scenarios
- Coverage target: 80%+
- Test infra under `tst/` and `src/UnitTests.*`

### Git Workflow
TBD

## Domain Context
MCP-driven SQL Server access with two modes:
- Database Mode: Operations scoped to a specific database; simpler tool parameters
- Server Mode: Cross-database operations; explicit `databaseName` parameter and database listing
Key features: schema discovery, stored procedure discovery/execution with JSON-to-SQL type conversion, capability detection, session-based background execution, and robust timeout management.
Primary users: AI assistants and developers needing secure, auditable SQL Server interactions.

## Important Constraints
- Security-first: All execution tools disabled by default; explicit opt-in required
- Permission-respecting: No privilege escalation; relies on SQL Server permissions
- Timeout controls: Connection, command, per-operation, total tool-call, and session timeouts
- Concurrency limits: Default max 10 concurrent sessions (configurable)
- Supported targets: SQL Server 2016+ and Azure SQL Database
- Cross-platform: Runs on .NET 9 (Windows, Linux, macOS); Docker image available

## External Dependencies
- Microsoft SQL Server / Azure SQL Database (network connectivity required)
- NuGet packages: Microsoft.Data.SqlClient; Microsoft.Extensions.*; ModelContextProtocol.NET; xUnit; Moq; Microsoft.NET.Test.Sdk
- Docker (for containerization and integration tests)
- MCP clients (e.g., Claude Desktop) for runtime integration
