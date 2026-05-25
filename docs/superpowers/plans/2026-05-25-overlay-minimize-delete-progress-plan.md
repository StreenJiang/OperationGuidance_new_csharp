# Admin Overlay Minimize & DELETE Phase Progress Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix popup minimize/close, add DELETE-phase progress reporting.

**Architecture:** Three changes: (1) `VisibleChanged` handler on backdrop to manually sync popup visibility; (2) Close only backdrop in `ShowLoadingOverlay(false)`; (3) `Phase` field in `ReimportProgressInfo` for DELETE-phase progress reporting from API to UI timer.

**Tech Stack:** C# WinForms, .NET 8

---

### Task 1: Add Phase to ReimportProgressInfo + API sending

**Files:**
- Modify: `OperationGuidance_service/Models/Responses/ReimportProgressInfo.cs`
- Modify: `OperationGuidance_service/Controllers/OperationGuidanceApis.cs`

- [ ] **Step 1: Add Phase to DTO**

Read `ReimportProgressInfo.cs`, add property:
```csharp
        public string? Phase { get; set; }
```

- [ ] **Step 2: Send deleting-phase progress from API**

Read `ReimportPartsBarcode` in `OperationGuidanceApis.cs`. After entering the try block (after the `try {`), but BEFORE the DELETE sql, add:
```csharp
                req.OnProgress?.Invoke(new ReimportProgressInfo { Phase = "deleting" });
```

After DELETE + COUNT, update Phase to "importing" in the first batch progress or right before the loop:
In the progress invoke inside the batch loop (after `batchCount++`), ensure `Phase = "importing"` is set:
```csharp
                    req.OnProgress?.Invoke(new ReimportProgressInfo {
                        Phase = "importing",
                        BatchCount = batchCount,
                        TotalInserted = totalInserted,
                        LastId = lastId,
                        TotalBatches = totalBatches,
                    });
```

- [ ] **Step 3: Build**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_service/Models/Responses/ReimportProgressInfo.cs OperationGuidance_service/Controllers/OperationGuidanceApis.cs
git commit -m "feat: add Phase field to ReimportProgressInfo and send deleting/importing phase progress"
```

---

### Task 2: VisibleChanged sync + Close order + Phase handling

**Files:**
- Modify: `OperationGuidance_new/Views/AdminManagementView.cs`

- [ ] **Step 1: Add OnBackdropVisibleChanged method**

Insert after `ShowLoadingOverlay` method:

```csharp
        private void OnBackdropVisibleChanged(object? sender, EventArgs e) {
            if (!_overlayBackdrop.Visible) {
                _overlayPopup.Hide();
            } else if (_overlayPopup != null && !_overlayPopup.IsDisposed) {
                _overlayPopup.Show();
            }
        }
```

- [ ] **Step 2: Update ShowLoadingOverlay(true)**

In the show branch, after `_overlayBackdrop.Show()` and `_overlayPopup.Show()`, subscribe:
```csharp
                _overlayBackdrop.VisibleChanged += OnBackdropVisibleChanged;
```

- [ ] **Step 3: Update ShowLoadingOverlay(false)**

Replace the entire else branch:
```csharp
            } else {
                _overlayBackdrop.VisibleChanged -= OnBackdropVisibleChanged;
                _overlayBackdrop.Close();
            }
```
(Removes `_overlayPopup.Close();` — cascade from backdrop handles popup close.)

- [ ] **Step 4: Handle "deleting" phase in OnProgressTimerTick**

In `OnProgressTimerTick`, add a branch for the "deleting" phase. Find the `if (progress != null && progress.TotalBatches > 0)` block and add BEFORE it:
```csharp
            if (progress != null && progress.Phase == "deleting") {
                string elapsed = _reimportStopwatch.Elapsed.ToString(@"hh\:mm\:ss");
                _reimportLogBox.AppendText(
                    $"[{DateTime.Now:HH:mm:ss}] 正在清空旧数据... 已耗时 {elapsed}\r\n");
                _reimportLogBox.ScrollToCaret();
            } else
```

The existing `if (progress != null && progress.TotalBatches > 0)` block follows with no change.

- [ ] **Step 5: Build**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 6: Commit**

```bash
git add OperationGuidance_new/Views/AdminManagementView.cs
git commit -m "fix: add VisibleChanged sync, fix close order, handle deleting phase in timer"
```

---

### Task 3: Final build and verify

- [ ] **Step 1: Full build**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: 0 errors.

- [ ] **Step 2: Commit log**

Run: `git log --oneline -4`

- [ ] **Step 3: Smoke test checklist**

1. Login as admin, click "重新导入物料码"
2. Verify log shows "正在清空旧数据..." during DELETE phase
3. Verify log shows batch progress after DELETE completes
4. Click taskbar icon to minimize → BOTH backdrop AND popup minimize together
5. Click taskbar icon to restore → both restore correctly
6. Wait for completion → completion popup appears, click OK
7. Verify NO leftover overlay forms after clicking OK
