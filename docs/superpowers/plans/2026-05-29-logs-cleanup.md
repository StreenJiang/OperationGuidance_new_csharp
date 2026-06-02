# Logs Cleanup Utility — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a utility that deletes log files older than a configurable N days (default 10) on each app startup, silently in background.

**Architecture:** A static utility class `LogsCleanupUtils` called via `Task.Run` from `Program.Main()`. Configurable via ini file (key: `logs_retention_days`), with UI in `AVariableSettingsView` system settings section.

**Tech Stack:** C#, .NET 6, WinForms, log4net

---

### Task 1: Add ini file key

**Files:**
- Modify: `OperationGuidance_new/Configs/IniFileKeys.cs:28-30`

- [ ] **Step 1: Add `LogsRetentionDays` property to `IniFileKeys`**

```csharp
// In OperationGuidance_new/Configs/IniFileKeys.cs, after line 30 (AutoLoginInfo)
public static string LogsRetentionDays => "logs_retention_days";
```

- [ ] **Step 2: Verify file compiles**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

---

### Task 2: Add MainUtils config getter/setter

**Files:**
- Modify: `OperationGuidance_new/Utils/MainUtils.cs` (insert after `SetAutoLoginInfo`, around line 697)

- [ ] **Step 1: Add `GetLogsRetentionDays`, `SetLogsRetentionDays`, `DefaultLogsRetentionDays` to `MainUtils`**

Insert after the `SetAutoLoginInfo` method (around line 697), before the `// Ping util method` comment block:

```csharp
// Logs retention days
public static int GetLogsRetentionDays() {
    string value = Settings.Read(IniFileKeys.LogsRetentionDays);
    if (string.IsNullOrEmpty(value)) {
        int days = DefaultLogsRetentionDays();
        SetLogsRetentionDays(days);
        return days;
    }
    return int.Parse(value);
}
public static int DefaultLogsRetentionDays() => 10;
public static void SetLogsRetentionDays(int days) => Settings.Write(IniFileKeys.LogsRetentionDays, days + "");
```

- [ ] **Step 2: Build and verify**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

---

### Task 3: Create LogsCleanupUtils.cs

**Files:**
- Create: `OperationGuidance_new/Utils/LogsCleanupUtils.cs`

- [ ] **Step 1: Create the file with full content**

```csharp
using log4net;

namespace OperationGuidance_new.Utils {
    public static class LogsCleanupUtils {
        private static readonly ILog logger = LogManager.GetLogger(typeof(LogsCleanupUtils));

        public static void CleanOldLogs(int retentionDays) {
            if (retentionDays <= 0) return;

            string logsDir = MainUtils.GetBaseDirectory() + "logs\\";
            if (!Directory.Exists(logsDir)) return;

            DateTime cutoff = DateTime.Now.AddDays(-retentionDays);
            string[] logFiles = Directory.GetFiles(logsDir, "*.log");

            foreach (string filePath in logFiles) {
                try {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    if (DateTime.TryParseExact(fileName, "yyyy-MM-dd", null,
                            System.Globalization.DateTimeStyles.None, out DateTime fileDate)) {
                        if (fileDate < cutoff) {
                            File.Delete(filePath);
                        }
                    }
                } catch (Exception ex) {
                    logger.Warn($"Failed to delete old log file [{filePath}]: {ex.Message}");
                }
            }
        }
    }
}
```

- [ ] **Step 2: Build and verify**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

---

### Task 4: Call cleanup from Program.cs on startup

**Files:**
- Modify: `OperationGuidance_new/Program.cs:21-24`

- [ ] **Step 1: Add background cleanup call after `DependencyInjector.Initialize()`**

Replace:
```csharp
                // Initialize dependencies injection 
                DependencyInjector.Initialize();
                // Run main form
```

With:
```csharp
                // Initialize dependencies injection 
                DependencyInjector.Initialize();

                // Background: clean old log files
                try {
                    int retentionDays = MainUtils.GetLogsRetentionDays();
                    Task.Run(() => LogsCleanupUtils.CleanOldLogs(retentionDays));
                } catch { }

                // Run main form
```

- [ ] **Step 2: Build and verify**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

---

### Task 5: Add UI controls to AVariableSettingsView — fields & init

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/AVariableSettingsView.cs:44-45` (fields)
- Modify: `OperationGuidance_new/Views/AbstractViews/AVariableSettingsView.cs:264-268` (init)

- [ ] **Step 1: Add fields after `_autoLoginOriginal`**

Replace:
```csharp
        private ToggleButtonGroup _autoLoginToggle;
        private bool _autoLoginOriginal;
```

With:
```csharp
        private ToggleButtonGroup _autoLoginToggle;
        private bool _autoLoginOriginal;
        private CustomTextBoxButtonGroup _logsRetentionDaysBox;
        private int _logsRetentionDaysOriginal;
```

- [ ] **Step 2: Add init code after `_autoLoginToggle` block in `InitializeSystemSettingsPanel`**

Replace:
```csharp
            _autoLoginToggle = new("自动登录") {
                Parent = _systemSettingsContentPanel,
                Ratio = 6.95,
            };
        }
```

With:
```csharp
            _autoLoginToggle = new("自动登录") {
                Parent = _systemSettingsContentPanel,
                Ratio = 6.95,
            };
            _logsRetentionDaysBox = new("日志保留天数") {
                Parent = _systemSettingsContentPanel,
                Ratio = 6.95,
                PositiveIntOnly = true,
            };
        }
```

- [ ] **Step 3: Build and verify**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

---

### Task 6: Add UI controls — resize layout

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/AVariableSettingsView.cs:879` (contentHeight)
- Modify: `OperationGuidance_new/Views/AbstractViews/AVariableSettingsView.cs:889-890` (add box sizing)

- [ ] **Step 1: Update `ResizeSystemSettingsPanel` — change contentHeight**

Replace:
```csharp
            int contentHeight = _boxNBtnHeight * 2 + _contentVPadding * 2 + boxVMargin;
```

With:
```csharp
            int contentHeight = _boxNBtnHeight * 3 + _contentVPadding * 2 + boxVMargin * 2;
```

- [ ] **Step 2: Add sizing for `_logsRetentionDaysBox` after `_autoLoginToggle` sizing**

Replace:
```csharp
            _autoLoginToggle.Size = new(boxWidth, this._boxNBtnHeight);
            _autoLoginToggle.Margin = new(0, boxVMargin, 0, 0);
```

With:
```csharp
            _autoLoginToggle.Size = new(boxWidth, this._boxNBtnHeight);
            _autoLoginToggle.Margin = new(0, boxVMargin, 0, 0);
            _logsRetentionDaysBox.Size = new(Width - _systemSettingsContentPanel.Padding.Size.Width, _boxNBtnHeight);
            _logsRetentionDaysBox.Margin = new(0, boxVMargin, _contentHGap / 2, 0);
```

- [ ] **Step 3: Build and verify**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

---

### Task 7: Add UI controls — save, load, reset, dirty-check

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/AVariableSettingsView.cs:305-308` (SaveSystemSettings)
- Modify: `OperationGuidance_new/Views/AbstractViews/AVariableSettingsView.cs:986-987` (LoadSettings)
- Modify: `OperationGuidance_new/Views/AbstractViews/AVariableSettingsView.cs:1034-1035` (ResetAllToDefault)
- Modify: `OperationGuidance_new/Views/AbstractViews/AVariableSettingsView.cs:197-198` (CheckSavedFunc_detail)

- [ ] **Step 1: Add save logic in `SaveSystemSettings` after auto-login block**

Replace:
```csharp
            // Auto login
            MainUtils.SetAutoLoginEnabled(_autoLoginToggle.Checked);
            _autoLoginOriginal = _autoLoginToggle.Checked;
            if (!_autoLoginOriginal) {
                MainUtils.SetAutoLoginInfo(MainUtils.GetDefaultAutoLoginInfo());
            }
```

With:
```csharp
            // Auto login
            MainUtils.SetAutoLoginEnabled(_autoLoginToggle.Checked);
            _autoLoginOriginal = _autoLoginToggle.Checked;
            if (!_autoLoginOriginal) {
                MainUtils.SetAutoLoginInfo(MainUtils.GetDefaultAutoLoginInfo());
            }

            // Logs retention days
            int logsRetentionDays = int.Parse(_logsRetentionDaysBox.GetTextBox(0).Box.Text);
            MainUtils.SetLogsRetentionDays(logsRetentionDays);
            _logsRetentionDaysOriginal = logsRetentionDays;
```

- [ ] **Step 2: Add load logic in `LoadSettings` after auto-login lines**

Replace:
```csharp
                    _autoLaunchOriginal = MainUtils.IsAutoLaunchEnabled();
                    _autoLoginOriginal = MainUtils.IsAutoLoginEnabled();
                    _autoLaunchToggle.Checked = _autoLaunchOriginal;
                    _autoLoginToggle.Checked = _autoLoginOriginal;
```

With:
```csharp
                    _autoLaunchOriginal = MainUtils.IsAutoLaunchEnabled();
                    _autoLoginOriginal = MainUtils.IsAutoLoginEnabled();
                    _autoLaunchToggle.Checked = _autoLaunchOriginal;
                    _autoLoginToggle.Checked = _autoLoginOriginal;
                    _logsRetentionDaysOriginal = MainUtils.GetLogsRetentionDays();
                    _logsRetentionDaysBox.SetValue(0, _logsRetentionDaysOriginal + "");
```

- [ ] **Step 3: Add reset logic in `ResetAllToDefault` after auto-login line**

Replace:
```csharp
                    _autoLaunchToggle.Checked = MainUtils.DefaultAutoLaunchEnabled();
                    _autoLoginToggle.Checked = MainUtils.DefaultAutoLoginEnabled();
```

With:
```csharp
                    _autoLaunchToggle.Checked = MainUtils.DefaultAutoLaunchEnabled();
                    _autoLoginToggle.Checked = MainUtils.DefaultAutoLoginEnabled();
                    _logsRetentionDaysBox.SetValue(0, MainUtils.DefaultLogsRetentionDays() + "");
```

- [ ] **Step 4: Add dirty-check in `CheckSavedFunc_detail` after auto-login check**

Replace:
```csharp
            || CheckSvedFuncSeparately(_autoLoginToggle.Checked != _autoLoginOriginal, "自动登录")
            || CheckSvedFuncSeparately(_usbScannerEnabledToggle.Checked != _usbScannerEnabledOriginal, "USB扫码枪")
```

With:
```csharp
            || CheckSvedFuncSeparately(_autoLoginToggle.Checked != _autoLoginOriginal, "自动登录")
            || CheckSvedFuncSeparately(_logsRetentionDaysBox.GetTextBox(0).Box.Text != _logsRetentionDaysOriginal + "", "日志保留天数")
            || CheckSvedFuncSeparately(_usbScannerEnabledToggle.Checked != _usbScannerEnabledOriginal, "USB扫码枪")
```

- [ ] **Step 5: Build final verification**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```
Expected: Build succeeded with 0 errors.

---

### Task 8: Manual smoke test

- [ ] **Step 1: Create test log files in output logs directory**

Find the logs directory (e.g., `OperationGuidance_new/bin/Debug/net6.0-windows/logs/`). Create fake old log files:
- `2026-05-10.log` (19 days ago — should be deleted)
- `2026-05-25.log` (4 days ago — should be kept)
- `2026-05-29.log` (today — should be kept)

- [ ] **Step 2: Launch the application**

Verify: App starts normally with no blocking/delay. Old file `2026-05-10.log` is deleted. `2026-05-25.log` and `2026-05-29.log` remain.

- [ ] **Step 3: Open system settings page**

Navigate to 系统配置 settings. Verify "日志保留天数" input appears below "自动登录". Verify default value is `10`. Change to `5`, click save, then re-open settings — verify value persists as `5`.

- [ ] **Step 4: Reset to defaults**

Click "默认" button in settings, confirm. Verify "日志保留天数" resets to `10`.
