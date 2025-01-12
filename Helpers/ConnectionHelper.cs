using System;

namespace MSSQLDataAutomation
{
    public static class ConnectionHelper
    {
        public static string GetConnectionString(string instanceName)
        {
            Console.Write("Do you want to use Windows Authentication? (Y/n): ");
            string authChoice = Console.ReadLine();

            if (authChoice?.ToLower() == "y")
            {
                return $"Server={instanceName};Integrated Security=True;";
            }
            else
            {
                Console.Write("SQL Username: ");
                string userId = Console.ReadLine();

                Console.Write("SQL Password: ");
                string password = Console.ReadLine();

                return $"Server={instanceName};User Id={userId};Password={password};";
            }
        }
    }
}
