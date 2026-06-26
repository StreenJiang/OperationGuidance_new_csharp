# 数据库连接池优化 & INSERT 合并

**日期：** 2026-06-05
**版本：** v1.6.x

## 问题

拧紧数据写入时偶发 `SELECT LAST_INSERT_ID()` 阻塞 → 连接池耗尽 → 其他线程 `GetConnection()` 超时 → 用户等待 1-2 分钟。

日志关键段：
```
13:06:02.058 INSERT SQL logged
13:06:02.059 idSql: SELECT LAST_INSERT_ID() logged
         ← "Result:" 日志从未出现——卡住了
13:06:06.065 MySqlException: Unable to connect, Timeout expired
```

## 根因

四个因素叠加：

1. **两次往返**：INSERT + `SELECT LAST_INSERT_ID()` 是独立的两次网络往返，连接持有时间翻倍
2. **统一长超时**：`commandTimeout = 10` 秒对所有查询生效，简单 INSERT/ID 查询也最多阻塞 10 秒
3. **连接池过大**：`Max Pool Size=200`，池满时排队的新连接在 `Connection Timeout=2` 秒内建连失败
4. **连接超时过短**：`Connection Timeout=2` 秒，网络抖动或服务器忙时立即假阳性超时

## 修复

### 改动 1：合并 INSERT + ID 查询

**文件：** `OperationGuidance_service/Wrapper/AbstractClasses/AWrapperBase.cs` — `Add` 方法

```csharp
// 之前：两次往返
using (DbConnection conn = DbConnector.GetConnection()) {
    int result = conn.Execute(sql, entity, commandTimeout: commandTimeout);
    entity.id = conn.QueryFirst<int>(idSql, null, commandTimeout: commandTimeout);
}

// 之后：合并为一次往返
string combinedSql = sql + "; " + idSql;
using (DbConnection conn = DbConnector.GetConnection()) {
    entity.id = conn.QuerySingle<int>(combinedSql, entity, commandTimeout: fastCommandTimeout);
}
```

> `LastInsertIdSql` 已是按 DB 分发：MySQL `SELECT LAST_INSERT_ID()` / SQLite `SELECT last_insert_rowid()` / SQLServer `SELECT SCOPE_IDENTITY()`。所有三个 DB 都支持多语句。
> 事务路径（`_conn != null`）也合并，用 `QueryFirstWithRetry` 替换 `ExecuteWithRetry` + `QueryFirstWithRetry` 的两次调用。

### 改动 2：区分 commandTimeout

**文件：** `OperationGuidance_service/Wrapper/AbstractClasses/AWrapperBase.cs`

```csharp
// 新增
private const int fastCommandTimeout = 3;   // INSERT / ID查询等简单操作
// 保留
private const int commandTimeout = 10;       // 复杂查询
```

`Add` 方法中的合并 SQL 用 `fastCommandTimeout`（3 秒）。其他已有查询保持 `commandTimeout`（10 秒）。

### 改动 3：调整连接池和超时

**文件：**
- `OperationGuidance_service/Database/MySqlConnector.cs:29,33`
- `OperationGuidance_service/Database/SQLiteConnector.cs:28,59,131,149`
- `OperationGuidance_service/Database/SqlServerConnector.cs:27,128`

```diff
- "Connection Timeout=2"
+ "Connection Timeout=5"

- "Max Pool Size=200"
+ "Max Pool Size=50"
```

> SQLite 没有连接池概念（纯文件 DB），只改 `Connection Timeout`，不改连接池相关设置。

## 不改的部分

| 项 | 理由 |
|---|---|
| `ExecuteWithRetry` / `QueryFirstWithRetry` 方法 | 合并 SQL 后的重试逻辑更适合内联处理 |
| 其他使用 `commandTimeout` 的查询 | 复杂度不确定，保持 10 秒 |
| SQLite 的连接字符串结构 | SQLite 没有 `Pooling`/`Max Pool Size`，无需修改 |

## 效果

| 指标 | 之前 | 之后 |
|---|---|---|
| INSERT 往返次数 | 2 | 1 |
| 连接最长持有 | 10 秒 | 3 秒 |
| 连接池上限 | 200 | 50 |
| 连接超时 | 2 秒 | 5 秒 |
| 池耗尽时行为 | 排队堆积 → 雪崩 | 快速失败，上层重试 |
