using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Serilog;
using SolidEdgeConfigurator.Domain.Entities;
using SolidEdgeConfigurator.Domain.Interfaces;

namespace SolidEdgeConfigurator.Infrastructure.Repositories
{
    public class OptionModuleRepository : IOptionModuleRepository
    {
        private readonly string _connectionString;

        public OptionModuleRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<OptionModule> GetByOption(int optionId)
        {
            var optionModules = new List<OptionModule>();
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand("SELECT * FROM OptionModules WHERE OptionId = @OptionId", connection))
                    {
                        command.Parameters.AddWithValue("@OptionId", optionId);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read()) optionModules.Add(MapOptionModule(reader));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting option modules by option: {OptionId}", optionId);
            }
            return optionModules;
        }

        public List<OptionModule> GetByModule(int moduleId)
        {
            var optionModules = new List<OptionModule>();
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand("SELECT * FROM OptionModules WHERE ModuleId = @ModuleId", connection))
                    {
                        command.Parameters.AddWithValue("@ModuleId", moduleId);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read()) optionModules.Add(MapOptionModule(reader));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting option modules by module: {ModuleId}", moduleId);
            }
            return optionModules;
        }

        public void Add(OptionModule optionModule)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string query = @"
                        INSERT INTO OptionModules (OptionId, ModuleId, Quantity)
                        VALUES (@OptionId, @ModuleId, @Quantity)";
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@OptionId", optionModule.OptionId);
                        command.Parameters.AddWithValue("@ModuleId", optionModule.ModuleId);
                        command.Parameters.AddWithValue("@Quantity", optionModule.Quantity);
                        command.ExecuteNonQuery();
                    }
                }
                Log.Information("OptionModule added: Option {OptionId} -> Module {ModuleId}", 
                    optionModule.OptionId, optionModule.ModuleId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error adding option module");
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
                    using (var command = new SQLiteCommand("DELETE FROM OptionModules WHERE Id = @Id", connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.ExecuteNonQuery();
                    }
                }
                Log.Information("OptionModule deleted: {Id}", id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting option module: {Id}", id);
                throw;
            }
        }

        private OptionModule MapOptionModule(SQLiteDataReader reader)
        {
            return new OptionModule
            {
                Id = Convert.ToInt32(reader["Id"]),
                OptionId = Convert.ToInt32(reader["OptionId"]),
                ModuleId = Convert.ToInt32(reader["ModuleId"]),
                Quantity = Convert.ToInt32(reader["Quantity"])
            };
        }
    }
}
