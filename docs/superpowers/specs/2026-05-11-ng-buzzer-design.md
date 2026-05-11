# NG Buzzer Design (SCII Only)

## Summary

When mission NG reaches max count and fails (SCII version only), activate a buzzer (light + sound) before the admin password dialog appears, and deactivate it after the correct password is entered.

## Architecture

### New: `BuzzerController` (`OperationGuidance_new/Utils/BuzzerController.cs`)

A static utility class that controls the DiDi buzzer hardware. No version check — callers decide when to use it.

```
BuzzerController.TurnOn()   → DiDi.exe POSTCOMMAND=01050000F00089CA (light on)
                            → DiDi.exe POSTCOMMAND=01050003F00079CA (sound on)

BuzzerController.TurnOff()  → DiDi.exe POSTCOMMAND=010500000000CDCA (light off)
                            → DiDi.exe POSTCOMMAND=0105000300003DCA (sound off)
```

- Locates `DiDi.exe` at `Application.StartupPath/didi_control/DiDi.exe`
- Invokes via `Process.Start` with `WindowStyle = ProcessWindowStyle.Hidden`
- Fire-and-forget — does not wait for process exit
- If the exe is missing, logs a warning and continues (no crash)

### Modified: `AWorkplaceContentPanel.MissionNGConfirmPopUp`

Change from `protected void` to `protected virtual void` so SCII subclass can override.

### Modified: `WorkplaceContentPanel_SCII` (overrides `MissionNGConfirmPopUp`)

```
TurnOn → base.MissionNGConfirmPopUp → TurnOff
```

Since `ShowDialog()` is blocking, `TurnOff()` only executes after the dialog returns (password validated).

## Files Changed

| File | Change |
|------|--------|
| `Utils/BuzzerController.cs` | New — buzzer hardware control |
| `Views/AbstractViews/AWorkplaceContentPanel.cs` | `MissionNGConfirmPopUp` → add `virtual` |
| `Views/WorkplaceMissionView_SCII.cs` | Override `MissionNGConfirmPopUp`, add buzzer on/off |

## Constraints

- SCII version only — `WorkplaceContentPanel_SCII` is only instantiated when `AppVersion == SCII`
- Only triggers on mission NG (max NG count), not on bolt NG or other admin password scenarios
- `allowCancel` is `false` for this dialog, so the user cannot dismiss without correct password
- If `DiDi.exe` is not found at the expected path, logs warning and continues silently
