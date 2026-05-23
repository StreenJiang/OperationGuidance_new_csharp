# ZPL Label Traceability Code Redesign

## Summary

Replace the old 64-bit QR code label format (partCode+supplierCode+batch+serial+softwareVer+hardwareVer) with a new 24-character traceability code format. The label layout changes from 4 lines of split QR content to 5 lines of human-readable info + QR code generated from the traceability code.

## Label Layout (110mm × 50mm, 203dpi)

| Line | Content | ZPL Position | Notes |
|---|---|---|---|
| 1 | Supplier name | `^FT50,40` | Replaces old logo GFA; default "SCII" |
| 2 | Project name | `^FT50,105` | No padding; default "NE17" |
| 3 | Date (yyyy/MM/dd) | `^FT50,140` | Auto-generated |
| 4 | Serial number | `^FT50,175` | No padding for display |
| 5 | 24-char traceability code | `^FT50,210` | Extends below QR code |
| QR | Traceability code content | `^FO350,50` | Module size 5; 24 chars → QR v2 (25×25) |

All text uses font `^A0N,33,31`. Row spacing ≈35 dots.

## 24-Character Traceability Code Format

| Positions | Length | Content | Source |
|---|---|---|---|
| 1-2 | 2 | `00` (placeholder) | Hardcoded |
| 3-4 | 2 | Manufacture location | Config `manufacture_location`, default "XA" |
| 5 | 1 | Last digit of year | Auto (2026 → "6") |
| 6-8 | 3 | Day of year | Auto (001-366) |
| 9-12 | 4 | Serial number | Config `sn`, zero-padded to 4 digits |
| 13-16 | 4 | `0000` (placeholder) | Hardcoded |
| 17-24 | 8 | Part number | Config `part_number`, default "12296650" |

Example: `00XA61430001000012296650`

## Config Changes (`SciiXtPrinterConfig`)

### Removed fields
- `part_code` (old 10-digit)
- `vender_code` (old 6-digit)
- `batch_code` (old 8-digit, was runtime)
- `soft_version`
- `hard_version`
- `location_y` (old logo GFA graphic)

### New fields
- `supplier_name` (string, default "SCII")
- `project_name` (string, default "NE17")
- `manufacture_location` (string, default "XA", 2 chars)
- `part_number` (string, default "12296650", 8 chars)

### Retained fields
- `sn` (int, runtime)
- `text_1` through `text_5` (ZPL positioning, updated defaults)
- `sn_pos_x`, `sn_pos_y` (QR position)
- `printer_name`, `second_printer_name`, `second_barcode_length`
- `enabled`, `enabled_second`

### Updated defaults

```
text_1 = "^FT50,40^A0N,33,31^FH\\^FD"   // supplier name
text_2 = "^FT50,105^A0N,33,31^FH\\^FD"  // project name
text_3 = "^FT50,140^A0N,33,31^FH\\^FD"  // date
text_4 = "^FT50,175^A0N,33,31^FH\\^FD"  // serial number
text_5 = "^FT50,210^A0N,33,31^FH\\^FD"  // traceability code
```

## Code Changes

### `ZplQrCodePrinter.cs`

1. **`Generate24BitTraceCode`** (new) — Builds 24-char traceability code. Inputs: location (2), serial (int), partNumber (8). Auto-derives year digit and dayOfYear from `DateTime.Now`.

2. **`GenerateZplCommand`** (rewrite) — Generates ZPL with 5 text lines + QR code. QR content = 24-char traceability code. Removes old 64-char length check (now 24). Removes `location_y` line. Adds `text_5`.

3. **`QuickPrint`** (rewrite) — Reads new fields from config, calls `Generate24BitTraceCode`, then `GenerateZplCommand`, then `PrintViaZpl`.

4. **`PrintWithSn`** (rewrite) — Same as QuickPrint but with explicit SN.

5. Remove old helpers no longer needed: `ValidateFixedLength`, `PadLeft` may simplify.

6. Remove old constants: `PART_CODE_LENGTH`, `SUPPLIER_CODE_LENGTH`, etc.

### `VariableSettingsView_SCII_XT.cs`

Replace 4 old input fields (`_partCodeBox`, `_venderCodeBox`, `_softVersionBox`, `_hardVersionBox`) with 4 new fields:

- `_supplierNameBox` — "供应商名称"
- `_projectNameBox` — "项目名称"
- `_manufactureLocationBox` — "制造地代码"
- `_partNumberBox` — "零件号"

All 4 fields follow enable/disable toggle with `_enablePrinter.Checked`.

Affected methods: `InitializePrinterSettingsPanel`, `ResizeMissionSettings`, `LoadSettings`, `SaveMissionSettings`, `ResetAllToDefault`, `CheckSavedFunc_detail`, enable toggle handler.

## Files Affected

| File | Change |
|---|---|
| `ZplQrCodePrinter.cs` | Rewrite code generation + ZPL methods |
| `SciiXtPrinterConfig.cs` | Field add/remove/update defaults |
| `VariableSettingsView_SCII_XT.cs` | Replace 4 input fields in printer config panel |
| `WorkplaceMissionView_SCII_XT.cs` | `SendToPrinter` — no API change (still calls `QuickPrint`) |
| `PrinterTestPopUpForm.cs` | Printer1 test uses `sn` directly, code path via `PrintWithSn` — no UI change needed |
