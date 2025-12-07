# 整合待办清单 - OperationGuidance_new

## 文档信息
- **项目**: OperationGuidance_new
- **创建日期**: 2025-12-07
- **最后更新**: 2025-12-07
- **版本**: v1.4
- **用途**: 整合多个分析文档中的重构和优化任务

---

## 概要

### 总览
本整合待办清单合并了以下来源的重构任务：
1. AWorkplaceContentPanel 代码重构计划（9个任务）
2. Tasks 命名空间优化计划（10个任务）
3. 任务4-9重新评估报告（优先级调整）
4. 任务修复总结（完成详情）

### 当前状态
- **总任务数**: 17个独立任务（新增任务12：增强日志记录）
- **已完成**: 5个任务（v1.5）
- **待完成**: 12个任务
- **预计剩余工时**: 68-95小时（新增增强日志记录3小时）

### 任务分布（按阶段）

| 阶段 | 状态 | 任务数 | 工时 |
|------|------|--------|------|
| v1.5 | 已完成 | 5 | ~30h |
| v1.6 | 推荐实施 | 4 | 15-20h |
| v1.7 | 可选实施 | 3 | 22-28h |
| v2.0 | 长期规划 | 5 | 40-60h |

### 当前文件结构（Tasks命名空间）

```
Tasks/
├── Abstracts/                          ✅ 已整理
│   ├── ATaskBase.cs
│   ├── AIoBoxDevice.cs
│   └── DeviceManagerBase.cs            (v1.5新增)
├── Interfaces/                         ✅ 已整理
│   └── IDeviceManager.cs
├── DeviceManagers/                     ✅ 已整理
│   ├── ToolManager.cs
│   ├── CommunicationManager.cs
│   └── SerialPortManager.cs
├── Implementations/                    ✅ 已整理
│   ├── ToolTask.cs
│   ├── CommunicationTask.cs
│   ├── SerialPortTask.cs
│   └── IoBoxTask.cs
├── Initializers/                       ✅ 已整理
│   └── TaskInitializer.cs
└── DeviceTypes/
    ├── IoBoxTypeArm.cs
    ├── IoBoxTypeArranger.cs
    ├── IoBoxTypeSetterSelector.cs      ✅ 已修复异步Bug
    └── IoBoxTypeSetterSelectorPlus.cs
```

---

## 第一阶段: v1.5 - 已完成

### 任务1: 异步任务取消机制
- **状态**: ✅ 已完成
- **提交**: `e45cd61` - feat: implement async task cancellation mechanism
- **文件**: AWorkplaceContentPanel.cs
- **实现内容**:
  - 添加 `CancellationTokenSource` 字段（`_activeMissionCts`, `_backgroundTaskCts`）
  - 重构 `StartLockCheckingTask`、`StartArrangerTask`、`StartSetterSelectorTask`
  - 在所有异步操作中集成取消令牌支持

### 任务2: PSet操作流程优化
- **状态**: ✅ 已完成
- **提交**: `5d9a103` - perf: optimize PSet operation with fast connection check
- **文件**: AWorkplaceContentPanel.cs, PSetRetryStrategy.cs
- **实现内容**:
  - 实现 `PSetRetryStrategy` 类
  - 添加自动重试机制
  - 移除阻塞对话框以改善用户体验

### 任务3: 数据存储竞态条件修复
- **状态**: ✅ 已完成
- **提交**: `b52e948`, `7a17a7e`, `41e7233`, `050dfd6`
- **文件**: AWorkplaceContentPanel.cs
- **实现内容**:
  - 将 `List<TighteningDataVO>` 替换为 `ConcurrentBag<TighteningDataVO>`
  - 使用 `Task.WhenAll` 优化异步任务结构
  - 移除冗余的 `Task.Run` 调用

### 任务4: 修复命名空间拼写错误
- **状态**: ✅ 已完成
- **提交**: `aa1d6de` - feat(Tasks): Fix namespace error and IoBox async bug
- **问题**: 文件夹名拼写错误 `AsbtractClasses` -> `AbstractClasses`
- **修改文件**:
  - `Tasks/AsbtractClasses/` -> `Tasks/AbstractClasses/`
  - `Tasks/AbstractClasses/AIoBoxDevice.cs`
  - `Tasks/AbstractClasses/ATaskBase.cs`
  - 所有引用该命名空间的9个文件
- **结果**: 命名空间错误完全修复，编译通过

### 任务5: 修复IoBox异步逻辑Bug（关键生产Bug）
- **状态**: ✅ 已完成
- **提交**: `aa1d6de` - feat(Tasks): Fix namespace error and IoBox async bug
- **文件**: `Tasks/DeviceTypes/IoBoxTypeSetterSelector.cs`
- **修复的问题**:
  1. while条件写反：`while (ok && ...)` 应为 `while (!ok && ...)`
  2. `async void` 方法转换为 `async Task`
  3. 硬编码魔法数字替换为常量

**修复前（有Bug）**:
```csharp
public virtual async void Reset() {
    // ...
    while (ok && tryTimes < tryMaxTimes) { // BUG: 应该是 !ok
        // ...
    }
}
```

**修复后**:
```csharp
private const int MaxRetryAttempts = 10;
private const int RetryDelayMs = 100;

public virtual async Task ResetAsync(CancellationToken cancellationToken = default) {
    if (_task == null) {
        throw new InvalidOperationException("Task is not initialized");
    }

    string resetCommand = DeviceType.GetResetCommand().GetMessage();
    string result = _task.SendCommand(resetCommand);

    bool success = await WaitForResetCompleteAsync(result, cancellationToken);

    if (!success) {
        throw new TimeoutException($"Reset operation failed after {MaxRetryAttempts} attempts");
    }
}

private async Task<bool> WaitForResetCompleteAsync(string initialResult, CancellationToken cancellationToken) {
    int attempt = 0;

    while (attempt < MaxRetryAttempts && !cancellationToken.IsCancellationRequested) {
        bool writeOk = DeviceType.WriteOk(initialResult);
        int currentStatus = DeviceType.CurrentStatus;

        if (writeOk && currentStatus == 0) {
            return true;
        }

        attempt++;

        if (attempt < MaxRetryAttempts) {
            await Task.Delay(RetryDelayMs, cancellationToken);
        }
    }

    return false;
}

// 向后兼容
public virtual void Reset() {
    try {
        ResetAsync().GetAwaiter().GetResult();
    } catch (AggregateException ex) {
        throw ex.InnerException ?? ex;
    }
}
```

**代码质量改进**:
- 标准异步模式（`async Task`）
- 正确的循环逻辑
- 可配置常量
- 取消令牌支持
- 保持向后兼容
- 完整XML文档注释

---

## 第二阶段: v1.6 - 推荐实施

### 任务6: 性能优化
- **状态**: ⏳ 待完成
- **优先级**: 中
- **工时**: 5-7小时
- **文件**: AWorkplaceContentPanel.cs 多处位置

**优化项目**:

1. **添加查询缓存**（2小时）
```csharp
private Dictionary<int, WorkstationDTO> _workstationCache;
private Dictionary<int, List<BoltButton>> _boltCacheBySide;

protected virtual void InitializeBeforeActivatingMission() {
    _workstationCache = _workstationsDTOs.ToDictionary(dto => dto.id, dto => dto);

    _boltCacheBySide = new Dictionary<int, List<BoltButton>>();
    foreach (ProductSideDTO side in _sides) {
        if (_allBolts.ContainsKey(side.id)) {
            _boltCacheBySide[side.id] = _allBolts[side.id];
        }
    }
}
```

2. **UI更新节流**（2小时）
```csharp
private readonly Throttler _uiUpdateThrottler = new(100);
```

3. **合并BeginInvoke调用**（1-2小时）
```csharp
protected virtual void DoAfterRecevingTighteningDataAsync(TighteningData data, int deviceId) {
    BeginInvoke(() => {
        if (!_activated) return;
        try {
            UpdateTorqueAndAngle(data);
            ProcessTighteningResult(data, deviceId);
        } catch (Exception e) {
            logger.Error($"Error handling tightening data: {e}");
        }
    });
}
```

**预期效果**:
- 查询时间减少30-40%
- UI更流畅
- 资源利用率更高

### 任务7: 简化锁检查逻辑
- **状态**: ⏳ 待完成
- **优先级**: 中低（从中优先级下调）
- **工时**: 4-6小时（从8-10小时下调）
- **文件**: AWorkplaceContentPanel.cs（第156-158行，1605-1680行）

**当前状态**: 锁检查机制已完整实现，支持取消令牌。主要问题是锁条件分散和硬编码逻辑。

**推荐方案**: 渐进式重构，不改变核心逻辑
```csharp
protected void UpdateLockConditions() {
    CheckCurrentPSetForLockMsg();
    CheckAdminConfirmationForLockMsg();
    // 其他锁检查...
}

protected virtual void StartLockCheckingTask() {
    // ... 现有代码 ...
    UpdateLockConditions();
    // ... 现有代码 ...
}
```

**备注**: 可延后到v1.6作为代码清理任务而非核心功能。

### 任务8: 统一设备状态管理
- **状态**: ⏳ 待完成
- **优先级**: 低（从中优先级下调）
- **工时**: 3-4小时（从6-8小时下调）
- **文件**: AWorkplaceContentPanel.cs（第539-942行）

**当前状态**: 设备管理机制已完整实现，每2000ms轮询一次。这在工业环境中是可靠的做法。

**推荐方案**: 仅做小幅优化
- 添加设备状态缓存，避免重复查询
- 将设备检查逻辑提取到独立方法
- 添加设备自动重连机制

**备注**: 标记为v1.6+的"可选优化"项。

### 任务9: 增强错误处理和日志
- **状态**: ⏳ 待完成
- **优先级**: 中
- **工时**: 4-6小时
- **文件**: 所有Task类

**实现方案**:
```csharp
public static class TaskExceptionHandler {
    public static void HandleException(Exception ex, string operation, string deviceInfo) {
        switch (ex) {
            case SocketException se when se.ErrorCode == (int)SocketError.TimedOut:
                Log.Debug($"Socket timeout during {operation} for {deviceInfo}");
                break;
            case SocketException se:
                Log.Warn($"Socket error ({se.ErrorCode}) during {operation} for {deviceInfo}: {se.Message}");
                break;
            case OperationCanceledException:
                Log.Info($"Operation cancelled for {deviceInfo}");
                break;
            default:
                Log.Error($"Unexpected error during {operation} for {deviceInfo}", ex);
                break;
        }
    }
}
```

---

## 第三阶段: v1.7 - 可选实施

### 任务10: 修复异步编程模式（详细分解）
- **状态**: ⏳ 待完成
- **优先级**: 高（但风险也高）
- **工时**: 16-20小时（分解为6个子任务）
- **文件**: 所有Task类

**问题**: 26处async void、嵌套Task.Run、ConnectAsync调用Connect（逻辑混乱）。

**详细实施计划**:

1. **重构ATaskBase基类**（3-4小时）
   - 添加 `CancellationToken` 支持
   - 提供统一的异步连接重试方法
   - 提供优雅关闭机制
   - 文件: Abstracts/ATaskBase.cs

2. **重构TaskInitializer异步模式**（2-3小时）
   - 将 `TaskCheckingLoop()` 改为 `async Task TaskCheckingLoopAsync()`
   - 消除内部 `Task.Run` 嵌套
   - 添加取消令牌支持
   - 文件: Initializers/TaskInitializer.cs

3. **重构ToolTask异步模式**（4-5小时）
   - 将 `RunTask()` 改为 `protected override async Task RunTaskAsync()`
   - 添加 `CancellationToken` 支持
   - 消除 `Task.Run` 嵌套
   - 使用异步Socket操作
   - 文件: Implementations/ToolTask.cs

4. **重构CommunicationTask异步模式**（4-5小时）
   - 相同的异步重构模式
   - 文件: Implementations/CommunicationTask.cs

5. **重构SerialPortTask异步模式**（4-5小时）
   - 相同的异步重构模式
   - 文件: Implementations/SerialPortTask.cs

6. **重构IoBoxTask异步模式**（4-5小时）
   - 相同的异步重构模式
   - 文件: Implementations/IoBoxTask.cs

**风险缓解**:
- 基于分支开发
- 逐个Task类重构，每完成一个立即测试
- 保持向后兼容（保留旧方法但标记为Obsolete）
- 全面集成测试

### 任务11: 完成IoBoxManager集成
- **状态**: 🟡 部分完成（剩余30%）
- **优先级**: 中
- **剩余工时**: 3-4小时（总工时8-10小时）
- **文件**: TaskInitializer.cs, DeviceManagers/IoBoxManager.cs

**当前状态**: DeviceManager架构已实现70%，只需完成IoBox设备集成。

**剩余工作**:
```csharp
// 1. 创建 IoBoxManager
public class IoBoxManager : DeviceManagerBase<IoBoxDTO, IoBoxTask> {
    // 基于 DeviceManagerBase 模式实现
}

// 2. 迁移 IoBox 设备初始化逻辑
// 将 TaskInitializer.cs 第84-87行的IoBox初始化迁移到IoBoxManager

// 3. 完成最终重构
// TaskInitializer.cs 最终减少到 ~150行
```

**备注**: 这是"快速获胜"任务，风险低，收益明确。

### 任务12: 增强日志记录
- **状态**: ⏳ 待完成
- **优先级**: 中
- **工时**: 2-3小时
- **文件**: 所有Task类

**需要实现**:
1. **结构化日志**
   - 使用参数化消息（避免字符串拼接）
   - 统一日志格式
   - 添加操作上下文

2. **操作上下文（OperationContext类）**
   - 跟踪操作生命周期
   - 包含设备ID、操作类型、时间戳
   - 支持嵌套操作

3. **性能计时器**
   - 使用 `Stopwatch` 测量关键操作
   - 记录连接时间、数据处理时间
   - 自动记录性能指标

**实现示例**:
```csharp
public static class TaskLogger {
    public static void LogOperation(string operation, int deviceId, string deviceName) {
        Log.Information("操作 {Operation} 开始 - 设备 {DeviceId} ({DeviceName})",
            operation, deviceId, deviceName);
    }

    public static void LogCompletion(string operation, int deviceId, long elapsedMs) {
        Log.Information("操作 {Operation} 完成 - 设备 {DeviceId} 耗时 {ElapsedMs}ms",
            operation, deviceId, elapsedMs);
    }
}
```

---

## 第四阶段: v2.0 - 长期规划

### 任务13: 引入策略模式处理螺栓切换
- **状态**: ⏳ 待完成
- **优先级**: 低
- **工时**: 8-10小时（因风险评估较高，从6-8小时上调）
- **文件**: AWorkplaceContentPanel.cs（第972行，974-1170行）

**当前状态**: 已区分单设备模式和独立设备模式。螺栓数据结构已按模式分离存储。

**推荐方案**: 添加接口但不改变现有逻辑
```csharp
public interface IBoltSelectionStrategy {
    BoltButton? GetNextBolt();
    BoltButton? GetCurrentBolt();
}

public class LegacyBoltSelectionStrategy : IBoltSelectionStrategy {
    // 基于现有逻辑实现
}
```

**备注**: 延后到v2.0作为架构改进任务。

### 任务14: 统一资源管理和配置
- **状态**: ⏳ 待完成
- **优先级**: 中
- **工时**: 5-7小时
- **文件**: 所有Task类

**实现方案**:
```csharp
namespace OperationGuidance_new.Tasks.Config {
    public static class TaskTimeouts {
        public static TimeSpan ToolHeartbeatInterval { get; } = TimeSpan.FromSeconds(5);
        public static TimeSpan ToolLoopingInterval { get; } = TimeSpan.FromMilliseconds(100);
        public static TimeSpan IoBoxLoopingInterval { get; } = TimeSpan.FromMilliseconds(100);
        public static TimeSpan SerialPortLoopingInterval { get; } = TimeSpan.FromSeconds(5);
        public static TimeSpan CommunicationKeepAliveDelay { get; } = TimeSpan.FromMilliseconds(200);
        public static TimeSpan DefaultReconnectDelay { get; } = TimeSpan.FromMilliseconds(500);
        public static int MaxReconnectAttempts { get; } = 3;
    }
}
```

### 任务15: 单元测试覆盖
- **状态**: ⏳ 待完成
- **优先级**: 低
- **工时**: 分阶段实施
- **文件**: 新建Tests文件夹

**分阶段方案**:

| 阶段 | 范围 | 工时 |
|------|------|------|
| v1.6 | 测试基础设施搭建 | 4h |
| v1.7 | 核心方法测试 | 8h |
| v2.0 | 完整覆盖（UI、集成、端到端） | 20+h |

**覆盖率目标**:
- 核心连接逻辑：100%
- 错误处理场景：80%+
- 异步操作正确性：100%

### 任务16: 高级性能优化
- **状态**: ⏳ 待完成
- **优先级**: 低
- **工时**: 10-15小时
- **文件**: 所有Task类

**优化领域**:
1. 连接池
2. 消息缓冲区对象池
3. 零拷贝数据处理
4. 批量操作

**预期改进**:
- CPU使用率：-20-30%
- 内存使用率：-10-15%
- 连接稳定性：+50%

### 任务17: 文档完善
- **状态**: ⏳ 待完成
- **优先级**: 低
- **工时**: 6-9小时（因范围扩大，从4-6小时上调）
- **文件**: 所有文件

**内容**:
1. 所有公共API的XML注释（3h）
2. 架构文档及图表（2h）
3. 使用示例和最佳实践（1h）
4. 故障排除指南

**备注**: 可由文档工程师协助完成。

---

## 📅 推荐实施时间表

### 第1周 (v1.6 核心任务)
- **Day 1-2**: IoBoxManager（快速获胜） - 3-4小时
- **Day 3-4**: 任务6（性能优化） - 5-7小时
- **Day 5**: 任务9（错误处理） - 2-3小时

### 第2周
- **Day 1-2**: 任务9（错误处理）继续 - 2-3小时
- **Day 3-4**: 任务8（设备状态管理） - 3-4小时
- **Day 5**: 任务7（锁检查逻辑）开始 - 2小时

### 第3周 (v1.7 开始)
- **Day 1-3**: 任务7（锁检查逻辑）完成 - 4-6小时
- **Day 4-5**: 任务10（异步模式）开始 - ATaskBase重构 - 3-4小时

### 第4周
- **Day 1-2**: 任务10（异步模式） - TaskInitializer重构 - 2-3小时
- **Day 3-5**: 任务10（异步模式） - ToolTask重构 - 4-5小时

### 第5周
- **Day 1-2**: 任务10（异步模式） - CommunicationTask重构 - 4-5小时
- **Day 3-4**: 任务10（异步模式） - SerialPortTask重构 - 4-5小时
- **Day 5**: 任务10（异步模式） - IoBoxTask重构 - 4-5小时

### 第6周
- **Day 1-2**: 任务12（增强日志记录） - 2-3小时
- **Day 3-5**: 任务15（单元测试）开始 - 8-10小时

### 后续周
- 任务15（单元测试）继续 - 12-20小时
- 任务13（策略模式） - 8-10小时
- 任务14（配置统一） - 5-7小时
- 任务16（高级性能优化） - 10-15小时
- 任务17（文档完善） - 6-9小时

---

## 成功指标

### 代码质量
- [ ] 生产代码中0个async void方法
- [ ] 100%的Task类支持取消令牌
- [ ] 代码重复减少80%+
- [ ] 所有异常有清晰的处理策略

### 测试覆盖
- [ ] 核心连接逻辑：100%覆盖
- [ ] 错误处理场景：80%+覆盖
- [ ] 异步操作正确性：已验证

### 性能基准
- [ ] 连接建立：< 3秒
- [ ] 内存泄漏：0个检测到
- [ ] 连接成功率：> 99.9%
- [ ] UI响应时间：< 100ms

### 可维护性
- [ ] 新设备类型支持：< 4小时
- [ ] Bug修复解决速度：提升40%
- [ ] 新开发者上手速度：提升50%

---

## 风险评估

### 高风险任务
| 任务 | 风险 | 影响 | 缓解措施 |
|------|------|------|----------|
| 任务10（异步模式） | 系统级连接问题 | 高 | 分支开发，单独测试 |
| 任务13（策略模式） | 核心业务流程中断 | 中 | 延后到v2.0，保持向后兼容 |

### 中风险任务
| 任务 | 风险 | 影响 | 缓解措施 |
|------|------|------|----------|
| 任务8（设备状态） | 竞态条件 | 中 | 线程安全集合，压力测试 |

### 低风险任务
任务6、7、9、11（快速获胜）、14、15、16、17 - 影响局部，采用标准缓解措施。

### 已移除任务
- ~~任务12（Coordinates类）~~ - 空文件已删除，无需实现

---

## 资源分配建议

| 领域 | 精力占比 |
|------|----------|
| v1.6核心任务 | 70% |
| Bug修复与稳定性 | 20% |
| 技术债务清理 | 10% |

---

## 修订历史

### v1.4 (2025-12-07)
- **合并 Tasks_Optimization_TodoList.md**：整合详细技术信息
- **新增任务12（增强日志记录）**：从 Tasks_Optimization 提取，2-3小时
- **详细分解任务10**：拆分为6个子任务（ATaskBase、TaskInitializer、ToolTask、CommunicationTask、SerialPortTask、IoBoxTask）
- **添加文件结构图**：展示Tasks命名空间的当前状态
- **添加6周实施时间表**：详细到每日任务的实施计划
- **总任务数调整**：从16个增加到17个
- **总剩余工时**：68-95小时

### v1.3 (2025-12-07)
- **移除任务12（Coordinates类）**：空文件已删除，无需实现
- **更新任务11状态**：标记为70%完成，剩余30%（创建IoBoxManager）
- **调整工时估计**：总剩余工时从70-100h下调到65-90h
- **更新任务分布**：v1.7阶段任务数从3个减少到2个
- **风险评估更新**：任务11从"中风险"调整为"低风险（快速获胜）"
- **调整实施顺序**：建议优先完成IoBoxManager作为"快速获胜"任务

### v1.2 (2025-12-07)
- 将文档从英文转换为全中文描述
- 保持所有技术内容和代码示例不变

### v1.1 (2025-12-07)
- 整合Task4-9_ReEvaluation_v1.5.md中重新评估的优先级
- 根据重新评估更新工时估计
- 标记任务4-5（命名空间修复、IoBox bug）为已完成，附提交引用
- 添加Tasks_Fix_Summary_v1.0.md中的详细实现代码
- 按阶段重新组织（v1.5已完成、v1.6推荐、v1.7可选、v2.0长期）
- 更新任务编号以反映新的优先级顺序
- 整合自3个源文档

### v1.0 (2025-12-07)
- 初始整合版本
- 合并CodeRefactor_TodoAndSuggestions.md和Tasks_Optimization_TodoList_v1.0.md的任务
- 消除2个重复任务（单元测试和文档）

**已合并文档**:
1. Consolidated_TodoList.md (v1.0)
2. Tasks_Fix_Summary_v1.0.md（合并后已删除）
3. Task4-9_ReEvaluation_v1.5.md（合并后已删除）
4. CodeRefactor_TodoAndSuggestions.md（合并后已删除）
5. Tasks_Optimization_TodoList_v1.0.md（合并后已删除）

---

**文档状态**: 待审核
**下一步**: 从IoBoxManager（任务11剩余30%）开始实施v1.7 - 这是"快速获胜"任务

## 🎯 快速获胜机会

### IoBoxManager（任务11剩余30%）
- **工时**: 3-4小时
- **风险**: 低
- **收益**:
  - 消除TaskInitializer.cs中剩余的重复代码
  - 将TaskInitializer.cs从264行减少到~150行
  - 统一所有设备类型使用DeviceManager模式
  - 为未来添加新设备类型奠定基础

**建议**: 立即开始此任务，作为下一个开发迭代的开端。
