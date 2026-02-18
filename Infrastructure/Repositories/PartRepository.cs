using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Serilog;
using SolidEdgeConfigurator.Domain.Entities;
using SolidEdgeConfigurator.Domain.Interfaces;

namespace SolidEdgeConfigurator.Infrastructure.Repositories
{
    public class PartRepository : IPartRepository
    {
        private readonly string _connectionString;

        public PartRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Part GetById(int id)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand("SELECT * FROM Parts WHERE Id = @Id", connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read()) return MapPart(reader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting part by ID: {Id}", id);
            }
            return null;
        }

        public Part GetByCode(string code)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand("SELECT * FROM Parts WHERE Code = @Code", connection))
                    {
                        command.Parameters.AddWithValue("@Code", code);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read()) return MapPart(reader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting part by code: {Code}", code);
            }
            return null;
        }

        public List<Part> GetAll()
        {
            var parts = new List<Part>();
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand("SELECT * FROM Parts ORDER BY Code", connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read()) parts.Add(MapPart(reader));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting all parts");
            }
            return parts;
        }

        public List<Part> GetActive()
        {
            var parts = new List<Part>();
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand("SELECT * FROM Parts WHERE IsActive = 1 ORDER BY Code", connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read()) parts.Add(MapPart(reader));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting active parts");
            }
            return parts;
        }

        public List<Part> GetByModule(int moduleId)
        {
            var parts = new List<Part>();
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string query = @"
                        SELECT p.* FROM Parts p
                        INNER JOIN ModuleParts mp ON p.Id = mp.PartId
                        WHERE mp.ModuleId = @ModuleId AND p.IsActive = 1
                        ORDER BY p.Code";
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ModuleId", moduleId);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read()) parts.Add(MapPart(reader));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting parts by module: {ModuleId}", moduleId);
            }
            return parts;
        }

        public void Add(Part part)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string query = @"
                        INSERT INTO Parts (Code, Name, Description, PartNumber, UnitPrice, Supplier, Unit, ComponentName, IsActive)
                        VALUES (@Code, @Name, @Description, @PartNumber, @UnitPrice, @Supplier, @Unit, @ComponentName, @IsActive)";
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Code", part.Code ?? "");
                        command.Parameters.AddWithValue("@Name", part.Name ?? "");
                        command.Parameters.AddWithValue("@Description", part.Description ?? "");
                        command.Parameters.AddWithValue("@PartNumber", part.PartNumber ?? "");
                        command.Parameters.AddWithValue("@UnitPrice", part.UnitPrice);
                        command.Parameters.AddWithValue("@Supplier", part.Supplier ?? "");
                        command.Parameters.AddWithValue("@Unit", part.Unit ?? "pcs");
                        command.Parameters.AddWithValue("@ComponentName", part.ComponentName ?? "");
                        command.Parameters.AddWithValue("@IsActive", part.IsActive ? 1 : 0);
                        command.ExecuteNonQuery();
                    }
                }
                Log.Information("Part added: {Code}", part.Code);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error adding part: {Code}", part.Code);
                throw;
            }
        }

        public void Update(Part part)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string query = @"
                        UPDATE Parts 
                        SET Code = @Code, Name = @Name, Description = @Description,
                            PartNumber = @PartNumber, UnitPrice = @UnitPrice, Supplier = @Supplier,
                            Unit = @Unit, ComponentName = @ComponentName, IsActive = @IsActive
                        WHERE Id = @Id";
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", part.Id);
                        command.Parameters.AddWithValue("@Code", part.Code ?? "");
                        command.Parameters.AddWithValue("@Name", part.Name ?? "");
                        command.Parameters.AddWithValue("@Description", part.Description ?? "");
                        command.Parameters.AddWithValue("@PartNumber", part.PartNumber ?? "");
                        command.Parameters.AddWithValue("@UnitPrice", part.UnitPrice);
                        command.Parameters.AddWithValue("@Supplier", part.Supplier ?? "");
                        command.Parameters.AddWithValue("@Unit", part.Unit ?? "pcs");
                        command.Parameters.AddWithValue("@ComponentName", part.ComponentName ?? "");
                        command.Parameters.AddWithValue("@IsActive", part.IsActive ? 1 : 0);
                        command.ExecuteNonQuery();
                    }
                }
                Log.Information("Part updated: {Code}", part.Code);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating part: {Code}", part.Code);
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
                    using (var command = new SQLiteCommand("DELETE FROM Parts WHERE Id = @Id", connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.ExecuteNonQuery();
                    }
                }
                Log.Information("Part deleted: {Id}", id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting part: {Id}", id);
                throw;
            }
        }

        private Part MapPart(SQLiteDataReader reader)
        {
            return new Part
            {
                Id = Convert.ToInt32(reader["Id"]),
                Code = reader["Code"].ToString(),
                Name = reader["Name"].ToString(),
                Description = reader["Description"].ToString(),
                PartNumber = reader["PartNumber"].ToString(),
                UnitPrice = Convert.ToDouble(reader["UnitPrice"]),
                Supplier = reader["Supplier"].ToString(),
                Unit = reader["Unit"].ToString(),
                ComponentName = reader["ComponentName"].ToString(),
                IsActive = Convert.ToInt32(reader["IsActive"]) == 1
            };
        }
    }
}
