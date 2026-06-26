# Mission 生命周期架构设计

> 设计日期: 2026-06-21  
> 修订日期: 2026-06-21 (审查修订)  
> 设计范围: Mission 从激活到完成的完整生命周期架构  
> 适用平台: 架构级设计，语言无关（C# / Java 均可实现）  
> 设计原则: 抛开现有代码，从业务和流程出发做纯粹架构设计

---

## 1. 设计目标

1. **可扩展的流程编排**: 客户随时可能在任何流程的前后增减校验、数据收集等需求
2. **多客户版本统一**: 通过组合/配置取代当前的继承重写模式
3. **架构级抽象**: 定义清晰的阶段边界、接口契约、流程编排规则，与具体 UI 框架和语言解耦

---

## 2. Mission 状态机

### 2.1 核心关系：工作台就绪 vs 生命周期

工作台就绪是**一次性的外部准备**，不在生命周期内部。切换 Mission 时需要重新执行。

```
┌────────────────────────────────────────────────────────────────┐
│                                                                │
│  [工作台初始化 — 生命周期外部，一次性 / Mission 切换时重执行]       │
│                                                                │
│  选择 Mission → 加载 MissionData → 加载设备 → 加载扫码规则 → READY │
│      ↑                                                         │
│      │ (切换 Mission: 重新走此流程)                               │
│      │ (自循环: 跳过，Mission 不变)                               │
│      │                                                         │
├──────┼─────────────────────────────────────────────────────────┤
│      │            Mission 生命周期 (4 阶段)                      │
│      │                                                         │
│      │   触发信号 → CheckCanActivate → VALIDATION                │
│      │      │                                                    │
│      │      ▼                                                    │
│      │   ACTIVATION                                              │
│      │      │                                                    │
│      │      ▼                                                    │
│      │   OPERATION                                               │
│      │      │                                                    │
│      │      ▼                                                    │
│      │   FINALIZATION ──────(自循环)──→ VALIDATION               │
│      │      │                                                    │
│      │      ▼                                                    │
│      │   生命周期结束（回到 READY，等待切换或下次激活）              │
│      │                                                         │
└──────┴─────────────────────────────────────────────────────────┘
```

**工作台就绪输入**（外部注入，生命周期不负责加载）：

| 输入 | 说明 |
|---|---|
| `missionData` | 已加载的 Mission 完整数据（含螺栓列表、条码规则、max_ng_num 等），生命周期内不可变 |
| `workstationConfig` | 站点配置（工具、力臂、排列机等设备映射） |
| `deviceRegistry` | 已连接设备注册表（引用，非所有权。设备生命周期长于单次生命周期） |
| `shouldSelfLoop` | 自循环开关标志 |

**关键规则**:
- **切换 Mission**：走完整的工作台就绪 + 新生命周期。生命周期进行中禁止切换 Mission（触发按钮禁用/不响应）
- **自循环**：Mission 不变。正常结束(OK) → 跳过工作台就绪，直接回到 VALIDATION。异常结束 → 等待操作员确认后回到 VALIDATION 或结束生命周期

Mission 生命周期划分为 4 个宏观阶段：

| 阶段 | 职责 | 进入 | 退出 |
|---|---|---|---|
| **VALIDATION** | 设备/工作站配置校验、版本特有检查（批头计数器等）。注：条码校验和挑战任务在触发阶段（§2.3）执行 | 收到触发信号 + CheckCanActivate 通过 | 校验通过 → ACTIVATION；校验失败 → FINALIZATION |
| **ACTIVATION** | 螺栓初始化、排列机/套筒选择器信号、PSet 下发、后台任务启动 | 校验通过 | 激活完成 → OPERATION；激活失败 → FINALIZATION |
| **OPERATION** | 主循环：接收拧紧数据、OK/NG 判定、螺栓切换、面切换、反松、管理员确认、排列机/套筒监控 | 激活完成 | 全螺栓 DONE / NG 达上限 / 设备超时 / SkipScrewCheck → FINALIZATION |
| **FINALIZATION** | 统一收尾：取消后台任务、锁定工具、重置状态、数据导出、释放资源、自循环判断 | OPERATION 结束 / 任意阶段异常中断 | 自循环 → VALIDATION；否则生命周期结束 |

**失败路径规则**:
- VALIDATION、ACTIVATION、OPERATION 内部 Capability 返回 `Fail` 或 `Interrupt` 时，引擎跳转到 FINALIZATION
- **FINALIZATION 内部特殊处理**：FINALIZATION Capability 返回 `Fail` → 记录日志，**继续执行**后续 Capability（确保收尾操作完整）。返回 `Interrupt` → 跳过当前子状态，继续下一个子状态。FINALIZATION 不可被外部中断（忽略 `interrupt()`）

**工作台就绪失败路径**：
- 加载 MissionData 失败（数据损坏/不存在）、设备连接失败 → 显示错误状态，**阻止进入 READY 状态**
- 生命周期引擎仅在 READY 后才接收触发信号

### 2.2 阶段内部子状态

#### VALIDATION

```
VALIDATING → PASSED → [→ACTIVATION]
      │
      └──→ FAILED → [→FINALIZATION]
```

#### ACTIVATION

```
PREPARING → BOLTS_INIT → SIGNALS_SENDING → PSET_SENDING → TASKS_STARTING → ACTIVATED
     │           │           │               │                   │
     └── ACTIVATION_FAILED ◄─┴───────────────┴───────────────────┘
                                    │
                              [→FINALIZATION]
```

#### OPERATION

```
AWAITING_TIGHTENING ◄──────────────────────────────────────┐
       │                                                     │
       ▼                                                     │
TIGHTENING_RECEIVED                                          │
       │                                                     │
       ▼                                                     │
JUDGING                                                      │
       │                                                     │
   ┌───┴───┐                                                 │
  OK       NG                                                │
   │        │                                                │
   ▼        ▼                                                │
STORING  STORING                                             │
   │        │                                                │
   ▼        │  (MaxNGCheck → LooseningControl                │
ADVANCING  │   → AdminConfirm → StoreData)                   │
   │        │        │                                       │
   ├────────┤        ├── 重试 → AWAITING_TIGHTENING ──────────┘
   │        │        │
   │        │        └── 终止 (NG达上限) → [→FINALIZATION]
   │        │
   ▼        ▼
   ├── 下一螺栓 (同面) + 下一面

[设备超时 / NG达上限 / SkipScrew] ──► [→FINALIZATION]
                       
ALL_BOLTS_DONE ──► [→FINALIZATION]
```

#### FINALIZATION

```
CLEANING_TASKS → LOCKING_TOOLS → RESETTING_STATE → EXPORTING → RELEASING
                                                                        │
                                                         ┌──────────────┤
                                                         ▼              ▼
                                           shouldSelfLoop=true   shouldSelfLoop=false
                                                         │              │
                                                ┌────────┴──────┐       ▼
                                           正常结束(OK)   异常结束   生命周期结束
                                                │           │
                                                ▼           ▼
                                         直接回到      阻断弹窗
                                      VALIDATION   (操作员确认)
                                                         │
                                                ┌────────┴──────┐
                                               确认          取消
                                                │             │
                                                ▼             ▼
                                          回到           生命周期结束
                                      VALIDATION
```

### 2.3 生命周期触发机制

生命周期触发由三个独立环节组合而成。触发前，工作台必须已处于 READY 状态（Mission 数据、设备、扫码规则全部就绪）。

```
[触发信号] → [条码获取] → [条码校验] → [CheckCanActivate] → 进入 VALIDATION
```

#### 环节 1：触发信号（谁发起）

| 信号来源 | 说明 |
|---|---|
| `OPERATOR_CLICK` | 操作员点击"激活"按钮 |
| `SCANNER_INPUT` | USB 扫码枪扫码，每次扫描后自动触发 |
| `PLC_SIGNAL` | PLC 发送信号（工件到位、工位就绪等） |
| `SELF_LOOP` | 上一个生命周期 FINALIZATION 结束后自动发出 |
| `HTTP_REQUEST` | Web 版本通过 HTTP API 远程触发 |

#### 环节 2：条码获取（从哪里拿到条码）

| 获取方式 | 说明 |
|---|---|
| `MANUAL_INPUT` | 操作员手输或扫码枪（通过 UI 输入框） |
| `PLC_READ` | PLC 从产线读取条码（RFID、条码阅读器等） |
| `HTTP_BODY` | HTTP 请求体中携带条码 |
| `NONE` | 不需要条码，跳过 |

#### 环节 3：条码校验

每次扫码都会触发校验（校验当前录入的全部条码）。校验策略由 Mission 的条码配置决定：

| 校验策略 | 说明 |
|---|---|
| `FULL_MATCH` | 产品追溯码 + 物料码全部匹配 |
| `PRODUCT_ONLY` | 仅校验产品追溯码 |
| `CUSTOM_RULES` | 客户自定义校验规则（如 SCII 扩展规则） |
| `SKIP` | Mission 未配置条码，跳过校验 |

#### CheckCanActivate（激活条件判定）

扫码校验通过后自动调用，操作员点击"激活"也调用同一个逻辑：

```
CheckCanActivate(context):
    ① mission 配置了产品追溯码？ ─── 未录入 → 拒绝，提示扫码
    ② mission 配置了物料码？   ─── 未录入 → 拒绝，提示扫码
    ③ ①+② 都满足 / 无条码配置 ──► 进入 VALIDATION
```

**关键**：扫码本身不直接触发激活。扫码 → 校验当前已录条码 → 校验通过后自动走 CheckCanActivate。点击激活也走同一个 CheckCanActivate。

#### 实际场景组合

```
场景 A — 标准产线（手动扫码）:
  SCANNER_INPUT → MANUAL_INPUT → FULL_MATCH → CheckCanActivate → VALIDATION

场景 B — PLC 全自动产线:
  PLC_SIGNAL → PLC_READ → FULL_MATCH → CheckCanActivate → VALIDATION

场景 C — 无条码工位:
  OPERATOR_CLICK → NONE → SKIP → CheckCanActivate → VALIDATION

场景 D — 自循环 + 扫码（标准自循环）:
  SELF_LOOP → 等待 MANUAL_INPUT → FULL_MATCH → CheckCanActivate → VALIDATION

场景 E — 自循环 + PLC 条码（PLC 自循环）:
  SELF_LOOP → PLC_READ → FULL_MATCH → CheckCanActivate → VALIDATION

场景 F — 自循环 + 无条码:
  SELF_LOOP → NONE → SKIP → CheckCanActivate → VALIDATION

场景 G — PLC 信号 + 手动输入（混合）:
  PLC_SIGNAL → 弹出提示 → MANUAL_INPUT → FULL_MATCH → CheckCanActivate → VALIDATION

场景 H — MES/上位机 HTTP 触发（Web版）:
  HTTP_REQUEST → HTTP_BODY → FULL_MATCH → CheckCanActivate → VALIDATION

场景 I — HTTP 触发 + 无条码（Web版远程激活）:
  HTTP_REQUEST → NONE → SKIP → CheckCanActivate → VALIDATION

场景 J — HTTP 触发 + PLC 条码（Web版混合产线）:
  HTTP_REQUEST → PLC_READ → FULL_MATCH → CheckCanActivate → VALIDATION
```

#### 自循环的本质

自循环（`SELF_LOOP`）只是一种**触发信号**，不等于"无条码直接激活"。FINALIZATION 中 `shouldSelfLoop=true` 时发出该信号，然后：

- 需要条码 → 等待扫码/PLC/HTTP 提供条码 → 校验 → 进入 VALIDATION
- 无条码配置 → 直接进入 VALIDATION

#### 异常阻断

`SELF_LOOP` 触发 + 上一个生命周期异常结束时，先弹出阻断弹窗等待操作员确认，防止异常导致无限自动循环。正常结束（OK）无需确认。

### 2.4 状态转换附着点（Attachment Point）

每个子状态转换提供两个附着点，Capability 可插入任意位置。对于状态转换 `A → B`：

```
         A ─────────────────► B
         │                    │
    Before(A→B)          After(A→B)
    (可阻止转换)         (仅通知, 不阻止)
```

- **Before(A→B)**（之前）：A 即将离开、B 即将进入时触发。可阻止转换——返回 `Fail` 或 `Interrupt`
- **After(A→B)**（之后）：B 已经进入后触发。事后通知/日志——无论成功失败都不影响已发生的转换

**执行规则**（适用于 Before 附着点；After 附着点仅通知，Fail/Interrupt 不阻止转换）：
- 同附着点的多个 Capability 按 `priority` 升序执行，`priority` 相同时按装配清单声明顺序
- 任一 Capability 返回 `Fail` 或 `Interrupt`：**立即停止**后续 Capability 执行
- `Fail` → 触发当前阶段的退出（转到 FINALIZATION）
- `Interrupt` → 直接跳转到 FINALIZATION（跳过当前阶段退出的附着点）
- 返回 `Suspended` → 视为 Skip（附着点不支持挂起）

---

## 3. Pipeline + Capability 管道模型

### 3.1 核心概念

每个宏观阶段内部是一条**可配置的执行管道**。管道由一组按序执行的 **Capability** 组成。Capability 分为两类：

- **管道 Capability**：在阶段管道的特定子状态下执行一次，返回 Pass/Fail/Skip/Interrupt 驱动状态转换
- **持久监控（Persistent Monitor）**：注册在引擎层，由定时器驱动，通过向 inbox 投递定时消息触发执行。不参与管道，不驱动状态转换（除非检测到超时→发 Interrupt 消息）。用于排列机/套筒就位监控等循环检查场景

```
┌──────────────────────────────────────────────────┐
│  STAGE: VALIDATION                                │
│                                                    │
│  Pipeline:                                         │
│    ┌──────────┐  ┌──────────────┐  ┌───────────┐  │
│    │ 工作站配置校验│→│ 批头计数器检查 │→│ 快速完成检查  │  │
│    └──────────┘  └──────────────┘  └───────────┘  │
│                                                    │
│  每个 Capability 返回:                              │
│    Pass ──► 继续下一个                              │
│    Fail ──► 中止管道，返回失败原因，→ FINALIZATION    │
│    Skip ──► 跳过（条件不满足时不执行）               │
│    Interrupt ──► 中断整个 Stage，直跳 FINALIZATION   │
│    Suspended ──► 异步等待，管道暂停                    │
└──────────────────────────────────────────────────┘
```

### 3.2 Capability 接口规范

```
Capability {
    id          : 唯一标识（如 "max_ng_limit_check"）
    name        : 可读名称（如 "NG次数上限检查"）
    stage       : 所属阶段（VALIDATION | ACTIVATION | OPERATION | FINALIZATION）
    attachPoint : 附着位置（如 Before(VALIDATING→PASSED) 或 After(ACTIVATED)）
    priority    : 执行优先级（同附着点多个 Capability 排序用，数字越小越先执行）
    
    // 前置条件：当前上下文是否启用此 Capability
    // 返回 false 时管道自动跳过（Skip），不执行 execute
    // 只检查 Context 状态，不保证外部系统状态不变（乐观检查 + 防御性执行模式）
    precondition(context) → bool
    
    // 执行逻辑（同步返回，不得内部 await 阻断式等待）
    //   - Pass/Fail/Skip/Interrupt: 立即返回，管道继续
    //   - Suspended(reason, resumeToken): 需要异步等待（用户确认/I/O），
    //     引擎暂停当前管道，Actor 循环继续处理其他消息。
    //     等待完成后通过 Resume(resumeToken, result) 消息恢复执行。
    execute(context) → CapabilityResult
    
    // 异常处理：execute 抛出异常时的处理策略
    // 仅处理 execute 内部抛出的未捕获异常，不处理 execute 返回的 Fail/Interrupt
    onError(context, error) → ErrorAction
}
```

### 3.3 返回值类型

```
CapabilityResult =
    | Pass                  // 成功，继续管道中下一个 Capability
    | Fail(reason)          // 失败，中止管道 → 转入 FINALIZATION
    | Skip(reason)          // 条件不满足，跳过此能力，继续下一个
    | Interrupt(reason)     // 中断整个 Stage → FINALIZATION
                            //   如 SkipScrewCheck → FINISHED_OK / 设备超时 → FINISHED_NG
    | Suspended(reason, resumeToken)  // 需要异步等待（用户确认/I/O），管道暂停，Actor 继续

ErrorAction =
    | Abort                 // 终止当前管道，转为 Interrupt
    | Retry(maxTimes)       // 重试当前 Capability（达到最大次数后 Abort）
    | SkipAndContinue       // 记录错误，跳过当前 Capability 继续执行
```

### 3.4 各 Stage 默认 Capability 清单

#### VALIDATION Pipeline

条码校验（ProductBarCodeCheck, PartsBarCodeMatching, ChallengeTaskCheck）在触发阶段执行（§2.3），
不属于 VALIDATION。VALIDATION 只做设备/工作站配置校验和版本特有检查。

```
┌─────────────────────┐  ┌──────────────────┐  ┌──────────────────┐
│ WorkstationConfig   │→│ ScrewBitCounter  │→│ SkipScrewCheck    │→ ...
│ Check               │  │ Check (SCII)     │  │ (SCII)            │
└─────────────────────┘  └──────────────────┘  └──────────────────┘
     ... ──► [全部 Pass] → ACTIVATION
     ... ──► [任一 Fail] → FINALIZATION(reason)
     ... ──► [SkipScrewCheck 返回 Interrupt] → FINALIZATION (FINISHED_OK 快捷路径)
```

#### ACTIVATION Pipeline

```
┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│ CreateMission│→│ PrepareBolts │→│ SendArranger  │→│ SendSetter    │→│ SendPSet      │
│ Record       │  │              │  │ Signal        │  │ Selector      │  │               │
└──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘
     ... ──► ┌──────────────┐  ┌──────────────┐
            │ StartLockCheck│→│ StartArranger │→ ...
            │ Task           │  │ Monitor       │
            └──────────────┘  └──────────────┘
     ... ──► ACTIVATED → OPERATION
```

#### OPERATION Pipeline

每次拧紧数据到达时触发，按子状态分支执行：

```
AWAITING_TIGHTENING ──► TIGHTENING_RECEIVED ──► JUDGING
                                                      │
                                              ┌───────┴───────┐
                                             OK               NG
                                              │                │
                                              ▼                ▼
                                          STORING           STORING
                                              │                │
                                              │                ├──→ 重试 → AWAITING_TIGHTENING
                                              │                │
                                              ▼                ▼
                                          ADVANCING       终止 → ALL_BOLTS_DONE
                                              │
                                              ├─── 下一螺栓 → AWAITING_TIGHTENING
                                              │
                                              ├─── 下一面   → AWAITING_TIGHTENING
                                              │
                                              └─── 全部完成 → ALL_BOLTS_DONE
```

各子状态的 Capability 分配：

| 子状态 | Capability 管道 |
|---|---|
| `TIGHTENING_RECEIVED` | `ReceiveData` |
| `JUDGING` | `OKNGJudge`（或 SCII 的 `OKNGJudgeWithMaxNG`） |
| `STORING` (OK) | `StoreData` |
| `STORING` (NG) | `MaxNGCheck` → `LooseningControl` → `AdminConfirm` → `StoreData` |
| `ADVANCING` | `AdvanceBolt` |

STORING(NG) 管道执行完毕后，`determineTransition` 根据 Context 中的 `ngDecision` 决定路由：重试 → AWAITING_TIGHTENING，或终止 → ALL_BOLTS_DONE。

#### OPERATION 持久监控（Persistent Monitor — 引擎层定时驱动，不属于阶段 Pipeline）

```
引擎层定时器 (100ms) → 向 inbox 投递 PreconditionCheck 消息
                              │
                              ▼
              ┌──────────────────────────────────────────────┐
              │ DevicePreconditionMonitor(deviceType, ...)   │
              │                                              │
              │   收到消息 → 检查前置设备就位状态:               │
              │     - 未就位 + 未超时 → 更新锁消息 (lockMsgs)   │
              │     - 未就位 + 超时 → 弹窗确认重试 → 达上限     │
              │       → 向 inbox 投递 Interrupt 消息           │
              │     - 已就位 → 移除锁定消息                    │
              │                                              │
              │   当前支持的 deviceType: arranger, setter_selector
              │   新增设备类型: 1) 实现 IPreconditionCheckable   │
              │                 2) 注册定时消息处理即完成        │
              └──────────────────────────────────────────────┘
```

- **DevicePreconditionMonitor**：持久监控，不是管道 Capability。引擎在 OPERATION 阶段启动一个 100ms 定时器，每次 tick 向 inbox 投递 `PreconditionCheck(deviceType)` 消息。收到消息后检查设备状态→更新锁消息。超时耗尽后向 inbox 投递 `Interrupt` 消息触发 FINALIZATION。排列机和套筒选择器各注册一个实例

#### OPERATION 曲线数据旁路（异步回调，不参与主 Pipeline）

```
┌───────────────────────┐
│ HandleCurveData       │
│ (曲线数据异步旁路)      │
└───────────────────────┘
```

- **HandleCurveData**：接收工具控制器推送的拧紧曲线数据（与拧紧数据异步到达），等待 `currentOperationData` 可用后关联 `operation_data_id` 存储。不参与 OK/NG 判定，不影响工具锁定

#### FINALIZATION Pipeline

```
┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│ CancelTasks  │→│ LockTools     │→│ ResetState    │→│ ExportData    │
└──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘
     ... ──► ┌──────────────┐
            │ SelfLoopCheck │→ ...
            └──────────────┘
```

### 3.5 客户版本 = Capability 装配清单

不再通过继承覆盖方法，而是通过**装配清单**声明每个 Stage 启用哪些 Capability。

```
// 注: 条码校验（ProductBarCodeCheck, PartsBarCodeMatching）属于触发阶段（§2.3），
//    在 CheckCanActivate 之前执行，不属于 VALIDATION。
//    ChallengeTaskCheck 也在触发阶段的条码校验环节执行。

标准版 (Base Profile):
  VALIDATION:    [WorkstationConfigCheck]
  ACTIVATION:    [CreateMissionRecord, PrepareBolts, SendArrangerSignal, SendSetterSelectorSignal, 
                  SendPSet, StartLockCheckTask, StartArrangerMonitor]
  OPERATION:     [ReceiveData, OKNGJudge, StoreData, AdvanceBolt,
                  DevicePreconditionMonitor(arranger), DevicePreconditionMonitor(setter_selector),
                  HandleCurveData]
  FINALIZATION:  [CancelTasks, LockTools, ResetState, ExportData, SelfLoopCheck]

SCII Profile:
  继承标准版装配，在以下 Stage 调整:
  VALIDATION:  标准版 + [ScrewBitCounterCheck, SkipScrewCheck]
               // ChallengeTaskCheck 在触发阶段（条码校验环节）
  OPERATION:   标准版 - [OKNGJudge]
               + [OKNGJudgeWithMaxNG, LooseningControl, AdminConfirm]
  
GLB Profile:
  // 注: 新架构中 GLB 装配继承自 SCII——因为 GLB 需要 max_ng_num/反松/管理员确认等能力，
  //    这些在 SCII Profile 中已定义。现有代码中 GLB 继承标准版并独立复制了 SCII 的逻辑，
  //    这正是新架构要消除的重复。
  继承 SCII 装配，在以下 Stage 调整:
  OPERATION:       + [DataCacheForOuterDB]
  FINALIZATION:    - [ExportData]                                    // GLB 禁用标准导出
                   + [StoreToOuterDB, SendPlcJobResult]

YMT Profile:
  继承标准版装配，在以下 Stage 调整:
  FINALIZATION:    - [ExportData]
                   + [YmtDataExportService]
```

**关键原则**: 
- 装配清单在引擎初始化时**展开为完整的** Capability 列表（含优先级排序），运行时不可变。
- 文档中的 `+ [X]` / `- [Y]` 是**描述性记号**，表示该 Profile 相对于父 Profile 的差异。
  实际实现中每个 Profile 存储自己的完整展开列表。
- 新增需求 = 新增一个 Capability 并注册到对应 Stage 的对应附着点。不修改任何现有 Capability 代码。
- Profile 间不建立运行时继承链——父 Profile 更新不影响子 Profile（避免继承脆弱性）。

### 3.6 Capability 粒度原则

一个 Capability 应该是一个**可独立决策的业务判断单元**——内聚相关的检查逻辑，对外暴露出一个明确的判定结果（Pass/Fail/Interrupt）。

- **合**：一个 Capability 可以包含多层内部检查。如 `OKNGJudge` 内部包含 tightening_status 诊断、扭矩范围检查、角度范围检查，只要这些检查构成一个不可拆分的"OK/NG 判定"
- **拆**：当某个内部检查需要被其他客户版本**单独替换**时，它就应该独立为一个 Capability。如 SCII 需要替换标准版的 OK/NG 判定逻辑 → 拆出 `OKNGJudge` 和 `OKNGJudgeWithMaxNG`
- **复用优先**：`AdminConfirm` 覆盖螺栓级和任务级两种确认场景——只做一个动作（弹窗让管理员输密码+记录日志），确认后的后续行为由触发它的 Capability 自行决定。未来新场景也可以复用同一个 `AdminConfirm`

### 3.7 Capability 间数据传递

- **显式数据流**：Pipeline 中相邻 Capability 之间的明确数据通过 Context 的可变字段定义（如 `judgeResult`、`ngDecision`、`currentOperationData`）
- **隐式数据流**：`context.extras{}` 键值存储用于 Capability 间的临时/自定义数据传递，引擎不感知其内容。仅用于装配清单未规划的临时扩展

---

## 4. 生命周期引擎与上下文

### 4.1 生命周期引擎（Lifecycle Engine）

引擎负责驱动状态机流转、调度管道、管理并发和取消。引擎**不持有业务数据**，数据在 Context 中。引擎采用 **Actor 模型**——每个实例是一个 Actor，一次处理一条消息，内部串行。

```
LifecycleEngine {
    capabilities    // Capability 装配清单（客户版本配置）
    shouldSelfLoop  // 自循环开关
    
    // 接收 Context（所有权转移），驱动完整生命周期。
    // 调用者传入 Context 后不应再持有引用——Context 是引擎的私有状态。
    // 生命周期结束后通过返回值获取结果。
    start(context) → LifecycleResult
    
    // 外部中断信号（窗口关闭、用户取消等）
    // FINALIZATION 阶段忽略此信号（关键清理不可中断）
    interrupt(reason)
    
    // 当前是否在生命周期进行中（用于切换 Mission 的门控判断）
    isActive() → bool
    
    // 通用消息入口：表现层适配器和设备层通过此方法向引擎投递消息
    // 内部将消息入队到 Actor inbox，由 onMessage 分发处理
    postMessage(msg: InboundMessage)
}

// 生命周期执行结果
LifecycleResult =
    | Completed(missionResult)  // 生命周期正常结束（FINISHED_OK | FINISHED_NG）
                                //   自循环: 引擎向 inbox 投递 SELF_LOOP 信号后返回 Completed
    | Faulted(error)            // 不可恢复的错误（如 FINALIZATION 自身失败）
```

### 4.2 上下文对象（Context）

Context 是引擎在各阶段之间传递的共享数据载体。分为**不可变部分**（外部注入，生命周期内只读）和**可变部分**（运行时状态）。

```
MissionContext {
    // === 不可变部分（生命周期外部注入） ===
    missionData       // 已加载的 Mission 完整数据（含螺栓配置列表、产品信息、max_ng_num 等）
                      //   missionData.bolts[i] 包含: boltId, torque_min/max, angle_min/max,
                      //   pset_id, arranger_id, setter_selector_id, bit_specification, ...
    workstationConfig // 站点设备配置
    capabilityProfile // 当前 Capability 装配清单（不可变引用）
    deviceRegistry    // 设备注册表引用（非所有权，设备生命周期长于单次生命周期）

    // === 可变部分（运行时状态） ===
    currentStage      // 当前所处宏观阶段
    currentSubState   // 当前子状态
    workplaceStatus   // UNACTIVATED | ACTIVATED | OPERATION_ENABLE | OPERATION_DISABLE
                      //   注: FINISHED_OK/NG 由 missionRecord.missionResult 承载，
                      //   workplaceStatus 仅表示工作台的操作锁定状态
    
    boltStates[]      // 螺栓运行时状态（按 boltId 关联 missionData.bolts）:
                      //   boltId, status(DEFAULT|WORKING|DONE|ERROR), ngTimes, index
                      //   配置属性（torque_min/max 等）一律从 missionData.bolts 读取
    
    currentBoltIndex  // 当前螺栓索引
    currentSideIndex  // 当前面索引
    
    missionRecord     // 本次激活的 MissionRecord（OK/NG 结果）
    tighteningData[]  // 本次生命周期的拧紧数据集合（线程安全，由 Actor 串行化自然保证）
    
    // === 管道执行中间数据 ===
    judgeResult       // 当前拧紧数据的 OK/NG 判定结果
    ngDecision        // 当前 NG 的处理决策（反松/管理员确认/终止）
    currentOperationData  // 当前关联的 OperationData（供曲线数据关联）
    
    // === 回滚信息 ===
    activationCheckpoint // ACTIVATION 阶段的步骤完成情况（每个子步骤完成后记录）
                         // 用于失败时精确回滚（补偿排列机信号、重置已修改的螺栓状态）
    
    // === 信号 ===
    cancellationToken  // 主取消令牌
    interruptRequested // 外部中断标志 — interrupt() 设置，executePipeline 检查
    interruptReason    // 中断原因字符串
    lockMessages{}      // 当前锁定消息集合
    errorInfo{}         // 当前错误信息
    
    // === 扩展存储 ===
    extras{}           // Capability 间自由传递数据的键值存储，引擎不感知内容
}
```

### 4.3 取消令牌管理

```
工作台就绪后创建主令牌 _lifecycleToken

Stage 切换规则:
  VALIDATION → ACTIVATION:      保留令牌
  ACTIVATION → OPERATION:       保留令牌（后台任务绑定此令牌）
  OPERATION → FINALIZATION:     取消令牌 → 等待后台任务排空 → 创建新令牌用于收尾
                                排空策略: 先发停止新请求信号 → 等待当前 StoreData 完成(超时 5s)
  FINALIZATION → VALIDATION (自循环): 创建新的 _lifecycleToken

FINALIZATION 阶段不可中断: 忽略 interrupt() 命令，使用独立的不可取消令牌执行关键清理
```

### 4.4 引擎驱动流程

引擎采用 **Actor 模型**（详见 §6.5），所有外部消息（设备回调、操作员命令）通过 inbox 入队后串行处理。以下伪代码展示 Actor 内部的消息处理逻辑。

```
// === Actor 消息循环入口 ===

onMessage(msg, context):
    switch msg:
        case ActivateMission:
            start(context)
        case Terminate(reason):
            interrupt(reason)
        case Confirm(id, response):
            completePendingConfirmation(id, response)  // 完成 TCS，Resume 消息将随后入队
        case Resume(resumeToken, result):
            resumeCapability(resumeToken, result, context)  // 恢复暂停的 Capability，继续管道
        case TighteningData(data):
            if context.currentStage == OPERATION:
                context.currentOperationData = data        // 存入 Context 供管道 Capability 读取
                context.currentSubState = AWAITING_TIGHTENING
                result = executeStage(OPERATION, context)
                handleStageResult(result, context)         // 处理返回值（可能进入 FINALIZATION）
        case CurveData(data):
            enqueueCurveData(data)
        case CoordinatesReceived(data):
            enqueueCoordinates(data)
        case DeviceDisconnected(deviceId):
            handleDeviceDisconnect(deviceId, context)

// 处理 executeStage 的返回值（跨阶段推进/中断/完成/挂起）
handleStageResult(result, context):
    switch result:
        case Advance(nextStage):
            if nextStage == FINALIZATION:
                context.cancellationToken.Cancel()
                await drainBackgroundTasks(context, timeoutMs: 5000)
                context.cancellationToken = new CancellationToken()
                executeExitAttachments(context.currentStage, context)
                context.currentStage = FINALIZATION
                context.currentSubState = CLEANING_TASKS
                context.interruptRequested = false
                executeEntryAttachments(FINALIZATION, context)
                finalResult = executeStage(FINALIZATION, context)
                // 递归处理 FINALIZATION 的返回值（可能 Suspended 或 Completed）
                handleStageResult(finalResult, context)
            else:
                executeExitAttachments(context.currentStage, context)
                context.currentStage = nextStage
                context.currentSubState = firstSubStateOf(nextStage)
                executeEntryAttachments(nextStage, context)
                subResult = executeStage(nextStage, context)
                handleStageResult(subResult, context)
        case Interrupted(reason):
            context.cancellationToken.Cancel()
            await drainBackgroundTasks(context, timeoutMs: 5000)
            context.cancellationToken = new CancellationToken()
            context.currentStage = FINALIZATION
            context.currentSubState = CLEANING_TASKS
            context.interruptRequested = false
            context.errorInfo = reason
            finalResult = executeStage(FINALIZATION, context)
            handleStageResult(finalResult, context)
        case Suspended:
            return  // 控制权交还 Actor 循环
        case Completed(lifecycleResult):
            if shouldSelfLoop && lifecycleResult.missionResult == OK:
                inbox.enqueue(SelfLoopSignal)
            // 生命周期结束，Actor 等待下一次 ActivateMission
        // ...

// === 生命周期入口 ===

start(context):
    // 被 ActivateMission 消息触发。启动后由 handleStageResult 驱动各阶段推进。
    → context.cancellationToken = new CancellationToken()
    → context.currentStage = VALIDATION
    → context.currentSubState = VALIDATING
    executeEntryAttachments(VALIDATION, context)
    result = executeStage(VALIDATION, context)
    handleStageResult(result, context)   // 处理返回值，递归推进

// === executeStage: 执行当前阶段的完整管道 ===

executeStage(stage, context):
    // 每个阶段内部包含一个或多个子状态。每个子状态有一组 Capability 管道。
    // 管道执行完毕 → 根据结果决定子状态转换 → 触发转换附着点 → 循环或退出。
    
    subState = context.currentSubState
    
    // 1. 执行当前子状态的 Capability 管道
    pipelineResult = executePipeline(stage, subState, context)
    
    // 1b. 如果管道中 Capability 返回 Suspended，向上传播
    if pipelineResult is PipelineSuspended:
        return Suspended(pipelineResult.reason, pipelineResult.resumeToken)
    
    // 2. 根据管道结果和阶段规则，决定子状态转换
    transition = determineTransition(stage, subState, pipelineResult, context)
    
    // 3. 等待子状态（无转换）：挂起，控制权交还 Actor
    //    但 RELEASING 是终端子状态，不走此路径——由步骤 7 的 switch 返回 Completed
    if transition.target == subState && transition.target != RELEASING:
        return Suspended  // 等待下一条消息驱动（如 AWAITING_TIGHTENING 等拧紧数据）
    
    // 4. 执行转换的 Before 附着点（可阻止转换）
    beforeResults = executeAttachmentPoint(Before(subState → transition.target), context)
    if anyInterrupted(beforeResults):
        if stage == FINALIZATION:
            return Completed({ stage: FINALIZATION, ... })
        return Interrupted(collectReasons(beforeResults))
    if anyFailed(beforeResults):
        // Before 失败 → 转为 FAILED 子状态而不是目标子状态
        transition = transitionWithFailed(stage)
    
    // 5. 执行子状态转换
    oldSubState = context.currentSubState
    context.currentSubState = transition.target
    advanceSubState(context, oldSubState, transition.target)
    
    // 6. 执行转换的 After 附着点（通知性，不阻止）
    afterResults = executeAttachmentPoint(After(oldSubState → transition.target), context)
    if anyFailed(afterResults):
        log("After-attachment failure (non-blocking): {collectReasons(afterResults)}")
    
    // 7. 根据转换目标决定返回值
    switch transition.target:
        // 终端子状态 → 跨阶段推进
        case PASSED:           return Advance(ACTIVATION)
        case ACTIVATED:        return Advance(OPERATION)
        case ALL_BOLTS_DONE:   return Advance(FINALIZATION)
        case RELEASING:        return Completed(context.missionRecord.result)
        
        // 失败 → FINALIZATION
        case FAILED, ACTIVATION_FAILED:
            return Advance(FINALIZATION)
        
        // 阶段内非终端子状态 → 同一消息处理中继续推进（递归，类似 FINALIZATION）
        case VALIDATING, PREPARING, BOLTS_INIT, SIGNALS_SENDING, PSET_SENDING, TASKS_STARTING:
            return executeStage(stage, context)
        
        // OPERATION 等待子状态 → Suspended，等待下一条消息驱动
        case TIGHTENING_RECEIVED, JUDGING, STORING, ADVANCING:
            return executeStage(stage, context)  // 子状态链继续（同步推进）
        
        case AWAITING_TIGHTENING:
            return Suspended  // 无拧紧数据，控制权交还 Actor
        
        // FINALIZATION 线性推进
        case CLEANING_TASKS, LOCKING_TOOLS, RESETTING_STATE, EXPORTING:
            return executeStage(FINALIZATION, context)
        
        default:
            return Advance(nextStageFor(stage))

// === executeEntryAttachments: 阶段进入附着点 ===

executeEntryAttachments(stage, context):
    // 触发阶段第一个子状态的进入前附着点
    // 对应 §7 目录中的"阶段首子状态" Before 条目
    // 如: Before(→VALIDATING), Before(→PREPARING), Before(→AWAITING_TIGHTENING), Before(→CLEANING_TASKS)
    firstSubState = firstSubStateOf(stage)
    executeAttachmentPoint(Before(→firstSubState), context)

// === executeExitAttachments: 阶段退出附着点 ===

executeExitAttachments(stage, context):
    // 阶段退出已在 executeStage 步骤 4-6 中处理（子状态转换的 Before/After 附着点）
    // 跨阶段转换 (如 PASSED→ACTIVATION) 的 Before 附着点由 §7 目录中的对应条目覆盖
    // executeExitAttachments 是这些逻辑的外层包装，由 start() 调用
    // 如 OPERATION→FINALIZATION: Before(ALL_BOLTS_DONE→FINALIZATION) 在 §7 中注册
    lastSubState = context.currentSubState
    nextStage = nextStageFor(stage)
    executeAttachmentPoint(Before(lastSubState → nextStage), context)

// === executePipeline: 执行一个子状态下的 Capability 管道 ===

executePipeline(stage, subState, context):
    // 获取属于此 (stage, subState) 的 Capability
    capabilities = getCapabilitiesForSubState(stage, subState, context.capabilityProfile)
    sort(capabilities, by: priority)
    
    // 检查中断（外部 interrupt 设置的标志）
    if context.interruptRequested:
        return PipelineInterrupted(context.interruptReason)
    
    for cap in capabilities:
        if context.interruptRequested:
            return PipelineInterrupted(context.interruptReason)
        
        // 前置条件检查
        if !cap.precondition(context):
            log("Skipped: {cap.id} — precondition not met")
            continue
        
        // 执行 Capability（同步调用，Capability 内部不得阻塞 await）
        try:
            result = cap.execute(context)
        catch Exception e:
            result = handleCapabilityError(cap, e, context)
        
        // 处理结果
        switch result:
            case Pass:     continue
            case Skip(r):  log("Skipped: {cap.id} — {r}"); continue
            case Suspended(reason, resumeToken):
                // Capability 需要异步等待（如 ConfirmationRequired）
                // 管道在此暂停，控制权交还 Actor 循环。
                // 等待完成后通过 Resume(resumeToken, result) 消息恢复。
                return PipelineSuspended(reason, resumeToken, cap)
            case Fail(r):
                if stage == FINALIZATION:
                    log("FINALIZATION Capability Fail (ignored): {cap.id} — {r}")
                    continue
                return PipelineFailed(r)
            case Interrupt(r):
                if stage == FINALIZATION:
                    log("FINALIZATION Capability Interrupt (skip sub-state): {cap.id} — {r}")
                    return PipelineSkipSubState
                return PipelineInterrupted(r)
    
    return PipelineAllPassed

// === 错误处理 ===

handleCapabilityError(cap, error, context):
    action = cap.onError(context, error)
    switch action:
        case Abort:
            return Interrupt("{cap.id}: {error.message}")
        case Retry(maxTimes):
            // 首次失败后重试 maxTimes 次（共 1 + maxTimes 次尝试）
            for retry in 1..maxTimes:
                try:
                    result = cap.execute(context)
                    return result    // 成功：立即返回
                catch:
                    continue         // 失败：继续下一轮
            return Fail("{cap.id}: 重试 {maxTimes} 次后仍然失败 — {error.message}")
        case SkipAndContinue:
            context.extras["_error.{cap.id}"] = error.message
            return Skip(error.message)

// === 附着点执行 ===

executeAttachmentPoint(attachmentType, context):
    caps = getCapabilitiesAt(attachmentType, context.capabilityProfile)
    results = []
    for cap in sorted(caps, by: priority):
        result = cap.execute(context)
        results.append(result)
        if result is Suspended:
            log("Attachment Capability Suspended (treated as Skip): {cap.id}")
            continue  // 附着点不支持挂起，视为 Skip
        if result is Fail or Interrupt:
            break  // 立即停止后续 Capability
    return results

// === 子状态转换规则（各阶段不同） ===

determineTransition(stage, subState, pipelineResult, context):
    // 每个阶段有自己的转换表:
    //
    // VALIDATION:
    //   VALIDATING + AllPassed → PASSED      (→ ACTIVATION)
    //   VALIDATING + Failed/Interrupted → FAILED  (→ FINALIZATION)
    //
    // ACTIVATION:
    //   PREPARING + AllPassed → BOLTS_INIT
    //   BOLTS_INIT + AllPassed → SIGNALS_SENDING
    //   SIGNALS_SENDING + AllPassed → PSET_SENDING
    //   PSET_SENDING + AllPassed → TASKS_STARTING
    //   TASKS_STARTING + AllPassed → ACTIVATED   (→ OPERATION)
    //   任何子状态 + Failed/Interrupted → ACTIVATION_FAILED
    //
    // OPERATION: 
    //   AWAITING_TIGHTENING → TIGHTENING_RECEIVED (触发: 拧紧数据到达)
    //   TIGHTENING_RECEIVED → JUDGING (自动)
    //   JUDGING + OK → STORING
    //   JUDGING + NG → STORING
    //   STORING(OK) + Pass → ADVANCING
    //   STORING(NG) + Pass → 读取 context.ngDecision:
    //      重试 → AWAITING_TIGHTENING
    //      终止 → ALL_BOLTS_DONE
    //   ADVANCING: 还有螺栓 → AWAITING_TIGHTENING; 全部完成 → ALL_BOLTS_DONE
    //
    // FINALIZATION:
    //   每个子状态完成后线性前进: CLEANING_TASKS → LOCKING_TOOLS → RESETTING_STATE → EXPORTING → RELEASING
    //   PipelineAllPassed → 前进到下一子状态
    //   PipelineSkipSubState (Interrupt in FINALIZATION) → 跳过当前子状态，前进到下一子状态
    //   Capability Fail → 记录日志，继续执行（不中止，等同于 Pass）
    //   Interrupt 在 FINALIZATION 中的处理由 executePipeline 层完成（返回 PipelineSkipSubState）
    return transition

// === 外部中断 ===

interrupt(reason):
    if context.currentStage == FINALIZATION:
        log("FINALIZATION 阶段忽略 interrupt: {reason}")
        return
    // 直接同步设置标志和取消（不通过 inbox 自发送）
    context.interruptRequested = true
    context.interruptReason = reason
    context.cancellationToken.Cancel()
    // 取消令牌会唤醒 inbox.take(ct)（如果正在等待），
    // 或 executePipeline 在下一个 Capability 前/中检测到取消并返回 PipelineInterrupted
```

---

## 5. 设备与外部集成

### 5.1 设计原则

- 设备是**可发现的服务**，不是引擎的一部分
- 引擎和 Capability 通过统一接口访问设备，不直接依赖具体设备实现
- **设备可用性由两层决定**：配置文件（有什么）+ 运行时开关（是否启用）
- Capability 装配清单不做设备选择——它只定义业务流程，执行时才判断设备状态
- **乐观初始化 + 防御性执行**：工作台就绪时尽可能加载设备，成功后进入 READY。设备就绪后可能断连（时序窗），Capability 执行时必须再次检查设备状态（如 §5.5 的 `arranger == null` 兜底）

### 5.2 设备注册表

生命周期外部初始化，注入 Context（引用，非所有权。设备连接在用户登录期间持续存在）。

```
DeviceRegistry {
    // 按设备类别注册
    registerTool(toolId, ITool)
    registerArm(armId, IArm)
    registerIOBox(ioBoxId, IIOBox)
    registerArranger(arrangerId, IArranger)
    registerBuzzer(buzzerId, IBuzzer)
    registerSerialPort(portId, ISerialPort)
    registerPLC(plcId, IPLC)
    
    // 查询
    getTool(toolId) → ITool
    getArm(armId) → IArm
    // ...
    
    // 批量操作
    getAllTools() → List<ITool>
    lockAllTools()
    resetAllIO()
}
```

### 5.3 通用设备接口契约

```
interface IDevice {
    id()        → DeviceId
    type()      → DeviceType
    connect()   → ConnectionResult
    disconnect()
    isConnected() → Boolean
    onDisconnect(callback)    // 断连回调注册
}

interface ITool extends IDevice {
    sendPSet(psetId, token)     // 下发程序号（可取消）
    sendLock()                   // 锁定
    sendUnlock()                 // 解锁
    sendBarcode(barcode)
    onTighteningData(callback)  // 拧紧数据回调注册
    onCurveData(callback)       // 曲线数据回调注册
}

interface IArranger extends IDevice, IPreconditionCheckable {
    sendSignal(bolt)
}

interface ISetterSelector extends IDevice, IPreconditionCheckable {
    sendSignal(bolt)
}

interface IArm extends IDevice {
    startListening()
    stopListening()
    onCoordinatesReceived(callback)
}

// 前置条件检查接口（供 DevicePreconditionMonitor 使用）
// IArranger 和 ISetterSelector 均扩展此接口
interface IPreconditionCheckable {
    // 检查设备是否满足前置条件（如排列机就位、套筒匹配）
    // 返回: OK / NotReady / Fault
    checkPrecondition() → PreconditionResult
}
```

### 5.4 设备开关

设备开关属于运行时状态，由用户在工作台中切换，存储在 Context 可变部分：

```
MissionContext {
    // ... 已有字段 ...
    
    // === 设备开关（运行时可变，用户/配置控制） ===
    armLocatingEnabled    : Boolean   // 力臂定位开关
    arrangerEnabled       : Boolean   // 排列机开关  
    setterSelectorEnabled : Boolean   // 套筒选择器开关
    autoLockToolEnabled   : Boolean   // 自动锁工具开关
}
```

### 5.5 Capability 执行时的设备判断

```
SendArrangerSignal.execute(ctx):
    arranger = ctx.deviceRegistry.getArranger(ctx.missionData.bolts[ctx.currentBoltIndex].arrangerId)
    
    if arranger == null:
        return Skip("当前螺栓未配置排列机")
    
    if !ctx.arrangerEnabled:
        return Skip("排列机功能未启用")
    
    // 发送信号 ...
    return Pass
```

### 5.6 外部系统出口

数据导出和外部系统通信作为 Capability 接入 FINALIZATION 阶段：

```
// 数据导出
interface IDataExporter {
    id() → String    // 如 "standard_excel", "ymt_daily_aggregate"
    export(missionRecord, tighteningData[], missionData) → ExportResult
}

// 外部数据库（GLB 专用）
interface IOuterDatabase {
    storeBatch(operationData[]) → StoreResult
}

// PLC 通知（GLB 专用）
interface IPLCNotifier {
    sendJobFinished(success: Boolean)
    sendJobResult(result: String)
}
```

---

## 6. 引擎通信模型（平台无关）

### 6.1 核心思路

引擎是独立运行的逻辑核心，通过统一的消息通道与外界交互。外界在 WinForms 中是桌面 UI，在 Web 中是浏览器。

```
                          ┌─────────────────────┐
                          │   Lifecycle Engine    │
                          │   (平台无关)           │
                          │                       │
  ┌──────────┐            │   - 状态机流转         │
  │ 设备层    │◄──回调───►│   - Capability 管道    │
  │(工具/PLC) │            │   - 设备管理           │
  └──────────┘            │                       │
                          │   ◄── 消息通道 ──►    │
                          └──────────┬──────────┘
                                     │
                          ┌──────────┴──────────┐
                          │  表现层适配器         │ ← 平台相关
                          │                      │
                          │  WinForms: 消息→UI线程 │
                          │  Web:     消息→SSE    │
                          │            命令←HTTP   │
                          └──────────────────────┘
```

### 6.2 消息通道

引擎只定义消息语义，不定义传输方式。

**引擎 → 外界（Outbound）：**

```
OutboundMessage =
    | StateChanged(stage, subState, workplaceStatus)   // 状态变更
    | BoltStatusChanged(boltIndex, status)              // 螺栓状态
    | LockMessagesUpdated(messages)                     // 锁定消息
    | TighteningDataUpdated(dataTable)                  // 拧紧数据
    
    | ConfirmationRequired(id, type, message, options)  // 阻断式确认请求
    | Notification(message, level)                      // 非阻断通知
    
    | ExportCompleted(result)                           // 导出完成
```

**外界 → 引擎（Inbound）：**

```
InboundMessage =
    | InboundCommand(                                     // 操作员/外部系统命令
    |     ActivateMission                                 // 激活
    |     Terminate(reason)                               // 终止
    |     Confirm(id, response: String)                   // 确认响应（密码等）
    |     Resume(resumeToken, result)                     // 恢复暂停的 Capability
    |     ToggleSwitch(switch: SwitchName, value: Boolean) // 开关切换
    |   )
    | DeviceEvent(                                        // 设备回调（表现层适配器入队）
    |     TighteningData(data)                            // 拧紧数据到达
    |     CurveData(data)                                 // 曲线数据到达
    |     CoordinatesReceived(data)                       // 力臂坐标
    |     DeviceDisconnected(deviceId)                    // 设备断连
    |   )
```

- `SwitchName` 取值与 Context 的设备开关字段一一对应：`armLocating`、`arranger`、`setterSelector`、`autoLockTool`
- 设备回调来自设备线程，由表现层适配器封装为 `DeviceEvent` 后投递到 inbox

### 6.3 阻断式确认

某些 Capability 要求操作员介入（如管理员密码确认）。`AdminConfirm` Capability 发出 `ConfirmationRequired` 后引擎挂起，等待对应的 `Confirm` 命令。

```
AdminConfirm Capability 执行:
  1. execute() → 创建 TCS → 发出 Outbound: ConfirmationRequired(id="c1", type=ADMIN_PASSWORD, ...)
  2. execute() → 返回 Suspended("waiting_admin_password", resumeToken)
  3. executePipeline → PipelineSuspended → executeStage → Suspended
  4. handleStageResult → Suspended → Actor 循环继续 (不阻塞)
  5. 操作员输入密码 → 外界发送 Inbound: Confirm(id="c1", response="xxx")
  6. completePendingConfirmation → TCS 完成 → 入队 Inbound: Resume(resumeToken, result)
  7. Actor 收到 Resume → resumeCapability 从暂停点恢复管道
```

引擎内部维护 `Map<ResumeToken, { cap, context, nextCapIndex }>`，Capability 返回 Suspended 时保存恢复点，收到 Resume 时从该点继续执行后续 Capability。不设置全局超时——超时策略由各 Capability 根据业务场景自行定义（可通过定时器 + timeout Token 实现）。

### 6.4 平台映射

| 维度 | WinForms | Web |
|---|---|---|
| **Outbound** | BeginInvoke → UI 线程 | SSE 推送 → 浏览器 |
| **Inbound** | 按钮事件 → 直接调用引擎 | HTTP POST → Controller → 引擎 |
| **阻断确认** | 模态弹窗 → 回调 | SSE → 对话框 → HTTP POST Confirm |
| **设备回调** | 设备线程 → 消息入队 | 同左（设备层不变） |

### 6.5 引擎并发模型

引擎采用 **Actor 模型**。每个 Engine 实例是一个 Actor：

- **私有状态**：`start(context)` 调用后 Context 所有权转移给引擎，外部不应再持有引用。引擎通过返回值（`LifecycleResult.Completed`）暴露结果
- **消息驱动**：设备数据、操作员命令都以消息形式到达，Actor 一次处理一条，内部串行
- **挂起释放**：`Capability.execute()` 同步返回 `CapabilityResult`。需要 I/O 或用户确认时返回 `Suspended(reason, resumeToken)`——引擎挂起管道但不阻塞线程，Actor 可继续处理收件箱中其他消息
- **消息队列**：`inbox` 为生产者-消费者队列，`take(ct)` 支持 CancellationToken——引擎取消时立即唤醒，无空闲延迟
- **外部消息入队**：设备回调（`onTighteningData`、`onCurveData`、`onCoordinatesReceived` 等）来自设备线程，**必须入队到 inbox**，不得直接操作 Context。表现层适配器负责将设备回调/操作员命令封装为消息投递到队列
- **无共享**：多个 Engine 实例（多个工作台）各自独立并行，互不干扰
- **FINALIZATION 保护**：进入 FINALIZATION 阶段后忽略 `interrupt()` 命令，确保关键清理操作不被二次中断

```
Engine Actor 循环:
  ┌──────────────────────────────────┐
  │ while (alive):                   │
  │   msg = await inbox.take(token)  │ ← 异步阻塞，等待消息
  │   await process(msg, context)    │ ← 串行处理，await 释放线程
  │                                  │
  │   process 内部:                   │
  │     - 设备数据 → 管道执行          │
  │     - 操作员命令 → 状态机流转       │
  │     - 同步推进子状态链     │
  │     - 需要确认 → 返回 Suspended → Actor 继续 │
  └──────────────────────────────────┘
```

Context 可变部分的读写无需锁——串行化由 Actor 模型保证。需要并发安全的只有 `inbox` 消息队列（生产者-消费者队列，标准实现）。

---

## 7. 生命周期附着点目录

### VALIDATION

| 附着点 (A→B) | 时机 | 说明 | 可插入的 Capability 示例 |
|---|---|---|---|
| `→ VALIDATING` | Before | 验证开始前 | 验证前置准备 |
| `VALIDATING → PASSED` | Before | 判定通过前 | `ScrewBitCounterCheck`（批头计数器）、`SkipScrewCheck`（快捷路径→Interrupt FINALIZATION OK） |
| `VALIDATING → PASSED` | After | 判定通过后 | 校验通过日志 |
| `VALIDATING → FAILED` | Before | 判定失败时 | 失败原因记录、通知推送 |
| `VALIDATING → FAILED` | After | 失败后 | 失败原因展示给操作员 |
| `FAILED → FINALIZATION` | Before | 验证失败进入收尾前 | 验证失败日志（注意：现有代码验证失败不导出，新设计统一走 FINALIZATION 导出——有意的行为变更） |

### ACTIVATION

| 附着点 (A→B) | 时机 | 说明 | 可插入的 Capability 示例 |
|---|---|---|---|
| `→ PREPARING` | Before | 准备阶段前 | NG 计数重置、额外初始化 |
| `PREPARING → BOLTS_INIT` | After | 螺栓初始化后 | 螺栓排序规则自定义 |
| `→ SIGNALS_SENDING` (排列机) | Before | 排列机信号发送前 | 排列机预检查 |
| `→ SIGNALS_SENDING` (套筒) | Before | 套筒选择器信号发送前 | 套筒状态检查 |
| `→ PSET_SENDING` | Before | PSet 下发前 | 程序号校验 |
| `→ PSET_SENDING` | After | PSet 下发后 | 下发状态日志 |
| `→ TASKS_STARTING` | Before | 后台任务启动前 | 力臂初始化、自定义监控任务注册 |
| `→ ACTIVATED` | After | 激活完成 | 通知 MES 系统、自定义上报 |
| `→ ACTIVATION_FAILED` | Before | 激活失败时 | 失败原因详情 |
| `→ ACTIVATION_FAILED` | After | 激活失败后 | 重置部分状态（读取 `activationCheckpoint` 做精确回滚） |
| `ACTIVATED → OPERATION` | Before | 进入拧紧阶段前 | 激活完成确认 |
| `ACTIVATION_FAILED → FINALIZATION` | Before | 激活失败进入收尾前 | 激活失败日志 |

### OPERATION

| 附着点 (A→B) | 时机 | 说明 | 可插入的 Capability 示例 |
|---|---|---|---|
| `→ AWAITING_TIGHTENING` | After | 开始等待数据时 | 工具连接状态快照 |
| `→ TIGHTENING_RECEIVED` | Before | 数据接收时 | 数据格式校验/转换 |
| `→ JUDGING` (OK 路径) | Before | OK 判定前 | 自定义 OK 条件（额外扭矩/角度阈值） |
| `→ JUDGING` (NG 路径) | Before | NG 判定前 | 自定义 NG 诊断规则 |
| `JUDGING → STORING` (OK) | Before | 存储 OK 数据前 | `MaxNGCheck`（NG次数上限）、数据预览 |
| `JUDGING → STORING` (NG) | Before | 存储 NG 数据前 | `LooseningControl`（反松）、`AdminConfirm`（管理员确认，螺栓级和任务级复用同一个 Capability） |
| `STORING → ADVANCING` (OK) | After | 螺栓推进后 | 自定义推进规则（跳螺栓、指定顺序） |
| `→ ADVANCING` (面切换) | Before | 切换面之前 | 面切换通知 |
| `STORING(NG) → AWAITING_TIGHTENING` (重试) | Before | 重试前 | 重试次数记录 |
| `STORING(NG) → ALL_BOLTS_DONE` (终止) | Before | NG达上限终止前 | 管理员确认日志 |
| `→ ALL_BOLTS_DONE` | Before | 全部完成判定 | 最后校验（遗漏检查） |
| `ALL_BOLTS_DONE → FINALIZATION` | Before | 进入收尾阶段前 | 操作完成确认 |

### FINALIZATION

| 附着点 (A→B) | 时机 | 说明 | 可插入的 Capability 示例 |
|---|---|---|---|
| `→ CLEANING_TASKS` | Before | 清理后台任务前 | 等待剩余数据落库 |
| `→ LOCKING_TOOLS` | After | 工具锁定后 | 工具状态确认 |
| `→ RESETTING_STATE` | Before | 状态重置时 | 自定义状态清理、回滚补偿（读取 `activationCheckpoint`） |
| `→ EXPORTING` | Before | 数据导出前 | 数据完整性检查、外部数据库写入 |
| `→ EXPORTING` | After | 数据导出后 | 导出成功通知、文件上传 |
| `→ RELEASING` | After | 资源释放后 | 资源释放确认 |
| `RELEASING → SelfLoop=true` (正常结束) | Before | 自循环跳转前 | 日志记录 |
| `RELEASING → SelfLoop=true` (异常结束) | Before | 阻断弹窗前 | 异常详情组装、通知推送 |

---

## 附录 A: 多设备并行支持（未来方向）

当前设计假设单工具串行拧紧（`currentBoltIndex` 单值，`boltStates[]` 扁平数组）。如需支持多工作站/多工具并行操作同一任务的不同螺栓，需将 Context 扩展为：

- `workstationStates[workstationId]`：每工作站独立的 `currentBoltIndex`、锁消息、错误信息
- `boltStates[boltId].workstationId`：每螺栓归属的工作站
- `DeviceEvent` 消息携带 `workstationId`，引擎按工作站路由到对应的子状态通道

各工作站的 OPERATION 子状态独立推进，互不阻塞。任务级决策（NG 达上限、全部螺栓完成）汇总所有工作站状态后判定。

此方向待产品需求明确后纳入装配清单和 Profile 设计。

---

## 附录 B: NG 概念的层次

文档和代码中 "NG" 出现在三个上下文中，需区分：

| 层次 | 含义 | Context 承载字段 | 说明 |
|---|---|---|---|
| **拧紧数据 NG** | 工具控制器的原始判定 — `TighteningStatus.NG` | `judgeResult` | 拧紧数据 NG **导致**螺栓状态变为 ERROR（因果关系，非等同） |
| **螺栓 NG** | 单螺栓拧紧不合格 — `BoltStatus.ERROR` | `boltStates[i].status` | 拧紧数据 NG 触发，一次 NG 触发反松/管理员确认后可重试 |
| **任务 NG** | 任务整体不合格 — `FINISHED_NG` | `missionRecord.missionResult` | NG 达上限/设备超时/SkipScrew 等导致 |

---

## 附录 C: Web 安全待考虑事项

以下事项当前设计未展开，供后续 Web 版本设计时参考：

| 层次 | 待考虑事项 | 说明 |
|---|---|---|
| 传输安全 | HTTPS | 明文 HTTP 下 Mission 激活/终止可被中间人篡改 |
| 认证 | 请求必须携带身份凭据 | JWT / API Key / mTLS — 需根据部署环境选择 |
| 授权 | 不同命令需要不同角色 | 操作员角色可以激活，管理员角色才能终止/确认 |
| 审计 | 所有 HTTP 触发的操作记录操作者身份 | 与 `missionRecord` 关联 |
| CSRF | 防止跨站请求伪造 | 对浏览器场景必要；MES 系统间 API 可通过 IP 白名单缓解 |
| 消息完整性 | 激活/终止/确认命令防篡改 | 签名/校验和 |
| 重放攻击 | 同一命令不能被重放多次 | 时间戳 + nonce / 幂等键 |
| 超时 | HTTP 请求和 Engine 响应的超时语义 | SSE 连接保活、请求超时重试策略 |
| 身份传递 | InboundCommand 应携带身份上下文 | `ActivateMission { identity: IdentityContext }` 等 |

---

## 附录 D: 术语对照表

| 分析文档名称 | 设计文档名称 | 说明 |
|---|---|---|
| `WorkplaceProcessStatus` | `workplaceStatus` | 工作台操作锁定状态（不含 FINISHED_OK/NG） |
| `IsMissionSelfLoopingModeEnabled` | `shouldSelfLoop` | 自循环开关 |
| `ActivateMissionAutomatically()` | `SELF_LOOP` 触发信号 | 实现方式 vs 抽象概念 |
| `_activeMissionCts` | `context.cancellationToken` | 具体实现 vs 抽象概念 |
| `CheckCanActivateMission()` | `CheckCanActivate` | 激活前置条件门控 |
| `TerminateMission()` | FINALIZATION 阶段 + `interrupt()` | 过程化调用 vs 声明式阶段 |
| `StartLockCheckingTask` | `StartLockCheckTask` Capability | 后台锁检查任务 |
| `StartArrangerTask` | `DevicePreconditionMonitor(arranger)` Capability | 排列机监控任务 |
| `StartSetterSelectorTask` | `DevicePreconditionMonitor(setter_selector)` Capability | 套筒选择器监控任务 |
| `BoltNGConfirmPopUp` / `MissionNGConfirmPopUp` | `AdminConfirm` Capability（复用同一个） | 管理员密码确认 |
| `SkipScrewPoints` | `SkipScrewCheck` Capability | 快速完成快捷路径 |
