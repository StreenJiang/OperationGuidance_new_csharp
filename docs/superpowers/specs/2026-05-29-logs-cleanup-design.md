# Logs Cleanup Utility — Design Spec

## Purpose

Automatically delete log files older than N days (default 10) on each application startup. Run silently in background without blocking UI.

## Components

### 1. New: `OperationGuidance_new/Utils/LogsCleanupUtils.cs`

Static utility class:

```csharp
public static class LogsCleanupUtils
{
    private static readonly ILog logger;

    /// <summary>
    /// Delete .log files in logs\ older than retentionDays.
    /// Runs silently — errors are logged but never shown to user.
    /// </summary>
    public static void CleanOldLogs(int retentionDays)
    {
        // Guard: retentionDays <= 0 → skip
        // Path: MainUtils.GetBaseDirectory() + "logs\"
        // If directory doesn't exist → return
        // Enumerate *.log files
        // Parse prefix as yyyy-MM-dd
        // Delete if parsed date < DateTime.Now.AddDays(-retentionDays)
        // Try-catch per file, log individual failures, continue
    }
}
```

### 2. Modifications

| File | Change |
|---|---|
| `OperationGuidance_new/Configs/IniFileKeys.cs` | Add `LogsRetentionDays` key |
| `OperationGuidance_new/Utils/MainUtils.cs` | Add `GetLogsRetentionDays()`, `SetLogsRetentionDays(int)`, `DefaultLogsRetentionDays()` — follow existing ini-read/write/default pattern |
| `OperationGuidance_new/Views/AbstractViews/AVariableSettingsView.cs` | Add "日志保留天数" `CustomTextBoxGroup` (PositiveIntOnly) in system settings section, after auto-login toggle |
| `OperationGuidance_new/Program.cs` | After `DependencyInjector.Initialize()`, call `Task.Run(() => LogsCleanupUtils.CleanOldLogs(MainUtils.GetLogsRetentionDays()))` |

### 3. Configuration

- **Key**: `LogsRetentionDays`
- **Default**: `10`
- **UI label**: "日志保留天数"
- **UI placement**: `_systemSettingsContentPanel`, inline with `_autoLoginToggle`, same Row flow

### 4. Integration Point (Program.cs)

```csharp
DependencyInjector.Initialize();

// Background: clean old logs
try {
    int retentionDays = MainUtils.GetLogsRetentionDays();
    Task.Run(() => LogsCleanupUtils.CleanOldLogs(retentionDays));
} catch { } // double-safety: even the ini read + task creation fails silently

// Run main form
MainForm mainForm = new MainForm();
```

## Data Flow

```
App Start → Program.Main()
  → read LogsRetentionDays from ini (default 10)
  → Task.Run: LogsCleanupUtils.CleanOldLogs(10)
    → MainUtils.GetBaseDirectory() + "logs\"
    → enumerate *.log
    → parse filename → delete if too old
    → errors → log4net only
  → main form launches immediately (non-blocking)
```

## Error Handling

- `logs\` directory not found: return silently
- File locked by log4net: skip file, log warning, continue to next
- Invalid filename format: skip, continue
- All exceptions caught per-file; never bubble up

## Edge Cases

- `retentionDays = 0`: treat as "skip cleanup" (guard clause)
- No log files exist: return immediately
- Today's log file: `2026-05-29.log` date matches today → not deleted (exclusive `<`, not `<=`)

## Test Plan

1. Manually create fake old log files with dates 11-15 days ago; verify only 11+ day files deleted
2. Verify today's file and files within retention period are kept
3. Verify cleanup runs without blocking main form launch
4. Verify save/load of retention days setting in AVariableSettingsView
5. Verify default value = 10 on fresh install
