# 本地持久化数据缓存（消息队列模式）

**日期：** 2026-06-05
**版本：** v1.6.x

## 问题

拧紧数据 INSERT 失败时，当前代码 `return null` 直接丢弃数据。用户要求数据绝不丢失，且不阻塞主流程。

## 设计

### 架构

```
拧紧事件 → StoreDataToDatabaseAsync
              ↓
         FlushCache(500ms)
              ↓ 缓存不为空？
              ├─ 清空失败 → 等待 500ms → 超时跳过
              └─ 清空成功 / 超时 / 缓存为空
              ↓
         InsertOrUpdate(MYSQL)
              ↓ 失败？
              ├─ 成功 → return
              └─ 失败 → EnqueueToLocalCache(SQLite)
                          → 数据持久化到本地文件
                          → return（不阻塞）
```

### 组件：`LocalDataCache`

**文件（新建）：** `OperationGuidance_service/Services/LocalDataCache.cs`

```csharp
[Service]
public class LocalDataCache {
    // 缓存目录：{AppData}/OperationGuidance/cache/
    // 数据库：pending_data.db（SQLite）
    
    // 核心方法：
    void Enqueue(string tableName, object entity);       // INSERT 失败时写入本地
    List<PendingItem> DequeuePending(int maxCount);       // 取出待重试项
    void ConfirmDequeued(List<int> ids);                  // 重试成功后删除
    bool IsEmpty { get; }
    int Count { get; }
}
```

### 流程

#### 1. FlushCache（写入前清空缓存）

```csharp
// AWrapperBase.Add() 开头调用
async Task FlushCacheAsync(CancellationToken token = default) {
    if (_localCache.IsEmpty) return;
    
    var pending = _localCache.DequeuePending(10);
    foreach (var item in pending) {
        try {
            // 反序列化 → INSERT → SELECT ID
            var entity = JsonConvert.DeserializeObject(item.EntityJson, entityType);
            _localCache.ConfirmDequeued(item.Id);
        } catch {
            break;  // 失败就停，下轮再试
        }
    }
}
```

#### 2. EnqueueOnFailure（INSERT 失败时入队）

```csharp
// AWrapperBase.Add() catch 块调用
void EnqueueOnFailure(T entity) {
    string json = JsonConvert.SerializeObject(entity);
    _localCache.Enqueue(TableName, json);
}
```

#### 3. 重试策略

| 参数 | 值 | 说明 |
|---|---|---|
| 每次 FlushCache 超时 | 500ms | 500ms 内没处理完就跳过，等下次 |
| 单次取出数量 | 10 条 | 批量处理，避免一次等太久 |
| 连续失败退避 | ×1.5 | 每次超时后下次等待时间 ×1.5，上限 30s |
| 最大等待 | 30 秒 | 退避上限，防止无限增长 |

### 数据库表（SQLite）

```sql
PRAGMA journal_mode=WAL;  -- 允许并发读 + 单一写者，避免 "database is locked"

CREATE TABLE IF NOT EXISTS pending_data (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    table_name TEXT NOT NULL,
    entity_json TEXT NOT NULL,
    created_at TEXT NOT NULL,
    retry_count INTEGER DEFAULT 0
);
```

### 改动范围

| 文件 | 改动 |
|---|---|
| `OperationGuidance_service/Services/LocalDataCache.cs` | **新建** 本地缓存服务 |
| `OperationGuidance_service/Wrapper/AbstractClasses/AWrapperBase.cs` | `Add()` 合并 SQL + 显式事务 + 失败入队 |
| `OperationGuidance_service/Database/MySqlConnector.cs` | `Connection Timeout` 2→5, `Max Pool Size` 200→50 |
| `OperationGuidance_service/Database/SQLiteConnector.cs` | `Connection Timeout` 2→5 |
| `OperationGuidance_service/Database/SqlServerConnector.cs` | `Connection Timeout` 2→5 |

### 与连接池优化的关系

这两个 spec 互补：

| 连接池优化 | 本地缓存 |
|---|---|
| 减少 INSERT 失败概率 | INSERT 失败后的兜底 |
| 合并 SQL + 短超时 + 合理池大小 | JSON 序列化 + SQLite 持久化 |
| 解决"为什么会卡" | 解决"卡了数据不丢" |

## 风险评估

- **低风险**：本地 SQLite 是进程内文件 DB，不依赖网络
- 序列化用 Newtonsoft.Json（项目已有依赖）
- 缓存数据在 INSERT 成功后立即删除，不累积
- SQLite 写入性能 ~ms 级，不影响主流程
