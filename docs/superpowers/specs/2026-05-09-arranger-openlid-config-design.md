# 螺丝机开盖配置优化 — 增加螺丝机组选择

## 概述

在 SCII_XT 变量设置页的"螺丝机开盖配置"中，每行新增一个 Combobox 用于选择螺丝机组（type=arranger 的 IO Box）。同时调整工作台螺丝机弹窗，使用配置中的 arranger_id 来指定对应的螺丝机组。

## 数据模型变更

### `ArrangerGroupDTO` (`Configs/DTOs/ArrangerGroupDTO.cs`)

新增字段：

```csharp
public int? arranger_id { get; set; }
```

### `SciiXtArrangerConfig`

无需变更。JSON 序列化自动处理新增字段。

## 变量设置页 (`VariableSettingsView_SCII_XT.cs`)

### 每行结构变更

当前：`CustomTextBoxButtonGroup`（名称、条码、点位 三个文本框 + +/- 按钮）  
改为：每行一个 Panel 容器，内含：

- `CustomTextBoxButtonGroup`：名称、条码、点位 三个文本框 + +/- 按钮
- `CustomComboBoxGroup<DeviceIoDTO>`：螺丝机组选择，数据源通过 `apis.QueryDeviceIoList()` 获取，过滤 `type == DeviceType_IoBox.Arranger.Id`

### 布局

```
┌─ Panel (一行) ──────────────────────────────────────────┐
│ 开盖组1  [名称] → [条码] → [点位] → [螺丝机组 ▼]  [+] │
└──────────────────────────────────────────────────────────┘
```

### 加载逻辑

1. 调用 `apis.QueryDeviceIoList()` 获取设备列表
2. 过滤 `type == Arranger` 的设备，填充 Combobox
3. 根据配置中的 `arranger_id` 查找对应设备：
   - 找到 → 选中对应项
   - 找不到 → `SetError(true)` 标红 + `WidgetUtils.ShowWarningPopUp("螺丝机开盖配置：螺丝机组「{name}」找不到指定设备，可能已被删除")`

### 校验（CheckBeforeSave）

在现有校验基础上新增：

1. **任一有值→全必填**：若4个字段（name, barcode, position, arranger_id）任一非空，则4个都必须非空
2. **同螺丝机组点位唯一**：遍历所有组，相同 `arranger_id` 的组的 `position` 不能重复
   - 保存时校验，阻止保存并弹出警告

### 未保存变更检测（CheckSavedFunc_detail）

新增对 arranger config 的变更检测（已有 `GetCurrentArrangerGroupsJson()` 的对比逻辑，需确保包含 `arranger_id`）。

## 工作台螺丝机弹窗 (`ArrangerOperationPopUpForm`)

### 构造函数变更

**当前签名**：
```csharp
ArrangerOperationPopUpForm(string categoryName, AWorkplaceContentPanel workplace, IoBoxTask ioBoxTask)
```

**改为**：
```csharp
ArrangerOperationPopUpForm(string categoryName, AWorkplaceContentPanel workplace, 
    Dictionary<string, IoBoxTask> ioBoxTasks, List<DeviceIoDTO> deviceIoDTOs)
```

### 每组按钮逻辑

对每个 ArrangerGroupDTO：

1. 根据 `group.arranger_id` 在 `deviceIoDTOs` 中查找 `DeviceIoDTO`
2. 若找到 → 通过 `GetTCPClientKey(dto.ip, dto.port)` 在 `ioBoxTasks` 中查找 `IoBoxTask`
3. 若 IoBoxTask 找到且 `ArrangerType != null` → 按钮正常，点击后打开 `ArrangerOpenLidScanPopUpForm`
4. 若任一步找不到 → 按钮置灰 + 标红，显示"设备已删除"或类似提示

### UI 表现

- 正常组：`组名称  [Arranger名称]  [点击开盖]`
- 异常组：`组名称  [设备已删除·红色]  [不可用·灰色]`

### 调用方变更 (`AWorkplaceContentPanel`)

当前逻辑（`DeviceCategories.IOBOX_ARRANGER` 分支）：
- 找第一个 `ArrangerType != null` 的 IoBoxTask
- 传递给 ArrangerOperationPopUpForm

改为：
- 传递 `_ioBoxTasks` 字典 + `DeviceIoDTOs` 列表
- 若没有任何 arranger 设备 → 依然弹出"没有配置螺丝机"

## `ArrangerOpenLidScanPopUpForm`

无需改动。该窗体仍然接收 `IoBoxTask`，由调用方确定传入正确的 task。

## 涉及文件清单

| 文件 | 改动类型 |
|------|----------|
| `Configs/DTOs/ArrangerGroupDTO.cs` | 新增字段 |
| `Views/VariableSettingsView_SCII_XT.cs` | 新增 arranger combobox、校验、加载逻辑 |
| `Views/SubViews/ArrangerOperationPopUpForm.cs` | 构造函数重构、每组独立查找 IoBoxTask |
| `Views/AbstractViews/AWorkplaceContentPanel.cs` | 调用 ArrangerOperationPopUpForm 时传递新参数 |

## 边界处理

- `arranger_id` 为 null（配置中没有选择螺丝机组）：视为未完成的行，按现有规则处理（全空则跳过/忽略）
- 工作台弹窗打开时所有组的 arranger 都不存在：弹窗正常打开但所有开盖按钮均置灰；IO 点位测试按钮不受影响
- 配置 JSON 向后兼容：旧配置没有 `arranger_id` 字段，反序列化时为 null，等同于未选择
