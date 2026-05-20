# Buzzer Sequential Send Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add sequential command sending with process-wait to BuzzerController so the hardware buzzer receives both light and sound commands.

**Architecture:** `BuzzerController` methods become async — each command runs on a thread pool thread, waits for `DiDi.exe` to exit (3s timeout), then the next command starts. Callers fire-and-forget with `_ =`.

**Tech Stack:** C# WinForms, `System.Diagnostics.Process`, `Task.Run`

---

### Task 1: Refactor BuzzerController

**Files:**
- Modify: `OperationGuidance_new/Utils/BuzzerController.cs`

- [ ] **Step 1: Rewrite SendCommand → SendCommandAsync**

Replace the fire-and-forget `Process.Start` with a `Task.Run`-wrapped `WaitForExit(3000)`.

Replace the entire file content:

```csharp
using System.Diagnostics;
using log4net;

namespace OperationGuidance_new.Utils {
    public static class BuzzerController {
        private static readonly ILog logger = LogManager.GetLogger(typeof(BuzzerController));

        private static readonly string LightOnCmd = "01050000F00089CA";
        private static readonly string LightOffCmd = "010500000000CDCA";
        private static readonly string SoundOnCmd = "01050003F00079CA";
        private static readonly string SoundOffCmd = "0105000300003DCA";

        private static string ExePath =>
            Path.Combine(Application.StartupPath, "didi_control", "DiDi.exe");

        public static async Task TurnOnAsync() {
            await SendCommandAsync(LightOnCmd);
            await SendCommandAsync(SoundOnCmd);
        }

        public static async Task TurnOffAsync() {
            await SendCommandAsync(LightOffCmd);
            await SendCommandAsync(SoundOffCmd);
        }

        private static async Task SendCommandAsync(string command) {
            try {
                if (!File.Exists(ExePath)) {
                    logger.Warn($"[BuzzerController] DiDi.exe not found at: {ExePath}");
                    return;
                }
                await Task.Run(() => {
                    using var process = Process.Start(new ProcessStartInfo {
                        FileName = ExePath,
                        Arguments = $"POSTCOMMAND={command}",
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true,
                    });
                    process?.WaitForExit(3000);
                });
            } catch (Exception ex) {
                logger.Error($"[BuzzerController] Failed to send command '{command}'", ex);
            }
        }
    }
}
```

- [ ] **Step 2: Build to verify compilation errors (expected: 4 call-site errors)**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: 4 errors — `TurnOn()` and `TurnOff()` no longer exist, callers need updating.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Utils/BuzzerController.cs
git commit -m "refactor(buzzer): make SendCommand async with sequential WaitForExit"
```

---

### Task 2: Update callers to async fire-and-forget

**Files:**
- Modify: `OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs:1174,1180`
- Modify: `OperationGuidance_new/Views/VariableSettingsView_SCII.cs:36,40`

- [ ] **Step 1: Update WorkplaceMissionView_SCII.cs**

Change lines 1174 and 1180 from sync to async fire-and-forget:

```csharp
// Line 1174 — was: BuzzerController.TurnOn();
_ = BuzzerController.TurnOnAsync();
```

```csharp
// Line 1180 — was: BuzzerController.TurnOff();
_ = BuzzerController.TurnOffAsync();
```

- [ ] **Step 2: Update VariableSettingsView_SCII.cs**

Change lines 36 and 40 from sync to async fire-and-forget:

```csharp
// Line 36 — was: BuzzerController.TurnOn();
_ = BuzzerController.TurnOnAsync();
```

```csharp
// Line 40 — was: BuzzerController.TurnOff();
_ = BuzzerController.TurnOffAsync();
```

- [ ] **Step 3: Build to verify all errors resolved**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs OperationGuidance_new/Views/VariableSettingsView_SCII.cs
git commit -m "fix(buzzer): update callers to TurnOnAsync/TurnOffAsync fire-and-forget"
```
