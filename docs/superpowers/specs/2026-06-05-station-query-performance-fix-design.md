# 站点查询性能与数据完整性修复

**日期：** 2026-06-05
**版本：** v1.6.x

## 问题

`DataQueryView_SCII` 按站点查询存在两个问题：

1. **慢**：`FORCE INDEX (PRIMARY)` 强制全表扫描，`mission_record.workstation_id` 无索引
2. **数据严重缺失**：历史 `mission_record` 的 `workstation_id` 为 NULL（字段近期才新增），过滤 `mr.workstation_id = @id` 只能命中新记录

## 根因

- `FORCE INDEX (PRIMARY)` 原本是为 `mr.id in @ids` + `ORDER BY mr.id` 场景添加的优化（确保 MySQL 走主键顺序扫描）。但加上 `workstation_id` 过滤后，强制走主键变成全表扫描
- `mission_record.workstation_id` 无索引（该字段在 commit `5d474a9` 前后新增，历史数据均为 NULL），`WHERE workstation_id = @id` 只能命中字段填充后的新记录
- 历史记录通过 `operation_data.workstation_id` 关联站点，但新 SQL 不走这条路径

## 修复方案

### 改动 1：新建索引 `ix_mr_ws`

**文件（新建）：**
- `OperationGuidance_service/Database/sqls/modify_mysql_20260605.sql`
- `OperationGuidance_service/Database/sqls/modify_sqlite_20260605.sql`
- `OperationGuidance_service/Database/sqls/modify_sqlserver_20260605.sql`

```sql
-- MySQL
CREATE INDEX ix_mr_ws ON mission_record(workstation_id, deleted);

-- SQLite
CREATE INDEX ix_mr_ws ON mission_record(workstation_id, deleted);

-- SQLServer
CREATE INDEX ix_mr_ws ON mission_record(workstation_id, deleted);
```

> `ix_opdata_ws ON operation_data(workstation_id, mission_record_id)` 已在 `20260602` 迁移中创建——子查询零回表。

### 改动 2：`QueryMissionRecordList` — OR 双源 + 条件 FORCE INDEX

**文件：** `OperationGuidance_service/Controllers/OperationGuidanceApis.cs`

#### 2a. `fromClause` 条件化

```csharp
// 之前
bool isMysql = SystemUtils.GetDBTypes() == DBTypes.MYSQL;
string fromClause = isMysql
    ? $"{_missionRecordService.TableName} mr FORCE INDEX (PRIMARY)"
    : $"{_missionRecordService.TableName} mr";

// 之后
bool isMysql = SystemUtils.GetDBTypes() == DBTypes.MYSQL;
string fromClause;
if (isMysql && req.WorkstationId == null) {
    fromClause = $"{_missionRecordService.TableName} mr FORCE INDEX (PRIMARY)";
} else {
    fromClause = $"{_missionRecordService.TableName} mr";
}
```

#### 2b. `workstation_id` 过滤改为双源

```csharp
// 之前
if (req.WorkstationId != null) {
    condition += " and mr.workstation_id = @workstation_id";
    parameters.Add("workstation_id", req.WorkstationId.Value);
}

// 之后
if (req.WorkstationId != null) {
    condition += @" and (mr.workstation_id = @workstation_id
        or mr.id in (select mission_record_id from operation_data
                     where workstation_id = @ws_od_id and deleted = @ws_od_deleted))";
    parameters.Add("workstation_id", req.WorkstationId.Value);
    parameters.Add("ws_od_id", req.WorkstationId.Value);
    parameters.Add("ws_od_deleted", (int)YesOrNo.NO);
}
```

> 两个分支用不同参数名 (`@workstation_id` / `@ws_od_id`) 避免 Dapper 的跨 provider 参数复用问题。

### 查询计划（MySQL）

```sql
SELECT mr.*, pm.name AS mission_name, pm.is_challenge_mission
FROM mission_record mr
LEFT JOIN product_mission pm ON mr.mission_id = pm.id AND pm.deleted = 2
WHERE mr.deleted = 2
  AND (
    mr.workstation_id = 1                                  -- 分支1
    OR mr.id IN (                                           -- 分支2
        SELECT mission_record_id FROM operation_data
        WHERE workstation_id = 1 AND deleted = 2
    )
  )
ORDER BY mr.id LIMIT 20;
```

| 分支 | 索引 | 操作 |
|---|---|---|
| 1: `mr.workstation_id = 1` | `ix_mr_ws(workstation_id, deleted)` ✅ 新建 | 索引查找 → PRIMARY KEY 回表 |
| 2: 子查询 | `ix_opdata_ws(workstation_id, mission_record_id)` ✅ 已有 | 覆盖索引扫描，零回表 |
| 2: `mr.id IN (...)` | PRIMARY KEY | 主键点查 |
| ORDER BY + LIMIT | — | 两路结果集小（单站点下），merge-sort 可接受 |

### COUNT 查询

COUNT 查询使用 `fromClauseNoForce`（无 FORCE INDEX），WHERE 子句与主查询相同，同样受益于索引。

## 优化：翻页/跳页加载遮罩

### 问题

`DataGridViewGroup` 已有半透明加载遮罩（`_loadingOverlay`），但只在首次 `_queryData` 时显示。翻页/跳页（`DataGridViewPanel.LoadPageData`）只禁用按钮，用户看不到加载状态，站点查询慢时 UI 像卡死。

### 修复

**文件：**
- `OperationGuidance_new/Views/ReusableWidgets/DataGridViewGroup.cs`
- `OperationGuidance_new/Views/ReusableWidgets/DataGridViewPanel.cs`

1. **`DataGridViewGroup`**：将首次查询中的遮罩显示逻辑提取为 `internal ShowLoadingOverlay()`，`HideLoadingOverlay` 改为 `internal`
2. **`DataGridViewPanel`**：在 `LoadPageData` 的 `ServerFetch` 前调用 `group.ShowLoadingOverlay()`，`finally` 中调用 `group.HideLoadingOverlay()`

## 不改的部分

| 项 | 理由 |
|---|---|
| `DataQueryView_SCII` | 已改为直接传 `workstation_id`，无需改动 |
| `QueryMissionRecordListReq` | 已有 `WorkstationId` 字段 |
| `QueryMissionRecordsByWorkstationIds`（双源 API） | 保留，其他调用方可能使用 |
| `QueryWorkstationInfoByMissionRecordIds`（双源 API） | 保留，`EnrichWorkstationBatch` 使用 |

## 风险评估

- **低风险**：新增索引是标准 DDL，不影响已有查询
- `FORCE INDEX (PRIMARY)` 只在无 `WorkstationId` 过滤时保留——原有行为不变
- 子查询使用已存在的覆盖索引 `ix_opdata_ws`，无额外性能开销
- 双源 OR 确保新老数据全覆盖
