# SCII XT 打印机图标及操作弹窗

为 SCII XT 版本工作台底部添加打印机状态图标和打印机操作弹窗。

## 新组件

### PrinterBlock（底部图标）

- 继承 `DeviceBlock`，复用 `ResizeBottom()` 中 `control is DeviceBlock` 的自动方形 sizing
- 通过 `CustomCategories()` 注入到 DeviceBlock 体系
- 点击打开 `PrinterOperationPopUpForm`

**状态判定**（读取 `SciiXtPrinterConfig`）：

| 条件 | 状态 |
|---|---|
| `enabled == NO` | EMPTY（灰显） |
| `enabled == YES` 且 `string.IsNullOrEmpty(printer_name)` | ERROR（红色） |
| `enabled == YES` 且 `printer_name` 已配置 | NORMAL（绿色） |

**刷新策略**：复用 `CheckDeviceConnections` 2 秒轮询。XT 重写 `CheckCustomConnections(block, category)`，当 category 为 PRINTER 时执行状态刷新。不新建独立定时器。

### PrinterOperationPopUpForm（操作弹窗）

- 继承 `CustomPopUpForm`
- Title: `"打印机操作"`
- 布局参照 `ToolOperationPopUpForm`：`TableLayoutPanel` + `AddButton()`
- 两个操作按钮 + 关闭按钮

**按钮规则**：

| 按钮 | 文案 | 启用条件 |
|---|---|---|
| 上盖码重打 | `"上盖码重打 — {printer_name}"` | `enabled == YES` 且 `printer_name` 非空 |
| 分流器码重打 | `"第二台 — {second_printer_name}"` | `enabled_second == YES` 且 `second_printer_name` 非空 |

按钮文案中的打印机名称从 `SciiXtPrinterConfig` 实时读取。

**按钮点击逻辑**：本期不做，按钮点击后暂不绑定具体逻辑，仅放置占位。

## 涉及文件

| 文件 | 改动 |
|---|---|
| `Views/SubViews/PrinterBlock.cs` | **新建** |
| `Views/SubViews/PrinterOperationPopUpForm.cs` | **新建** |
| `Constants/DeviceConstants.cs` | 添加 PRINTER DeviceCategory |
| `Views/WorkplaceMissionView_SCII_XT.cs` | 重写 `CustomCategories()`、`CheckCustomConnections()`，在 `InitializeAfterHandelCreated()` 中绑定 PrinterBlock 的 Click |

## 层级关系

```
WorkplaceContentPanel_SCII_XT
├── CustomCategories() → 返回 [DeviceCategories.PRINTER]
├── CheckCustomConnections() → 处理 PRINTER 状态刷新
├── InitializeAfterHandelCreated()
│   └── 找到 PRINTER 对应的 DeviceBlock，绑定 Click → new PrinterOperationPopUpForm()
└── _bottom
    └── PrinterBlock (DeviceBlock) ← 图标
```

## 边缘情况

- **弹窗打开期间配置变更**：按钮状态不实时刷新，以弹窗打开时的配置快照为准
- **printer_name 为空字符串**：视为未配置（等同于 null）
- **弹窗已打开时重复点击图标**：不重复创建，检查 `IsDisposed` 后复用
