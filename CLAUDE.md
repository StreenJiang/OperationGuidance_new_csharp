# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a **C# .NET 6.0 Windows Forms application** for industrial operation guidance and device management. The solution consists of multiple projects:

- **OperationGuidance_new** - Main WinForms application (UI layer)
- **CustomLibrary** - Reusable UI components library
- **OperationGuidance_service** - Business logic and services layer
- **OperationGuidanceNew_Setup** - MSI installer project

## Build & Development Commands

### Building the Solution
```bash
# Build the entire solution
dotnet build

# Build in Release mode
dotnet build -c Release

# Build a specific project
dotnet build OperationGuidance_new/OperationGuidance_new.csproj

# Clean build artifacts
dotnet clean
```

### Running the Application
```bash
# Run the main application
dotnet run --project OperationGuidance_new

# Or use Visual Studio / debugging
```

### .NET CLI
- **SDK Version**: .NET 8.0.303
- **Target Framework**: net6.0-windows
- **No tests exist** in this repository

## Code Architecture

### 1. Main Application (`OperationGuidance_new/`)
**Entry Point**: `Program.cs`
- Initializes dependency injection (`DependencyInjector.Initialize()`)
- Uses mutex to prevent multiple instances
- Launches `MainForm` as the main UI

**Key Directories**:
- `Views/` - All UI views (~70 view files for different product variants)
- `Configs/` - Product-specific configurations (GLB, SCII, SCII_XT, TZYX variants)
- `Utils/` - Utility classes
- `PLC/` - PLC communication code (Modbus, Siemens S7)
- `Tasks/` - Background tasks

**Configuration Files** (in root):
- `DatabaseConfigs.ini` - Database configuration
- `GlbPlcConfig.ini` - GLB PLC configuration
- `HttpConfig.ini` - HTTP server configuration
- `MesConfig.ini` - MES integration configuration
- `SciiBatchConfig.ini`, `SciiXtConfig.ini` - Product-specific settings
- `Settings.ini` - Application settings

### 2. Custom Library (`CustomLibrary/`)
Reusable UI components shared across the application:
- `Buttons/` - Custom button implementations
- `Panels/` - Menu panels, content panels, abstract base classes
- `ComboBoxes/` - Custom combo box controls
- `PictureBoxes/` - Image handling controls
- `TextBoxes/` - Text input controls
- `Utils/WidgetUtils.cs` - Common UI utilities

### 3. Service Layer (`OperationGuidance_service/`)
Business logic and data access layer with **70+ service classes**:
- **Device Services**: `DeviceCommunicationService`, `DeviceArmService`, `DeviceIoService`, `DeviceSerialPortService`
- **Data Services**: `OperationDataService`, `MissionRecordService`, `ProductMissionService`
- **Database Services**: `DapperDBService` (supports SQLite and MySQL)
- **Bar Code Services**: `PartsBarCodeService`, `MatCodeMapWhycService`
- **Supporting Services**: `UserAccountInfoService`, `WorkstationService`, `CurveDataService`

**Key Directories**:
- `Services/` - All service implementations
- `Database/` - Database models and migrations
- `Models/` - Data models
- `Controllers/` - HTTP controllers for web interface
- `Wrapper/` - External library wrappers

### 4. Dependencies
**UI & Visualization**:
- `LiveCharts.WinForms.NetCore3`, `LiveChartsCore.SkiaSharpView.WinForms` - Charts
- `ClosedXML` - Excel file manipulation
- `MouseKeyHook` - Global input handling

**Communication**:
- `EasyModbusTCP` - Modbus TCP communication
- `S7netplus` - Siemens PLC communication
- `SerialPortStream`, `System.IO.Ports` - Serial port communication

**Data & Database**:
- `Dapper` - ORM
- `Microsoft.EntityFrameworkCore.Sqlite` - SQLite database
- `MySql.Data` - MySQL database
- `System.Data.SQLite` - SQLite provider

**Other**:
- `log4net` - Logging
- `Newtonsoft.Json` - JSON serialization
- `Microsoft.Extensions.DependencyInjection` - DI container
- `WmiLight` - Windows Management Instrumentation

## Product Variants & Configuration

The application supports multiple product variants through configuration files:
- **GLB** - Standard variant with PLC support
- **SCII** / **SCII_XT** - Product-specific variants
- **TZYX** - Another product variant
- **Http** - HTTP server variant

Configuration is managed in `Configs/` directory with variant-specific classes:
- `ConfigBase.cs`, `ConfigsVariables.cs`, `SystemConfigs.cs`
- `Config_TZYX.cs`, `ConfigName_Http.cs`, `ConfigName_SCII_XT.cs`
- `PlcTagConfig_GLB.cs`, `PlcConfigsKeys_GLB.cs`

## Development Notes

### Nullable Reference Types
The projects have nullable reference types enabled, but there are many **CS8618 warnings** about non-nullable properties that need to be initialized. Consider updating these to allow null or ensure proper initialization.

### Common Issues
- Some packages show compatibility warnings with .NET 6.0 (EasyModbusTCP, OpenTK)
- Nullability warnings in `CustomLibrary` components
- Code analysis warnings (CA2200) about exception rethrowing

### Editor Configuration
- `.editorconfig` - Enforces UTF-8 encoding
- `.vscode/settings.json` - Minimal VS Code settings
- `.claude/settings.local.json` - Claude Code permissions configured

### Logging
All projects use `log4net` for logging. Check configuration files for log4net settings.

### Database Support
Multiple database backends are supported:
- **SQLite** (primary) - via Entity Framework Core and System.Data.SQLite
- **MySQL** - via MySql.Data package
- Access layer uses **Dapper** ORM

### PLC Communication
The application supports communication with industrial PLCs:
- **Modbus TCP** - via EasyModbusTCP library
- **Siemens S7** - via S7netplus library

## Key Implementation Patterns

1. **Dependency Injection** - Configured in `OperationGuidance_service/Configurations/`
2. **Service Layer Pattern** - 70+ services in `OperationGuidance_service/Services/`
3. **Abstract View Pattern** - Base classes in `Views/AbstractViews/`
4. **Product Variant Pattern** - Configuration-based feature toggles
5. **Database Abstraction** - Support for multiple database providers

## Branch Information

- **Current Branch**: v2.0.x
- **Main Branch**: master
- **Recent Commits**: Focus on data uploading improvements, upper cover requests, and type fixes
