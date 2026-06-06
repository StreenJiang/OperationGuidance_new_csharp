# Skip Screw Export & PSet/Lock Fix — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix skip-screw export (material code in row 2), add diagnostics for mission_record save, fix Lock/Unlock cooldown mechanism.

**Architecture:** All lock/unlock changes are isolated in `ToolTask.cs`. Export changes touch `DataExportService.cs` + two callers. Mission record diagnostics touch one file.

**Tech Stack:** C#, WinForms, .NET, TCP sockets, ClosedXML (Excel export)

**Spec:** `docs/superpowers/specs/2026-06-06-skip-screw-export-and-pset-retry-design.md`

---

## File Map

| File | Action | Purpose |
|---|---|---|
| `OperationGuidance_new/Utils/DataExportService.cs` | Modify | Add `PartsBarCode` to `ExportRequest`, insert row on empty data |
| `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs` | Modify | Pass `PartsBarCode` to `ExportRequest` |
| `OperationGuidance_new/Views/AbstractViews/AVariableSettingsView.cs` | Modify | Pass `PartsBarCode` (null) to `ExportRequest` |
| `OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs` | Modify | Add try-catch + logging around `AddOrUpdateMissionRecord` |
| `OperationGuidance_new/Tasks/ToolTask.cs` | Modify | Lock/Unlock cooldown on send + response mismatch reset |

---

### Task 1: Export — Add PartsBarCode to ExportRequest and use it

**Files:**
- Modify: `OperationGuidance_new/Utils/DataExportService.cs:7-19` (ExportRequest class)
- Modify: `OperationGuidance_new/Utils/DataExportService.cs:43-54` (ExportAsync method)

- [ ] **Step 1: Add `PartsBarCode` property to `ExportRequest`**

After `public string ProductBarCode { get; init; }` add:
```csharp
        public string? PartsBarCode { get; init; }
```

- [ ] **Step 2: Insert material-code row when data empty in `ExportAsync`**

After `var rows = BuildRows(data, propertyNames);` add:
```csharp
            // Skip-screw: insert material-code row when data is empty
            if (rows.Count == 0 && !string.IsNullOrEmpty(request.PartsBarCode)) {
                int partsColIndex = propertyNames.IndexOf("parts_bar_code");
                if (partsColIndex >= 0) {
                    var materialRow = new List<object?>(new object?[propertyNames.Count]);
                    materialRow[partsColIndex] = request.PartsBarCode;
                    rows.Add(materialRow);
                    _logger.Info($"[DataExport] Inserted parts_bar_code row for empty data: col={partsColIndex}, value={request.PartsBarCode}");
                }
            }
```

- [ ] **Step 3: Build and verify**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_new/Utils/DataExportService.cs
git commit -m "feat(export): add PartsBarCode to ExportRequest, insert material-code row for empty data"
```

---

### Task 2: Export — Pass PartsBarCode from callers

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs:2751-2759`
- Modify: `OperationGuidance_new/Views/AbstractViews/AVariableSettingsView.cs:513-525`

- [ ] **Step 1: In `AWorkplaceContentPanel.OnMissionCompleted`**, add line to ExportRequest:
```csharp
                    PartsBarCode = _missionRecord?.parts_bar_code,
```

- [ ] **Step 2: In `AVariableSettingsView` test export**, add line to ExportRequest:
```csharp
                    PartsBarCode = null,
```

- [ ] **Step 3: Build and verify**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs OperationGuidance_new/Views/AbstractViews/AVariableSettingsView.cs
git commit -m "feat(export): pass PartsBarCode to ExportRequest from all callers"
```

---

### Task 3: Mission Record — Add diagnostic logging

**Files:**
- Modify: `OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs:1188-1198`

- [ ] **Step 1: Wrap `AddOrUpdateMissionRecord` call with try-catch + detailed logging**

Replace:
```csharp
                _apis.AddOrUpdateMissionRecord(new(_missionRecord));
```

With:
```csharp
                logger.Info($"[SCII:SkipScrew] Saving mission_record: mission_id={_missionRecord.mission_id}, " +
                    $"parts_bar_code={_missionRecord.parts_bar_code}, product_batch={_missionRecord.product_batch}, " +
                    $"workstation_id={_missionRecord.workstation_id}, workstation_name={_missionRecord.workstation_name}");
                try {
                    var rsp = _apis.AddOrUpdateMissionRecord(new(_missionRecord));
                    if (rsp?.MissionRecordDTO != null) {
                        logger.Info($"[SCII:SkipScrew] mission_record saved OK, id={rsp.MissionRecordDTO.id}");
                    } else {
                        logger.Warn($"[SCII:SkipScrew] mission_record save returned null response");
                    }
                } catch (Exception ex) {
                    logger.Error($"[SCII:SkipScrew] mission_record save FAILED: {ex}", ex);
                }
```

- [ ] **Step 2: Build and verify**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs
git commit -m "fix(skip-screw): add diagnostic logging around mission_record save"
```

---

### Task 4: ToolTask — Lock/Unlock cooldown on send + response mismatch reset

> **Note:** All changes in ONE commit — single logical unit.

**Files:**
- Modify: `OperationGuidance_new/Tasks/ToolTask.cs`

**Design rules:**
- Cooldown timestamps set on **send** (not on tool response)
- Lock and Unlock cooldowns are **independent**
- `ForceSendLock`/`ForceSendUnlock` bypass all checks, set same-direction cooldown
- `_pendingLockState` (0/1/-1) tracks expected direction
- `UpdateInternalLockState`: if tool response does NOT match expected state, reset that direction's cooldown

- [ ] **Step 1: Rename constant (line 22)**

Change `private readonly int LockingCooldownPeriod = 5000;` to `private const int LockCooldownMs = 5000;`

- [ ] **Step 2: Add `_pendingLockState` field (after line 38)**

Insert after `private long _lastUnlockTimestamp = 0;`:
```csharp
        private volatile int _pendingLockState = 0;  // 0=none, 1=waiting lock, -1=waiting unlock
```

- [ ] **Step 3: Replace `SendLock` (lines 597-616)**

```csharp
        public void SendLock() {
            lock (LockSyncObject) {
                if (!Connected) {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Lock failed - not connected");
                    return;
                }
                if (IsInCooldown(Volatile.Read(ref _lastLockTimestamp))) return;
                if (_locked) return;

                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Locking");
                Volatile.Write(ref _lastLockTimestamp, DateTimeOffset.Now.ToUnixTimeMilliseconds());
                Volatile.Write(ref _pendingLockState, 1);
                PerformLock();
            }
        }
```

- [ ] **Step 4: Replace `ForceSendLock` (lines 619-629)**

```csharp
        public void ForceSendLock() {
            lock (LockSyncObject) {
                if (!Connected) {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Force lock failed - not connected");
                    return;
                }

                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Force locking");
                PerformLock();
                UpdateInternalLockState(true);
                Volatile.Write(ref _lastLockTimestamp, DateTimeOffset.Now.ToUnixTimeMilliseconds());
            }
        }
```

- [ ] **Step 5: Replace `SendUnlock` (lines 654-673)**

```csharp
        public void SendUnlock() {
            lock (LockSyncObject) {
                if (!Connected) {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Unlock failed - not connected");
                    return;
                }
                if (IsInCooldown(Volatile.Read(ref _lastUnlockTimestamp))) return;
                if (!_locked) return;

                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Unlocking");
                Volatile.Write(ref _lastUnlockTimestamp, DateTimeOffset.Now.ToUnixTimeMilliseconds());
                Volatile.Write(ref _pendingLockState, -1);
                PerformUnlock();
            }
        }
```

- [ ] **Step 6: Replace `ForceSendUnlock` (lines 676-687)**

```csharp
        public void ForceSendUnlock() {
            lock (LockSyncObject) {
                if (!Connected) {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Force unlock failed - not connected");
                    return;
                }

                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Force unlocking");
                PerformUnlock();
                UpdateInternalLockState(false);
                Volatile.Write(ref _lastUnlockTimestamp, DateTimeOffset.Now.ToUnixTimeMilliseconds());
            }
        }
```

- [ ] **Step 7: Replace `UpdateInternalLockState` (lines 744-763)**

```csharp
        private void UpdateInternalLockState(bool newLockedState) {
            bool oldLocked = _locked;
            _locked = newLockedState;
            int expected = Volatile.Read(ref _pendingLockState);

            if (expected == 1 && !newLockedState) {
                // Sent Lock but tool reports unlocked -> Lock failed, reset cooldown
                Volatile.Write(ref _lastLockTimestamp, 0);
                logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Lock failed (tool reports unlocked), cooldown reset");
            } else if (expected == -1 && newLockedState) {
                // Sent Unlock but tool reports locked -> Unlock failed, reset cooldown
                Volatile.Write(ref _lastUnlockTimestamp, 0);
                logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Unlock failed (tool reports locked), cooldown reset");
            }
            Volatile.Write(ref _pendingLockState, 0);

            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Lock state: {oldLocked} -> {_locked}");
        }
```

- [ ] **Step 8: Update `IsInCooldown` (line 734-741)**

Replace `LockingCooldownPeriod` with `LockCooldownMs`:
```csharp
        private bool IsInCooldown(long timestamp) {
            if (timestamp == 0) return false;
            long currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            return (currentTime - timestamp) < LockCooldownMs;
        }
```

- [ ] **Step 9: Build and verify**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: Build succeeded, 0 errors, 0 warnings.

- [ ] **Step 10: Commit**

```bash
git add OperationGuidance_new/Tasks/ToolTask.cs
git commit -m "fix(tool): Lock/Unlock cooldown on send, response mismatch resets cooldown

- Cooldown timestamps set on send (not on tool response), prevents dead loop
- Lock and Unlock cooldowns are independent
- _pendingLockState tracks expected direction
- UpdateInternalLockState: if tool response contradicts expected state, reset cooldown
- ForceSendLock/ForceSendUnlock bypass all checks but set same-direction cooldown"
```

---

### Task 5: Full build + sanity check

- [ ] **Step 1: Full clean rebuild**

```bash
dotnet clean OperationGuidance_new/OperationGuidance_new.csproj
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: Clean build, 0 errors, 0 warnings.

- [ ] **Step 2: Verify all changes are committed**

```bash
git status
git log --oneline -5
```

Expected: Clean working tree, 4 commits on the feature branch.

---

## Summary

| Task | Files | Commit |
|---|---|---|
| 1 | `DataExportService.cs` | feat(export): add PartsBarCode to ExportRequest |
| 2 | `AWorkplaceContentPanel.cs`, `AVariableSettingsView.cs` | feat(export): pass PartsBarCode from all callers |
| 3 | `WorkplaceMissionView_SCII.cs` | fix(skip-screw): add diagnostic logging |
| 4 | `ToolTask.cs` | fix(tool): Lock/Unlock cooldown on send |
| 5 | Build + sanity check | (no commit) |

**Total: 5 files modified, 4 commits**
