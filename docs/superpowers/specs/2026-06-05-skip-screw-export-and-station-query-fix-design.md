# 跳过螺丝点位文件夹 + 站点查询兼容修复

**日期：** 2026-06-05
**版本：** v1.6.x

## 背景

### 跳过螺丝点位功能

`skip_screw_points = YES` 的任务激活后直接跳过所有螺栓拧紧，创建一条 `mission_result = OK` 的 `mission_record` 后立即终止。该流程在 `WorkplaceMissionView_SCII.ActivateMission`（line 1162-1201）中实现。

### 当前问题

1. **文件夹未创建**：跳过螺丝点位任务完成时，`OnMissionCompleted` → `DataExportService.ExportAsync` 因为没有任何拧紧数据而提前 return，`Directory.CreateDirectory` 从未执行
2. **站点查询有遗漏**：`OperationDataService` 的两个聚合方法只查 `operation_data` 表。跳过螺丝点位的 `mission_record` 无对应的拧紧数据行，站点维度查询时被遗漏

## 问题 1：空数据时不创建文件夹且不写文件

### 根因

`DataExportService.ExportAsync`（line 26-28）在 `data.Count == 0` 时直接 return，导致：

1. `Directory.CreateDirectory(batchFolder)` 从未执行——跳过螺丝点位的任务连文件夹都没有
2. 所有文件写入逻辑被跳过——跳过螺丝点位的任务没有 .xlsx 文件

### 需求

每个任务（不论是否有拧紧数据）都必须产出：
- 一个完整的文件夹层级 `工作站/任务名/日期/批次/`
- 一个 `.xlsx` 文件（若开启），带完整表头，数据行为空
- 一个 `.txt` 文件（若开启），带完整表头，数据行为空

### 修复

**文件：** `OperationGuidance_new/Utils/DataExportService.cs` — `ExportAsync` 方法

将路径拼装和 `Directory.CreateDirectory` 移到前面，**移除 `data.Count == 0` 的提前 return**，确保 0 行数据时文件照写：

```csharp
// 之后
public async Task ExportAsync(ExportRequest request) {
    var data = request.Data ?? new List<OperationDataVO>();
    string workstation = string.IsNullOrEmpty(request.WorkstationName) ? "null" : request.WorkstationName;
    string mission = string.IsNullOrEmpty(request.MissionName) ? "null" : request.MissionName;
    string date = request.CompletedAt.ToString("yyyy-MM-dd");
    string batch = string.IsNullOrEmpty(request.ProductBatch) ? "null" : request.ProductBatch;
    string batchFolder = Path.Combine(request.BasePath, workstation, mission, date, batch);
    string barCode = string.IsNullOrEmpty(request.ProductBarCode) ? "null" : request.ProductBarCode;
    string timestamp = request.CompletedAt.ToString("yyyyMMdd_HHmmss");
    string fileNameBody = $"{barCode}_{timestamp}_{request.Result}";

    // 目录总是创建
    try {
        Directory.CreateDirectory(batchFolder);
    } catch (Exception ex) {
        _logger.Error($"[DataExport] Failed to create directory: {batchFolder}", ex);
        throw new IOException($"无法创建导出目录: {batchFolder}", ex);
    }

    // 无论是否有拧紧数据，都生成文件（0 行数据 = 只有表头的空文件）
    var propertyNames = request.Fields.Where(f => f.Visible).Select(f => f.PropertyName).ToList();
    var headers = request.Fields.Where(f => f.Visible).Select(f => f.FieldName).ToList();
    if (propertyNames.Count == 0) {
        _logger.Warn("[DataExport] No visible fields configured — export may produce empty columns");
    }

    var rows = BuildRows(data, propertyNames);
    _logger.Info($"[DataExport] Exporting {rows.Count} rows x {propertyNames.Count} cols to {batchFolder}");

    // ... 文件写入逻辑保持不变 ...
}
```

> `rows` 在 `data.Count == 0` 时为空列表 `[]`，`BuildRows` 直接返回空列表，`WriteExcelAsync` / `WriteTxtAsync` 照常写表头行。

## 问题 2：站点查询兼容 mission_record

### 根因

`OperationDataService` 的两个聚合方法只查询 `operation_data` 表：

```csharp
// GetMissionRecordIdsByWorkstationIds — 只查 operation_data
select mission_record_id, workstation_id from operation_data
where workstation_id in @ids group by mission_record_id, workstation_id

// GetWorkstationInfoByMissionRecordIds — 只查 operation_data
select distinct(mission_record_id), workstation_id from operation_data
where mission_record_id in @ids
```

`mission_record` 表已具备 `workstation_id` 和 `workstation_name` 字段，但未被利用。

### 修复

**文件：** `OperationGuidance_service/Controllers/OperationGuidanceApis.cs`

双源合并逻辑在控制器层实现——`OperationGuidanceApis` 已通过 `[Autowired]` 持有 `_operationDataService` 和 `_missionRecordService`，无需新增注入。`OperationDataService` 的原有方法保持不变。

#### 2a. `QueryMissionRecordsByWorkstationIds`（line 857-863）

```csharp
// 之后
public QueryMissionRecordsByWorkstationIdsRsp QueryMissionRecordsByWorkstationIds(QueryMissionRecordsByWorkstationIdsReq req) {
    var result = new Dictionary<int, List<int>>();
    if (req.WorkstationIds.Count > 0) {
        // 源1: operation_data — 有拧紧数据的任务（向后兼容）
        result = _operationDataService.GetMissionRecordIdsByWorkstationIds(req.WorkstationIds);

        // 源2: mission_record — 跳过螺丝点位等无拧紧数据的任务
        string sqlMr = $"select id, workstation_id from {_missionRecordService.TableName} " +
                       "where workstation_id in @ids and deleted = @deleted";
        var mrList = _missionRecordService.FindBySql(sqlMr,
            new() { { "ids", req.WorkstationIds }, { "deleted", (int)YesOrNo.NO } });

        foreach (var mr in mrList) {
            if (mr.workstation_id != null) {
                if (!result.TryGetValue(mr.workstation_id.Value, out var list)) {
                    result[mr.workstation_id.Value] = new() { mr.id };
                } else if (!list.Contains(mr.id)) {
                    list.Add(mr.id);
                }
            }
        }
    }
    return new(result);
}
```

#### 2b. `QueryWorkstationInfoByMissionRecordIds`（line 684-704）

```csharp
// 之后
public QueryWorkstationInfoByMissionRecordIdsRsp QueryWorkstationInfoByMissionRecordIds(QueryWorkstationInfoByMissionRecordIdsReq req) {
    Dictionary<int, Dictionary<int, string>> workstationInfos = new();
    if (req.MissionRecordIds.Count > 0) {
        // 源1: operation_data — 有拧紧数据的记录（向后兼容）
        workstationInfos = _operationDataService.GetWorkstationInfoByMissionRecordIds(req.MissionRecordIds);

        // 源2: mission_record — 跳过螺丝点位等无拧紧数据的记录
        string sqlMr = $"select id, workstation_id from {_missionRecordService.TableName} " +
                       "where id in @ids and workstation_id is not null and deleted = @deleted";
        var mrList = _missionRecordService.FindBySql(sqlMr,
            new() { { "ids", req.MissionRecordIds }, { "deleted", (int)YesOrNo.NO } });

        foreach (var mr in mrList) {
            if (mr.workstation_id != null) {
                if (!workstationInfos.TryGetValue(mr.id, out var inner)) {
                    workstationInfos[mr.id] = new() { { mr.workstation_id.Value, "" } };
                } else if (!inner.ContainsKey(mr.workstation_id.Value)) {
                    inner[mr.workstation_id.Value] = "";
                }
            }
        }

        // 根据所有 workstation_ids 查询到每个 id 对应的 name（后续逻辑不变）
        List<int> workstationIds = new();
        workstationInfos.Values.ToList().ForEach(dict => workstationIds.AddRange(dict.Keys));
        Dictionary<int, string> workstationInfo = _workstationService.GetWorkstationNamesByIds(workstationIds);
        foreach (var dict in workstationInfos.Values) {
            foreach (var pair in dict) {
                if (workstationInfo.ContainsKey(pair.Key)) {
                    dict[pair.Key] = workstationInfo[pair.Key];
                }
            }
        }
    }

    return new(workstationInfos);
}
```

### 性能分析

- 两条查询各自用 `workstation_id in @ids` / `id in @ids` 过滤，结果集受限于单站点下的记录数
- C# 侧 `Dictionary.TryGetValue` 去重是 O(n) 内存操作
- 相比 SQL `NOT IN (SELECT DISTINCT ...)` 子查询扫描全表，此方案数据库开销为零增量
- 控制器层已持有两个 Service 引用，不需要额外的依赖注入

## 不改的部分

| 项 | 理由 |
|---|---|
| `QueryOperationDataList` | 返回拧紧数据明细行，跳过螺丝点位无可展示的拧紧数据，合理 |
| `DataQueryView.RefreshWorkstationOptions` | 仅列出所有站点，不涉及数据过滤 |
| `TerminateMission` / `ActivateMission` 流程 | 跳过螺丝链路已正确创建 mission_record → FINISHED_OK → export |

## 风险评估

- **低风险**：两处改动范围局限、逻辑独立
- 问题 1 只改变函数体内顺序，不影响已有调用方
- 问题 2 新增查询源，不修改原有查询逻辑，向后兼容
