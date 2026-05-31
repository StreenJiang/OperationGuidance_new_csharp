# Data Export Redesign — Design Spec

**Date:** 2026-05-30
**Status:** approved

## Motivation

当前 `StoreDataToFilesCore` 有两个核心问题：

1. **Excel 文件锁冲突** — `ExportToExcelFile` 是 `async void`（fire-and-forget），ClosedXML 的 `SaveAs` 持有文件句柄。拧紧数据快速连续到达时，前一个 SaveAs 未完成，后一个已开始 → 文件锁冲突。
2. **配置每次调用都从 INI 文件读取 + 反射** — 每条数据写入都走一遍文件系统 I/O + 反射，性能差。

此外，SCII 客户需求：任务结束时按批次归档导出，文件名基于追溯码。

## 版本差异

| 功能 | 标准版 (WHYC/GLB/YF/TZYX) | SCII | XT (v2.1.x only) |
|------|---------------------------|------|-------------------|
| 文件导出 | ❌ 移除 | ✅ 开关控制（默认关） | 继承 SCII |
| 导出配置 UI | ❌ 隐藏全部 | ✅ 仅 excel 开关可见 | ❌ 隐藏全部 |

## Design

### 1. 数据完整性与竞态修复

两个排队点导致数据在导出时丢失：

| 排队点 | 位置 | 后果 |
|--------|------|------|
| A: `SemaphoreSlim.WaitAsync()` | `StoreTighteningData` 入口 | 后续数据在锁外排队 |
| B: 内层 `BeginInvoke` | `_tighteningDataVOs.Add` 在 `BeginInvoke` 内 | 数据在 UI 消息队列中，`TerminateMission` 结束后才执行 |

**修复：内联 Add + 排空 + 防重入**

```csharp
// StoreTighteningDataInternal — Add 移出 BeginInvoke，线程安全直接执行
_tighteningDataVOs.Add(dataFormatted);
BeginInvoke(() => RefreshTighteningDataPanel(_tighteningDataVOs.ToList()));

// OnMissionCompleted — 排空 + 防重入
private int _exportTriggered;
if (Interlocked.Exchange(ref _exportTriggered, 1) == 1) return;
// 获取 _storeTighteningDataLock（超时 5s）→ 排空 → 快照 → 导出 → Clear
```

### 2. 架构概览

```
AWorkplaceContentPanel (抽象基类)
  ├── 模板方法: OnMissionCompleted(status) ← 仅 FINISHED_OK / FINISHED_NG
  ├── 抽象属性 (默认 false): IsExcelExportEnabled, IsTxtExportEnabled
  ├── 抽象属性 (默认路径): ExportBasePath, ExportSortConfig
  └── StoreTighteningDataInternal: 移除文件导出调用

WorkplaceContentPanel_SCII : AWorkplaceContentPanel
  └── override 开关属性 → ExportConfig 缓存

DataExportService  ← 无 UI 依赖，原子 .tmp→Move 写入
ExportConfig       ← 单例，包装 Settings DTO，内存缓存
```

### 3. DataExportService

- `ExportAsync(ExportRequest)` → 构建路径 `{BasePath}/{yyyy-MM-dd}/{batch}/`
- 文件名: `{追溯码}（{追溯码}）_{yyyyMMdd_HHmmss}_{OK|NG}.xlsx`
- Excel + Txt 并行写入，各自 `.tmp → File.Move` 原子替换
- `ConcurrentDictionary<string, SemaphoreSlim(1,1)>` 按文件路径细粒度锁
- 空值占位 `"null"`

### 4. ExportConfig

单例，包装 `Settings` DTO（通过 `ConfigUtils.LoadConfig`）。属性：`ExcelExportEnabled`, `TxtExportEnabled`, `StoragePath`, `SortConfig`。`Reload()` 在设置保存时调用。任务执行期间不反复读 INI。

### 5. 配置 UI

#### 控件列表（从上到下）

| 控件 | 类型 | 标准版 | SCII |
|------|------|--------|------|
| `_enableExcelExportToggle` | ToggleButtonGroup "导出至Excel" | Hidden | **Show** |
| `_storagePathTextBox` | CustomTextBoxButtonGroup "数据存储路径" | Hidden | Show |
| `_storageFieldsButton` | CommonButtonGroup "数据存储字段" | Hidden | Show |
| `_exportTestButton` | CommonButtonGroup "导出测试" | Hidden | Show |
| `_storeLooseningDataToggle` | ToggleButtonGroup "记录反松数据" | Hidden | Hidden |
| `_enableTxtExportToggle` | ToggleButtonGroup "导出至Txt" | Hidden | Hidden |

#### 联动规则

- **开关依赖**: `_storagePathTextBox`, `_storageFieldsButton`, `_exportTestButton` 仅在 `_enableExcelExportToggle.Checked == true || _enableTxtExportToggle.Checked == true` 时 Enabled = true；否则 Enabled = false
- **Changed 事件**: `_enableExcelExportToggle` 和 `_enableTxtExportToggle` 的 `CheckedChanged` 触发依赖控件 Enabled 更新

#### 导出测试按钮

`_exportTestButton` 与 `_storageFieldsButton` 同类组件（CommonButtonGroup）。
- 两个按钮："导出至Excel"、"导出至Txt"（默认 Hidden）
- Click 时调用 `DataExportService.ExportAsync` 测试导出

### 6. 文件变更

| 文件 | 动作 |
|------|------|
| `Utils/ExportConfig.cs` | **新建** |
| `Utils/DataExportService.cs` | **新建** |
| `Configs/DTOs/Setting.cs` | 新增 `data_storage_excel_export_enabled`, `data_storage_txt_export_enabled`；移除 `data_storage_name_format`；修复 GetSortConfig bug |
| `Configs/IniFileKeys.cs` | 移除 `DataStorageNameFormat` |
| `Utils/MainUtils.cs` | 存储方法委托 ExportConfig；删除文件名格式方法 |
| `Views/AbstractViews/AWorkplaceContentPanel.cs` | 删除旧导出方法；OnMissionCompleted；内联 Add；排空 |
| `Views/AbstractViews/AVariableSettingsView.cs` | 删除文件名输入框；新增导出开关 + 导出测试按钮；联动逻辑 |
| `Views/VariableSettingsView_SCII.cs` | Show 导出开关/路径/字段/测试按钮 |
| `Views/WorkplaceMissionView_SCII.cs` | Override 导出开关属性 |
| `Extensions/ExtensionMethods.cs` | **删除** |
| `Views/DataQueryView.cs` | 内联替换 ExportToExcelFile |

### 7. 错误处理

```
ExportAsync:
├── 文件夹创建失败 → Error 日志，return
├── Excel 写入失败 → Error 日志，不影响 Txt
├── Txt 写入失败   → Error 日志，不影响 Excel
├── .tmp → Move 失败 → Error 日志，.tmp 保留可手动恢复
└── 任何异常不向上传播
```

### 8. 不变的部分

- `StoreDataToDatabaseAsync`
- `StoreTighteningData` SemaphoreSlim 排队
- `_tighteningDataVOs` UI 刷新
- `GetOperationDataFields` 反射逻辑
- INI 键: `DataStorageFieldsSort`, `DataStoragePath`, `DataStorageStoreLooseningData`
