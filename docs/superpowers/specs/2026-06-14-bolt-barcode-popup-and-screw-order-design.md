# 点位物料码自动弹窗修复 & 螺丝点位顺序校验跳过

## 背景

SCII 厂区，任务激活后切换点位时：
1. 如果当前点位绑定了物料码，需自动弹窗让操作员扫码 — 目前 LockMsg 生效但弹窗不出现
2. 螺丝点位（ProductBolt，见 `CONTEXT.md`）绑定的物料码不需要按顺序录入

关键事实：
- **全局物料码**（未绑定到任何点位）：只在任务激活前校验，此时需要顺序校验
- **点位绑定物料码**（`parts_bar_code_ids` 中的规则）：只在任务激活后校验，此时无顺序要求
- 弹窗是模态带遮罩的，不存在并发切换点位的问题

## 修改1：修复自动弹窗

### 文件

`OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs`

### 变更

`CheckBoltBoundPartsBarCode`（第2007行）从 `async void` + `Task.Run` + `BeginInvoke` 改为普通同步方法：

```csharp
protected virtual void CheckBoltBoundPartsBarCode(BoltButton boltButton) {
    if (!string.IsNullOrEmpty(boltButton.BoltDTO.parts_bar_code_ids)) {
        List<int> list = CommonUtils.StringToList(boltButton.BoltDTO.parts_bar_code_ids);
        if (!list.All(_barCodeObj.PartsMatchingRulesCached.Contains)) {
            AddLockMsg(WorkingProcessPanel.LockedBoltBarCode);
            OpenBarCodePopUpForm(null);
        }
    }
}
```

### 理由

`ChangeBoltStatusToWorking` 的两条调用路径都在 UI 线程：
- 手动切换：按钮 Click 事件 → UI 线程
- 拧紧后自动切换：`DoAfterRecevingTighteningDataAsync` 本身就在 `StoreTighteningData` 的 `BeginInvoke` 回调内 → UI 线程

原先的 `Task.Run` + `BeginInvoke` 包装不仅多余，还导致 `OpenBarCodePopUpForm`（创建 Form + Show）被推迟到嵌套的 `BeginInvoke` 回调中，与拧紧回调嵌套时弹窗被吞掉。

## 修改2：螺丝点位跳过顺序校验

### 文件

- `OperationGuidance_new/Views/ReusableWidgets/BarCodeInputPopUpForm_SCII.cs`
- `OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs`

### 变更

**2a. `BarCodeInputPopUpForm_SCII`**：增加字段和方法，跳过绑定到点位的物料码的顺序校验。

```csharp
private HashSet<int> _boltBoundRuleIds = new();

public void SetBoltBoundRuleIds(HashSet<int> ruleIds) {
    _boltBoundRuleIds = ruleIds ?? new();
}

protected override bool PartsBarCodeExtraCheck(int ruleId) {
    if (!base.PartsBarCodeExtraCheck(ruleId)) return false;
    if (_boltBoundRuleIds.Contains(ruleId)) return true;  // 螺丝点位物料码，跳过顺序
    // ... 原有逻辑 ...
}
```

**2b. SCII `OpenBarCodePopUpForm`**：创建弹窗前汇总当前任务所有点位的 `parts_bar_code_ids`。

```csharp
HashSet<int> boltBoundRuleIds = new();
if (_mission.ProductSides != null) {
    foreach (var side in _mission.ProductSides) {
        if (side.Bolts != null) {
            foreach (var bolt in side.Bolts) {
                if (!string.IsNullOrEmpty(bolt.parts_bar_code_ids)) {
                    foreach (int id in CommonUtils.StringToList(bolt.parts_bar_code_ids)) {
                        boltBoundRuleIds.Add(id);
                    }
                }
            }
        }
    }
}
_barCodePopUpForm.SetBoltBoundRuleIds(boltBoundRuleIds);
```

### 理由

判断螺丝点位物料码的方式：汇总所有 ProductBolt 的 `parts_bar_code_ids`。这些规则在激活前被排除（在 `GetCurrentExcludedRules` 中），激活后才由弹窗逐个校验。弹窗校验时若命中这些 ID，跳过顺序。

## 影响范围

- 修改1：所有厂区生效。逻辑等价（去掉不必要的异步包装），不影响现有行为。
- 修改2：仅 SCII。`BarCodeInputPopUpForm`（WHYC/GLB/YF）和 `BarCodeInputPopUpForm_TZYX`（TZYX）的 `PartsBarCodeExtraCheck` 均直接返回 `true`，无顺序校验逻辑，不受影响。
