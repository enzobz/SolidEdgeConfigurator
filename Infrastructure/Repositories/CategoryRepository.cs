using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Serilog;
using SolidEdgeConfigurator.Domain.Entities;
using SolidEdgeConfigurator.Domain.Interfaces;

namespace SolidEdgeConfigurator.Infrastructure.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly string _connectionString;

        public CategoryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Category GetById(int id)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string query = "SELECT * FROM Categories WHERE Id = @Id";
                    
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return MapCategory(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting category by ID: {Id}", id);
            }
            return null;
        }

        public Category GetByCode(string code)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string query = "SELECT * FROM Categories WHERE Code = @Code";
                    
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Code", code);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return MapCategory(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting category by code: {Code}", code);
            }
            return null;
        }

        public List<Category> GetAll()
        {
            var categories = new List<Category>();
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string query = "SELECT * FROM Categories ORDER BY DisplayOrder";
                    
                    using (var command = new SQLiteCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            categories.Add(MapCategory(reader));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting all categories");
            }
            return categories;
        }

        public List<Category> GetActive()
        {
            var categories = new List<Category>();
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string query = "SELECT * FROM Categories WHERE IsActive = 1 ORDER BY DisplayOrder";
                    
                    using (var command = new SQLiteCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            categories.Add(MapCategory(reader));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting active categories");
            }
            return categories;
        }

        public void Add(Category category)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string query = @"
                        INSERT INTO Categories (Code, Name, Description, DisplayOrder, IsActive)
                        VALUES (@Code, @Name, @Description, @DisplayOrder, @IsActive)";
                    
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Code", category.Code ?? "");
                        command.Parameters.AddWithValue("@Name", category.Name ?? "");
                        command.Parameters.AddWithValue("@Description", category.Description ?? "");
                        command.Parameters.AddWithValue("@DisplayOrder", category.DisplayOrder);
                        command.Parameters.AddWithValue("@IsActive", category.IsActive ? 1 : 0);
                        command.ExecuteNonQuery();
                    }
                }
                Log.Information("Category added: {Code}", category.Code);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error adding category: {Code}", category.Code);
                throw;
            }
        }

        public void Update(Category category)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string query = @"
                        UPDATE Categories 
                        SET Code = @Code, Name = @Name, Description = @Description,
                            DisplayOrder = @DisplayOrder, IsActive = @IsActive
                        WHERE Id = @Id";
                    
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", category.Id);
                        command.Parameters.AddWithValue("@Code", category.Code ?? "");
                        command.Parameters.AddWithValue("@Name", category.Name ?? "");
                        command.Parameters.AddWithValue("@Description", category.Description ?? "");
                        command.Parameters.AddWithValue("@DisplayOrder", category.DisplayOrder);
                        command.Parameters.AddWithValue("@IsActive", category.IsActive ? 1 : 0);
                        command.ExecuteNonQuery();
                    }
                }
                Log.Information("Category updated: {Code}", category.Code);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating category: {Code}", category.Code);
                throw;
            }
        }

        public void Delete(int id)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string query = "DELETE FROM Categories WHERE Id = @Id";
                    
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.ExecuteNonQuery();
                    }
                }
                Log.Information("Category deleted: {Id}", id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting category: {Id}", id);
                throw;
            }
        }

        private Category MapCategory(SQLiteDataReader reader)
        {
            return new Category
            {
                Id = Convert.ToInt32(reader["Id"]),
                Code = reader["Code"].ToString(),
                Name = reader["Name"].ToString(),
                Description = reader["Description"].ToString(),
                DisplayOrder = Convert.ToInt32(reader["DisplayOrder"]),
                IsActive = Convert.ToInt32(reader["IsActive"]) == 1
            };
        }
    }
}
