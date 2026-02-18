using System.Collections.Generic;
using SolidEdgeConfigurator.Domain.Entities;

namespace SolidEdgeConfigurator.Domain.Interfaces
{
    public interface IModuleRepository
    {
        Module GetById(int id);
        Module GetByCode(string code);
        List<Module> GetAll();
        List<Module> GetActive();
        List<Module> GetByOptions(List<int> optionIds);
        void Add(Module module);
        void Update(Module module);
        void Delete(int id);
    }
}
