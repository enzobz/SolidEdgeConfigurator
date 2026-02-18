using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using SolidEdgeConfigurator.Application.DTOs;
using SolidEdgeConfigurator.Domain.Interfaces;

namespace SolidEdgeConfigurator.Application.UseCases
{
    /// <summary>
    /// Use case for generating BOM from selected options
    /// Flow: Selected Options → Modules → Parts → Consolidated Quantities
    /// This is 100% database-based, no CAD extraction required
    /// </summary>
    public class GenerateBOMUseCase
    {
        private readonly IOptionRepository _optionRepository;
        private readonly IModuleRepository _moduleRepository;
        private readonly IPartRepository _partRepository;
        private readonly IOptionModuleRepository _optionModuleRepository;
        private readonly IModulePartRepository _modulePartRepository;

        public GenerateBOMUseCase(
            IOptionRepository optionRepository,
            IModuleRepository moduleRepository,
            IPartRepository partRepository,
            IOptionModuleRepository optionModuleRepository,
            IModulePartRepository modulePartRepository)
        {
            _optionRepository = optionRepository;
            _moduleRepository = moduleRepository;
            _partRepository = partRepository;
            _optionModuleRepository = optionModuleRepository;
            _modulePartRepository = modulePartRepository;
        }

        public BOMResult Execute(List<int> selectedOptionIds, string configurationName)
        {
            Log.Information("Generating BOM for configuration: {ConfigName}", configurationName);
            Log.Information("Selected option IDs: {OptionIds}", string.Join(", ", selectedOptionIds));

            var result = new BOMResult
            {
                ConfigurationName = configurationName,
                GeneratedDate = DateTime.Now
            };

            // Step 1: Get selected options
            var selectedOptions = selectedOptionIds
                .Select(id => _optionRepository.GetById(id))
                .Where(o => o != null)
                .ToList();

            result.SelectedOptions = selectedOptions.Select(o => $"{o.Name} ({o.Code})").ToList();
            Log.Information("Selected options: {Options}", string.Join(", ", result.SelectedOptions));

            // Step 2: Determine activated modules
            var moduleQuantities = new Dictionary<int, int>(); // moduleId -> total quantity

            foreach (var option in selectedOptions)
            {
                var optionModules = _optionModuleRepository.GetByOption(option.Id);
                
                foreach (var om in optionModules)
                {
                    if (!moduleQuantities.ContainsKey(om.ModuleId))
                    {
                        moduleQuantities[om.ModuleId] = 0;
                    }
                    moduleQuantities[om.ModuleId] += om.Quantity;
                }
            }

            Log.Information("Activated modules: {ModuleCount}", moduleQuantities.Count);

            // Store activated module names
            foreach (var moduleId in moduleQuantities.Keys)
            {
                var module = _moduleRepository.GetById(moduleId);
                if (module != null)
                {
                    result.ActivatedModules.Add($"{module.Name} (x{moduleQuantities[moduleId]})");
                }
            }

            // Step 3: Collect parts from all activated modules
            var partQuantities = new Dictionary<int, (int quantity, List<string> modules)>(); // partId -> (quantity, source modules)

            foreach (var kvp in moduleQuantities)
            {
                int moduleId = kvp.Key;
                int moduleQty = kvp.Value;

                var module = _moduleRepository.GetById(moduleId);
                if (module == null) continue;

                var moduleParts = _modulePartRepository.GetByModule(moduleId);

                foreach (var mp in moduleParts)
                {
                    if (!partQuantities.ContainsKey(mp.PartId))
                    {
                        partQuantities[mp.PartId] = (0, new List<string>());
                    }

                    var current = partQuantities[mp.PartId];
                    current.quantity += mp.Quantity * moduleQty;
                    if (!current.modules.Contains(module.Name))
                    {
                        current.modules.Add(module.Name);
                    }
                    partQuantities[mp.PartId] = current;
                }
            }

            Log.Information("Total unique parts: {PartCount}", partQuantities.Count);

            // Step 4: Build BOM line items with consolidated quantities
            foreach (var kvp in partQuantities)
            {
                var part = _partRepository.GetById(kvp.Key);
                if (part == null) continue;

                var lineItem = new BOMLineItem
                {
                    PartId = part.Id,
                    PartCode = part.Code,
                    PartName = part.Name,
                    PartNumber = part.PartNumber,
                    Description = part.Description,
                    TotalQuantity = kvp.Value.quantity,
                    Unit = part.Unit,
                    UnitPrice = part.UnitPrice,
                    Supplier = part.Supplier,
                    SourceModules = kvp.Value.modules
                };

                result.LineItems.Add(lineItem);
            }

            // Sort by part code
            result.LineItems = result.LineItems.OrderBy(li => li.PartCode).ToList();

            Log.Information("BOM generated: {UniquePartCount} unique parts, {TotalItems} total items, ${TotalCost:F2}",
                result.UniquePartCount, result.TotalItems, result.TotalCost);

            return result;
        }
    }
}
