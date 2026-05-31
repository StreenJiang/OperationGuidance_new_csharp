# Storage Panel 隐藏优化 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 非 SCII 版本隐藏整个"存储参数"区块（_storagePanel），不占高度，跳过存储路径校验和保存。

**Architecture:** 以 `_storagePanel.Visible` 为全局守卫条件——基类默认 Hide，SCII 显式 Show。Resize/ScrollBar/CheckBeforeSave/SaveStorageSettings 四个方法在入口处检查 Visible 后提前返回。

**Tech Stack:** C# WinForms, .NET

---

### Task 1: AVariableSettingsView 基类 — 5 处守卫

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/AVariableSettingsView.cs`

- [ ] **Step 1: `InitializeStoragePanel()` — 末尾加 `_storagePanel.Hide()`**

```csharp
// line 426, after `_exportTestButton.Hide();`
            _exportTestButton.Hide();
            _storagePanel.Hide();
```

- [ ] **Step 2: `SaveStorageSettings()` — 开头加可见性守卫**

```csharp
// line 431, before `string newPath = ...`
        protected void SaveStorageSettings() {
            if (!_storagePanel.Visible) return;

            string newPath = _storagePathTextBox.GetTextBox(0).Box.Text;
```

- [ ] **Step 3: `CheckBeforeSave()` — 开头加可见性守卫**

```csharp
// line 995, before `string newPath = ...`
        protected virtual string? CheckBeforeSave() {
            if (!_storagePanel.Visible) return null;

            string newPath = _storagePathTextBox.GetTextBox(0).Box.Text;
```

- [ ] **Step 4: `ResizeStoragePanel()` — 开头加可见性守卫**

```csharp
// line 1026, before `// Resize title`
        protected virtual void ResizeStoragePanel() {
            if (!_storagePanel.Visible) return;

            // Resize title
            _storageTitlePanel.Size = new(Width, _titleHeight);
```

- [ ] **Step 5: `CheckNeedsScrollBar()` — 条件包含 _storagePanel 高度**

```csharp
// lines 1077-1083, replace `NewHeight += _storagePanel.Height + ...`
        public override bool CheckNeedsScrollBar(int parentNewHeight) {
            NewHeight = _buttonsOuterPanel.Height + _buttonsOuterPanel.Margin.Size.Height;
            NewHeight += _systemSettingsPanel.Height + _systemSettingsPanel.Margin.Size.Height;
            if (_storagePanel.Visible) {
                NewHeight += _storagePanel.Height + _storagePanel.Margin.Size.Height;
            }
            NewHeight += _workPanel.Height + _workPanel.Margin.Size.Height;
            return NewHeight > parentNewHeight;
        }
```

- [ ] **Step 6: Build 验证编译通过**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

---

### Task 2: SCII 构造函数 — 显式 Show _storagePanel

**Files:**
- Modify: `OperationGuidance_new/Views/VariableSettingsView_SCII.cs`

- [ ] **Step 1: 构造函数中加 `StoragePanel.Show()`**

```csharp
// line 20, before the `EnableExcelExportToggle.Show()` block
            // 导出相关 — SCII 可见
            StoragePanel.Show();
            EnableExcelExportToggle.Show();
```

- [ ] **Step 2: Build 验证编译通过**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

---

## 改动文件总览

| 文件 | 改动数 | 方法 |
|------|--------|------|
| `AVariableSettingsView.cs` | 5 处 | `InitializeStoragePanel`, `ResizeStoragePanel`, `CheckNeedsScrollBar`, `SaveStorageSettings`, `CheckBeforeSave` |
| `VariableSettingsView_SCII.cs` | 1 处 | 构造函数 |

**不受影响的子类:** `VariableSettingsView`, `VariableSettingsView_GLB`, `VariableSettingsView_WHYC`, `VariableSettingsView_TZYX`

---

## 手动验证

启动非 SCII 版本（如默认版），打开变量设置页：
- [ ] "存储参数"标题不显示
- [ ] 存储参数区域不占空白高度
- [ ] 下方"操作配置"区块紧接"系统配置"区块
- [ ] 点击"保存"不报存储路径错误，正常保存其他配置

启动 SCII 版本：
- [ ] "存储参数"区块正常显示
- [ ] 所有子组件正常显示和工作
