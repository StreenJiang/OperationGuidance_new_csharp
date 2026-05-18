# SCII XT — 物料码上传改造 & 点检去MES化 & 料号默认值

## 背景

1. 物料码上传从上盖绑定改为通用配件上传
2. 点检任务不需要与 MES 交互
3. 物料规则中料号默认填入"1"

## 改动

### 1. 料号默认值 "1"

**文件：** `BarCodeMatchingRuleManagementView_SCII_XT.cs`

新建规则时（`dto.id <= 0`），`partNo` 文本框默认填 `"1"`。

### 2. CheckCanActivateMission — BindAccessory 替代上盖

**文件：** `BarCodeInputPopUpForm_SCII_XT.cs`

移除 `send_upper_cover` 配置的 `BindUppderCover` 调用。替换为：将所有已录入物料码通过 `BindAccessory` 一次性上传。

`Accessory` 字段映射：

| 字段 | 来源 |
|---|---|
| `accessoryCode` | 物料条码值 |
| `accessoryType` | 条码匹配规则 `name` |
| `partNo` | 条码匹配规则 `part_no` |
| `orderId` | 物料序号（规则按 id 升序排列） |

组装 `SCII_XT_BindAccessoryReq`，`procedureCode` / `recipeCode` / `employeeNumber` 等从现有 config 和 workplace 获取。调用失败弹提示并返回 `false`。

第二打印机逻辑不变。

### 3. 点检去掉 MES 交互

**文件：** `WorkplaceMissionView_SCII_XT.cs`

- `TerminateMission` 点检分支：删除 `SendCheckToMES`，保留 `_inBoundStationOk = false`，然后 `base.TerminateMission`
- `SendCheckToMES` 方法删除
- `ProductBarCodeExtraCheck` 点检逻辑不变
