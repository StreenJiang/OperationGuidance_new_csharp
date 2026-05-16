# Wrong-Code Toggle + Async DataQuery Loading — Design

## 1. 错码 vs 重码 区分

### 1.1 当前现状

`ABarCodeInputPopUpForm.cs` 中有 7 处 `OpenAdminPasswordPopUpForm` 调用，按语义分为三类：

| 类别 | 位置 | 触发条件 | 消息 |
|------|------|----------|------|
| **错码** | L255 | 产品码与当前已选任务不匹配 | `当前条码【{barCode}】与选择的任务不匹配` |
| **错码** | L280 | 产品码匹配不到任何任务 | `没有检索到匹配条码【{barCode}】的任务` |
| **错码** | L317 | 前置任务未完成 | `未检测到前置任务的加工完成记录...` |
| **错码** | L413 | 物料码重复录入（查数据库判定） | `请勿重复录入物料` |
| **错码** | L428 | 物料码与当前任务配置规则不匹配 | `当前物料条码【{barCode}】与当前任务所配置的物料条码不匹配` |
| 返工 | L331 | 产品返工确认 | `产品返工确认，请输入管理员密码解锁` |
| 返工 | L479 | 物料返工确认 | `物料返工确认。请输入管理员密码解锁。` |

- **错码** = 条码与任务/规则匹配不上、前置任务未完成、数据库中已存在重复
- **返工** = 主动要求对已有加工记录的产品/物料再次加工

### 1.2 需求

为**全部 5 处错码**场景（L255、L280、L317、L413、L428）添加开关控制：
- 开关**开启**时 → 保持现有行为（弹出管理员密码弹窗）
- 开关**关闭**时 → 改为普通 `ShowWarningPopUp` 警告提示，无需管理员解锁

返工场景（L331、L479）**不受此开关影响**，始终弹出管理员密码弹窗。

### 1.3 改动点

**`Configs/IniFileKeys.cs`** — 新 Key：
```csharp
public static string MissionErrorPromptForWrongBarcode => "mission_error_prompt_for_wrong_barcode";
```

**`Utils/MainUtils.cs`** — 读/写/默认值（完全遵循 buzzer 的 `IsBuzzerEnabled` / `SetBuzzerEnabled` / `DefaultIsBuzzerEnabled` 模式）：
```csharp
public static bool IsErrorPromptForWrongBarcodeEnabled() { ... }
public static bool DefaultIsErrorPromptForWrongBarcodeEnabled() => false;  // 默认关闭
public static void SetErrorPromptForWrongBarcodeEnabled(bool flag) { ... }
```

**`Views/AbstractViews/AWorkplaceContentPanel.cs`** — 新增检查方法（仿照 `CheckErrorPromptForArmEnabled`）：
```csharp
public bool CheckErrorPromptForWrongBarcodeEnabled() {
    return MainUtils.IsErrorPromptForWrongBarcodeEnabled();
}
```

**`Views/AbstractViews/ABarCodeInputPopUpForm.cs`** — 在 L255、L280、L317、L413、L428 五处 `OpenAdminPasswordPopUpForm` 调用前增加开关判断：
```csharp
// Before (L255):
_workplace.OpenAdminPasswordPopUpForm($"当前条码【{barCode}】与选择的任务不匹配", allowCancel: false);

// After:
if (_workplace.CheckErrorPromptForWrongBarcodeEnabled()) {
    _workplace.OpenAdminPasswordPopUpForm($"当前条码【{barCode}】与选择的任务不匹配", allowCancel: false);
} else {
    WidgetUtils.ShowWarningPopUp($"当前条码【{barCode}】与选择的任务不匹配");
}
```

五处改动模式相同。L331、L479（返工）**不**受此开关影响。

---

## 2. SCII / SCII_XT 配置界面新增开关

### 2.1 布局目标

在 `VariableSettingsView_SCII` 的 "操作配置" 面板中，新开关放在"启用批次计数"同一行的右侧，蜂鸣器行不变：

```
操作配置
┌─────────────────────────────┬─────────────────────────────┐
│ 力臂定位警告 (左)            │ USB扫码枪 (右)               │  ← 第1行 (base)
├─────────────────────────────┼─────────────────────────────┤
│ 启用批次计数 (左)            │ 启用错码验证 (右)  [新增]    │  ← 第2行
├─────────────────────────────┼─────────────────────────────┤
│ 启用蜂鸣器 (左)              │ 蜂鸣器测试 (右)              │  ← 第3行
└─────────────────────────────┴─────────────────────────────┘
```

布局高度仍是 3 行 (`BoxNBtnHeight * 3 + ...`)，无需增加行数。

`SCII_XT` 继承 `SCII`，自动获得该开关（无需重复添加）。

### 2.2 改动清单

**`VariableSettingsView_SCII.cs`**：

1. 新增字段 `_errorPromptForWrongBarcodeToggle` 和 `_errorPromptForWrongBarcodeOriginal`
2. `InitializeMissionSettings()` — 在 `_buzzerEnabledToggle` 之前构造新 Toggle，使其在 FlowLayoutPanel 中排在 `_enableBatchCounter` 之后（同一行右侧）:
   ```csharp
   _errorPromptForWrongBarcodeToggle = new("启用错码验证") {
       Parent = WorkContentPanel,
       Ratio = 6.95,
   };
   ```
3. `ResizeMissionSettings()` — 新 toggle 占据第2行右侧:
   - `_enableBatchCounter` — 第2行左 (不变)
   - `_errorPromptForWrongBarcodeToggle` — 第2行右 (新增, `Margin = new(0, boxVMargin, 0, 0)`)
   - `_buzzerEnabledToggle` + `_buzzerTestButtons` — 第3行左右 (不变)
   - `WorkContentPanel.Height` 保持 `BoxNBtnHeight * 3 + ContentVPadding * 2 + boxVMargin * 2`
   - `WorkPanel.Height` 保持不变
4. `SaveMissionSettings()` — 写入 INI:
   ```csharp
   MainUtils.SetErrorPromptForWrongBarcodeEnabled(_errorPromptForWrongBarcodeToggle.Checked);
   _errorPromptForWrongBarcodeOriginal = _errorPromptForWrongBarcodeToggle.Checked;
   ```
5. `LoadSettings()` — 读取 INI 回填:
   ```csharp
   _errorPromptForWrongBarcodeOriginal = MainUtils.IsErrorPromptForWrongBarcodeEnabled();
   _errorPromptForWrongBarcodeToggle.Checked = _errorPromptForWrongBarcodeOriginal;
   ```
6. `ResetAllToDefault()` — 恢复默认（关闭）:
   ```csharp
   _errorPromptForWrongBarcodeToggle.Checked = MainUtils.DefaultIsErrorPromptForWrongBarcodeEnabled();
   ```
7. `CheckSavedFunc_detail()` — 加入未保存变更检测:
   ```csharp
   && _errorPromptForWrongBarcodeToggle.Checked == _errorPromptForWrongBarcodeOriginal
   ```

---

## 3. 数据查询界面异步加载优化

### 3.1 根因分析

`DataQueryView_SCII.VisibleToTrue()` (L356-363) 在 UI 线程的实际调用链（经 `CustomContentPanelBase` 源码核实）：

```
CustomVScrollingContentPanel.VisibleToTrue()
 └→ DataQueryView_SCII.VisibleToTrue()
     ├→ RefreshWorkstationOptions()
     │   ├→ apis.QueryWorkstationList(...)           ← 同步
     │   └→ apis.QueryMissionRecordsByWorkstationIds(...) ← 同步，全表！
     └→ base.VisibleToTrue()  ← CustomContentPanelBase.ResizeChildren()，纯布局
```

`CustomContentPanelBase.VisibleToTrue()` **不向下传播**，只做 `ResizeChildren()`。`DataGridViewGroup.VisibleToTrue()` → `QueryAndRefresh()` 从未被自动触发——网格数据只在用户手动点击"查询"按钮时才加载。

**唯一瓶颈**：`RefreshWorkstationOptions()` 中 `QueryMissionRecordsByWorkstationIds` 对 MissionRecord 做全表分组查询以填充站点下拉框。数据量大的工位单次调用可耗时数十秒。

`DataQueryView`（非 SCII 版）同样：`RefreshWorkstationOptions()` 中的 `QueryWorkstationList` 为同步阻塞。

### 3.2 解决方案

`VisibleToTrue` 保持调用 `base.VisibleToTrue()`（纯布局，瞬间完成）。只需将 `RefreshWorkstationOptions()` 的 API 调用移至后台：

```csharp
// DataQueryView_SCII.cs
public override void VisibleToTrue() {
    var operationDataFields = MainUtils.GetOperationDataFields();
    if (!_operationDataFields.SequenceEqual(operationDataFields)) {
        _operationDataFields = operationDataFields;
        _dataGridView.ResetColumnHeaders();
    }
    base.VisibleToTrue();  // 纯 ResizeChildren，瞬间完成，不触发数据查询

    // 后台加载工作站下拉选项
    _loadCts?.Cancel();
    _loadCts = new CancellationTokenSource();
    var token = _loadCts.Token;

    Task.Run(() => {
        try {
            var workstations = apis.QueryWorkstationList(
                new(SystemUtils.MacAddressesDTO.id)).WorkstationsDTOs;
            if (token.IsCancellationRequested) return;

            var missionRecordIds = apis.QueryMissionRecordsByWorkstationIds(
                new(workstations.Select(w => w.id).ToList())).MissionRecordsDict;
            if (token.IsCancellationRequested) return;

            BeginInvoke(() => {
                if (IsDisposed) return;
                _workstations = workstations;
                _workstationNameComboBox.ClearItem();
                foreach (var ws in _workstations) {
                    if (missionRecordIds.ContainsKey(ws.id)) {
                        List<int?> ids = new();
                        missionRecordIds[ws.id].ForEach(id => ids.Add(id));
                        _workstationNameComboBox.AddItem(ws.name, ids);
                    }
                }
                _workstationNameComboBox.AddItem("无", null);
            });
        } catch (Exception ex) {
            BeginInvoke(() => {
                if (!IsDisposed) {
                    logger.Error("Failed to load workstation options", ex);
                }
            });
        }
    }, token);
}
```

**关键点：**
- `base.VisibleToTrue()` 保留不变——它只是 `ResizeChildren()`，不触发数据查询
- 只把 `RefreshWorkstationOptions` 中的 2 次 API 调用移到后台
- 网格数据加载仍由用户点击"查询"按钮触发（已是分页查询，快速）
- `_loadCts` + `IsDisposed` 守卫遵循项目现有模式

`DataQueryView.cs` 同理，将其 `RefreshWorkstationOptions()`（包含 `QueryWorkstationList`）移至后台。

---

## 4. NG 终止后重激活时第一个点位程序号下发失败

### 4.1 现象

任务以 NG 结束 → 再次激活同一任务 → 第一个点位程序号看似下发成功（软件 UI 显示"发送成功"），但控制器实际仍保留上次 NG 时最后一个点位的程序号。导致第一个点位的拧紧数据标记了错误的程序号，造成数据出错。

### 4.2 根因分析

关键代码在 `ChangeBoltStatusToWorking` (`AWorkplaceContentPanel.cs:1861-1874`)：

```csharp
if (boltDTO.parameters_set != boltButton.CurrentParameterSet) {
    boltButton.CurrentParameterSet = null;
    _ = Task.Run(async () => {
        await SendPSet(boltButton, _toolTasks[toolId], boltDTO.parameters_set);
    });
} else {
    // Same pset → SKIP sending!
    logger.Info($"Same pset as previous={boltButton.CurrentParameterSet}, so skip...");
}
```

这段逻辑用 `BoltButton.CurrentParameterSet` 作为"控制器当前程序号"的本地缓存。如果缓存的程序号与新点位配置的程序号相同，则**跳过下发**。

**问题链路**：

```
第一次激活：
  bolt 0 → SendPSet(pset0) 成功 → bolt0.CurrentParameterSet = pset0 ✓
  ...
  bolt N → SendPSet(pset_N) 成功 → boltN.CurrentParameterSet = pset_N ✓
  bolt N → NG → TerminateMission(FINISHED_NG)
         → 控制器仍保留 pset_N
         → PrepareBeforeActivatingMission() 调用 ResetStatusWithoutChangingVisible()
         → 但 CurrentParameterSet 未被清除！

第二次激活：
  InitializeBeforeActivatingMission() → SwitchBolt(0) → ChangeBoltStatusToWorking(bolt0)
  → bolt0.CurrentParameterSet == pset0 (缓存！)
  → boltDTO.parameters_set == pset0 (配置)
  → pset0 == pset0 → 跳过下发！ ✗
  → 控制器实为 pset_N → 程序号错位！
```

**`ResetStatusWithoutChangingVisible()` (BoltButton.cs:140-146) 不清除 `CurrentParameterSet`**：

```csharp
public void ResetStatusWithoutChangingVisible() {
    _showingWhileWorking = true;
    _boltStatus = BoltStatus.DEFAULT;
    BackColor = ...;
    ForeColor = TEXT_BLACK;
    SetLabel();
    // _currentParameterSet 未清除！
}
```

**`PrepareBeforeActivatingMission()` (AWorkplaceContentPanel.cs:1269-1271) 也不清除**：

```csharp
_allBolts[sideId].ForEach(b => {
    b.ResetStatusWithoutChangingVisible();  // 不含 CurrentParameterSet
    b.NgTimes = 0;
    // b.CurrentParameterSet 未重置！
});
```

### 4.3 修复方案

在 `PrepareBeforeActivatingMission()` 中为 `_allBolts` 加上 `CurrentParameterSet = null`，同时为 `_allBoltsIndependence` 补齐缺失的 `ResetStatusWithoutChangingVisible()`、`NgTimes = 0`、`CurrentParameterSet = null`（当前代码只排序了 `_allBoltsIndependence`，完全没有重置操作）：

```csharp
// AWorkplaceContentPanel.cs, line 1269 — _allBolts 循环
_allBolts[sideId].ForEach(b => {
    b.ResetStatusWithoutChangingVisible();
    b.NgTimes = 0;
    b.CurrentParameterSet = null;  // ← 确保下次激活重新下发程序号
});

// Line 1278 — _allBoltsIndependence 循环，补全重置逻辑
foreach (int sideId in _allBoltsIndependence.Keys) {
    foreach (int workstationId in _allBoltsIndependence[sideId].Keys) {
        _allBoltsIndependence[sideId][workstationId] = _allBoltsIndependence[sideId][workstationId]
            .OrderBy(btn => btn.BoltDTO.serial_num).ToList();
        // 以下三行为新增
        _allBoltsIndependence[sideId][workstationId].ForEach(b => {
            b.ResetStatusWithoutChangingVisible();
            b.NgTimes = 0;
            b.CurrentParameterSet = null;
        });
    }
}
```

改动在 `AWorkplaceContentPanel.PrepareBeforeActivatingMission()` 中：`_allBolts` 加 1 行，`_allBoltsIndependence` 补 3 行。

### 4.4 为什么 NG 场景下这个问题并不总是出现

如果 NG 发生在 bolt 0（第一个点位），则第二次激活时：
- `bolt0.CurrentParameterSet` 为空（因为 fire-and-forget 的 SendPSet 失败或从未执行成功，CurrentParameterSet 仍为 null 或被取消令牌中断）
- `boltDTO.parameters_set != null` → 触发 SendPSet → 正常下发

如果 NG 发生在 bolt N（N>0），且 bolt 0 之前成功下发过：
- 缓存命中 → 跳过 → 控制器仍为 bolt N 的 pset → 错位

这就解释了为什么有些工位会遇到、有些不会——取决于 NG 发生前是否已经有其他点位成功下发过程序号。

---

## 5. 文件改动总览

| 文件 | 改动 |
|------|------|
| `Configs/IniFileKeys.cs` | +1 Key |
| `Utils/MainUtils.cs` | +3 methods (read/write/default) |
| `Views/AbstractViews/AWorkplaceContentPanel.cs` | +1 check method; PrepareBeforeActivatingMission 加 CurrentParameterSet 清除 |
| `Views/AbstractViews/ABarCodeInputPopUpForm.cs` | 5 处调用前加开关判断 |
| `Views/VariableSettingsView_SCII.cs` | 新 Toggle (第2行右) + save/load/reset/check |
| `Views/VariableSettingsView_SCII_XT.cs` | 无需改动（继承自 SCII） |
| `Views/DataQueryView_SCII.cs` | VisibleToTrue 异步化 + _loadCts |
| `Views/DataQueryView.cs` | 同上 |

## 6. 注意事项

- 错码开关 `mission_error_prompt_for_wrong_barcode` 默认值 `false`（关闭），不影响现有工位直到配置界面手动开启
- 开关仅影响 **5 处错码场景**（产品码不匹配、无匹配任务、前置任务未完成、物料码重复、物料码不匹配），返工场景不受影响
- SCII_XT 完全继承 SCII 的 UI 改动，无需单独编码
- 异步数据加载遵循项目现有的 `CancellationTokenSource` + `BeginInvoke` + `IsDisposed` 守卫模式
- `base.VisibleToTrue()` 实际只是 `CustomContentPanelBase.ResizeChildren()`，不传播到子控件，不触发数据查询——无需跳过
- NG 重激活时程序号错位：根因是 `CurrentParameterSet` 缓存未清除，修复为两处 `= null`（`_allBolts` + `_allBoltsIndependence`）
