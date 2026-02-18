# Migration Guide: Legacy to Modular Architecture

## Overview

This document explains the migration from the legacy CAD-extraction based BOM generation to the new modular, database-driven architecture.

## Key Changes

### Before (Legacy System)
- **BOM Generation**: Scanned .asm files in physical folder structures
- **Part Discovery**: Extracted parts from CAD assemblies using SolidEdgeService
- **Configuration**: Hardcoded dropdown options in UI
- **Dependencies**: Required CAD files and specific folder organization

### After (Modular Architecture)
- **BOM Generation**: 100% database-driven, no CAD dependency
- **Part Discovery**: Parts defined in database with pricing and suppliers
- **Configuration**: Dynamic options loaded from database Categories/Options
- **Dependencies**: Only requires database; CAD integration is separate

## Architecture Comparison

### Legacy Flow
```
Physical Folders → .ASM Files → CAD Extraction → Parts → BOM
```

### New Modular Flow
```
Database → Categories/Options → Modules → Parts → Consolidated BOM
```

## Migration Steps

### Step 1: Understand the New Schema

The new system uses these core tables:
- **Categories**: Logical groupings (e.g., "Columns", "IP Rating")
- **Options**: Selectable choices within categories
- **Modules**: Assemblies/subassemblies
- **Parts**: Physical components with pricing
- **OptionModules**: Links options to modules
- **ModuleParts**: Links modules to parts

### Step 2: Seed Initial Data

Use the built-in seeding service:

```csharp
// In MainWindow, click "Seed Sample Data" button
// Or programmatically:
var seedingService = new DataSeedingService(
    categoryRepo, optionRepo, moduleRepo, 
    partRepo, optionModuleRepo, modulePartRepo);
seedingService.SeedSampleData();
```

### Step 3: Migrate Existing Parts

If you have existing parts in the legacy `Parts` table:

1. The migration happens automatically when the database initializes
2. Legacy parts get a generated `Code` field (e.g., "PART_1", "PART_2")
3. You can update codes manually or through the Parts Management UI

### Step 4: Define Your Configuration Model

1. **Create Categories**:
   ```sql
   INSERT INTO Categories (Code, Name, Description, DisplayOrder, IsActive)
   VALUES ('COLUMNS', 'Column Size', 'Available column dimensions', 1, 1);
   ```

2. **Add Options**:
   ```sql
   INSERT INTO Options (Code, Name, Description, CategoryId, DisplayOrder, IsActive, IsDefault)
   VALUES ('COL_700', '700x1000', '700x1000mm column', 1, 1, 1, 1);
   ```

3. **Define Modules**:
   ```sql
   INSERT INTO Modules (Code, Name, Description, MasterAssemblyPath, IsActive)
   VALUES ('MOD_COL_700', 'Column 700x1000', 'Standard column', 'C:\Asm\Col700.asm', 1);
   ```

4. **Link Options to Modules**:
   ```sql
   INSERT INTO OptionModules (OptionId, ModuleId, Quantity)
   VALUES (1, 1, 4); -- 4 columns per configuration
   ```

5. **Link Modules to Parts**:
   ```sql
   INSERT INTO ModuleParts (ModuleId, PartId, Quantity)
   VALUES (1, 10, 1), (1, 15, 4); -- 1 profile + 4 brackets per column
   ```

### Step 5: Use the New UI

1. Launch the application
2. Click "Modular Config" button
3. Select options from each category
4. Click "Generate BOM" to see results
5. Export as CSV or HTML

## Coexistence Period

Both systems can coexist:
- **Legacy UI**: Tab-based interface with manual dropdowns
- **New UI**: Modular Configuration window with dynamic options
- **Legacy Services**: PartHierarchyService still available (deprecated)
- **New Services**: BOMService, DataSeedingService

## Deprecated Components

### Services
- `PartHierarchyService`: Marked with `[Obsolete]` attribute
  - Still functional for part discovery
  - Use `BOMService` for BOM generation instead

### Models
- Legacy `Models/Part.cs`: Replaced by `Domain/Entities/Part.cs`
  - Old model has `PartName`, `Quantity` properties
  - New model has `Code`, `Name`, `IsActive` properties
  - Database migration handles field mapping

## Benefits of New Architecture

1. **No CAD Dependency for BOM**: Generate BOMs without opening CAD files
2. **Flexible Configuration**: Add/modify options without changing code
3. **Quantity Consolidation**: Automatic consolidation of duplicate parts
4. **Pricing Integration**: Unit prices and suppliers stored with parts
5. **Layered Architecture**: Clear separation of concerns
6. **Testability**: Use cases can be unit tested
7. **Extensibility**: Easy to add new categories and options

## Rollback Plan

If needed, you can revert to legacy behavior:
1. The legacy UI and services remain functional
2. Legacy Parts table is preserved (with added fields)
3. Old BOM generation methods still work
4. New tables don't interfere with legacy operations

## Support and Questions

- Review the README for detailed architecture documentation
- Check code comments in new classes for usage examples
- Sample data provides working examples of the new model
- Legacy code is maintained for reference

## Timeline

- **Phase 1** (Current): Both systems coexist
- **Phase 2** (Future): Migrate all configurations to new model
- **Phase 3** (Future): Remove legacy code and services

## Best Practices

1. **Use Codes, Not Names**: Always reference by `Code` field, not `Name`
2. **Immutable IDs**: Never change database IDs; change Codes instead
3. **Consistent Naming**: Use prefixes like `COL_`, `MOD_`, `PART_`
4. **Module Reuse**: Design modules to be reusable across configurations
5. **Part Sharing**: Same part can be used in multiple modules
6. **Test with Sample Data**: Use seeded data to verify configurations

## Example Configuration

Here's a complete example configuration:

**Category**: Columns Size
- **Option 1**: 700x1000 → **Module**: Column 700x1000 (qty: 4)
  - **Part**: Steel Profile 700 (qty: 1 per module = 4 total)
  - **Part**: Mounting Bracket (qty: 4 per module = 16 total)
  - **Part**: M8 Bolts (qty: 16 per module = 64 total)

When user selects "700x1000" option:
1. System activates "Column 700x1000" module with quantity 4
2. Each module contains 1 profile, 4 brackets, 16 bolts
3. BOM consolidates: 4 profiles, 16 brackets, 64 bolts

## Conclusion

The new modular architecture provides a flexible, maintainable foundation for product configuration. While the migration requires some initial setup, the benefits in flexibility and maintainability far outweigh the effort.

For questions or issues, consult the main README or create an issue in the repository.
