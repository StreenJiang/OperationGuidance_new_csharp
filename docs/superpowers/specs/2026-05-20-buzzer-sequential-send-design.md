# Buzzer Sequential Send Design

**Date**: 2026-05-20
**Status**: Approved

## Problem

`BuzzerController.TurnOn()` sends `LightOnCmd` + `SoundOnCmd` back-to-back via `Process.Start`, same for `TurnOff()`. Two processes are launched nearly simultaneously — the hardware buzzer only registers one of the two commands.

## Solution

Make `SendCommand` asynchronous with `Task.Run` + `WaitForExit(timeout)`, ensuring each command completes before the next starts. Public API changes from sync `TurnOn()`/`TurnOff()` to async `TurnOnAsync()`/`TurnOffAsync()`.

## Changes

### `BuzzerController.cs`

- `SendCommand` → `SendCommandAsync`: wraps `Process.Start` + `WaitForExit(3000)` in `Task.Run`
- `TurnOn/TurnOff` → `TurnOnAsync/TurnOffAsync`: `await` each command sequentially
- No change to `ExePath`, `LightOnCmd`, `SoundOnCmd`, `LightOffCmd`, `SoundOffCmd`

### Callers (fire-and-forget)

- `WorkplaceMissionView_SCII.cs` line 1174, 1180: `_ = BuzzerController.TurnOnAsync();` / `TurnOffAsync();`
- `VariableSettingsView_SCII.cs` line 36, 40: same pattern

## Rationale

- `WaitForExit()` blocks — delegated to thread pool via `Task.Run` to keep UI responsive
- 3-second timeout prevents hangs if DiDi.exe misbehaves
- `_ =` discards the Task; callers don't need to await
- API rename to `*Async` signals the change to any future callers
