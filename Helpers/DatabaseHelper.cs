using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace MSSQLDataAutomation
{
    public static class DatabaseHelper
    {
        public static List<string> GetDatabases(string connectionString)
        {
            var databases = new List<string>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand("SELECT name FROM sys.databases", connection))
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            databases.Add(reader.GetString(0));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving databases: {ex.Message}");
            }

            return databases;
        }
    }
}
