# 迁移脚本超时致命错误 — 修复说明

**日期**：2026-06-16
**关联设计文档**：`docs/superpowers/specs/2026-06-16-migration-fatal-error-design.md`

## 修复内容

两个修复，同一文件：

| # | 性质 | 改动 | 位置 |
|---|------|------|------|
| 1 | **治本** | `command.CommandTimeout = 600` | `MySqlConnector.cs:75` |
| 2 | 治标 | 连接断开检测 → 重连一次 → 跳过当前脚本 → 继续 | `MySqlConnector.cs:108` 后 |

## 修复 1：提升 CommandTimeout

```csharp
command.CommandTimeout = 600; // 迁移 DDL 在大表上可能耗时数分钟
```

从默认 30 秒 → 600 秒，覆盖百万行级表的 CREATE INDEX / ALTER TABLE。

## 修复 2：连接断开 → 重连 → 继续

**检测**：`conn.State != ConnectionState.Open` — 仅此一个条件，跨 MySQL 版本一致

**重连流程**：
1. `command.Dispose()` → `conn.Close()` → `conn.Open()` → 创建新 `MySqlCommand` → 设 `CommandTimeout = 600`
2. 成功 → INFO → 跳过当前脚本（标记为待重试）→ 继续下一个脚本
3. 失败 → ERROR（需人工介入）→ 中止全部

## 三级异常分类

| 级别 | 条件 | 日志 | 行为 |
|------|------|------|------|
| 幂等性 | `Number` ∈ {1060, 1061, 1091} | INFO | 视为已应用，继续 |
| 连接致命 | `conn.State != Open` | ERROR + INFO | 重连一次，跳过当前脚本，继续后续 |
| 普通错误 | 其余异常 | WARN | 标记失败，继续 |

## 不入

- 不配置化（当前无配置文件）
- 不异步化（避免 schema 竞态）
- 不改动其他文件
