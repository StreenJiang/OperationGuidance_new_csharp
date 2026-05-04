# SCII_XT 配置 UI 化设计方案

## 概述

将 SciiXtPrinterConfig 和 SciiXtConfig 中的配置项添加到 VariableSettingsView_SCII_XT 的 UI 中。

## 1. 打印机配置 UI 扩展

在现有的"打印机配置"子标题下添加 4 个新配置项，扩展为 5 行布局：

| 行 | 配置项 | 控件类型 | 配置文件属性 |
|----|-------|---------|-----------|
| 第1行 | 启用打印机 + 打印机名称 | Toggle + ComboBox | `enabled`, `printer_name` |
| 第2行 | 启用第二打印机 + 第二打印机名称 | Toggle + ComboBox | `enabled_second`, `second_printer_name` |
| 第3行 | 第二条码长度 | TextBox | `second_barcode_length` |
| 第4行 | 零部件编码 + 供应商编码 | 两个 TextBox | `part_code`, `vender_code` |
| 第5行 | 软件版本 + 硬件版本 | 两个 TextBox | `soft_version`, `hard_version` |

### 实现要点

- 新增字段声明：
  ```csharp
  private CustomTextBoxGroup _partCodeBox;
  private string _partCodeOriginal;
  private CustomTextBoxGroup _venderCodeBox;
  private string _venderCodeOriginal;
  private CustomTextBoxGroup _softVersionBox;
  private string _softVersionOriginal;
  private CustomTextBoxGroup _hardVersionBox;
  private string _hardVersionOriginal;
  ```
- 扩展 `InitializePrinterSettingsPanel()` - 添加新的 TextBox 控件
- 扩展 `ResizeMissionSettings()` - 行数从 `BoxNBtnHeight * 3` 增加到 `BoxNBtnHeight * 5`
- 扩展 `SaveMissionSettings()` - 保存 SciiXtPrinterConfig 属性
- 扩展 `LoadSettings()` - 使用 `Task.Run()` + `BeginInvoke()` 模式加载配置
- 扩展 `ResetAllToDefault()` - 重置到默认值
- 扩展 `CheckBeforeSave()` - 验证逻辑
- 扩展 `CheckSavedFunc_detail()` - 未保存变化检测

### 验证规则

| 配置项 | 验证规则 |
|--------|---------|
| 零部件编码 | 10位字符，可留空 |
| 供应商编码 | 6位字符，可留空 |
| 软件版本 | 可留空，默认 "V1.0.0" |
| 硬件版本 | 可留空，默认 "HW3.2" |

## 2. MES交互配置新建

创建新的"MES交互配置"子标题，位于打印机配置下方。包含 7 个配置项，共 4 行：

| 行 | 配置项 | 控件类型 | 配置文件属性 |
|----|-------|---------|-----------|
| 第1行 | HTTP地址 | TextBox（长文本） | `http_host` |
| 第2行 | 工序编码 + 设备编码 | 两个 TextBox | `procedure_code`, `equipment_code` |
| 第3行 | 批次号 + 配方编码 | 两个 TextBox | `batch_no`, `recipe_code` |
| 第4行 | PLC就绪地址 + PLC寄存器地址 | 两个 TextBox | `plc_is_ready_addr`, `plc_register_addr` |

### 实现要点

- 新增面板字段：
  ```csharp
  private CustomContentPanel _mesSettingsPanel;
  private TitlePanel _mesSettingsTitlePanel;
  private CustomContentPanel _mesSettingsContentPanel;
  ```
- 新增 TextBox 字段：
  ```csharp
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
- 新增 `InitializeMesSettingsPanel()` 方法创建面板和控件
- 新增 `SaveMesSettings()` 方法保存配置到 SciiXtConfig
- 新增 `ResizeMesSettings()` 方法处理布局
- 扩展 `ResizeChildren()` 包含 MES 面板的 resize 调用
- 扩展 `LoadSettings()` 加载 SciiXtConfig
- 扩展 `ResetAllToDefault()` 重置到默认值
- 扩展 `CheckBeforeSave()` 验证逻辑
- 扩展 `CheckSavedFunc_detail()` 未保存检测
- 在 `SaveBtnMouseUp()` 中调用 `SaveMesSettings()`

### 验证规则

| 配置项 | 验证规则 |
|--------|---------|
| HTTP地址 | 可留空，默认 "http://10.10.59.1:5400" |
| 工序编码 | 可留空 |
| 设备编码 | 可留空 |
| 批次号 | 可留空 |
| 配方编码 | 可留空 |
| PLC就绪地址 | 可留空，默认 "6000" |
| PLC寄存器地址 | 可留空，默认 "6002" |

## 3. 布局结构

### 面板层次

```
WorkPanel
├── WorkTitlePanel ("操作配置")
├── WorkContentPanel
│   ├── ErrorPromptForArmToggle
│   ├── UsbScannerEnabledToggle
│   └── _enableBatchCounter
├── _printerSettingsPanel
│   ├── _printerSettingsTitlePanel ("打印机配置")
│   └── _printerSettingsContentPanel
│       ├── _enablePrinter + _printerName (第1行)
│       ├── _enableSecondPrinter + _secondPrinterName (第2行)
│       ├── _secondBarcodeLength (第3行)
│       ├── _partCodeBox + _venderCodeBox (第4行)
│       └── _softVersionBox + _hardVersionBox (第5行)
└── _mesSettingsPanel
    ├── _mesSettingsTitlePanel ("MES交互配置")
    └── _mesSettingsContentPanel
        ├── _httpHostBox (第1行，全宽)
        ├── _procedureCodeBox + _equipmentCodeBox (第2行)
        ├── _batchNoBox + _recipeCodeBox (第3行)
        └── _plcIsReadyAddrBox + _plcRegisterAddrBox (第4行)
```

### Resize 调用链

```
ResizeChildren()
├── ResizeSystemSettingsPanel()
├── ResizeStoragePanel()
├── ResizeMissionSettings()     // 扩展包含打印机配置
└── ResizeMesSettings()      // 新增 MES 配置
```

## 4. 受影响文件

- `OperationGuidance_new/Views/VariableSettingsView_SCII_XT.cs` - 主要修改文件

## 5. 依赖项

- `SciiXtPrinterConfig.cs` - 已存在
- `SciiXtConfig.cs` - 已存在

## 6. 约束

- 不修改 text_1~text_4、location_y、sn_pos_x、sn_pos_y 等 ZPL 格式配置
- 不 UI 化 send_upper_cover 配置
- 遵循现有 K&R 代码风格
- 保持与父类 SCII 的一致性
- 使用 `CustomTextBoxGroup` 而非 `CustomTextBoxButtonGroup`（无按钮）
- 所有新控件值加载必须使用 `Task.Run()` + `BeginInvoke()` 模式