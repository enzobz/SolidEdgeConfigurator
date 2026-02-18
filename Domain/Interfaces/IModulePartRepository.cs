using System.Collections.Generic;
using SolidEdgeConfigurator.Domain.Entities;

namespace SolidEdgeConfigurator.Domain.Interfaces
{
    public interface IModulePartRepository
    {
        List<ModulePart> GetByModule(int moduleId);
        List<ModulePart> GetByPart(int partId);
        void Add(ModulePart modulePart);
        void Delete(int id);
    }
}
