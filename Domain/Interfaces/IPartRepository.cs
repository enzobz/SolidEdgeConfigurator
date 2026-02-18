using System.Collections.Generic;
using SolidEdgeConfigurator.Domain.Entities;

namespace SolidEdgeConfigurator.Domain.Interfaces
{
    public interface IPartRepository
    {
        Part GetById(int id);
        Part GetByCode(string code);
        List<Part> GetAll();
        List<Part> GetActive();
        List<Part> GetByModule(int moduleId);
        void Add(Part part);
        void Update(Part part);
        void Delete(int id);
    }
}
