# Standard Version Export Enable — Design Spec

**Date:** 2026-06-10
**Status:** approved

## Motivation

当前任务结束自动导出仅对 SCII 开放。标准版（STANDARD/WHYC/GLB/TZYX）无导出功能，YF 也无。
需要将导出功能开放给标准版，同时暂时屏蔽所有版本的 DataQueryView 手动导出按钮。

## 版本差异（变更后）

| 功能 | STANDARD | WHYC | GLB | TZYX | YF | SCII |
|------|:---:|:---:|:---:|:---:|:---:|:---:|
| Settings 导出配置 | ✅ Excel+TXT | ❌ 隐藏 | ❌ 隐藏 | ❌ 隐藏 | ❌ 隐藏 | ✅ 仅Excel |
| 任务结束自动导出 | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ |
| DataQueryView 导出按钮 | ❌ 屏蔽 | ❌ 屏蔽 | ❌ 屏蔽 | ❌ 屏蔽 | ❌ 屏蔽 | ❌ 无按钮 |

## Design

### 1. DataQueryView — 屏蔽导出按钮

**文件:** `Views/DataQueryView.cs`

移除或注释第 105-178 行的 `AddExtraButton("导出")` 及完整 Click 回调。
`DataQueryView_SCII` 无此按钮，无需改动。

### 2. Settings UI — 标准版开放导出配置

#### VariableSettingsView（STANDARD）

**文件:** `Views/VariableSettingsView.cs`

构造函数新增：
```csharp
public VariableSettingsView() {
    // 导出相关 — 标准版全部可见
    StoragePanel.Show();
    EnableExcelExportToggle.Show();
    EnableTxtExportToggle.Show();    // ← 区别于 SCII（SCII 隐藏 TXT）
    StoragePathTextBox.Show();
    StorageFieldsButton.Show();
    ExportTestButton.Show();
}
```

#### VariableSettingsView_YF（新建）

**文件:** `Views/VariableSettingsView_YF.cs`（新建）

```csharp
public class VariableSettingsView_YF: AVariableSettingsView {
    public VariableSettingsView_YF() {
        // YF 不使用导出功能 — 显式隐藏
        StoragePanel.Hide();
        EnableExcelExportToggle.Hide();
        EnableTxtExportToggle.Hide();
        StoragePathTextBox.Hide();
        StorageFieldsButton.Hide();
        ExportTestButton.Hide();
        StoreLooseningDataToggle.Hide();
    }
}
```

#### VariableSettingsView_WHYC / _GLB / _TZYX（防御性）

**文件:** `Views/VariableSettingsView_WHYC.cs`, `_GLB.cs`, `_TZYX.cs`

各构造函数新增显式 Hide() 调用（与 YF 相同），确保即使基类默认行为变更也不受影响。

#### SystemConfigs

**文件:** `Configs/SystemConfigs.cs`

第 119 行附近，YF 映射新增：
```csharp
{AppVersion.YF, typeof(VariableSettingsView_YF)},
```

### 3. 运行时自动导出

#### WorkplaceContentPanel（STANDARD）

**文件:** `Views/WorkplaceMissionView.cs` — `WorkplaceContentPanel` 类

新增 override：
```csharp
protected override bool IsExcelExportEnabled => ExportConfig.Instance.ExcelExportEnabled;
protected override bool IsTxtExportEnabled => ExportConfig.Instance.TxtExportEnabled;
protected override string ExportBasePath => ExportConfig.Instance.StoragePath;
protected override List<int> ExportSortConfig => ExportConfig.Instance.SortConfig;
```

#### 继承链影响

```
AWorkplaceContentPanel (基类: IsExcelExportEnabled=false, IsTxtExportEnabled=false)
├── WorkplaceContentPanel (STANDARD) ← 新增 override → 读 ExportConfig
│   ├── WorkplaceContentPanel_WHYC ← 继承 → 自动获得
│   ├── WorkplaceContentPanel_GLB  ← 继承 → 自动获得
│   └── WorkplaceContentPanel_TZYX ← 继承 → 自动获得
├── WorkplaceContentPanel_YF     ← 直接继承基类 → 不受影响
└── WorkplaceContentPanel_SCII   ← 已有 override → 不受影响
```

### 4. 文件夹结构 — 无批次号时跳过 batch 层

**文件:** `Utils/DataExportService.cs`

标准版没有批次号（`ProductBatch` 为空），当前路径 `${BasePath}/${ws}/${mission}/${date}/null/` 产生无意义的 `null/` 文件夹。

**修改** `ExportAsync` 第 31 行：`ProductBatch` 为空时跳过 batch 层。

```csharp
string batchFolder = string.IsNullOrEmpty(request.ProductBatch)
    ? Path.Combine(request.BasePath, workstation, mission, date)
    : Path.Combine(request.BasePath, workstation, mission, date, request.ProductBatch);
```

SCII 有批次号，路径不变：`${BasePath}/${ws}/${mission}/${date}/${batch}/`

标准版无批次号，路径变为：`${BasePath}/${ws}/${mission}/${date}/`

### 5. 联动规则（已内置）

`AVariableSettingsView.UpdateExportControlsEnabled()` 已处理：
- Excel 或 TXT 任意一个 Checked → path/fields/test 控件 Enabled = true
- 两者都 Unchecked → path/fields/test 控件 Enabled = false

无需额外改动。

### 5. 文件变更清单

| 文件 | 动作 |
|------|------|
| `Views/DataQueryView.cs` | 移除/注释导出按钮 |
| `Views/VariableSettingsView.cs` | 构造函数 Show 导出控件 |
| `Views/VariableSettingsView_YF.cs` | **新建** — Hide 导出控件 |
| `Views/VariableSettingsView_WHYC.cs` | 构造函数 Hide 导出控件（防御性） |
| `Views/VariableSettingsView_GLB.cs` | 同上 |
| `Views/VariableSettingsView_TZYX.cs` | 同上 |
| `Views/WorkplaceMissionView.cs` | `WorkplaceContentPanel` 新增 override |
| `Configs/SystemConfigs.cs` | YF 映射 VariableSettingsView_YF |
| `Utils/DataExportService.cs` | 无批次号时跳过 batch 文件夹层 |

### 7. 不变的部分

- `AVariableSettingsView` 基类默认隐藏行为
- `VariableSettingsView_SCII` 已有 Show/Hide 逻辑
- `DataExportService` 双格式并行导出逻辑
- `ExportConfig` 读写 INI 逻辑
- `OnMissionCompleted` 模板方法
- `DataQueryView_SCII` 不受影响
