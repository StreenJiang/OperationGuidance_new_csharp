# SCII XT 打印机重打弹窗实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为 PrinterOperationPopUpForm 的两个按钮增加功能——上盖码重打弹窗和分流器码重打弹窗。

**Architecture:** 两个新 CustomPopUpForm 子类 + WorkplaceContentPanel_SCII_XT 状态追踪与方法暴露 + 扫码枪路由扩展。弹窗复用现有 CustomPopUpForm 基础设施（ContentPanel、AddButton、ResizeChildren）。

**Tech Stack:** C# WinForms, CustomPopUpForm, CustomTextBoxGroup, FunctionButton, TableLayoutPanel, ZplQrCodePrinter

---

### Task 1: WorkplaceContentPanel_SCII_XT — 状态追踪字段 + SendToPrinter 修改

**Files:**
- Modify: `OperationGuidance_new/Views/WorkplaceMissionView_SCII_XT.cs:60-61,285-311`

- [ ] **Step 1: 添加状态追踪字段**

在 `_canReceiveBarcode` 字段之后添加：

```csharp
private bool _lidCodePrinted;
private SciiXtPrinterConfig? _lastPrintedConfig;
```

- [ ] **Step 2: 修改 SendToPrinter — 打印成功后设置状态标记**

将 `SendToPrinter()` 方法中 `printer.QuickPrint(config)` 的成功分支改为设置状态：

```csharp
public async Task SendToPrinter() {
    await Task.Run(() => BeginInvoke(() => {
        var config = ConfigUtils.LoadConfig<SciiXtPrinterConfig>();
        if (config.enabled == (int) YesOrNo.YES) {
            if (config.printer_name == null) {
                WidgetUtils.ShowWarningPopUp("打印机名称配置未设置，请先配置打印机。");
            } else {
                int _okSumToday = int.Parse(_okSumPerDay.GetTextBox(0).Box.Text);
                config.sn = _okSumToday + 1;

                using (ZplQrCodePrinter printer = new()) {
                    List<string> list = printer.GetAvailablePrinters();
                    if (list.Count > 0) {
                        string? printerName = list.Find(p => p == config.printer_name);
                        if (printerName == null) {
                            WidgetUtils.ShowWarningPopUp("未找到指定配置的打印机，请检查配置或打印机。");
                        } else if (!printer.QuickPrint(config)) {
                            WidgetUtils.ShowWarningPopUp("发送指令至打印机失败！请检查日志信息定位问题。");
                        } else {
                            _lidCodePrinted = true;
                            _lastPrintedConfig = config;
                        }
                    } else {
                        WidgetUtils.ShowWarningPopUp("未找到任何打印机设备！");
                    }
                }
            }
        }
    }));
}
```

- [ ] **Step 3: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 4: 提交**

```bash
git add OperationGuidance_new/Views/WorkplaceMissionView_SCII_XT.cs
git commit -m "feat: add lid code print state tracking for reprint feature"
```

---

### Task 2: LidCodeReprintPopUpForm — 新建上盖码重打弹窗

**Files:**
- Create: `OperationGuidance_new/Views/SubViews/LidCodeReprintPopUpForm.cs`

- [ ] **Step 1: 创建 LidCodeReprintPopUpForm 完整实现**

```csharp
using CustomLibrary.Configs;
using CustomLibrary.Forms;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Utils.IIPSC;

namespace OperationGuidance_new.Views.SubViews {
    public class LidCodeReprintPopUpForm: CustomPopUpForm {
        private readonly WorkplaceContentPanel_SCII_XT _workplace;
        private readonly bool _hasQuickReprint;
        private readonly SciiXtPrinterConfig? _lastPrintedConfig;
        private SciiXtPrinterConfig _config;

        private TableLayoutPanel _tablePanel;
        private FunctionButton? _btnQuickReprint;
        private CustomTextBoxGroup _traceCodeBox;
        private FunctionButton _btnConfirm;

        public int ContentWidth {
            get {
                int btnH = WidgetUtils.TextOrComboBoxHeight();
                using (var font = new Font(WidgetsConfigs.SystemFontFamily,
                                           btnH * 0.425f, FontStyle.Bold, GraphicsUnit.Pixel)) {
                    int wConfirm = TextRenderer.MeasureText("确定", font).Width;
                    // Estimate label width: "追溯码（24位）" ≈ 120px at standard DPI
                    int wInput = 300;
                    return wInput + wConfirm + btnH + btnH / 3;
                }
            }
        }

        public LidCodeReprintPopUpForm(WorkplaceContentPanel_SCII_XT workplace,
                                        bool hasQuickReprint,
                                        SciiXtPrinterConfig? lastPrintedConfig) {
            _workplace = workplace;
            _hasQuickReprint = hasQuickReprint;
            _lastPrintedConfig = lastPrintedConfig;

            BorderColor = ColorConfigs.COLOR_POP_UP_BORDER;
            Title = "上盖码重打";

            _config = ConfigUtils.LoadConfig<SciiXtPrinterConfig>();

            int rowCount = _hasQuickReprint ? 2 : 1;
            _tablePanel = new() {
                Margin = new(0),
                Padding = new(0),
                ColumnCount = 2,
                RowCount = rowCount,
                Parent = ContentPanel,
            };
            _tablePanel.ColumnStyles.Add(new(SizeType.Percent, 70F));
            _tablePanel.ColumnStyles.Add(new(SizeType.Percent, 30F));

            if (_hasQuickReprint && _lastPrintedConfig != null) {
                _btnQuickReprint = new() {
                    Label = "快速重打上盖码",
                    Parent = _tablePanel,
                };
                _tablePanel.SetColumnSpan(_btnQuickReprint, 2);
                _btnQuickReprint.Click += (s, e) => QuickReprint();
            }

            _traceCodeBox = new("追溯码（24位）") {
                Parent = _tablePanel,
            };
            _traceCodeBox.GetTextBox(0).Box.MaxLength = 24;
            _traceCodeBox.GetTextBox(0).Box.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) ReprintByInput();
            };

            _btnConfirm = new() {
                Label = "确定",
                Parent = _tablePanel,
            };
            _btnConfirm.Click += (s, e) => ReprintByInput();

            // 打印机未配置时禁用
            if (string.IsNullOrEmpty(_config.printer_name)) {
                _traceCodeBox.Enabled = false;
                _btnConfirm.Enabled = false;
            }

            FunctionButton btnClose = AddButton("关闭");
            btnClose.Click += (s, e) => Dispose();
        }

        private async void QuickReprint() {
            if (_lastPrintedConfig == null) return;
            bool ok = await Task.Run(() => {
                using ZplQrCodePrinter printer = new();
                return printer.QuickPrint(_lastPrintedConfig);
            });
            if (ok)
                WidgetUtils.ShowNoticePopUp("打印成功", 2);
            else
                WidgetUtils.ShowWarningPopUp("打印失败");
        }

        private async void ReprintByInput() {
            string traceCode = _traceCodeBox.GetTextBox(0).Box.Text;
            if (traceCode.Length != 24) {
                WidgetUtils.ShowWarningPopUp("追溯码必须为24位");
                return;
            }

            bool ok = await Task.Run(() => {
                try {
                    using ZplQrCodePrinter printer = new();
                    string zpl = printer.GenerateZplCommand(_config, traceCode);
                    return printer.PrintViaZpl(_config.printer_name, zpl);
                } catch (Exception ex) {
                    LogManager.GetLogger(GetType()).Error("上盖码重打失败", ex);
                    return false;
                }
            });
            if (ok)
                WidgetUtils.ShowNoticePopUp("打印成功", 2);
            else
                WidgetUtils.ShowWarningPopUp("打印失败");
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            base.ResizeChildren(sender, eventArgs);
            _tablePanel.Size = new(ContentPanel.Width - ContentPanel.Padding.Size.Width,
                                    ContentPanel.Height - ContentPanel.Padding.Size.Height);

            int gap = _tablePanel.Height / (_hasQuickReprint ? 8 : 12);
            int rowHeight;
            int inputAndBtnTop;

            if (_hasQuickReprint && _btnQuickReprint != null) {
                rowHeight = (_tablePanel.Height - gap) / 2;
                _btnQuickReprint.Size = new(_tablePanel.Width, rowHeight);
                inputAndBtnTop = rowHeight + gap;
            } else {
                rowHeight = _tablePanel.Height;
                inputAndBtnTop = 0;
            }

            int col2Width = _tablePanel.Width * 30 / 100;
            int col1Width = _tablePanel.Width - col2Width;
            int btnMargin = rowHeight / 6;

            _traceCodeBox.Margin = new(0, inputAndBtnTop, 0, 0);
            _traceCodeBox.Size = new(col1Width, rowHeight);

            _btnConfirm.Margin = new(btnMargin, inputAndBtnTop, 0, 0);
            _btnConfirm.Size = new(col2Width - btnMargin, rowHeight);
        }
    }
}
```

- [ ] **Step 2: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 3: 提交**

```bash
git add OperationGuidance_new/Views/SubViews/LidCodeReprintPopUpForm.cs
git commit -m "feat: add LidCodeReprintPopUpForm with quick reprint and manual input"
```

---

### Task 3: PrinterOperationPopUpForm — 传入 workplace + 绑定上盖码重打按钮

**Files:**
- Modify: `OperationGuidance_new/Views/SubViews/PrinterOperationPopUpForm.cs:10,26-65,205-222`
- Modify: `OperationGuidance_new/Views/WorkplaceMissionView_SCII_XT.cs:205-222` (click handler)

- [ ] **Step 1: 修改 PrinterOperationPopUpForm 构造函数 — 接受 workplace 参数**

修改 `PrinterOperationPopUpForm.cs`：

```csharp
using CustomLibrary.Configs;
using CustomLibrary.Forms;
using CustomLibrary.Utils;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Utils.IIPSC;
using OperationGuidance_service.Constants;

namespace OperationGuidance_new.Views.SubViews {
    public class PrinterOperationPopUpForm: CustomPopUpForm {
        private readonly WorkplaceContentPanel_SCII_XT _workplace;
        private TableLayoutPanel _tablePanel;
        private FunctionButton _btnReprintLid;
        private FunctionButton _btnReprintDiverter;

        public int ContentWidth {
            get {
                int btnH = WidgetUtils.TextOrComboBoxHeight();
                using (var font = new Font(WidgetsConfigs.SystemFontFamily,
                                           btnH * 0.425f, FontStyle.Bold, GraphicsUnit.Pixel)) {
                    int w1 = TextRenderer.MeasureText(_btnReprintLid.Label, font).Width;
                    int w2 = TextRenderer.MeasureText(_btnReprintDiverter.Label, font).Width;
                    return Math.Max(w1, w2) + btnH * 2 + 10;
                }
            }
        }

        public PrinterOperationPopUpForm(WorkplaceContentPanel_SCII_XT workplace) {
            _workplace = workplace;
            BorderColor = ColorConfigs.COLOR_POP_UP_BORDER;
            Title = "打印机操作";

            var config = ConfigUtils.LoadConfig<SciiXtPrinterConfig>();

            bool firstEnabled = config.enabled == (int) YesOrNo.YES
                && !string.IsNullOrEmpty(config.printer_name);
            bool secondEnabled = config.enabled_second == (int) YesOrNo.YES
                && !string.IsNullOrEmpty(config.second_printer_name);

            string firstLabel = firstEnabled
                ? $"上盖码重打 — {config.printer_name}"
                : "上盖码重打（未启用）";
            string secondLabel = secondEnabled
                ? $"分流器码重打 — {config.second_printer_name}"
                : "分流器码重打（未启用）";

            _tablePanel = new() {
                Margin = new(0),
                Padding = new(0),
                ColumnCount = 1,
                RowCount = 2,
                Parent = ContentPanel,
            };

            _btnReprintLid = new() {
                Label = firstLabel,
                Enabled = firstEnabled,
                Parent = _tablePanel,
            };
            _btnReprintLid.Click += (s, e) => {
                Dispose();
                _workplace.OpenLidCodeReprintPopUp();
            };

            _btnReprintDiverter = new() {
                Label = secondLabel,
                Enabled = secondEnabled,
                Parent = _tablePanel,
            };
            _btnReprintDiverter.Click += (s, e) => {
                Dispose();
                _workplace.OpenDiverterCodeReprintPopUp();
            };

            FunctionButton btnClose = AddButton("关闭");
            btnClose.Click += (s, e) => Dispose();
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            base.ResizeChildren(sender, eventArgs);
            _tablePanel.Size = new(ContentPanel.Width - ContentPanel.Padding.Size.Width,
                                    ContentPanel.Height - ContentPanel.Padding.Size.Height);
            int gap = _tablePanel.Height / 6;
            int btnHeight = (_tablePanel.Height - gap) / 2;
            _btnReprintLid.Size = new(_tablePanel.Width, btnHeight);
            _btnReprintDiverter.Size = new(_tablePanel.Width, btnHeight);
            _btnReprintDiverter.Margin = new Padding(0, gap, 0, 0);
        }
    }
}
```

- [ ] **Step 2: 在 WorkplaceContentPanel_SCII_XT 中添加 OpenLidCodeReprintPopUp 方法**

在 `WorkplaceMissionView_SCII_XT.cs` 的 `WorkplaceContentPanel_SCII_XT` 类中，`SendToPrinter()` 方法之后添加：

```csharp
public void OpenLidCodeReprintPopUp() {
    var popUpForm = new LidCodeReprintPopUpForm(this, _lidCodePrinted, _lastPrintedConfig);
    popUpForm.PretendToShowToCreateHandlesForChildren();

    int btnHeight = WidgetUtils.TextOrComboBoxHeight();
    // 有快速重打按钮时多一个按钮高度 + gap
    int gap = btnHeight / 6;
    int contentHPadding = popUpForm.ContentPanel.Padding.Horizontal;
    int contentVPadding = popUpForm.ContentPanel.Padding.Vertical;
    int contentWidth = popUpForm.ContentWidth + contentHPadding;
    int contentHeight = _lidCodePrinted
        ? btnHeight * 2 + gap + contentVPadding
        : btnHeight + contentVPadding;
    Size contentSize = new(contentWidth, contentHeight);
    popUpForm.SetContentSizeAndSelfSize(contentSize);
    popUpForm.Show();
}
```

- [ ] **Step 3: 更新 PrinterOperationPopUpForm 创建处 — 传入 workplace**

在 `InitializeAfterHandelCreated()` 中，将：
```csharp
var popUpForm = new PrinterOperationPopUpForm();
```
改为：
```csharp
var popUpForm = new PrinterOperationPopUpForm(this);
```

- [ ] **Step 4: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 5: 提交**

```bash
git add OperationGuidance_new/Views/SubViews/PrinterOperationPopUpForm.cs OperationGuidance_new/Views/WorkplaceMissionView_SCII_XT.cs
git commit -m "feat: wire lid code reprint button to LidCodeReprintPopUpForm"
```

---

### Task 4: DiverterCodeReprintPopUpForm — 新建分流器码重打弹窗

**Files:**
- Create: `OperationGuidance_new/Views/SubViews/DiverterCodeReprintPopUpForm.cs`

- [ ] **Step 1: 创建 DiverterCodeReprintPopUpForm 完整实现**

```csharp
using CustomLibrary.Configs;
using CustomLibrary.Forms;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Utils.IIPSC;
using OperationGuidance_service.Constants;
using log4net;

namespace OperationGuidance_new.Views.SubViews {
    public class DiverterCodeReprintPopUpForm: CustomPopUpForm {
        private readonly WorkplaceContentPanel_SCII_XT _workplace;
        private SciiXtPrinterConfig _config;
        private CustomTextBoxGroup _inputBox;
        private ILog _log = LogManager.GetLogger(typeof(DiverterCodeReprintPopUpForm));

        public CustomTextBoxGroup InputBox { get => _inputBox; }

        public DiverterCodeReprintPopUpForm(WorkplaceContentPanel_SCII_XT workplace) {
            _workplace = workplace;
            BorderColor = ColorConfigs.COLOR_POP_UP_BORDER;
            Title = "分流器码重打";

            _config = ConfigUtils.LoadConfig<SciiXtPrinterConfig>();

            _inputBox = new("二维码内容") {
                Parent = ContentPanel,
            };
            _inputBox.GetTextBox(0).Box.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) {
                    ProcessAndPrint();
                }
            };

            // 第二打印机未配置时禁用
            if (_config.enabled_second != (int) YesOrNo.YES
                || string.IsNullOrEmpty(_config.second_printer_name)) {
                _inputBox.Enabled = false;
            }

            FunctionButton btnClose = AddButton("关闭");
            btnClose.Click += (s, e) => Dispose();
        }

        public void FillBarcode(string barcode) {
            if (_inputBox.IsDisposed) return;
            _inputBox.GetTextBox(0).Box.Text = barcode;
            ProcessAndPrint();
        }

        private void ProcessAndPrint() {
            string barcode = _inputBox.GetTextBox(0).Box.Text;
            if (string.IsNullOrEmpty(barcode)) return;

            if (_config.enabled_second == (int) YesOrNo.YES && _config.second_barcode_length > 0) {
                if (barcode.Length != _config.second_barcode_length) {
                    WidgetUtils.ShowWarningPopUp(
                        $"条码长度不匹配！当前长度为 {barcode.Length}，要求长度为 {_config.second_barcode_length}。");
                    return;
                }
            }

            _ = _workplace.SendQRCodeToPrinter(barcode);
            WidgetUtils.ShowNoticePopUp("打印指令已发送", 2);
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            base.ResizeChildren(sender, eventArgs);
            int boxMargin = WidgetUtils.TextOrComboBoxHeight() / 5;
            int boxWidth = ContentPanel.Width - ContentPanel.Padding.Size.Width - boxMargin * 2;
            int boxHeight = WidgetUtils.TextOrComboBoxHeight();
            _inputBox.Size = new(boxWidth, boxHeight);
            _inputBox.Margin = new(boxMargin);
        }
    }
}
```

- [ ] **Step 2: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 3: 提交**

```bash
git add OperationGuidance_new/Views/SubViews/DiverterCodeReprintPopUpForm.cs
git commit -m "feat: add DiverterCodeReprintPopUpForm with barcode scan support"
```

---

### Task 5: WorkplaceContentPanel_SCII_XT — OpenDiverterCodeReprintPopUp + 扫码路由

**Files:**
- Modify: `OperationGuidance_new/Views/WorkplaceMissionView_SCII_XT.cs:60-61,483-492`

- [ ] **Step 1: 添加 _reprintBarcodeDialog 字段**

在 `_lastPrintedConfig` 字段之后添加：

```csharp
private DiverterCodeReprintPopUpForm? _reprintBarcodeDialog;
```

- [ ] **Step 2: 添加 OpenDiverterCodeReprintPopUp 方法** (续)

在 `WorkplaceContentPanel_SCII_XT` 类的 `OpenLidCodeReprintPopUp()` 方法之后添加：

```csharp
public void OpenDiverterCodeReprintPopUp() {
    var popUpForm = new DiverterCodeReprintPopUpForm(this);
    _reprintBarcodeDialog = popUpForm;
    popUpForm.FormClosed += (s, e) => _reprintBarcodeDialog = null;

    popUpForm.PretendToShowToCreateHandlesForChildren();

    int boxHeight = WidgetUtils.TextOrComboBoxHeight();
    int boxMargin = boxHeight / 5;
    Padding contentPadding = popUpForm.ContentPanel.Padding;
    int contentWidth = (int) (WidgetUtils.MainSize.Width * .45);
    int contentHeight = boxHeight + boxMargin * 2 + contentPadding.Size.Height;
    popUpForm.SetContentSizeAndSelfSize(new(contentWidth, contentHeight));
    popUpForm.Show();
}
```

- [ ] **Step 3: 修改扫码枪路由 — 增加 _reprintBarcodeDialog 处理**

在 `InitSerialPortTasks` 的 barcode handler 中，在 `_barcodeDialog` 处理之后、`_barCodePopUpForm` 处理之前插入：

将：
```csharp
                                if (_barcodeDialog != null && !_barcodeDialog.IsDisposed) {
                                    _barcodeDialog.TextBox.GetTextBox(0).Text = msg;
                                    ProcessSecondBarCode();
                                }
                                // 交给弹窗处理
                                else if (_barCodePopUpForm == null || _barCodePopUpForm.IsDisposed) {
                                    OpenBarCodePopUpForm(msg);
                                } else {
                                    _barCodePopUpForm.ValidateBarCode(msg);
                                }
```

改为：
```csharp
                                if (_barcodeDialog != null && !_barcodeDialog.IsDisposed) {
                                    _barcodeDialog.TextBox.GetTextBox(0).Text = msg;
                                    ProcessSecondBarCode();
                                }
                                // 分流器重打弹窗
                                else if (_reprintBarcodeDialog != null && !_reprintBarcodeDialog.IsDisposed) {
                                    _reprintBarcodeDialog.FillBarcode(msg);
                                }
                                // 交给弹窗处理
                                else if (_barCodePopUpForm == null || _barCodePopUpForm.IsDisposed) {
                                    OpenBarCodePopUpForm(msg);
                                } else {
                                    _barCodePopUpForm.ValidateBarCode(msg);
                                }
```

- [ ] **Step 4: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 5: 提交**

```bash
git add OperationGuidance_new/Views/WorkplaceMissionView_SCII_XT.cs
git commit -m "feat: wire diverter code reprint with barcode scan routing"
```

---

## 自审清单

- [x] **Spec coverage**: 所有 spec 需求均有对应 Task（状态追踪→T1, LidCode 弹窗→T2, 按钮绑定→T3, Diverter 弹窗→T4, 扫码路由→T5）
- [x] **Placeholder scan**: 无 TBD/TODO
- [x] **Type consistency**: `WorkplaceContentPanel_SCII_XT` 引用在 T2/T3/T4/T5 中一致
