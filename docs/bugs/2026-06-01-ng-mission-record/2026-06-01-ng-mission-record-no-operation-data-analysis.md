# 任务记录 NG 且无 operation_data 问题分析

**日期**: 2026-06-01
**日志文件**: `2026-06-01.log`
**问题追溯码**: `P7162400011#VPA01#T2605300446#V777168#B0000#`
**问题描述**: 任务在 UI 上显示正常完成（OK），但任务记录查询显示 NG 结果，且该记录没有任何对应的 operation_data。
**客户版本**: **v1.4.35**（v1.4.x 分支，当前仓库为 v1.5.x HEAD）
**数据库验证**: 已完成 ✅

---

## 1. 数据库验证结果

### 1.1 mission_record — 同一个 product_bar_code 出现 3 条

| id | mission_id | result | is_redo | create_time | modify_time | product_batch | op_data |
|---|---|---|---|---|---|---|---|
| **50907** 🔴 | 1 | **NG (0)** | NO | **13:29:19** | **13:29:19** | **空** | **0 条** |
| 51145 | 1 | OK (1) | YES | 15:08:37 | 15:08:58 | Q0626-805 | 2 条 |
| 51147 | 2 | OK (1) | NO | 15:09:35 | 15:09:57 | Q0626-805 | 2 条 |

### 1.2 铁证

1. **50907 是 NG 孤儿**: `create_time == modify_time == 13:29:19`，`product_batch` 为空。说明后续两次 UPDATE 均未命中它。
2. **日志中 13:28:29 第1次 0446 的 record 从数据库消失**: 按 barcode 查不到，按 id 范围（50902-50907）查不到，按 create_time 查不到。**被错误 UPDATE 覆盖了 `create_time` 和 `product_bar_code`**——因为 UPDATE SQL 包含所有字段（含 `create_time = @create_time`）。
3. **51145 是后来独立的返工记录**（15:08:37，is_redo=YES），51147 是另一条正常记录（mission_id=2）。
4. **operation_data 全落在后来的 OK 记录上**: 50907 没有任何 operation_data。

### 1.3 结论

**`_missionRecord.id` 在 13:29:19.894（INSERT 50907）→ 13:29:20.904（SCII UPDATE）之间的 1 秒内被篡改**。两次 UPDATE 都返回了 `Result: 1`，但打在了第1次 0446 的旧 record 上。那条旧 record 的 `create_time` 和 `product_bar_code` 被 UPDATE 覆盖后，无法再通过原始条件查到。

---

## 2. 日志完整时间线

| # | 时间 | 条码 | 操作 | 说明 |
|---|---|---|---|---|
| 1 | 13:28:29 | **0446** (第1次) | 扫码 → INSERT → UPDATE product_batch | 正常激活，mission_record 创建 |
| 2 | 13:28:36 | 0446 | bolt_serial=1 | operation_data #111940 |
| 3 | 13:28:41 | 0446 | bolt_serial=2 → **UPDATE→OK** | FINISHED_OK，Total=434 OK=430 **NG=4** |
| 4 | 13:29:01 | **0447** | 扫码 → INSERT → UPDATE → bolts → OK | 正常激活完成，**NG=4** |
| 5 | 13:29:19 | **0446** (第2次) | 扫码激活 "Already chosen mission" | |
| 6 | 13:29:19.894 | 0446 | **INSERT mission_record** | base 方法创建 record **50907**（result=NG）🔴 |
| 7 | 13:29:20.172 | - | ForceLockAllTools | |
| 8 | **13:29:20.416** | - | **TerminateMission** | 切换流程终止，DEBUG 日志 1ms 完成 ⚠️ |
| 9 | 13:29:20.904 | 0446 | UPDATE mission_record | SCII 覆写设 product_batch — **未命中 50907** |
| 10 | 13:29:28 | 0446 | bolt_serial=1 | operation_data #111946 |
| 11 | 13:29:38 | 0446 | bolt_serial=2 | operation_data #111947 |
| 12 | 13:29:38.770 | 0446 | **UPDATE mission_record** | 全部螺栓完成 — **未命中 50907** |
| 13 | 13:29:38 | - | 统计 | Total=435 OK=430 **NG=5** ⚠️ |
| 14 | 13:29:56 | **0448** | 扫码 | 新条码 |

**关键窗口**: 13:29:19.894（INSERT 50907）→ 13:29:20.904（UPDATE），1 秒内 `_missionRecord.id` 被篡改。

---

## 3. 根因

### 3.1 已排除的假设

- ~~**Double INSERT**~~: DB 实际是 2 OK + 1 NG，非 2 NG + 1 OK
- ~~**字段映射出错**~~: 正常 record 的 UPDATE 都正确执行
- ~~**并发写入**~~: 螺栓顺序执行，都在线程 [1]
- ~~**SCII override UPDATE 变成了 INSERT**~~: create_time 13:29:20-13:29:21 无其他 INSERT

### 3.2 确认的假设

**`_missionRecord.id` 在 INSERT 后被重定向，指向了第1次 0446 的旧 record。**

证据：
- 50907 的 `product_batch` 为空 → SCII override UPDATE 没打中 50907
- 50907 的 `mission_result` 仍为 NG → 完成时 UPDATE 没打中 50907
- 两次 UPDATE 日志都返回 `Result: 1` → 它们打中了其他 record
- 第1次 0446 的 record 从 DB 消失 → 被 UPDATE 覆盖了 `product_bar_code` 和 `create_time`

### 3.3 v1.4.35 代码追踪结论

对 v1.4.x 分支关键路径的完整追踪结果：

| 方法 | 路径 | 是否修改 `_missionRecord` |
|---|---|---|
| `ActivateMission` | 取消 CTS → Prepare → Validation → Initialize → `_activated=true` → `ActionAfterActivatingMission` | 否 |
| `ActionAfterActivatingMission` (base) | `_missionRecord = new()` → INSERT（设 id=50907） → ForceLock → `await Task.Delay(500)` | **是 — 设 id=50907** |
| `TerminateMission` (base) | Cancel tasks → ForceLock → `_activated=false` → `await Task.Delay(300)` → `_barCodeObj.Reset()` → `OnMissionCompleted` | 否（不修改 `_missionRecord`） |
| `TerminateMission` (SCII override) | INFO → `await base.TerminateMission` → DEBUG "Base completed" → DEBUG "completed" | 否 |
| `ActivateMissionAutomatically` (SCII override) | 仅处理 USB scanner → `OpenBarCodePopUpForm()`，**不处理 self-looping** | 否 |
| `InitializeBeforeActivatingMission` | SwitchBolt → ChangeBoltStatusToWorking → SendPSet | 否 |
| `ValidateProductBarCodeAsync` | Check barcode → SwitchToMission → `ActivateMission()` → `await Task.Delay(200)` → Hide | 否 |

**各路径均不修改 `_missionRecord.id`**。但日志证明了它被修改了。可能原因：

1. **v1.4.35 二进制与 v1.4.x 分支源码有差异** — 存在我们看不到的代码路径
2. **TerminateMission 的 1ms 执行**指向 base.TerminateMission 内某处抛异常或提前返回——但 v1.4.x 源码中没有对应的提前返回路径
3. **`_missionRecord` 引用被替换** — 如果在 `await Task.Delay(500)` 期间，另一段代码执行了 `_missionRecord = new MissionRecordDTO(...)`（可能引用旧 record 的数据），会导致此后的 `_missionRecord.id` 指向旧值

**无法仅从源码定位精确触发点**——需要客户环境部署诊断日志（§6）后复现。

---

## 4. 代码关键路径

### 4.1 激活流程 & `_missionRecord.id` 传递

```
ActivateMission()                                          [AWorkplaceContentPanel:1310]
  └─> ActionAfterActivatingMission()                       [AWorkplaceContentPanel:1542]
        ├─> _missionRecord = new() { id=0, result=NG, ... }
        ├─> _apis.AddOrUpdateMissionRecord(...)            ← INSERT → DB returns id → _missionRecord.id = 50907（引用传递）
        ├─> ForceLockAllTools()                             ← 13:29:20.172
        ├─> await Task.Delay(500)                           ← 挂起，UI 线程空闲
        │     └─> 【TerminateMission 在此刻被调用】          ← 13:29:20.416
        └─> StartLockCheckingTask()                        ← 500ms 后恢复
  └─> SCII override:
        ├─> _missionRecord.product_batch = ...             ← _missionRecord.id 已被篡改
        ├─> _apis.AddOrUpdateMissionRecord(...)            ← UPDATE 错误 record，50907 未受影响
        ...
DoAfterRecevingTighteningDataAsync                         [WorkplaceMissionView_SCII:1341]
  └─> _missionRecord.mission_result = OK
  └─> _apis.AddOrUpdateMissionRecord(...)                  ← UPDATE 错误 record，50907 保持 NG
  └─> TerminateMission(FINISHED_OK)
```

### 4.2 TerminateMission 基类逻辑

```
TerminateMission(status)                                   [AWorkplaceContentPanel:2746]
  ├─> Cancel background tasks
  ├─> ForceLockAllTools() (if activated)
  ├─> _activated = false
  ├─> await Task.Delay(300)                                ← 无条件！Type B 的 1ms 完成证明此路径被跳过
  ├─> ClearAndResetAllCurrentBolts()
  ├─> ResetWorkingProcessPanel()
  ├─> _barCodeObj.Reset()
  ├─> _ruleIdsCheckedCached = null
  ├─> _isRedo = NO
  ├─> await OnMissionCompleted(status)
  └─> if (_missionRecord?.mission_result == OK)
        └─> ActivateMissionAutomatically()                 ← 可能触发新的激活！
```

### 4.3 InsertOrUpdate 逻辑

```csharp
// AServiceBase.cs:61
public T InsertOrUpdate(T entity) {
    if (entity.id > 0) return UpdateEntity(entity);  // UPDATE WHERE id = @id
    else               return AddEntity(entity);      // INSERT
}
```

### 4.4 版本差异（v1.4.35）

v1.4.x 的 SCII `TerminateMission` 不包含 `ResetMissionDetails()`（该调用在 v1.5.x 的 commit `3bfa2cb` 引入）：

```csharp
// v1.4.x
public override async Task TerminateMission(WorkplaceProcessStatus status) {
    logger.Info($"Terminating mission with status: {status}");
    await base.TerminateMission(status);          // 直接调用 base
    logger.Debug("Base termination completed");
    logger.Debug("Mission termination completed");
}
```

---

## 5. TerminateMission 日志异常

| 类型 | 时间 | INFO "status" | DEBUG "Base completed" | 间隔 |
|---|---|---|---|---|
| FINISHED_OK | 13:28:41, 13:29:13, 13:29:38 | ✅ | ❌ | N/A |
| 切换流程 | 13:28:29, 13:29:01, **13:29:20** | ❌ | ✅ | **1ms** |

1ms 间隔说明 `base.TerminateMission` 内的 `await Task.Delay(300)` 未执行。可能原因：运行时二进制与源码不同，或存在绕过 SCII override 的调用路径。

---

## 6. 已添加的诊断日志（v1.5.x HEAD）

| 文件 | 位置 | 内容 |
|---|---|---|
| `AWorkplaceContentPanel.cs` | 1556 | INSERT 后记录 `id`, `barcode`, `result` |
| `AWorkplaceContentPanel.cs` | 174-175 | `ProductScanCount` / `PartsScanCount` 属性（激活时归零） |
| `ABarCodeInputPopUpForm.cs` | 396, 415, 590 | 每次扫码打印 `Product/Parts scan #N: [barcode]` |
| `WorkplaceMissionView_SCII.cs` | 151 | 中断按钮日志追加 `_missionRecord.id` + `barcode` |
| `WorkplaceMissionView_SCII.cs` | 1110 | 第二次 API 调用前打印 `_missionRecord.id` |
| `WorkplaceMissionView_SCII.cs` | 1435 | TerminateMission 入口打印 `_missionRecord` 状态 + `_activated` |

以上改动在 v1.5.x HEAD，**需移植到 v1.4.x 才能在客户环境生效**。

---

## 7. 修复方案

### 7.1 立即修复：防御性捕获 `_missionRecord.id`

在 SCII override 的 `ActionAfterActivatingMission` 中，将 `_missionRecord.id` 捕获为局部变量，后续 UPDATE 用局部变量而非 `_missionRecord.id`：

```csharp
protected override async Task ActionAfterActivatingMission() {
    await base.ActionAfterActivatingMission();

    // 防御：捕获 INSERT 分配的 id，防止后续被意外篡改
    int missionRecordId = _missionRecord.id;
    if (missionRecordId <= 0) {
        logger.Error($"[SCII:ActionAfterActivatingMission] _missionRecord.id is {missionRecordId}, aborting update");
        return;
    }
    logger.Info($"[SCII:ActionAfterActivatingMission] Captured missionRecordId={missionRecordId}");

    _missionRecord.product_batch = ...;
    _missionRecord.id = missionRecordId;  // 强制回写，确保 UPDATE 目标正确
    _apis.AddOrUpdateMissionRecord(new(_missionRecord));
}
```

同样在 `DoAfterRecevingTighteningDataAsync` 中也要做相同防御。

### 7.2 部署诊断日志后修复

诊断日志部署到 v1.4.35 后复现，确认 `_missionRecord.id` 被修改的精确触发点，再针对性修复调用来源。

### 7.3 长期改进

1. 在 "Already chosen mission" 切换流程中增加防御性检查，确保新旧 record 不混淆
2. 统一 TerminateMission 调用路径，确保所有终止都经过 SCII override
3. 考虑将 `_missionRecord` 改为不可变引用——INSERT 后锁定，仅允许显式更新其字段

---

## 8. 相关代码文件

| 文件 | 关键行号 | 说明 |
|---|---|---|
| `AWorkplaceContentPanel.cs` | 171-177 | `ProductScanCount`/`PartsScanCount` + `MissionRecord` 属性 |
| `AWorkplaceContentPanel.cs` | 1310-1342 | `ActivateMission` — 激活入口 |
| `AWorkplaceContentPanel.cs` | 1542-1556 | `ActionAfterActivatingMission` — INSERT + 诊断日志 |
| `AWorkplaceContentPanel.cs` | 2746-2825 | `TerminateMission` — 终止流程 |
| `AWorkplaceContentPanel.cs` | 2332-2344 | `mission_record_id` 赋值给 operation_data |
| `ABarCodeInputPopUpForm.cs` | 395-397 | `ValidateProductBarCode` — 追溯码扫描 |
| `ABarCodeInputPopUpForm.cs` | 412-416, 587-591 | `ValidatePartsBarCode` — 物料码扫描 |
| `WorkplaceMissionView_SCII.cs` | 1098-1113 | SCII `ActionAfterActivatingMission` 覆写 |
| `WorkplaceMissionView_SCII.cs` | 1341-1354 | 全部螺栓完成 → UPDATE OK |
| `WorkplaceMissionView_SCII.cs` | 1434-1442 | `TerminateMission` 覆写 + 诊断日志 |
| `OperationGuidanceApis.cs` | 785-797 | `AddOrUpdateMissionRecord` API |
| `AServiceBase.cs` | 61-67 | `InsertOrUpdate` 逻辑 |
