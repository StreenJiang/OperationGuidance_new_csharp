# Printer Test Popup — Design Spec

**Date:** 2026-05-06
**Branch:** v2.0.x

## Summary

Add test-print buttons for printer 1 and printer 2 in `VariableSettingsView_SCII_XT`,
each opening a popup where the operator can input test data and trigger a print.

## Components

### New file: `OperationGuidance_new/Views/ReusableWidgets/PrinterTestPopUpForm.cs`

A `CustomPopUpForm` subclass with a `PrinterTestMode` enum:

- `Printer1` — test the primary printer with a custom SN value
- `Printer2` — test the second printer with arbitrary QR content

#### Popup content

| Element | Printer1 mode | Printer2 mode |
|---|---|---|
| Title | "打印机1测试" | "打印机2测试" |
| Input 1 | `CustomTextBoxGroup` label "SN", `PositiveIntOnly = true` | `CustomTextBoxGroup` label "二维码内容" |
| Input 2 (shared) | `CustomComboBoxGroup<string>` label "打印机名称" | same |
| Buttons | "打印测试" + "关闭" | same |

- Printer list loaded from `ZplQrCodePrinter.GetAvailablePrinters()` on `OnHandleCreated`.
- On open, pre-fill SN with current `SciiXtPrinterConfig.sn`; select current printer name.
- Layout follows `BoltPopUpForm` / `BarCodeInputPopUpForm_SCII_XT` sizing patterns
  (`ResizeSelf` → calculate dimensions → `SetContentSizeAndSelfSize`).

### Modified file: `VariableSettingsView_SCII_XT.cs`

Two `CommonButtonGroup` fields (`_printerTestBtn`, `_secondPrinterTestBtn`), each with one button labeled "测试", placed next to the printer-name fields.

- **Initialize:** in `InitializePrinterSettingsPanel()`, instantiate the button groups next to `_printerName` / `_secondPrinterName`.
- **Resize:** in `ResizeMissionSettings()`, size and position them to the right of the corresponding printer-name combo boxes.
- **Click handlers:** open `PrinterTestPopUpForm` with the appropriate mode.
- Not tracked in `CheckSavedFunc_detail` — test buttons do not mutate config.

### Modified file: `ZplQrCodePrinter.cs`

Two new methods:

```csharp
// Printer 1: load config, override SN, generate 64-bit code + ZPL, print
public bool PrintWithSn(SciiXtPrinterConfig config, int sn, string printerName)

// Printer 2: generate QR ZPL from arbitrary content, print
public bool PrintQrContent(string content, string printerName)
```

## Data Flow

### Printer 1 test print

1. User clicks "测试" next to printer 1 → `PrinterTestPopUpForm(mode: Printer1)` opens.
2. User enters SN, selects printer, clicks "打印测试".
3. Validate SN > 0 and printer selected.
4. Call `ZplQrCodePrinter.PrintWithSn(config, sn, printerName)`:
   - Load current `SciiXtPrinterConfig`.
   - Override `config.sn` with the input value.
   - `Generate64BitCode(partCode, supplierCode, batchCode, paddedSN, softVersion, hardVersion)`.
   - `GenerateZplCommand(config, qrContent)`.
   - `PrintViaZpl(printerName, zpl)`.
5. Show success/error toast, close popup.

### Printer 2 test print

1. User clicks "测试" next to printer 2 → `PrinterTestPopUpForm(mode: Printer2)` opens.
2. User enters QR content, selects printer, clicks "打印测试".
3. Validate content not empty and printer selected.
4. Call `ZplQrCodePrinter.PrintQrContent(content, printerName)`:
   - `GenerateQrZpl(content)`.
   - `PrintViaZpl(printerName, zpl)`.
5. Show success/error toast, close popup.

## Error Handling

| Condition | Behavior |
|---|---|
| No printer selected | `SetError(true)` on printer combo box |
| SN empty or ≤ 0 (Printer1) | `SetError(true)` on SN input (plus `PositiveIntOnly` UI-level guard) |
| Content empty (Printer2) | `SetError(true)` on content input |
| `PrintViaZpl` returns false | `WidgetUtils.ShowWarningPopUp("打印失败")` |
| Exception during print | Caught in printer methods, logged, returns false → warning popup |

## Edge Cases

- If `GetAvailablePrinters()` returns an empty list, the printer dropdown is empty; "打印测试" validates and shows an error.
- The SN input uses `PositiveIntOnly = true` for UI-level validation, with an additional non-empty check on submit as a safety net.
- Popup does not auto-close on print failure — user can retry with different values.
- Popup does not modify any saved config — it only reads current config for Printer1 flow.
