using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using SolidEdgeConfigurator.Models;
using Serilog;

namespace SolidEdgeConfigurator.Services
{
    public class DatabaseService : IDisposable
    {
        private string _connectionString;
        private const string DatabaseName = "SolidEdgeConfigurator.db";

        public DatabaseService()
        {
            string dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SolidEdgeConfigurator",
                DatabaseName
            );

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath));

            _connectionString = $"Data Source={dbPath};";
            InitializeDatabase();
        }

        /// <summary>
        /// Initialize or create the database
        /// </summary>
        private void InitializeDatabase()
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    // Create Parts table - REMOVE UNIQUE from PartNumber
                    string createPartsTable = @"
                        CREATE TABLE IF NOT EXISTS Parts (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            PartName TEXT NOT NULL,
                            PartNumber TEXT,
                            ComponentName TEXT,
                            UnitPrice REAL NOT NULL,
                            Supplier TEXT,
                            Description TEXT,
                            Quantity INTEGER DEFAULT 1,
                            Unit TEXT DEFAULT 'pcs'
                        )";

                    using (var command = new SqliteCommand(createPartsTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    // Create BOM_Items junction table
                    string createBOMItemsTable = @"
                        CREATE TABLE IF NOT EXISTS BOM_Items (
                            BOMId INTEGER,
                            PartId INTEGER,
                            Quantity INTEGER,
                            FOREIGN KEY(BOMId) REFERENCES BOM(Id),
                            FOREIGN KEY(PartId) REFERENCES Parts(Id)
                        )";

                    using (var command = new SqliteCommand(createBOMItemsTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    Log.Information("✓ Database initialized");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "✗ Database initialization error: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Add a new part to the database
        /// </summary>
        public void AddPart(Part part)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    string query = @"
                        INSERT INTO Parts (PartName, PartNumber, ComponentName, UnitPrice, Supplier, Description, Quantity, Unit)
                        VALUES (@PartName, @PartNumber, @ComponentName, @UnitPrice, @Supplier, @Description, @Quantity, @Unit)";

                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@PartName", part.PartName ?? "");
                        command.Parameters.AddWithValue("@PartNumber", part.PartNumber ?? "");
                        command.Parameters.AddWithValue("@ComponentName", part.ComponentName ?? "");
                        command.Parameters.AddWithValue("@UnitPrice", part.UnitPrice);
                        command.Parameters.AddWithValue("@Supplier", part.Supplier ?? "");
                        command.Parameters.AddWithValue("@Description", part.Description ?? "");
                        command.Parameters.AddWithValue("@Quantity", part.Quantity);
                        command.Parameters.AddWithValue("@Unit", part.Unit ?? "pcs");

                        command.ExecuteNonQuery();
                        Log.Information("✓ Part '{PartName}' added to database", part.PartName);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error adding part: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Update an existing part
        /// </summary>
        public void UpdatePart(Part part)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    string query = @"
                        UPDATE Parts 
                        SET PartName = @PartName, PartNumber = @PartNumber, ComponentName = @ComponentName,
                            UnitPrice = @UnitPrice, Supplier = @Supplier, Description = @Description,
                            Quantity = @Quantity, Unit = @Unit
                        WHERE Id = @Id";

                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", part.Id);
                        command.Parameters.AddWithValue("@PartName", part.PartName ?? "");
                        command.Parameters.AddWithValue("@PartNumber", part.PartNumber ?? "");
                        command.Parameters.AddWithValue("@ComponentName", part.ComponentName ?? "");
                        command.Parameters.AddWithValue("@UnitPrice", part.UnitPrice);
                        command.Parameters.AddWithValue("@Supplier", part.Supplier ?? "");
                        command.Parameters.AddWithValue("@Description", part.Description ?? "");
                        command.Parameters.AddWithValue("@Quantity", part.Quantity);
                        command.Parameters.AddWithValue("@Unit", part.Unit ?? "pcs");

                        command.ExecuteNonQuery();
                        Log.Information("✓ Part '{PartName}' updated", part.PartName);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating part: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Delete a part
        /// </summary>
        public void DeletePart(int partId)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    string query = "DELETE FROM Parts WHERE Id = @Id";

                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", partId);
                        command.ExecuteNonQuery();
                        Log.Information("✓ Part deleted");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting part: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Get all parts
        /// </summary>
        public List<Part> GetAllParts()
        {
            var parts = new List<Part>();

            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    string query = "SELECT * FROM Parts ORDER BY PartName";

                    using (var command = new SqliteCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                parts.Add(new Part
                                {
                                    Id = (int)reader["Id"],
                                    PartName = reader["PartName"].ToString(),
                                    PartNumber = reader["PartNumber"].ToString(),
                                    ComponentName = reader["ComponentName"].ToString(),
                                    UnitPrice = Convert.ToDouble(reader["UnitPrice"]),
                                    Supplier = reader["Supplier"].ToString(),
                                    Description = reader["Description"].ToString(),
                                    Quantity = (int)reader["Quantity"],
                                    Unit = reader["Unit"].ToString()
                                });
                            }
                        }
                    }
                }

                Log.Information("✓ Retrieved {PartCount} parts from database", parts.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving parts: {Message}", ex.Message);
            }

            return parts;
        }

        /// <summary>
        /// Get parts by component name
        /// </summary>
        public List<Part> GetPartsByComponent(string componentName)
        {
            var parts = new List<Part>();

            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    string query = "SELECT * FROM Parts WHERE ComponentName = @ComponentName";

                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ComponentName", componentName);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                parts.Add(new Part
                                {
                                    Id = (int)reader["Id"],
                                    PartName = reader["PartName"].ToString(),
                                    PartNumber = reader["PartNumber"].ToString(),
                                    ComponentName = reader["ComponentName"].ToString(),
                                    UnitPrice = Convert.ToDouble(reader["UnitPrice"]),
                                    Supplier = reader["Supplier"].ToString(),
                                    Description = reader["Description"].ToString(),
                                    Quantity = (int)reader["Quantity"],
                                    Unit = reader["Unit"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving parts by component: {Message}", ex.Message);
            }

            return parts;
        }

        /// <summary>
        /// Generate BOM based on configuration
        /// </summary>
        public BOM GenerateBOM(ConfigurationSettings config)
        {
            var bom = new BOM
            {
                ConfigurationName = config.ConfigurationName,
                CreatedDate = DateTime.Now
            };

            try
            {
                foreach (var componentConfig in config.ComponentConfigs)
                {
                    if (componentConfig.IsVisible)
                    {
                        var parts = GetPartsByComponent(componentConfig.ComponentName);
                        bom.Parts.AddRange(parts);
                    }
                }

                Log.Information("✓ BOM generated with {PartCount} parts - Total Cost: ${TotalCost}", bom.Parts.Count, bom.TotalCost);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating BOM: {Message}", ex.Message);
            }

            return bom;
        }

        /// <summary>
        /// Export BOM to CSV
        /// </summary>
        public void ExportBOMToCSV(BOM bom, string filePath)
        {
            try
            {
                using (var writer = new StreamWriter(filePath))
                {
                    writer.WriteLine($"Bill of Materials - {bom.ConfigurationName}");
                    writer.WriteLine($"Generated: {bom.CreatedDate:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine();

                    writer.WriteLine("Part Name,Part Number,Component Name,Unit Price,Quantity,Unit,Total Price,Supplier,Description");

                    foreach (var part in bom.Parts)
                    {
                        writer.WriteLine($"\"{part.PartName}\",\"{part.PartNumber}\",\"{part.ComponentName}\",{part.UnitPrice},{part.Quantity},{part.Unit},{part.TotalPrice},\"{part.Supplier}\",\"{part.Description}\"");
                    }

                    writer.WriteLine();
                    writer.WriteLine($"Total Items: {bom.TotalItems}");
                    writer.WriteLine($"Total Cost: ${bom.TotalCost:F2}");
                }

                Log.Information("✓ BOM exported to: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error exporting BOM: {Message}", ex.Message);
                throw;
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}