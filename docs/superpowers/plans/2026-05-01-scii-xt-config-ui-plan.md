# SCII_XT 配置 UI 化实施计划

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 SciiXtPrinterConfig 和 SciiXtConfig 中的配置项添加到 VariableSettingsView_SCII_XT 的 UI 中

**Architecture:** 在现有的打印机配置面板中添加 4 个新 TextBox 控件（part_code, vender_code, soft_version, hard_version），并新建 MES 交互配置子标题面板，包含 7 个 TextBox 控件

**Tech Stack:** C# WinForms, CustomLibrary 控件

---

## 受影响文件

| 文件 | 操作 |
|------|------|
| `OperationGuidance_new/OperationGuidance_new/Views/VariableSettingsView_SCII_XT.cs` | 修改 |

## 依赖项

- `SciiXtPrinterConfig` - namespace: `OperationGuidance_new.Utils.IIPSC`
- `SciiXtConfig` - namespace: `OperationGuidance_new.Configs`
- `CustomTextBoxGroup` - CustomLibrary 控件
- `TitlePanel` - CustomLibrary 控件
- `CustomContentPanel` - CustomLibrary 控件
- `ConfigUtils` - 配置工具类
- `ColorConfigs` - 颜色配置类

---

## Chunk 1: 打印机配置 UI 扩展

**Files:**
- Modify: `OperationGuidance_new/OperationGuidance_new/Views/VariableSettingsView_SCII_XT.cs`

### Task 1: 添加新字段声明

- [ ] **Step 1: 添加 8 个新字段声明**

在类 `VariableSettingsView_SCII_XT` 中添加以下私有字段：

```csharp
// Printer info fields
private CustomTextBoxGroup _partCodeBox;
private string _partCodeOriginal;
private CustomTextBoxGroup _venderCodeBox;
private string _venderCodeOriginal;
private CustomTextBoxGroup _softVersionBox;
private string _softVersionOriginal;
private CustomTextBoxGroup _hardVersionBox;
private string _hardVersionOriginal;
```

位置：在 `_secondBarcodeLength` 字段声明后（约第28行）

### Task 2: 扩展 InitializePrinterSettingsPanel

- [ ] **Step 1: 添加 4 个 TextBox 控件初始化代码**

在 `_secondBarcodeLength` 初始化后添加：

```csharp
_partCodeBox = new("零部件编码") {
    Parent = _printerSettingsContentPanel,
    Ratio = 6.95,
};

_venderCodeBox = new("供应商编码") {
    Parent = _printerSettingsContentPanel,
    Ratio = 6.95,
};

_softVersionBox = new("软件版本") {
    Parent = _printerSettingsContentPanel,
    Ratio = 6.95,
};

_hardVersionBox = new("硬件版本") {
    Parent = _printerSettingsContentPanel,
    Ratio = 6.95,
};
```

位置：在 `_secondBarcodeLength` 初始化后（约第91行）

### Task 3: 扩展 ResizeMissionSettings - 计算高度

- [ ] **Step 1: 修改打印机配置面板内容高度计算**

修改 `ResizeMissionSettings()` 方法中第3行的内容高度计算：

原代码：
```csharp
int contentHeight = BoxNBtnHeight * 3 + ContentVPadding * 2 + boxVMargin * 2;
```

修改为：
```csharp
int contentHeight = BoxNBtnHeight * 5 + ContentVPadding * 2 + boxVMargin * 4;
```

### Task 4: 扩展 ResizeMissionSettings - 布局代码

- [ ] **Step 1: 添加新控件的布局代码**

在 `_secondBarcodeLength` 布局代码后添加（第 4、5 行）：

```csharp
// Resize box - fourth row
_partCodeBox.Size = new(boxWidth, BoxNBtnHeight);
_partCodeBox.Margin = new(0, boxVMargin, ContentHGap / 2, 0);
_venderCodeBox.Size = new(boxWidth, BoxNBtnHeight);
_venderCodeBox.Margin = new(0, boxVMargin, 0, 0);
// Resize box - fifth row
_softVersionBox.Size = new(boxWidth, BoxNBtnHeight);
_softVersionBox.Margin = new(0, boxVMargin, ContentHGap / 2, 0);
_hardVersionBox.Size = new(boxWidth, BoxNBtnHeight);
_hardVersionBox.Margin = new(0, boxVMargin, 0, 0);
```

同时修改 `_printerSettingsContentPanel.Size` 的高度计算：
```csharp
_printerSettingsContentPanel.Size = new(Width, BoxNBtnHeight * 5 + ContentVPadding * 2 + boxVMargin * 4);
```

### Task 5: 扩展 ResizeMissionSettings - WorkPanel 尺寸

- [ ] **Step 1: 更新 WorkPanel 尺寸计算**

在 `ResizeMissionSettings()` 方法末尾修改 WorkPanel 尺寸：

原代码：
```csharp
WorkPanel.Size = new(Width, WorkTitlePanel.Height + WorkContentPanel.Height + _printerSettingsPanel.Height + ContentVPadding);
```

修改为（移除 ContentVPadding，改为动态计算）：
```csharp
// WorkPanel height will be updated when MES panel is added
_workPanelActualHeight = WorkTitlePanel.Height + WorkContentPanel.Height + _printerSettingsPanel.Height + ContentVPadding;
WorkPanel.Size = new(Width, _workPanelActualHeight);
```

注：如果不想修改原有的 WorkPanel 计算，可以在 Task 16 中一次性更新。

### Task 6: 扩展 SaveMissionSettings

- [ ] **Step 1: 添加保存新配置项代码**

在 `SaveMissionSettings()` 方法中，`SaveMissionSettings()` 末尾添加：

```csharp
// Save printer info
printerConfig.part_code = _partCodeBox.GetTextBox(0).Box.Text;
printerConfig.vender_code = _venderCodeBox.GetTextBox(0).Box.Text;
printerConfig.soft_version = _softVersionBox.GetTextBox(0).Box.Text;
printerConfig.hard_version = _hardVersionBox.GetTextBox(0).Box.Text;

_partCodeOriginal = printerConfig.part_code;
_venderCodeOriginal = printerConfig.vender_code;
_softVersionOriginal = printerConfig.soft_version;
_hardVersionOriginal = printerConfig.hard_version;
```

位置：在 `_secondBarcodeLengthOriginal` 赋值后（约第121行）

### Task 7: 扩展 LoadSettings

- [ ] **Step 1: 添加加载新配置项代码**

在 `LoadSettings()` 方法中，`BeginInvoke()` 回调内添加：

```csharp
// Load printer info
_partCodeBox.GetTextBox(0).Box.Text = printerConfig.part_code;
_venderCodeBox.GetTextBox(0).Box.Text = printerConfig.vender_code;
_softVersionBox.GetTextBox(0).Box.Text = printerConfig.soft_version;
_hardVersionBox.GetTextBox(0).Box.Text = printerConfig.hard_version;

// Initialize Original values
_partCodeOriginal = printerConfig.part_code;
_venderCodeOriginal = printerConfig.vender_code;
_softVersionOriginal = printerConfig.soft_version;
_hardVersionOriginal = printerConfig.hard_version;
```

位置：在 `_secondBarcodeLengthOriginal` 赋值后（约第222行）

### Task 8: 扩展 ResetAllToDefault

- [ ] **Step 1: 添加重置到默认值代码**

在 `ResetAllToDefault()` 方法中，`BeginInvoke()` 回调内添加：

```csharp
// Reset printer info to default
var defaultPrinterConfig = ConfigUtils.GetDefault<SciiXtPrinterConfig>();
_partCodeBox.GetTextBox(0).Box.Text = defaultPrinterConfig.part_code;
_venderCodeBox.GetTextBox(0).Box.Text = defaultPrinterConfig.vender_code;
_softVersionBox.GetTextBox(0).Box.Text = defaultPrinterConfig.soft_version;
_hardVersionBox.GetTextBox(0).Box.Text = defaultPrinterConfig.hard_version;
```

### Task 9: 扩展 CheckBeforeSave

- [ ] **Step 1: 验证规则保持不变**

在 `CheckBeforeSave()` 方法中，验证逻辑保持不变（所有字段可留空），无需额外验证。

### Task 10: 重写 CheckSavedFunc_detail

- [ ] **Step 1: 添加方法重写和未保存变化检测**

在 `VariableSettingsView_SCII_XT` 类中添加方法重写：

```csharp
protected override bool CheckSavedFunc_detail() {
    if (base.CheckSavedFunc_detail()) return true;
    return CheckSvedFuncSeparately(_partCodeBox.GetTextBox(0).Box.Text != _partCodeOriginal, "零部件编码")
        || CheckSvedFuncSeparately(_venderCodeBox.GetTextBox(0).Box.Text != _venderCodeOriginal, "供应商编码")
        || CheckSvedFuncSeparately(_softVersionBox.GetTextBox(0).Box.Text != _softVersionOriginal, "软件版本")
        || CheckSvedFuncSeparately(_hardVersionBox.GetTextBox(0).Box.Text != _hardVersionOriginal, "硬件版本")
        || false;
}
```

位置：在类中靠近其他 Override 方法的位置

---

## Chunk 2: MES 交互配置新建

**Files:**
- Modify: `OperationGuidance_new/OperationGuidance_new/Views/VariableSettingsView_SCII_XT.cs`

### Task 11: 添加 MES 配置面板字段声明

- [ ] **Step 1: 添加 MES 面板和 TextBox 字段声明**

在类中添加以下私有字段：

```csharp
// MES settings panel
private CustomContentPanel _mesSettingsPanel;
private TitlePanel _mesSettingsTitlePanel;
private CustomContentPanel _mesSettingsContentPanel;

// MES settings fields
private CustomTextBoxGroup _httpHostBox;
private string _httpHostOriginal;
private CustomTextBoxGroup _procedureCodeBox;
private string _procedureCodeOriginal;
private CustomTextBoxGroup _equipmentCodeBox;
private string _equipmentCodeOriginal;
private CustomTextBoxGroup _batchNoBox;
private string _batchNoOriginal;
private CustomTextBoxGroup _recipeCodeBox;
private string _recipeCodeOriginal;
private CustomTextBoxGroup _plcIsReadyAddrBox;
private string _plcIsReadyAddrOriginal;
private CustomTextBoxGroup _plcRegisterAddrBox;
private string _plcRegisterAddrOriginal;
```

位置：在 `_hardVersionBox` 字段声明后

### Task 12: 创建 InitializeMesSettingsPanel

- [ ] **Step 1: 创建 MES 设置面板初始化方法**

添加新方法 `InitializeMesSettingsPanel()`：

```csharp
protected virtual void InitializeMesSettingsPanel() {
    _mesSettingsPanel = new() {
        Parent = WorkPanel,
        FlowDirection = FlowDirection.TopDown,
    };
    _mesSettingsTitlePanel = new("MES交互配置") {
        Parent = _mesSettingsPanel,
        UnderlineColor = ColorConfigs.COLOR_TITLE_UNDERLINE,
    };
    _mesSettingsContentPanel = new() {
        Parent = _mesSettingsPanel,
    };

    _httpHostBox = new("HTTP地址") {
        Parent = _mesSettingsContentPanel,
        Ratio = 6.95,
    };

    _procedureCodeBox = new("工序编码") {
        Parent = _mesSettingsContentPanel,
        Ratio = 6.95,
    };

    _equipmentCodeBox = new("设备编码") {
        Parent = _mesSettingsContentPanel,
        Ratio = 6.95,
    };

    _batchNoBox = new("批次号") {
        Parent = _mesSettingsContentPanel,
        Ratio = 6.95,
    };

    _recipeCodeBox = new("配方编码") {
        Parent = _mesSettingsContentPanel,
        Ratio = 6.95,
    };

    _plcIsReadyAddrBox = new("PLC就绪地址") {
        Parent = _mesSettingsContentPanel,
        Ratio = 6.95,
    };

    _plcRegisterAddrBox = new("PLC寄存器地址") {
        Parent = _mesSettingsContentPanel,
        Ratio = 6.95,
    };
}
```

在 `Override InitializeMissionSettings()` 方法中 `InitializePrinterSettingsPanel()` 后添加调用 `InitializeMesSettingsPanel()`

### Task 13: 创建 ResizeMesSettings

- [ ] **Step 1: 创建 MES 配置布局方法**

添加新方法 `ResizeMesSettings()`：

```csharp
protected virtual void ResizeMesSettings() {
    // Position after printer settings panel
    _mesSettingsPanel.Location = new(0, _printerSettingsPanel.Location.Y + _printerSettingsPanel.Height + ContentVPadding);
    _mesSettingsPanel.Size = new(Width, 0);

    // Title panel
    _mesSettingsTitlePanel.Size = new(Width, TitleHeight);

    // Box layout
    int boxWidth = (Width - ContentHPadding * 3) / 2;
    int boxVMargin = BoxNBtnHeight / 2;

    // Row 1 - HTTP address (full width)
    _httpHostBox.Size = new(Width - ContentHPadding * 2, BoxNBtnHeight);
    _httpHostBox.Margin = new(ContentHPadding, boxVMargin, ContentHPadding, 0);

    // Row 2 - procedure code + equipment code
    _procedureCodeBox.Size = new(boxWidth, BoxNBtnHeight);
    _procedureCodeBox.Margin = new(ContentHPadding, boxVMargin, ContentHGap / 2, 0);
    _equipmentCodeBox.Size = new(boxWidth, BoxNBtnHeight);
    _equipmentCodeBox.Margin = new(0, boxVMargin, ContentHPadding, 0);

    // Row 3 - batch no + recipe code
    _batchNoBox.Size = new(boxWidth, BoxNBtnHeight);
    _batchNoBox.Margin = new(ContentHPadding, boxVMargin, ContentHGap / 2, 0);
    _recipeCodeBox.Size = new(boxWidth, BoxNBtnHeight);
    _recipeCodeBox.Margin = new(0, boxVMargin, ContentHPadding, 0);

    // Row 4 - PLC addresses
    _plcIsReadyAddrBox.Size = new(boxWidth, BoxNBtnHeight);
    _plcIsReadyAddrBox.Margin = new(ContentHPadding, boxVMargin, ContentHGap / 2, 0);
    _plcRegisterAddrBox.Size = new(boxWidth, BoxNBtnHeight);
    _plcRegisterAddrBox.Margin = new(0, boxVMargin, ContentHPadding, 0);

    // Content panel height (4 rows)
    _mesSettingsContentPanel.Size = new(Width, BoxNBtnHeight * 4 + ContentVPadding * 2 + boxVMargin * 3);
    _mesSettingsContentPanel.Padding = new(ContentHPadding, ContentVPadding, ContentHPadding, ContentVPadding);

    // Outer panel
    _mesSettingsPanel.Size = new(Width, _mesSettingsTitlePanel.Height + _mesSettingsContentPanel.Height);
}
```

在 `ResizeMissionSettings()` 末尾添加对此方法的调用

同时在 `ResizeChildren()` 方法中添加 `ResizeMesSettings()` 调用

### Task 14: 创建 SaveMesSettings

- [ ] **Step 1: 创建 MES 配置保存方法**

添加���方法 `SaveMesSettings()`：

```csharp
protected virtual void SaveMesSettings() {
    var mesConfig = ConfigUtils.LoadConfig<SciiXtConfig>();
    mesConfig.http_host = _httpHostBox.GetTextBox(0).Box.Text;
    mesConfig.procedure_code = _procedureCodeBox.GetTextBox(0).Box.Text;
    mesConfig.equipment_code = _equipmentCodeBox.GetTextBox(0).Box.Text;
    mesConfig.batch_no = _batchNoBox.GetTextBox(0).Box.Text;
    mesConfig.recipe_code = _recipeCodeBox.GetTextBox(0).Box.Text;
    mesConfig.plc_is_ready_addr = _plcIsReadyAddrBox.GetTextBox(0).Box.Text;
    mesConfig.plc_register_addr = _plcRegisterAddrBox.GetTextBox(0).Box.Text;
    ConfigUtils.SaveConfig(mesConfig);

    // Update original values
    _httpHostOriginal = mesConfig.http_host;
    _procedureCodeOriginal = mesConfig.procedure_code;
    _equipmentCodeOriginal = mesConfig.equipment_code;
    _batchNoOriginal = mesConfig.batch_no;
    _recipeCodeOriginal = mesConfig.recipe_code;
    _plcIsReadyAddrOriginal = mesConfig.plc_is_ready_addr;
    _plcRegisterAddrOriginal = mesConfig.plc_register_addr;
}
```

在 `Override SaveMissionSettings()` 方法末尾添加 `SaveMesSettings()` 调用

### Task 15: 扩展 LoadSettings 加载 MES 配置

- [ ] **Step 1: 添加 MES 配置加载代码**

在 `LoadSettings()` 方法中，加载打印机配置后添加：

```csharp
// Load MES config
var mesConfig = ConfigUtils.LoadConfig<SciiXtConfig>();
_httpHostBox.GetTextBox(0).Box.Text = mesConfig.http_host;
_procedureCodeBox.GetTextBox(0).Box.Text = mesConfig.procedure_code;
_equipmentCodeBox.GetTextBox(0).Box.Text = mesConfig.equipment_code;
_batchNoBox.GetTextBox(0).Box.Text = mesConfig.batch_no;
_recipeCodeBox.GetTextBox(0).Box.Text = mesConfig.recipe_code;
_plcIsReadyAddrBox.GetTextBox(0).Box.Text = mesConfig.plc_is_ready_addr;
_plcRegisterAddrBox.GetTextBox(0).Box.Text = mesConfig.plc_register_addr;

// Initialize MES original values
_httpHostOriginal = mesConfig.http_host;
_procedureCodeOriginal = mesConfig.procedure_code;
_equipmentCodeOriginal = mesConfig.equipment_code;
_batchNoOriginal = mesConfig.batch_no;
_recipeCodeOriginal = mesConfig.recipe_code;
_plcIsReadyAddrOriginal = mesConfig.plc_is_ready_addr;
_plcRegisterAddrOriginal = mesConfig.plc_register_addr;
```

### Task 16: 扩展 ResetAllToDefault 重置 MES 配置

- [ ] **Step 1: 添加 MES 默认值重置代码**

在 `ResetAllToDefault()` 方法中，添加：

```csharp
// Reset MES config to default
var defaultMesConfig = ConfigUtils.GetDefault<SciiXtConfig>();
_httpHostBox.GetTextBox(0).Box.Text = defaultMesConfig.http_host;
_procedureCodeBox.GetTextBox(0).Box.Text = defaultMesConfig.procedure_code;
_equipmentCodeBox.GetTextBox(0).Box.Text = defaultMesConfig.equipment_code;
_batchNoBox.GetTextBox(0).Box.Text = defaultMesConfig.batch_no;
_recipeCodeBox.GetTextBox(0).Box.Text = defaultMesConfig.recipe_code;
_plcIsReadyAddrBox.GetTextBox(0).Box.Text = defaultMesConfig.plc_is_ready_addr;
_plcRegisterAddrBox.GetTextBox(0).Box.Text = defaultMesConfig.plc_register_addr;
```

### Task 17: 扩展 CheckBeforeSave MES 验证

- [ ] **Step 1: 添加 MES 配置验证**

在 `CheckBeforeSave()` 方法末尾添加 MES 验证（当前所有字段可留空，无需验证）

### Task 18: 扩展 CheckSavedFunc_detail MES 检测

- [ ] **Step 1: 添加 MES 未保存检测**

在 `CheckSavedFunc_detail()` 方法中添加：

```csharp
|| CheckSvedFuncSeparately(_httpHostBox.GetTextBox(0).Box.Text != _httpHostOriginal, "HTTP地址")
|| CheckSvedFuncSeparately(_procedureCodeBox.GetTextBox(0).Box.Text != _procedureCodeOriginal, "工序编码")
|| CheckSvedFuncSeparately(_equipmentCodeBox.GetTextBox(0).Box.Text != _equipmentCodeOriginal, "设备编码")
|| CheckSvedFuncSeparately(_batchNoBox.GetTextBox(0).Box.Text != _batchNoOriginal, "批次号")
|| CheckSvedFuncSeparately(_recipeCodeBox.GetTextBox(0).Box.Text != _recipeCodeOriginal, "配方编码")
|| CheckSvedFuncSeparately(_plcIsReadyAddrBox.GetTextBox(0).Box.Text != _plcIsReadyAddrOriginal, "PLC就绪地址")
|| CheckSvedFuncSeparately(_plcRegisterAddrBox.GetTextBox(0).Box.Text != _plcRegisterAddrOriginal, "PLC寄存器地址")
```

### Task 19: 更新 WorkPanel 最终高度

- [ ] **Step 1: 更新 WorkPanel 尺寸包含 MES 配置**

在 `ResizeMissionSettings()` 或 `ResizeMesSettings()` 末尾，确保 WorkPanel 包含 MES 面板：

```csharp
// Update WorkPanel to include all panels
int totalHeight = WorkTitlePanel.Height + WorkContentPanel.Height
    + _printerSettingsPanel.Height + _mesSettingsPanel.Height + ContentVPadding * 2;
WorkPanel.Size = new(Width, totalHeight);
```

---

## 实施顺序

1. **Chunk 1** (Task 1-10): 打印机配置 UI 扩展
2. **Chunk 2** (Task 11-19): MES 交互配置新建

建议每个 Task 完成后进行测试编译验证。