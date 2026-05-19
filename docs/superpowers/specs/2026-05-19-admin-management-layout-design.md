# Admin Management Layout Redesign

2026-05-19

## Problem

`AdminManagementView` inherits from `AbstractCustomPanel` → `FlowLayoutPanel`. The flow layout engine overrides all manual `Location` assignments, causing:

1. Back button, page title, and two cards to flow left-to-right in a single row (title label pushed out of view)
2. Cards not horizontally centered
3. Card internal spacings are equal everywhere — no rhythmic grouping (rows vs button area look the same distance)

## Root Cause

`FlowLayoutPanel.OnLayout` repositions all child controls on every layout pass. `LayoutCards()` sets `Location`, then the flow engine immediately moves them. All views inheriting from `AbstractCustomPanel` have this behavior.

## Solution

Add a plain `Panel` (`_contentArea`, `Dock = Fill`) as the sole child of `AdminManagementView`. All content goes into `_contentArea` with absolute positioning — bypasses the flow engine entirely.

No changes to `CardPanel.cs`. No changes to the inheritance chain.

## Layout

```
_contentArea (Panel, Dock=Fill, absolute positioning)
├── ← 返回          (flat label, left-aligned, 12px gray)
├── 后台管理          (page title, 22px bold, below back)
├── Card 1            (centered, max 640px)
├── Card 2            (centered, max 640px)
└── loading overlay   (unchanged)
```

## Spacing System

| Zone | Value |
|------|-------|
| Content area horizontal padding | `WidgetUtils.ContentInnerBorderMargin()` |
| Back link → page title | 8px |
| Page title → first card | 28px |
| Card ↔ card | 24px |
| Card internal: form row ↔ form row | 16px (up from 12px, distinct from logical grouping) |
| Card internal: last row → action button | 20px (breaking rhythm to separate actions from inputs) |
| Card internal: label → input | 80px fixed label column |
| Card internal: button right-align | anchored to input right edge |

## Back Link

- `Label`, no border, no background, `Cursor = Hand`
- Text `"← 返回"`, 12px regular, color `#888888`
- MouseEnter → color `#E86C10`, MouseLeave → `#888888`
- Click → `WidgetUtils.BackToLoginView?.Invoke(false)`

## Page Title

- `Label`, `"后台管理"`, 22px bold, color `#333333`
- Position: below back link + 8px

## Cards

- Max width 640px, centered: `(contentArea.Width - cardWidth) / 2`
- `CardPanel` unchanged from current — Title (16px orange), rule, ContentPadding (54 top)
- Password card: `Height = 220`
- Reimport card: `Height = 170`

## Card Internal — Password Card

```
y = pad.Top (54)
  "登录密码" label (pad.Left, y+4)  |  TextBox (pad.Left+80, y, width=260, height=rowH)
y += rowH + 16
  "操作密码" label (pad.Left, y+4)  |  TextBox (pad.Left+80, y, width=260, height=rowH)
y += rowH + 20
  [保存修改] button, right-aligned
```

## Card Internal — Reimport Card

```
y = pad.Top (54)
  Description Label, 14px (up from default 9pt), multiline
y = desc.Bottom + 20
  [重新导入物料码] button
```

## Files Changed

| File | Scope |
|------|-------|
| `Views/AdminManagementView.cs` | Rewrite layout: add `_contentArea` Panel, new header, spacing constants |

`CardPanel.cs` — no changes.

## Performance

No gradients, no blur, no animation. All coordinates computed in `LayoutCards()` on `SizeChanged`. Static GDI+ paint only. One extra `Panel` allocation — negligible.
