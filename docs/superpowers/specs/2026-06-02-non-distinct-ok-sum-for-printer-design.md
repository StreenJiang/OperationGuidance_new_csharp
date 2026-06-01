# Non-Distinct OK Count for Printer SN — Design Spec

**Date:** 2026-06-02
**Status:** approved

## Motivation

`SendToPrinter` in `WorkplaceContentPanel_SCII_XT` currently calls `ComputeTodayStats(_mission.id).okSum` to get the printer serial number. `ComputeTodayStats` uses `Distinct` on `product_bar_code`, so multiple OK results for the same product barcode are counted only once.

The requirement: printer SN should count ALL OK records for the day (no distinct), so every tightening that passed OK increments the printed serial number — even for the same product barcode.

`ComputeTodayStats` must remain unchanged in its distinct logic because:
- `SetTodayData` (SCII) uses it to update the `_okSumPerDay` UI text box, which should continue showing distinct OK counts
- The SCII version's display logic should not change

## Changes

### File 1: `OperationGuidance_new/Views/WorkplaceMissionView_SCII_XT.cs`

**New private method** `ComputeTodayOkCount(int missionId)` — returns `int`:

- Calls `GetRecoreds(missionId)` (inherited virtual, XT override filters by `Date = DateTime.Now`)
- Counts all records where `mission_result == (int)TighteningStatus.OK` — no `Distinct`, no grouping
- Returns `0` when `missionId <= 0`
- Includes `logger.Debug` for record count
- Wraps logic in try-catch: on exception logs `logger.Error` and returns `0`

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

Placement: after `SendToPrinter()` (after line 331).

**Refactor** `SendToPrinter()` line 302:

```diff
- int okSum = await Task.Run(() => ComputeTodayStats(_mission.id).okSum);
+ int okSum = await Task.Run(() => ComputeTodayOkCount(_mission.id));
```

The rest of `SendToPrinter` is unchanged.

### File 2: `OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs`

**Harden** `ComputeTodayStats` — wrap existing logic in try-catch, log on failure, return `(0, 0, 0)`:

```diff
 protected virtual (int sum, int okSum, double ngRate) ComputeTodayStats(int missionId) {
     int sum = 0;
     int okSum = 0;
     double ngRate = 0;

+    try {
         if (missionId > 0) {
             List<MissionRecordDTO> missionRecordDTOs = GetRecoreds(missionId);
             // ... existing logic ...
         } else {
             logger.Debug($"[SCII:ComputeTodayStats] Mission ID is 0, skipping data retrieval");
         }
+    } catch (Exception ex) {
+        logger.Error($"[SCII:ComputeTodayStats] Failed to compute today stats: {ex.Message}", ex);
+    }

     return (sum, okSum, ngRate);
 }
```

## No changes

- `SetTodayData` — unchanged; continues distinct logic for UI display
- `IncrementTodayCountersOptimistically` — unchanged
- `DelayedReconcileTodayData` — unchanged
- All other files — unchanged
