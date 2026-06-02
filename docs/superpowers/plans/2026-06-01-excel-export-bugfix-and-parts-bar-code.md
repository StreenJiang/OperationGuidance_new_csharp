# Excel 导出：Bug 修复 + parts_bar_code + 目录层级 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复导出文件名错误和追溯码 null bug，新增 parts_bar_code 字段到数据链路，重构导出目录层级为 站点/任务/日期/批次

**Architecture:** 数据链路逐层追加 `parts_bar_code` 属性（Entity→DTO→VO），API 层 LEFT JOIN 补齐，ORM 层加 `[NotMapped]` 过滤防止写入不存在的列。导出目录通过 `ExportRequest` 扩展字段传入站点/任务名

**Tech Stack:** C# WinForms, Dapper, ClosedXML, SQLite/MySQL

---

### Task 1: `GetFiedsList` 跳过 `[NotMapped]` 属性

**Files:**
- Modify: `OperationGuidance_service/Wrapper/AbstractClasses/AWrapperBase.cs:244-259`

- [ ] **Step 1: 修改 `GetFiedsList` 方法，跳过标记了 `[NotMapped]` 的属性**

`System.ComponentModel.DataAnnotations.Schema` 已在文件头部引用（第9行），直接修改方法体：

```csharp
private List<string> GetFiedsList() {
    List<string> fields = new();
    foreach (PropertyInfo property in typeof(T).GetProperties()) {
        // 跳过 [NotMapped] 属性，防止将非物理列写入 INSERT/UPDATE SQL
        var notMapped = property.GetCustomAttribute<NotMappedAttribute>();
        if (notMapped != null) continue;

        string fieldsName = property.Name;
        foreach (Attribute attribute in property.GetCustomAttributes()) {
            if (attribute is ColumnAttribute) {
                string? name = ((ColumnAttribute) attribute).Name;
                if (name != null) {
                    fieldsName = name;
                }
            }
        }
        fields.Add(fieldsName);
    }
    return fields;
}
```

- [ ] **Step 2: 编译验证**

```bash
dotnet build OperationGuidance_service/OperationGuidance_service.csproj
```

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_service/Wrapper/AbstractClasses/AWrapperBase.cs
git commit -m "feat(orm): skip [NotMapped] properties in GetFiedsList to support non-physical columns"
```

---

### Task 2: `OperationData` 实体加 `parts_bar_code`

**Files:**
- Modify: `OperationGuidance_service/Models/OperationData.cs:80`

- [ ] **Step 1: 在 `result_type` 属性之后、类结束之前添加 `[NotMapped]` 属性**

```csharp
        public int? result_type { get; set; }                                               //
        [NotMapped]
        public string? parts_bar_code { get; set; }                                          // 物料码（来自 mission_record JOIN，非 operationdata 物理列）
    }
}
```

`NotMappedAttribute` 来自 `System.ComponentModel.DataAnnotations.Schema`，需在文件头部确认或添加：
```csharp
using System.ComponentModel.DataAnnotations.Schema;
```

- [ ] **Step 2: 编译验证**

```bash
dotnet build OperationGuidance_service/OperationGuidance_service.csproj
```

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_service/Models/OperationData.cs
git commit -m "feat(operationdata): add [NotMapped] parts_bar_code for mission_record JOIN"
```

---

### Task 3: `OperationDataDTO` 加 `parts_bar_code`

**Files:**
- Modify: `OperationGuidance_service/Models/DTOs/OperationDataDTO.cs:78`

- [ ] **Step 1: 在 `result_type` 之后添加属性**

```csharp
        public int? result_type { get; set; }                                               //
        public string? parts_bar_code { get; set; }                                          // 物料码（来自 mission_record JOIN）
    }
}
```

- [ ] **Step 2: 编译验证**

```bash
dotnet build OperationGuidance_service/OperationGuidance_service.csproj
```

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_service/Models/DTOs/OperationDataDTO.cs
git commit -m "feat(dto): add parts_bar_code to OperationDataDTO"
```

---

### Task 4: `OperationDataVO` 加 `[GridColumn] parts_bar_code`

**Files:**
- Modify: `OperationGuidance_new/ViewObjects/OperationDataVO.cs:234`

- [ ] **Step 1: 在 `mission_record_id` 下方添加带 `[GridColumn]` 的属性**

```csharp
        public int? mission_record_id { get; set; }                                         // 任务记录ID
        [GridColumn("物料码")]
        public string? parts_bar_code { get; set; }                                          // 物料码（来自 mission_record JOIN）
```

- [ ] **Step 2: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/ViewObjects/OperationDataVO.cs
git commit -m "feat(vo): add [GridColumn] parts_bar_code to OperationDataVO"
```

---

### Task 5: API 查询加 LEFT JOIN

**Files:**
- Modify: `OperationGuidance_service/Controllers/OperationGuidanceApis.cs:851-881`

- [ ] **Step 1: 用子查询方式加 LEFT JOIN，condition / countSql / 分页全部不动**

将第885行：
```csharp
List<OperationData> operationDatas = _operationDataService.FindBySql(sql + condition, parameters);
```
改为（子查询外包 LEFT JOIN，`sql + condition` 作为子查询内容）：
```csharp
string joinedSql = $"select o.*, m.parts_bar_code from ({sql} {condition}) o left join {_missionRecordService.TableName} m on o.mission_record_id = m.id";
List<OperationData> operationDatas = _operationDataService.FindBySql(joinedSql, parameters);
```

`sql`、`condition`、`countSql`、分页 clause — **全部不改动**。`_missionRecordService` 已在控制器第49行注入。

- [ ] **Step 2: 编译验证**

```bash
dotnet build OperationGuidance_service/OperationGuidance_service.csproj
```

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_service/Controllers/OperationGuidanceApis.cs
git commit -m "feat(api): add LEFT JOIN mission_record for parts_bar_code in QueryOperationDataList"
```

---

### Task 6: `ExportRequest` 加 `MissionName` / `WorkstationName`

**Files:**
- Modify: `OperationGuidance_new/Utils/DataExportService.cs:7-17`

- [ ] **Step 1: 在 `ExportRequest` 类中添加两个属性**

```csharp
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
    public string MissionName { get; init; }        // 新增：任务名（目录层级用）
    public string WorkstationName { get; init; }    // 新增：站点名（目录层级用）
}
```

- [ ] **Step 2: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Utils/DataExportService.cs
git commit -m "feat(export): add MissionName/WorkstationName to ExportRequest"
```

---

### Task 7: `ExportAsync` — 文件名修正 + 目录层级重构

**Files:**
- Modify: `OperationGuidance_new/Utils/DataExportService.cs:22-40`

- [ ] **Step 1: 修改 `ExportAsync` 的目录构建和文件名逻辑**

将第28-33行：
```csharp
string dateFolder = Path.Combine(request.BasePath, request.CompletedAt.ToString("yyyy-MM-dd"));
string batch = string.IsNullOrEmpty(request.ProductBatch) ? "null" : request.ProductBatch;
string batchFolder = Path.Combine(dateFolder, batch);
string barCode = string.IsNullOrEmpty(request.ProductBarCode) ? "null" : request.ProductBarCode;
string timestamp = request.CompletedAt.ToString("yyyyMMdd_HHmmss");
string fileNameBody = $"{barCode}（{barCode}）_{timestamp}_{request.Result}";
```

改为：
```csharp
string workstation = string.IsNullOrEmpty(request.WorkstationName) ? "null" : request.WorkstationName;
string mission = string.IsNullOrEmpty(request.MissionName) ? "null" : request.MissionName;
string date = request.CompletedAt.ToString("yyyy-MM-dd");
string batch = string.IsNullOrEmpty(request.ProductBatch) ? "null" : request.ProductBatch;
string batchFolder = Path.Combine(request.BasePath, workstation, mission, date, batch);
string barCode = string.IsNullOrEmpty(request.ProductBarCode) ? "null" : request.ProductBarCode;
string timestamp = request.CompletedAt.ToString("yyyyMMdd_HHmmss");
string fileNameBody = $"{barCode}_{timestamp}_{request.Result}";
```

- [ ] **Step 2: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Utils/DataExportService.cs
git commit -m "fix(export): correct file name format and restructure directory hierarchy to workstation/mission/date/batch"
```

---

### Task 8: `AWorkplaceContentPanel` — Bug2 修复 + parts_bar_code 回填 + 层级字段传入

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs:2699-2707`

- [ ] **Step 1: 修改 `OnMissionCompleted` 中 `ExportRequest` 的构造**

当前代码（第2693-2707行）：
```csharp
var snapshot = GetTighteningDataSnapshot();
if (snapshot.Count == 0) return;

string result = status == WorkplaceProcessStatus.FINISHED_OK ? "OK" : "NG";
var fields = MainUtils.GetOperationDataFields(ExportSortConfig);

var request = new ExportRequest {
    Data = snapshot, Fields = fields, BasePath = ExportBasePath,
    ProductBatch = _missionRecord?.product_batch,
    ProductBarCode = _barCodeObj.ProductBarCode,
    CompletedAt = DateTime.Now, Result = result,
    EnableExcel = IsExcelExportEnabled, EnableTxt = IsTxtExportEnabled,
};
```

改为：
```csharp
var snapshot = GetTighteningDataSnapshot();
if (snapshot.Count == 0) return;

string result = status == WorkplaceProcessStatus.FINISHED_OK ? "OK" : "NG";
var fields = MainUtils.GetOperationDataFields(ExportSortConfig);

// 回填 parts_bar_code 到每个 VO（同一批次所有行共享同一个物料码）
if (_missionRecord?.parts_bar_code != null) {
    foreach (var vo in snapshot) {
        vo.parts_bar_code = _missionRecord.parts_bar_code;
    }
}

var request = new ExportRequest {
    Data = snapshot, Fields = fields, BasePath = ExportBasePath,
    ProductBatch = _missionRecord?.product_batch,
    ProductBarCode = _missionRecord?.product_bar_code,
    CompletedAt = DateTime.Now, Result = result,
    EnableExcel = IsExcelExportEnabled, EnableTxt = IsTxtExportEnabled,
    MissionName = _mission?.name,
    WorkstationName = snapshot[0].workstation_name,
};
```

- [ ] **Step 2: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs
git commit -m "fix(export): read ProductBarCode from _missionRecord, backfill parts_bar_code, pass workstation/mission for directory hierarchy"
```

---

### Task 8b: `AVariableSettingsView` — 测试导出同步层级字段

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/AVariableSettingsView.cs:509-519`

- [ ] **Step 1: 测试导出的 `ExportRequest` 加 `MissionName`/`WorkstationName`**

第509-519行改为：
```csharp
var request = new ExportRequest {
    Data = fakeData,
    Fields = fields,
    BasePath = ExportConfig.Instance.StoragePath,
    ProductBatch = "TEST_BATCH",
    ProductBarCode = "TEST_BARCODE",
    CompletedAt = DateTime.Now,
    Result = "OK",
    EnableExcel = enableExcel,
    EnableTxt = enableTxt,
    MissionName = "TEST_MISSION",
    WorkstationName = "TEST_WORKSTATION",
};
```

- [ ] **Step 2: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/AbstractViews/AVariableSettingsView.cs
git commit -m "fix(export): add MissionName/WorkstationName to test export in settings"
```

---

### Task 9: 全量编译 + 验证

- [ ] **Step 1: 全量编译**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

确认 0 errors。

- [ ] **Step 2: 验证清单**

| # | 验证项 | 方法 |
|---|---|---|
| 1 | 文件名格式 | 导出后检查文件名是否为 `追溯码_时间_结果.xlsx` |
| 2 | 追溯码非 null | 扫描条码后完成任务，文件名追溯码不为 "null" |
| 3 | 目录层级 | 导出路径为 `{base}/{workstation}/{mission}/{date}/{batch}/` |
| 4 | parts_bar_code 列 | Excel 中包含"物料码"列且有值 |
| 5 | 字段配置界面 | 变量设置中"物料码"出现在字段列表，可勾选/排序 |
| 6 | DataQueryView | 数据查询界面显示物料码列 |

- [ ] **Step 3: Commit（如有微调）**

```bash
git add -A
git commit -m "chore: final verification adjustments"
```
