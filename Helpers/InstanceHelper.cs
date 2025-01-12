using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Smo.Wmi;

namespace MSSQLDataAutomation
{
    public static class InstanceHelper
    {
        public static List<string> GetSqlInstances()
        {
            var instances = new List<string>();

            try
            {
                ManagedComputer managedComputer = new ManagedComputer();
                foreach (ServerInstance instance in managedComputer.ServerInstances)
                {
                    // Combine machine name and instance name
                    string fullInstanceName = $"{Environment.MachineName}\\{instance.Name}";
                    instances.Add(fullInstanceName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving SQL Server instances: {ex.Message}");
            }

            return instances;
        }
    }
}
