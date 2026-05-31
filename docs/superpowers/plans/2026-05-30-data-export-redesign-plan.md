# Data Export Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace per-bolt file export with task-completion-triggered export via DataExportService, atomic .tmp→Move writes, and ExportConfig caching.

**Architecture:** Template-method in AWorkplaceContentPanel (virtual switches default false). SCII overrides switches from ExportConfig. All export controls Hidden in base → SCII Shows needed ones.

**Tech Stack:** C#, WinForms, ClosedXML, ConcurrentDictionary, SemaphoreSlim

---

## File Structure

| File | Action |
|------|--------|
| `Utils/ExportConfig.cs` | **Create** — singleton wrapping Settings DTO |
| `Utils/DataExportService.cs` | **Create** — atomic export service |
| `Configs/DTOs/Setting.cs` | **Modify** — remove name format, add toggle fields, fix GetSortConfig |
| `Configs/IniFileKeys.cs` | **Modify** — remove DataStorageNameFormat |
| `Utils/MainUtils.cs` | **Modify** — delegate storage to ExportConfig, remove filename methods |
| `Extensions/ExtensionMethods.cs` | **Delete** |
| `Views/DataQueryView.cs` | **Modify** — inline ExportToExcelFile |
| `Views/AbstractViews/AWorkplaceContentPanel.cs` | **Modify** — remove old export, add OnMissionCompleted |
| `Views/AbstractViews/AVariableSettingsView.cs` | **Modify** — toggles, test button, dependency logic |
| `Views/VariableSettingsView_SCII.cs` | **Modify** — Show export controls |
| `Views/WorkplaceMissionView_SCII.cs` | **Modify** — override export switches |

---

### Task 1: ExportConfig + Settings DTO

- Remove `data_storage_name_format` from `Setting.cs`, add `data_storage_excel_export_enabled` / `data_storage_txt_export_enabled` (int, default NO)
- Fix `Settings.GetSortConfig()` — return deserialized list, not always default
- Remove `DataStorageNameFormat` from `IniFileKeys.cs`
- Update `MainUtils.GetStoragePath/SetStoragePath/GetSortConfig/SetSortConfig/GetSortConfigCurr/SetSortConfigCurr/SetStoreLooseningData` to use `ConfigUtils.LoadConfig<Settings>()` pattern
- Create `Utils/ExportConfig.cs` — singleton, properties: `ExcelExportEnabled`, `TxtExportEnabled`, `StoragePath`, `SortConfig` (cached), methods: `Reload()`, `SetExcelExportEnabled()`, `SetTxtExportEnabled()`
- Add `GetDefaultExcelExportEnabled()` / `GetDefaultTxtExportEnabled()` to MainUtils

### Task 2: DataExportService

- Create `Utils/DataExportService.cs`
- `ExportRequest` DTO: Data, Fields, BasePath, ProductBatch, ProductBarCode, CompletedAt, Result, EnableExcel, EnableTxt
- `ExportAsync()`: build path `{BasePath}/{date}/{batch}/`, filename `{barCode}（{barCode}）_{timestamp}_{Result}.xlsx/.txt`
- `WriteExcelAsync/WriteTxtAsync`: per-file SemaphoreSlim, .tmp→File.Move atomic
- Empty/null → "null" placeholder

### Task 3: AWorkplaceContentPanel

- Delete: `StoreDataToFilesCore`, `StoreDataToFilesAsync`, `StoreDataToFilesAsyncWithTimeout`, `DataStorageLockObj`
- Inline `_tighteningDataVOs.Add` out of inner BeginInvoke in `StoreTighteningDataInternal`
- Remove `fileTask` from parallel WhenAll
- Add `_exportTriggered` field, virtual properties (`IsExcelExportEnabled` etc., all default false)
- Add `OnMissionCompleted(WorkplaceProcessStatus)`: interlock guard, drain via `_storeTighteningDataLock` (5s timeout), snapshot, `DataExportService.ExportAsync`, clear, UI refresh
- Reset `_exportTriggered` in `PrepareBeforeActivatingMission`
- Wire `await OnMissionCompleted(status)` in `TerminateMission` after `_isRedo`

### Task 4: Delete ExtensionMethods + Fix DataQueryView

- Delete `Extensions/ExtensionMethods.cs`
- In `DataQueryView.cs`: replace `using OperationGuidance_new.Extensions` with `using ClosedXML.Excel`, replace `finalData.ExportToExcelFile(...)` with inline ClosedXML code

### Task 5: AVariableSettingsView — Config UI

- Remove `_storageFileNameTextBox` field/property/init/check/save/load/resize
- Add: `_enableExcelExportToggle`, `_enableTxtExportToggle`, `_exportTestButton` (all Hidden in base)
- Public properties for all new controls
- `_enableExcelExportToggle` positioned first in storage panel (above `_storagePathTextBox`)
- `_exportTestButton` after `_storageFieldsButton`, two buttons "导出至Excel"/"导出至Txt"
- `UpdateExportControlsEnabled()`: enable path/fields/test when any toggle is ON
- `RunExportTest(bool, bool)`: 5 fake `OperationDataVO` records, call `DataExportService.ExportAsync`
- `CheckedChanged` subscriptions on both toggles
- Save: call `ExportConfig.SetExcelExportEnabled/SetTxtExportEnabled` + `Reload`
- Load: read originals from `ExportConfig`, set toggle states, call `UpdateExportControlsEnabled`
- Defaults: reset toggles + call `UpdateExportControlsEnabled`
- Resize: update `contentHeight` for 6 controls, add Size/Margin for all new controls in correct order

### Task 6: VariableSettingsView_SCII — Show Controls

```csharp
EnableExcelExportToggle.Show();
StoragePathTextBox.Show();
StorageFieldsButton.Show();
ExportTestButton.Show();
```

### Task 7: WorkplaceMissionView_SCII — Override Switches

```csharp
protected override bool IsExcelExportEnabled => ExportConfig.Instance.ExcelExportEnabled;
protected override bool IsTxtExportEnabled => ExportConfig.Instance.TxtExportEnabled;
protected override string ExportBasePath => ExportConfig.Instance.StoragePath;
protected override List<int> ExportSortConfig => ExportConfig.Instance.SortConfig;
```

### Task 8: MainUtils Cleanup

- Remove `GetStorageFileName()`, `GetDefaultStorageFileName()`, `SetStorageFileName()`, `GetStorageFormattedName()`

### Task 9: Build & Verify

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj  # 0 errors
grep -rn "StoreDataToFilesCore\|ExportToExcelFile\|ExportToTextFile\|GetStorageFormattedName\|DataStorageNameFormat" --include="*.cs" .  # no matches
```
