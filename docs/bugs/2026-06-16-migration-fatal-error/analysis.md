# 迁移脚本超时致命错误 — 根因分析

**日期**：2026-06-16
**发现版本**：v1.6.10

## 现象

启动时数据库迁移脚本执行失败，日志中大量 "Connection must be valid and open" 错误。

## 时间线

```
12:06:59,446  INFO   发现未执行脚本 modify_mysql_20260526
     ↓
   恰好 30 秒 — MySQL Connector/NET 默认 CommandTimeout
     ↓
12:07:29,528  WARN   Fatal error encountered during command execution  ← CREATE INDEX 超时
12:07:29,530  WARN   Connection must be valid and open               ← 连接已死 ×15 条
12:07:29,534  WARN   completed with errors — will retry on next startup
```

## 根因（两级因果）

### 第一层：CommandTimeout 不足

`MySqlConnector.cs:96` 未设置 `CommandTimeout`，默认 30 秒。客户 `operation_data` 表数据量大（可能百万行级），`CREATE INDEX` 耗时超过 30 秒 → 超时。

日志中 30 秒间隔是确凿证据。

### 第二层：死连接上级联执行

超时后 MySQL 服务端断开连接。代码的通用 `catch (Exception)` 不区分异常类型，继续在死连接上执行剩余 15 条 SQL → 全部失败 → 级联噪音。

## 影响

- 三个迁移脚本未执行：
  - `modify_mysql_20260526`：6 个索引 + 4 个 datetime 类型迁移
  - `modify_mysql_20260603`：`skip_screw_points` 列
  - `modify_mysql_20260603_2`：`workstation_id` 和 `workstation_name` 列
- **每次启动都重试，但因为 CommandTimeout=30s 未变，会无限超时重试**
- 日志噪音严重影响问题排查

## 关联代码

- `OperationGuidance_service/Database/MySqlConnector.cs:96` — `command.ExecuteNonQuery()` 未设置 CommandTimeout
- `OperationGuidance_service/Database/sqls/modify_mysql_20260526.sql` — 出错的脚本（CREATE INDEX + ALTER TABLE）
- `OperationGuidance_service/Database/AbstractClasses/ADbConnector.cs` — 基类，无超时配置
