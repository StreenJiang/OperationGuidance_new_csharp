# Extract ComputeTodayStats — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extract sum/okSum calculation from `SetTodayData` into `protected virtual ComputeTodayStats`, then refactor `SendToPrinter` to call it instead of reading a stale UI textbox.

**Architecture:** `ComputeTodayStats(int missionId)` returns `(int sum, int okSum, double ngRate)` by calling `GetRecoreds` (virtual → XT override filters by date). `SetTodayData` delegates computation to it and only handles UI updates. `SendToPrinter` fetches okSum on a background thread before the UI-thread printer work.

**Tech Stack:** C# WinForms, .NET

---

### Task 1: Extract ComputeTodayStats in SCII.cs

**Files:**
- Modify: `OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs:562-598`

- [ ] **Step 1: Add `ComputeTodayStats` method**

Insert after `ResetMissionDetails()` (after line 561). The method body is the calculation logic moved from `SetTodayData`:

```csharp
protected virtual (int sum, int okSum, double ngRate) ComputeTodayStats(int missionId) {
    int sum = 0;
    int okSum = 0;
    double ngRate = 0;

    if (missionId > 0) {
        List<MissionRecordDTO> missionRecordDTOs = GetRecoreds(missionId);
        logger.Debug($"[SCII:ComputeTodayStats] Retrieved {missionRecordDTOs.Count} mission records");

        IEnumerable<MissionRecordDTO> distinctData = missionRecordDTOs
                    .DistinctBy(dto => dto.product_bar_code);
        sum = distinctData.Count();
        okSum = missionRecordDTOs
                    .Where(dto => dto.mission_result == (int) TighteningStatus.OK)
                    .Select(dto => dto.product_bar_code)
                    .Distinct()
                    .Count();
        if (sum > 0) {
            ngRate = (sum - okSum) / (double) sum * 100;
        }
        logger.Debug($"[SCII:ComputeTodayStats] Calculated statistics - Total: {sum}, OK: {okSum}, NG Rate: {ngRate:F2}%");
    } else {
        logger.Debug($"[SCII:ComputeTodayStats] Mission ID is 0, skipping data retrieval");
    }

    return (sum, okSum, ngRate);
}
```

- [ ] **Step 2: Refactor `SetTodayData` to delegate to `ComputeTodayStats`**

Replace the current `SetTodayData` body (lines 562-598) with:

```csharp
private void SetTodayData(int? missionId = null) {
    if (InvokeRequired) {
        Invoke(() => SetTodayData(missionId));
        return;
    }
    logger.Debug($"[SCII:SetTodayData] Setting today's data");

    int mId = missionId ?? _mission.id;
    var (sum, okSum, ngRate) = ComputeTodayStats(mId);

    _productSumPerDay.SetValue(0, sum + "");
    _okSumPerDay.SetValue(0, okSum + "");
    _ngRatePerDay.SetValue(0, $"{ngRate.ToString("F2")}%");
    logger.Debug($"[SCII:SetTodayData] UI updated - Sum: {sum}, OK: {okSum}, NG Rate: {ngRate:F2}%");
}
```

- [ ] **Step 3: Build to verify compilation**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: Build succeeds with no errors.

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs
git commit -m "refactor(scii): extract ComputeTodayStats from SetTodayData"
```

---

### Task 2: Refactor SendToPrinter in SCII_XT.cs

**Files:**
- Modify: `OperationGuidance_new/Views/WorkplaceMissionView_SCII_XT.cs:304-333`

- [ ] **Step 1: Replace `SendToPrinter` to call `ComputeTodayStats`**

Replace the existing `SendToPrinter` method (lines 304-333) with:

```csharp
public async Task SendToPrinter() {
    int okSum = await Task.Run(() => ComputeTodayStats(_mission.id).okSum);

    await Task.Run(() => BeginInvoke(() => {
        var config = ConfigUtils.LoadConfig<SciiXtPrinterConfig>();
        if (config.enabled == (int) YesOrNo.YES) {
            if (config.printer_name == null) {
                WidgetUtils.ShowWarningPopUp("打印机名称配置未设置，请先配置打印机。");
            } else {
                config.sn = okSum + 1;

                using (ZplQrCodePrinter printer = new()) {
                    List<string> list = printer.GetAvailablePrinters();
                    if (list.Count > 0) {
                        string? printerName = list.Find(p => p == config.printer_name);
                        if (printerName == null) {
                            WidgetUtils.ShowWarningPopUp("未找到指定配置的打印机，请检查配置或打印机。");
                        } else if (!printer.QuickPrint(config)) {
                            WidgetUtils.ShowWarningPopUp("发送指令至打印机失败！请检查日志信息定位问题。");
                        } else {
                            _lidCodePrinted = true;
                            _lastPrintedConfig = config;
                        }
                    } else {
                        WidgetUtils.ShowWarningPopUp("未找到任何打印机设备！");
                    }
                }
            }
        }
    }));
}
```

Key change: `int _okSumToday = int.Parse(_okSumPerDay.GetTextBox(0).Box.Text)` → `int okSum = await Task.Run(() => ComputeTodayStats(_mission.id).okSum)` placed before the `BeginInvoke` block. The captured `okSum` is then used inside the UI-thread lambda.

- [ ] **Step 2: Build to verify compilation**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: Build succeeds with no errors.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/WorkplaceMissionView_SCII_XT.cs
git commit -m "fix(scii-xt): use ComputeTodayStats for printer SN instead of stale UI textbox"
```

---

### Self-Review

1. **Spec coverage:** Task 1 covers the extraction + refactoring of `SetTodayData`. Task 2 covers the `SendToPrinter` change. All spec requirements implemented.
2. **Placeholder scan:** No TBD/TODO/incomplete sections. All code is shown inline.
3. **Type consistency:** `ComputeTodayStats` signature `(int sum, int okSum, double ngRate)` is consistent across both tasks. `GetRecoreds` is called from the base method; XT's override filters by date automatically through virtual dispatch.
