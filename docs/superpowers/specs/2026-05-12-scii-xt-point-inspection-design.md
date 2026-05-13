# SCII_XT 点检任务改造设计

## 概述

将 SCII_XT 版本的"挑战任务"功能替换为"点检任务"。复用 `is_challenge_mission` 数据库字段，在 UI 层面展示为"是否是点检任务"，运行时任务结束时调用 MES `/check` 接口替代 `BindProductData`。挑战任务相关字段（`challenge_mission_id`、`is_first_mission`、`predecessor_mission_id`、`predecessor_part_mission_ids`）在 SCII_XT 下全部废弃。

仅影响 SCII_XT 产品线，SCII / GLB / WHYC / TZYX 不受影响。

## 新增文件

### 1. HTTP 请求 DTO — `EquipmentCheckReq`

**路径**: `OperationGuidance_new/HttpObjects/Requests/SCII_XT/EquipmentCheckReq.cs`

继承 `HttpRequestBase_SCII_XT`，字段：

| 字段 | 类型 | 来源 |
|---|---|---|
| `equipmentCheckInfos` | `List<EquipmentCheckInfo>` | 和 BindProductData 的 `productInfos` 构建逻辑一致 |
| `employeeNumber` | `string` | `SystemUtils.UserInfo.account` |
| `equipmentCode` | `string` | `SciiXtConfig.equipment_code` |

`EquipmentCheckInfo` 包含 `List<Attribute> attributeList`，`Attribute` 结构复用 `SCII_XT_BindProductDataReq.ProductInfo.Attribute`（attributeName / attributeCode / attributeUnit / attributeType / orderId / value）。

### 2. HTTP 响应 DTO — `EquipmentCheckDTO`

**路径**: `OperationGuidance_service/Models/DTOs/EquipmentCheckDTO.cs`

| 字段 | 类型 |
|---|---|
| `checkSuccess` | `bool` |
| `message` | `string` |

### 3. 数据查询视图 — `DataQueryView_SCII_XT`

**路径**: `OperationGuidance_new/Views/DataQueryView_SCII_XT.cs`

继承 `DataQueryView_SCII`，将"是否挑战任务"筛选标签改为"是否点检任务"。其余逻辑不变。

## 修改文件

### 4. `Workflow_SCII_XT.cs`

新增静态方法 `EquipmentCheck(EquipmentCheckReq req)`：
- URL: `{RequestPrefix}/api/check`
- HTTP 方法: POST
- 请求/响应类型: `EquipmentCheckReq` / `SCII_XT_Response`
- 成功条件: `rsp.code == (int) SCII_XT_ResponseCode.OK`

### 5. `MissionEditionView_SCII.cs` — 提取 virtual 方法

在 `MissionDetailPopUpForm` 中将挑战任务控件的初始化提取为 `protected virtual` 方法：
- `InitChallengeControls()` — 创建 `IsChallengeMission`、`IsFirstMission`、`ChallengMission`、`PredecessorMission`、`PredecessorPartMissionMaps` 等控件
- SCII 保留原逻辑，SCII_XT 重写为只创建"是否点检任务" ToggleButton（复用 `IsChallengeMission` 字段）

`AfterShown` 中回填逻辑同样提取为 `protected virtual FillChallengeFields()`。

### 6. `MissionEditionView_SCII_XT.cs` — 弹窗与校验改造

#### `MissionDetailPopUpForm_SCII_XT`

- 重写 `InitChallengeControls()`：只创建"是否点检任务" ToggleButton，标签改为"是否是点检任务"
- 重写 `FillChallengeFields()`：只回填 `_isChallengeMission.Checked`
- `InitScrewCounterBoxes`、`AddScrewBitCounter`、`AfterShown` 沿用现有重写

#### `MissionEditionPage_SCII_XT.OpenMissionDetailPopUp` 校验逻辑

删除以下校验：
- 挑战任务必须对应普通任务（102-106 行）
- 对应普通任务已存在挑战任务（112-117 行）
- 首道岗位时前置任务检查（120-128 行）
- `IsFirstMission` / `PredecessorMission` / `PredecessorPartMissionMaps` 相关校验（132-206 行中挑战相关部分）

保留的校验：任务名非空、不重复、最大 NG 数、密码次数、套筒位。

#### 保存逻辑

- `_missionDTO.is_challenge_mission` → 存为"是否点检任务"的值
- `_missionDTO.challenge_mission_id` → `null`
- `_missionDTO.is_first_mission` → `null`
- `_missionDTO.predecessor_mission_id` → `null`
- `_missionDTO.predecessor_part_mission_ids` → `null`

### 7. `AWorkplaceContentPanel.cs` — 挑战任务逻辑提取为 virtual

将以下方法/代码块提取为 `protected virtual`：

| 内容 | 提取为 |
|---|---|
| `CheckChallengeMissionConfirmation()` (L1147-1196) | `protected virtual CheckChallengeMissionConfirmation()`，基类返回 `true` |
| `ChallengeChecks()` (L1198-1233) | `protected virtual ChallengeChecks()` |
| `AddChallengeResult()` (L1235-1247) | `protected virtual AddChallengeResult()`，基类实现为空 |
| MISSION_OK 检查 (L2502-2504) | `protected virtual OnMissionComplete()` |
| 挑战任务自动激活跳过 (L2872-2873) | 保留在基类，判断改为调用 `virtual bool IsChallengeMission()` |

SCII (`WorkplaceContentPanel_SCII`) 重写这些方法保留原挑战任务逻辑。SCII_XT 重写为空/no-op。

### 8. `ABarCodeInputPopUpForm.cs` — 挑战检查提取为 virtual

将挑战任务相关检查点提取为 `protected virtual` 方法：

| 位置 | 提取为 |
|---|---|
| PRODUCT_BAR_CODE_ERROR (L246-248) | `protected virtual CheckProductBarCodeErrorForChallenge()` |
| PRODUCT_BAR_CODE_ERROR (L280-283) | 同上 |
| PRODUCT_PREDECESSOR (L319-330) | `protected virtual CheckProductPredecessorForChallenge()` |
| PRODUCT_BAR_CODE_REDO (L346-349) | `protected virtual CheckProductBarCodeRedoForChallenge()` |
| PARTS_BAR_CODE_ERROR (L450-453) | `protected virtual CheckPartsBarCodeErrorForChallenge()` |
| PARTS_PREDECESSOR (L476-487) | `protected virtual CheckPartsPredecessorForChallenge()` |
| PARTS_BAR_CODE_REDO (L509-518) | `protected virtual CheckPartsBarCodeRedoForChallenge()` |

基类中这些方法默认为空实现。`BarCodeInputPopUpForm_SCII` 重写保留原逻辑。`BarCodeInputPopUpForm_SCII_XT` 无需重写（继承基类空实现即可）。

### 9. `WorkplaceMissionView_SCII_XT.cs` — 工作台运行时

#### `TerminateMission()` 修改

```
if (_mission.is_challenge_mission == YesOrNo.YES)  // 点检任务
    await SendCheckToMES(_operationDataDTOs);       // 调 /check
else
    await SendDataToMES(_operationDataDTOs);        // 调 /api/product-data/bind（原逻辑）

// 点检任务跳过 OutBound
if (_mission.is_challenge_mission != YesOrNo.YES)
    await OutBound();

await base.TerminateMission(status);
```

#### `SendCheckToMES()` 新方法

构建 `EquipmentCheckReq`，`equipmentCheckInfos[].attributeList` 的数据构建完全复用 `SendDataToMES` 的反射逻辑（取 `OperationDataDTO_SCII_XT` 上 `[SCII_XT_Column]` 属性，序列化为 JSON）。

#### `SwitchMissionByRecipe()` 修改

```
if (_mission != null && _mission.is_challenge_mission == YesOrNo.YES)
    return;  // 点检任务不被配方强制切换
```

#### `ProductBarCodeExtraCheck()` 修改

```
if (_mission.is_challenge_mission == YesOrNo.YES)
    return true;  // 点检任务跳过进站
// 原进站逻辑
```

#### 挑战任务 virtual 方法重写

| 方法 | SCII_XT 实现 |
|---|---|
| `CheckChallengeMissionConfirmation()` | 返回 `true` |
| `ChallengeChecks()` | 返回 `true` |
| `AddChallengeResult()` | 空 |
| `OnMissionComplete()` | 空 |

### 10. `MissionEditionView_SCII.cs` — SCII 保留挑战任务逻辑

在 `WorkplaceContentPanel_SCII` 中重写基类的 virtual 方法，将原来从 `AWorkplaceContentPanel` 挪出的挑战任务逻辑放回此处。

### 11. `BarCodeInputPopUpForm_SCII.cs`

重写 `ABarCodeInputPopUpForm` 的 7 个 `protected virtual` 挑战检查方法，保留原逻辑。

SCII_XT 已有 `BarCodeInputPopUpForm_SCII_XT` 继承 `BarCodeInputPopUpForm_SCII`，但由于基类方法已是空实现，且 SCII 的挑战逻辑不再需要被 XT 继承，需调整继承链：`BarCodeInputPopUpForm_SCII_XT` 改为直接继承 `ABarCodeInputPopUpForm`，避免执行 SCII 的挑战检查。

## 注意事项

- **GLB / WHYC / TZYX 无影响**：挑战任务逻辑仅 SCII 和 SCII_XT 存在。基类 virtual 方法默认空实现，SCII 重写保留原逻辑，SCII_XT 及 GLB/WHYC/TZYX 继承空实现即可。
- **`BarCodeInputPopUpForm_SCII_XT` 继承链**：改为直接继承 `ABarCodeInputPopUpForm` 后，需确保 SCII_XT 原有的非挑战功能（`OnHandleCreated` 中的工序/设备编码校验、`CheckCanActivateMission` 的上盖绑定逻辑）不丢失。当前这些方法已在 SCII_XT 中完整重写，不受影响。
- **点检任务与普通任务切换**：点检任务不会被配方强制切换，但用户可以手动从任务列表选择切换。选中的若是普通任务，配方自动切换恢复正常行为。

## 不变项

- `ChallengeTaskEnum`、`ChallengeTask`、`ChallengeTaskUtil` — 保留，SCII/GLB/WHYC/TZYX 继续使用
- 数据库迁移脚本 — 不修改，`is_challenge_mission` 字段复用，不新增列
- `ProductMission` / `ProductMissionDTO` 模型 — 不修改字段定义

## API 接口规格

### POST `/api/check`（MES 端提供）

**请求体**:
```json
{
  "equipmentCheckInfos": [
    {
      "attributeList": [
        {
          "attributeName": "string",
          "attributeCode": "string",
          "attributeUnit": "string",
          "attributeType": 0,
          "orderId": 0,
          "value": "string"
        }
      ]
    }
  ],
  "employeeNumber": "string",
  "equipmentCode": "string"
}
```

**响应体**:
```json
{
  "code": 200,
  "dataInfo": null,
  "message": "string"
}
```

- `code == 200` 为成功
