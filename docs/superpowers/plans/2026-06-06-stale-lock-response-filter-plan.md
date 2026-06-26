# Stale Lock Response Filter — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Filter stale Lock/Unlock responses from PF6000 tool to eliminate false "Unlock failed" warnings and reduce bolt-transition latency.

**Architecture:** Time-based staleness check in `UpdateInternalLockState`. When a tool response contradicts `_pendingLockCommand` but the current command was sent very recently (< configurable ms, default 100ms), the response is a stale leftover from a previous command — discard it instead of treating it as failure.

**Tech Stack:** C#, .NET, WinForms

**Spec:** `docs/superpowers/specs/2026-06-06-stale-lock-response-filter-design.md`

---

## File Map

| File | Action | Purpose |
|---|---|---|
| `OperationGuidance_new/Configs/IniFileKeys.cs` | Modify | Add config key |
| `OperationGuidance_new/Utils/MainUtils.cs` | Modify | Add public getter with default 100ms |
| `OperationGuidance_new/Tasks/ToolTask.cs` | Modify | Staleness check in `UpdateInternalLockState` |

**Total: 3 files modified, 1 commit**

---

### Task 1: Add config key and getter

**Files:**
- Modify: `OperationGuidance_new/Configs/IniFileKeys.cs:30` (add key)
- Modify: `OperationGuidance_new/Utils/MainUtils.cs` (add getter)

- [ ] **Step 1: Add key to IniFileKeys**

After `public static string LogsRetentionDays => "logs_retention_days";` (line 30), add:

```csharp
        public static string StaleResponseDelayMs => "stale_response_delay_ms";
```

- [ ] **Step 2: Add getter to MainUtils**

After `SetLogsRetentionDays` (line 720), add:

```csharp
        public static int GetStaleResponseDelayMs() {
            string value = Settings.Read(IniFileKeys.StaleResponseDelayMs);
            if (string.IsNullOrEmpty(value)) {
                Settings.Write(IniFileKeys.StaleResponseDelayMs, "100");
                return 100;
            }
            return int.TryParse(value, out int result) ? result : 100;
        }
```

- [ ] **Step 3: Build and verify**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: Build succeeded, 0 errors.

---

### Task 2: Add staleness filter to UpdateInternalLockState

**Files:**
- Modify: `OperationGuidance_new/Tasks/ToolTask.cs:24` (field)
- Modify: `OperationGuidance_new/Tasks/ToolTask.cs:617-629` (ForceSendLock)
- Modify: `OperationGuidance_new/Tasks/ToolTask.cs:670-682` (ForceSendUnlock)
- Modify: `OperationGuidance_new/Tasks/ToolTask.cs:725-741` (UpdateInternalLockState)

- [ ] **Step 1: Add config-backed static field**

Replace line 24:
```csharp
        private const int LockCooldownMs = 5000;
```

With:
```csharp
        private const int LockCooldownMs = 5000;
        private static readonly int StaleResponseThresholdMs = MainUtils.GetStaleResponseDelayMs();
```

- [ ] **Step 2: Clear `_pendingLockCommand` before force-path lock state updates**

In `ForceSendLock` (line 626), add clear before `UpdateInternalLockState`:

```csharp
        public void ForceSendLock() {
            lock (LockSyncObject) {
                if (!Connected) {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Force lock failed - not connected");
                    return;
                }

                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Force locking");
                PerformLock();
                _pendingLockCommand = PendingLockCommand.None;
                UpdateInternalLockState(true);
                _lastLockTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }
        }
```

In `ForceSendUnlock` (line 679), add clear before `UpdateInternalLockState`:

```csharp
        public void ForceSendUnlock() {
            lock (LockSyncObject) {
                if (!Connected) {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Force unlock failed - not connected");
                    return;
                }

                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Force unlocking");
                PerformUnlock();
                _pendingLockCommand = PendingLockCommand.None;
                UpdateInternalLockState(false);
                _lastUnlockTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }
        }
```

- [ ] **Step 3: Replace `UpdateInternalLockState` method body**

Replace the method at lines 725-741:

```csharp
        private void UpdateInternalLockState(bool newLockedState) {
            bool oldLocked = _locked;
            var expected = _pendingLockCommand;
            long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            if (expected == PendingLockCommand.Lock && !newLockedState) {
                long lockAge = now - Volatile.Read(ref _lastLockTimestamp);
                if (lockAge < StaleResponseThresholdMs) {
                    logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Stale Unlock response discarded (expecting Lock, lock sent {lockAge}ms ago)");
                    return;
                }
                if (lockAge >= LockCooldownMs) {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Lock response timeout after {lockAge}ms, clearing pending state");
                } else {
                    _lastLockTimestamp = 0;
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Lock failed (tool reports unlocked), cooldown reset");
                }
            } else if (expected == PendingLockCommand.Unlock && newLockedState) {
                long unlockAge = now - Volatile.Read(ref _lastUnlockTimestamp);
                if (unlockAge < StaleResponseThresholdMs) {
                    logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Stale Lock response discarded (expecting Unlock, unlock sent {unlockAge}ms ago)");
                    return;
                }
                if (unlockAge >= LockCooldownMs) {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Unlock response timeout after {unlockAge}ms, clearing pending state");
                } else {
                    _lastUnlockTimestamp = 0;
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Unlock failed (tool reports locked), cooldown reset");
                }
            }

            _locked = newLockedState;
            _pendingLockCommand = PendingLockCommand.None;

            if (oldLocked != _locked) {
                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Lock state: {oldLocked} -> {_locked}");
            }
        }
```

- [ ] **Step 4: Build and verify**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add OperationGuidance_new/Configs/IniFileKeys.cs OperationGuidance_new/Utils/MainUtils.cs OperationGuidance_new/Tasks/ToolTask.cs
git commit -m "fix(tool): discard stale Lock/Unlock responses within configurable threshold

When ForceSendLock clears _pendingLockCommand before a delayed Lock
response arrives, and SendUnlock then sets _pendingLockCommand=Unlock,
the stale 'Lock ok' is misidentified as 'Unlock failed'. Add time-based
staleness check: contradictory responses within StaleResponseThresholdMs
(default 100ms) are discarded instead of triggering cooldown reset + retry.

Threshold is configurable via ini key stale_response_delay_ms (no UI).

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Summary

| Task | Files | Commit |
|---|---|---|
| 1 | `IniFileKeys.cs`, `MainUtils.cs` | (combined with Task 2) |
| 2 | `ToolTask.cs` | fix(tool): discard stale Lock/Unlock responses |
