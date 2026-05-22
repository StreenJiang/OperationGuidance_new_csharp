# 前道工序链追溯码重码排除

**日期:** 2026-05-22  
**状态:** 已确认  
**分支:** v2.1.x

## 背景

`ABarCodeInputPopUpForm` 中的产品码重码校验在 2026-05-12 改为跨配方检查（`MissionId = null`），但遗漏了前道工序链场景：同一产品在工序链 A→B→C 中流转，每个工序使用同一个追溯码是正常流程，不应触发返工提示。

`predecessor_mission_id` 字段已建模工序前后关系，当前深度 < 10 层。

## 目标

产品码重码检查时，排除当前任务的所有前道工序记录，避免正常流转被误判为返工。

## 设计

### 1. 客户端构建前道工序链缓存

**位置:** `AWorkplaceContentPanel.GetBarCodeMatchingRules()`（约第 788 行）

该处已有 `QueryProductMissions` 调用，当前只取 `id` 字段用于清洗无效规则。改为保留完整 DTO，利用 `predecessor_mission_id` 构建全量链缓存。

**新字段:**
```csharp
protected Dictionary<int, HashSet<int>> _predecessorChains = new();
```

**构建逻辑（零额外 DB 查询）:**
```csharp
List<ProductMissionDTO> missions = apis.QueryProductMissions(...).ProductMissionsDTOs;

_predecessorChains.Clear();
var predecessorMap = missions.ToDictionary(m => m.id, m => m.predecessor_mission_id);
foreach (var m in missions) {
    var ancestors = new HashSet<int>();
    int? cursor = m.predecessor_mission_id;
    while (cursor != null && cursor > 0 && !ancestors.Contains(cursor.Value)) {
        ancestors.Add(cursor.Value);
        cursor = predecessorMap.ContainsKey(cursor.Value) ? predecessorMap[cursor.Value] : null;
    }
    _predecessorChains[m.id] = ancestors;
}
```

环检测：`!ancestors.Contains(cursor.Value)` 防止数据异常导致死循环。

**新公开方法:**
```csharp
public List<int> GetPredecessorChainMissionIds(int missionId) {
    return _predecessorChains.TryGetValue(missionId, out var ids) ? ids.ToList() : new();
}
```

### 2. API Request DTO

**文件:** `OperationGuidance_service/Models/Requests/CheckIfBarCodeExistsInMissionRecordReq.cs`

新增属性：
```csharp
public List<int>? ExcludeMissionIds { get; set; }
```

### 3. API SQL

**文件:** `OperationGuidance_service/Controllers/OperationGuidanceApis.cs`  
**方法:** `CheckIfBarCodeExistsInMissionRecord`（约第 936 行）

在现有 WHERE 条件之后拼接：
```csharp
if (req.ExcludeMissionIds != null && req.ExcludeMissionIds.Count > 0) {
    sql += $" and mission_id not in ({string.Join(",", req.ExcludeMissionIds)})";
}
```

注意：`ExcludeMissionIds` 由内部数据（predecessor_mission_id 链）生成，不存在 SQL 注入风险。

### 4. 调用点

**文件:** `OperationGuidance_new/Views/AbstractViews/ABarCodeInputPopUpForm.cs`  
**位置:** `ValidateProductBarCodeAsync` 方法第 322 行

```csharp
// 改前：
_workplace.Apis.CheckIfBarCodeExistsInMissionRecord(
    new(null) { ProductBarCode = barCode }).Yes

// 改后：
var redoReq = new CheckIfBarCodeExistsInMissionRecordReq(null) {
    ProductBarCode = barCode,
    ExcludeMissionIds = _workplace.GetPredecessorChainMissionIds(_mission.id),
};
_workplace.Apis.CheckIfBarCodeExistsInMissionRecord(redoReq).Yes
```

### 5. 不改动

- 物料码重码检查（`CheckPartsBarCode`）保持现状。`predecessor_part_mission_ids` 建模方式不同，需单独评估
- 前置任务完成检查（`predecessor_mission_id` + `MissionResult = OK`）逻辑不变
- 返工确认流程不变
- 挑战任务逻辑不变

## 涉及文件

| 文件 | 改动类型 |
|---|---|
| `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs` | +字段、+方法、`GetBarCodeMatchingRules` 中构建缓存 |
| `OperationGuidance_service/Models/Requests/CheckIfBarCodeExistsInMissionRecordReq.cs` | +`ExcludeMissionIds` 属性 |
| `OperationGuidance_service/Controllers/OperationGuidanceApis.cs` | SQL 拼接 NOT IN |
| `OperationGuidance_new/Views/AbstractViews/ABarCodeInputPopUpForm.cs` | 第 322 行传入排除列表 |
