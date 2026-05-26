# SCII XT 打印机重打弹窗设计

> 为 PrinterOperationPopUpForm 的两个按钮增加功能：上盖码重打弹窗和分流器码重打弹窗。

## 架构

两个新弹窗 + WorkplaceContentPanel_SCII_XT 状态追踪 + 扫码枪路由扩展。

### 组件

| 组件 | 类型 | 说明 |
|---|---|---|
| `LidCodeReprintPopUpForm` | 新建，继承 `CustomPopUpForm` | 上盖码重打弹窗 |
| `DiverterCodeReprintPopUpForm` | 新建，继承 `CustomPopUpForm` | 分流器码重打弹窗 |
| `WorkplaceContentPanel_SCII_XT` | 修改 | 状态追踪、方法暴露、扫码路由 |
| `PrinterOperationPopUpForm` | 修改 | 按钮绑定 Click 事件 |

---

## LidCodeReprintPopUpForm

### 布局

方案 B（输入与按钮同行）：

```
┌─ 上盖码重打 ───────────────────── × ─┐  ← Title bar
│                                        │
│  ┌──────────────────────────────────┐  │
│  │     快速重打上盖码                │  │  ← 条件可见（_lidCodePrinted）
│  └──────────────────────────────────┘  │
│                                        │
│  追溯码（24位）                         │
│  ┌──────────────────────┐ ┌────────┐  │  ← 输入框 + 确定按钮同行
│  │ 001234567890120001... │ │  确定  │  │
│  └──────────────────────┘ └────────┘  │
│                                        │
├────────────────────────────────────────┤
│                               ┌ 关闭 ┐ │  ← Footer
└───────────────────────────────────────┘
```

- ContentPanel 内部使用 `TableLayoutPanel`（2列1行，或嵌套布局）
- "快速重打上盖码"按钮全宽
- 输入框 `MaxLength = 24`，Enter 键同确定按钮
- Footer：关闭按钮

### 行为

**快速重打：**
- 仅 `_lidCodePrinted == true` 时可见
- 点击 → 用 `_lastPrintedConfig` 调用 `ZplQrCodePrinter.QuickPrint()`
- 成功/失败提示，弹窗不关闭

**手动输入打印：**
- 输入 24 位追溯码 → Enter 或点击"确定"
- 校验：长度 != 24 → `ShowWarningPopUp("追溯码必须为24位")`
- 用当前 config + 输入的 traceCode 生成 ZPL（调用 `GenerateZplCommand`）
- 通过 `PrintViaZpl` 发送到第一打印机
- 成功/失败提示，弹窗不关闭

### 错误处理

| 场景 | 处理 |
|---|---|
| 追溯码长度 ≠ 24 | Warn，不打印 |
| `GenerateZplCommand` 抛异常 | Catch + Error popup |
| 第一打印机未配置名称 | 弹窗打开时禁用"确定"按钮 |
| 打印机未找到 | Warn |
| 发送指令失败 | Warn |

---

## DiverterCodeReprintPopUpForm

### 布局

```
┌─ 分流器码重打 ──────────────────── × ─┐  ← Title bar
│                                        │
│  二维码内容                             │
│  ┌──────────────────────────────────┐  │
│  │ 请扫描或输入条码...               │  │  ← 输入框
│  └──────────────────────────────────┘  │
│  按 Enter 键或扫码枪输入               │  ← 提示文字
│                                        │
├────────────────────────────────────────┤
│                               ┌ 关闭 ┐ │  ← Footer
└───────────────────────────────────────┘
```

### 行为

- 支持扫码枪输入（通过 workplace barcode handler 路由）
- 支持手动输入 + Enter 键
- Enter → 校验 `second_barcode_length`（> 0 时）→ 调用 `SendQRCodeToPrinter()`
- 打印结果提示，弹窗不关闭

### 错误处理

| 场景 | 处理 |
|---|---|
| 条码长度不匹配 | Warn + 显示实际/期望长度 |
| 第二打印机未配置 | 弹窗打开时禁用输入框 |
| 打印机未找到 / 打印失败 | 复用 `SendQRCodeToPrinter` 现有逻辑 |

---

### LidCodeReprintPopUpForm 依赖

构造函数接收：`WorkplaceContentPanel_SCII_XT workplace`、`bool lidCodePrinted`、`SciiXtPrinterConfig? lastPrintedConfig`。弹窗内部持有这些引用用于打印操作。

### DiverterCodeReprintPopUpForm 依赖

构造函数接收：`WorkplaceContentPanel_SCII_XT workplace`。通过 workplace 访问 config（`second_printer_name`、`second_barcode_length`）和 `SendQRCodeToPrinter` 方法。

---

## WorkplaceContentPanel_SCII_XT 修改

### 新增字段

```csharp
private bool _lidCodePrinted;                    // SendToPrinter 成功后置 true
private SciiXtPrinterConfig? _lastPrintedConfig;  // 上次打印的配置快照
private DiverterCodeReprintPopUpForm? _reprintBarcodeDialog; // 分流器重打弹窗引用
```

### 新增方法

```csharp
public void OpenLidCodeReprintPopUp()   // 创建并展示上盖码重打弹窗
public void OpenDiverterCodeReprintPopUp() // 创建并展示分流器码重打弹窗
```

### SendToPrinter 修改

`QuickPrint` 成功后：
- `_lidCodePrinted = true`
- 保存当前 config 快照到 `_lastPrintedConfig`

### 扫码枪路由修改

在现有 barcode handler 中增加对 `_reprintBarcodeDialog` 的处理：

```
收到扫码 →
  1. _barcodeDialog != null → ProcessSecondBarCode()
  2. _reprintBarcodeDialog != null → 填入输入框
  3. _barCodePopUpForm → ValidateBarCode()
```

---

## PrinterOperationPopUpForm 修改

- 构造函数新增参数 `WorkplaceContentPanel_SCII_XT workplace`
- "上盖码重打" Click → `workplace.OpenLidCodeReprintPopUp()`
- "分流器码重打" Click → `workplace.OpenDiverterCodeReprintPopUp()`
- 创建处（`InitializeAfterHandelCreated`）已有 `this` 即 workplace，直接传入
