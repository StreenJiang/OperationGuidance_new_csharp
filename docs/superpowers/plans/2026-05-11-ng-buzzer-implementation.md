# NG Buzzer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Activate buzzer (light + sound) before NG admin password dialog and deactivate after correct password in SCII version.

**Architecture:** New `BuzzerController` static class encapsulates DiDi.exe calls. `MissionNGConfirmPopUp` made virtual in base class, overridden in SCII subclass to add buzzer on/off wrapping.

**Tech Stack:** C#, .NET 6, Windows Forms, System.Diagnostics.Process

---

### Task 1: Create BuzzerController

**Files:**
- Create: `OperationGuidance_new/Utils/BuzzerController.cs`

- [ ] **Step 1: Write BuzzerController class**

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

        public static void TurnOn() {
            SendCommand(LightOnCmd);
            SendCommand(SoundOnCmd);
        }

        public static void TurnOff() {
            SendCommand(LightOffCmd);
            SendCommand(SoundOffCmd);
        }

        private static void SendCommand(string command) {
            try {
                if (!File.Exists(ExePath)) {
                    logger.Warn($"[BuzzerController] DiDi.exe not found at: {ExePath}");
                    return;
                }
                Process.Start(new ProcessStartInfo {
                    FileName = ExePath,
                    Arguments = $"POSTCOMMAND={command}",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                });
            } catch (Exception ex) {
                logger.Error($"[BuzzerController] Failed to send command '{command}'", ex);
            }
        }
    }
}
```

- [ ] **Step 2: Verify the file compiles**

No compilation step needed individually — will verify at Task 4 after all changes are complete.

---

### Task 2: Make MissionNGConfirmPopUp virtual

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs`

- [ ] **Step 1: Change method signature from `protected void` to `protected virtual void`**

Locate line 2228 in `AWorkplaceContentPanel.cs`:

```csharp
// Before (line 2228):
protected void MissionNGConfirmPopUp(string msg) {

// After:
protected virtual void MissionNGConfirmPopUp(string msg) {
```

---

### Task 3: Override MissionNGConfirmPopUp in SCII

**Files:**
- Modify: `OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs`

- [ ] **Step 1: Add override method in WorkplaceContentPanel_SCII class**

Add after `BoltNGConfirmPopUp` is called (around line 1380 or any appropriate location within the class):

```csharp
protected override void MissionNGConfirmPopUp(string msg) {
    BuzzerController.TurnOn();
    base.MissionNGConfirmPopUp(msg);
    BuzzerController.TurnOff();
}
```

The exact insertion location is after line 1136 (the closing brace of `CountScrewBitUsedTime`) or any other suitable location within the `WorkplaceContentPanel_SCII` class body, before line 1167 (`DoAfterRecevingTighteningDataAsync`).

---

### Task 4: Build and verify

**Files:** None (verification only)

- [ ] **Step 1: Build the project**

Run: `dotnet build "D:/AllProjects/CsharpProjects/OperationGuidance_new/OperationGuidance_new/OperationGuidance_new.csproj"`

Expected: Build succeeds with no errors.

- [ ] **Step 2: Commit all changes**

```bash
git add OperationGuidance_new/Utils/BuzzerController.cs
git add OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs
git add OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs
git commit -m "feat: add NG buzzer control for SCII mission NG scenario"
```
