# 日志精简设计

**日期**: 2026-06-01
**范围**: 全局（OperationGuidance_new + OperationGuidance_service）
**目标**: 当前根日志级别为 ALL，大量 DEBUG 日志在产线环境被无条件写入，导致日志文件膨胀、关键信息被淹没。需要系统性地精简无意义日志。

---

## 1. 现状

### 1.1 日志配置

三个 `App.config` 的根日志级别均为 `ALL`：

| 文件 | 行号 | 级别 |
|---|---|---|
| `OperationGuidance_new/App.config` | 28 | `<level value = "ALL"/>` |
| `OperationGuidance_service/App.config` | 28 | `<level value = "ALL"/>` |
| `CustomLibrary/App.config` | 28 | `<level value = "ALL"/>` |

产物线无任何日志过滤——所有 DEBUG/INFO/WARN/ERROR 全部输出。日志文件动辄数百 KB（本次分析的日志即 690KB）。

### 1.2 日志数量统计

| 文件 | 日志调用数（近似） |
|---|---|
| `WorkplaceMissionView_SCII.cs` | ~132 |
| `AWorkplaceContentPanel.cs` | ~90 |
| `ToolTask.cs` | ~60 |
| `ABarCodeInputPopUpForm.cs` | ~50 |
| 其他视图/服务 | ~200+ |

---

## 2. 问题分类

### 类别 A：热路径 DEBUG 泛滥

**典型场景**: `ActionAfterArmDataReceived` 对每一帧力臂数据都打 DEBUG 日志，每秒数条。

```
[SCII:ActionAfterArmDataReceived] Received arm data, maxValue: 0, coordinates: ...
[SCII:ActionAfterArmDataReceived] Mission activated and current bolt exists
[SCII:ActionAfterArmDataReceived] Tool ID found: 1
[SCII:ActionAfterArmDataReceived] Removed manual lock/unlock messages
[SCII:ActionAfterArmDataReceived] Arm position is OK, removed position lock message
[SCII:ActionAfterArmDataRemoved] No admin confirmation needed, removed admin confirmation lock
[SCII:ActionAfterArmDataReceived] Parameter set is configured: 1
```

每一帧数据触发 6-8 条 DEBUG 日志。500 个任务 × 每任务数十帧 = **数万条无意义日志**。

**建议**: 合并为 1 条 TRACE 级别日志，或在状态变化时（OK→NG / NG→OK）才打 INFO。

### 类别 B：纯进出标记

**典型场景**: 方法入口/出口的 DEBUG 日志，无参数无返回值。

```csharp
logger.Debug($"[SCII:ActionAfterActivatingMission] Action after activating mission started");
// ... 方法体 ...
logger.Info($"[SCII:ActionAfterActivatingMission] Mission record updated with product batch");
```

"started" 级别的日志在调用栈完整时毫无价值。保留有数据的结果日志（如 "updated with product batch"），删除纯粹的进入标记。

**建议**: 删除所有 `X started` / `X completed` / `X entry` / `X exit` 样式的 DEBUG 日志，除非紧随其后的是具体数据。

### 类别 C：冗余的双级别日志

**典型场景**: 同一事件先打 DEBUG 再打 INFO/WARN，DEBUG 描述状态，INFO 描述结果。

```
logger.Debug("Arm position is OK, removed position lock message");  // 状态
logger.Debug("Parameter set is configured: 1");                      // 状态
// ... 后续某个时刻 ...
logger.Warn("Arm position is not OK, added position lock message");  // 结果
```

**建议**: 仅保留 WARN（状态变化），删除中间状态的 DEBUG。

### 类别 D：联接/心跳噪音

**典型场景**: `ToolTask` 的锁状态检查和 `SerialPortTask` 的联接日志。

```
[TOOL:扭矩枪-192.168.1.16:1200] Lock skipped - in cooldown period
[TOOL:扭矩枪-192.168.1.16:1200] Lock state: True -> False
[TOOL:扭矩枪-192.168.1.16:1200] Unlocking
[TOOL:扭矩枪-192.168.1.16:1200] Lock state: False -> True
[TOOL:扭矩枪-192.168.1.16:1200] Locking
```

工具锁状态几乎每秒都在变化，每次变化都产生 2-4 条日志。对调试锁竞争有用，但对正常产线运行没有意义。

**建议**: 
- 仅记录状态变化（Lock state 变化本身是有用的，合并为一条）
- "skipped - in cooldown" 改为 TRACE 或不记录
- "Unlocking" / "Locking" 改为 TRACE

### 类别 E：SQL 执行日志

**典型场景**: 每个 SQL 查询都有 INFO 级别的日志：

```
INFO  OperationGuidance_service.Models.MissionRecord - sql: [select * from mission_record where ...]
INFO  OperationGuidance_service.Models.MissionRecord - Size of result: 439
```

产线环境每次登录/激活/查询都触发大量 SQL 日志。

**建议**: 将 SQL 执行日志降为 DEBUG，保留 `Size of result` 在 INFO 仅当结果异常时输出。

---

## 3. 日志级别策略

| 级别 | 用途 | 示例 |
|---|---|---|
| **TRACE** | 逐帧/逐包数据，锁状态轮询 | arm data frame, lock check polling |
| **DEBUG** | SQL 执行，方法中间状态，开发期诊断 | SQL statements, intermediate state |
| **INFO** | 关键业务事件，状态变更，用户操作 | mission activated, bolt completed, interrupt clicked |
| **WARN** | 可恢复的异常，需要关注但不阻断 | arm position not OK, redo required |
| **ERROR** | 不可恢复的错误，需要立即处理 | mission NG max reached, database connection lost |

### 3.1 生产配置

产线环境建议将根级别设为 **INFO**：

```xml
<root>
  <level value="INFO"/>
  <appender-ref ref="RollingLogFileAppender"/>
</root>
```

仅特定组件可覆写为 DEBUG（通过 `<logger name="...">` 元素），用于现场调试。

---

## 4. 实施计划

按优先级分阶段执行：

### 阶段 1：热路径（最大收益）

| 文件 | 改动 | 预估削减 |
|---|---|---|
| `WorkplaceMissionView_SCII.cs:ActionAfterArmDataReceived` | 合并 6-8 条 DEBUG → 1 条 TRACE | 60% |
| `WorkplaceMissionView_SCII.cs:ActionAfterArmDataRemoved` | 合并到 ActionAfterArmDataReceived | |
| `Tasks/ToolTask.cs:锁检查循环` | "skipped - in cooldown" → TRACE | 30% |
| `Tasks/ToolTask.cs:锁状态变化` | "Unlocking"/"Locking" → TRACE，保留状态变化 | 50% |

### 阶段 2：进出标记（批量删除）

| 文件 | 改动 |
|---|---|
| 全项目 `logger.Debug("X started")` | 删除无参数的 started/completed/entry/exit 日志 |
| 全项目 `logger.Debug("X end")` | 同上 |

### 阶段 3：SQL 和双级别

| 文件 | 改动 |
|---|---|
| `AServiceBase.cs` 及所有 Model 文件 | SQL 语句 → DEBUG，异常结果保留 INFO |
| `WorkplaceMissionView_SCII.cs` | 合并同事件的 INFO+DEBUG 双日志 |

### 阶段 4：生产配置

| 文件 | 改动 |
|---|---|
| `OperationGuidance_new/App.config` | `<level value = "INFO"/>` |
| `OperationGuidance_service/App.config` | `<level value = "INFO"/>` |

---

## 5. 风险与约束

- **不能删除错误路径的日志** — ERROR/WARN 日志全部保留
- **不能删除用户可见操作的日志** — 中断按钮、条码扫描、任务激活/完成的 INFO 保留
- **SQL 日志降级需要过渡期** — 先在测试环境运行一周，确认排障能力不受影响再推产线
- **第三方库（log4net）的日志级别不受影响** — 仅控制项目自身的 logger

---

## 6. 决策记录

| 决策 | 选择 | 理由 |
|---|---|---|
| 生产日志级别 | INFO | 保留关键业务事件，过滤热路径噪音 |
| 热路径帧数据 | TRACE | 仅开发/调试时开启 |
| 方法进出标记 | 删除 | 栈跟踪提供相同信息 |
| SQL 语句 | DEBUG | 数据显示 INFO，语句降为 DEBUG |
| 工具锁状态 | TRACE（跳过）+ INFO（状态变化） | 保留有意义的转折点 |
