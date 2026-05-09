# ArrangerOpenLidScanPopUpForm 校验失败 UX 优化

## 目标

优化 `ArrangerOpenLidScanPopUpForm` 中条码校验失败时的用户体验：增加警告弹窗提示、输入框标红、自动全选内容。

## 改动范围

单文件单方法：`OperationGuidance_new/Views/SubViews/ArrangerOpenLidScanPopUpForm.cs` → `ValidateAndProcess()`

## 当前行为

校验失败时：清空输入框 → 聚焦 → 标红（`IsError = true`）

问题：没有警告提示，用户不知道为何失败；清空内容后无法查看刚才扫了什么码。

## 目标行为

校验失败时：弹出警告提示 → 标红 → 聚焦 → 全选内容

1. `WidgetUtils.ShowWarningPopUp($"条码校验不通过，当前条码【{barcode}】与期望条码不匹配")` — 警告弹窗
2. `IsError = true` — 输入框红框（保留现有逻辑）
3. `Focus()` + `SelectAll()` — 聚焦并全选，重新扫码直接覆盖

## 实现

将 `ValidateAndProcess()` 中校验失败分支从清空文本改为保留文本并全选，并在最前面插入警告弹窗调用。
