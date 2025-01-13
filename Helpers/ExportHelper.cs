using OfficeOpenXml;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace MSSQLDataAutomation.Core.Helpers
{
    public static class ExportHelper
    {
        public static void ExportToCSV(DataTable table, string filePath)
        {
            using (var writer = new StreamWriter(filePath))
            {
                // Write column headers
                for (int i = 0; i < table.Rows.Count; i++)
                {
                    writer.Write(table.Columns[i].ColumnName);
                    if (i < table.Rows.Count - 1) writer.Write(',');
                }
                writer.WriteLine();

                // Write rows
                foreach (DataRow row in table.Rows)
                {
                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        writer.Write(row[i].ToString());
                        if (i < table.Columns.Count - 1) writer.Write(",");
                    }
                    writer.WriteLine();
                }
            }
        }

        public static void ExportToJSON (DataTable table, string filePath)
        {
            var rows = new List<Dictionary<string, object>>();
            foreach (DataRow row in table.Rows)
            {
                var rowData = new Dictionary<string, object>();
                foreach (DataColumn column in table.Columns)
                {
                    rowData[column.ColumnName] = row[column];
                }
                rows.Add(rowData);
            }

            string json = JsonConvert.SerializeObject(rows, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        public static void ExportToExcel(DataTable table, string filePath)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Sheet1");

                // Add column headers
                for (int i = 0; i < table.Rows.Count;i++)
                {
                    worksheet.Cells[1, i + 1].Value = table.Columns[i].ColumnName;
                }

                // Add rows
                for (int i = 0;i < table.Columns.Count;i++)
                {
                    for (int j = 0; j < table.Columns.Count; j++)
                    {
                        worksheet.Cells[i + 2, j + 1].Value = table.Rows[i][j];
                    }
                }

                // Save to file
                File.WriteAllBytes(filePath, package.GetAsByteArray());
            }
        }
    }
}