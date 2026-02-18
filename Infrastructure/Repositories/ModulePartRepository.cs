using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Serilog;
using SolidEdgeConfigurator.Domain.Entities;
using SolidEdgeConfigurator.Domain.Interfaces;

namespace SolidEdgeConfigurator.Infrastructure.Repositories
{
    public class ModulePartRepository : IModulePartRepository
    {
        private readonly string _connectionString;

        public ModulePartRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<ModulePart> GetByModule(int moduleId)
        {
            var moduleParts = new List<ModulePart>();
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand("SELECT * FROM ModuleParts WHERE ModuleId = @ModuleId", connection))
                    {
                        command.Parameters.AddWithValue("@ModuleId", moduleId);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read()) moduleParts.Add(MapModulePart(reader));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting module parts by module: {ModuleId}", moduleId);
            }
            return moduleParts;
        }

        public List<ModulePart> GetByPart(int partId)
        {
            var moduleParts = new List<ModulePart>();
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand("SELECT * FROM ModuleParts WHERE PartId = @PartId", connection))
                    {
                        command.Parameters.AddWithValue("@PartId", partId);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read()) moduleParts.Add(MapModulePart(reader));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting module parts by part: {PartId}", partId);
            }
            return moduleParts;
        }

        public void Add(ModulePart modulePart)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string query = @"
                        INSERT INTO ModuleParts (ModuleId, PartId, Quantity)
                        VALUES (@ModuleId, @PartId, @Quantity)";
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ModuleId", modulePart.ModuleId);
                        command.Parameters.AddWithValue("@PartId", modulePart.PartId);
                        command.Parameters.AddWithValue("@Quantity", modulePart.Quantity);
                        command.ExecuteNonQuery();
                    }
                }
                Log.Information("ModulePart added: Module {ModuleId} -> Part {PartId}", 
                    modulePart.ModuleId, modulePart.PartId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error adding module part");
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
                    using (var command = new SQLiteCommand("DELETE FROM ModuleParts WHERE Id = @Id", connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.ExecuteNonQuery();
                    }
                }
                Log.Information("ModulePart deleted: {Id}", id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting module part: {Id}", id);
                throw;
            }
        }

        private ModulePart MapModulePart(SQLiteDataReader reader)
        {
            return new ModulePart
            {
                Id = Convert.ToInt32(reader["Id"]),
                ModuleId = Convert.ToInt32(reader["ModuleId"]),
                PartId = Convert.ToInt32(reader["PartId"]),
                Quantity = Convert.ToInt32(reader["Quantity"])
            };
        }
    }
}
