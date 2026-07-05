# SendPSet 重试机制日志分析报告

**分析日期**: 2026-06-29
**日志文件**: 2026-06-27.log / 2026-06-28.log / 2026-06-29.log
**分析范围**: `SendPSet` 方法的重试触发、执行及结果

---

## 1. 重试机制架构

`SendPSet`（`AWorkplaceContentPanel.cs:2043`）采用三阶段重试策略：

```
Phase 1: 快速路径
  └─ SendPSetAsync(pset)   // 现有连接，不断连
       ↓ 失败

Phase 2: 自动重试
  └─ RunPsetRetryRound     // RetryStrategy.FixedDelay(N, 0)
       └─ ReconnectAndResendPset × N 次
            ├─ CloseToTriggerReconnectionAsync
            ├─ Connect()              // 重置 _currentPSet = -1
            ├─ 轮询等待连接 (200ms × 50)
            └─ SendPSetAsync(pset)    // 新连接单次尝试
       ↓ 全部失败

Phase 3: 用户交互
  └─ ShowConfirmPopUp → 用户选择重试 → 回到 Phase 2 (循环)
```

**相关源码**:
- `Views/AbstractViews/AWorkplaceContentPanel.cs:2043` — SendPSet 主逻辑
- `Tasks/ToolTask.cs:560` — ReconnectAndResendPset
- `Tasks/ToolTask.cs:473` — SendPSetAsync
- `Utils/RetryStrategy.cs:48` — ExecuteAsync 重试引擎

---

## 2. 三天统计总览

| 指标 | 06-27 | 06-28 | 06-29 |
|------|-------|-------|-------|
| SendPSet 总调用数 | ~3,442 | ~4,072 | ~2,487 |
| 触发重试的 SendPSet | **1** | **2** | **0** |
| 重试尝试总数 | 3 | 15 | 0 |
| 重试成功次数 | **0** | **0** | — |
| 用户交互轮次 | 0 | 3 | 0 |
| 所有失败原因 | PSet timeout | PSet timeout | — |
| 失败率 | 0.029% | 0.049% | 0% |

---

## 3. 逐日详细分析

### 3.1 06-27 — 单次重试失败

**boltNum=8, pset=4 @ 09:00:10**

```
09:00:10.158  SendPSet boltNum=8, pset=4 start
09:00:10.159  Sending PSet 4
09:00:11.183  ⚠ PSet sending timeout (初始快速路径, waited=5, 1000ms)
              ↓ 自动重试触发
09:00:11.237  ReconnectAndResendPset #1 start
09:00:11.238  CloseToTriggerReconnectionAsync (_currentPSet=5)
09:00:13.289  reconnected after 2000ms ✅
09:00:14.308  ⚠ PSet sending timeout (#1)
09:00:14.313  ReconnectAndResendPset #2 start
09:00:14.620  reconnected after 200ms (_currentPSet=-1 ✅)
09:00:15.631  ⚠ PSet sending timeout (#2)
09:00:15.633  ReconnectAndResendPset #3 start
09:00:15.850  reconnected after 200ms
09:00:16.878  ⚠ PSet sending timeout (#3)
              ↓ 自动重试耗尽 → 弹窗 → 用户放弃
09:00:22.789  SendPSet boltNum=8, pset=4 end (总耗时 12.6s)
```

---

### 3.2 06-28 — 两个失败案例

#### 案例 A: boltNum=9, pset=6 @ 08:21:39（最严重）

```
08:21:16.510  PSet sending to 6 result: True (boltNum=6 成功 — 同一pset在23秒前正常)
              ↓
08:21:39.312  SendPSet boltNum=9, pset=6 start
08:21:39.313  Sending PSet 6
08:21:40.356  ⚠ PSet sending timeout (初始快速路径失败)

━━━ 自动重试轮 (3次) ━━━
08:21:40.380  #1: reconnected after 1800ms → timeout
08:21:43.278  #2: reconnected after 200ms  → timeout
08:21:44.621  #3: reconnected after 200ms  → timeout

              ⏱ ~15s 间隔 (用户弹窗)

━━━ 用户重试轮 1 (3次) ━━━
08:22:00.494  #4: reconnected after 200ms → timeout
08:22:01.746  #5: reconnected after 200ms → timeout
08:22:02.991  #6: reconnected after 200ms → timeout

              ⏱ ~24s 间隔 (用户弹窗)

━━━ 用户重试轮 2 (3次) ━━━
08:22:28.284  #7: reconnected after 200ms → timeout
08:22:29.515  #8: reconnected after 200ms → timeout
08:22:30.791  #9: reconnected after 200ms → timeout

              ⏱ ~2s 间隔 (用户弹窗)

━━━ 用户重试轮 3 (3次) ━━━
08:22:34.067  #10: reconnected after 200ms → timeout
08:22:35.295  #11: reconnected after 200ms → timeout
08:22:36.563  #12: reconnected after 200ms → timeout

              ↓ 用户最终放弃
08:22:41.072  SendPSet boltNum=9, pset=6 end (总耗时 ~62s!)
08:22:58.311  PSet sending to 5 result: True (下一个bolt自行恢复，无任务重新激活)
```

#### 案例 B: pset=4 @ 10:14:42

```
10:14:41.746  Sending PSet 4
10:14:42.773  ⚠ PSet sending timeout (初始快速路径失败)
10:14:42.797  ReconnectAndResendPset #1 start
10:14:44.649  reconnected after 1800ms → timeout
10:14:45.671  #2: reconnected after 200ms → timeout
10:14:46.912  #3: reconnected after 600ms, hasRunTask=False → timeout
              ↓ 用户放弃
10:15:36.459  PSet sending to 5 result: True (约48秒后自行恢复)
```

---

### 3.3 06-29 — 零重试

所有 ~2,487 次 SendPSet 调用全部在快速路径成功。无任何失败。

---

## 4. 根因分析

### 4.1 直接原因

每次失败特征完全相同：

```
SendCommand(command) → true        ← TCP 写入成功
  ↓
等待 _psetSentOk (5 × 200ms = 1000ms)
  ↓
_psetSentOk 始终为 false           ← 工具无任何响应
  ↓
PSet sending timeout after 1000ms
```

关键事实：**失败期间完全没有 `PSet sending to X result: True/False` 日志**。这意味着工具不仅没有确认（true），也没有拒绝（false）——它**完全静默**。

### 4.2 排除的假设

| 假设 | 验证方式 | 结论 |
|------|---------|:---:|
| 连接断开导致 | 每次重连都成功，SendCommand 返回 true | ❌ 排除 |
| _currentPSet 缓存短路 | 重连后 `_currentPSet=-1`，不命中缓存 | ❌ 排除 |
| Lock/Unlock 竞态 | **成功案例中存在完全相同的竞态模式**（见 §4.3） | ❌ 排除 |
| PSet 值被工具拒绝 | 工具不返回任何响应帧（连 false 都没有） | ❌ 排除 |

### 4.3 关键反证：Lock/Unlock 竞态在成功案例中同样存在

以下是正常成功的 PSet 案例，时间模式与失败案例几乎一致：

| 时间 | 事件序列 | 间隔 | 结果 |
|------|---------|------|:---:|
| 08:16:05 | Force locking → Unlocking(31ms) → Sending PSet 4 | 106ms | ✅ |
| 08:16:59 | Force locking → Unlocking(55ms) → Sending PSet 4 | 46ms | ✅ |
| 08:15:00 | Force locking → Sending PSet 6 → Unlocking(17ms) | — | ✅ |
| 08:15:39 | Force locking → Sending PSet 7 → Unlocking(38ms) | — | ✅ |

对比失败案例：

| 时间 | 事件序列 | 间隔 | 结果 |
|------|---------|------|:---:|
| 09:00:10 | Force locking → Unlocking(60ms) → Sending PSet 4 | 70ms | ❌ |
| 08:21:39 | Force locking → Unlocking(23ms) → Sending PSet 6 | 84ms | ❌ |
| 10:14:41 | Force locking → Unlocking(35ms) → Sending PSet 4 | 63ms | ❌ |

**成功与失败的 Lock/Unlock 时间模式在同一个区间内，无区分度。Lock/Unlock 翻转是系统正常工作节奏，不是导致 PSet 失败的原因。**

### 4.4 诚实结论

> **工具（拧紧枪 PF6000-OP 192.168.1.91:4545）间歇性进入一种不响应特定 PSet 命令的状态。**
>
> TCP 连接正常、命令已发送、锁状态正常，但工具在响应帧中完全不返回 `pSetSendingOk`（无论是 true 还是 false）。
> 此状态与代码层面的锁状态、连接状态、缓存状态均无关。重连无法解决，重试策略对此无效。
>
> **根本原因在工具端，当前代码层面无法定位。**

### 4.5 代码组件评估

| 组件 | 状态 | 说明 |
|------|:----:|------|
| `RetryStrategy.ExecuteAsync` | ✅ | 正确触发 N 次重试，回调执行无误 |
| `ReconnectAndResendPset` | ✅ | 断连→重连→发 PSet 流程完整 |
| `Connect()` 中 `_currentPSet=-1` | ✅ | 提交 67e1abd 的修复生效 |
| `CloseToTriggerReconnectionAsync` | ✅ | 旧连接正确关闭 |
| `SendPSetAsync` 超时等待 | ✅ | 1000ms 超时检测正确 |
| ReconnectAndResendPset 内部 PSet/Unlock 时序 | ⚠️ | PSet 可能先于 ForceUnlock 发出，但未被证实导致问题 |
| 用户弹窗交互 | ⚠️ | 无重试次数上限 |
| **工具端 (192.168.1.91:4545)** | **❌** | **间歇性不响应 PSet 命令** |

---

## 5. 改进建议

| 优先级 | 建议 | 说明 |
|--------|------|------|
| **高** | 排查工具固件/协议 | 工具为何间歇性对 PSet 命令静默？检查 PF6000-OP 固件版本、是否有已知问题 |
| **中** | 增加诊断日志区分「拒绝」与「静默」 | 当前 `_psetSentOk = pSetSendingOk.Value` 处的日志 `result: {_psetSentOk}` 无法一眼区分 true/false。改为 `result: Accepted` / `result: Rejected`，下次故障时可立即判断工具是否发送了响应帧 |
| **低** | 限制用户弹窗次数 | Phase 3 循环中加计数器，避免用户反复无效重试 |
| **低** | 重试失败后的工具健康检查 | 全部重试失败后触发一次工具状态查询，帮助诊断 |

---

## 6. 附录

### 6.1 故障恢复时间

| 案例 | 最后超时 | 下一成功 PSet | 间隔 | 是否需人工干预 |
|------|---------|-------------|------|:---:|
| 06-27 pset=4 | 09:00:16 | (任务被放弃) | — | — |
| 06-28 pset=6 | 08:22:37 | 08:22:58 | ~20s | 否 |
| 06-28 pset=4 | 10:14:48 | 10:15:36 | ~48s | 否 |

工具均可自行恢复，不需要重新激活任务。

### 6.2 日志检索命令

```bash
# 统计 SendPSet 调用
grep -a "SendPSet boltNum=" *.log | wc -l

# 统计重试触发
grep -a "ReconnectAndResendPset" *.log | wc -l

# 统计超时次数
grep -a "PSet sending timeout" *.log | wc -l

# 提取重试完整时间线
grep -a "ReconnectAndResendPset\|PSet sending timeout\|CloseToTriggerReconnectionAsync\|Sending PSet\|PSet sending to" *.log

# 对比成功与失败的 Lock/Unlock 上下文
grep -a "Force locking\|Unlocking\|Sending PSet\|PSet sending to" *.log
```
