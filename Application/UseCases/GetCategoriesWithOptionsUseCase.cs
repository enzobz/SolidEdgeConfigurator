using System.Collections.Generic;
using System.Linq;
using SolidEdgeConfigurator.Application.DTOs;
using SolidEdgeConfigurator.Domain.Interfaces;

namespace SolidEdgeConfigurator.Application.UseCases
{
    /// <summary>
    /// Use case for retrieving categories with their options for configuration UI
    /// </summary>
    public class GetCategoriesWithOptionsUseCase
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IOptionRepository _optionRepository;

        public GetCategoriesWithOptionsUseCase(
            ICategoryRepository categoryRepository,
            IOptionRepository optionRepository)
        {
            _categoryRepository = categoryRepository;
            _optionRepository = optionRepository;
        }

        public List<CategoryDTO> Execute()
        {
            var categories = _categoryRepository.GetActive()
                .OrderBy(c => c.DisplayOrder)
                .ToList();

            var result = new List<CategoryDTO>();

            foreach (var category in categories)
            {
                var options = _optionRepository.GetActiveByCategory(category.Id)
                    .OrderBy(o => o.DisplayOrder)
                    .Select(o => new OptionDTO
                    {
                        Id = o.Id,
                        Code = o.Code,
                        Name = o.Name,
                        Description = o.Description,
                        IsDefault = o.IsDefault
                    })
                    .ToList();

                result.Add(new CategoryDTO
                {
                    Id = category.Id,
                    Code = category.Code,
                    Name = category.Name,
                    Description = category.Description,
                    DisplayOrder = category.DisplayOrder,
                    Options = options
                });
            }

            return result;
        }
    }
}
