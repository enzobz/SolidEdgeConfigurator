# Usage Examples

This document provides practical examples of how to use the new modular architecture.

## Example 1: Using the UI to Generate a BOM

### Step 1: Launch Application
```
Run SolidEdgeConfigurator.exe
```

### Step 2: Seed Sample Data (First Time Only)
```
1. Click "🌱 Seed Sample Data" button in main window
2. Confirm the dialog
3. Wait for success message
```

### Step 3: Open Modular Configuration
```
1. Click "⚙️ Modular Config" button
2. ModularConfigurationWindow opens
```

### Step 4: Select Options
```
In the left panel, select one option per category:
- Columns Size: Choose "700x1000" or "800x1200"
- IP Rating: Choose "IP54" or "IP42"
- Ventilated Roof: Choose "Yes" or "No"
- Horizontal Busbar: Choose "1600A" or "2500A"
```

### Step 5: Generate BOM
```
1. Click "Generate BOM" button
2. View results in right panel:
   - Activated modules shown at top
   - Parts list in data grid
   - Total cost shown in status bar
```

### Step 6: Export BOM
```
1. Click "Export BOM" button
2. Choose format (CSV or HTML)
3. Select save location
4. View exported file
```

## Example 2: Programmatic BOM Generation

```csharp
// Initialize database service
var dbService = new DatabaseService();

// Initialize repositories
var optionRepo = new OptionRepository(dbService.ConnectionString);
var moduleRepo = new ModuleRepository(dbService.ConnectionString);
var partRepo = new PartRepository(dbService.ConnectionString);
var optionModuleRepo = new OptionModuleRepository(dbService.ConnectionString);
var modulePartRepo = new ModulePartRepository(dbService.ConnectionString);

// Create BOM service
var bomService = new BOMService(
    optionRepo, 
    moduleRepo, 
    partRepo, 
    optionModuleRepo, 
    modulePartRepo);

// Get option IDs (example: find by code)
var col700Option = optionRepo.GetByCode("COL_700x1000");
var ip54Option = optionRepo.GetByCode("IP54");
var roofYesOption = optionRepo.GetByCode("ROOF_YES");
var hbb1600Option = optionRepo.GetByCode("HBB_1600");

var selectedOptionIds = new List<int>
{
    col700Option.Id,
    ip54Option.Id,
    roofYesOption.Id,
    hbb1600Option.Id
};

// Generate BOM
var bom = bomService.GenerateBOM(selectedOptionIds, "MyConfiguration");

// Display results
Console.WriteLine($"Configuration: {bom.ConfigurationName}");
Console.WriteLine($"Generated: {bom.GeneratedDate}");
Console.WriteLine($"Total Parts: {bom.UniquePartCount}");
Console.WriteLine($"Total Items: {bom.TotalItems}");
Console.WriteLine($"Total Cost: ${bom.TotalCost:F2}");

Console.WriteLine("\nActivated Modules:");
foreach (var module in bom.ActivatedModules)
{
    Console.WriteLine($"  - {module}");
}

Console.WriteLine("\nBOM Line Items:");
foreach (var item in bom.LineItems)
{
    Console.WriteLine($"  {item.PartCode}: {item.PartName} x{item.TotalQuantity} = ${item.TotalPrice:F2}");
}

// Export to file
bomService.ExportToCSV(bom, @"C:\Exports\BOM.csv");
bomService.ExportToHTML(bom, @"C:\Exports\BOM.html");
```

## Example 3: Adding a New Category with Options

```csharp
var categoryRepo = new CategoryRepository(dbService.ConnectionString);
var optionRepo = new OptionRepository(dbService.ConnectionString);

// Create new category
var doorCategory = new Category
{
    Code = "DOOR",
    Name = "Door Type",
    Description = "Front door configuration",
    DisplayOrder = 5,
    IsActive = true
};
categoryRepo.Add(doorCategory);

// Retrieve the category to get its ID
doorCategory = categoryRepo.GetByCode("DOOR");

// Add options for the category
var singleDoor = new Option
{
    Code = "DOOR_SINGLE",
    Name = "Single Door",
    Description = "Standard single door",
    CategoryId = doorCategory.Id,
    DisplayOrder = 1,
    IsActive = true,
    IsDefault = true
};
optionRepo.Add(singleDoor);

var doubleDoor = new Option
{
    Code = "DOOR_DOUBLE",
    Name = "Double Door",
    Description = "Wide double door",
    CategoryId = doorCategory.Id,
    DisplayOrder = 2,
    IsActive = true,
    IsDefault = false
};
optionRepo.Add(doubleDoor);

Console.WriteLine("✓ New category and options added");
```

## Example 4: Creating a Module with Parts

```csharp
var moduleRepo = new ModuleRepository(dbService.ConnectionString);
var partRepo = new PartRepository(dbService.ConnectionString);
var optionModuleRepo = new OptionModuleRepository(dbService.ConnectionString);
var modulePartRepo = new ModulePartRepository(dbService.ConnectionString);

// Create module
var doorModule = new Module
{
    Code = "MOD_DOOR_SINGLE",
    Name = "Single Door Module",
    Description = "Complete single door assembly",
    MasterAssemblyPath = @"C:\Assemblies\Door_Single.asm",
    IsActive = true
};
moduleRepo.Add(doorModule);
doorModule = moduleRepo.GetByCode("MOD_DOOR_SINGLE");

// Create parts
var doorPanel = new Part
{
    Code = "PART_DOOR_PANEL",
    Name = "Door Panel",
    PartNumber = "DP-001",
    Description = "Steel door panel",
    UnitPrice = 120.00,
    Supplier = "DoorCorp",
    Unit = "pcs",
    IsActive = true
};
partRepo.Add(doorPanel);
doorPanel = partRepo.GetByCode("PART_DOOR_PANEL");

var doorHinge = new Part
{
    Code = "PART_DOOR_HINGE",
    Name = "Door Hinge",
    PartNumber = "DH-001",
    Description = "Heavy duty hinge",
    UnitPrice = 15.00,
    Supplier = "Hardware Inc",
    Unit = "pcs",
    IsActive = true
};
partRepo.Add(doorHinge);
doorHinge = partRepo.GetByCode("PART_DOOR_HINGE");

// Link option to module
var singleDoorOption = optionRepo.GetByCode("DOOR_SINGLE");
optionModuleRepo.Add(new OptionModule
{
    OptionId = singleDoorOption.Id,
    ModuleId = doorModule.Id,
    Quantity = 1
});

// Link module to parts
modulePartRepo.Add(new ModulePart
{
    ModuleId = doorModule.Id,
    PartId = doorPanel.Id,
    Quantity = 1
});

modulePartRepo.Add(new ModulePart
{
    ModuleId = doorModule.Id,
    PartId = doorHinge.Id,
    Quantity = 3  // 3 hinges per door
});

Console.WriteLine("✓ Module created and linked to parts");
```

## Example 5: Querying the Configuration Data

```csharp
// Get all categories with options
var getCategoriesUseCase = new GetCategoriesWithOptionsUseCase(
    categoryRepo, 
    optionRepo);

var categories = getCategoriesUseCase.Execute();

foreach (var category in categories)
{
    Console.WriteLine($"\nCategory: {category.Name} ({category.Code})");
    Console.WriteLine($"  Description: {category.Description}");
    Console.WriteLine($"  Options:");
    
    foreach (var option in category.Options)
    {
        var defaultMarker = option.IsDefault ? " [DEFAULT]" : "";
        Console.WriteLine($"    - {option.Name}{defaultMarker}: {option.Description}");
    }
}

// Output example:
// Category: Columns Size (COLUMNS)
//   Description: Column dimensions
//   Options:
//     - 700x1000 [DEFAULT]: Column size 700x1000mm
//     - 800x1200: Column size 800x1200mm
```

## Example 6: Finding Parts by Module

```csharp
// Get a specific module
var module = moduleRepo.GetByCode("MOD_COL_700");

// Get all parts for this module
var parts = partRepo.GetByModule(module.Id);

Console.WriteLine($"Parts in {module.Name}:");
foreach (var part in parts)
{
    Console.WriteLine($"  - {part.Code}: {part.Name} (${part.UnitPrice})");
}

// Get quantities for each part in module
var moduleParts = modulePartRepo.GetByModule(module.Id);

Console.WriteLine($"\nPart quantities in {module.Name}:");
foreach (var mp in moduleParts)
{
    var part = partRepo.GetById(mp.PartId);
    Console.WriteLine($"  - {part.Name}: {mp.Quantity} {part.Unit}");
}
```

## Example 7: Exporting BOM to Different Formats

```csharp
// Generate BOM
var bom = bomService.GenerateBOM(selectedOptionIds, "Export Example");

// Export to CSV
bomService.ExportToCSV(bom, @"C:\Exports\BOM.csv");
// Creates a CSV file with headers and all BOM data

// Export to HTML
bomService.ExportToHTML(bom, @"C:\Exports\BOM.html");
// Creates a styled HTML file that can be opened in browser

// Manual export to custom format
var sb = new StringBuilder();
sb.AppendLine("Custom BOM Format");
sb.AppendLine($"Config: {bom.ConfigurationName}");
sb.AppendLine($"Date: {bom.GeneratedDate:yyyy-MM-dd}");
sb.AppendLine();

foreach (var item in bom.LineItems)
{
    sb.AppendLine($"{item.PartCode}|{item.PartName}|{item.TotalQuantity}|${item.TotalPrice}");
}

File.WriteAllText(@"C:\Exports\BOM_Custom.txt", sb.ToString());
```

## Example 8: Data Validation

```csharp
// Validate that all options belong to active categories
var allOptions = optionRepo.GetAll();
foreach (var option in allOptions)
{
    var category = categoryRepo.GetById(option.CategoryId);
    if (category == null || !category.IsActive)
    {
        Console.WriteLine($"⚠ Warning: Option {option.Code} belongs to inactive category");
    }
}

// Validate that all module parts reference active parts
var allModuleParts = modulePartRepo.GetByModule(module.Id);
foreach (var mp in allModuleParts)
{
    var part = partRepo.GetById(mp.PartId);
    if (part == null || !part.IsActive)
    {
        Console.WriteLine($"⚠ Warning: Module {module.Code} references inactive part");
    }
}

// Validate that all parts have valid prices
var allParts = partRepo.GetAll();
foreach (var part in allParts)
{
    if (part.UnitPrice <= 0)
    {
        Console.WriteLine($"⚠ Warning: Part {part.Code} has invalid price: ${part.UnitPrice}");
    }
}
```

## Example 9: Bulk Data Import

```csharp
// Import parts from CSV
var csvLines = File.ReadAllLines(@"C:\Import\Parts.csv");
var header = csvLines[0]; // Skip header

for (int i = 1; i < csvLines.Length; i++)
{
    var fields = csvLines[i].Split(',');
    
    var part = new Part
    {
        Code = fields[0].Trim(),
        Name = fields[1].Trim(),
        PartNumber = fields[2].Trim(),
        Description = fields[3].Trim(),
        UnitPrice = double.Parse(fields[4]),
        Supplier = fields[5].Trim(),
        Unit = fields[6].Trim(),
        IsActive = true
    };
    
    try
    {
        partRepo.Add(part);
        Console.WriteLine($"✓ Imported: {part.Code}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Failed: {part.Code} - {ex.Message}");
    }
}
```

## Example 10: Testing Configuration Scenarios

```csharp
// Test scenario: Small configuration
var smallConfig = new List<int>
{
    optionRepo.GetByCode("COL_700x1000").Id,
    optionRepo.GetByCode("IP42").Id,
    optionRepo.GetByCode("ROOF_NO").Id,
    optionRepo.GetByCode("HBB_1600").Id
};

var smallBOM = bomService.GenerateBOM(smallConfig, "Small Config");
Console.WriteLine($"Small Config Cost: ${smallBOM.TotalCost:F2}");

// Test scenario: Large configuration
var largeConfig = new List<int>
{
    optionRepo.GetByCode("COL_800x1200").Id,
    optionRepo.GetByCode("IP54").Id,
    optionRepo.GetByCode("ROOF_YES").Id,
    optionRepo.GetByCode("HBB_2500").Id
};

var largeBOM = bomService.GenerateBOM(largeConfig, "Large Config");
Console.WriteLine($"Large Config Cost: ${largeBOM.TotalCost:F2}");

// Compare
var difference = largeBOM.TotalCost - smallBOM.TotalCost;
Console.WriteLine($"Price difference: ${difference:F2}");
```

## Tips and Best Practices

1. **Always use Codes**: Reference entities by their `Code` property, not by `Id`
2. **Check for null**: Always check if `GetByCode()` returns null before using
3. **Transaction handling**: For complex operations, consider wrapping in database transactions
4. **Error handling**: Always wrap database operations in try-catch blocks
5. **Logging**: Use Serilog for structured logging throughout the application
6. **Validation**: Validate data before inserting into database
7. **Testing**: Use sample data to test configurations before production use

## Common Pitfalls to Avoid

1. ❌ Don't hardcode option IDs - use codes instead
2. ❌ Don't modify IDs - they're immutable
3. ❌ Don't forget to link options to modules via OptionModules
4. ❌ Don't forget to link modules to parts via ModuleParts
5. ❌ Don't create orphaned data (parts without modules, etc.)
6. ❌ Don't assume default options exist - always check IsDefault
7. ❌ Don't skip data validation before BOM generation

## Additional Resources

- See `MIGRATION_GUIDE.md` for legacy system migration
- See `README.md` for architecture overview
- See `IMPLEMENTATION_SUMMARY.md` for technical details
- Check inline code comments for specific method usage
