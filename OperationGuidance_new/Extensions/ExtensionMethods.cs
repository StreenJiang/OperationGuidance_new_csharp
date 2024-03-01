using ClosedXML.Excel;

namespace OperationGuidance_new.Extensions {
    public static class ExtenstionMethods {
        public static void ExportToTextFile<T>(this IEnumerable<T> data, List<string>? headers, 
                string filePath, bool fileExists, string columnSeperator = "\t") where T: List<object?> {
            FileStream fileStream;
            if (fileExists) {
                fileStream = File.Open(filePath, FileMode.Append, FileAccess.Write);
            } else {
                fileStream = File.Create(filePath);
            }
            using (StreamWriter streamWriter = new(fileStream)) {
                string seperator = columnSeperator.ToString();
                if (headers != null) {
                    if (fileExists) {
                        streamWriter.WriteLine();
                    }
                    streamWriter.WriteLine(string.Join(seperator, headers));
                }
                foreach (T item in data) {
                    streamWriter.WriteLine(string.Join(seperator, item));
                }
            }
        }

        public static bool ExportToExcelFile<T>(this IEnumerable<T> data, List<string>? headers, string filePath, bool fileExists) { 
            XLWorkbook? xLWorkbook = null;
            try {
                string sheetName = "TighteningData";
                if (fileExists) {
                    xLWorkbook = new XLWorkbook(filePath);
                } else {
                    xLWorkbook = new();
                }
                IXLWorksheet sheet1;
                if (!xLWorkbook.Worksheets.Contains(sheetName)) {
                    sheet1 = xLWorkbook.Worksheets.Add(sheetName);
                } else {
                    sheet1 = xLWorkbook.Worksheet(sheetName);
                }
                int rowCount = sheet1.Rows().Count();
                if (headers != null) {
                    if (rowCount > 0) {
                        rowCount++;
                    }
                    sheet1.Cell(++rowCount, 1).InsertData(new List<List<string>>() { headers });
                }
                sheet1.Cell(rowCount + 1, 1).InsertData(data);
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
