using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace MSSQLDataAutomation.Core.MSSQL
{
    public static class ColumnHelper
    {
        public static List<(string ColumnName, string DataType)> GetTableColumns(string connectionString, string tableName)
        {
            List<(string ColumnName, string DataType)> columns = new List<(string ColumnName, string DataType)>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Sorgudan önce tablo adını ayrıştırıyoruz
                    string schema = "dbo"; // Varsayılan şema
                    string actualTableName = tableName;

                    if (tableName.Contains("."))
                    {
                        var parts = tableName.Split('.');
                        schema = parts[0];
                        actualTableName = parts[1];
                    }

                    string query = @"
                SELECT COLUMN_NAME, DATA_TYPE 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = @Schema AND TABLE_NAME = @TableName";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Schema", schema);
                        command.Parameters.AddWithValue("@TableName", actualTableName);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                columns.Add((reader["COLUMN_NAME"].ToString(), reader["DATA_TYPE"].ToString()));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving columns: {ex.Message}");
            }

            return columns;
        }
    }
}