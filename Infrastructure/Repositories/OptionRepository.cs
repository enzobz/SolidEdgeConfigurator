using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Serilog;
using SolidEdgeConfigurator.Domain.Entities;
using SolidEdgeConfigurator.Domain.Interfaces;

namespace SolidEdgeConfigurator.Infrastructure.Repositories
{
    public class OptionRepository : IOptionRepository
    {
        private readonly string _connectionString;

        public OptionRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Option GetById(int id)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string query = "SELECT * FROM Options WHERE Id = @Id";
                    
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return MapOption(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting option by ID: {Id}", id);
            }
            return null;
        }

        public Option GetByCode(string code)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string query = "SELECT * FROM Options WHERE Code = @Code";
                    
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Code", code);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return MapOption(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting option by code: {Code}", code);
            }
            return null;
        }

        public List<Option> GetAll()
        {
            var options = new List<Option>();
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string query = "SELECT * FROM Options ORDER BY CategoryId, DisplayOrder";
                    
                    using (var command = new SQLiteCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            options.Add(MapOption(reader));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting all options");
            }
            return options;
        }

        public List<Option> GetByCategory(int categoryId)
        {
            var options = new List<Option>();
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string query = "SELECT * FROM Options WHERE CategoryId = @CategoryId ORDER BY DisplayOrder";
                    
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CategoryId", categoryId);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                options.Add(MapOption(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting options by category: {CategoryId}", categoryId);
            }
            return options;
        }

        public List<Option> GetActiveByCategory(int categoryId)
        {
            var options = new List<Option>();
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string query = "SELECT * FROM Options WHERE CategoryId = @CategoryId AND IsActive = 1 ORDER BY DisplayOrder";
                    
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CategoryId", categoryId);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                options.Add(MapOption(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting active options by category: {CategoryId}", categoryId);
            }
            return options;
        }

        public void Add(Option option)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string query = @"
                        INSERT INTO Options (Code, Name, Description, CategoryId, DisplayOrder, IsActive, IsDefault)
                        VALUES (@Code, @Name, @Description, @CategoryId, @DisplayOrder, @IsActive, @IsDefault)";
                    
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Code", option.Code ?? "");
                        command.Parameters.AddWithValue("@Name", option.Name ?? "");
                        command.Parameters.AddWithValue("@Description", option.Description ?? "");
                        command.Parameters.AddWithValue("@CategoryId", option.CategoryId);
                        command.Parameters.AddWithValue("@DisplayOrder", option.DisplayOrder);
                        command.Parameters.AddWithValue("@IsActive", option.IsActive ? 1 : 0);
                        command.Parameters.AddWithValue("@IsDefault", option.IsDefault ? 1 : 0);
                        command.ExecuteNonQuery();
                    }
                }
                Log.Information("Option added: {Code}", option.Code);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error adding option: {Code}", option.Code);
                throw;
            }
        }

        public void Update(Option option)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string query = @"
                        UPDATE Options 
                        SET Code = @Code, Name = @Name, Description = @Description,
                            CategoryId = @CategoryId, DisplayOrder = @DisplayOrder, 
                            IsActive = @IsActive, IsDefault = @IsDefault
                        WHERE Id = @Id";
                    
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", option.Id);
                        command.Parameters.AddWithValue("@Code", option.Code ?? "");
                        command.Parameters.AddWithValue("@Name", option.Name ?? "");
                        command.Parameters.AddWithValue("@Description", option.Description ?? "");
                        command.Parameters.AddWithValue("@CategoryId", option.CategoryId);
                        command.Parameters.AddWithValue("@DisplayOrder", option.DisplayOrder);
                        command.Parameters.AddWithValue("@IsActive", option.IsActive ? 1 : 0);
                        command.Parameters.AddWithValue("@IsDefault", option.IsDefault ? 1 : 0);
                        command.ExecuteNonQuery();
                    }
                }
                Log.Information("Option updated: {Code}", option.Code);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating option: {Code}", option.Code);
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
                    const string query = "DELETE FROM Options WHERE Id = @Id";
                    
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.ExecuteNonQuery();
                    }
                }
                Log.Information("Option deleted: {Id}", id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting option: {Id}", id);
                throw;
            }
        }

        private Option MapOption(SQLiteDataReader reader)
        {
            return new Option
            {
                Id = Convert.ToInt32(reader["Id"]),
                Code = reader["Code"].ToString(),
                Name = reader["Name"].ToString(),
                Description = reader["Description"].ToString(),
                CategoryId = Convert.ToInt32(reader["CategoryId"]),
                DisplayOrder = Convert.ToInt32(reader["DisplayOrder"]),
                IsActive = Convert.ToInt32(reader["IsActive"]) == 1,
                IsDefault = Convert.ToInt32(reader["IsDefault"]) == 1
            };
        }
    }
}
