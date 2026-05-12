# Buzzer Settings Toggle Design (SCII Only)

## Summary

Add a buzzer enable/disable toggle in `VariableSettingsView_SCII` that persists as a configuration item (default: off). When off, `BuzzerController` silently skips all operations. When on, a `CommonButtonGroup` with two test buttons ("测试开"/"测试关") becomes enabled for testing the buzzer hardware.

## Architecture

### 1. Persistence (`MainUtils` + `IniFileKeys`)

Follow existing settings pattern:

- `IniFileKeys.MissionBuzzerEnabled` — ini key
- `MainUtils.IsBuzzerEnabled()` → reads ini, defaults to false
- `MainUtils.DefaultIsBuzzerEnabled()` → `false`
- `MainUtils.SetBuzzerEnabled(bool)` → writes ini

### 2. BuzzerController — guard check

`TurnOn()` and `TurnOff()` check `MainUtils.IsBuzzerEnabled()` before executing commands. When disabled, log at debug level and return early.

### 3. UI (`VariableSettingsView_SCII`)

Add to "操作配置" section:

- `ToggleButtonGroup` — label "启用蜂鸣器", default unchecked
- `CommonButtonGroup` — two buttons "测试开" / "测试关", initially `Enabled = false`
- Toggle `CheckedChanged`: set test buttons `Enabled` to match toggle state
- Test button clicks: directly call `BuzzerController.TurnOn()` / `TurnOff()`
- `SaveMissionSettings()` override: persist toggle state via `MainUtils.SetBuzzerEnabled()`
- `LoadSettings()` override: load toggle state via `MainUtils.IsBuzzerEnabled()`
- `ResizeMissionSettings()` override: calculate sizes for both new controls

## Files Changed

| File | Change |
|------|--------|
| `Configs/IniFileKeys.cs` | Add `MissionBuzzerEnabled` key |
| `Utils/MainUtils.cs` | Add `IsBuzzerEnabled()` / `SetBuzzerEnabled()` / `DefaultIsBuzzerEnabled()` |
| `Utils/BuzzerController.cs` | `TurnOn()`/`TurnOff()` guard with `IsBuzzerEnabled()` check |
| `Views/VariableSettingsView_SCII.cs` | Add toggle + test buttons + save/load/resize |

## Constraints

- SCII version only — UI controls only in `VariableSettingsView_SCII`
- Default off — user must manually enable via settings
- Test buttons disabled (greyed) when toggle is off, not hidden
- BuzzerController guard applies globally, not just to SCII paths — if another version somehow calls it, the guard still works
- Test buttons bypass the NG flow entirely — they call `BuzzerController` directly for hardware testing
