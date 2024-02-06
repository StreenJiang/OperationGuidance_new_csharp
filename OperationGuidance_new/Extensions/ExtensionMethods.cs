using System.Reflection;
using ClosedXML.Excel;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Models;

namespace OperationGuidance_new.Extensions {
    public static class ExtenstionMethods {
        public static void ExportToTextFile<T>(this IEnumerable<T> data, string filePath, string columnSeperator = "\t") where T: OperationDataDTO {
            filePath = MainUtils.Settings.Read(IniFileKeys.DataStoragePath) + filePath;
            FileStream fileStream;
            bool needNewHeader = false;
            if (File.Exists(filePath)) {
                fileStream = File.Open(filePath, FileMode.Append, FileAccess.Write);
            } else {
                fileStream = File.Create(filePath);
                needNewHeader = true;
            }
            using (StreamWriter streamWriter = new(fileStream)) {
                List<PropertyInfo> properties = typeof(T).GetProperties().Where(p => p.CanRead && (p.PropertyType.IsValueType || p.PropertyType == typeof(string)) && p.GetIndexParameters().Length == 0).ToList();
                if (properties.Count > 0) {
                    var seperator = columnSeperator.ToString();
                    if (needNewHeader) {
                        streamWriter.WriteLine(string.Join(seperator, properties.Select(p => p.Name)));
                    }
                    foreach (T item in data) {
                        if (item != null) {
                            List<object> values = new();
                            foreach (var p in properties) values.Add(p.GetValue(item, null));
                            streamWriter.WriteLine(string.Join(seperator, values));
                        }
                    }
                }
            }
        }

        public static bool ExportToExcelFile<T>(this IEnumerable<T> data, string filePath) where T: OperationDataDTO { 
            XLWorkbook? xLWorkbook = null;
            try {
                filePath = MainUtils.Settings.Read(IniFileKeys.DataStoragePath) + filePath;
                List<string>? names = null;
                if (File.Exists(filePath)) {
                    xLWorkbook = new XLWorkbook(filePath);
                } else {
                    xLWorkbook = new();
                    names = typeof(T).GetProperties().Where(p => p.CanRead && (p.PropertyType.IsValueType || p.PropertyType == typeof(string)) && p.GetIndexParameters().Length == 0).Select(p => p.Name).ToList();
                }
                IXLWorksheet sheet1;
                if (!xLWorkbook.Worksheets.Contains("Sheet1")) {
                    sheet1 = xLWorkbook.Worksheets.Add("Sheet1");
                } else {
                    sheet1 = xLWorkbook.Worksheet("Sheet1");
                }
                int row = sheet1.Rows().Count();
                if (names != null) {
                    row += 1;
                    sheet1.Cell(1, 1).InsertData(new List<List<string>>() { names });
                }
                sheet1.Cell(row + 1, 1).InsertData(data);
                xLWorkbook.SaveAs(filePath);
                // xLWorkbook.Save(); // this will throw exception
                xLWorkbook.Dispose();
                return true;
            } catch (Exception e) {
                System.Console.WriteLine($"Store data failed, e: {e}");
                return false;
            } finally {
                if (xLWorkbook != null) {
                    xLWorkbook.Dispose();
                }
            }
        }
    }
}
