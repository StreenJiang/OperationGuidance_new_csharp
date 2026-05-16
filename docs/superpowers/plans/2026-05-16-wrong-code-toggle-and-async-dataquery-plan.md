# Wrong-Code Toggle + Async DataQuery + NG Pset Fix — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add configurable wrong-barcode admin-password toggle, async data-query page loading, and fix NG-termination pset caching bug.

**Architecture:** Three independent changes in shared layers: (1) INI key → MainUtils → AWorkplaceContentPanel → ABarCodeInputPopUpForm for the toggle; (2) VariableSettingsView_SCII for the config UI; (3) DataQueryView/DataQueryView_SCII VisibleToTrue async extraction. Plus a one-line fix in PrepareBeforeActivatingMission for the NG pset bug.

**Tech Stack:** C# WinForms, settings.ini via SettingsFileUtil (kernel32 P/Invoke), Task.Run + CancellationTokenSource + BeginInvoke for async

---

### Task 1: Add INI key for wrong-barcode error prompt

**Files:**
- Modify: `OperationGuidance_new/Configs/IniFileKeys.cs`

- [ ] **Step 1: Add the new key constant**

In `IniFileKeys.cs`, add after the `MissionErrorPromptForArmEnabled` line (line 13):

```csharp
public static string MissionErrorPromptForWrongBarcode => "mission_error_prompt_for_wrong_barcode";
```

- [ ] **Step 2: Verify the file compiles**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj --no-restore`
Expected: Build succeeds (only a new static string property, no consumers yet).

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Configs/IniFileKeys.cs
git commit -m "feat: add MissionErrorPromptForWrongBarcode INI key"
```

---

### Task 2: Add MainUtils read/write/default for wrong-barcode toggle

**Files:**
- Modify: `OperationGuidance_new/Utils/MainUtils.cs`

- [ ] **Step 1: Add the three methods**

In `MainUtils.cs`, add after the `SetErrorPromptForArmEnabled` method (near line 466). Follow the exact pattern used by `IsBuzzerEnabled` / `SetBuzzerEnabled` / `DefaultIsBuzzerEnabled`:

```csharp
// Wrong barcode error prompt (SCII / SCII_XT)
public static bool IsErrorPromptForWrongBarcodeEnabled() {
    string enabled = Settings.Read(IniFileKeys.MissionErrorPromptForWrongBarcode);
    if (string.IsNullOrEmpty(enabled)) {
        bool flag = DefaultIsErrorPromptForWrongBarcodeEnabled();
        SetErrorPromptForWrongBarcodeEnabled(flag);
        return flag;
    }
    return int.Parse(enabled) == (int) YesOrNo.YES;
}
public static bool DefaultIsErrorPromptForWrongBarcodeEnabled() => false;
public static void SetErrorPromptForWrongBarcodeEnabled(bool flag) {
    if (flag) {
        Settings.Write(IniFileKeys.MissionErrorPromptForWrongBarcode, (int) YesOrNo.YES + "");
    } else {
        Settings.Write(IniFileKeys.MissionErrorPromptForWrongBarcode, (int) YesOrNo.NO + "");
    }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj --no-restore`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Utils/MainUtils.cs
git commit -m "feat: add Is/Set/Default ErrorPromptForWrongBarcodeEnabled to MainUtils"
```

---

### Task 3: Add check method to AWorkplaceContentPanel + NG pset fix

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs`

**Part A — Add the check method (after `CheckErrorPromptForArmEnabled` near line 1143):**

- [ ] **Step 1: Insert the check method**

After the closing `}` of `CheckErrorPromptForArmEnabled()` (line 1144), add:

```csharp
public bool CheckErrorPromptForWrongBarcodeEnabled() {
    return MainUtils.IsErrorPromptForWrongBarcodeEnabled();
}
```

**Part B — NG pset fix: clear CurrentParameterSet on all bolts in PrepareBeforeActivatingMission:**

- [ ] **Step 2: Add `CurrentParameterSet = null` to _allBolts reset loop**

At line 1269-1274, change:

```csharp
_allBolts[sideId].ForEach(b => {
    b.ResetStatusWithoutChangingVisible();
    b.NgTimes = 0;
});
```

To:

```csharp
_allBolts[sideId].ForEach(b => {
    b.ResetStatusWithoutChangingVisible();
    b.NgTimes = 0;
    b.CurrentParameterSet = null;
});
```

- [ ] **Step 3: Add missing reset logic to _allBoltsIndependence loop**

At line 1277-1282, change:

```csharp
foreach (int sideId in _allBoltsIndependence.Keys) {
    foreach (int workstationId in _allBoltsIndependence[sideId].Keys) {
        _allBoltsIndependence[sideId][workstationId] = _allBoltsIndependence[sideId][workstationId].OrderBy(btn => btn.BoltDTO.serial_num).ToList();
    }
}
```

To:

```csharp
foreach (int sideId in _allBoltsIndependence.Keys) {
    foreach (int workstationId in _allBoltsIndependence[sideId].Keys) {
        _allBoltsIndependence[sideId][workstationId] = _allBoltsIndependence[sideId][workstationId]
            .OrderBy(btn => btn.BoltDTO.serial_num).ToList();
        _allBoltsIndependence[sideId][workstationId].ForEach(b => {
            b.ResetStatusWithoutChangingVisible();
            b.NgTimes = 0;
            b.CurrentParameterSet = null;
        });
    }
}
```

- [ ] **Step 4: Build**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj --no-restore`
Expected: Build succeeds.

- [ ] **Step 5: Commit**

```bash
git add OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs
git commit -m "fix: add wrong-barcode check method + clear CurrentParameterSet on mission reactivation"
```

---

### Task 4: Add wrong-barcode toggle logic to ABarCodeInputPopUpForm (5 locations)

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/ABarCodeInputPopUpForm.cs`

Each of the 5 locations follows the same transformation pattern. The original call:

```csharp
_workplace.OpenAdminPasswordPopUpForm("message", allowCancel: false);
```

Becomes:

```csharp
if (_workplace.CheckErrorPromptForWrongBarcodeEnabled()) {
    _workplace.OpenAdminPasswordPopUpForm("message", allowCancel: false);
} else {
    WidgetUtils.ShowWarningPopUp("message");
}
```

**Location 1 — line 255 (产品码与已选任务不匹配):**

- [ ] **Step 1: Wrap the OpenAdminPasswordPopUpForm call**

Change:
```csharp
                        _workplace.OpenAdminPasswordPopUpForm($"当前条码【{barCode}】与选择的任务不匹配", allowCancel: false);
```

To:
```csharp
                        if (_workplace.CheckErrorPromptForWrongBarcodeEnabled()) {
                            _workplace.OpenAdminPasswordPopUpForm($"当前条码【{barCode}】与选择的任务不匹配", allowCancel: false);
                        } else {
                            WidgetUtils.ShowWarningPopUp($"当前条码【{barCode}】与选择的任务不匹配");
                        }
```

**Location 2 — line 280 (没有检索到匹配条码的任务):**

- [ ] **Step 2: Wrap the OpenAdminPasswordPopUpForm call**

Change:
```csharp
                    _workplace.OpenAdminPasswordPopUpForm($"没有检索到匹配条码【{barCode}】的任务", allowCancel: false);
```

To:
```csharp
                    if (_workplace.CheckErrorPromptForWrongBarcodeEnabled()) {
                        _workplace.OpenAdminPasswordPopUpForm($"没有检索到匹配条码【{barCode}】的任务", allowCancel: false);
                    } else {
                        WidgetUtils.ShowWarningPopUp($"没有检索到匹配条码【{barCode}】的任务");
                    }
```

**Location 3 — line 317 (前置任务未完成):**

- [ ] **Step 3: Wrap the OpenAdminPasswordPopUpForm call**

Change:
```csharp
                        _workplace.OpenAdminPasswordPopUpForm("未检测到前置任务的加工完成记录，请先完成前置任务", allowCancel: false);
```

To:
```csharp
                        if (_workplace.CheckErrorPromptForWrongBarcodeEnabled()) {
                            _workplace.OpenAdminPasswordPopUpForm("未检测到前置任务的加工完成记录，请先完成前置任务", allowCancel: false);
                        } else {
                            WidgetUtils.ShowWarningPopUp("未检测到前置任务的加工完成记录，请先完成前置任务");
                        }
```

**Location 4 — line 413 (物料码重复录入):**

- [ ] **Step 4: Wrap the OpenAdminPasswordPopUpForm call**

Change:
```csharp
                _workplace.OpenAdminPasswordPopUpForm($"请勿重复录入物料", allowCancel: false);
```

To:
```csharp
                if (_workplace.CheckErrorPromptForWrongBarcodeEnabled()) {
                    _workplace.OpenAdminPasswordPopUpForm($"请勿重复录入物料", allowCancel: false);
                } else {
                    WidgetUtils.ShowWarningPopUp($"请勿重复录入物料");
                }
```

Note: this `return` statement on the next line remains unchanged. The warning popup blocks until user clicks OK, then the return executes. The duplicate barcode is never accepted regardless of switch state.

**Location 5 — line 428 (物料码与配置不匹配):**

- [ ] **Step 5: Wrap the OpenAdminPasswordPopUpForm call**

Change:
```csharp
                _workplace.OpenAdminPasswordPopUpForm($"当前物料条码【{barCode}】与当前任务所配置的物料条码不匹配", allowCancel: false);
```

To:
```csharp
                if (_workplace.CheckErrorPromptForWrongBarcodeEnabled()) {
                    _workplace.OpenAdminPasswordPopUpForm($"当前物料条码【{barCode}】与当前任务所配置的物料条码不匹配", allowCancel: false);
                } else {
                    WidgetUtils.ShowWarningPopUp($"当前物料条码【{barCode}】与当前任务所配置的物料条码不匹配");
                }
```

**L331 and L479 (返工) are NOT modified — they remain unchanged.**

- [ ] **Step 6: Build**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj --no-restore`
Expected: Build succeeds.

- [ ] **Step 7: Commit**

```bash
git add OperationGuidance_new/Views/AbstractViews/ABarCodeInputPopUpForm.cs
git commit -m "feat: gate 5 wrong-barcode admin popups behind settings.ini toggle"
```

---

### Task 5: Add wrong-barcode toggle to VariableSettingsView_SCII config UI

**Files:**
- Modify: `OperationGuidance_new/Views/VariableSettingsView_SCII.cs`

- [ ] **Step 1: Add fields**

After `private bool _buzzerEnabledOriginal;` (near line 15), add:

```csharp
private ToggleButtonGroup _errorPromptForWrongBarcodeToggle;
private bool _errorPromptForWrongBarcodeOriginal;
```

- [ ] **Step 2: Initialize the toggle in InitializeMissionSettings()**

In `InitializeMissionSettings()`, after the `_enableBatchCounter` block (line 33) and before `_buzzerEnabledToggle` (line 34), add:

```csharp
_errorPromptForWrongBarcodeToggle = new("启用错码验证") {
    Parent = WorkContentPanel,
    Ratio = 6.95,
};
```

- [ ] **Step 3: Resize the toggle in ResizeMissionSettings()**

In `ResizeMissionSettings()`, after `_enableBatchCounter.Margin = ...` line (line 95), add:

```csharp
_errorPromptForWrongBarcodeToggle.Size = new(boxWidth, BoxNBtnHeight);
_errorPromptForWrongBarcodeToggle.Margin = new(0, boxVMargin, 0, 0);
```

The `WorkContentPanel.Height` and `WorkPanel.Height` lines stay unchanged (still 3-row formula).

- [ ] **Step 4: Save in SaveMissionSettings()**

In `SaveMissionSettings()`, after `_enableBatchCounterOriginal = sciiBatchConfig.enabled.ToYesOrNoBool();` (line 64), add:

```csharp
bool wrongBarcodeEnabled = _errorPromptForWrongBarcodeToggle.Checked;
MainUtils.SetErrorPromptForWrongBarcodeEnabled(wrongBarcodeEnabled);
_errorPromptForWrongBarcodeOriginal = wrongBarcodeEnabled;
```

- [ ] **Step 5: Load in LoadSettings()**

In `LoadSettings()`, inside the `BeginInvoke(...)` lambda, add before the closing `});` (line 111):

```csharp
_errorPromptForWrongBarcodeOriginal = MainUtils.IsErrorPromptForWrongBarcodeEnabled();
_errorPromptForWrongBarcodeToggle.Checked = _errorPromptForWrongBarcodeOriginal;
```

- [ ] **Step 6: Reset in ResetAllToDefault()**

In `ResetAllToDefault()`, inside the `BeginInvoke(...)` lambda, add before the closing `});` (line 124):

```csharp
_errorPromptForWrongBarcodeToggle.Checked = MainUtils.DefaultIsErrorPromptForWrongBarcodeEnabled();
```

- [ ] **Step 7: Add to unsaved-changes check in CheckSavedFunc_detail()**

In `CheckSavedFunc_detail()`, in the `return` expression, add before `&& _buzzerEnabledToggle.Checked == _buzzerEnabledOriginal`:

```csharp
&& _errorPromptForWrongBarcodeToggle.Checked == _errorPromptForWrongBarcodeOriginal
```

- [ ] **Step 8: Build**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj --no-restore`
Expected: Build succeeds.

- [ ] **Step 9: Commit**

```bash
git add OperationGuidance_new/Views/VariableSettingsView_SCII.cs
git commit -m "feat: add wrong-barcode verification toggle to SCII settings panel"
```

---

### Task 6: Async DataQueryView_SCII VisibleToTrue

**Files:**
- Modify: `OperationGuidance_new/Views/DataQueryView_SCII.cs`

- [ ] **Step 1: Add _loadCts field**

In the `#region Fields` section (after line 27), add:

```csharp
private CancellationTokenSource _loadCts;
```

Add `using System.Threading;` if not already present.

- [ ] **Step 2: Remove synchronous RefreshWorkstationOptions call from InitializeGridView**

In `InitializeGridView()`, remove the line `RefreshWorkstationOptions();` (currently at line 109). The dropdown will now be populated asynchronously by `VisibleToTrue`.

- [ ] **Step 3: Replace RefreshWorkstationOptions body with async version**

Replace the body of `RefreshWorkstationOptions()` (lines 162-177) with:

```csharp
private void RefreshWorkstationOptions() {
    _workstationNameComboBox.Enabled = false;

    _loadCts?.Cancel();
    _loadCts = new CancellationTokenSource();
    var token = _loadCts.Token;

    Task.Run(() => {
        try {
            var workstations = apis.QueryWorkstationList(
                new(SystemUtils.MacAddressesDTO.id)).WorkstationsDTOs;
            if (token.IsCancellationRequested) return;

            Dictionary<int, List<int>> missionRecordIds = new();
            if (workstations.Count > 0) {
                missionRecordIds = apis.QueryMissionRecordsByWorkstationIds(
                    new(workstations.Select(w => w.id).ToList())).MissionRecordsDict;
            }
            if (token.IsCancellationRequested) return;

            BeginInvoke(() => {
                if (IsDisposed) return;
                try {
                    _workstations = workstations;
                    _workstationNameComboBox.ClearItem();
                    foreach (WorkstationDTO workstation in _workstations) {
                        if (missionRecordIds.ContainsKey(workstation.id)) {
                            List<int?> ids = new();
                            missionRecordIds[workstation.id].ForEach(id => ids.Add(id));
                            _workstationNameComboBox.AddItem(workstation.name, ids);
                        }
                    }
                    _workstationNameComboBox.AddItem("无", null);
                } finally {
                    _workstationNameComboBox.Enabled = true;
                }
            });
        } catch (Exception ex) {
            BeginInvoke(() => {
                if (!IsDisposed) {
                    logger.Error("Failed to load workstation options", ex);
                    _workstationNameComboBox.Enabled = true;
                }
            });
        }
    }, token);
}
```

The `VisibleToTrue()` method stays unchanged — it already calls `RefreshWorkstationOptions()` + `base.VisibleToTrue()`.

- [ ] **Step 4: Build**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj --no-restore`
Expected: Build succeeds.

- [ ] **Step 5: Commit**

```bash
git add OperationGuidance_new/Views/DataQueryView_SCII.cs
git commit -m "perf: load DataQueryView_SCII workstation dropdown asynchronously to prevent UI freeze"
```

---

### Task 7: Async DataQueryView VisibleToTrue

**Files:**
- Modify: `OperationGuidance_new/Views/DataQueryView.cs`

- [ ] **Step 1: Add _loadCts field**

In the `#region Fields` section (after line 24), add:

```csharp
private CancellationTokenSource _loadCts;
```

Add `using System.Threading;` if not already present.

- [ ] **Step 2: Remove synchronous RefreshWorkstationOptions call from InitializeGridView**

In `InitializeGridView()`, remove the line `RefreshWorkstationOptions();` (currently at line 84). The dropdown will now be populated asynchronously by `VisibleToTrue`.

- [ ] **Step 3: Replace RefreshWorkstationOptions body with async version**

Replace the body of `RefreshWorkstationOptions()` (lines 200-206) with:

```csharp
private void RefreshWorkstationOptions() {
    _workstationNameComboBox.Enabled = false;

    _loadCts?.Cancel();
    _loadCts = new CancellationTokenSource();
    var token = _loadCts.Token;

    Task.Run(() => {
        try {
            var workstations = apis.QueryWorkstationList(
                new(SystemUtils.MacAddressesDTO.id)).WorkstationsDTOs;
            if (token.IsCancellationRequested) return;

            BeginInvoke(() => {
                if (IsDisposed) return;
                try {
                    _workstations = workstations;
                    _workstationNameComboBox.ClearItem();
                    foreach (WorkstationDTO workstation in _workstations) {
                        _workstationNameComboBox.AddItem(workstation.name, workstation.id);
                    }
                } finally {
                    _workstationNameComboBox.Enabled = true;
                }
            });
        } catch (Exception ex) {
            BeginInvoke(() => {
                if (!IsDisposed) {
                    logger.Error("Failed to load workstation options", ex);
                    _workstationNameComboBox.Enabled = true;
                }
            });
        }
    }, token);
}
```

The `VisibleToTrue()` method stays as-is — it already calls `RefreshWorkstationOptions()` (the now-async version).

- [ ] **Step 4: Build**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj --no-restore`
Expected: Build succeeds.

- [ ] **Step 5: Commit**

```bash
git add OperationGuidance_new/Views/DataQueryView.cs
git commit -m "perf: load DataQueryView workstation dropdown asynchronously to prevent UI freeze"
```

---

### Task 8: Final build verification

- [ ] **Step 1: Full build**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeds with zero warnings.

- [ ] **Step 2: Check git status**

Run: `git status`
Expected: Working tree clean, all changes committed.

---

## Summary

| Task | Files Changed | Purpose |
|------|--------------|---------|
| 1 | `IniFileKeys.cs` | +1 INI key constant |
| 2 | `MainUtils.cs` | +3 read/write/default methods |
| 3 | `AWorkplaceContentPanel.cs` | +1 check method; Clear CurrentParameterSet on all bolts (NG pset fix) |
| 4 | `ABarCodeInputPopUpForm.cs` | 5 admin popup gates |
| 5 | `VariableSettingsView_SCII.cs` | Toggle UI + save/load/reset/check |
| 6 | `DataQueryView_SCII.cs` | Async workstation dropdown + disabled-while-loading guard |
| 7 | `DataQueryView.cs` | Async workstation dropdown + disabled-while-loading guard |
| 8 | — | Build verification only |

**Total: 7 modified files, 0 new files, 8 commits.**
