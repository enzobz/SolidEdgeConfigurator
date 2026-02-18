# SolidEdgeConfigurator

## Overview
SolidEdgeConfigurator is a powerful modular configuration tool for Solid Edge environments. The application uses a database-driven architecture to generate Bills of Materials (BOM) and manage product configurations without dependency on physical folder structures or CAD file extraction.

## Architecture

The application follows a **layered architecture** with clear separation of concerns:

### Layers

1. **Domain Layer** (`Domain/`)
   - **Entities**: Core business objects (Category, Option, Module, Part, etc.)
   - **Interfaces**: Repository contracts defining data access patterns

2. **Infrastructure Layer** (`Infrastructure/`)
   - **Repositories**: Concrete implementations of domain interfaces
   - **Data Services**: BOMService, DataSeedingService
   - Database access and persistence logic

3. **Application Layer** (`Application/`)
   - **Use Cases**: Business logic orchestration (GenerateBOM, GetCategoriesWithOptions)
   - **DTOs**: Data Transfer Objects for cross-layer communication

4. **Presentation Layer** (`Presentation/`)
   - **Windows**: WPF UI components
   - User interaction and display logic

### Legacy Components

- **Models**: Legacy domain models (being phased out)
- **Services**: Legacy services including DatabaseService (migrated to use new schema)

## Key Concepts

### Database as Source of Truth

The new architecture uses the database as the single source of truth for:
- Product configurations
- Bill of Materials generation
- Module and part relationships

This eliminates dependencies on:
- Physical folder structures
- Prefix-based component naming
- CAD file scanning for BOM generation

### Modular Configuration Flow

The configuration process follows this flow:

```
Selected Options → Activated Modules → Parts → Consolidated BOM
```

1. **Categories and Options**: Users select options from predefined categories
2. **Module Activation**: Selected options activate specific modules
3. **Part Collection**: Each module contains parts with quantities
4. **BOM Consolidation**: Parts are consolidated with total quantities across all modules

### Immutable IDs vs Mutable Codes

- **IDs**: Internal immutable keys (database primary keys)
- **Codes**: Business identifiers that can be changed (e.g., "COL_700x1000", "PART_001")

This separation allows business codes to evolve without breaking referential integrity.

## Database Schema

### Core Tables

#### Categories
Logical groupings for options (e.g., "Columns Size", "IP Rating")
- `Id` (PK), `Code`, `Name`, `Description`, `DisplayOrder`, `IsActive`

#### Options
Selectable choices within categories (e.g., "700x1000" for Columns)
- `Id` (PK), `Code`, `Name`, `Description`, `CategoryId` (FK), `DisplayOrder`, `IsActive`, `IsDefault`

#### Modules
Assemblies/subassemblies that can be included in configurations
- `Id` (PK), `Code`, `Name`, `Description`, `MasterAssemblyPath`, `IsActive`

#### Parts
Physical components with pricing information
- `Id` (PK), `Code`, `Name`, `PartNumber`, `Description`, `UnitPrice`, `Supplier`, `Unit`, `ComponentName`, `IsActive`

#### OptionModules (Junction)
Defines which modules are activated by which options
- `Id` (PK), `OptionId` (FK), `ModuleId` (FK), `Quantity`

#### ModuleParts (Junction)
Defines which parts belong to which modules
- `Id` (PK), `ModuleId` (FK), `PartId` (FK), `Quantity`

## Features

### Modular Configuration
- **Dynamic UI**: Categories and options loaded from database
- **Option Selection**: Select one option per category
- **Real-time BOM**: Generate BOM instantly from selected options
- **Module Display**: See which modules are activated by selections

### BOM Generation
- **100% Database-Driven**: No CAD file scanning required
- **Quantity Consolidation**: Automatic consolidation of duplicate parts
- **Export Options**: CSV and HTML export formats
- **Cost Calculation**: Real-time total cost and item count

### Data Management
- **Sample Data Seeding**: Built-in sample data for testing
- **Part Management**: Add, edit, and manage parts
- **Flexible Schema**: Easy to extend with new categories and options

### CAD Integration (Separate from BOM)
- **Assembly Generation**: Uses `MasterAssemblyPath` per module
- **Solid Edge Service**: Maintains integration with Solid Edge for physical assembly generation
- **Clear Separation**: BOM generation (DB) is independent of assembly generation (CAD)

## Requirements
- **Operating System**: Windows 10 or later
- **.NET**: .NET 6.0 or later
- **Solid Edge**: Version 2023 or later (optional, for CAD assembly generation)
- **Database**: SQLite (included via NuGet)

## Installation Instructions
1. Download the latest release from the [Releases](https://github.com/enzobz/SolidEdgeConfigurator/releases) page
2. Unzip the downloaded package
3. Run `SolidEdgeConfigurator.exe`

## Usage Workflow

### Initial Setup
1. Launch the application
2. Click **"Seed Sample Data"** to populate the database with example data
3. The database is created automatically at `%AppData%\SolidEdgeConfigurator\SolidEdgeConfigurator.db`

### Creating a Configuration
1. Click **"Modular Config"** button
2. Select options from each category
3. Click **"Generate BOM"** to see parts list
4. Review activated modules and parts
5. Click **"Export BOM"** to save as CSV or HTML

### Managing Parts
1. Click **"Manage Parts"** button
2. Add, edit, or delete parts
3. Set pricing, suppliers, and descriptions

### Legacy CAD Workflow (Optional)
1. Use the tabbed interface for traditional workflow
2. Load assembly files from folders
3. Configure using dropdowns
4. Generate Solid Edge assemblies

## Configuration Settings
- **Database Location**: `%AppData%\SolidEdgeConfigurator\SolidEdgeConfigurator.db`
- **Logging**: Console output via Serilog
- **Master Assembly Paths**: Configured per module in database

## Development Setup
1. Clone the repository:
   ```bash
   git clone https://github.com/enzobz/SolidEdgeConfigurator.git
   ```
2. Navigate to the project directory:
   ```bash
   cd SolidEdgeConfigurator
   ```
3. Restore dependencies:
   ```bash
   dotnet restore
   ```
4. Build the project:
   ```bash
   dotnet build
   ```
5. Run the application:
   ```bash
   dotnet run
   ```

## Extending the System

### Adding a New Category
1. Insert into `Categories` table with code and name
2. Add options for the category in `Options` table
3. Link options to modules via `OptionModules` table

### Adding a New Module
1. Insert into `Modules` table with code, name, and MasterAssemblyPath
2. Link to options via `OptionModules` table
3. Link to parts via `ModuleParts` table

### Adding New Parts
1. Use "Manage Parts" window, or
2. Insert directly into `Parts` table with unique code
3. Link to modules via `ModuleParts` table

## Migration from Legacy System

The system maintains backward compatibility with the legacy schema while introducing the new modular architecture:

- **Legacy Parts Table**: Automatically migrated to include `Code` and `IsActive` fields
- **Coexistence**: Both old and new configuration methods work side-by-side
- **Gradual Migration**: Existing data is preserved and enhanced

## Roadmap
- ✅ **Q1 2026**: Modular configuration architecture
- ✅ **Q1 2026**: Database-driven BOM generation
- **Q2 2026**: Enhanced UI with configuration templates
- **Q3 2026**: REST API for external integrations
- **Q4 2026**: Multi-language support

## Troubleshooting
- **Database Issues**: Delete `%AppData%\SolidEdgeConfigurator\SolidEdgeConfigurator.db` and restart to recreate
- **No Categories Found**: Click "Seed Sample Data" button
- **Solid Edge Not Available**: CAD features disabled but BOM generation still works
- **Migration Errors**: Check logs in application console

## Contributing Guidelines
- Fork the repository
- Create a feature branch (`git checkout -b feature/AmazingFeature`)
- Follow the layered architecture pattern
- Add tests for new use cases
- Commit your changes (`git commit -m 'Add some AmazingFeature'`)
- Push to the branch (`git push origin feature/AmazingFeature`)
- Open a pull request

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments
- Built with WPF and .NET 6
- SQLite for embedded database
- Serilog for structured logging
- ClosedXML for Excel export capabilities