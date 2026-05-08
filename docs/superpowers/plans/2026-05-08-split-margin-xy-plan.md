# Split Second Printer Margin Factor into X/Y Independent Parameters — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Split the single `margin_factor` into `margin_x_factor` and `margin_y_factor` across config, ZPL generation, test UI, and caller.

**Architecture:** Four-file refactor — config model defines two independent coefficients (0-1), ZPL generator computes X/Y margins separately, test popup exposes two fields, workplace caller passes both values.

**Tech Stack:** C# .NET 6.0, Windows Forms

---

### Task 1: Update SecondPrinterDetailConfig — replace margin_factor with X/Y

**Files:**
- Modify: `OperationGuidance_new/Utils/IIPSC/SecondPrinterDetailConfig.cs`

- [ ] **Step 1: Replace margin_factor with margin_x_factor and margin_y_factor**

Replace the `margin_factor` property and its default value:

```csharp
// Remove:
// 边距系数(0-1)，0=左上角，1=最大边距
// public double margin_factor { get; set; }

// Add:
// X边距系数(0-1)，0=左对齐，1=最大右边距
public double margin_x_factor { get; set; }
// Y边距系数(0-1)，0=顶部对齐，1=最大下边距
public double margin_y_factor { get; set; }
```

In the constructor, replace `margin_factor = 0.5;` with:
```csharp
margin_x_factor = 0.5;
margin_y_factor = 0.5;
```

- [ ] **Step 2: Build to verify**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```
Expected: build errors in other files referencing `margin_factor` — expected, will fix in subsequent tasks.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Utils/IIPSC/SecondPrinterDetailConfig.cs
git commit -m "refactor: split margin_factor into margin_x_factor and margin_y_factor in SecondPrinterDetailConfig"
```

---

### Task 2: Update ZplQrCodePrinter — split margin parameter in GenerateQrZpl and PrintQrContent

**Files:**
- Modify: `OperationGuidance_new/Utils/IIPSC/ZplQrCodePrinter.cs`

- [ ] **Step 1: Update GenerateQrZpl signature and body**

Change the method signature from:
```csharp
public string GenerateQrZpl(string qrContent,
                            double dpmm = DPMM_300DPI,
                            double labelSizeMm = 9,
                            double qrSizeMm = 5.4,
                            double marginFactor = 0.5) {
```
To:
```csharp
public string GenerateQrZpl(string qrContent,
                            double dpmm = DPMM_300DPI,
                            double labelSizeMm = 9,
                            double qrSizeMm = 5.4,
                            double marginXFactor = 0.5,
                            double marginYFactor = 0.5) {
```

Replace the margin calculation:
```csharp
// Remove:
// int centeredMargin = (labelDots - actualQrDots) / 2;
// int margin = Math.Max(0, (int)(centeredMargin * marginFactor));

// Add:
int centeredMargin = (labelDots - actualQrDots) / 2;
int marginX = Math.Max(0, (int)(centeredMargin * marginXFactor));
int marginY = Math.Max(0, (int)(centeredMargin * marginYFactor));
```

Replace the ZPL return:
```csharp
// Remove:
// return $"^XA^PW{labelDots}^LL{labelDots}^FO{margin},{margin}^BQN,2,{moduleWidth},0,{version}^FDMA,{qrContent}^FS^XZ";

// Add:
return $"^XA^PW{labelDots}^LL{labelDots}^FO{marginX},{marginY}^BQN,2,{moduleWidth},0,{version}^FDMA,{qrContent}^FS^XZ";
```

- [ ] **Step 2: Update PrintQrContent signature**

Change from:
```csharp
public bool PrintQrContent(string content, string printerName,
    double dpmm, double labelSizeMm, double qrSizeMm, double marginFactor) {
    try {
        string zpl = GenerateQrZpl(content, dpmm, labelSizeMm, qrSizeMm, marginFactor);
```
To:
```csharp
public bool PrintQrContent(string content, string printerName,
    double dpmm, double labelSizeMm, double qrSizeMm, double marginXFactor, double marginYFactor) {
    try {
        string zpl = GenerateQrZpl(content, dpmm, labelSizeMm, qrSizeMm, marginXFactor, marginYFactor);
```

- [ ] **Step 3: Build to verify**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```
Expected: build errors now only in PrinterTestPopUpForm.cs and WorkplaceMissionView_SCII_XT.cs.

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_new/Utils/IIPSC/ZplQrCodePrinter.cs
git commit -m "refactor: split marginFactor into marginXFactor/marginYFactor in GenerateQrZpl and PrintQrContent"
```

---

### Task 3: Update PrinterTestPopUpForm — replace single margin field with X/Y fields

**Files:**
- Modify: `OperationGuidance_new/Views/ReusableWidgets/PrinterTestPopUpForm.cs`

- [ ] **Step 1: Replace field declaration**

```csharp
// Remove:
// private CustomTextBoxGroup _marginFactorBox = null!;

// Add:
private CustomTextBoxGroup _marginXFactorBox = null!;
private CustomTextBoxGroup _marginYFactorBox = null!;
```

- [ ] **Step 2: Replace control creation in constructor**

Remove:
```csharp
_marginFactorBox = new("边距系数") {
    Parent = ContentPanel,
    Ratio = 6.95,
};
```

Add:
```csharp
_marginXFactorBox = new("X边距系数") {
    Parent = ContentPanel,
    Ratio = 6.95,
};
_marginYFactorBox = new("Y边距系数") {
    Parent = ContentPanel,
    Ratio = 6.95,
};
```

- [ ] **Step 3: Replace load logic in OnHandleCreated**

Remove:
```csharp
_marginFactorBox.SetValue(0, _detailConfig.margin_factor.ToString());
```

Add:
```csharp
_marginXFactorBox.SetValue(0, _detailConfig.margin_x_factor.ToString());
_marginYFactorBox.SetValue(0, _detailConfig.margin_y_factor.ToString());
```

- [ ] **Step 4: Replace validation in PrintTest (Printer2 branch)**

Remove the `marginFactor` validation block:
```csharp
double marginFactor = double.TryParse(_marginFactorBox.GetTextBox(0).Box.Text, out double mf) && mf >= 0 && mf <= 1 ? mf : -1;
if (marginFactor < 0) {
    _marginFactorBox.CheckError(0, true);
    valid = false;
} else {
    _marginFactorBox.CheckError(0, false);
}
```

Add two validation blocks:
```csharp
double marginXFactor = double.TryParse(_marginXFactorBox.GetTextBox(0).Box.Text, out double mxf) && mxf >= 0 && mxf <= 1 ? mxf : -1;
if (marginXFactor < 0) {
    _marginXFactorBox.CheckError(0, true);
    valid = false;
} else {
    _marginXFactorBox.CheckError(0, false);
}

double marginYFactor = double.TryParse(_marginYFactorBox.GetTextBox(0).Box.Text, out double myf) && myf >= 0 && myf <= 1 ? myf : -1;
if (marginYFactor < 0) {
    _marginYFactorBox.CheckError(0, true);
    valid = false;
} else {
    _marginYFactorBox.CheckError(0, false);
}
```

- [ ] **Step 5: Replace save and print call**

Remove:
```csharp
_detailConfig.margin_factor = marginFactor;
```

Add:
```csharp
_detailConfig.margin_x_factor = marginXFactor;
_detailConfig.margin_y_factor = marginYFactor;
```

Update the `PrintQrContent` call:
```csharp
// Remove:
// return printer.PrintQrContent(content, printerName, dpmm, labelSizeMm, qrSizeMm, marginFactor);

// Add:
return printer.PrintQrContent(content, printerName, dpmm, labelSizeMm, qrSizeMm, marginXFactor, marginYFactor);
```

- [ ] **Step 6: Update ResizeSelf — layout for 7 rows**

Change `rowCount = 6;` to `rowCount = 7;`

Replace the `_marginFactorBox` layout with both fields:
```csharp
// Remove:
// _marginFactorBox.Size = new(boxWidth, boxHeight);
// _marginFactorBox.Margin = new(boxMargin, boxMargin / 2, boxMargin, boxMargin);

// Add:
_marginXFactorBox.Size = new(boxWidth, boxHeight);
_marginXFactorBox.Margin = new(boxMargin, boxMargin / 2, boxMargin, boxMargin / 2);

_marginYFactorBox.Size = new(boxWidth, boxHeight);
_marginYFactorBox.Margin = new(boxMargin, boxMargin / 2, boxMargin, boxMargin);
```

- [ ] **Step 7: Build to verify**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```
Expected: build error only in WorkplaceMissionView_SCII_XT.cs.

- [ ] **Step 8: Commit**

```bash
git add OperationGuidance_new/Views/ReusableWidgets/PrinterTestPopUpForm.cs
git commit -m "refactor: split margin factor into X/Y fields in PrinterTestPopUpForm"
```

---

### Task 4: Update WorkplaceMissionView_SCII_XT — pass two factors to PrintQrContent

**Files:**
- Modify: `OperationGuidance_new/Views/WorkplaceMissionView_SCII_XT.cs`

- [ ] **Step 1: Update SendQRCodeToPrinter call**

Find the `PrintQrContent` call in `SendQRCodeToPrinter` and change:
```csharp
// Remove:
// if (!printer.PrintQrContent(qrContent, printerName,
//     detailConfig.dpmm, detailConfig.label_size_mm,
//     detailConfig.qr_size_mm, detailConfig.margin_factor)) {

// Add:
if (!printer.PrintQrContent(qrContent, printerName,
    detailConfig.dpmm, detailConfig.label_size_mm,
    detailConfig.qr_size_mm, detailConfig.margin_x_factor, detailConfig.margin_y_factor)) {
```

- [ ] **Step 2: Build to verify clean**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```
Expected: clean build, no errors.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/WorkplaceMissionView_SCII_XT.cs
git commit -m "refactor: pass margin_x_factor and margin_y_factor to PrintQrContent in SCII_XT workplace"
```
