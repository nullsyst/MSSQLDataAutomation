using System;
using System.Data;
using System.Data.SqlClient;

namespace MSSQLDataAutomation.Core.Helpers
{
    public static class DataRetriavalHelper
    {
        public static DataTable GetTableData(string connectionString, string tableName)
        {
            DataTable table = new DataTable();
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = $"SELECT * FROM {tableName}";
                    using (var command = new SqlCommand(query, connection))
                    using (var adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(table);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving data: {ex.Message}");
            }

            return table;
        }
    }
}
