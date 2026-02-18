using System.Collections.Generic;
using SolidEdgeConfigurator.Domain.Entities;

namespace SolidEdgeConfigurator.Domain.Interfaces
{
    public interface ICategoryRepository
    {
        Category GetById(int id);
        Category GetByCode(string code);
        List<Category> GetAll();
        List<Category> GetActive();
        void Add(Category category);
        void Update(Category category);
        void Delete(int id);
    }
}
