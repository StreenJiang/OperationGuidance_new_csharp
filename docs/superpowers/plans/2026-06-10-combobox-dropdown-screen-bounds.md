# ComboBox 下拉框屏幕边界自适应 — 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复 `CustomComboBox<T>` 下拉框在屏幕底部被截断的问题——超出屏幕工作区时自动向上展开。

**Architecture:** 在 `CustomComboBox.cs` 的 `_selectButton.Click` 处理器中，`OuterForm` 定位逻辑增加屏幕边界检查：计算目标位置 + 下拉高度是否超过 `Screen.GetWorkingArea()` 底部，超出则翻转到 ComboBox 上方。

**Tech Stack:** C# / WinForms / .NET 6

---

### Task 1: 修改 OuterForm 定位逻辑，增加屏幕边界检查

**Files:**
- Modify: `CustomLibrary/ComboBoxes/CustomComboBox.cs:236-238`

- [ ] **Step 1: 替换定位代码**

将 `CustomLibrary/ComboBoxes/CustomComboBox.cs` 第 236-238 行：

```csharp
                        Point point = PointToScreen(Point.Empty);
                        _itemsOuterForm.Location = _itemsOuterForm.PointToClient(new(point.X, point.Y + Height));
                        _itemsOuterForm.Show();
```

替换为：

```csharp
                        Point point = PointToScreen(Point.Empty);
                        Point location = new(point.X, point.Y + Height);

                        // 下拉框超出屏幕工作区底部时，改为向上展开
                        Rectangle workingArea = Screen.GetWorkingArea(this);
                        if (location.Y + _itemsScrollPanelHeight > workingArea.Bottom) {
                            location.Y = point.Y - _itemsScrollPanelHeight;
                        }

                        _itemsOuterForm.Location = location;
                        _itemsOuterForm.Show();
```

- [ ] **Step 2: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

预期: Build succeeded.

- [ ] **Step 3: 代码自审**

检查要点：
- `_itemsScrollPanelHeight` 在 Click 触发时已被 `ResizeChildren()`（`OnHandleCreated` 时注册）和 `AddItem()` 更新，值是最新的
- `Screen.GetWorkingArea(this)` 返回当前控件所在屏幕的工作区（排除任务栏），`this` 是 `CustomComboBox<T>` 控件，其 Handle 已创建
- 去除了 `_itemsOuterForm.PointToClient()` 的不可靠调用——`Form.Location` 直接接收屏幕坐标
- 动画逻辑（`_collapseTimer`）不受影响——它仅操作 `OuterForm.Height`，不依赖 `Location`

- [ ] **Step 4: 功能验证（手动测试）**

启动应用，进入点位编辑弹窗（`BoltPopUpForm_SCII`），绑定多条物料码（10+ 条）：
1. 拖动弹窗到**屏幕上半部分** → 点击底部 ComboBox → 下拉框向下展开，所有项可见 ✓
2. 拖动弹窗到**屏幕下半部分** → 点击底部 ComboBox → 下拉框向上展开，所有项可见 ✓
3. 物料码较少（< 8 条）→ 下拉框正常展开，行为不变 ✓
4. 多次点击同一 ComboBox 开合 → 下拉框每次正确定位 ✓

- [ ] **Step 5: 提交**

```bash
git add CustomLibrary/ComboBoxes/CustomComboBox.cs
git commit -m "fix(combobox): flip dropdown upward when it extends past screen bottom

OuterForm now checks Screen.GetWorkingArea before positioning — if the
dropdown would render below the working area, it opens above the ComboBox
instead. Fixes material-code dropdown truncation in BoltPopUpForm_SCII."

```

---

## 自审

1. **Spec 覆盖**: 唯一改动点（`CustomComboBox.cs:236-238`）与 spec 完全对应，屏幕边界检查逻辑一致
2. **占位符扫描**: 无 TBD/TODO/占位符
3. **类型一致性**: 无跨任务引用，单一改动点，`Point`、`Rectangle`、`Screen` 均为 .NET 标准类型
