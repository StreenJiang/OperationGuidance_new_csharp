# Non-Distinct OK Count for Printer SN — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `ComputeTodayOkCount` in `WorkplaceContentPanel_SCII_XT` that counts all OK records without distinct, then call it from `SendToPrinter` instead of `ComputeTodayStats`. Also harden `ComputeTodayStats` in SCII with try-catch.

**Architecture:** `ComputeTodayOkCount(int missionId)` → `int` — calls inherited `GetRecoreds`, counts `mission_result == OK` without `Distinct`. Lives in XT class only. `SendToPrinter` replaces one line. Both compute methods get independent try-catch + error logging.

**Tech Stack:** C# WinForms, .NET

---

### Task 1: Add ComputeTodayOkCount and wire SendToPrinter (XT)

**Files:**
- Modify: `OperationGuidance_new/Views/WorkplaceMissionView_SCII_XT.cs`

- [ ] **Step 1: Add `ComputeTodayOkCount` method**

Insert after `SendToPrinter()` (after line 331). The method is `private`, returns `int`, with try-catch and logging:

```csharp
private int ComputeTodayOkCount(int missionId) {
    if (missionId <= 0) return 0;

    try {
        List<MissionRecordDTO> records = GetRecoreds(missionId);
        logger.Debug($"[SCII_XT:ComputeTodayOkCount] Retrieved {records.Count} mission records");

        int okCount = records.Count(dto => dto.mission_result == (int)TighteningStatus.OK);
        logger.Debug($"[SCII_XT:ComputeTodayOkCount] OK count (non-distinct): {okCount}");
        return okCount;
    } catch (Exception ex) {
        logger.Error($"[SCII_XT:ComputeTodayOkCount] Failed to compute OK count: {ex.Message}", ex);
        return 0;
    }
}
```

- [ ] **Step 2: Update `SendToPrinter` to call `ComputeTodayOkCount`**

Replace line 302:
```csharp
int okSum = await Task.Run(() => ComputeTodayStats(_mission.id).okSum);
```
With:
```csharp
int okSum = await Task.Run(() => ComputeTodayOkCount(_mission.id));
```

- [ ] **Step 3: Build to verify compilation**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: Build succeeds with no errors.

---

### Task 2: Harden ComputeTodayStats with try-catch (SCII)

**Files:**
- Modify: `OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs`

- [ ] **Step 1: Wrap ComputeTodayStats logic in try-catch**

In `ComputeTodayStats` (line 564-590), wrap the body inside a try-catch. On exception, log via `logger.Error` and return `(0, 0, 0)` (the already-initialized defaults). The method structure becomes:

```csharp
protected virtual (int sum, int okSum, double ngRate) ComputeTodayStats(int missionId) {
    int sum = 0;
    int okSum = 0;
    double ngRate = 0;

    try {
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
                ngRate = Math.Max(0, (sum - okSum) / (double) sum * 100);
            }
            logger.Debug($"[SCII:ComputeTodayStats] Calculated statistics - Total: {sum}, OK: {okSum}, NG Rate: {ngRate:F2}%");
        } else {
            logger.Debug($"[SCII:ComputeTodayStats] Mission ID is 0, skipping data retrieval");
        }
    } catch (Exception ex) {
        logger.Error($"[SCII:ComputeTodayStats] Failed to compute today stats: {ex.Message}", ex);
    }

    return (sum, okSum, ngRate);
}
```

- [ ] **Step 2: Build to verify compilation**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: Build succeeds with no errors.

---

### Task 3: Commit

```bash
git add OperationGuidance_new/Views/WorkplaceMissionView_SCII_XT.cs OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs
git commit -m "feat(scii-xt): add non-distinct ComputeTodayOkCount for printer SN

- Add ComputeTodayOkCount in XT: counts all OK records without Distinct
- SendToPrinter calls ComputeTodayOkCount instead of ComputeTodayStats
- Add try-catch + error logging to both ComputeTodayStats and ComputeTodayOkCount"
```

---

### Self-Review

1. **Spec coverage:** Task 1 covers new method + wire-up. Task 2 covers try-catch hardening. All spec requirements implemented.
2. **Placeholder scan:** No TBD/TODO/incomplete sections. All code shown inline.
3. **Type consistency:** `GetRecoreds` is already overridden in XT to filter by `Date = DateTime.Now`. New method inherits this behavior automatically. Both try-catch blocks return zero-values that are safe for callers.
4. **Scope:** Two files, one new private method, one line changed in `SendToPrinter`, one try-catch wrap. No risk of regressions.
