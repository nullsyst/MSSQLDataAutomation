using Microsoft.Data.Sql;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using Newtonsoft.Json;
using System.Text;
using System.Linq;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Retrieving available SQL Server instances...\n");
        string[] instances = GetSqlServerInstances();

        if (instances.Length == 0)
        {
            Console.WriteLine("No SQL Server instances found.");
            return;
        }

        Console.WriteLine("Available SQL Server Instances:");
        for (int i = 0; i < instances.Length; i++)
        {
            Console.WriteLine($"{i + 1}. {instances[i]}");
        }

        Console.Write("Select the number of the instance you want to connect to: ");
        int instanceIndex = int.Parse(Console.ReadLine()) - 1;

        string instance = instances[instanceIndex];
        Console.Write("Do you want to use Windows Authentication? (Y/n): ");
        bool useWindowsAuth = Console.ReadLine().Trim().ToLower() == "y";

        string connectionString = BuildConnectionString(instance, useWindowsAuth);

        Console.WriteLine("\nRetrieving available databases...\n");
        string[] databases = GetDatabases(connectionString);

        Console.WriteLine("Available Databases:");
        for (int i = 0; i < databases.Length; i++)
        {
            Console.WriteLine($"{i + 1}. {databases[i]}");
        }

        Console.Write("Select the number of the database you want to connect to: ");
        int databaseIndex = int.Parse(Console.ReadLine()) - 1;

        string database = databases[databaseIndex];
        string connectionStringWithDb = $"{connectionString};Database={database};";

        while (true)
        {
            Console.WriteLine("\nWhat would you like to do?");
            Console.WriteLine("1. Database Maintenance");
            Console.WriteLine("2. Table/Column Operations");
            Console.WriteLine("3. Export Data");
            Console.WriteLine("4. Exit");
            Console.Write("Enter your choice (1-4): ");

            string actionChoice = Console.ReadLine();

            switch (actionChoice)
            {
                case "1":
                    PerformDatabaseMaintenance(connectionStringWithDb, database);
                    break;
                case "2":
                    PerformTableAndColumnOperations(connectionStringWithDb);
                    break;
                case "3":
                    ExportData(connectionStringWithDb);
                    break;
                case "4":
                    return;
                default:
                    Console.WriteLine("Invalid choice.");
                    break;
            }
        }
    }

    static string[] GetSqlServerInstances()
    {
        var instancesList = new List<string>();

        SqlDataSourceEnumerator instanceEnumerator = SqlDataSourceEnumerator.Instance;
        var table = instanceEnumerator.GetDataSources();

        foreach (DataRow row in table.Rows)
        {
            string serverName = row["ServerName"].ToString();
            string instanceName = row["InstanceName"] as string;

            if (string.IsNullOrEmpty(instanceName))
            {
                instancesList.Add(serverName);
            }
            else
            {
                instancesList.Add($"{serverName}\\{instanceName}");
            }
        }

        return instancesList.ToArray();
    }

    static string BuildConnectionString(string instance, bool useWindowsAuth)
    {
        if (useWindowsAuth)
        {
            return $"Server={instance};Integrated Security=True;";
        }

        Console.Write("Enter username: ");
        string username = Console.ReadLine();
        Console.Write("Enter password: ");
        string password = Console.ReadLine();
        return $"Server={instance};User Id={username};Password={password};";
    }

    static string[] GetDatabases(string connectionString)
    {
        var databases = new List<string>();
        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();
            using (var command = new SqlCommand(
                "SELECT name FROM sys.databases WHERE database_id > 4", connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    databases.Add(reader.GetString(0));
                }
            }
        }
        return databases.ToArray();
    }

    static void PerformDatabaseMaintenance(string connectionString, string databaseName)
    {
        Console.WriteLine("\nPerforming Database Maintenance...\n");
        string logFilePath = $"{databaseName}_MaintenanceLog_{DateTime.Now:yyyyMMdd_HHmmss}.log";

        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();
            using (var logWriter = new StreamWriter(logFilePath))
            {
                logWriter.WriteLine($"Database Maintenance Log for {databaseName}");
                logWriter.WriteLine($"Start Time: {DateTime.Now}");
                logWriter.WriteLine();

                try
                {
                    // 1. Check and fix database consistency
                    logWriter.WriteLine("Checking database consistency...");
                    ExecuteNonQuery(connection, "DBCC CHECKDB WITH DATA_PURITY", logWriter);

                    // 2. Update statistics with fullscan
                    logWriter.WriteLine("Updating statistics with fullscan...");
                    ExecuteNonQuery(connection, "EXEC sp_updatestats @resample = 'resample'", logWriter);

                    // 3. Rebuild or reorganize indexes based on fragmentation
                    logWriter.WriteLine("Analyzing and rebuilding indexes...");
                    string indexMaintenanceQuery = @"
                        DECLARE @TableName NVARCHAR(255)
                        DECLARE @IndexName NVARCHAR(255)
                        DECLARE @Fragmentation FLOAT
                        
                        DECLARE IndexCursor CURSOR FOR
                        SELECT OBJECT_NAME(ind.OBJECT_ID) AS TableName,
                               ind.name AS IndexName,
                               indexstats.avg_fragmentation_in_percent
                        FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, NULL) indexstats
                        INNER JOIN sys.indexes ind 
                        ON ind.object_id = indexstats.object_id 
                        AND ind.index_id = indexstats.index_id
                        WHERE indexstats.avg_fragmentation_in_percent > 5
                        
                        OPEN IndexCursor
                        FETCH NEXT FROM IndexCursor INTO @TableName, @IndexName, @Fragmentation
                        
                        WHILE @@FETCH_STATUS = 0
                        BEGIN
                            IF @Fragmentation >= 30
                                EXEC('ALTER INDEX [' + @IndexName + '] ON [' + @TableName + '] REBUILD WITH (ONLINE = ON)')
                            ELSE
                                EXEC('ALTER INDEX [' + @IndexName + '] ON [' + @TableName + '] REORGANIZE')
                                
                            FETCH NEXT FROM IndexCursor INTO @TableName, @IndexName, @Fragmentation
                        END
                        
                        CLOSE IndexCursor
                        DEALLOCATE IndexCursor";

                    ExecuteNonQuery(connection, indexMaintenanceQuery, logWriter);

                    // 4. Clean up unused space
                    logWriter.WriteLine("Cleaning up unused space...");
                    ExecuteNonQuery(connection, "DBCC CLEANTABLE", logWriter);

                    logWriter.WriteLine("\nDatabase maintenance completed successfully.");
                }
                catch (Exception ex)
                {
                    logWriter.WriteLine($"Error during maintenance: {ex.Message}");
                }
                finally
                {
                    logWriter.WriteLine($"End Time: {DateTime.Now}");
                }
            }
        }

        Console.WriteLine($"Database maintenance completed. Log saved to {logFilePath}");
    }

    static void PerformTableAndColumnOperations(string connectionString)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();

            // Get all tables
            Console.WriteLine("\nAvailable Tables:");
            DataTable tables = connection.GetSchema("Tables");
            for (int i = 0; i < tables.Rows.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {tables.Rows[i]["TABLE_NAME"]}");
            }

            Console.Write("\nSelect table number to analyze: ");
            int tableIndex = int.Parse(Console.ReadLine()) - 1;
            string tableName = tables.Rows[tableIndex]["TABLE_NAME"].ToString();

            Console.WriteLine($"\nAnalyzing table: {tableName}");

            // Check for missing indexes
            string missingIndexQuery = @"
                SELECT 
                    dm_mid.database_id AS DatabaseID,
                    dm_migs.avg_user_impact*(dm_migs.user_seeks+dm_migs.user_scans) Avg_Estimated_Impact,
                    dm_migs.last_user_seek AS Last_User_Seek,
                    OBJECT_NAME(dm_mid.OBJECT_ID,dm_mid.database_id) AS [TableName],
                    'CREATE INDEX [IX_' + OBJECT_NAME(dm_mid.OBJECT_ID,dm_mid.database_id) + '_'
                    + REPLACE(REPLACE(REPLACE(ISNULL(dm_mid.equality_columns,''),']',''),'[',''),' ','_')
                    + CASE
                    WHEN dm_mid.equality_columns IS NOT NULL
                    AND dm_mid.inequality_columns IS NOT NULL THEN '_'
                    ELSE ''
                    END
                    + REPLACE(REPLACE(REPLACE(ISNULL(dm_mid.inequality_columns,''),']',''),'[',''),' ','_')
                    + ']'
                    + ' ON ' + dm_mid.statement
                    + ' (' + ISNULL (dm_mid.equality_columns,'')
                    + CASE WHEN dm_mid.equality_columns IS NOT NULL AND dm_mid.inequality_columns 
                    IS NOT NULL THEN ',' ELSE
                    '' END
                    + ISNULL (dm_mid.inequality_columns, '')
                    + ')'
                    + ISNULL (' INCLUDE (' + dm_mid.included_columns + ')', '') AS Create_Statement
                FROM sys.dm_db_missing_index_groups dm_mig
                INNER JOIN sys.dm_db_missing_index_group_stats dm_migs
                ON dm_migs.group_handle = dm_mig.index_group_handle
                INNER JOIN sys.dm_db_missing_index_details dm_mid
                ON dm_mig.index_handle = dm_mid.index_handle
                WHERE dm_mid.database_ID = DB_ID()
                AND OBJECT_NAME(dm_mid.OBJECT_ID,dm_mid.database_id) = @TableName
                ORDER BY Avg_Estimated_Impact DESC";

            using (var command = new SqlCommand(missingIndexQuery, connection))
            {
                command.Parameters.AddWithValue("@TableName", tableName);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        Console.WriteLine("\nMissing Index Recommendations:");
                        while (reader.Read())
                        {
                            Console.WriteLine($"\nEstimated Impact: {reader["Avg_Estimated_Impact"]}");
                            Console.WriteLine($"Create Statement: {reader["Create_Statement"]}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No missing index recommendations found.");
                    }
                }
            }
        }
    }

    static void ExportData(string connectionString)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();

            // Get all tables
            Console.WriteLine("\nAvailable Tables:");
            DataTable tables = connection.GetSchema("Tables");
            for (int i = 0; i < tables.Rows.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {tables.Rows[i]["TABLE_NAME"]}");
            }

            Console.Write("\nSelect table number to export: ");
            int tableIndex = int.Parse(Console.ReadLine()) - 1;
            string tableName = tables.Rows[tableIndex]["TABLE_NAME"].ToString();

            Console.WriteLine("\nExport format:");
            Console.WriteLine("1. CSV");
            Console.WriteLine("2. JSON");
            Console.WriteLine("3. Excel (CSV with Excel formatting)");
            Console.Write("Select format (1-3): ");
            string format = Console.ReadLine();

            string query = $"SELECT * FROM [{tableName}]";
            using (var command = new SqlCommand(query, connection))
            using (var reader = command.ExecuteReader())
            {
                string fileName = $"{tableName}_{DateTime.Now:yyyyMMdd_HHmmss}";

                switch (format)
                {
                    case "1":
                    case "3":
                        ExportToCSV(reader, $"{fileName}.csv");
                        break;
                    case "2":
                        ExportToJSON(reader, $"{fileName}.json");
                        break;
                    default:
                        Console.WriteLine("Invalid format selected.");
                        return;
                }
            }
        }
    }

    static void ExportToCSV(SqlDataReader reader, string fileName)
    {
        using (var writer = new StreamWriter(fileName))
        {
            // Write headers
            var headers = new string[reader.FieldCount];
            for (int i = 0; i < reader.FieldCount; i++)
            {
                headers[i] = reader.GetName(i);
            }
            writer.WriteLine(string.Join(",", headers.Select(h => $"\"{h}\"")));

            // Write data
            while (reader.Read())
            {
                var values = new string[reader.FieldCount];
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? "" : reader.GetValue(i).ToString();
                    values[i] = $"\"{value.Replace("\"", "\"\"")}\"";
                }
                writer.WriteLine(string.Join(",", values));
            }
        }
        Console.WriteLine($"Data exported to {fileName}");
    }

    static void ExportToJSON(SqlDataReader reader, string fileName)
    {
        var data = new List<Dictionary<string, object>>();

        while (reader.Read())
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
            data.Add(row);
        }

        using (var writer = new StreamWriter(fileName))
        {
            writer.Write(JsonConvert.SerializeObject(data, Formatting.Indented));
        }
        Console.WriteLine($"Data exported to {fileName}");
    }

    static void ExecuteNonQuery(SqlConnection connection, string query, StreamWriter logWriter)
    {
        static void ExecuteNonQuery(SqlConnection connection, string query, StreamWriter logWriter)
        {
            try
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.CommandTimeout = 3600; // 1 saat timeout
                    int rowsAffected = command.ExecuteNonQuery();
                    logWriter.WriteLine($"Query executed successfully:");
                    logWriter.WriteLine($"Query: {query}");
                    logWriter.WriteLine($"Rows affected: {rowsAffected}");
                    logWriter.WriteLine();
                }
            }
            catch (Exception ex)
            {
                logWriter.WriteLine($"Error executing query:");
                logWriter.WriteLine($"Query: {query}");
                logWriter.WriteLine($"Error message: {ex.Message}");
                logWriter.WriteLine();
                throw;
            }
        }

        static void CheckAndCreateMissingIndexes(SqlConnection connection, string tableName, StreamWriter logWriter)
        {
            string checkIndexQuery = @"
            SELECT 
                OBJECT_NAME(id.[object_id]) AS [TableName],
                id.index_handle,
                id.equality_columns,
                id.inequality_columns,
                id.included_columns,
                gs.avg_total_user_cost * gs.avg_user_impact * (gs.user_seeks + gs.user_scans) AS improvement_measure,
                'CREATE INDEX [IX_' + OBJECT_NAME(id.[object_id]) + '_' +
                REPLACE(REPLACE(REPLACE(ISNULL(id.equality_columns,''), ']', ''), '[', ''), ', ', '_') + '_' +
                REPLACE(REPLACE(REPLACE(ISNULL(id.inequality_columns,''), ']', ''), '[', ''), ', ', '_') +
                CASE WHEN id.included_columns IS NOT NULL THEN '_includes' ELSE '' END + '] ON ' +
                id.[statement] + ' (' + ISNULL(id.equality_columns, '') +
                CASE WHEN id.inequality_columns IS NOT NULL AND id.equality_columns IS NOT NULL THEN ',' ELSE '' END +
                ISNULL(id.inequality_columns, '') + ')' +
                CASE WHEN id.included_columns IS NOT NULL THEN ' INCLUDE (' + id.included_columns + ')' ELSE '' END +
                ';' AS create_index_statement
            FROM sys.dm_db_missing_index_group_stats gs
            INNER JOIN sys.dm_db_missing_index_groups ig ON gs.group_handle = ig.index_group_handle
            INNER JOIN sys.dm_db_missing_index_details id ON ig.index_handle = id.index_handle
            WHERE OBJECT_NAME(id.[object_id]) = @tableName
            ORDER BY improvement_measure DESC";

            try
            {
                using (var command = new SqlCommand(checkIndexQuery, connection))
                {
                    command.Parameters.AddWithValue("@tableName", tableName);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string createIndexStatement = reader["create_index_statement"].ToString();
                            logWriter.WriteLine($"Creating missing index for table {tableName}:");
                            logWriter.WriteLine(createIndexStatement);

                            // Execute the create index statement
                            using (var createCommand = new SqlCommand(createIndexStatement, connection))
                            {
                                createCommand.ExecuteNonQuery();
                                logWriter.WriteLine("Index created successfully.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logWriter.WriteLine($"Error checking/creating indexes for table {tableName}:");
                logWriter.WriteLine(ex.Message);
                throw;
            }
        }

        static void ValidateTableData(SqlConnection connection, string tableName, StreamWriter logWriter)
        {
            try
            {
                // Check for duplicate primary key values
                string checkDuplicatesQuery = $@"
                SELECT COUNT(*) as duplicate_count
                FROM (
                    SELECT *
                    FROM [{tableName}]
                    GROUP BY /* primary key columns */
                    HAVING COUNT(*) > 1
                ) t";

                // Check for NULL values in required columns
                string checkNullsQuery = $@"
                SELECT COLUMN_NAME
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = @tableName
                AND IS_NULLABLE = 'NO'";

                // Check data integrity
                using (var command = new SqlCommand(checkNullsQuery, connection))
                {
                    command.Parameters.AddWithValue("@tableName", tableName);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string columnName = reader["COLUMN_NAME"].ToString();
                            string checkNullQuery = $"SELECT COUNT(*) FROM [{tableName}] WHERE [{columnName}] IS NULL";
                            using (var nullCheckCommand = new SqlCommand(checkNullQuery, connection))
                            {
                                int nullCount = (int)nullCheckCommand.ExecuteScalar();
                                if (nullCount > 0)
                                {
                                    logWriter.WriteLine($"Warning: Found {nullCount} NULL values in required column [{columnName}]");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logWriter.WriteLine($"Error validating table data for {tableName}:");
                logWriter.WriteLine(ex.Message);
                throw;
            }
        }
    }
}