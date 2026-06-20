# 永茂泰（YMT）厂商 — 日聚合导出实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 新增 YMT 厂商工作台 + 日聚合导出（每天每任务一个文件，多表头空行分隔）

**Architecture:** 基类 `AWorkplaceContentPanel` 抽取 1 行 virtual 方法；`WorkplaceContentPanel_YMT` 继承 STANDARD `WorkplaceContentPanel`，仅 override `ExportDataAsync` 路由到 `YmtDataExportService`；基类 `AVariableSettingsView` 抽取 1 行 virtual 方法；`VariableSettingsView_YMT` 继承标准版，override `ExportTestAsync` 路由到 `YmtDataExportService`；后者按 `yyyy-MM/MissionName-yyyy-MM-dd.xlsx(txt)` 路径执行追加式写入，per-file SemaphoreSlim 防并发冲突

**Tech Stack:** C# WinForms, ClosedXML, .NET 8

## Global Constraints

- MySQL 5.7 兼容（不涉及 SQL）
- 不改动 `DataExportService`、`ExportRequest`、`ExportConfig`
- XAIA（西艾爱）/ SCII_XT 零改动
- 所有其他厂商零改动
- AppVersion.YMT = 7 已存在

---

### Task 1: 基类抽取 `ExportDataAsync` virtual 方法

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs` — 2 处

**Interfaces:**
- Produces: `protected virtual async Task ExportDataAsync(ExportRequest request)` — 默认调用 `DataExportService.ExportAsync`

- [ ] **Step 1: 修改 `OnMissionCompleted` 中的导出调用**

将 line 2783:
```csharp
                await new DataExportService().ExportAsync(request);
```
改为:
```csharp
                await ExportDataAsync(request);
```

- [ ] **Step 2: 在 `AWorkplaceContentPanel` 类中添加 virtual 方法**

在 `OnMissionCompleted` 方法之后（约 line 2793，`RefreshTighteningDataPanel` 之前）插入:

```csharp
        protected virtual async Task ExportDataAsync(ExportRequest request) {
            await new DataExportService().ExportAsync(request);
        }
```

- [ ] **Step 3: 构建验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

预期: Build succeeded，无错误。

---

### Task 2: 创建 `YmtDataExportService`

**Files:**
- Create: `OperationGuidance_new/Utils/YmtDataExportService.cs`

**Interfaces:**
- Consumes: `ExportRequest` (from `DataExportService.cs:7`), `MainUtils.GetCachedOperationDataVOPropInfos()` (internal), `MainUtils.GetLogger(Type)` (static)
- Produces: `YmtDataExportService.ExportAsync(ExportRequest)` — public 实例方法

- [ ] **Step 1: 创建文件并写完整实现**

```csharp
using ClosedXML.Excel;
using log4net;
using OperationGuidance_new.ViewObjects;
using System.Collections.Concurrent;
using System.Text;
using System.IO;

namespace OperationGuidance_new.Utils {
    public class YmtDataExportService {
        private static readonly ILog _logger = MainUtils.GetLogger(typeof(YmtDataExportService));
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new();

        public async Task ExportAsync(ExportRequest request) {
            var data = request.Data ?? new List<OperationDataVO>();
            string mission = string.IsNullOrEmpty(request.MissionName) ? "null" : request.MissionName;
            // 任务名中的文件名非法字符 → %XX 转义（保证可逆，永不冲突）
            mission = SanitizeFileName(mission);
            string yyyyMM = request.CompletedAt.ToString("yyyy-MM");
            string yyyyMMdd = request.CompletedAt.ToString("yyyy-MM-dd");
            string monthFolder = Path.Combine(request.BasePath, yyyyMM);
            string fileNameBody = $"{mission}-{yyyyMMdd}";

            var propertyNames = request.Fields.Where(f => f.Visible).Select(f => f.PropertyName).ToList();
            var headers = request.Fields.Where(f => f.Visible).Select(f => f.FieldName).ToList();

            if (propertyNames.Count == 0) {
                _logger.Warn("[YMT Export] No visible fields configured");
            }

            var rows = BuildRows(data, propertyNames);

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
```

添加必要的 using；文件顶部还需:
```csharp
using System.Collections.Concurrent;
```

- [ ] **Step 2: 构建验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

预期: Build succeeded。若有编译错误则修复。

---

### Task 3: 创建 `WorkplaceMissionView_YMT.cs`

**Files:**
- Create: `OperationGuidance_new/Views/WorkplaceMissionView_YMT.cs`

**Interfaces:**
- Consumes: `AWorkplaceMissionView<T,V>`, `WorkplaceTopBar`, `WorkplaceContentPanel` (STANDARD, 作为基类), `YmtDataExportService`
- Produces: `WorkplaceMissionView_YMT` (view), `WorkplaceContentPanel_YMT` (content panel，继承 `WorkplaceContentPanel`)

- [ ] **Step 1: 创建文件**

```csharp
using CustomLibrary.Configs;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.ReusableWidgets;

namespace OperationGuidance_new.Views {
    public class WorkplaceMissionView_YMT: AWorkplaceMissionView<WorkplaceContentPanel_YMT, WorkplaceTopBar> {
        public WorkplaceMissionView_YMT() { }
        public WorkplaceMissionView_YMT(bool operatorOpenning) : base(operatorOpenning) { }

        protected override WorkplaceContentPanel_YMT GetWrokplacePanel(int? missionId, WorkplaceTopBar topBar) {
            return new(missionId, missionName => {
                topBar.Title = missionName;
            }) {
                BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND_2,
                Margin = new Padding(0),
            };
        }
    }

    public class WorkplaceContentPanel_YMT: WorkplaceContentPanel {
        public WorkplaceContentPanel_YMT() { }
        public WorkplaceContentPanel_YMT(int? missionId, Action<string> resetMissionName) : base(missionId, resetMissionName) { }

        // YMT 路由到日聚合导出；导出开关及 BasePath/SortConfig 继承自 WorkplaceContentPanel（读 ExportConfig）
        protected override async Task ExportDataAsync(ExportRequest request) {
            await new YmtDataExportService().ExportAsync(request);
        }
    }
}
```

- [ ] **Step 2: 构建验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

预期: Build succeeded。若有编译错误则修复。

---

### Task 4: 注册 YMT 到系统菜单

**Files:**
- Modify: `OperationGuidance_new/Configs/SystemConfigs.cs`

- [ ] **Step 1: 添加 YMT 工作台注册**

在 "工作台" (id: 200) 的 `ViewTypes` 字典中添加一行：

```csharp
// 约 line 38，在 TZYX 之后添加:
                    {AppVersion.YMT, typeof(WorkplaceMissionView_YMT)},
```

- [ ] **Step 2: 添加 YMT 系统设置注册**

在 "系统设置" (id: 509) 的 `ViewTypes` 字典中添加一行：

```csharp
// 约 line 122，在 TZYX 之后添加:
                    {AppVersion.YMT, typeof(VariableSettingsView_YMT)},
```

完整 diff：
```diff
             // 工作台 (id: 200)
             ViewTypes = new() {
                 {AppVersion.STANDARD, typeof(WorkplaceMissionView)},
                 {AppVersion.SCII, typeof(WorkplaceMissionView_SCII)},
                 {AppVersion.YF, typeof(WorkplaceMissionView_YF)},
                 {AppVersion.GLB, typeof(WorkplaceMissionView_GLB)},
                 {AppVersion.WHYC, typeof(WorkplaceMissionView_WHYC)},
                 {AppVersion.TZYX, typeof(WorkplaceMissionView_TZYX)},
+                {AppVersion.YMT, typeof(WorkplaceMissionView_YMT)},
             },

             // 系统设置 (id: 509)
             ViewTypes = new() {
                 {AppVersion.STANDARD, typeof(VariableSettingsView)},
                 {AppVersion.YF, typeof(VariableSettingsView_YF)},
                 {AppVersion.SCII, typeof(VariableSettingsView_SCII)},
                 {AppVersion.GLB, typeof(VariableSettingsView_GLB)},
                 {AppVersion.WHYC, typeof(VariableSettingsView_WHYC)},
                 {AppVersion.TZYX, typeof(VariableSettingsView_TZYX)},
+                {AppVersion.YMT, typeof(VariableSettingsView_YMT)},
             },
```

- [ ] **Step 3: 构建验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

预期: Build succeeded。

---

### Task 5: 基类抽取 `ExportTestAsync` virtual + 创建 `VariableSettingsView_YMT`

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/AVariableSettingsView.cs` — 2 处
- Create: `OperationGuidance_new/Views/VariableSettingsView_YMT.cs`

**Interfaces:**
- Consumes: `AVariableSettingsView`, `VariableSettingsView`, `YmtDataExportService`
- Produces: `protected virtual Task ExportTestAsync(ExportRequest)` (base), `VariableSettingsView_YMT` (override)

- [ ] **Step 1: 修改 `RunExportTest` 中的导出调用**

在 `AVariableSettingsView.cs` line 529，将:
```csharp
                await new DataExportService().ExportAsync(request);
```
改为:
```csharp
                await ExportTestAsync(request);
```

- [ ] **Step 2: 在 `AVariableSettingsView` 中添加 virtual 方法**

在 `RunExportTest` 方法结束后添加:
```csharp
        protected virtual async Task ExportTestAsync(ExportRequest request) {
            await new DataExportService().ExportAsync(request);
        }
```

- [ ] **Step 3: 创建 `VariableSettingsView_YMT.cs`**

```csharp
using OperationGuidance_new.Utils;

namespace OperationGuidance_new.Views {
    public class VariableSettingsView_YMT: VariableSettingsView {
        protected override async Task ExportTestAsync(ExportRequest request) {
            await new YmtDataExportService().ExportAsync(request);
        }
    }
}
```

- [ ] **Step 4: 构建验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

预期: Build succeeded。

---

### Task 6: 全量构建 + 验证

- [ ] **Step 1: 全量构建**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

预期: Build succeeded，0 errors。

- [ ] **Step 2: 检查所有文件存在且未被意外修改**

```bash
git diff --stat
```

确认只有以下文件有变更：
- `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs` （≤ 5 行改动）
- `OperationGuidance_new/Views/AbstractViews/AVariableSettingsView.cs` （≤ 5 行改动）
- `OperationGuidance_new/Views/WorkplaceMissionView_YMT.cs` （新建）
- `OperationGuidance_new/Views/VariableSettingsView_YMT.cs` （新建）
- `OperationGuidance_new/Utils/YmtDataExportService.cs` （新建）
- `OperationGuidance_new/Configs/SystemConfigs.cs` （+2 行）

确认以下文件**未被修改**：
- `OperationGuidance_new/Utils/DataExportService.cs`
- 所有其他厂商的 `WorkplaceMissionView_*.cs`
- `OperationGuidance_new/Constants/AppVersion.cs`
