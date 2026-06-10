# ComboBox 下拉框屏幕边界自适应

**日期**: 2026-06-10
**状态**: 已评审
**关联**: `BoltPopUpForm_SCII.cs`

## 背景

`BoltPopUpForm_SCII` 中物料码绑定点位时，每个物料码行包含一个 `CustomComboBoxGroup<int>` 用于选择条码规则 ID。当物料码数量较多（如 20+ 条），弹窗表单变高，底部 ComboBox 的下拉框向下展开时会超出屏幕工作区底部，导致部分物料码 ID 无法看到也无法选中。

## 根因

`CustomComboBox<T>` 的下拉框实现为一个独立的无边框 `OuterForm`（`FormBorderStyle.None`, `TopMost=true`），通过动画从高度 0 展开到 `_itemsScrollPanelHeight`。

`CustomComboBox.cs:236-238` 的定位逻辑：

```csharp
Point point = PointToScreen(Point.Empty);
_itemsOuterForm.Location = _itemsOuterForm.PointToClient(
    new(point.X, point.Y + Height));
_itemsOuterForm.Show();
```

下拉框**始终出现在 ComboBox 正下方**，不考虑屏幕剩余空间。当 ComboBox 靠近屏幕底部时，下拉框延伸出屏幕工作区。

"偶尔"出现的原因取决于三个变量：
- 物料码数量 → 决定弹窗高度 → 决定底部 ComboBox 的屏幕位置
- 弹窗在屏幕上的位置（用户可拖动 `CustomPopUpForm`）
- 屏幕分辨率

另一个附带问题：`_itemsOuterForm.PointToClient()` 在 Form 显示前调用，其转换结果依赖于 Form 的默认屏幕位置（由 Windows 决定，通常为 `(0,0)` 但无保证）。直接用屏幕坐标赋值 `Location` 是正确做法。

## 方案

### 选定方案：展开方向翻转

在 `CustomComboBox.cs` 的 select button Click 处理器中，计算 `OuterForm` 的目标位置后，检查是否会超出 `Screen.GetWorkingArea(this)` 的底部边界。如果超出，将 `OuterForm` 定位到 ComboBox 上方。

**修改文件**: `CustomLibrary/ComboBoxes/CustomComboBox.cs`
**修改位置**: 第 236-238 行附近（`_selectButton.Click` 处理器内）

**修改内容**:

```csharp
// Before
Point point = PointToScreen(Point.Empty);
_itemsOuterForm.Location = _itemsOuterForm.PointToClient(
    new(point.X, point.Y + Height));
_itemsOuterForm.Show();

// After
Point point = PointToScreen(Point.Empty);
Point location = new(point.X, point.Y + Height);

// 如果下拉框超出屏幕工作区底部，则向上展开
Rectangle workingArea = Screen.GetWorkingArea(this);
if (location.Y + _itemsScrollPanelHeight > workingArea.Bottom) {
    location.Y = point.Y - _itemsScrollPanelHeight;
}

_itemsOuterForm.Location = location;
_itemsOuterForm.Show();
```

### 为什么这是最简方案

1. **3 行变 7 行** — 最小代码增量
2. **不触及** — 动画逻辑、滚动条逻辑、ItemsScrollPanel 尺寸计算、宽度自适应（`ResetWidthOfItemScrollPanelByItemProperWidth`）
3. **遵循标准 WinForms 模式** — 原生 `ComboBox` 的 `DropDownAlign` 属性就是做这件事
4. **附带修正** — 去掉了未显示 Form 上不可靠的 `PointToClient` 调用，直接用屏幕坐标赋值 `Location`

### 不考虑的方案

- **增大 MaxItemsShown**：治标不治本，物料码多到一定数量仍然会超出屏幕
- **在 BoltPopUpForm_SCII 层面 workaround**：每个使用者都要处理，不如在控件层统一修复
- **重构整个下拉 Popup 机制**：风险高、改动大，不符合"最简单"的要求

## 测试要点

1. 物料码少时（< 8 条）：下拉框正常向下展开，行为不变
2. 物料码多时（> 15 条），弹窗在屏幕上半部分：下拉框向下展开，行为不变
3. 物料码多时（> 15 条），弹窗在屏幕下半部分：下拉框向上展开，所有项可选中
4. 弹窗在屏幕顶部，ComboBox 上方空间不足：向下展开（不处理这种情况，视为极端边缘场景）
