# Excel 导出：Bug 修复 + parts_bar_code 字段扩展 + 目录层级重构

## 背景

两个 bug + 一个字段扩展 + 一个目录层级调整。

## Bug 1：文件名格式错误

**文件：** `OperationGuidance_new/Utils/DataExportService.cs:33`

**现状：** `$"{barCode}（{barCode}）_{timestamp}_{request.Result}"` — `barCode` 用了两次。

**修正：** `$"{barCode}_{timestamp}_{request.Result}"`

## Bug 2：追溯码正常情况下为 null

**根因：** `TerminateMission` 中 `_barCodeObj.Reset()`（第2803行）在 `OnMissionCompleted`（第2807行）之前调用，`ProductBarCode` 已清空。

**修正：** `ProductBarCode` 和 `ProductBatch` 统一从 `_missionRecord` 读取。

```csharp
ProductBarCode = _missionRecord?.product_bar_code,
ProductBatch  = _missionRecord?.product_batch,
```

## 需求 1：parts_bar_code 可配置导出

`parts_bar_code` 来自 `mission_record` 表，需纳入字段配置体系，同时在 DataQueryView 中展示。

## 需求 2：导出目录层级重构

当前层级与目标层级：

```
现: {BasePath}/{yyyy-MM-dd}/{batch}/{追溯码}_时间_结果.xlsx
新: {BasePath}/{workstation}/{mission}/{yyyy-MM-dd}/{batch}/{追溯码}_时间_结果.xlsx
```

---

## 设计方案

### 核心思路

- `parts_bar_code`：利用现有 `[GridColumn]` → `_fieldMetaCache` 反射机制，数据链路各层追加属性，API 层 LEFT JOIN 补齐
- `[NotMapped]`：`OperationData` 实体也被 INSERT/UPDATE 使用，`GetFiedsList` 反射所有属性生成 SQL。需让 `GetFiedsList` 跳过 `[NotMapped]` 属性，防止写入不存在的列
- 目录层级：`ExportRequest` 加 `MissionName`/`WorkstationName`，`ExportAsync` 重构目录构建逻辑

### 改动清单（共 11 处）

| # | 层级 | 文件 | 改动 |
|---|---|---|---|
| 1 | 实体 | `Models/OperationData.cs` | 加 `[NotMapped] string? parts_bar_code` |
| 1b | ORM | `Wrapper/AbstractClasses/AWrapperBase.cs` | `GetFiedsList` 跳过 `[NotMapped]` 属性 |
| 2 | DTO | `Models/DTOs/OperationDataDTO.cs` | 加 `string? parts_bar_code` |
| 3 | VO | `ViewObjects/OperationDataVO.cs` | 加 `[GridColumn("物料码")] string? parts_bar_code` |
| 4 | SQL | `Controllers/OperationGuidanceApis.cs` | `QueryOperationDataList`：主查询外包子查询 `select o.*, m.parts_bar_code from ({原sql}) o left join {mission_record} m on o.mission_record_id = m.id`，condition/countSql/分页均不改动 |
| 5 | Bug1 | `Utils/DataExportService.cs` | 文件名修正 + 目录层级重构 |
| 6 | Bug2+层级 | `Views/AbstractViews/AWorkplaceContentPanel.cs` | `ProductBarCode`/`ProductBatch` 改从 `_missionRecord` 读取 |
| 7 | 导出 | `Views/AbstractViews/AWorkplaceContentPanel.cs` | 导出前把 `_missionRecord?.parts_bar_code` 写入每个 VO |
| 8 | 层级 | `Utils/DataExportService.cs` | `ExportRequest` 加 `MissionName`/`WorkstationName` |
| 9 | 层级 | `Views/AbstractViews/AWorkplaceContentPanel.cs` | 构造 `ExportRequest` 时传入 `MissionName`/`WorkstationName` |
| 9b | 层级 | `Views/AbstractViews/AVariableSettingsView.cs` | 测试导出同步加 `MissionName`/`WorkstationName` |
| 10 | 层级 | `Utils/DataExportService.cs` | `ExportAsync` 目录构建改为 `{base}/{workstation}/{mission}/{date}/{batch}/` |

### 数据流

```
SQL LEFT JOIN → OperationData → OperationDataDTO → OperationDataVO（DataQueryView Grid）

导出：
OnMissionCompleted
  → snapshot 中每个 VO.parts_bar_code = _missionRecord?.parts_bar_code
  → ExportRequest {
      ProductBarCode  = _missionRecord?.product_bar_code
      ProductBatch    = _missionRecord?.product_batch
      MissionName     = _mission?.name
      WorkstationName = snapshot[0]?.workstation_name
    }
  → ExportAsync
      → 目录 = {base}/{workstation}/{mission}/{yyyy-MM-dd}/{batch}/
      → 文件 = {barCode}_{timestamp}_{result}.xlsx
```

### 目录构建伪代码

```csharp
string w = string.IsNullOrEmpty(request.WorkstationName) ? "null" : request.WorkstationName;
string m = string.IsNullOrEmpty(request.MissionName) ? "null" : request.MissionName;
string d = request.CompletedAt.ToString("yyyy-MM-dd");
string b = string.IsNullOrEmpty(request.ProductBatch) ? "null" : request.ProductBatch;

string exportDir = Path.Combine(request.BasePath, w, m, d, b);
Directory.CreateDirectory(exportDir);
```

### 错误处理

| 场景 | 行为 |
|---|---|
| 站点/任务/批次/追溯码为 null | fallback "null" |
| `_missionRecord` 为 null | 追溯码/物料码均为 null |
| `parts_bar_code` 不在 sortConfig | 不导出该列 |
| SQL LEFT JOIN 无匹配 | `parts_bar_code` 为 null |

---

## 测试验证

1. **正常导出**：扫描条码 → 完成任务 → 目录 `站点/任务/日期/批次/`，文件 `追溯码_时间_结果.xlsx`，含物料码列
2. **追溯码为 null**：不扫描条码直接完成任务 → 文件名追溯码为 "null"，目录正常创建
3. **字段配置**：变量设置界面 → 物料码出现在字段列表 → 可勾选/取消/排序
4. **数据查询**：DataQueryView → 物料码列正常显示
