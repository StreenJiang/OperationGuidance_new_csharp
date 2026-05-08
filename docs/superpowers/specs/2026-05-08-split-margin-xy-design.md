# Split Second Printer Margin Factor into X/Y Independent Parameters

**Date:** 2026-05-08
**Branch:** v2.0.x

## Summary

The `margin_factor` in second printer detail config controls both X and Y margins as a single value. Split it into two independent coefficients (`margin_x_factor`, `margin_y_factor`) so X and Y offsets can be adjusted independently.

## Design

### File Changes (4 files)

#### 1. `SecondPrinterDetailConfig.cs`
- Remove `margin_factor`
- Add `margin_x_factor` (double, default 0.5, range 0-1)
- Add `margin_y_factor` (double, default 0.5, range 0-1)

#### 2. `ZplQrCodePrinter.cs`
- `GenerateQrZpl`: change signature from `(..., marginFactor)` to `(..., marginXFactor, marginYFactor)`
- Compute X and Y margins independently:
  ```
  marginX = Math.Max(0, (int)(centeredMargin * marginXFactor))
  marginY = Math.Max(0, (int)(centeredMargin * marginYFactor))
  ```
- ZPL output: `^FO{marginX},{marginY}` (was `^FO{margin},{margin}`)
- `PrintQrContent`: update signature to accept two factor parameters

#### 3. `PrinterTestPopUpForm.cs`
- Remove `_marginFactorBox` (single field)
- Add `_marginXFactorBox` ("X边距系数") and `_marginYFactorBox` ("Y边距系数")
- Load from `detailConfig.margin_x_factor` / `margin_y_factor`
- Save to `detailConfig.margin_x_factor` / `margin_y_factor`
- Row count: 6 → 7

#### 4. `WorkplaceMissionView_SCII_XT.cs`
- `SendQRCodeToPrinter`: pass `detailConfig.margin_x_factor` and `detailConfig.margin_y_factor` to `PrintQrContent`

### Backward Compatibility
Old config files with `margin_factor` will lose that value on next save. On first load after upgrade, both X and Y factors default to 0.5.
