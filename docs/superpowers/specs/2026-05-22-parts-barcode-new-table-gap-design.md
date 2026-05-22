# 物料码新表读写缺口修复 & 重新导入功能事务化

**日期:** 2026-05-22  
**状态:** 草稿  
**分支:** v2.1.x

## 背景

v2.0 引入独立 `parts_bar_code` 表（`modify_*_20250625_1.sql`），将物料码从 `mission_record.parts_bar_code` 逗号分隔列拆分为独立行。迁移脚本负责历史数据导入，但**正常扫码激活流程从未写入新表**，导致 `CheckPartsBarCode`（查新表）永远返回 false——物料码重码校验失效。

同时后台管理"重新导入物料码"功能（`ReimportPartsBarcode`）因全量 `SELECT *` 无分页导致 MySQL 超时，且无事务保护。

## 目标

1. **任务激活时写入 `parts_bar_code` 新表**——异步、带重试、不阻塞激活流程
2. **`ReimportPartsBarcode` 加事务 + 分页**——防止部分成功导致数据丢失，避免超时

## 设计

### 1. 任务激活时写入新表

**改动点:** `AWorkplaceContentPanel.ActionAfterActivatingMission()`（line 1442）

**现有流程:**
```
ActionAfterActivatingMission
  → MissionRecord.parts_bar_code = string.Join(",")  // 旧列
  → AddOrUpdateMissionRecord                         // 只写旧列
```

**改造后:**
```
ActionAfterActivatingMission
  → MissionRecord.parts_bar_code = string.Join(",")  // 旧列（保持不变）
  → AddOrUpdateMissionRecord                         // 写旧列
  → Task.Run(async () => {                           // 异步写新表
        重试3次(间隔1s/2s/4s)
        foreach partsBarCode:
          AddOrUpdatePartsBarCode(mission_record_id, barCode)
        全部成功 → 结束
        重试耗尽 → logger.Error(mission_record_id + 条码列表)
    })
```

**保障机制:**

| 层级 | 机制 |
|------|------|
| 重试 | 单次失败重试全量（非逐条），3 次，间隔 1s/2s/4s |
| 日志 | 全部重试失败后打 Error，包含 `mission_record_id` 和完整条码列表 |
| 兜底 | 后台管理"重新导入物料码"可从旧列全量恢复到新表 |

**设计决策:**
- 不阻塞激活流程——新表是冗余查询用，旧列是主路径
- 不做逐条重试——物料码通常 1-5 个，全量重试更简单
- 不做本地队列——`ReimportPartsBarcode` 已是兜底

### 2. ReimportPartsBarcode 事务 + 分页

**改动点:** `OperationGuidanceApis.ReimportPartsBarcode()`（line 245）

**现有问题:**
```
1. DELETE from parts_bar_code          ← 无事务
2. SELECT * from mission_record        ← 全量、无分页、MySQL 超时
3. INSERT into parts_bar_code          ← 如果步骤2失败，表已空
```

**改造后——复用 `AddOrUpdateProductMission` 的事务模式:**

```csharp
using DbConnection conn = DbConnector.GetConnection();
DbTransaction transaction = conn.BeginTransaction();
_partsBarCodeService.UseConnection(conn, transaction);
_missionRecordService.UseConnection(conn, transaction);
try {
    // 1. DELETE
    _partsBarCodeService.ExecuteSql(deleteSql);
    
    // 2. 分页 SELECT（每批 1000）
    int offset = 0;
    while (true) {
        var batch = _missionRecordService.FindBySql(
            $"{selectSql} LIMIT {batchSize} OFFSET {offset}");
        if (batch.Count == 0) break;
        
        // 3. 拆分、逐批 INSERT
        _partsBarCodeService.AddBatch(splitAndConvert(batch));
        
        offset += batchSize;
    }
    
    transaction.Commit();
} catch {
    transaction.Rollback();
    throw;
}
```

**分页参数:** `batchSize = 1000`

**关键:** `ExecuteSql` 通过 Service 调用 → `Wrapper.ExecuteWithRetry` → 当 `UseConnection` 被调用后走共享连接和事务。`FindBySql` 同理。三者（DELETE / SELECT / INSERT）在同一事务内。

### 3. 点位绑定物料码的动态插入（已有逻辑，无需改动）

任务激活后，螺栓点位绑定的物料码（`product_bolt.parts_bar_code_ids`）通过 `CheckBoltBoundPartsBarCode` → `OpenBarCodePopUpForm` 触发扫码。扫码校验通过后 `ValidatePartsBarCode`（line 536）调用 `AddOrUpdatePartsBarCode` **已写入新表**。

此路径在激活前被 `PartsBarCodeExtraCheck`（line 562）拦截（"此物料与点位绑定，任务进行时才需录入"），因此激活时 `_barCodeObj.PartsBarCodes` 不包含点位绑定物料码——它们只在激活后动态插入，且已覆盖新表写入。

### 4. 链路一致性验证

修复后物料码完整链路：

| 阶段 | 写入新表 | 写入旧列 | 查询新表 |
|------|:--:|:--:|:--:|
| 扫码（激活前） | - | - | `CheckPartsBarCode` 重码校验 |
| 激活 | **异步** `AddOrUpdatePartsBarCode` | `MissionRecord.parts_bar_code` | - |
| 激活后追扫（含点位绑定） | `AddOrUpdatePartsBarCode`（已有） | `MissionRecord.parts_bar_code` | `CheckPartsBarCode` |
| 历史数据 | `ReimportPartsBarcode` / 迁移脚本 | 已存在 | - |

## 涉及文件

| 文件 | 改动类型 |
|------|------|
| `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs` | `ActionAfterActivatingMission` 新增异步写入新表 |
| `OperationGuidance_service/Controllers/OperationGuidanceApis.cs` | `ReimportPartsBarcode` 加事务 + 分页 |

## 不改动

- `CheckPartsBarCode` API 逻辑不变
- `AddOrUpdatePartsBarCode` API 逻辑不变
- `ValidatePartsBarCode` 激活后追扫逻辑不变（已写双表）
- `AddOrUpdateProductMission` 事务模式不变（仅复用）
- `ABarCodeInputPopUpForm` 校验流程不变
