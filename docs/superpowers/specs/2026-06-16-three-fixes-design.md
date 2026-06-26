# Design: SQLite 日志降噪 + ToolTask 握手修复 + 任务列表改名刷新

**日期:** 2026-06-16
**状态:** 待实现
**分支:** v1.6.x

---

## 1. SQLite 表存在检查日志降噪

### 问题

`ConnectionUtils.CheckTableExists` 使用 `information_schema.tables` 作为首选查询。SQLite 不支持 `information_schema`，每次建立连接都抛出异常并产生 WARN 日志（含完整 stacktrace），然后才走 fallback `select 1 from tableName`。日志噪音大。

### 方案

三个数据库 Connector 各自在 `GetDbConnection()` 中内联表存在检查，使用各自数据库的本地查询方式。删除 `ConnectionUtils.CheckTableExists` 两个重载。

| Connector | 查询 |
|-----------|------|
| `SQLiteConnector` | `SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name=@name` |
| `MySqlConnector` | `SELECT COUNT(1) FROM information_schema.tables WHERE table_schema=@db AND table_name=@name` |
| `SqlServerConnector` | 同 MySQL |

`OperationGuidanceApis:1489` 的外库表存在检查一并内联到该方法中。

### 改动文件

- `OperationGuidance_service/Database/SQLiteConnector.cs` — 替换 line 30
- `OperationGuidance_service/Database/MySqlConnector.cs` — 替换 line 42
- `OperationGuidance_service/Database/SqlServerConnector.cs` — 替换 line 33
- `OperationGuidance_service/Utils/ConnectionUtils.cs` — 删除两个 `CheckTableExists` 方法
- `OperationGuidance_service/Controllers/OperationGuidanceApis.cs` — line 1489 改为内联查询

### 验证

SQLite 启动后日志不再出现 `information_schema` 相关的 WARN。

---

## 2. ToolTask PF4000 握手失败修复

### 问题

PF4000 在 TCP socket 连接成功后进行协议握手（发送连接命令 → 检查 MID 响应 → 发送数据使能命令 → 检查 MID 响应）。存在两个缺陷：

1. **异常路径未清理 socket**：内层 catch (line 363) 只打日志不清理 socket，socket 泄漏
2. **握手失败日志不明确**：MID 不匹配时只打 Info 级别 `"Connect response: {mid1}"`，没有说明期望值、实际值及失败原因

### 方案

在 `ToolTask.ConnectToServer()` 中：

1. 内层 catch 增加 `socketClient?.Close(); socketClient = null;`
2. MID 不匹配时日志级别提升为 **Warn**，格式：`"Handshake failed: expected MID 0002/0005, got {mid1}"`
3. `result1 == null` 时 Warn 文本改为 `"No handshake response from device"`
4. data enable 响应检查做同样处理

### 改动文件

- `OperationGuidance_new/Tasks/ToolTask.cs` — `ConnectToServer()` 方法

### 验证

模拟 PF4000 握手失败场景：确认 `Connected` 为 false，日志明确指出握手失败原因。

---

## 3. 任务列表改名原地刷新

### 问题

`MissionListPanel.RefreshMissionBlocks` 的 sameIds=true 路径调用 `blocks[i].Entity = missionDTOs[i]` 更新数据，但 `ProductMissionBlock.Entity` setter 只赋值 `_t = value`，不更新 `_missionName` 和 `_innerButton.Label`。导致改名后卡片文字不变化。

### 方案

`ProductMissionBlock.Entity` setter 在赋值后同步更新显示：

```csharp
set {
    _t = value;
    _missionName = value.name;
    _innerButton.Label = value.name;
    _innerButton.Invalidate();
}
```

### 改动文件

- `OperationGuidance_new/Views/ReusableWidgets/ProductMissionBlock.cs` — `Entity` setter

### 验证

编辑任务名 → 返回列表页 → 列表不滚动、不跳位置，目标卡片的名称变为新值。

---

## 影响范围

三个改动独立，互不依赖，可分别提交。
