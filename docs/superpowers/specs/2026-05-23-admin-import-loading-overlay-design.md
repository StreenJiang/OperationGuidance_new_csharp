# Admin Management Import Loading Overlay Design

## Context

`AdminManagementView` 中"重新导入物料码"按钮触发一个 loading 遮罩层，防止用户在异步导入期间操作界面。当前实现存在三个问题：

1. 非 `TopMost`，可能被其他窗口遮挡
2. 未拦截 `Alt+F4`，用户可在导入中途关闭窗口
3. 单 `Form` + `Opacity = 0.4` 导致弹窗面板本身也变半透明

## Decision

将现有单 Form（`_loadingOverlay`）拆为双 Form 架构：背景遮罩 + 弹窗内容，各自独立控制透明度。

## Architecture

| 组件 | 类型 | 配置 |
|---|---|---|
| `_overlayBackdrop` | `Form` | `BackColor = Black`, `Opacity = 0.4`, `FormBorderStyle = None`, `ShowInTaskbar = false`, `TopMost = true` |
| `_overlayPopup` | `Form` | `BackColor = White`, `Opacity = 1.0`, `FormBorderStyle = None`, `ShowInTaskbar = false`, `TopMost = true`，圆角 Region，内含 Label + ProgressBar |

## Key Behaviors

- **TopMost**: 两个窗体均 `TopMost = true`，`ShowLoadingOverlay(false)` 时 `Dispose()` 释放
- **Block Alt+F4**: 两个窗体均订阅 `FormClosing`，无条件 `e.Cancel = true`，仅能通过代码 `Dispose()` 关闭
- **Transparency**: 背景窗体半透明、内容窗体不透明，互不干扰

## Lifecycle

```
ShowLoadingOverlay(true):
  new _overlayBackdrop → Show() → 定位到主窗体
  new _overlayPopup → Show() → 居中定位

ShowLoadingOverlay(false):
  _overlayPopup.Close() + Dispose()
  _overlayBackdrop.Close() + Dispose()
```

## Scope

- **改**: `AdminManagementView.cs` — `_loadingOverlay` 替换为 `_overlayBackdrop` + `_overlayPopup`，`ShowLoadingOverlay` 方法对应调整
- **不改**: `CustomPopUpForm`、`WaitDialog`、`AdminPasswordDialog`、`OnReimport` 业务逻辑
