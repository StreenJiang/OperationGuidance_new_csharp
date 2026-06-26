# 迁移脚本超时与致命错误处理 — 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复 MySQL 迁移脚本在大表上因默认 30s CommandTimeout 超时导致连接断开、级联失败的问题

**Architecture:** 单一文件改动 `MySqlConnector.cs`，两步修复：(1) 迁移循环前设置 `CommandTimeout=600`（治本）；(2) 连接断开时重连一次，跳过当前脚本，继续后续脚本（治标）

**Tech Stack:** C#, MySQL Connector/NET (MySql.Data), log4net

---

### Task 1: 实现 CommandTimeout + 连接断开重连

**Files:**
- Modify: `OperationGuidance_service/Database/MySqlConnector.cs:75-124`

- [ ] **Step 1: 在迁移循环前设置 CommandTimeout**

在 `MySqlConnector.cs` 第 75 行 `using (MySqlCommand command = conn.CreateCommand())` { 之后，第 76 行 `List<string> fileNames = ...` 之前，加一行：

```csharp
command.CommandTimeout = 600; // 迁移 DDL 在大表上可能耗时数分钟
```

- [ ] **Step 2: 重构 catch 块为三级分类**

将第 92-112 行的 SQL 执行循环的 catch 块替换为三级分类。

**原代码**（第 92-112 行）：
```csharp
                                    foreach (string stmt in fileText.Split(';')) {
                                        string s = stmt.Trim();
                                        if (string.IsNullOrEmpty(s)) continue;
                                        try {
                                            command.CommandText = s;
                                            command.ExecuteNonQuery();
                                        } catch (MySqlException stmtEx) when (
                                            stmtEx.Number == 1060  // Duplicate column name
                                            || stmtEx.Number == 1061  // Duplicate key name
                                            || stmtEx.Number == 1091  // Can't DROP; column/key doesn't exist
                                        ) {
                                            // Idempotency: treat "already exists / already gone" as success.
                                            // MySQL 5.7 does not support ALTER TABLE in PREPARE, so the
                                            // engine must tolerate these errors rather than requiring
                                            // idempotency wrappers in every SQL script.
                                            logger.Info($"Statement in [{fileName}] is a no-op (already applied): {stmtEx.Message}");
                                        } catch (Exception stmtEx) {
                                            logger.Warn($"Statement in [{fileName}] failed: {stmtEx.Message}. SQL: {s.Substring(0, Math.Min(s.Length, 100))}...");
                                            allOk = false;
                                        }
                                    }
```

**改为**：
```csharp
                                    foreach (string stmt in fileText.Split(';')) {
                                        string s = stmt.Trim();
                                        if (string.IsNullOrEmpty(s)) continue;
                                        try {
                                            command.CommandText = s;
                                            command.ExecuteNonQuery();
                                        } catch (MySqlException stmtEx) when (
                                            stmtEx.Number == 1060  // Duplicate column name
                                            || stmtEx.Number == 1061  // Duplicate key name
                                            || stmtEx.Number == 1091  // Can't DROP; column/key doesn't exist
                                        ) {
                                            // Idempotency: treat "already exists / already gone" as success.
                                            // MySQL 5.7 does not support ALTER TABLE in PREPARE, so the
                                            // engine must tolerate these errors rather than requiring
                                            // idempotency wrappers in every SQL script.
                                            logger.Info($"Statement in [{fileName}] is a no-op (already applied): {stmtEx.Message}");
                                        } catch (MySqlException stmtEx) when (
                                            conn.State != ConnectionState.Open
                                        ) {
                                            // Connection-level fatal error (e.g., timeout on large table).
                                            // Break out to try reconnect — skip this script, continue next.
                                            logger.Error($"Connection lost during [{fileName}]: {stmtEx.Message}. SQL: {s.Substring(0, Math.Min(s.Length, 100))}...");
                                            allOk = false;
                                            break;
                                        } catch (Exception stmtEx) {
                                            // Ordinary SQL error (syntax, constraint, etc.) — log and continue.
                                            logger.Warn($"Statement in [{fileName}] failed: {stmtEx.Message}. SQL: {s.Substring(0, Math.Min(s.Length, 100))}...");
                                            allOk = false;
                                        }
                                    }
```

- [ ] **Step 3: 在语句循环后增加重连逻辑**

在 `allOk` 判断之前（原第 113 行前），插入重连检测与重连逻辑：

```csharp
                                    // 连接断开 → 重连一次，跳过当前脚本，继续后续脚本
                                    if (conn.State != ConnectionState.Open) {
                                        try {
                                            command.Dispose();  // 释放死连接上的旧 command
                                            conn.Close();
                                            conn.Open();
                                            command = conn.CreateCommand();  // 创建新 command
                                            command.CommandTimeout = 600;
                                            logger.Info($"Reconnected successfully after connection loss during [{fileName}] — skipping current script, continuing remaining scripts");
                                        } catch (Exception reconnectEx) {
                                            logger.Error($"Reconnect failed after connection loss during [{fileName}]: {reconnectEx.Message}. Requires manual intervention.");
                                            break; // 中止全部脚本
                                        }
                                    }
```

插入位置：内层 `foreach (string stmt ...)` 结束后，`if (allOk)` 之前。

- [ ] **Step 4: 构建验证编译**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

预期：Build succeeded，无编译错误。

- [ ] **Step 5: 代码自查**

检查点：
- `ConnectionState` 通过已有 `using System.Data.Common` 可用（`System.Data.Common` 引用 `System.Data`）
- 新增的 `MySqlException` catch 仅用 `conn.State != ConnectionState.Open` 判断，无 `Number` 依赖
- 重连使用 `conn.Close()` + `conn.Open()` 而非新的 `MySqlConnection`，保持连接池
- 重连前 `command.Dispose()` 释放死连接上的旧 command，避免 using 块结束时泄漏
- `Component.Dispose()` 幂等，极端情况下 using 块 double-dispose 安全
- 重连后创建新 `MySqlCommand` 并设 `CommandTimeout = 600`
- 重连成功：`logger.Info` → 不 `break`，让外层 foreach 自然进入下一个脚本
- 重连失败：`logger.Error` → `break` 跳出外层 foreach
- 重连失败时 `newExecutedSqlFileName` 中已成功的脚本不会被记录，下次启动靠幂等性兜底（安全）
- 当前脚本通过已有的 `allOk = false` 分支记录为 "completed with errors — will retry"，符合语义

---

### 修改总结

| 位置 | 改动 | 行数 |
|------|------|------|
| 第 75 行后 | `command.CommandTimeout = 600;` | +1 |
| 原 catch (Exception) 前 | 新增 `MySqlException` catch（仅 `conn.State != Open`） | +4 |
| 内层 foreach 后 | 重连 + 新 command + 继续/中止 | +9 |
