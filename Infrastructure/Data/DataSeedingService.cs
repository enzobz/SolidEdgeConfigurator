using System;
using Serilog;
using SolidEdgeConfigurator.Domain.Entities;
using SolidEdgeConfigurator.Domain.Interfaces;

namespace SolidEdgeConfigurator.Infrastructure.Data
{
    /// <summary>
    /// Service to seed the database with sample data for testing the modular architecture
    /// </summary>
    public class DataSeedingService
    {
        private readonly ICategoryRepository _categoryRepo;
        private readonly IOptionRepository _optionRepo;
        private readonly IModuleRepository _moduleRepo;
        private readonly IPartRepository _partRepo;
        private readonly IOptionModuleRepository _optionModuleRepo;
        private readonly IModulePartRepository _modulePartRepo;

        public DataSeedingService(
            ICategoryRepository categoryRepo,
            IOptionRepository optionRepo,
            IModuleRepository moduleRepo,
            IPartRepository partRepo,
            IOptionModuleRepository optionModuleRepo,
            IModulePartRepository modulePartRepo)
        {
            _categoryRepo = categoryRepo;
            _optionRepo = optionRepo;
            _moduleRepo = moduleRepo;
            _partRepo = partRepo;
            _optionModuleRepo = optionModuleRepo;
            _modulePartRepo = modulePartRepo;
        }

        /// <summary>
        /// Seed the database with sample configuration data
        /// </summary>
        public void SeedSampleData()
        {
            try
            {
                Log.Information("Starting data seeding...");

                // Check if data already exists
                var existingCategories = _categoryRepo.GetAll();
                if (existingCategories.Count > 0)
                {
                    Log.Information("Data already exists, skipping seeding");
                    return;
                }

                // 1. Create Categories
                var columnsCat = new Category { Code = "COLUMNS", Name = "Columns Size", Description = "Column dimensions", DisplayOrder = 1 };
                var ipCat = new Category { Code = "IP", Name = "IP Rating", Description = "Ingress Protection rating", DisplayOrder = 2 };
                var roofCat = new Category { Code = "ROOF", Name = "Ventilated Roof", Description = "Roof ventilation options", DisplayOrder = 3 };
                var hbbCat = new Category { Code = "HBB", Name = "Horizontal Busbar", Description = "Horizontal busbar configuration", DisplayOrder = 4 };

                _categoryRepo.Add(columnsCat);
                _categoryRepo.Add(ipCat);
                _categoryRepo.Add(roofCat);
                _categoryRepo.Add(hbbCat);

                // Retrieve IDs after insertion
                columnsCat = _categoryRepo.GetByCode("COLUMNS");
                ipCat = _categoryRepo.GetByCode("IP");
                roofCat = _categoryRepo.GetByCode("ROOF");
                hbbCat = _categoryRepo.GetByCode("HBB");

                // 2. Create Options
                // Columns options
                var col700 = new Option { Code = "COL_700x1000", Name = "700x1000", Description = "Column size 700x1000mm", CategoryId = columnsCat.Id, DisplayOrder = 1, IsDefault = true };
                var col800 = new Option { Code = "COL_800x1200", Name = "800x1200", Description = "Column size 800x1200mm", CategoryId = columnsCat.Id, DisplayOrder = 2 };
                
                // IP Rating options
                var ip54 = new Option { Code = "IP54", Name = "IP54", Description = "IP54 protection", CategoryId = ipCat.Id, DisplayOrder = 1, IsDefault = true };
                var ip42 = new Option { Code = "IP42", Name = "IP42", Description = "IP42 protection", CategoryId = ipCat.Id, DisplayOrder = 2 };
                
                // Roof options
                var roofYes = new Option { Code = "ROOF_YES", Name = "Yes", Description = "With ventilated roof", CategoryId = roofCat.Id, DisplayOrder = 1, IsDefault = true };
                var roofNo = new Option { Code = "ROOF_NO", Name = "No", Description = "Without ventilated roof", CategoryId = roofCat.Id, DisplayOrder = 2 };
                
                // HBB options
                var hbb1600 = new Option { Code = "HBB_1600", Name = "1600A", Description = "1600A horizontal busbar", CategoryId = hbbCat.Id, DisplayOrder = 1, IsDefault = true };
                var hbb2500 = new Option { Code = "HBB_2500", Name = "2500A", Description = "2500A horizontal busbar", CategoryId = hbbCat.Id, DisplayOrder = 2 };

                _optionRepo.Add(col700);
                _optionRepo.Add(col800);
                _optionRepo.Add(ip54);
                _optionRepo.Add(ip42);
                _optionRepo.Add(roofYes);
                _optionRepo.Add(roofNo);
                _optionRepo.Add(hbb1600);
                _optionRepo.Add(hbb2500);

                // Retrieve option IDs
                col700 = _optionRepo.GetByCode("COL_700x1000");
                col800 = _optionRepo.GetByCode("COL_800x1200");
                ip54 = _optionRepo.GetByCode("IP54");
                ip42 = _optionRepo.GetByCode("IP42");
                roofYes = _optionRepo.GetByCode("ROOF_YES");
                roofNo = _optionRepo.GetByCode("ROOF_NO");
                hbb1600 = _optionRepo.GetByCode("HBB_1600");
                hbb2500 = _optionRepo.GetByCode("HBB_2500");

                // 3. Create Modules
                var modColumn700 = new Module { Code = "MOD_COL_700", Name = "Column 700x1000 Module", Description = "Standard column module", MasterAssemblyPath = @"C:\Assemblies\Column_700x1000.asm" };
                var modColumn800 = new Module { Code = "MOD_COL_800", Name = "Column 800x1200 Module", Description = "Large column module", MasterAssemblyPath = @"C:\Assemblies\Column_800x1200.asm" };
                var modRoof = new Module { Code = "MOD_ROOF", Name = "Ventilated Roof Module", Description = "Roof with ventilation", MasterAssemblyPath = @"C:\Assemblies\Roof_Ventilated.asm" };
                var modBusbar1600 = new Module { Code = "MOD_HBB_1600", Name = "Busbar 1600A Module", Description = "1600A busbar assembly", MasterAssemblyPath = @"C:\Assemblies\Busbar_1600A.asm" };
                var modBusbar2500 = new Module { Code = "MOD_HBB_2500", Name = "Busbar 2500A Module", Description = "2500A busbar assembly", MasterAssemblyPath = @"C:\Assemblies\Busbar_2500A.asm" };

                _moduleRepo.Add(modColumn700);
                _moduleRepo.Add(modColumn800);
                _moduleRepo.Add(modRoof);
                _moduleRepo.Add(modBusbar1600);
                _moduleRepo.Add(modBusbar2500);

                // Retrieve module IDs
                modColumn700 = _moduleRepo.GetByCode("MOD_COL_700");
                modColumn800 = _moduleRepo.GetByCode("MOD_COL_800");
                modRoof = _moduleRepo.GetByCode("MOD_ROOF");
                modBusbar1600 = _moduleRepo.GetByCode("MOD_HBB_1600");
                modBusbar2500 = _moduleRepo.GetByCode("MOD_HBB_2500");

                // 4. Create Parts
                var part1 = new Part { Code = "PART_001", Name = "Steel Column Profile", PartNumber = "SC-700-001", Description = "Steel profile for 700x1000 column", UnitPrice = 150.00, Supplier = "SteelCorp", Unit = "pcs" };
                var part2 = new Part { Code = "PART_002", Name = "Steel Column Profile Large", PartNumber = "SC-800-001", Description = "Steel profile for 800x1200 column", UnitPrice = 200.00, Supplier = "SteelCorp", Unit = "pcs" };
                var part3 = new Part { Code = "PART_003", Name = "Mounting Bracket", PartNumber = "MB-001", Description = "Universal mounting bracket", UnitPrice = 25.00, Supplier = "FastenerInc", Unit = "pcs" };
                var part4 = new Part { Code = "PART_004", Name = "Roof Panel", PartNumber = "RP-001", Description = "Ventilated roof panel", UnitPrice = 80.00, Supplier = "RoofMaster", Unit = "pcs" };
                var part5 = new Part { Code = "PART_005", Name = "Ventilation Grill", PartNumber = "VG-001", Description = "Air ventilation grill", UnitPrice = 30.00, Supplier = "VentCo", Unit = "pcs" };
                var part6 = new Part { Code = "PART_006", Name = "Copper Busbar 1600A", PartNumber = "BB-1600-CU", Description = "Copper busbar 1600A", UnitPrice = 450.00, Supplier = "ElectricSupply", Unit = "pcs" };
                var part7 = new Part { Code = "PART_007", Name = "Copper Busbar 2500A", PartNumber = "BB-2500-CU", Description = "Copper busbar 2500A", UnitPrice = 650.00, Supplier = "ElectricSupply", Unit = "pcs" };
                var part8 = new Part { Code = "PART_008", Name = "Busbar Insulator", PartNumber = "BI-001", Description = "Busbar insulator", UnitPrice = 15.00, Supplier = "ElectricSupply", Unit = "pcs" };
                var part9 = new Part { Code = "PART_009", Name = "Bolt M8x40", PartNumber = "BOLT-M8-40", Description = "M8x40 bolt", UnitPrice = 0.50, Supplier = "FastenerInc", Unit = "pcs" };

                _partRepo.Add(part1);
                _partRepo.Add(part2);
                _partRepo.Add(part3);
                _partRepo.Add(part4);
                _partRepo.Add(part5);
                _partRepo.Add(part6);
                _partRepo.Add(part7);
                _partRepo.Add(part8);
                _partRepo.Add(part9);

                // Retrieve part IDs
                part1 = _partRepo.GetByCode("PART_001");
                part2 = _partRepo.GetByCode("PART_002");
                part3 = _partRepo.GetByCode("PART_003");
                part4 = _partRepo.GetByCode("PART_004");
                part5 = _partRepo.GetByCode("PART_005");
                part6 = _partRepo.GetByCode("PART_006");
                part7 = _partRepo.GetByCode("PART_007");
                part8 = _partRepo.GetByCode("PART_008");
                part9 = _partRepo.GetByCode("PART_009");

                // 5. Link Options to Modules (OptionModules)
                _optionModuleRepo.Add(new OptionModule { OptionId = col700.Id, ModuleId = modColumn700.Id, Quantity = 4 }); // 4 columns
                _optionModuleRepo.Add(new OptionModule { OptionId = col800.Id, ModuleId = modColumn800.Id, Quantity = 4 });
                _optionModuleRepo.Add(new OptionModule { OptionId = roofYes.Id, ModuleId = modRoof.Id, Quantity = 1 });
                _optionModuleRepo.Add(new OptionModule { OptionId = hbb1600.Id, ModuleId = modBusbar1600.Id, Quantity = 1 });
                _optionModuleRepo.Add(new OptionModule { OptionId = hbb2500.Id, ModuleId = modBusbar2500.Id, Quantity = 1 });

                // 6. Link Modules to Parts (ModuleParts)
                // Column 700 module parts
                _modulePartRepo.Add(new ModulePart { ModuleId = modColumn700.Id, PartId = part1.Id, Quantity = 1 });
                _modulePartRepo.Add(new ModulePart { ModuleId = modColumn700.Id, PartId = part3.Id, Quantity = 4 });
                _modulePartRepo.Add(new ModulePart { ModuleId = modColumn700.Id, PartId = part9.Id, Quantity = 16 });

                // Column 800 module parts
                _modulePartRepo.Add(new ModulePart { ModuleId = modColumn800.Id, PartId = part2.Id, Quantity = 1 });
                _modulePartRepo.Add(new ModulePart { ModuleId = modColumn800.Id, PartId = part3.Id, Quantity = 4 });
                _modulePartRepo.Add(new ModulePart { ModuleId = modColumn800.Id, PartId = part9.Id, Quantity = 16 });

                // Roof module parts
                _modulePartRepo.Add(new ModulePart { ModuleId = modRoof.Id, PartId = part4.Id, Quantity = 2 });
                _modulePartRepo.Add(new ModulePart { ModuleId = modRoof.Id, PartId = part5.Id, Quantity = 4 });
                _modulePartRepo.Add(new ModulePart { ModuleId = modRoof.Id, PartId = part9.Id, Quantity = 8 });

                // Busbar 1600 module parts
                _modulePartRepo.Add(new ModulePart { ModuleId = modBusbar1600.Id, PartId = part6.Id, Quantity = 3 });
                _modulePartRepo.Add(new ModulePart { ModuleId = modBusbar1600.Id, PartId = part8.Id, Quantity = 6 });
                _modulePartRepo.Add(new ModulePart { ModuleId = modBusbar1600.Id, PartId = part9.Id, Quantity = 12 });

                // Busbar 2500 module parts
                _modulePartRepo.Add(new ModulePart { ModuleId = modBusbar2500.Id, PartId = part7.Id, Quantity = 3 });
                _modulePartRepo.Add(new ModulePart { ModuleId = modBusbar2500.Id, PartId = part8.Id, Quantity = 6 });
                _modulePartRepo.Add(new ModulePart { ModuleId = modBusbar2500.Id, PartId = part9.Id, Quantity = 12 });

                Log.Information("✓ Sample data seeded successfully!");
                Log.Information("  - Categories: 4");
                Log.Information("  - Options: 8");
                Log.Information("  - Modules: 5");
                Log.Information("  - Parts: 9");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error seeding data: {Message}", ex.Message);
                throw;
            }
        }
    }
}
