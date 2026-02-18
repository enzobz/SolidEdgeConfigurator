using System.Collections.Generic;
using SolidEdgeConfigurator.Domain.Entities;

namespace SolidEdgeConfigurator.Domain.Interfaces
{
    public interface IOptionRepository
    {
        Option GetById(int id);
        Option GetByCode(string code);
        List<Option> GetAll();
        List<Option> GetByCategory(int categoryId);
        List<Option> GetActiveByCategory(int categoryId);
        void Add(Option option);
        void Update(Option option);
        void Delete(int id);
    }
}
