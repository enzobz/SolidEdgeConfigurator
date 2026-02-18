using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using Serilog;
using SolidEdgeConfigurator.Domain.Entities;
using SolidEdgeConfigurator.Domain.Interfaces;

namespace SolidEdgeConfigurator.Infrastructure.Repositories
{
    public class ModuleRepository : IModuleRepository
    {
        private readonly string _connectionString;

        public ModuleRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Module GetById(int id)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string query = "SELECT * FROM Modules WHERE Id = @Id";
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read()) return MapModule(reader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting module by ID: {Id}", id);
            }
            return null;
        }

        public Module GetByCode(string code)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string query = "SELECT * FROM Modules WHERE Code = @Code";
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Code", code);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read()) return MapModule(reader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting module by code: {Code}", code);
            }
            return null;
        }

        public List<Module> GetAll()
        {
            var modules = new List<Module>();
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string query = "SELECT * FROM Modules ORDER BY Name";
                    using (var command = new SQLiteCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read()) modules.Add(MapModule(reader));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting all modules");
            }
            return modules;
        }

        public List<Module> GetActive()
        {
            var modules = new List<Module>();
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string query = "SELECT * FROM Modules WHERE IsActive = 1 ORDER BY Name";
                    using (var command = new SQLiteCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read()) modules.Add(MapModule(reader));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting active modules");
            }
            return modules;
        }

        public List<Module> GetByOptions(List<int> optionIds)
        {
            var modules = new List<Module>();
            if (optionIds == null || !optionIds.Any()) return modules;

            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var placeholders = string.Join(",", optionIds.Select((_, i) => $"@OptionId{i}"));
                    var query = $@"
                        SELECT DISTINCT m.* 
                        FROM Modules m
                        INNER JOIN OptionModules om ON m.Id = om.ModuleId
                        WHERE om.OptionId IN ({placeholders}) AND m.IsActive = 1
                        ORDER BY m.Name";
                    
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        for (int i = 0; i < optionIds.Count; i++)
                        {
                            command.Parameters.AddWithValue($"@OptionId{i}", optionIds[i]);
                        }
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read()) modules.Add(MapModule(reader));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting modules by options");
            }
            return modules;
        }

        public void Add(Module module)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string query = @"
                        INSERT INTO Modules (Code, Name, Description, MasterAssemblyPath, IsActive)
                        VALUES (@Code, @Name, @Description, @MasterAssemblyPath, @IsActive)";
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Code", module.Code ?? "");
                        command.Parameters.AddWithValue("@Name", module.Name ?? "");
                        command.Parameters.AddWithValue("@Description", module.Description ?? "");
                        command.Parameters.AddWithValue("@MasterAssemblyPath", module.MasterAssemblyPath ?? "");
                        command.Parameters.AddWithValue("@IsActive", module.IsActive ? 1 : 0);
                        command.ExecuteNonQuery();
                    }
                }
                Log.Information("Module added: {Code}", module.Code);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error adding module: {Code}", module.Code);
                throw;
            }
        }

        public void Update(Module module)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string query = @"
                        UPDATE Modules 
                        SET Code = @Code, Name = @Name, Description = @Description,
                            MasterAssemblyPath = @MasterAssemblyPath, IsActive = @IsActive
                        WHERE Id = @Id";
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", module.Id);
                        command.Parameters.AddWithValue("@Code", module.Code ?? "");
                        command.Parameters.AddWithValue("@Name", module.Name ?? "");
                        command.Parameters.AddWithValue("@Description", module.Description ?? "");
                        command.Parameters.AddWithValue("@MasterAssemblyPath", module.MasterAssemblyPath ?? "");
                        command.Parameters.AddWithValue("@IsActive", module.IsActive ? 1 : 0);
                        command.ExecuteNonQuery();
                    }
                }
                Log.Information("Module updated: {Code}", module.Code);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating module: {Code}", module.Code);
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
                    const string query = "DELETE FROM Modules WHERE Id = @Id";
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.ExecuteNonQuery();
                    }
                }
                Log.Information("Module deleted: {Id}", id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting module: {Id}", id);
                throw;
            }
        }

        private Module MapModule(SQLiteDataReader reader)
        {
            return new Module
            {
                Id = Convert.ToInt32(reader["Id"]),
                Code = reader["Code"].ToString(),
                Name = reader["Name"].ToString(),
                Description = reader["Description"].ToString(),
                MasterAssemblyPath = reader["MasterAssemblyPath"].ToString(),
                IsActive = Convert.ToInt32(reader["IsActive"]) == 1
            };
        }
    }
}
