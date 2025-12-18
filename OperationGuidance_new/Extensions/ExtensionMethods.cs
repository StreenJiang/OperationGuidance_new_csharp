using ClosedXML.Excel;
using log4net;
using OperationGuidance_new.Utils;

namespace OperationGuidance_new.Extensions {
    public static class ExtenstionMethods {
        private static ILog logger = MainUtils.GetLogger(typeof(ExtenstionMethods));

        // 将IEnumerable中的数据存入Txt文件
        public static void ExportToTextFile<T>(this IEnumerable<T> data, List<string>? headers,
                string filePath, bool fileExists, string columnSeperator = "\t") where T : List<object?> {
            try {

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
            } catch (Exception e) {
                logger.Debug($"Store data to txt failed, e: {e}");
            }
        }
        // 将IEnumerable中的数据存入Excel文件
        public static async void ExportToExcelFile<T>(this IEnumerable<T> data, List<string>? headers, string filePath, bool fileExists) {
            await Task.Run(async () => {
                try {
                    XLWorkbook xLWorkbook;
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

                    // 添加重试次数限制和超时机制
                    int maxRetries = 5;
                    int retryCount = 0;
                    bool success = false;

                    while (!success && retryCount < maxRetries) {
                        try {
                            Write(xLWorkbook, filePath);
                            success = true;
                            break;
                        } catch (Exception e) {
                            retryCount++;
                            logger.Debug($"Excel write attempt {retryCount} failed: {e.Message}");

                            if (retryCount >= maxRetries) {
                                logger.Warn($"Excel write failed after {maxRetries} attempts, giving up");
                                break;
                            }

                            await Task.Delay(200); // 延迟200ms后重试
                        }
                    }

                    xLWorkbook.Dispose();
                } catch (Exception e) {
                    logger.Debug($"Store data to excel failed, e: {e}");
                }

                bool Write(XLWorkbook book, string path) {
                    try {
                        book.SaveAs(path);
                        return true;
                    } catch (Exception e) {
                        logger.Debug($"Store data to excel failed while writing, e: {e}");
                        return false;
                    }
                }
            });
        }
    }
}
