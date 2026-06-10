# Standard Version Export Enable — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Open mission-completion auto-export to STANDARD version with Excel+TXT toggles, hide DataQueryView export button, keep all other versions unchanged.

**Architecture:** Show export settings UI in `VariableSettingsView` (STANDARD) + override `IsExcelExportEnabled`/`IsTxtExportEnabled` in `WorkplaceContentPanel` (STANDARD) → WHYC/GLB/TZYX inherit runtime but settings remain hidden. YF gets dedicated `VariableSettingsView_YF` to hide export explicitly. DataQueryView export button commented out.

**Tech Stack:** C#, WinForms, ClosedXML, .NET Framework 4.8

---

## File Structure

| File | Action | Responsibility |
|------|--------|----------------|
| `Views/DataQueryView.cs` | Modify | Comment out export button |
| `Views/VariableSettingsView.cs` | Modify | Show export controls + TXT test button |
| `Views/VariableSettingsView_YF.cs` | **Create** | Hide all export controls |
| `Views/VariableSettingsView_WHYC.cs` | Modify | Defensive Hide all export controls |
| `Views/VariableSettingsView_GLB.cs` | Modify | Defensive Hide all export controls |
| `Views/VariableSettingsView_TZYX.cs` | Modify | Defensive Hide all export controls |
| `Views/WorkplaceMissionView.cs` | Modify | Override IsExcelExportEnabled/IsTxtExportEnabled |
| `Configs/SystemConfigs.cs` | Modify | Map YF → VariableSettingsView_YF |
| `Utils/DataExportService.cs` | Modify | Skip batch folder when ProductBatch is empty |

---

### Task 1: Comment out DataQueryView export button

**Files:**
- Modify: `OperationGuidance_new/Views/DataQueryView.cs:105-189`

- [ ] **Step 1: Comment out the export button and its callback**

Replace lines 105-189 with the commented version:

```csharp
            // 导出按钮（暂时屏蔽 — 2026-06-10）
            // CommonButton exportBtn = _dataGridView.AddExtraButton("导出");
            // exportBtn.Click += async (sender, eventArgs) => {
            //     string filePath = ShowSaveFileDialog();
            //     if (string.IsNullOrEmpty(filePath)) return;
            //     exportBtn.Enabled = false;
            //     var filterVO = _dataGridView.FilterParametersVO;
            //     try {
            //         await Task.Run(() => {
            //             // ... streaming export logic ...
            //         });
            //         WidgetUtils.ShowNoticePopUp("导出完成！");
            //     } catch (Exception ex) {
            //         WidgetUtils.ShowErrorPopUp($"导出失败：{ex.Message}");
            //     } finally {
            //         if (!IsDisposed) {
            //             exportBtn.Enabled = true;
            //         }
            //     }
            // };
```

> **Note:** Keep `ShowSaveFileDialog()` method intact — it may be referenced elsewhere or restored later.

- [ ] **Step 2: Build and verify compile**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: Build succeeds with no errors.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/DataQueryView.cs
git commit -m "fix(dataquery): comment out export button for all versions"
```

---

### Task 2: Show export settings UI in VariableSettingsView (STANDARD)

**Files:**
- Modify: `OperationGuidance_new/Views/VariableSettingsView.cs`

- [ ] **Step 1: Add constructor that shows all export controls**

Replace the current empty class:

```csharp
using OperationGuidance_new.Views.AbstractViews;

namespace OperationGuidance_new.Views {
    public class VariableSettingsView: AVariableSettingsView {
        public VariableSettingsView() {
            // 导出相关 — 标准版全部可见（Excel + TXT）
            StoragePanel.Show();
            EnableExcelExportToggle.Show();
            EnableTxtExportToggle.Show();
            StoragePathTextBox.Show();
            StorageFieldsButton.Show();
            ExportTestButton.Show();
            // 同时显示 TXT 导出测试按钮（基类默认隐藏）
            ExportTestButton.GetButton(1).Show();
        }
    }
}
```

- [ ] **Step 2: Build and verify compile**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: Build succeeds with no errors.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/VariableSettingsView.cs
git commit -m "feat(settings): show export controls for STANDARD version"
```

---

### Task 3: Create VariableSettingsView_YF (hide export)

**Files:**
- Create: `OperationGuidance_new/Views/VariableSettingsView_YF.cs`

- [ ] **Step 1: Create VariableSettingsView_YF.cs**

```csharp
using OperationGuidance_new.Views.AbstractViews;

namespace OperationGuidance_new.Views {
    public class VariableSettingsView_YF: AVariableSettingsView {
        public VariableSettingsView_YF() {
            // YF 不使用导出功能 — 显式隐藏，防止基类默认行为变更
            StoragePanel.Hide();
            EnableExcelExportToggle.Hide();
            EnableTxtExportToggle.Hide();
            StoragePathTextBox.Hide();
            StorageFieldsButton.Hide();
            ExportTestButton.Hide();
            StoreLooseningDataToggle.Hide();
        }
    }
}
```

- [ ] **Step 2: Build and verify compile**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: Build succeeds with no errors.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/VariableSettingsView_YF.cs
git commit -m "feat(settings): add VariableSettingsView_YF with export hidden"
```

---

### Task 4: Defensive Hide in WHYC/GLB/TZYX settings views

**Files:**
- Modify: `OperationGuidance_new/Views/VariableSettingsView_WHYC.cs`
- Modify: `OperationGuidance_new/Views/VariableSettingsView_GLB.cs`
- Modify: `OperationGuidance_new/Views/VariableSettingsView_TZYX.cs`

- [ ] **Step 1: VariableSettingsView_WHYC — add constructor with explicit Hide**

Add to the class (keep all existing code, add the constructor):

```csharp
public VariableSettingsView_WHYC() {
    // 防御性隐藏 — 确保导出功能不意外暴露
    StoragePanel.Hide();
    EnableExcelExportToggle.Hide();
    EnableTxtExportToggle.Hide();
    StoragePathTextBox.Hide();
    StorageFieldsButton.Hide();
    ExportTestButton.Hide();
    StoreLooseningDataToggle.Hide();
}
```

- [ ] **Step 2: VariableSettingsView_GLB — add constructor with explicit Hide**

Same constructor code as WHYC.

- [ ] **Step 3: VariableSettingsView_TZYX — add constructor with explicit Hide**

Same constructor code as WHYC.

- [ ] **Step 4: Build and verify compile**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: Build succeeds with no errors.

- [ ] **Step 5: Commit**

```bash
git add OperationGuidance_new/Views/VariableSettingsView_WHYC.cs OperationGuidance_new/Views/VariableSettingsView_GLB.cs OperationGuidance_new/Views/VariableSettingsView_TZYX.cs
git commit -m "fix(settings): add defensive export Hide in WHYC/GLB/TZYX settings views"
```

---

### Task 5: Override export properties in WorkplaceContentPanel (STANDARD)

**Files:**
- Modify: `OperationGuidance_new/Views/WorkplaceMissionView.cs`

- [ ] **Step 1: Add using for ExportConfig**

Add to the `using` block at the top of the file:

```csharp
using OperationGuidance_new.Utils;
```

- [ ] **Step 2: Add overrides to WorkplaceContentPanel class**

Inside the `WorkplaceContentPanel` class, add after the existing member declarations:

```csharp
// 导出开关 — 标准版从 ExportConfig 读取
protected override bool IsExcelExportEnabled => ExportConfig.Instance.ExcelExportEnabled;
protected override bool IsTxtExportEnabled => ExportConfig.Instance.TxtExportEnabled;
protected override string ExportBasePath => ExportConfig.Instance.StoragePath;
protected override List<int> ExportSortConfig => ExportConfig.Instance.SortConfig;
```

- [ ] **Step 3: Build and verify compile**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: Build succeeds with no errors.

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_new/Views/WorkplaceMissionView.cs
git commit -m "feat(workplace): override export properties for STANDARD WorkplaceContentPanel"
```

---

### Task 6: Register VariableSettingsView_YF in SystemConfigs

**Files:**
- Modify: `OperationGuidance_new/Configs/SystemConfigs.cs`

- [ ] **Step 1: Add YF mapping**

In the `_menuConfigs` list, find the "系统设置" menu entry (id: 509, line ~116). Add the YF mapping:

Change:
```csharp
new(id: 509, name: "系统设置", icon: Properties.Resources.variable_settings) {
    ViewTypes = new() {
        {AppVersion.STANDARD, typeof(VariableSettingsView)},
        {AppVersion.SCII, typeof(VariableSettingsView_SCII)},
        {AppVersion.GLB, typeof(VariableSettingsView_GLB)},
        {AppVersion.WHYC, typeof(VariableSettingsView_WHYC)},
        {AppVersion.TZYX, typeof(VariableSettingsView_TZYX)},
    },
},
```

To:
```csharp
new(id: 509, name: "系统设置", icon: Properties.Resources.variable_settings) {
    ViewTypes = new() {
        {AppVersion.STANDARD, typeof(VariableSettingsView)},
        {AppVersion.YF, typeof(VariableSettingsView_YF)},
        {AppVersion.SCII, typeof(VariableSettingsView_SCII)},
        {AppVersion.GLB, typeof(VariableSettingsView_GLB)},
        {AppVersion.WHYC, typeof(VariableSettingsView_WHYC)},
        {AppVersion.TZYX, typeof(VariableSettingsView_TZYX)},
    },
},
```

- [ ] **Step 2: Build and verify compile**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: Build succeeds with no errors.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Configs/SystemConfigs.cs
git commit -m "feat(config): map YF to VariableSettingsView_YF"
```

---

### Task 7: Skip batch folder when ProductBatch is empty

**Files:**
- Modify: `OperationGuidance_new/Utils/DataExportService.cs:30-31`

- [ ] **Step 1: Change batch folder construction**

Find line 30-31 in `DataExportService.ExportAsync`:
```csharp
string batch = string.IsNullOrEmpty(request.ProductBatch) ? "null" : request.ProductBatch;
string batchFolder = Path.Combine(request.BasePath, workstation, mission, date, batch);
```

Replace with:
```csharp
string batchFolder = string.IsNullOrEmpty(request.ProductBatch)
    ? Path.Combine(request.BasePath, workstation, mission, date)
    : Path.Combine(request.BasePath, workstation, mission, date, request.ProductBatch);
```

Remove the now-unused `batch` local variable declaration.

- [ ] **Step 2: Build and verify compile**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: Build succeeds with no errors.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Utils/DataExportService.cs
git commit -m "fix(export): skip batch folder when ProductBatch is empty"
```

---

### Task 8: Final verification

- [ ] **Step 1: Full build**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: Build succeeds with zero errors and zero warnings.

- [ ] **Step 2: Verify export config save/load round-trip**

Manual verification steps:
1. Launch app in STANDARD mode
2. Navigate to 系统设置 → toggle 导出至Excel ON, 导出至Txt ON
3. Click 保存
4. Navigate away and back → both toggles should still be ON
5. Verify `ExportConfig.Instance.ExcelExportEnabled` = true, `ExportConfig.Instance.TxtExportEnabled` = true

- [ ] **Step 3: Verify export test button**

1. In 系统设置, click 导出测试 → 导出至Excel → verify Excel file created
2. Click 导出测试 → 导出至Txt → verify TXT file created

- [ ] **Step 4: Verify mission-completion export**

1. Complete a mission (OK or NG) with export toggles ON
2. Verify both .xlsx and .txt files appear in the configured storage path
3. Complete a mission with both toggles OFF
4. Verify no files are created

- [ ] **Step 5: Verify DataQueryView has no export button**

1. Navigate to 数据查询
2. Confirm the "导出" button is NOT visible

- [ ] **Step 6: Verify YF settings hide export**

1. Launch app in YF mode
2. Navigate to 系统设置
3. Confirm no export-related controls are visible

- [ ] **Step 7: Commit**

```bash
# (no files to commit — verification only)
```
