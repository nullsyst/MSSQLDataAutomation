using System;
using System.Collections.Generic;

namespace MSSQLDataAutomation
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("MSSQL Data Automation");

            // Retrieve SQL Server instances
            Console.WriteLine("\nRetrieving available SQL Server instances...");
            List<string> instances = InstanceHelper.GetSqlInstances();

            if (instances.Count > 0)
            {
                Console.WriteLine("\nAvailable SQL Server Instances:");
                for (int i = 0; i < instances.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {instances[i]}");
                }

                Console.WriteLine("\nEnter the number of the instance you want to connect to: ");
                if (int.TryParse(Console.ReadLine(), out int selectedInstanceIndex) &&
                    selectedInstanceIndex > 0 && selectedInstanceIndex <= instances.Count)
                {
                    string selectedInstance = instances[selectedInstanceIndex - 1];
                    Console.WriteLine($"Selected instance: {selectedInstance}");

                    // Get connection string
                    string connectionString = ConnectionHelper.GetConnectionString(selectedInstance);

                    // Retrieve databases
                    Console.WriteLine("\nRetrieving available databases...");
                    List<string> databases = DatabaseHelper.GetDatabases(connectionString);

                    if (databases.Count > 0)
                    {
                        Console.WriteLine("\nAvailable Databases:");
                        for (int i = 0; i < databases.Count; i++)
                        {
                            Console.WriteLine($"{i + 1}. {databases[i]}");
                        }

                        Console.WriteLine("\nEnter the number of the database you want to connect to: ");
                        string selectedDatabase = null;
                        if (int.TryParse(Console.ReadLine(), out int selectedDatabaseIndex) &&
                            selectedDatabaseIndex > 0 && selectedDatabaseIndex <= databases.Count)
                        {
                            selectedDatabase = databases[selectedDatabaseIndex - 1];
                            Console.WriteLine($"Selected database: {selectedDatabase}");

                            // Connection string with selected database
                            string connectionStringWithDb = $"{connectionString};Database={selectedDatabase};";

                            // Retrieve tables from the selected database
                            Console.WriteLine("\nRetrieving tables from the selected database...");
                            List<string> tables = TableHelper.GetTables(connectionStringWithDb);

                            if (tables.Count > 0)
                            {
                                Console.WriteLine("\nAvailable Tables:");
                                for (int i = 0; i < tables.Count; i++)
                                {
                                    Console.WriteLine($"{i + 1}. {tables[i]}");
                                }

                                // Filter tables by keyword
                                Console.WriteLine("\nEnter a keyword to filter tables (leave empty to skip): ");
                                string filterKeyword = Console.ReadLine();

                                List<string> filteredTables = string.IsNullOrEmpty(filterKeyword)
                                    ? tables
                                    : tables.Where(t => t.Contains(filterKeyword, StringComparison.OrdinalIgnoreCase)).ToList();
                                if (filteredTables.Count > 0)
                                {
                                    Console.WriteLine("\nFiltered Tables:");
                                    for (int i = 0; i < filteredTables.Count; i++)
                                    {
                                        Console.WriteLine($"{i + 1}.{filteredTables[i]}");
                                    }

                                    Console.WriteLine("\nEnter the number of the table you want to interact with: ");
                                    if (int.TryParse(Console.ReadLine(), out int selectedTableIndex) &&
                                        selectedTableIndex > 0 && selectedTableIndex <= filteredTables.Count)
                                    {
                                        string selectedTable = filteredTables[selectedTableIndex - 1];
                                        Console.WriteLine($"Selected table: {selectedTable}");

                                        // Here, you can add the code interact with selected table
                                        Console.WriteLine($"Performing operations on table: {selectedTable}");
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("No tables found in the selected database.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid database selection.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No databases found.");
                    }
                }
                else
                {
                    Console.WriteLine("Invalid instance selection.");
                }
            }
            else
            {
                Console.WriteLine("No SQL Server instances found.");
            }
        }
    }
}
