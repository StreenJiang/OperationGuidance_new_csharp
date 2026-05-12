# 条码跨配方校验 & 错码管理员密码

**日期:** 2026-05-12  
**状态:** 已确认  
**分支:** v2.0.x

## 背景

`ABarCodeInputPopUpForm` 中的产品码/物料码重码校验当前限定在当前配方（mission_id），导致同一产品码在不同配方下可重复加工。同时错码提示仅为可关闭的警告弹窗，没有管理员确认机制。

## 目标

1. 产品码重码校验不再限制当前配方
2. 物料码重码校验不再限制当前配方；产品码已返工时跳过物料重码检查
3. 错码加入管理员密码检查（与重码一样不可手动关闭）
4. 确认物料码重码守卫逻辑正确性

## 设计

### 1. API Request DTO 改造

**`CheckIfBarCodeExistsInMissionRecordReq.MissionId`** — `int` → `int?`

- 传具体值：SQL 保持 `where mission_id = @mission_id`（前置任务检查使用）
- 传 `null`：SQL 跳过 `mission_id` 条件（重码跨配方检查）

**`CheckPartsBarCodeReq.MissionId`** — `int` → `int?`

- 传 `null`：`CheckPartsBarCode` SQL 中，找到 parts_bar_code 后不再限定 mission_id

### 2. 调用点改造

| 文件 | 行号 | 场景 | 改动 |
|---|---|---|---|
| `ABarCodeInputPopUpForm.cs` | 342 | 产品码重码 | `new(mission.id)` → `new(null)` |
| `ABarCodeInputPopUpForm.cs` | 511 | 物料码重码 | `new(_mission.id, barCode)` → `new(null, barCode)` |

### 3. 物料码守卫逻辑（需求 #4 确认）

line 509 守卫：`_workplace.IsRedo != (int)YesOrNo.YES || _mission.is_challenge_mission == (int)YesOrNo.YES`

- 产品码已返工 → 跳过物料重码（正确）
- 挑战任务 → 始终检查（正确）
- 不需要改动

### 4. 错码管理员密码弹窗

涉及 6 处 `ShowWarningPopUp` 调用，改造模式统一为：先 `IsError = true` → 弹 `OpenAdminPasswordPopUpForm(title, allowCancel: false)` → 密码正确后返回，用户重新扫码。

| 方法 | 行号 | 场景 | 弹窗文案 |
|---|---|---|---|
| `ValidateProductBarCodeAsync` | 258 | 已选任务，条码不匹配 | "条码与任务不匹配，请输入管理员密码解锁" |
| `ValidateProductBarCodeAsync` | 287 | 未选任务，匹配不到任务 | "未找到匹配任务，请输入管理员密码解锁" |
| `ValidateProductBarCodeAsync` | 337 | 前置任务未完成 | "前置任务未完成，请输入管理员密码解锁" |
| `ValidatePartsBarCode` | 433 | 物料码为空 | "请扫描物料条码，请输入管理员密码解锁" |
| `ValidatePartsBarCode` | 437 | 物料码重复录入 | "物料重复录入，请输入管理员密码解锁" |
| `ValidatePartsBarCode` | 456 | 物料码不匹配 | "物料与规则不匹配，请输入管理员密码解锁" |

### 5. 不改动

- `CheckIfBarCodeExistsInMissionRecordReq.MissionResult` 不变
- 前置任务检查逻辑不变
- 返工确认流程不变
- SCII 子类 `PartsBarCodeExtraCheck` 不变
- 挑战任务结果记录逻辑不变

## 涉及文件

| 文件 | 改动类型 |
|---|---|
| `OperationGuidance_service/Models/Requests/CheckIfBarCodeExistsInMissionRecordReq.cs` | `MissionId` int→int? |
| `OperationGuidance_service/Models/Requests/CheckPartsBarCodeReq.cs` | `MissionId` int→int? |
| `OperationGuidance_service/Controllers/OperationGuidanceApis.cs` | SQL 条件适配 nullable |
| `OperationGuidance_new/Views/AbstractViews/ABarCodeInputPopUpForm.cs` | 调用点 + 错码弹窗 |
