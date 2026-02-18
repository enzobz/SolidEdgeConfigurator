using System.Collections.Generic;
using SolidEdgeConfigurator.Domain.Entities;

namespace SolidEdgeConfigurator.Domain.Interfaces
{
    public interface IOptionModuleRepository
    {
        List<OptionModule> GetByOption(int optionId);
        List<OptionModule> GetByModule(int moduleId);
        void Add(OptionModule optionModule);
        void Delete(int id);
    }
}
