# Extract ComputeTodayStats — Design Spec

**Date:** 2026-05-29
**Status:** approved

## Motivation

`SendToPrinter` in `WorkplaceContentPanel_SCII_XT` reads `_okSumPerDay` UI textbox to get today's OK count for printer SN. When the app stays running overnight, the textbox may hold yesterday's stale value, causing the printed QR code SN to be yesterday's count + 1 instead of a fresh day's count.

The fix: extract the sum/okSum calculation from `SetTodayData` into a reusable `protected virtual` method, and have `SendToPrinter` call it to get authoritative data from the API instead of reading a potentially stale UI textbox.

## Changes

### File 1: `OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs`

**New method** `ComputeTodayStats(int missionId)` — `protected virtual`, returns `(int sum, int okSum, double ngRate)`:

- Calls `GetRecoreds(missionId)` (virtual, so XT override applies automatically)
- Computes `sum` = distinct product barcodes
- Computes `okSum` = distinct product barcodes with OK result
- Computes `ngRate` = `(sum - okSum) / sum * 100`
- Returns `(0, 0, 0)` when `missionId <= 0`

**Refactor** `SetTodayData(int? missionId)`:

- Remove inline calculation logic
- Call `ComputeTodayStats` and use the returned tuple for UI updates
- Logging and `InvokeRequired` guard stay unchanged

### File 2: `OperationGuidance_new/Views/WorkplaceMissionView_SCII_XT.cs`

**Refactor** `SendToPrinter()`:

- Before (reads stale UI textbox):
  ```csharp
  int _okSumToday = int.Parse(_okSumPerDay.GetTextBox(0).Box.Text);
  config.sn = _okSumToday + 1;
  ```
- After (calls new method on background thread):
  ```csharp
  int okSum = await Task.Run(() => ComputeTodayStats(_mission.id).okSum);
  // ... then on UI thread:
  config.sn = okSum + 1;
  ```

The API call runs on a thread-pool thread. Only the printer operation (`QuickPrint`) requires the UI thread.

## No changes

- `GetRecoreds` — unchanged; already virtual, XT override filters by `Date = DateTime.Now`
- `IncrementTodayCountersOptimistically` — unchanged; still updates UI optimistically
- `DelayedReconcileTodayData` — unchanged; still reconciles UI against API after delay
- `BarCodeInputPopUpForm_SCII_XT` — unchanged; still calls `_ = workplace.SendToPrinter()` fire-and-forget
