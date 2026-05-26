# SCII XT 打印机图标及操作弹窗 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 SCII XT 工作台底部添加打印机状态图标，点击弹出打印机操作窗口（纯 UI）

**Architecture:** PrinterBlock 复用 DeviceBlock 体系 — 通过 CustomCategories 注入 PRINTER 类别，CheckCustomConnections 刷新状态，重绑 Click 打开 PrinterOperationPopUpForm。弹窗参照 ToolOperationPopUpForm 模式。

**Tech Stack:** C# WinForms, CustomPopUpForm, DeviceBlock/DeviceCategory 模式

---

### Task 1: 创建 PRINTER DeviceCategory

**Files:**
- Modify: `OperationGuidance_new/Constants/DeviceConstants.cs:93`（在 IOBOX_SETTERSELECTOR 类之后添加）

- [ ] **Step 1: 添加 PRINTER DeviceCategory 类和静态字段**

在 `IOBOX_SETTERSELECTOR` 类闭合大括号 `}` 之后、`DeviceTypeBase` 类之前插入：

```csharp
    public class PRINTER: DeviceCategory {
        public PRINTER(): base(99, "打印机",
            Properties.Resources.printer,
            Properties.Resources.printer_error,
            Properties.Resources.printer_empty) {}
    }
```

在 `DeviceCategories` 静态类内部，`IOBOX_SETTERSELECTOR` 那行之后添加静态字段（**不放 Elements 列表**，XT 专属）：

```csharp
        public static DeviceCategory PRINTER = new PRINTER();
```

- [ ] **Step 2: 构建验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 3: 提交**

```bash
git add OperationGuidance_new/Constants/DeviceConstants.cs
git commit -m "feat: add PRINTER DeviceCategory for SCII XT printer icon

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

### Task 2: 创建 PrinterOperationPopUpForm

**Files:**
- Create: `OperationGuidance_new/Views/SubViews/PrinterOperationPopUpForm.cs`

- [ ] **Step 1: 创建弹窗类**

```csharp
using CustomLibrary.Configs;
using CustomLibrary.Forms;
using CustomLibrary.Utils;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Utils.IIPSC;
using OperationGuidance_service.Constants;

namespace OperationGuidance_new.Views.SubViews {
    public class PrinterOperationPopUpForm: CustomPopUpForm {
        private FunctionButton _btnReprintLid;
        private FunctionButton _btnReprintDiverter;

        public PrinterOperationPopUpForm() {
            BorderColor = ColorConfigs.COLOR_POP_UP_BORDER;
            Title = "打印机操作";

            var config = ConfigUtils.LoadConfig<SciiXtPrinterConfig>();

            bool firstEnabled = config.enabled == (int) YesOrNo.YES
                && !string.IsNullOrEmpty(config.printer_name);
            bool secondEnabled = config.enabled_second == (int) YesOrNo.YES
                && !string.IsNullOrEmpty(config.second_printer_name);

            string firstLabel = firstEnabled
                ? $"上盖码重打 — {config.printer_name}"
                : "上盖码重打（未启用）";
            string secondLabel = secondEnabled
                ? $"分流器码重打 — {config.second_printer_name}"
                : "分流器码重打（未启用）";

            _btnReprintLid = AddButton(firstLabel);
            _btnReprintLid.Enabled = firstEnabled;
            // 点击逻辑暂不实现

            _btnReprintDiverter = AddButton(secondLabel);
            _btnReprintDiverter.Enabled = secondEnabled;
            // 点击逻辑暂不实现

            FunctionButton btnClose = AddButton("关闭");
            btnClose.Click += (s, e) => Dispose();
        }
    }
}
```

- [ ] **Step 2: 构建验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 3: 提交**

```bash
git add OperationGuidance_new/Views/SubViews/PrinterOperationPopUpForm.cs
git commit -m "feat: add PrinterOperationPopUpForm for SCII XT printer operations

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

### Task 3: 集成 PrinterBlock 到 SCII XT 工作台

**Files:**
- Modify: `OperationGuidance_new/Views/WorkplaceMissionView_SCII_XT.cs`

- [ ] **Step 1: 添加 using**

在文件顶部 using 区域添加：

```csharp
using OperationGuidance_new.Constants;
using OperationGuidance_new.Views.SubViews;
```

- [ ] **Step 2: 重写 CustomCategories() 返回 PRINTER 类别**

在 `WorkplaceContentPanel_SCII_XT` 类中任意位置添加：

```csharp
        protected override List<DeviceCategory>? CustomCategories() {
            return new() { DeviceCategories.PRINTER };
        }
```

- [ ] **Step 3: 重写 CheckCustomConnections() 处理打印机状态刷新**

在 `WorkplaceContentPanel_SCII_XT` 类中添加：

```csharp
        protected override void CheckCustomConnections(DeviceBlock block, DeviceCategory category) {
            if (category == DeviceCategories.PRINTER) {
                var config = ConfigUtils.LoadConfig<SciiXtPrinterConfig>();
                if (config.enabled == (int) YesOrNo.NO) {
                    block.ResetIconByStatus(DeviceStatus.EMPTY);
                } else if (string.IsNullOrEmpty(config.printer_name)) {
                    block.ResetIconByStatus(DeviceStatus.ERROR);
                } else {
                    block.ResetIconByStatus(DeviceStatus.NORMAL);
                }
            }
        }
```

- [ ] **Step 4: 在 InitializeAfterHandelCreated 中重绑打印机图标的 Click**

在 `InitializeAfterHandelCreated()` 方法末尾（`SwitchMissionByRecipe` 调用之后）添加：

```csharp
            // 绑定打印机图标点击 → 弹出 PrinterOperationPopUpForm
            DeviceBlock? printerBlock = _deviceBlocks.Find(b => b.Category == DeviceCategories.PRINTER);
            if (printerBlock != null) {
                printerBlock.Click -= (s, e) => { }; // 清除基类绑定的空操作
                printerBlock.Click += (s, e) => {
                    if (printerBlock.PopUpForm == null || printerBlock.PopUpForm.IsDisposed) {
                        var popUpForm = new PrinterOperationPopUpForm();
                        printerBlock.PopUpForm = popUpForm;
                        popUpForm.PretendToShowToCreateHandlesForChildren();
                        popUpForm.ResizeChildren();
                        popUpForm.Show();
                    }
                };
            }
```

注意：`_deviceBlocks` 是 `AWorkplaceContentPanel` 中的 `protected` 字段，XT 类可直接访问。

- [ ] **Step 5: 构建验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 6: 提交**

```bash
git add OperationGuidance_new/Views/WorkplaceMissionView_SCII_XT.cs
git commit -m "feat: integrate PrinterBlock icon and popup into SCII XT workplace

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

### 验证清单

构建成功后，手动验证：

1. 启动应用，进入 SCII XT 工作台
2. 确认底部出现打印机图标（位于其他设备图标右侧、时钟左侧）
3. 修改 `SciiXtPrinterConfig` 中的 `enabled` 为 0 → 图标变灰（EMPTY）
4. 设置 `enabled` 为 1，清空 `printer_name` → 图标变红（ERROR）
5. 设置 `enabled` 为 1，填入 `printer_name` → 图标变绿（NORMAL）
6. 点击正常状态图标 → 弹出"打印机操作"窗口
7. 弹窗中确认按钮文案、启用/禁用状态与配置一致
