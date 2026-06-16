# 迁移脚本超时与致命错误处理 — 设计文档

**日期**：2026-06-16
**版本**：v1.6.10
**文件**：`docs/superpowers/specs/2026-06-16-migration-fatal-error-design.md`

## 问题

启动时 MySQL 迁移脚本执行失败。日志中首条 SQL 报 "Fatal error"，后续 15 条全部报 "Connection must be valid and open"，产生大量级联噪音。

**日志证据**（`docs/bugs/2026-06-16-migration-fatal-error/2026-06-16.log`）：

```
行3  12:06:59  INFO   发现未执行脚本 [modify_mysql_20260526]
行4  12:07:29  WARN   Fatal error → CREATE INDEX 失败  ← 恰好 30 秒后
行5+ 12:07:29  WARN   Connection must be valid and open × 15 条
行19 12:07:29  WARN   completed with errors — will retry on next startup
```

三个迁移脚本全部失败，下次启动重试。

## 根因

**两级因果：**

```
根因：CommandTimeout 默认 30 秒，DDL 在大表上超时
  ↓
后果：超时后连接被 MySQL 杀死，代码在死连接上继续执行 → 级联噪音
```

### 第一层：CommandTimeout 不足

`MySqlConnector.cs:96` 创建 `MySqlCommand` 后未设置 `CommandTimeout`，MySQL Connector/NET 默认值 = 30 秒。`CREATE INDEX` 在百万行级 `operation_data` 表上耗时远超 30 秒，触发超时。

日志中行3→行4 恰好间隔 30 秒，印证此结论。

### 第二层：死连接上级联执行

超时后 MySQL 服务端断开连接。代码的通用 `catch (Exception)` 不区分异常类型，继续在死连接上执行剩余语句——后续全部 `ExecuteNonQuery()` 必然失败，产生 15 条无效 WARN。

## 设计

### 改动范围

**单一文件**：`OperationGuidance_service/Database/MySqlConnector.cs`
**改动量**：~25 行

### 修复 1：设置迁移 CommandTimeout（治本）

在执行迁移脚本前，将超时从 30 秒提升到 600 秒（10 分钟），覆盖大表 DDL：

```csharp
command.CommandTimeout = 600; // 迁移 DDL 在大表上可能耗时数分钟
```

### 修复 2：连接断开检测 + 重连一次 + 继续后续脚本

三级异常分类：

| 级别 | 异常类型 | 检测方式 | 处理 |
|------|---------|---------|------|
| 1. 幂等性 | Duplicate column/key (1060/1061)，Can't DROP (1091) | `MySqlException.Number` | **INFO** · 视为成功 · 继续 |
| 2. 连接致命 | 连接断开（含超时） | `conn.State != ConnectionState.Open` | **ERROR** · 重连一次 · 跳过当前脚本 · 继续后续 |
| 3. 普通错误 | 语法错误、约束冲突等 | 其余 Exception | **WARN** · 标记失败 · 继续 |

**重连流程：**

1. 检测到 `conn.State != Open` → 记录 ERROR
2. `conn.Close()` → `conn.Open()` → 创建新 `MySqlCommand` → 设 `CommandTimeout = 600`
3. 重连成功 → INFO 日志 → `allOk = false` 标记当前脚本 → 继续下一个脚本
4. 重连失败 → ERROR 提示需人工介入 → `break` 中止全部

**关键：跳过当前脚本，不重试已失败的语句。** 当前脚本标记为失败，下次启动由幂等性 catch (1060/1061/1091) 兜底重试。

### 伪代码

```csharp
command.CommandTimeout = 600; // 修复 1

foreach (string fileName in pendingScripts) {
    try {
        bool allOk = true;
        foreach (string stmt in fileText.Split(';')) {
            string s = stmt.Trim();
            if (string.IsNullOrEmpty(s)) continue;
            try {
                command.CommandText = s;
                command.ExecuteNonQuery();
            } catch (MySqlException stmtEx) when (
                stmtEx.Number == 1060 || stmtEx.Number == 1061 || stmtEx.Number == 1091
            ) {
                logger.Info($"Statement in [{fileName}] is a no-op..."); // 不变
            } catch (MySqlException stmtEx) when (
                conn.State != ConnectionState.Open
            ) {
                // 修复 2：连接断开，跳出语句循环
                logger.Error($"Connection lost during [{fileName}]: {stmtEx.Message}. SQL: {s.Substring(0, Math.Min(s.Length, 100))}...");
                allOk = false;
                break;
            } catch (Exception stmtEx) {
                logger.Warn($"Statement in [{fileName}] failed: {stmtEx.Message}..."); // 不变
                allOk = false;
            }
        }

        // 修复 2：连接断开 → 重连一次，继续后续脚本
        if (conn.State != ConnectionState.Open) {
            try {
                conn.Close();
                conn.Open();
                command = conn.CreateCommand();
                command.CommandTimeout = 600;
                logger.Info($"Reconnected successfully after connection loss during [{fileName}] — skipping current script, continuing remaining scripts");
            } catch (Exception reconnectEx) {
                logger.Error($"Reconnect failed after connection loss during [{fileName}]: {reconnectEx.Message}. Requires manual intervention.");
                break; // 中止全部
            }
        }

        // ... 后续 allOk 判断逻辑不变
    } catch (Exception e) {
        logger.Warn($"Execute sql script[{fileName}] failed, e: {e}");
    }
}
```

### 关键决策

| 决策 | 说明 |
|------|------|
| **600 秒超时** | 覆盖百万行级表的 DDL 操作。客户环境实测 `CREATE INDEX` 通常 1-5 分钟完成 |
| **重连一次** | 跳过失败脚本继续后续脚本，最大化一次启动中的迁移成功率 |
| **仅 `conn.State != Open` 判断** | 不附加 `Number == 0`，不同 MySQL 版本更一致 |
| **重连后创建新 command** | 旧 command 内部状态可能损坏，新 command 更安全 |
| **不在后台线程执行** | 避免迁移未完成时业务代码读到旧 schema 的竞态 |
| **硬编码 600，不配置化** | 当前没有配置文件，且 600 覆盖 99% 场景 |

### 预期效果

**正常情况**（大表，600 秒内完成）：

```
INFO  Not executed sql script[modify_mysql_20260526] found
INFO  Execute sql script[modify_mysql_20260526] successfully
INFO  Execute sql script[modify_mysql_20260603] successfully
INFO  Execute sql script[modify_mysql_20260603_2] successfully
```

**超时但仍可重连**（600 秒内仍超时，但 MySQL 正常）：

```
ERROR Connection lost during [modify_mysql_20260526]: Fatal error... SQL: CREATE INDEX...
INFO  Reconnected successfully — skipping current script, continuing remaining scripts
WARN  Execute sql script[modify_mysql_20260526] completed with errors — will retry on next startup
INFO  Execute sql script[modify_mysql_20260603] successfully
INFO  Execute sql script[modify_mysql_20260603_2] successfully
```

**极端情况**（超时且重连失败，MySQL 不可用）：

```
ERROR Connection lost during [modify_mysql_20260526]: Fatal error... SQL: CREATE INDEX...
ERROR Reconnect failed after connection loss: ... Requires manual intervention.
```

## 文档产出

`docs/bugs/2026-06-16-migration-fatal-error/`：
- `2026-06-16.log` — 原始日志
- `analysis.md` — 根因分析
- `fix-summary.md` — 修复说明
