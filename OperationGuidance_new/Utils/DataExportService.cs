using ClosedXML.Excel;
using log4net;
using OperationGuidance_new.Configs;
using OperationGuidance_new.ViewObjects;

namespace OperationGuidance_new.Utils {
    public class ExportRequest {
        public List<OperationDataVO> Data { get; init; }
        public List<OperationDataField> Fields { get; init; }
        public string BasePath { get; init; }
        public string ProductBatch { get; init; }
        public string ProductBarCode { get; init; }
        public DateTime CompletedAt { get; init; }
        public string Result { get; init; }
        public bool EnableExcel { get; init; }
        public bool EnableTxt { get; init; }
    }

    public class DataExportService {
        private static readonly ILog _logger = MainUtils.GetLogger(typeof(DataExportService));

        public async Task ExportAsync(ExportRequest request) {
            if (request.Data == null || request.Data.Count == 0) {
                _logger.Warn("[DataExport] ExportAsync skipped: no data");
                return;
            }

            string dateFolder = Path.Combine(request.BasePath, request.CompletedAt.ToString("yyyy-MM-dd"));
            string batch = string.IsNullOrEmpty(request.ProductBatch) ? "null" : request.ProductBatch;
            string batchFolder = Path.Combine(dateFolder, batch);
            string barCode = string.IsNullOrEmpty(request.ProductBarCode) ? "null" : request.ProductBarCode;
            string timestamp = request.CompletedAt.ToString("yyyyMMdd_HHmmss");
            string fileNameBody = $"{barCode}（{barCode}）_{timestamp}_{request.Result}";

            try {
                Directory.CreateDirectory(batchFolder);
            } catch (Exception ex) {
                _logger.Error($"[DataExport] Failed to create directory: {batchFolder}", ex);
                throw new IOException($"无法创建导出目录: {batchFolder}", ex);
            }

            var propertyNames = request.Fields.Where(f => f.Visible).Select(f => f.PropertyName).ToList();
            var headers = request.Fields.Where(f => f.Visible).Select(f => f.FieldName).ToList();
            if (propertyNames.Count == 0) {
                _logger.Warn("[DataExport] No visible fields configured — export may produce empty columns");
            }

            var rows = BuildRows(request.Data, propertyNames);
            _logger.Info($"[DataExport] Exporting {rows.Count} rows x {propertyNames.Count} cols to {batchFolder}");

            var exceptions = new List<Exception>();
            if (request.EnableExcel) {
                try {
                    await WriteExcelAsync(batchFolder, fileNameBody, headers, rows);
                    _logger.Info($"[DataExport] Excel written: {Path.Combine(batchFolder, fileNameBody + ".xlsx")}");
                } catch (Exception ex) {
                    _logger.Error($"[DataExport] Excel write failed", ex);
                    exceptions.Add(new IOException($"Excel导出失败: {ex.Message}", ex));
                }
            }
            if (request.EnableTxt) {
                try {
                    await WriteTxtAsync(batchFolder, fileNameBody, headers, rows);
                    _logger.Info($"[DataExport] Txt written: {Path.Combine(batchFolder, fileNameBody + ".txt")}");
                } catch (Exception ex) {
                    _logger.Error($"[DataExport] Txt write failed", ex);
                    exceptions.Add(new IOException($"Txt导出失败: {ex.Message}", ex));
                }
            }
            if (exceptions.Count == 1) throw exceptions[0];
            if (exceptions.Count > 1) throw new AggregateException("导出过程中发生错误", exceptions);
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

        private Task WriteExcelAsync(string folder, string fileNameBody, List<string> headers, List<List<object?>> rows) {
            string filePath = Path.Combine(folder, $"{fileNameBody}.xlsx");
            using (var wb = new XLWorkbook()) {
                var sheet = wb.Worksheets.Add("TighteningData");
                if (headers.Count > 0) {
                    sheet.Cell(1, 1).InsertData(new List<List<string>> { headers });
                    sheet.Cell(2, 1).InsertData(rows);
                } else {
                    sheet.Cell(1, 1).InsertData(rows);
                }
                wb.SaveAs(filePath);
            }
            return Task.CompletedTask;
        }

        private Task WriteTxtAsync(string folder, string fileNameBody, List<string> headers, List<List<object?>> rows) {
            string filePath = Path.Combine(folder, $"{fileNameBody}.txt");
            using (var sw = new StreamWriter(filePath, false)) {
                if (headers.Count > 0) {
                    sw.WriteLine(string.Join("\t", headers));
                }
                foreach (var row in rows) sw.WriteLine(string.Join("\t", row));
            }
            return Task.CompletedTask;
        }
    }
}
