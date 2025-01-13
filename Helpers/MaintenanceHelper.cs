using Microsoft.SqlServer.Management.HadrModel;
using System;
using System.Data.SqlClient;

namespace MSSQLDataAutomation.Core.MSSQL
{
    public static class MaintenanceHelper
    {
        public static void PerformDatabaseMaintenance(string connectionString, string databaseName)
        {
            try
            {
                Console.WriteLine($"Performing maintenance on database: {databaseName}...");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Step 1: Check database consistency
                    Console.WriteLine("Step 1: Checking database consistency...");
                    ExecuteNonQuery(connection, $"DBCC CHECKDB ('{databaseName}') WITH NO_INFOMSGS");

                    // Step 2: Reorganize and rebuild indexes
                    string indexOptimizeQuery = @"
                        DECLARE @TableName NVARCHAR(MAX)
                        DECLARE @IndexName NVARCHAR(MAX)
                        DECLARE @SchemaName NVARCHAR(MAX)

                        DECLARE index_cursor CURSOR FOR
                        SELECT
                            s.name AS SchemaName,
	                        t.name AS TableName,
	                        i.name AS IndexName
                        FROM sys.dm_db_index_physical_stats (DB_ID(), NULL, NULL, NULL, 'DETAILED') ips
                        JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
                        JOIN sys.tables t ON t.object_id = ips.object_id
                        JOIN sys.schemas s ON t.schema_id = s.schema_id
                        WHERE ips.avg_fragmentation_in_percent > 30 AND ips.index_id > 0;

                        OPEN index_cursor
                        FETCH NEXT FROM index_cursor INTO @SchemaName, @TableName, @IndexName

                        WHILE @@FETCH_STATUS = 0
                        BEGIN
	                    PRINT 'Rebuilding Index: ' + @SchemaName + '.' + @TableName + ' -> ' + @IndexName;
	                    EXEC ('ALTER INDEX [' + @IndexName + '] ON [' +@SchemaName + '].[' + @TableName + '] REBUILD');
	                    FETCH NEXT FROM index_cursor INTO @SchemaName, @TableName, @IndexName
                        END
                        CLOSE index_cursor
                        DEALLOCATE index_cursor";
                    ExecuteNonQuery(connection, indexOptimizeQuery );

                    // Step 3: Updat statistics
                    Console.WriteLine("Step 3: Updating statistics...");
                    ExecuteNonQuery(connection, "EXEC sp_updatestats");

                    Console.WriteLine("Database maintenance completed successfully!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error performing database maintenance: {ex.Message}");
            }
        }

        private static void ExecuteNonQuery(SqlConnection connection, string query)
        {
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.CommandTimeout = 300; // 5 minutes timeout for long-running queries
                command.ExecuteNonQuery();
            }
        }
    }
}