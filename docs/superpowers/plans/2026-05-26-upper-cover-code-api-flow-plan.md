# 上盖码录入 → API 获取追溯码 → 打印 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 上盖码录入后通过 MES API 获取24位追溯码，解析校验后由第一台 ZPL 打印机打印标签。

**Architecture:** 在 `Workflow_SCII_XT` 新增 GET 请求方法；在 `ZplQrCodePrinter` 新增追溯码解析/校验/打印方法；在 `WorkplaceContentPanel_SCII_XT` 新增处理方法串联 API → 解析 → 打印；`CheckSecondBarCode` 改事件绑定。

**Tech Stack:** C# WinForms, .NET, ZPL 标签打印

---

### Task 1: `Workflow_SCII_XT` — 新增 `GetUpperCode` API 方法

**Files:**
- Modify: `OperationGuidance_new/Utils/Workflow_SCII_XT.cs`

- [ ] **Step 1: 在 `BindUppderCover` 方法后添加 `GetUpperCode` 方法**

在 `Workflow_SCII_XT.cs` 第119行（`BindUppderCover` 方法 `}` 之后，空行处）插入：

```csharp
        // 上盖码 → 追溯码
        public static async Task<string?> GetUpperCode(string productCode) {
            var api = $"/api/product/GetUpperCode/{productCode}";

            try {
                var rsp = await HttpUtils.SendGet_SCII_XT<SCII_XT_Response>(RequestPrefix + api);
                if (rsp.code == (int) SCII_XT_ResponseCode.OK && rsp.dataInfo != null) {
                    return rsp.dataInfo.ToString();
                }
                log.Warn($"GetUpperCode 失败，productCode = [{productCode}]，code = [{rsp.code}]，message = [{rsp.message}]");
                return null;
            } catch (Exception ex) {
                log.Error($"GetUpperCode 异常，productCode = [{productCode}]", ex);
                return null;
            }
        }
```

- [ ] **Step 2: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 3: 提交**

```bash
git add OperationGuidance_new/Utils/Workflow_SCII_XT.cs
git commit -m "feat(scii-xt): add GetUpperCode API method for upper cover trace code retrieval"
```

---

### Task 2: `ZplQrCodePrinter` — 新增追溯码解析与打印方法

**Files:**
- Modify: `OperationGuidance_new/Utils/IIPSC/ZplQrCodePrinter.cs`

- [ ] **Step 1: 添加 `ParseTraceCode` 私有静态方法**

在 `ZplQrCodePrinter` 类中，`MmToDots` 方法之前插入：

```csharp
        /// <summary>
        /// 解析24位追溯码，提取流水号和日期。校验失败抛 ArgumentException。
        /// 格式: 00(2) + 制造地(2) + 年份尾号(1) + 自然日(3) + 流水号(4) + 0000(4) + 零件号(8)
        /// </summary>
        private static (int serialNumber, DateTime date) ParseTraceCode(string traceCode) {
            if (string.IsNullOrEmpty(traceCode) || traceCode.Length != TRACE_CODE_LENGTH)
                throw new ArgumentException($"追溯码必须为{TRACE_CODE_LENGTH}位");

            if (traceCode.Substring(0, 2) != "00")
                throw new ArgumentException("追溯码前两位必须为\"00\"");
            if (traceCode.Substring(12, 4) != "0000")
                throw new ArgumentException("追溯码第13-16位必须为\"0000\"");

            if (!int.TryParse(traceCode.Substring(4, 1), out int yearDigit) || yearDigit < 0 || yearDigit > 9)
                throw new ArgumentException("追溯码第5位年份尾号格式不正确");

            if (!int.TryParse(traceCode.Substring(5, 3), out int dayOfYear) || dayOfYear < 1 || dayOfYear > 366)
                throw new ArgumentException("追溯码第6-8位自然日格式不正确");

            if (!int.TryParse(traceCode.Substring(8, 4), out int serialNumber) || serialNumber < 0 || serialNumber > 9999)
                throw new ArgumentException("追溯码第9-12位流水号格式不正确");

            int baseYear = DateTime.Now.Year / 10 * 10 + yearDigit;
            if (baseYear > DateTime.Now.Year)
                baseYear -= 10;

            DateTime date;
            try {
                date = new DateTime(baseYear, 1, 1).AddDays(dayOfYear - 1);
            } catch (ArgumentOutOfRangeException) {
                throw new ArgumentException($"追溯码中日期无效：年份={baseYear}，日序={dayOfYear}");
            }

            if (date > DateTime.Now)
                throw new ArgumentException($"追溯码中日期({date:yyyy/MM/dd})不能是未来日期");

            return (serialNumber, date);
        }
```

- [ ] **Step 2: 编译确认无语法错误**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 3: 添加 `GenerateZplCommand` 新重载（接收外部日期）**

在原有 `GenerateZplCommand(SciiXtPrinterConfig, string, int)` 方法之后插入：

```csharp
        private string GenerateZplCommand(SciiXtPrinterConfig sProfile, string traceCode, DateTime date, int moduleSize = 5) {
            if (string.IsNullOrEmpty(traceCode) || traceCode.Length != TRACE_CODE_LENGTH)
                throw new ArgumentException($"追溯码必须是{TRACE_CODE_LENGTH}位", nameof(traceCode));

            var zpl = new StringBuilder();
            zpl.AppendLine("^XA");

            zpl.AppendLine($"{sProfile.text_1}{sProfile.supplier_name}^FS");
            zpl.AppendLine($"{sProfile.text_2}{sProfile.project_name}^FS");
            zpl.AppendLine($"{sProfile.text_3}{date:yyyy/MM/dd}^FS");
            zpl.AppendLine($"{sProfile.text_4}{sProfile.sn.ToString().PadLeft(4, '0')}^FS");
            zpl.AppendLine($"{sProfile.text_5}{traceCode}^FS");

            int labelWidthMm = 110;
            int labelHeightMm = 50;
            zpl.AppendLine($"^LH0,0");
            zpl.AppendLine($"^PW{MmToDots(labelWidthMm, DPMM_203DPI)}");
            zpl.AppendLine($"^LL{MmToDots(labelHeightMm, DPMM_203DPI)}");

            zpl.AppendLine($"^FO{sProfile.sn_pos_x},{sProfile.sn_pos_y}^BQN,2,{moduleSize}");
            zpl.AppendLine($"^FDQA,{traceCode}^FS");
            zpl.AppendLine("^XZ");

            return zpl.ToString();
        }
```

- [ ] **Step 4: 编译确认**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 5: 添加 `PrintWithTraceCode` 公开方法**

在 `QuickPrint` 方法之后插入：

```csharp
        /// <summary>
        /// 使用 API 返回的24位追溯码打印标签。解析追溯码、校验格式、提取流水号、组装ZPL并打印。
        /// </summary>
        public bool PrintWithTraceCode(SciiXtPrinterConfig config, string traceCode) {
            try {
                var (serialNumber, date) = ParseTraceCode(traceCode);

                config.sn = serialNumber;

                string zpl = GenerateZplCommand(config, traceCode, date);
                return PrintViaZpl(config.printer_name, zpl);
            } catch (Exception ex) {
                log.Error($"PrintWithTraceCode 失败，traceCode = [{traceCode}]：{ex.Message}");
                return false;
            }
        }
```

- [ ] **Step 6: 编译确认**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 7: 提交**

```bash
git add OperationGuidance_new/Utils/IIPSC/ZplQrCodePrinter.cs
git commit -m "feat(scii-xt): add ParseTraceCode and PrintWithTraceCode for API-sourced trace code printing"
```

---

### Task 3: `WorkplaceContentPanel_SCII_XT` — 新增 `ProcessUpperCoverCode`

**Files:**
- Modify: `OperationGuidance_new/Views/WorkplaceMissionView_SCII_XT.cs`

- [ ] **Step 1: 在 `ProcessSecondBarCode` 方法之前添加 `ProcessUpperCoverCode` 方法**

在 `WorkplaceMissionView_SCII_XT.cs` 第543行（`public void ProcessSecondBarCode()` 之前）插入：

```csharp
        public async void ProcessUpperCoverCode() {
            if (_barcodeDialog == null) return;

            string productCode = _barcodeDialog.TextBox.GetTextBox(0).Text;
            if (string.IsNullOrEmpty(productCode)) {
                WidgetUtils.ShowWarningPopUp("请输入或扫描上盖码");
                return;
            }

            var config = ConfigUtils.LoadConfig<SciiXtPrinterConfig>();
            if (string.IsNullOrEmpty(config.printer_name)) {
                WidgetUtils.ShowWarningPopUp("打印机名称配置未设置，请先配置打印机。");
                return;
            }

            string? traceCode = await Workflow_SCII_XT.GetUpperCode(productCode);
            if (string.IsNullOrEmpty(traceCode)) {
                WidgetUtils.ShowWarningPopUp("获取追溯码失败，请检查上盖码是否正确或稍后重试。");
                return;
            }

            bool ok = await Task.Run(() => {
                using ZplQrCodePrinter printer = new();
                List<string> list = printer.GetAvailablePrinters();
                if (list.Count == 0) {
                    WidgetUtils.ShowWarningPopUp("未找到任何打印机设备！");
                    return false;
                }
                string? printerName = list.Find(p => p == config.printer_name);
                if (printerName == null) {
                    WidgetUtils.ShowWarningPopUp("未找到指定配置的打印机，请检查配置或打印机。");
                    return false;
                }
                return printer.PrintWithTraceCode(config, traceCode);
            });

            if (ok) {
                _lidCodePrinted = true;
                _lastPrintedConfig = config;
                _barcodeDialog.SignalComplete();
                _barcodeDialog.Dispose();
                _barcodeDialog = null;
                _canReceiveBarcode = false;
            } else {
                WidgetUtils.ShowWarningPopUp("发送指令至打印机失败！请检查日志信息定位问题。");
            }
        }
```

- [ ] **Step 2: 编译确认**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 3: 提交**

```bash
git add OperationGuidance_new/Views/WorkplaceMissionView_SCII_XT.cs
git commit -m "feat(scii-xt): add ProcessUpperCoverCode for API-driven upper cover code printing"
```

---

### Task 4: 改事件绑定

**Files:**
- Modify: `OperationGuidance_new/Views/ReusableWidgets/BarCodeInputPopUpForm_SCII_XT.cs`

- [ ] **Step 1: 将 `CheckSecondBarCode` 中的 Enter 事件绑定从 `ProcessSecondBarCode` 改为 `ProcessUpperCoverCode`**

第135行，将：
```csharp
                    workplace.ProcessSecondBarCode();
```
改为：
```csharp
                    workplace.ProcessUpperCoverCode();
```

- [ ] **Step 2: 编译确认**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 3: 提交**

```bash
git add OperationGuidance_new/Views/ReusableWidgets/BarCodeInputPopUpForm_SCII_XT.cs
git commit -m "fix(scii-xt): route upper cover code input to ProcessUpperCoverCode instead of ProcessSecondBarCode"
```

---

## 验证清单

- [ ] 上盖码对话框回车后调 `GetUpperCode` API
- [ ] API 返回24位追溯码成功解析
- [ ] 追溯码格式校验（长度、前导00、0000段、数字位）
- [ ] 流水号正确提取并传入 ZPL
- [ ] 日期从追溯码反向推导正确（非未来日期）
- [ ] 第一台 ZPL 打印机打印成功
- [ ] API 失败时对话框保持、可重试
- [ ] 分流器 `ProcessSecondBarCode` 不受影响
