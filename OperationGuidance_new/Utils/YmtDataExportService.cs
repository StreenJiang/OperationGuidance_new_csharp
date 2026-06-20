using ClosedXML.Excel;
using log4net;
using OperationGuidance_new.Configs;
using OperationGuidance_new.ViewObjects;
using System.Collections.Concurrent;
using System.Text;
using System.IO;

namespace OperationGuidance_new.Utils {
    public class YmtDataExportService {
        private static readonly ILog _logger = MainUtils.GetLogger(typeof(YmtDataExportService));
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new();

        public async Task ExportAsync(ExportRequest request) {
            string mission = string.IsNullOrEmpty(request.MissionName) ? "null" : request.MissionName;
            // 任务名中的文件名非法字符 → %XX 转义（保证可逆，永不冲突）
            mission = SanitizeFileName(mission);
            string yyyyMM = request.CompletedAt.ToString("yyyy-MM");
            string yyyyMMdd = request.CompletedAt.ToString("yyyy-MM-dd");
            string monthFolder = Path.Combine(request.BasePath, yyyyMM);
            string fileNameBody = $"{mission}-{yyyyMMdd}";

            var visibleFields = request.Fields.Where(f => f.Visible).ToList();
            var propertyNames = visibleFields.Select(f => f.PropertyName).ToList();
            var headers = visibleFields.Select(f => f.FieldName).ToList();

            if (propertyNames.Count == 0) {
                _logger.Warn("[YMT Export] No visible fields configured");
            }

            var rows = BuildRows(request.Data ?? new List<OperationDataVO>(), propertyNames);

            // YMT: 无数据则不创建文件（等有实际数据再创建）
            if (rows.Count == 0) {
                _logger.Info("[YMT Export] No data rows — skipping file creation");
                return;
            }

            _logger.Info($"[YMT Export] Exporting {rows.Count} rows x {propertyNames.Count} cols to {monthFolder}");

            try {
                Directory.CreateDirectory(monthFolder);
            } catch (Exception ex) {
                _logger.Error($"[YMT Export] Failed to create directory: {monthFolder}", ex);
                throw new IOException($"无法创建导出目录: {monthFolder}", ex);
            }

            var exceptions = new List<Exception>();

            if (request.EnableExcel) {
                string xlsxPath = Path.Combine(monthFolder, $"{fileNameBody}.xlsx");
                try {
                    await AppendOrCreateExcelAsync(xlsxPath, headers, rows);
                    _logger.Info($"[YMT Export] Excel written: {xlsxPath}");
                } catch (Exception ex) {
                    _logger.Error($"[YMT Export] Excel write failed", ex);
                    exceptions.Add(new IOException($"Excel导出失败: {ex.Message}", ex));
                }
            }

            if (request.EnableTxt) {
                string txtPath = Path.Combine(monthFolder, $"{fileNameBody}.txt");
                try {
                    await AppendOrCreateTxtAsync(txtPath, headers, rows);
                    _logger.Info($"[YMT Export] Txt written: {txtPath}");
                } catch (Exception ex) {
                    _logger.Error($"[YMT Export] Txt write failed", ex);
                    exceptions.Add(new IOException($"Txt导出失败: {ex.Message}", ex));
                }
            }

            if (exceptions.Count == 1) throw exceptions[0];
            if (exceptions.Count > 1) throw new AggregateException("导出过程中发生错误", exceptions);
        }

        private async Task AppendOrCreateExcelAsync(string filePath, List<string> headers, List<List<object?>> rows) {
            var fileLock = _fileLocks.GetOrAdd(filePath, _ => new SemaphoreSlim(1, 1));
            await fileLock.WaitAsync();
            try {
                if (!File.Exists(filePath)) {
                    using (var wb = new XLWorkbook()) {
                        var sheet = wb.Worksheets.Add("TighteningData");
                        sheet.Cell(1, 1).InsertData(new List<List<string>> { headers });
                        sheet.Cell(2, 1).InsertData(rows);
                        wb.SaveAs(filePath);
                    }
                    return;
                }

                // 文件存在 — 读最后一段表头，与本次比对
                using (var wb = new XLWorkbook(filePath)) {
                    var sheet = wb.Worksheet(1);
                    var (existingHeaders, _) = ReadLastHeaderSection(sheet);

                    bool headersMatch = existingHeaders.Count == headers.Count
                        && existingHeaders.SequenceEqual(headers);

                    int lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
                    int nextRow;

                    if (headersMatch) {
                        nextRow = lastRow + 1;
                    } else {
                        // 表头不一致 → 空行 + 新 header（lastRow+1 为空行，lastRow+2 为表头）
                        nextRow = lastRow + 3;
                        sheet.Cell(nextRow - 1, 1).InsertData(new List<List<string>> { headers });
                    }

                    sheet.Cell(nextRow, 1).InsertData(rows);
                    wb.Save();
                }
            } finally {
                fileLock.Release();
                if (_fileLocks.TryRemove(filePath, out var removed)) {
                    removed.Dispose();
                }
            }
        }

        private async Task AppendOrCreateTxtAsync(string filePath, List<string> headers, List<List<object?>> rows) {
            var fileLock = _fileLocks.GetOrAdd(filePath, _ => new SemaphoreSlim(1, 1));
            await fileLock.WaitAsync();
            try {
                if (!File.Exists(filePath)) {
                    using (var sw = new StreamWriter(filePath, false)) {
                        sw.WriteLine(string.Join("\t", headers));
                        foreach (var row in rows) sw.WriteLine(string.Join("\t", row));
                    }
                    return;
                }

                // 文件存在 — 读最后一段表头比对
                var existingLines = File.ReadAllLines(filePath).ToList();
                // 从尾部向上找最近空行，下一行即最后一段表头
                int headerLineIdx = 0;
                for (int i = existingLines.Count - 1; i >= 0; i--) {
                    if (string.IsNullOrWhiteSpace(existingLines[i]) && i + 1 < existingLines.Count) {
                        headerLineIdx = i + 1;
                        break;
                    }
                }
                string? existingHeaderLine = headerLineIdx < existingLines.Count ? existingLines[headerLineIdx] : null;
                string newHeaderLine = string.Join("\t", headers);

                bool headersMatch = existingHeaderLine == newHeaderLine;

                using (var sw = new StreamWriter(filePath, true)) {
                    if (!headersMatch) {
                        sw.WriteLine(); // 空行
                        sw.WriteLine(newHeaderLine); // 新表头
                    }
                    foreach (var row in rows) sw.WriteLine(string.Join("\t", row));
                }
            } finally {
                fileLock.Release();
                if (_fileLocks.TryRemove(filePath, out var removed)) {
                    removed.Dispose();
                }
            }
        }

        private static List<List<object?>> BuildRows(List<OperationDataVO> data, List<string> propertyNames) {
            var propInfos = MainUtils.GetCachedOperationDataVOPropInfos();
            var rows = new List<List<object?>>(data.Count);
            foreach (var vo in data) {
                var row = new List<object?>(propertyNames.Count);
                foreach (var pName in propertyNames) {
                    row.Add(propInfos.TryGetValue(pName, out var pi) ? pi.GetValue(vo) : null);
                }
                rows.Add(row);
            }
            return rows;
        }

        /// <summary>
        /// 文件名非法字符 → %XX 十六进制转义（可逆），保留 Unicode 字符及安全 ASCII
        /// </summary>
        private static string SanitizeFileName(string name) {
            var invalids = new HashSet<char>(Path.GetInvalidFileNameChars());
            var sb = new StringBuilder(name.Length);
            foreach (char c in name) {
                if (invalids.Contains(c)) {
                    sb.Append('%');
                    sb.Append(((int)c).ToString("X2"));
                } else {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 从表格读取最后一段表头（遇到空行后下一次出现的 header 行即新段）
        /// 返回 (headers, dataStartRow: 该段数据起始行号, 从1开始)
        /// </summary>
        private static (List<string> headers, int dataStartRow) ReadLastHeaderSection(IXLWorksheet sheet) {
            int lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
            if (lastRow == 0) return (new List<string>(), 1);

            // 从最后一行向上扫描，找最近的空行分隔符
            int scanRow = lastRow;
            while (scanRow > 1 && !IsRowEmpty(sheet, scanRow)) {
                scanRow--;
            }
            // 空行的下一行就是当前段表头
            int headerRow = scanRow == 1 ? 1 : scanRow + 1;
            if (IsRowEmpty(sheet, headerRow)) return (new List<string>(), headerRow);

            int colCount = sheet.Row(headerRow).LastCellUsed()?.Address.ColumnNumber ?? 0;
            var headers = new List<string>(colCount);
            for (int c = 1; c <= colCount; c++) {
                headers.Add(sheet.Cell(headerRow, c).GetString());
            }
            return (headers, headerRow);
        }

        private static bool IsRowEmpty(IXLWorksheet sheet, int rowNum) {
            var row = sheet.Row(rowNum);
            return row.CellsUsed().All(c => c.IsEmpty() || string.IsNullOrEmpty(c.GetString()));
        }
    }
}
