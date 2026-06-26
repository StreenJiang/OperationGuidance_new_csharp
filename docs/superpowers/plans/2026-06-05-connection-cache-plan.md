# 连接池优化 & 本地数据缓存 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** INSERT + ID 查询合并为一次往返 + 连接池参数调优 + 本地 SQLite 缓存兜底，确保拧紧数据绝不丢失

**Architecture:** 新建 `LocalDataCache` 服务（SQLite 持久化消息队列）+ 修改 `AWrapperBase.Add()` 合并 SQL/事务/重试/失败入队 + 三个 DB 连接器参数调优

**Tech Stack:** C# / SQLite / Newtonsoft.Json / Dapper / MySQL

**Specs:**
- `docs/superpowers/specs/2026-06-05-connection-pool-optimization-design.md`
- `docs/superpowers/specs/2026-06-05-local-data-cache-design.md`

---

## File Map

| File | Action | Responsibility |
|---|---|---|
| `OperationGuidance_service/Services/LocalDataCache.cs` | Create | 本地持久化消息队列 |
| `OperationGuidance_service/Wrapper/AbstractClasses/AWrapperBase.cs` | Modify | 合并 SQL + 事务 + 重试 + 缓存兜底 |
| `OperationGuidance_service/Database/MySqlConnector.cs` | Modify | 连接超时 + 池大小 |
| `OperationGuidance_service/Database/SQLiteConnector.cs` | Modify | 连接超时 |
| `OperationGuidance_service/Database/SqlServerConnector.cs` | Modify | 连接超时 |

---

### Task 1: 创建 LocalDataCache 服务

**Files:**
- Create: `OperationGuidance_service/Services/LocalDataCache.cs`

- [ ] **Step 1: 创建服务类 + pending_data 表**

```csharp
using Newtonsoft.Json;
using System.Data.Common;
using System.Data.SQLite;
using OperationGuidance_service.Attributes;
using OperationGuidance_service.Utils;

namespace OperationGuidance_service.Services {
    [Service]
    public class LocalDataCache {
        private readonly string _dbPath;
        private readonly string _connectionString;
        private int _backoffMs = 0;
        private const int BaseBackoffMs = 500;
        private const int MaxBackoffMs = 30000;
        private const double BackoffMultiplier = 1.5;

        public LocalDataCache() {
            string cacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OperationGuidance", "cache");
            Directory.CreateDirectory(cacheDir);
            _dbPath = Path.Combine(cacheDir, "pending_data.db");
            _connectionString = $"Data source={_dbPath}; UseUTF16Encoding=True; Connection Timeout=5;";
            InitTable();
        }

        private void InitTable() {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            // WAL mode — allows concurrent reads with a single writer (no "database is locked")
            using (var pragma = conn.CreateCommand()) {
                pragma.CommandText = "PRAGMA journal_mode=WAL;";
                pragma.ExecuteNonQuery();
            }
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS pending_data (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    table_name TEXT NOT NULL,
                    entity_json TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    retry_count INTEGER DEFAULT 0
                )";
            cmd.ExecuteNonQuery();
        }

        /// <summary>INSERT 失败时将实体序列化并写入本地缓存。</summary>
        public void Enqueue(string tableName, object entity) {
            string json = JsonConvert.SerializeObject(entity);
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO pending_data (table_name, entity_json, created_at)
                VALUES (@table, @json, @created)";
            AddParameter(cmd, "@table", tableName);
            AddParameter(cmd, "@json", json);
            AddParameter(cmd, "@created", DateTime.Now.ToString("O"));
            cmd.ExecuteNonQuery();
        }

        /// <summary>取出最多 maxCount 条指定表的待重试数据，按入队时间升序。</summary>
        public List<(int Id, string TableName, string EntityJson)> DequeuePending(int maxCount, string tableName) {
            var result = new List<(int, string, string)>();
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, table_name, entity_json FROM pending_data WHERE table_name = @table ORDER BY id ASC LIMIT @limit";
            AddParameter(cmd, "@table", tableName);
            AddParameter(cmd, "@limit", maxCount);
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) {
                result.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
            }
            return result;
        }

        /// <summary>重试成功后删除缓存记录。</summary>
        public void ConfirmDequeued(List<int> ids) {
            if (ids.Count == 0) return;
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"DELETE FROM pending_data WHERE id IN ({string.Join(",", ids)})";
            cmd.ExecuteNonQuery();
        }

        public bool IsEmpty {
            get {
                using var conn = new SQLiteConnection(_connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM pending_data";
                return (long)cmd.ExecuteScalar()! == 0;
            }
        }

        public int Count {
            get {
                using var conn = new SQLiteConnection(_connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM pending_data";
                return Convert.ToInt32((long)cmd.ExecuteScalar()!);
            }
        }

        /// <summary>获取下次 FlushCache 应等待的退避时间，并根据结果更新状态。</summary>
        public int GetBackoffMs() => _backoffMs;

        public void ResetBackoff() => _backoffMs = 0;

        public void IncreaseBackoff() {
            if (_backoffMs == 0) _backoffMs = BaseBackoffMs;
            else _backoffMs = Math.Min((int)(_backoffMs * BackoffMultiplier), MaxBackoffMs);
        }

        private static void AddParameter(SQLiteCommand cmd, string name, object value) {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            cmd.Parameters.Add(p);
        }
    }
}
```

- [ ] **Step 2: 构建验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_service/Services/LocalDataCache.cs
git commit -m "feat(cache): add LocalDataCache service for persistent retry queue

SQLite-backed FIFO queue for failed INSERT operations.
Enqueue serializes entity as JSON, DequeuePending returns up to N items,
ConfirmDequeued removes successfully retried items.
Exponential backoff from 500ms to 30s for repeated failures."
```

---

### Task 2: AWrapperBase — 合并 SQL + 事务 + 重试 + 缓存兜底

**Files:**
- Modify: `OperationGuidance_service/Wrapper/AbstractClasses/AWrapperBase.cs`

- [ ] **Step 0: 新增 using 语句**

在文件顶部 using 区域添加：

```csharp
using Newtonsoft.Json;
using System.Diagnostics;
using OperationGuidance_service.Services;
```

> `Newtonsoft.Json` — `JsonConvert.DeserializeObject<T>` 在 `FlushCache` 中使用。
> `System.Diagnostics` — `Stopwatch` 在 `FlushCache` 的 500ms 超时中使用。
> `OperationGuidance_service.Services` — `LocalDataCache` 在此命名空间，`AWrapperBase` 命名空间为 `Wrapper.AbstractClasses`，需显式引用。

- [ ] **Step 1: 新增 fastCommandTimeout 常量**

在 line 23 (`commandTimeout` 定义处) 后面添加：

```csharp
        private const int commandTimeout = 10;
        private const int fastCommandTimeout = 3;   // INSERT / ID 查询等简单操作

        // QueryFirstWithRetry 方法（lines 462-488）删除——Add() 改用 ExecuteScalar<int> 后无调用方
```

- [ ] **Step 2: 新增 _localCache 字段及其初始化**

在类字段区域添加：

```csharp
        private static readonly LocalDataCache _localCache = new();
```

> `static` — 全局唯一实例，所有 Wrapper 共享同一个 SQLite 缓存文件。

- [ ] **Step 3: 新增 FlushCacheAsync 方法**

```csharp
        /// <summary>
        /// Before each Add(), try to flush any pending cached items for this entity type.
        /// Respects backoff — if previous flush timed out, wait before retrying.
        /// </summary>
        private void FlushCache() {
            string tableName = GetTableName();
            if (_localCache.IsEmpty) return;

            int backoff = _localCache.GetBackoffMs();
            if (backoff > 0) {
                Thread.Sleep(backoff);
            }

            var pending = _localCache.DequeuePending(10, tableName);
            if (pending.Count == 0) {
                _localCache.ResetBackoff();
                return;
            }

            var confirmed = new List<int>();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            foreach (var (id, _, entityJson) in pending) {
                if (sw.ElapsedMilliseconds > 500) break; // 500ms 超时
                try {
                    var entity = JsonConvert.DeserializeObject<T>(entityJson);
                    using (var conn = DbConnector.GetConnection()) {
                        string sql = GenerateInsertSql();
                        string combinedSql = sql + "; " + LastInsertIdSql;
                        using (var tx = conn.BeginTransaction()) {
                            conn.ExecuteScalar<int>(combinedSql, entity, tx, commandTimeout: fastCommandTimeout);
                            tx.Commit();
                        }
                    }
                    confirmed.Add(id);
                } catch {
                    break; // 失败就停，下轮再试
                }
            }

            if (confirmed.Count > 0) {
                _localCache.ConfirmDequeued(confirmed);
            }
            if (confirmed.Count < pending.Count) {
                _localCache.IncreaseBackoff();
            } else {
                _localCache.ResetBackoff();
            }
        }
```

- [ ] **Step 4: 重写 Add() — 合并 SQL + 事务 + 全异常重试 + 缓存兜底**

替换 `Add` 方法（lines 46-80）：

```csharp
        public T? Add(T entity) {
            try {
                // 1. 优先清空本地缓存（同线程同步调用，500ms 超时）
                FlushCache();

                string sql = GenerateInsertSql();
                logger.Info("sql: " + sql);
                string idSql = LastInsertIdSql;
                logger.Info("idSql: " + idSql);
                string combinedSql = sql + "; " + idSql;

                if (_conn != null) {
                    // 事务路径：合并 SQL（共享 _conn + _transaction，一次往返）
                    entity.id = _conn.ExecuteScalar<int>(
                        combinedSql, entity, _transaction, commandTimeout: commandTimeout);
                } else {
                    // 非事务路径：合并 SQL + 显式事务 + 全异常重试
                    const int maxRetries = 3;
                    for (int attempt = 0; attempt < maxRetries; attempt++) {
                        try {
                            using (DbConnection conn = DbConnector.GetConnection()) {
                                using (var tx = conn.BeginTransaction()) {
                                    entity.id = conn.ExecuteScalar<int>(
                                        combinedSql, entity, tx, commandTimeout: fastCommandTimeout);
                                    tx.Commit();
                                }
                            }
                            break; // 成功
                        } catch (Exception ex) {
                            logger.Warn($"Add attempt {attempt + 1}/{maxRetries} failed: {ex.Message}");
                            if (attempt < maxRetries - 1) {
                                Thread.Sleep(100 * (attempt + 1)); // 100ms → 200ms
                            } else {
                                throw; // 重试耗尽 → 进入外层 catch → 入缓存
                            }
                        }
                    }
                }

                return entity;
            } catch (Exception e) {
                logger.Error($"Failed to add entity to table {_tabelName}, saving to local cache: {e.Message}", e);
                try {
                    _localCache.Enqueue(GetTableName(), entity);
                    logger.Info($"Entity saved to local cache, table={GetTableName()}, pending={_localCache.Count}");
                } catch (Exception cacheEx) {
                    logger.Error($"CRITICAL: Failed to save entity to local cache: {cacheEx}", cacheEx);
                }
                return null;
            }
        }
```

> 事务路径（`_conn != null`）保持简单合并——调用方（如 `AddProductMission`）已在外层管理事务和重试。
> `TableName(_tabelName)` 需要确认：`_tabelName` 字段是否已有值。若无则用 `GetTableName()` 替代。

- [ ] **Step 5: 构建验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

预期：Build succeeded，0 errors

> 注：`Newtonsoft.Json` 和 `System.Data.SQLite` 是项目已有依赖。`GetTableName()` 是 `AWrapperBase` 已有方法。

- [ ] **Step 6: Commit**

```bash
git add OperationGuidance_service/Wrapper/AbstractClasses/AWrapperBase.cs
git commit -m "fix(orm): merge INSERT+ID query, add transaction+retry+cache fallback

- Combine INSERT+SELECT LAST_INSERT_ID() via ExecuteScalar<int> (one round-trip)
- Works across MySQL/SQLite/SQLServer — Dapper ExecuteScalar handles multi-statement
- Wrap in explicit transaction for atomicity (safe to retry on any exception)
- Retry ALL exceptions 3x with 100ms/200ms backoff (was deadlock-only 5x)
- On final failure, serialize entity to LocalDataCache (SQLite) for async retry
- fastCommandTimeout=3s for INSERT, commandTimeout=10s for complex queries
- FlushCache drains pending cache items before each Add() (500ms timeout, backoff)
- Remove dead code: QueryFirstWithRetry method deleted"
```

---

### Task 3: 连接池参数调优

**Files:**
- Modify: `OperationGuidance_service/Database/MySqlConnector.cs`
- Modify: `OperationGuidance_service/Database/SQLiteConnector.cs`
- Modify: `OperationGuidance_service/Database/SqlServerConnector.cs`

- [ ] **Step 1: MySqlConnector — 连接超时 + 池大小**

替换 line 29 和 33：

```csharp
// 之前
                    "Connection Timeout=2",
// 之后
                    "Connection Timeout=5",
```

```csharp
// 之前
                    "Max Pool Size=200",
// 之后
                    "Max Pool Size=50",
```

- [ ] **Step 2: SQLiteConnector — 连接超时**

替换三处 `Connection Timeout=2` 为 `Connection Timeout=5`：
- line 28: `GetDbConnection()` 首次连接
- line 59: `GetDbConnection()` 重连
- line 131: `GetOuterDbConnection()`

```csharp
// 之前
                    conn = new($"Data source = {dataSource}; UseUTF16Encoding = True; Connection Timeout=2;");
// 之后
                    conn = new($"Data source = {dataSource}; UseUTF16Encoding = True; Connection Timeout=5;");
```

- [ ] **Step 3: SqlServerConnector — 连接超时**

替换两处 `Connect Timeout=2` 为 `Connect Timeout=5`：
- line 27: `GetDbConnection()`
- line 128: `GetOuterDbConnection()`

```csharp
// 之前
                        Connect Timeout=2;
// 之后
                        Connect Timeout=5;
```

- [ ] **Step 4: 构建验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 5: Commit**

```bash
git add OperationGuidance_service/Database/MySqlConnector.cs \
        OperationGuidance_service/Database/SQLiteConnector.cs \
        OperationGuidance_service/Database/SqlServerConnector.cs
git commit -m "fix(db): tune connection pool — timeout 2→5s, max pool 200→50

- Connection Timeout: 2s → 5s (reduce false-positive failures)
- Max Pool Size: 200 → 50 (prevents queue buildup on exhaustion)
- Applied to MySQL, SQLite, and SQLServer connectors"
```

---

### Task 4: 整体验证

- [ ] **Step 1: 完整构建**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 2: 确认所有改动文件**

```bash
git diff --stat HEAD~3
```

预期输出：
```
OperationGuidance_service/Services/LocalDataCache.cs               (new)
OperationGuidance_service/Wrapper/AbstractClasses/AWrapperBase.cs  (modified)
OperationGuidance_service/Database/MySqlConnector.cs               (modified)
OperationGuidance_service/Database/SQLiteConnector.cs              (modified)
OperationGuidance_service/Database/SqlServerConnector.cs           (modified)
```
