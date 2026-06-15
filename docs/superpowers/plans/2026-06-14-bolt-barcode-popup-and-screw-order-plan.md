# 点位物料码自动弹窗修复 & 螺丝点位顺序校验跳过 — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复切点位时物料码自动弹窗失效；螺丝点位绑定的物料码跳过顺序校验。

**Architecture:** 三个小改动：(1) `AWorkplaceContentPanel.CheckBoltBoundPartsBarCode` 去异步包装变同步，两个调用路径（手动切+拧紧后自动切）均在 UI 线程；(2) `BarCodeInputPopUpForm_SCII` 加 `_boltBoundRuleIds` 集合，顺序校验时跳过点位绑定的规则；(3) SCII `OpenBarCodePopUpForm` 汇总所有 ProductBolt 的 `parts_bar_code_ids` 传入弹窗。全局物料码仅激活前校验（保持顺序），点位绑定物料码仅激活后校验（跳过顺序），两者不重叠。

**Tech Stack:** C# WinForms (.NET)

---

### Task 1: 修复 CheckBoltBoundPartsBarCode 自动弹窗

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs:2007-2019`

- [ ] **Step 1: 将 async void 改为 void，去 Task.Run + BeginInvoke**

```csharp
// 将第2007-2019行替换为：
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

- [ ] **Step 2: 构建验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs
git commit -m "fix(bolt): remove async wrapper from CheckBoltBoundPartsBarCode to restore auto popup

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2: BarCodeInputPopUpForm_SCII 加螺丝点位跳过逻辑

**Files:**
- Modify: `OperationGuidance_new/Views/ReusableWidgets/BarCodeInputPopUpForm_SCII.cs`

- [ ] **Step 1: 加字段和 SetBoltBoundRuleIds 方法，PartsBarCodeExtraCheck 加跳过逻辑**

```csharp
using CustomLibrary.Utils;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class BarCodeInputPopUpForm_SCII: ABarCodeInputPopUpForm {

        private HashSet<int> _boltBoundRuleIds = new();                                  // 新增

        public BarCodeInputPopUpForm_SCII(AWorkplaceContentPanel workplace,
                string initStr, ProductMissionDTO mission, bool activated,
                Dictionary<int, List<BarCodeMatchingRuleDTO>> productBarCodeRules,
                Dictionary<int, List<BarCodeMatchingRuleDTO>> partsBarCodeRules,
                string? barCode, List<BarCodeMatchingRuleDTO> boltRules, bool isForBolt)
            : base(workplace, initStr, mission, activated, productBarCodeRules, partsBarCodeRules, barCode, boltRules, isForBolt) { }

        public void SetBoltBoundRuleIds(HashSet<int> ruleIds) {                          // 新增
            _boltBoundRuleIds = ruleIds ?? new();
        }

        protected override bool PartsBarCodeExtraCheck(int ruleId) {
            if (!base.PartsBarCodeExtraCheck(ruleId)) {
                return false;
            }

            // 螺丝点位绑定的物料码，跳过顺序校验                                    // 新增
            if (_boltBoundRuleIds.Contains(ruleId)) {                                   // 新增
                return true;                                                            // 新增
            }                                                                           // 新增

            var allValidRules = _partsBarCodeRules[_mission.id]
                                    .Where(rule => !_rulesExcluded.Any(r => r.id == rule.id))
                                    .ToList();

            var currentRule = allValidRules.SingleOrDefault(r => r.id == ruleId);
            if (currentRule == null) {
                return true;
            }

            int expectedIndex = allValidRules.IndexOf(currentRule);

            int savedCount = _workplace.BarCodeObj.PartsMatchingRulesCached
                                .Count(savedId => allValidRules.Any(r => r.id == savedId));

            if (savedCount != expectedIndex) {
                WidgetUtils.ShowWarningPopUp("请按顺序依次录入物料码");
                return false;
            }

            return true;
        }
    }
}
```

- [ ] **Step 2: 构建验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/ReusableWidgets/BarCodeInputPopUpForm_SCII.cs
git commit -m "feat(scii): skip parts barcode order validation for bolt-bound screw points

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 3: SCII OpenBarCodePopUpForm 传入点位绑定规则

**Files:**
- Modify: `OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs:370-375`

- [ ] **Step 1: 在 new BarCodeInputPopUpForm_SCII 前汇总 boltBoundRuleIds 并传入**

找到第371行 `_barCodePopUpForm = new BarCodeInputPopUpForm_SCII(...)` 前插入：

```csharp
                // 汇总所有点位绑定的物料码规则 ID（螺丝点位跳过顺序校验用）
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
```

然后在 `_barCodePopUpForm = new BarCodeInputPopUpForm_SCII(...);` 之后加一行：

```csharp
                _barCodePopUpForm.SetBoltBoundRuleIds(boltBoundRuleIds);
```

完整上下文（第360-375行区域）：

```csharp
            if (_barCodePopUpForm == null || _barCodePopUpForm.IsDisposed) {
                logger.Info($"[SCII:OpenBarCodePopUpForm] Creating new barcode popup form");

                if (_activated && _currentWorkingBolt != null) {
                    _rulesExcluded = GetCurrentExcludedRules(_currentWorkingBolt.BoltDTO);
                    logger.Debug($"[SCII:OpenBarCodePopUpForm] Mission activated, getting excluded rules for current bolt");
                } else {
                    _rulesExcluded = GetCurrentExcludedRules();
                    logger.Debug($"[SCII:OpenBarCodePopUpForm] Mission not activated, getting general excluded rules");
                }

                // 汇总所有点位绑定的物料码规则 ID（螺丝点位跳过顺序校验用）
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

                _barCodePopUpForm = new BarCodeInputPopUpForm_SCII(this, ConfigsVariables.BAR_CODE_NOTE, _mission, _activated,
                        _productBarCodeMatchingRules, _partsBarCodeMatchingRules, barCode, _rulesExcluded, CheckLockMsg(WorkingProcessPanel.LockedBoltBarCode)) {
                    Title = "录入条码",
                    BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
                };
                ((BarCodeInputPopUpForm_SCII)_barCodePopUpForm).SetBoltBoundRuleIds(boltBoundRuleIds);
```

- [ ] **Step 2: 构建验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs
git commit -m "feat(scii): pass bolt-bound rule IDs to barcode popup for screw point order skip

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 4: 最终构建验证

- [ ] **Step 1: 全量构建**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```
