# 任务保存名称重复校验、条码规则名称去重、GridView滚动条修复

**日期：** 2026-06-11
**状态：** 已确认

---

## 需求1：任务保存时名称重复校验

### 背景

当前任务保存（详情弹窗"确定"和外层"保存"按钮）不检查名称是否与其他未删除任务重复，可能导致同名任务混淆。

### 需求

保存任务时，查询整个数据库中 `deleted = 2`（即未删除）的任务，如果名称重复则阻止保存。需要在两个地方校验：

1. **详情弹窗"确定"按钮** — `MissionEditionView.cs:224`、`MissionEditionView_SCII.cs:228`
2. **外层"保存"按钮** — `MissionEditionView.cs:288`、`MissionEditionView_SCII.cs:478`

### 设计

#### 校验规则

- 调用已有 API `QueryProductMissions`，传入当前用户的 `MacsId` 和 `Role`，与现有代码行为一致：
  - 非 admin（OPERATOR）：按 `macs_id` 过滤 → 工位内名称唯一
  - admin / DEVELOPER：不过滤 `macs_id` → 全局名称唯一
- 名称比对使用 C# 默认 `==`（大小写敏感，`"A" != "a"`）
- 排除当前编辑的任务自身（`m.id != _missionDTO.id`）
- 编辑已有任务但未改名称时，自身被排除，不会触发重复

#### 详情弹窗"确定"按钮

在现有 `missionName` 非空校验之后、其他校验之前，加入名称重复检查：

```csharp
// 名称重复校验
string missionName = _detialPopUpForm.MissionName.GetTextBox(0).Box.Text;
List<ProductMissionDTO> allMissions = _apis.QueryProductMissions(new(SystemUtils.MacAddressesDTO.id) { Role = SystemUtils.GetRoleNameByUserId(SystemUtils.LoggedUserId) }).ProductMissionsDTOs;
if (allMissions.Any(m => m.name == missionName && m.id != _missionDTO.id)) {
    check = false;
    _detialPopUpForm.MissionName.GetTextBox(0).IsError = true;
    warningMsg += $"{warningIndex++}. 任务名称已存在，请修改\r\n";
}
```

**位置：** 紧接在 `if (string.IsNullOrEmpty(missionName))` 校验块之后。

**涉及文件：**
- `OperationGuidance_new/Views/MissionEditionView.cs` → `InitializeTop` → detail popup "确定" Click handler
- `OperationGuidance_new/Views/MissionEditionView_SCII.cs` → `InitializeTop` → detail popup "确定" Click handler

#### 外层"保存"按钮

在 `_currentProductImageFile.SaveSideInfo()` 之后、`apis.AddOrUpdateProductMission` 之前，加入名称重复检查：

```csharp
// 名称重复校验
List<ProductMissionDTO> allMissions = _apis.QueryProductMissions(new(SystemUtils.MacAddressesDTO.id) { Role = SystemUtils.GetRoleNameByUserId(SystemUtils.LoggedUserId) }).ProductMissionsDTOs;
if (allMissions.Any(m => m.name == _missionDTO.name && m.id != _missionDTO.id)) {
    WidgetUtils.ShowWarningPopUp("任务名称已存在，请修改后再保存");
    return;
}
```

**涉及文件：**
- `OperationGuidance_new/Views/MissionEditionView.cs` → `_buttonSave.Click` handler
- `OperationGuidance_new/Views/MissionEditionView_SCII.cs` → `_buttonSave.Click` handler

#### 服务端校验

在 `AddOrUpdateProductMission` API 中增加名称重复检查，作为最后一道防线。按 `macs_id` + `name` + `deleted = 2` 查询，排除自身 ID：

```csharp
// 在 ObjectConverter 之后、InsertOrUpdate 之前
string checkSql = $"select id from {_productMissionService.TableName} " +
    "where deleted = @deleted and macs_id = @macs_id and name = @name and id != @id";
List<ProductMission> existing = _productMissionService.FindBySql(checkSql, new Dictionary<string, object> {
    {"deleted", (int)YesOrNo.NO},
    {"macs_id", mission.macs_id},
    {"name", mission.name},
    {"id", mission.id}
});
if (existing.Count > 0) {
    rsp.RsponseCode = HttpResponseCode.ERROR;
    rsp.RsponseMessage = "任务名称已存在，请修改";
    transaction.Rollback();
    return rsp;
}
```

**涉及文件：** `OperationGuidance_service/Controllers/OperationGuidanceApis.cs` → `AddOrUpdateProductMission`（第471行）

---

## 需求2：条码规则管理界面任务名称去重

### 背景

`RefreshMissionOptions` 直接将所有任务填入过滤条件下拉框，当存在同名任务时出现重复条目。

### 需求

对下拉框中的任务名称去重。同名但不同 ID 的任务，用 `"name (id)"` 格式保留。

### 设计

抽取共用方法 `PopulateMissionComboBox`，在三个填充点复用：

```csharp
/// <summary>
/// 将 _missions 按名称去重填充到下拉框。同名不同 ID 的用 "name (id)" 格式保留。
/// </summary>
protected void PopulateMissionComboBox<T>(CustomComboBoxGroup<T> comboBox) {
    var groups = _missions.GroupBy(m => m.name);
    foreach (var group in groups) {
        if (group.Count() == 1) {
            var m = group.First();
            comboBox.AddItem(m.name, m.id);
        } else {
            foreach (var m in group) {
                comboBox.AddItem($"{m.name} ({m.id})", m.id);
            }
        }
    }
}
```

三处调用点改为：

```csharp
// RefreshMissionOptions（第92-98行）
protected void RefreshMissionOptions() {
    _missions = apis.QueryProductMissions(...).ProductMissionsDTOs;
    _missionNameComboBox.ClearItem();
    PopulateMissionComboBox(_missionNameComboBox);
}

// OpenEditEntityPopUpForm 基类（第114-118行）
// 将 foreach (ProductMissionDTO mission in _missions) { ... }
// 替换为：
PopulateMissionComboBox(missionName);

// BarCodeMatchingRuleManagementView_SCII.OpenEditEntityPopUpForm 覆盖（第26-30行）
// 同样替换为：
PopulateMissionComboBox(missionName);
```

**涉及文件：**
- `OperationGuidance_new/Views/AbstractViews/ABarCodeMatchingRuleManagementView.cs` — 新增 `PopulateMissionComboBox` + 修改 `RefreshMissionOptions` 和 `OpenEditEntityPopUpForm`
- `OperationGuidance_new/Views/BarCodeMatchingRuleManagementView_SCII.cs` — 修改 `OpenEditEntityPopUpForm` 覆盖

---

## 需求3：GridView 滚动条无法滚动到底部

### 背景

`DataGridViewPanel.ResizeChildren` 中滚动条 Maximum 设为 `RowCount - 1`，加上 `LargeChange = DisplayedRowCount(true)`，用户可滚动到的最大行为 `RowCount - DisplayedRowCount`，导致最后一行数据被截断不可见。

### 需求

确保所有数据行都能滚动到可见区域，底部可以适当留白。

### 设计

在 `Maximum` 上加一个 `DisplayedRowCount` 的余量：

```csharp
// 改前：
_vScrollBar.Maximum = Math.Max(0, _gridView.RowCount - 1);
// 改后：
_vScrollBar.Maximum = Math.Max(0, _gridView.RowCount - 1 + _gridView.DisplayedRowCount(true));
```

`ValueChanged` handler 中已有 `Math.Clamp(rowIndex, 0, RowCount - 1)` 保护，不会越界。滚动到底后下方自然留白。

**涉及文件：** `OperationGuidance_new/Views/ReusableWidgets/DataGridViewPanel.cs`，第791行。

---

## 影响范围总结

| 文件 | 变更 |
|---|---|
| `Views/MissionEditionView.cs` | 详情弹窗"确定" + "保存"按钮 → 加名称重复校验 |
| `Views/MissionEditionView_SCII.cs` | 详情弹窗"确定" + "保存"按钮 → 加名称重复校验 |
| `Controllers/OperationGuidanceApis.cs` | `AddOrUpdateProductMission` → 服务端名称重复校验 |
| `Views/AbstractViews/ABarCodeMatchingRuleManagementView.cs` | 新增 `PopulateMissionComboBox` + `RefreshMissionOptions`/`OpenEditEntityPopUpForm` 调用之 |
| `Views/BarCodeMatchingRuleManagementView_SCII.cs` | `OpenEditEntityPopUpForm` 覆盖 → 调用 `PopulateMissionComboBox` |
| `Views/ReusableWidgets/DataGridViewPanel.cs` | `ResizeChildren` → 滚动条 Maximum 加余量 |
