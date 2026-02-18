using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text;
using Serilog;
using SolidEdgeConfigurator.Models;

namespace SolidEdgeConfigurator.Services
{
    public class DatabaseService : IDisposable
    {
        private readonly string _databasePath;
        private readonly string _connectionString;

        public string ConnectionString => _connectionString;

        public DatabaseService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "SolidEdgeConfigurator");
            Directory.CreateDirectory(appFolder);
            
            _databasePath = Path.Combine(appFolder, "SolidEdgeConfigurator.db");
            _connectionString = $"Data Source={_databasePath};Version=3;";
            
            InitializeDatabase();
        }

        /// <summary>
        /// Initialize or create the database
        /// </summary>
        private void InitializeDatabase()
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    // Create new modular architecture tables
                    CreateModularTables(connection);

                    // Keep legacy Parts table for backward compatibility (will be migrated)
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

                    CreateConfigurationTables();
                    
                    using (var command = new SQLiteCommand(createPartsTable, connection))
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

                    using (var command = new SQLiteCommand(createBOMItemsTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    Log.Information("✓ Database initialized at: {DatabasePath}", _databasePath);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "✗ Database initialization error: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Create new modular architecture tables
        /// </summary>
        private void CreateModularTables(SQLiteConnection connection)
        {
            try
            {
                // Categories table
                const string createCategoriesTable = @"
                    CREATE TABLE IF NOT EXISTS Categories (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Code TEXT NOT NULL UNIQUE,
                        Name TEXT NOT NULL,
                        Description TEXT,
                        DisplayOrder INTEGER DEFAULT 0,
                        IsActive INTEGER DEFAULT 1
                    )";
                ExecuteCommand(connection, createCategoriesTable);

                // Options table
                const string createOptionsTable = @"
                    CREATE TABLE IF NOT EXISTS Options (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Code TEXT NOT NULL UNIQUE,
                        Name TEXT NOT NULL,
                        Description TEXT,
                        CategoryId INTEGER NOT NULL,
                        DisplayOrder INTEGER DEFAULT 0,
                        IsActive INTEGER DEFAULT 1,
                        IsDefault INTEGER DEFAULT 0,
                        FOREIGN KEY(CategoryId) REFERENCES Categories(Id)
                    )";
                ExecuteCommand(connection, createOptionsTable);

                // Modules table
                const string createModulesTable = @"
                    CREATE TABLE IF NOT EXISTS Modules (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Code TEXT NOT NULL UNIQUE,
                        Name TEXT NOT NULL,
                        Description TEXT,
                        MasterAssemblyPath TEXT,
                        IsActive INTEGER DEFAULT 1
                    )";
                ExecuteCommand(connection, createModulesTable);

                // OptionModules junction table
                const string createOptionModulesTable = @"
                    CREATE TABLE IF NOT EXISTS OptionModules (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        OptionId INTEGER NOT NULL,
                        ModuleId INTEGER NOT NULL,
                        Quantity INTEGER DEFAULT 1,
                        FOREIGN KEY(OptionId) REFERENCES Options(Id),
                        FOREIGN KEY(ModuleId) REFERENCES Modules(Id)
                    )";
                ExecuteCommand(connection, createOptionModulesTable);

                // ModuleParts junction table
                const string createModulePartsTable = @"
                    CREATE TABLE IF NOT EXISTS ModuleParts (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ModuleId INTEGER NOT NULL,
                        PartId INTEGER NOT NULL,
                        Quantity INTEGER DEFAULT 1,
                        FOREIGN KEY(ModuleId) REFERENCES Modules(Id),
                        FOREIGN KEY(PartId) REFERENCES Parts(Id)
                    )";
                ExecuteCommand(connection, createModulePartsTable);

                Log.Information("✓ Modular tables created");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating modular tables: {Message}", ex.Message);
                throw;
            }
        }

        private void ExecuteCommand(SQLiteConnection connection, string commandText)
        {
            using (var command = new SQLiteCommand(commandText, connection))
            {
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Add a new part to the database
        /// </summary>
        public void AddPart(Part part)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    string query = @"
                        INSERT INTO Parts (PartName, PartNumber, ComponentName, UnitPrice, Supplier, Description, Quantity, Unit)
                        VALUES (@PartName, @PartNumber, @ComponentName, @UnitPrice, @Supplier, @Description, @Quantity, @Unit)";

                    using (var command = new SQLiteCommand(query, connection))
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
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    
                    const string query = @"
                        UPDATE Parts 
                        SET PartName = @PartName,
                            PartNumber = @PartNumber,
                            UnitPrice = @UnitPrice,
                            Quantity = @Quantity,
                            Unit = @Unit,
                            Supplier = @Supplier,
                            Description = @Description
                        WHERE Id = @Id";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", part.Id);
                        command.Parameters.AddWithValue("@PartName", part.PartName ?? "");
                        command.Parameters.AddWithValue("@PartNumber", part.PartNumber ?? "");
                        command.Parameters.AddWithValue("@UnitPrice", part.UnitPrice);
                        command.Parameters.AddWithValue("@Quantity", part.Quantity);
                        command.Parameters.AddWithValue("@Unit", part.Unit ?? "pcs");
                        command.Parameters.AddWithValue("@Supplier", part.Supplier ?? "");
                        command.Parameters.AddWithValue("@Description", part.Description ?? "");

                        int rowsAffected = command.ExecuteNonQuery();
                        Log.Information("UpdatePart: Updated {PartName} - Rows affected: {RowsAffected}", 
                            part.PartName, rowsAffected);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating part: {PartName}", part.PartName);
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
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    string query = "DELETE FROM Parts WHERE Id = @Id";

                    using (var command = new SQLiteCommand(query, connection))
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
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    string query = "SELECT * FROM Parts ORDER BY PartName";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                parts.Add(new Part
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    PartName = reader["PartName"].ToString(),
                                    PartNumber = reader["PartNumber"].ToString(),
                                    ComponentName = reader["ComponentName"].ToString(),
                                    UnitPrice = Convert.ToDouble(reader["UnitPrice"]),
                                    Supplier = reader["Supplier"].ToString(),
                                    Description = reader["Description"].ToString(),
                                    Quantity = Convert.ToInt32(reader["Quantity"]),
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
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    string query = "SELECT * FROM Parts WHERE ComponentName = @ComponentName";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ComponentName", componentName);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                parts.Add(new Part
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    PartName = reader["PartName"].ToString(),
                                    PartNumber = reader["PartNumber"].ToString(),
                                    ComponentName = reader["ComponentName"].ToString(),
                                    UnitPrice = Convert.ToDouble(reader["UnitPrice"]),
                                    Supplier = reader["Supplier"].ToString(),
                                    Description = reader["Description"].ToString(),
                                    Quantity = Convert.ToInt32(reader["Quantity"]),
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
        
        /// <summary>
        /// Create Configuration Options table
        /// </summary>
        private void CreateConfigurationTables()
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    // Configuration Options table
                    string createConfigTable = @"
                        CREATE TABLE IF NOT EXISTS ConfigurationOptions (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            ConfigName TEXT NOT NULL,
                            ColumnsSize TEXT,
                            IP TEXT,
                            VentilatedRoof TEXT,
                            HBB TEXT,
                            VBB TEXT,
                            Earth TEXT,
                            Neutral TEXT,
                            ESFile TEXT,
                            CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP
                        )";

                    using (var command = new SQLiteCommand(createConfigTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    // Configuration Parts mapping table
                    string createConfigPartsTable = @"
                        CREATE TABLE IF NOT EXISTS ConfigurationParts (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            ConfigurationOptionId INTEGER,
                            PartId INTEGER,
                            PartNumber TEXT,
                            PSMFileName TEXT,
                            FOREIGN KEY(ConfigurationOptionId) REFERENCES ConfigurationOptions(Id),
                            FOREIGN KEY(PartId) REFERENCES Parts(Id)
                        )";

                    using (var command = new SQLiteCommand(createConfigPartsTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    Log.Information("✓ Configuration tables created");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating configuration tables: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Add a configuration option
        /// </summary>
        public void AddConfigurationOption(ConfigurationOption config)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    const string query = @"
                        INSERT INTO ConfigurationOptions 
                        (ConfigName, ColumnsSize, IP, VentilatedRoof, HBB, VBB, Earth, Neutral, ESFile)
                        VALUES 
                        (@ConfigName, @ColumnsSize, @IP, @VentilatedRoof, @HBB, @VBB, @Earth, @Neutral, @ESFile)";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ConfigName", config.ConfigName ?? "");
                        command.Parameters.AddWithValue("@ColumnsSize", config.ColumnsSize ?? "");
                        command.Parameters.AddWithValue("@IP", config.IP ?? "");
                        command.Parameters.AddWithValue("@VentilatedRoof", config.VentilatedRoof ?? "");
                        command.Parameters.AddWithValue("@HBB", config.HBB ?? "");
                        command.Parameters.AddWithValue("@VBB", config.VBB ?? "");
                        command.Parameters.AddWithValue("@Earth", config.Earth ?? "");
                        command.Parameters.AddWithValue("@Neutral", config.Neutral ?? "");
                        command.Parameters.AddWithValue("@ESFile", config.ESFile ?? "");

                        command.ExecuteNonQuery();
                        Log.Information("✓ Configuration option added: {ConfigName}", config.ConfigName);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error adding configuration: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Get parts for a specific configuration
        /// </summary>
        public List<Part> GetPartsByConfiguration(string configName)
        {
            var parts = new List<Part>();

            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    const string query = @"
                        SELECT DISTINCT p.* 
                        FROM Parts p
                        INNER JOIN ConfigurationParts cp ON p.Id = cp.PartId
                        INNER JOIN ConfigurationOptions co ON cp.ConfigurationOptionId = co.Id
                        WHERE co.ConfigName = @ConfigName";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ConfigName", configName);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                parts.Add(new Part
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    PartName = reader["PartName"].ToString(),
                                    PartNumber = reader["PartNumber"].ToString(),
                                    ComponentName = reader["ComponentName"].ToString(),
                                    UnitPrice = Convert.ToDouble(reader["UnitPrice"]),
                                    Supplier = reader["Supplier"].ToString(),
                                    Description = reader["Description"].ToString(),
                                    Quantity = Convert.ToInt32(reader["Quantity"]),
                                    Unit = reader["Unit"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting parts by configuration: {Message}", ex.Message);
            }

            return parts;
        }

        /// <summary>
        /// Get all configuration options
        /// </summary>
        public List<ConfigurationOption> GetAllConfigurationOptions()
        {
            var configs = new List<ConfigurationOption>();

            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    const string query = "SELECT * FROM ConfigurationOptions ORDER BY ConfigName";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                configs.Add(new ConfigurationOption
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    ConfigName = reader["ConfigName"].ToString(),
                                    ColumnsSize = reader["ColumnsSize"].ToString(),
                                    IP = reader["IP"].ToString(),
                                    VentilatedRoof = reader["VentilatedRoof"].ToString(),
                                    HBB = reader["HBB"].ToString(),
                                    VBB = reader["VBB"].ToString(),
                                    Earth = reader["Earth"].ToString(),
                                    Neutral = reader["Neutral"].ToString(),
                                    ESFile = reader["ESFile"].ToString(),
                                    CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting configuration options: {Message}", ex.Message);
            }

            return configs;
        }
    }
}