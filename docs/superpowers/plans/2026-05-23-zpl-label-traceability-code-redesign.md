# ZPL Label Traceability Code Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace 64-bit label format with 24-char traceability code label, update config model and parameter UI.

**Architecture:** Config model (`SciiXtPrinterConfig`) drives both ZPL generation and the settings UI. `ZplQrCodePrinter` generates the 24-char traceability code and builds ZPL commands with 5 text lines + QR. `VariableSettingsView_SCII_XT` exposes the new config fields in the printer settings panel.

**Tech Stack:** C#, WinForms, ZPL (Zebra Programming Language)

---

## File Structure

| File | Responsibility |
|---|---|
| `OperationGuidance_new/Utils/IIPSC/SciiXtPrinterConfig.cs` | Config model — fields, defaults, ignore attributes |
| `OperationGuidance_new/Utils/IIPSC/ZplQrCodePrinter.cs` | Traceability code generation + ZPL command building + printing |
| `OperationGuidance_new/Views/VariableSettingsView_SCII_XT.cs` | Settings UI panel — field bindings, save/load/reset/check |
| `OperationGuidance_new/Views/WorkplaceMissionView_SCII_XT.cs` | `SendToPrinter` — remove `batch_code` assignment |
| `OperationGuidance_new/Views/ReusableWidgets/PrinterTestPopUpForm.cs` | Printer1 test — remove `batch_code` assignment |

---

### Task 1: Config Model Field Migration

**Files:**
- Modify: `OperationGuidance_new/Utils/IIPSC/SciiXtPrinterConfig.cs`

- [ ] **Step 1: Replace fields and defaults in SciiXtPrinterConfig**

Replace the old field declarations (lines 8-19) and constructor defaults (lines 55-66) with new fields:

Remove these fields:
```csharp
public string part_code { get; set; }
public string vender_code { get; set; }
[ConfigIgnore] public string batch_code { get; set; }
public string soft_version { get; set; }
public string hard_version { get; set; }
public string location_y { get; set; }
```

Add these fields in their place:
```csharp
public string supplier_name { get; set; }
public string project_name { get; set; }
public string manufacture_location { get; set; }
public string part_number { get; set; }
```

Change `text_4` to `text_5` by adding a new property:
```csharp
public string text_5 { get; set; }
```

Remove old defaults from constructor:
```csharp
part_code = "7161620072";
vender_code = "777168";
batch_code = "";
sn = 0;  // keep
soft_version = "V1.0.0";
hard_version = "HW3.2";
location_y = "^FO0,30^GFA,..."; // the long GFA string
```

Add new defaults to constructor:
```csharp
supplier_name = "SCII";
project_name = "NE17";
manufacture_location = "XA";
part_number = "12296650";
sn = 0;
```

Update text defaults (replace old `text_1`-`text_4` + `location_y` with new `text_1`-`text_5`):
```csharp
text_1 = "^FT50,40^A0N,33,31^FH\\^FD";   // supplier name
text_2 = "^FT50,105^A0N,33,31^FH\\^FD";  // project name
text_3 = "^FT50,140^A0N,33,31^FH\\^FD";  // date
text_4 = "^FT50,175^A0N,33,31^FH\\^FD";  // serial number
text_5 = "^FT50,210^A0N,33,31^FH\\^FD";  // traceability code
```

- [ ] **Step 2: Verify build compiles (will fail — expected, callers still reference old fields)**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj 2>&1 | head -20
```

Expected: compile errors for removed fields in ZplQrCodePrinter.cs, VariableSettingsView_SCII_XT.cs, WorkplaceMissionView_SCII_XT.cs, PrinterTestPopUpForm.cs

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Utils/IIPSC/SciiXtPrinterConfig.cs
git commit -m "refactor: replace old label fields with traceability code fields in SciiXtPrinterConfig
Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

### Task 2: Rewrite ZPL Code Generation Logic

**Files:**
- Modify: `OperationGuidance_new/Utils/IIPSC/ZplQrCodePrinter.cs`

- [ ] **Step 1: Remove old constants and add new traceability code method**

Remove lines 10-15 (old constants):
```csharp
private const int PART_CODE_LENGTH = 10;
private const int SUPPLIER_CODE_LENGTH = 6;
private const int BATCH_CODE_LENGTH = 8;
private const int SERIAL_NUM_LENGTH = 8;
private const int SOFTWARE_VERSION_LENGTH = 16;
private const int HARDWARE_VERSION_LENGTH = 16;
```

Add new constant for traceability code length:
```csharp
private const int TRACE_CODE_LENGTH = 24;
```

- [ ] **Step 2: Replace Generate64BitCode with Generate24BitTraceCode**

Replace the entire `Generate64BitCode` method (lines 31-50) with:

```csharp
/// <summary>
/// 生成24位追溯码
/// 格式: 00(2) + 制造地(2) + 年份尾号(1) + 自然日(3) + 流水号(4) + 0000(4) + 零件号(8)
/// </summary>
public string Generate24BitTraceCode(string manufactureLocation, string partNumber, int serialNumber) {
    if (string.IsNullOrEmpty(manufactureLocation) || manufactureLocation.Length != 2)
        throw new ArgumentException("制造地代码必须为2位", nameof(manufactureLocation));
    if (string.IsNullOrEmpty(partNumber) || partNumber.Length != 8)
        throw new ArgumentException("零件号必须为8位", nameof(partNumber));

    var now = DateTime.Now;
    string yearDigit = (now.Year % 10).ToString();
    string dayOfYear = now.DayOfYear.ToString().PadLeft(3, '0');
    string serial = serialNumber.ToString().PadLeft(4, '0');

    return $"00{manufactureLocation}{yearDigit}{dayOfYear}{serial}0000{partNumber}";
}
```

- [ ] **Step 3: Rewrite GenerateZplCommand**

Replace the entire `GenerateZplCommand` method (lines 58-86) with:

```csharp
public string GenerateZplCommand(SciiXtPrinterConfig sProfile, string traceCode, int moduleSize = 5) {
    if (string.IsNullOrEmpty(traceCode) || traceCode.Length != TRACE_CODE_LENGTH)
        throw new ArgumentException($"追溯码必须是{TRACE_CODE_LENGTH}位", nameof(traceCode));

    var zpl = new StringBuilder();
    zpl.AppendLine("^XA");

    zpl.AppendLine($"{sProfile.text_1}{sProfile.supplier_name}^FS");
    zpl.AppendLine($"{sProfile.text_2}{sProfile.project_name}^FS");
    zpl.AppendLine($"{sProfile.text_3}{DateTime.Now:yyyy/MM/dd}^FS");
    zpl.AppendLine($"{sProfile.text_4}{sProfile.sn}^FS");
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

- [ ] **Step 4: Rewrite QuickPrint**

Replace the `QuickPrint` method (lines 148-176) with:

```csharp
public bool QuickPrint(SciiXtPrinterConfig config) {
    string printerName = string.Empty;
    try {
        printerName = config.printer_name;
        string traceCode = Generate24BitTraceCode(
            config.manufacture_location, config.part_number, config.sn);
        string zpl = GenerateZplCommand(config, traceCode);
        return PrintViaZpl(printerName, zpl);
    } catch (Exception ex) {
        log.Error($"Print fails! PrinterName = [{printerName}]：{ex.Message}");
        return false;
    }
}
```

- [ ] **Step 5: Rewrite PrintWithSn**

Replace the `PrintWithSn` method (lines 178-189) with:

```csharp
public bool PrintWithSn(SciiXtPrinterConfig config, int sn, string printerName) {
    try {
        string traceCode = Generate24BitTraceCode(
            config.manufacture_location, config.part_number, sn);
        string zpl = GenerateZplCommand(config, traceCode);
        return PrintViaZpl(printerName, zpl);
    } catch (Exception ex) {
        log.Error($"PrintWithSn fails! PrinterName = [{printerName}], SN = [{sn}]：{ex.Message}");
        return false;
    }
}
```

- [ ] **Step 6: Remove dead helper methods**

Remove `ValidateFixedLength` method (lines 217-221) — no longer called.

Keep `PadLeft` (lines 226-233) — still used by `Generate24BitTraceCode`.

- [ ] **Step 7: Commit**

```bash
git add OperationGuidance_new/Utils/IIPSC/ZplQrCodePrinter.cs
git commit -m "refactor: rewrite ZPL generation for 24-char traceability code label
Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

### Task 3: Update Call Sites (remove batch_code assignments)

**Files:**
- Modify: `OperationGuidance_new/Views/WorkplaceMissionView_SCII_XT.cs:249`
- Modify: `OperationGuidance_new/Views/ReusableWidgets/PrinterTestPopUpForm.cs:191`

- [ ] **Step 1: Remove batch_code assignment in WorkplaceMissionView_SCII_XT.cs**

In `SendToPrinter` method, remove line 249:
```csharp
config.batch_code = DateTime.Now.ToString(MainUtils.DATETIME_FORMAT_YYYYMMDD);
```

Keep line 250: `config.sn = _okSumToday + 1;`

- [ ] **Step 2: Remove batch_code assignment in PrinterTestPopUpForm.cs**

In `PrintTest` method, remove line 191:
```csharp
_config.batch_code = DateTime.Now.ToString(MainUtils.DATETIME_FORMAT_YYYYMMDD);
```

- [ ] **Step 3: Build check**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj 2>&1 | tail -10
```

Expected: build succeeds (ZplQrCodePrinter + SciiXtPrinterConfig changes should be backward-compatible for these callers)

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_new/Views/WorkplaceMissionView_SCII_XT.cs OperationGuidance_new/Views/ReusableWidgets/PrinterTestPopUpForm.cs
git commit -m "fix: remove batch_code assignments after traceability code migration
Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

### Task 4: Replace Config UI Fields

**Files:**
- Modify: `OperationGuidance_new/Views/VariableSettingsView_SCII_XT.cs`

There are 12 locations in this file that reference the old field names. Below each location is listed with the replacement code.

- [ ] **Step 1: Replace field declarations (lines 53-60)**

Replace:
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

With:
```csharp
private CustomTextBoxGroup _supplierNameBox;
private string _supplierNameOriginal;
private CustomTextBoxGroup _projectNameBox;
private string _projectNameOriginal;
private CustomTextBoxGroup _manufactureLocationBox;
private string _manufactureLocationOriginal;
private CustomTextBoxGroup _partNumberBox;
private string _partNumberOriginal;
```

- [ ] **Step 2: Update InitializePrinterSettingsPanel (lines 350-368)**

Replace the 4 old field initializations:
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

With:
```csharp
_supplierNameBox = new("供应商名称") {
    Parent = _printerSettingsContentPanel,
    Ratio = 6.95,
};

_projectNameBox = new("项目名称") {
    Parent = _printerSettingsContentPanel,
    Ratio = 6.95,
};

_manufactureLocationBox = new("制造地代码") {
    Parent = _printerSettingsContentPanel,
    Ratio = 6.95,
};

_partNumberBox = new("零件号") {
    Parent = _printerSettingsContentPanel,
    Ratio = 6.95,
};
```

- [ ] **Step 3: Update enable/disable toggle handler (lines 397-400)**

Replace:
```csharp
_partCodeBox.Enabled = _enablePrinter.Checked;
_venderCodeBox.Enabled = _enablePrinter.Checked;
_softVersionBox.Enabled = _enablePrinter.Checked;
_hardVersionBox.Enabled = _enablePrinter.Checked;
```

With:
```csharp
_supplierNameBox.Enabled = _enablePrinter.Checked;
_projectNameBox.Enabled = _enablePrinter.Checked;
_manufactureLocationBox.Enabled = _enablePrinter.Checked;
_partNumberBox.Enabled = _enablePrinter.Checked;
```

- [ ] **Step 4: Update SaveMissionSettings (lines 436-453)**

Replace the old field save block:
```csharp
printerConfig.part_code = _partCodeBox.GetTextBox(0).Box.Text;
printerConfig.vender_code = _venderCodeBox.GetTextBox(0).Box.Text;
printerConfig.soft_version = _softVersionBox.GetTextBox(0).Box.Text;
printerConfig.hard_version = _hardVersionBox.GetTextBox(0).Box.Text;
```

With:
```csharp
printerConfig.supplier_name = _supplierNameBox.GetTextBox(0).Box.Text;
printerConfig.project_name = _projectNameBox.GetTextBox(0).Box.Text;
printerConfig.manufacture_location = _manufactureLocationBox.GetTextBox(0).Box.Text;
printerConfig.part_number = _partNumberBox.GetTextBox(0).Box.Text;
```

Replace the old original-value assignment block:
```csharp
_partCodeOriginal = printerConfig.part_code;
_venderCodeOriginal = printerConfig.vender_code;
_softVersionOriginal = printerConfig.soft_version;
_hardVersionOriginal = printerConfig.hard_version;
```

With:
```csharp
_supplierNameOriginal = printerConfig.supplier_name;
_projectNameOriginal = printerConfig.project_name;
_manufactureLocationOriginal = printerConfig.manufacture_location;
_partNumberOriginal = printerConfig.part_number;
```

- [ ] **Step 5: Update ResizeMissionSettings (lines 579-587)**

Replace the old layout block (second and third row):
```csharp
// Resize box - second row
_partCodeBox.Size = new(boxWidth, BoxNBtnHeight);
_partCodeBox.Margin = new(0, boxVMargin, ContentHGap / 2, 0);
_venderCodeBox.Size = new(boxWidth, BoxNBtnHeight);
_venderCodeBox.Margin = new(0, boxVMargin, 0, 0);
// Resize box - third row
_softVersionBox.Size = new(boxWidth, BoxNBtnHeight);
_softVersionBox.Margin = new(0, boxVMargin, ContentHGap / 2, 0);
_hardVersionBox.Size = new(boxWidth, BoxNBtnHeight);
_hardVersionBox.Margin = new(0, boxVMargin, 0, 0);
```

With:
```csharp
// Resize box - second row
_supplierNameBox.Size = new(boxWidth, BoxNBtnHeight);
_supplierNameBox.Margin = new(0, boxVMargin, ContentHGap / 2, 0);
_projectNameBox.Size = new(boxWidth, BoxNBtnHeight);
_projectNameBox.Margin = new(0, boxVMargin, 0, 0);
// Resize box - third row
_manufactureLocationBox.Size = new(boxWidth, BoxNBtnHeight);
_manufactureLocationBox.Margin = new(0, boxVMargin, ContentHGap / 2, 0);
_partNumberBox.Size = new(boxWidth, BoxNBtnHeight);
_partNumberBox.Margin = new(0, boxVMargin, 0, 0);
```

- [ ] **Step 6: Update LoadSettings (lines 737-740, 761-764, 773-776)**

Replace the old enable-state block:
```csharp
_partCodeBox.Enabled = _enablePrinter.Checked;
_venderCodeBox.Enabled = _enablePrinter.Checked;
_softVersionBox.Enabled = _enablePrinter.Checked;
_hardVersionBox.Enabled = _enablePrinter.Checked;
```

With:
```csharp
_supplierNameBox.Enabled = _enablePrinter.Checked;
_projectNameBox.Enabled = _enablePrinter.Checked;
_manufactureLocationBox.Enabled = _enablePrinter.Checked;
_partNumberBox.Enabled = _enablePrinter.Checked;
```

Replace the old value-binding block:
```csharp
_partCodeBox.GetTextBox(0).Box.Text = printerConfig.part_code;
_venderCodeBox.GetTextBox(0).Box.Text = printerConfig.vender_code;
_softVersionBox.GetTextBox(0).Box.Text = printerConfig.soft_version;
_hardVersionBox.GetTextBox(0).Box.Text = printerConfig.hard_version;
```

With:
```csharp
_supplierNameBox.GetTextBox(0).Box.Text = printerConfig.supplier_name;
_projectNameBox.GetTextBox(0).Box.Text = printerConfig.project_name;
_manufactureLocationBox.GetTextBox(0).Box.Text = printerConfig.manufacture_location;
_partNumberBox.GetTextBox(0).Box.Text = printerConfig.part_number;
```

Replace the old original-value block:
```csharp
_partCodeOriginal = printerConfig.part_code;
_venderCodeOriginal = printerConfig.vender_code;
_softVersionOriginal = printerConfig.soft_version;
_hardVersionOriginal = printerConfig.hard_version;
```

With:
```csharp
_supplierNameOriginal = printerConfig.supplier_name;
_projectNameOriginal = printerConfig.project_name;
_manufactureLocationOriginal = printerConfig.manufacture_location;
_partNumberOriginal = printerConfig.part_number;
```

- [ ] **Step 7: Update ResetAllToDefault (lines 863-866, 889-892)**

Replace the old enable-state reset block:
```csharp
_partCodeBox.Enabled = _enablePrinter.Checked;
_venderCodeBox.Enabled = _enablePrinter.Checked;
_softVersionBox.Enabled = _enablePrinter.Checked;
_hardVersionBox.Enabled = _enablePrinter.Checked;
```

With:
```csharp
_supplierNameBox.Enabled = _enablePrinter.Checked;
_projectNameBox.Enabled = _enablePrinter.Checked;
_manufactureLocationBox.Enabled = _enablePrinter.Checked;
_partNumberBox.Enabled = _enablePrinter.Checked;
```

Replace the old value reset block:
```csharp
_partCodeBox.GetTextBox(0).Box.Text = defaultConfig.part_code;
_venderCodeBox.GetTextBox(0).Box.Text = defaultConfig.vender_code;
_softVersionBox.GetTextBox(0).Box.Text = defaultConfig.soft_version;
_hardVersionBox.GetTextBox(0).Box.Text = defaultConfig.hard_version;
```

With:
```csharp
_supplierNameBox.GetTextBox(0).Box.Text = defaultConfig.supplier_name;
_projectNameBox.GetTextBox(0).Box.Text = defaultConfig.project_name;
_manufactureLocationBox.GetTextBox(0).Box.Text = defaultConfig.manufacture_location;
_partNumberBox.GetTextBox(0).Box.Text = defaultConfig.part_number;
```

- [ ] **Step 8: Update CheckSavedFunc_detail (lines 993-996)**

Replace:
```csharp
|| CheckSvedFuncSeparately(_partCodeBox.GetTextBox(0).Box.Text != _partCodeOriginal, "零部件编码")
|| CheckSvedFuncSeparately(_venderCodeBox.GetTextBox(0).Box.Text != _venderCodeOriginal, "供应商编码")
|| CheckSvedFuncSeparately(_softVersionBox.GetTextBox(0).Box.Text != _softVersionOriginal, "软件版本")
|| CheckSvedFuncSeparately(_hardVersionBox.GetTextBox(0).Box.Text != _hardVersionOriginal, "硬件版本")
```

With:
```csharp
|| CheckSvedFuncSeparately(_supplierNameBox.GetTextBox(0).Box.Text != _supplierNameOriginal, "供应商名称")
|| CheckSvedFuncSeparately(_projectNameBox.GetTextBox(0).Box.Text != _projectNameOriginal, "项目名称")
|| CheckSvedFuncSeparately(_manufactureLocationBox.GetTextBox(0).Box.Text != _manufactureLocationOriginal, "制造地代码")
|| CheckSvedFuncSeparately(_partNumberBox.GetTextBox(0).Box.Text != _partNumberOriginal, "零件号")
```

- [ ] **Step 9: Build verification**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj 2>&1 | tail -5
```

Expected: build succeeds with no errors.

- [ ] **Step 10: Commit**

```bash
git add OperationGuidance_new/Views/VariableSettingsView_SCII_XT.cs
git commit -m "refactor: replace printer config UI fields for traceability code
Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

### Task 5: Final Build Verification

- [ ] **Step 1: Clean build**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj 2>&1
```

Expected: Zero errors, zero warnings related to these changes.

- [ ] **Step 2: Verify all references resolved**

Search for any remaining references to removed fields:

```bash
cd D:/AllProjects/CsharpProjects/OperationGuidance_new && grep -rn "part_code\|vender_code\|batch_code\|soft_version\|hard_version\|location_y\|partCode\|venderCode\|softVersion\|hardVersion" --include="*.cs" OperationGuidance_new/
```

Expected: No matches for removed SciiXtPrinterConfig fields. (Some variable names in unrelated files may match but those use different property names — `batch_no` in MES config, etc. — confirm they are not SciiXtPrinterConfig references.)

