# Storage Panel 隐藏优化

**日期:** 2026-05-31  
**状态:** 设计中

## 背景

`AVariableSettingsView` 是变量设置页面的抽象基类，包含三个配置区块：系统配置、存储参数、操作配置。

基类 `InitializeStoragePanel()` 将所有存储参数子组件（导出开关、存储路径、字段配置等）默认 `Hide()`，仅 SCII 版本在构造函数中显式 `Show()`。但副标题 `_storageTitlePanel` 和外部面板 `_storagePanel` 未被隐藏，导致非 SCII 版本出现空白标题和占位高度。

## 目标

- 非 SCII 版本：整个"存储参数"区块（标题 + 所有子组件 + 内容面板）不可见，不占高度
- SCII 版本：行为不变，区块正常显示

## 设计

以 `_storagePanel` 为可见性基准——基类默认隐藏，SCII 显式回显。

### 改动

| # | 文件 | 方法/位置 | 改动 |
|---|------|----------|------|
| 1 | `AVariableSettingsView.cs` | `InitializeStoragePanel()` 末尾 | 加 `_storagePanel.Hide();` |
| 2 | `AVariableSettingsView.cs` | `ResizeStoragePanel()` 开头 | `if (!_storagePanel.Visible) return;` 提前返回 |
| 3 | `AVariableSettingsView.cs` | `CheckNeedsScrollBar()` | `_storagePanel.Height` 包在 `if (_storagePanel.Visible)` 内 |
| 4 | `AVariableSettingsView.cs` | `SaveStorageSettings()` 开头 | `if (!_storagePanel.Visible) return;` |
| 5 | `AVariableSettingsView.cs` | `CheckBeforeSave()` 开头 | `if (!_storagePanel.Visible) return null;` |
| 6 | `VariableSettingsView_SCII.cs` | 构造函数 | 加 `StoragePanel.Show();` |

### 改动理由

- **#1-3:** 布局层面——隐藏面板 + 跳过 resize + 跳过滚动条计算，消除空白占位
- **#4-5:** 逻辑层面——`SaveBtnMouseUp` 所有版本都会调用 `CheckBeforeSave` 和 `SaveStorageSettings`。如果面板不可见，存储路径可能有默认无效值导致保存被阻止，且保存默认值会覆写现有有效配置。方法级守卫让所有调用者（基类 `SaveBtnMouseUp`、TZYX override、GLB override）自动受益
- **#6:** SCII 显式回显面板

### 不需改动

- `VariableSettingsView`（默认）、`VariableSettingsView_GLB`、`VariableSettingsView_WHYC`、`VariableSettingsView_TZYX` — 零改动
- `LoadSettings()`、`ResetAllToDefault()` — 对隐藏控件 set value 无副作用
- `CheckSavedFunc_detail()` — 存储相关字段值不会变更（控件隐藏无法操作），比较始终为 false，无害

### 逻辑流

```
基类 InitializeStoragePanel()
  ├─ 创建 _storagePanel, _storageTitlePanel, _storageContentPanel
  ├─ 所有子组件创建后 .Hide()
  └─ _storagePanel.Hide()                         ← #1 新增

ResizeStoragePanel()
  └─ if (!_storagePanel.Visible) return;           ← #2 新增

CheckNeedsScrollBar()
  └─ if (_storagePanel.Visible) NewHeight += ...   ← #3 条件化

SaveStorageSettings()
  └─ if (!_storagePanel.Visible) return;           ← #4 新增

CheckBeforeSave()
  └─ if (!_storagePanel.Visible) return null;      ← #5 新增

SCII 构造函数
  ├─ StoragePanel.Show()                           ← #6 新增
  └─ 子组件 .Show()（已有）
```

## 风险

- 无。所有改动仅影响 WinForms Visible 属性、布局计算和保存守卫，不涉及数据变更。
- GLB 的 `CheckBeforeSave()` override 调用 `base.CheckBeforeSave()`——base 返回 null 后继续执行 GLB 自有逻辑，行为正确。
