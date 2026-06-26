# PSet 缓存过期修复实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 `_currentPSet` 重置收敛到 `Connect()` 单一入口，消除自动重连路径的缓存过期 bug。

**Architecture:** 在 `Connect()` 方法中加 `_currentPSet = -1`，同时删除 `CloseToTriggerReconnection()` 和 `CloseToTriggerReconnectionAsync()` 中的冗余重置。`Connect()` 是所有重连路径的汇聚点。

**Tech Stack:** C#, WinForms, .NET Framework

## Global Constraints

- MySQL 5.7 兼容（本次不改数据库，无影响）
- 遵循现有代码风格

---

### Task 1: 实现修复

**Files:**
- Modify: `OperationGuidance_new/Tasks/ToolTask.cs:202-206` (Connect 方法)
- Modify: `OperationGuidance_new/Tasks/ToolTask.cs:254-259` (CloseToTriggerReconnection)
- Modify: `OperationGuidance_new/Tasks/ToolTask.cs:260-262` (CloseToTriggerReconnectionAsync)

**Interfaces:**
- Consumes: 无
- Produces: 无（内部重构，公开接口不变）

- [ ] **Step 1: `Connect()` 方法开头加 `_currentPSet = -1;`**

位置：`Connect()` 的 `Interlocked.Exchange` 检查之后、`Task.Run` 之前。

当前代码（line 202-206）：
```csharp
public override void Connect() {
    if (Interlocked.Exchange(ref _connectInProgress, 1) == 1) {
        logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Connect already in progress, skipping");
        return;
    }
    Task.Run(async () => {
```

改为：
```csharp
public override void Connect() {
    if (Interlocked.Exchange(ref _connectInProgress, 1) == 1) {
        logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Connect already in progress, skipping");
        return;
    }
    _currentPSet = -1;  // 重连后缓存失效，强制下次 SendPSetAsync 真实下发
    Task.Run(async () => {
```

- [ ] **Step 2: `CloseToTriggerReconnection()` 删除 `_currentPSet = -1;`**

当前代码（line 254-258）：
```csharp
public void CloseToTriggerReconnection() {
    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Closing connection to trigger reconnection...");
    _currentPSet = -1;  // 连接断开后缓存不可信，下次发送时强制真实下发
    socketClient?.Close();
    socketClient = null;
}
```

改为：
```csharp
public void CloseToTriggerReconnection() {
    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Closing connection to trigger reconnection...");
    socketClient?.Close();
    socketClient = null;
}
```

- [ ] **Step 3: `CloseToTriggerReconnectionAsync()` 删除 `_currentPSet = -1;`**

当前代码（line 260-264）：
```csharp
public async Task CloseToTriggerReconnectionAsync(CancellationToken token = default) {
    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] CloseToTriggerReconnectionAsync start, _currentPSet={_currentPSet}, hasRunTask={_runTaskTask != null}");
    _currentPSet = -1;
    socketClient?.Close();
    socketClient = null;
```

改为：
```csharp
public async Task CloseToTriggerReconnectionAsync(CancellationToken token = default) {
    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] CloseToTriggerReconnectionAsync start, _currentPSet={_currentPSet}, hasRunTask={_runTaskTask != null}");
    socketClient?.Close();
    socketClient = null;
```

- [ ] **Step 4: Build**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```
Expected: 编译成功，无错误无警告。

- [ ] **Step 5: 运行 PSet 相关测试**

```bash
dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj --filter "FullyQualifiedName~ToolTaskPSetTests"
```
Expected: 全部 PASS。

- [ ] **Step 6: Commit**

```bash
git add OperationGuidance_new/Tasks/ToolTask.cs
git commit -m "fix(tooltask): reset _currentPSet on every Connect to prevent stale cache

Move _currentPSet = -1 to Connect() as single reset point, covering all
reconnect paths including TaskCheckingLoop auto-reconnect. Remove redundant
resets from CloseToTriggerReconnection and CloseToTriggerReconnectionAsync.

Co-Authored-By: Claude <noreply@anthropic.com>"
```
