# SCII_XT 点检任务改造实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 SCII_XT 版本的"挑战任务"替换为"点检任务"，复用 `is_challenge_mission` 字段，新增 MES `/check` 接口。

**Architecture:** SCII_XT 继承 SCII 的 MissionDetailPopUpForm，通过重写 virtual 方法精简弹窗控件；基类 AWorkplaceContentPanel/ABarCodeInputPopUpForm 的挑战逻辑提取为 virtual 空实现，SCII 重写保留原逻辑，SCII_XT 继承空实现。

**Tech Stack:** C# .NET 6.0 WinForms, Newtonsoft.Json, HttpClient

---

## 文件清单

| 操作 | 文件 |
|------|------|
| **Create** | `OperationGuidance_new/HttpObjects/Requests/SCII_XT/EquipmentCheckReq.cs` |
| **Create** | `OperationGuidance_service/Models/DTOs/EquipmentCheckDTO.cs` |
| **Create** | `OperationGuidance_new/Views/DataQueryView_SCII_XT.cs` |
| **Modify** | `OperationGuidance_new/Utils/Workflow_SCII_XT.cs` |
| **Modify** | `OperationGuidance_new/Views/MissionEditionView_SCII.cs` |
| **Modify** | `OperationGuidance_new/Views/MissionEditionView_SCII_XT.cs` |
| **Modify** | `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs` |
| **Modify** | `OperationGuidance_new/Views/AbstractViews/ABarCodeInputPopUpForm.cs` |
| **Modify** | `OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs` |
| **Modify** | `OperationGuidance_new/Views/ReusableWidgets/BarCodeInputPopUpForm_SCII.cs` |
| **Modify** | `OperationGuidance_new/Views/ReusableWidgets/BarCodeInputPopUpForm_SCII_XT.cs` |
| **Modify** | `OperationGuidance_new/Views/WorkplaceMissionView_SCII_XT.cs` |

---

### Task 1: 创建 `/check` 请求 DTO

**Files:**
- Create: `OperationGuidance_new/HttpObjects/Requests/SCII_XT/EquipmentCheckReq.cs`

- [ ] **Step 1: 编写 EquipmentCheckReq 类**

```csharp
using OperationGuidance_new.HttpObjects.AbstractClasses;

namespace OperationGuidance_new.HttpObjects.Requests.SCII_XT {
    public class EquipmentCheckReq : HttpRequestBase_SCII_XT {
        public List<EquipmentCheckInfo> equipmentCheckInfos { get; set; } = new();
        public string employeeNumber { get; set; } = string.Empty;
        public string equipmentCode { get; set; } = string.Empty;

        public class EquipmentCheckInfo {
            public List<Attribute> attributeList { get; set; } = new();
        }

        public class Attribute {
            public string attributeName { get; set; } = string.Empty;
            public string attributeCode { get; set; } = string.Empty;
            public string attributeUnit { get; set; } = string.Empty;
            public int attributeType { get; set; }
            public int orderId { get; set; }
            public string value { get; set; } = string.Empty;
        }
    }
}
```

- [ ] **Step 2: 构建验证**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/HttpObjects/Requests/SCII_XT/EquipmentCheckReq.cs
git commit -m "feat: add EquipmentCheckReq DTO for SCII_XT /check endpoint"
```

---

### Task 2: 创建 `/check` 响应 DTO

**Files:**
- Create: `OperationGuidance_service/Models/DTOs/EquipmentCheckDTO.cs`

- [ ] **Step 1: 编写 EquipmentCheckDTO 类**

```csharp
namespace OperationGuidance_service.Models.DTOs {
    public class EquipmentCheckDTO {
        public bool checkSuccess { get; set; }
        public string message { get; set; } = string.Empty;
    }
}
```

- [ ] **Step 2: 构建验证**

Run: `dotnet build OperationGuidance_service/OperationGuidance_service.csproj`

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_service/Models/DTOs/EquipmentCheckDTO.cs
git commit -m "feat: add EquipmentCheckDTO for SCII_XT /check response"
```

---

### Task 3: Workflow_SCII_XT 新增 EquipmentCheck 方法

**Files:**
- Modify: `OperationGuidance_new/Utils/Workflow_SCII_XT.cs`

- [ ] **Step 1: 在 `Workflow_SCII_XT` 类末尾（`OutBoundStation` 方法之后，类闭合 `}` 之前）添加方法**

```csharp
// 7. 设备点检数据上报
public static async Task<EquipmentCheckDTO> EquipmentCheck(EquipmentCheckReq req) {
    var api = "/api/check";
    var result = new EquipmentCheckDTO();

    try {
        var rsp = await HttpUtils.SendPost_SCII_XT<EquipmentCheckReq, SCII_XT_Response>(RequestPrefix + api, req);
        result.checkSuccess = rsp.code == (int) SCII_XT_ResponseCode.OK;
        result.message = rsp.message;
    } catch (Exception ex) {
        result.checkSuccess = false;
        result.message = ex.Message;
    }

    return result;
}
```

- [ ] **Step 2: 添加必要的 using**

在文件顶部 using 块中添加：
```csharp
using OperationGuidance_new.HttpObjects.Requests.SCII_XT;
using OperationGuidance_service.Models.DTOs;
```

（如果已存在则跳过）

- [ ] **Step 3: 构建验证**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_new/Utils/Workflow_SCII_XT.cs
git commit -m "feat: add EquipmentCheck workflow method for SCII_XT /check endpoint"
```

---

### Task 4: MissionEditionView_SCII — 提取挑战控件初始化为 virtual 方法

**Files:**
- Modify: `OperationGuidance_new/Views/MissionEditionView_SCII.cs:1551-1846`

- [ ] **Step 1: 将构造函数中的挑战控件创建代码提取为 `InitChallengeControls()`**

在 `MissionDetailPopUpForm` 类的构造函数中（第 1604-1666 行），将：
```csharp
_isChallengeMission = new("是否挑战任务") {
    Parent = _tablePanel,
    Ratio = _boxRatio,
    NameAlignment = HorizontalAlignment.Right,
};
_isFirstMission = new("是否首道岗位") {
    Parent = _tablePanel,
    Ratio = _boxRatio,
    NameAlignment = HorizontalAlignment.Right,
    Enabled = false,
};
_challengMission = new("挑战对应任务") {
    Parent = _tablePanel,
    Ratio = _boxRatioOneLine,
    NameAlignment = HorizontalAlignment.Right,
    Enabled = false,
};
// ... _allOtherMissions.ForEach, _isChallengeMission.CheckedChanged, _challengMission.ItemSelected
```
替换为调用 `InitChallengeControls();`

然后添加 virtual 方法（在 `InitScrewCounterBoxes` 附近）：
```csharp
protected virtual void InitChallengeControls() {
    _isChallengeMission = new("是否挑战任务") {
        Parent = _tablePanel,
        Ratio = _boxRatio,
        NameAlignment = HorizontalAlignment.Right,
    };
    _isFirstMission = new("是否首道岗位") {
        Parent = _tablePanel,
        Ratio = _boxRatio,
        NameAlignment = HorizontalAlignment.Right,
        Enabled = false,
    };
    _challengMission = new("挑战对应任务") {
        Parent = _tablePanel,
        Ratio = _boxRatioOneLine,
        NameAlignment = HorizontalAlignment.Right,
        Enabled = false,
    };

    _allOtherMissions.ForEach(m => {
        _challengMission.AddItem(m.name, m.id);
        _predecessorMission.AddItem(m.name, m.id);
    });
    _isChallengeMission.CheckedChanged += (s, e) => {
        if (_isChallengeMission.Checked) {
            _isFirstMission.Enabled = true;
            _challengMission.Enabled = true;
            _isFirstMission.Checked = _missionDTO.is_first_mission == (int) YesOrNo.YES;
            if (_missionDTO.challenge_mission_id != null) {
                _challengMission.SetCurrent(_challengMission.IndexOf(_missionDTO.challenge_mission_id.Value));
            }
        } else {
            _isFirstMission.Checked = false;
            _isFirstMission.Enabled = false;
            _challengMission.Reset();
            _challengMission.Enabled = false;
        }
        _isFirstMission.Invalidate();
        _challengMission.Invalidate();
    };
    _challengMission.ItemSelected += () => _challengMission.SetError(false);
}
```

- [ ] **Step 2: 提取 `AfterShown` 中挑战字段回填为 `FillChallengeFields()`**

在 `AfterShown` 方法（第 1826 行）中，将：
```csharp
_isChallengeMission.Checked = _missionDTO.is_challenge_mission == (int) YesOrNo.YES;
```
替换为调用 `FillChallengeFields();`

添加 virtual 方法：
```csharp
protected virtual void FillChallengeFields() {
    _isChallengeMission.Checked = _missionDTO.is_challenge_mission == (int) YesOrNo.YES;
}
```

- [ ] **Step 3: 构建验证**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_new/Views/MissionEditionView_SCII.cs
git commit -m "refactor: extract challenge controls init/fill to virtual methods in MissionDetailPopUpForm"
```

---

### Task 5: MissionDetailPopUpForm_SCII_XT — 重写为点检任务控件

**Files:**
- Modify: `OperationGuidance_new/Views/MissionEditionView_SCII_XT.cs:346-437`

- [ ] **Step 1: 在 `MissionDetailPopUpForm_SCII_XT` 类中添加 `InitChallengeControls` 重写**

在类中 `AfterShown` 方法之前添加：
```csharp
protected override void InitChallengeControls() {
    _isChallengeMission = new("是否是点检任务") {
        Parent = _tablePanel,
        Ratio = _boxRatio,
        NameAlignment = HorizontalAlignment.Right,
    };
}
```

- [ ] **Step 2: 在类中添加 `FillChallengeFields` 重写**

```csharp
protected override void FillChallengeFields() {
    _isChallengeMission.Checked = _missionDTO.is_challenge_mission == (int) YesOrNo.YES;
}
```

- [ ] **Step 3: 修改 `AfterShown`**

移除 `AfterShown` 中对 `_predecessorMission.SetCurrent(...)` 的调用（第 423-425 行）。修改后的 `AfterShown`：
```csharp
protected override void AfterShown() {
    _missionName.SetValue(0, _missionDTO.name);
    _isChallengeMission.Checked = _missionDTO.is_challenge_mission == (int) YesOrNo.YES;
    _maxNGNum.SetValue(0, _missionDTO.max_ng_num + "");
    _passwordNeedTime.SetValue(0, _missionDTO.password_need_time + "");

    for (int i = 0; i < _screwBitCounterDTOs.Count - 1; i++) {
        AddScrewBitCounter();
    }

    for (int i = 0; i < _screwBitCounterDTOs.Count; i++) {
        ScrewBitCounterDTO sbc = _screwBitCounterDTOs[i];
        _screwBitCounters[i].SetValue(0, sbc.bit_position + "");
        _screwBitCounters[i].SetValue(1, sbc.max_num + "");
    }
}
```

- [ ] **Step 4: 构建验证**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`

- [ ] **Step 5: Commit**

```bash
git add OperationGuidance_new/Views/MissionEditionView_SCII_XT.cs
git commit -m "feat: replace challenge mission controls with point inspection toggle in SCII_XT popup"
```

---

### Task 6: MissionEditionPage_SCII_XT — 删减校验和保存逻辑

**Files:**
- Modify: `OperationGuidance_new/Views/MissionEditionView_SCII_XT.cs:57-303`

- [ ] **Step 1: 删除挑战任务相关校验（第 102-130 行）**

移除以下代码块：
```csharp
// Check challenge mission settings
if (_detialPopUpForm.IsChallengeMission.Checked) {
    // ... (102-130)
}
```

- [ ] **Step 2: 删除 `PredecessorPartMissionMaps` 重复校验（第 162-206 行中挑战相关部分）**

将第 132-206 行的 `PredecessorPartMissionMaps` 校验逻辑整体删除。因为 SCII_XT 弹窗不再有 `_predecessorMission` 和 `_predecessorPartMissionMaps` 控件。

- [ ] **Step 3: 修改 `_detialPopUpForm.IsFirstMission` 相关引用**

删除第 121 行和第 183 行中引用 `_detialPopUpForm.IsFirstMission` 的代码。

具体地：
- 删除第 121 行: `if (_detialPopUpForm.IsFirstMission.Checked) {`
- 删除第 122-127 行: 对应的检查块
- 删除第 183 行: `if (_detialPopUpForm.IsFirstMission.Checked && !_detialPopUpForm.PredecessorMission.IsDefaultValue()) {`
- 删除第 184-186 行: 对应的检查块

- [ ] **Step 4: 简化套筒位计数器校验**

原 SCII 版本检查 3 个 box（套筒位 + 批头上限 + 单次计数），SCII_XT 只有 2 个 box。修改第 208-235 行中 counters 的校验逻辑，移除对 `box.GetTextBox(2)` 的检查。修改后：

```csharp
List<CustomTextBox> bitPositions = new();
List<CustomTextBox> counters = new();
_detialPopUpForm.ScrewBitCounters.ForEach(box => {
    if (!box.GetTextBox(0).IsEmpty()) {
        if (int.Parse(box.GetTextBox(0).Box.Text) <= 0) {
            bitPositions.Add(box.GetTextBox(0));
        }

        if (box.GetTextBox(1).IsEmpty()) {
            counters.Add(box.GetTextBox(1));
        }
    }
});
if (bitPositions.Count > 0) {
    check = false;
    foreach (CustomTextBox box in bitPositions) {
        box.IsError = true;
    }
    warningMsg += $"{warningIndex++}. 套筒位不能小于0\r\n";
}
if (counters.Count > 0) {
    check = false;
    foreach (CustomTextBox box in counters) {
        box.IsError = true;
    }
    warningMsg += $"{warningIndex++}. 套筒位不为空时，批头使用上限也不能为空\r\n";
}
```

- [ ] **Step 5: 删除对 `_detialPopUpForm.PredecessorPartMissionMaps` 的 ForEach 循环**

删除第 163 行: `if (!_detialPopUpForm.PredecessorMission.IsDefaultValue())` 及后续重复检查逻辑。

- [ ] **Step 6: 修改保存逻辑（第 240-265 行）**

将保存代码修改为：
```csharp
_missionName.SetValue(0, missionName);
_missionDTO.name = missionName;
_missionDTO.is_challenge_mission = (int) (_detialPopUpForm.IsChallengeMission.Checked ? YesOrNo.YES : YesOrNo.NO);
_missionDTO.is_first_mission = null;
_missionDTO.challenge_mission_id = null;
_missionDTO.max_ng_num = int.Parse(maxNGNum);
_missionDTO.password_need_time = int.Parse(passwordNeedTime);
_missionDTO.predecessor_mission_id = null;
_missionDTO.predecessor_part_mission_ids = null;
```

删除 `_missionDTO.is_first_mission`、`_missionDTO.challenge_mission_id`、`_missionDTO.predecessor_mission_id`、`_missionDTO.predecessor_part_mission_ids` 的原有赋值逻辑。

- [ ] **Step 7: 调整 `_detialPopUpForm.ScrewBitCounters` 的 ForEach**

SCII_XT 版本只有 2 个 box（套筒位、批头使用上限），移除对 `box.GetTextBox(2)` 的引用。修改保存代码（第 266-282 行）：

```csharp
_detialPopUpForm.ScrewBitCounters.ForEach(box => {
    if (!box.GetTextBox(0).IsEmpty() && !box.GetTextBox(1).IsEmpty()) {
        int bitPosition = int.Parse(box.GetTextBox(0).Box.Text);
        int maxNum = int.Parse(box.GetTextBox(1).Box.Text);

        ScrewBitCounterDTO? temp = _screwBitCounterDTOs.Find(dto => dto.bit_position == bitPosition);
        if (temp == null) {
            temp = new() {
                mission_id = _missionDTO.id,
            };
            _screwBitCounterDTOs.Add(temp);
        }

        temp.bit_position = bitPosition;
        temp.max_num = maxNum;
    }
});
```

- [ ] **Step 8: 构建验证**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`

- [ ] **Step 9: Commit**

```bash
git add OperationGuidance_new/Views/MissionEditionView_SCII_XT.cs
git commit -m "feat: remove challenge validation/save logic from SCII_XT mission edition"
```

---

### Task 7: AWorkplaceContentPanel — 挑战逻辑提取为 virtual

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs:1147-1250, 2502-2504, 2872-2878`

- [ ] **Step 1: `CheckChallengeMissionConfirmation` 改为 virtual，基类返回 true**

将第 1147 行的 `public bool CheckChallengeMissionConfirmation()` 改为：
```csharp
public virtual bool CheckChallengeMissionConfirmation() {
    return true;
}
```
将原有方法体（第 1148-1196 行）删除，保留签名和 `return true;`。

- [ ] **Step 2: `ChallengeChecks` 改为 virtual，基类返回 true**

将第 1198 行的 `private bool ChallengeChecks(...)` 改为：
```csharp
protected virtual bool ChallengeChecks(int challengeMissionId, bool hasPredecessorMission) {
    return true;
}
```
删除原有方法体。

- [ ] **Step 3: `AddChallengeResult` 改为 virtual，基类空实现**

将第 1235 行的 `public void AddChallengeResult(...)` 改为：
```csharp
public virtual void AddChallengeResult(int challengeMissionId, ChallengeTaskEnum type) { }
```
删除原有方法体。

- [ ] **Step 4: MISSION_OK 检查改为 virtual**

将第 2502-2504 行：
```csharp
if (_mission.is_challenge_mission == (int) YesOrNo.YES) {
    AddChallengeResult(_mission.id, ChallengeTaskEnum.MISSION_OK);
}
```
改为调用 virtual 方法：
```csharp
OnMissionComplete();
```

添加 virtual 方法（在 `AddChallengeResult` 附近）：
```csharp
protected virtual void OnMissionComplete() { }
```

- [ ] **Step 5: 添加 `IsChallengeMission` virtual 属性**

在第 2872-2878 行，将：
```csharp
if (_mission.is_challenge_mission != (int) YesOrNo.YES
```
改为：
```csharp
if (!IsChallengeMission()
```

添加 virtual 方法：
```csharp
protected virtual bool IsChallengeMission() {
    return _mission.is_challenge_mission == (int) YesOrNo.YES;
}
```

- [ ] **Step 6: 构建验证**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`

- [ ] **Step 7: Commit**

```bash
git add OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs
git commit -m "refactor: extract challenge mission logic from AWorkplaceContentPanel to virtual methods"
```

---

### Task 8: WorkplaceContentPanel_SCII — 重写虚拟方法恢复挑战逻辑

**Files:**
- Modify: `OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs`

- [ ] **Step 1: 在 `WorkplaceContentPanel_SCII` 类中添加挑战方法的 override**

在类中添加（在 `TerminateMission` 方法附近）：

```csharp
public override bool CheckChallengeMissionConfirmation() {
    List<ProductMissionDTO> allOtherMissions = _apis.QueryProductMissions(new()).ProductMissionsDTOs.Where(m => m.id != _mission.id).ToList();
    ProductMissionDTO? challengeMission = allOtherMissions.Find(m =>
        m.challenge_mission_id != null &&
        m.challenge_mission_id > 0 &&
        m.challenge_mission_id == _mission.id);

    if (challengeMission != null) {
        if (challengeMission.is_first_mission == (int) YesOrNo.YES) {
            if (_mission.predecessor_mission_id != null && _mission.predecessor_mission_id > 0) {
                WidgetUtils.ShowWarningPopUp("当前任务绑定了【挑战任务 - 首档岗位】，但此任务存在前置任务，配置出错，请联系开发人员检查软件逻辑！");
                return false;
            } else {
                if (!ChallengeChecks(challengeMission.id, false)) {
                    return false;
                }
            }
        } else {
            if (_mission.predecessor_mission_id != null && _mission.predecessor_mission_id > 0) {
                if (challengeMission.predecessor_mission_id == null) {
                    WidgetUtils.ShowWarningPopUp("当前任务绑定了【挑战任务 - 非首档岗位】且当前任务存在前置任务，但挑战任务不存在前置任务，配置出错，请联系开发人员检查软件逻辑！");
                    return false;
                } else {
                    ProductMissionDTO? predecessorMissionForChallengeMission =
                        allOtherMissions.Find(m => m.predecessor_mission_id == challengeMission.predecessor_mission_id);
                    if (!ChallengeChecks(challengeMission.id, predecessorMissionForChallengeMission != null)) {
                        return false;
                    }
                }
            } else {
                if (challengeMission.predecessor_mission_id != null && challengeMission.predecessor_mission_id > 0) {
                    WidgetUtils.ShowWarningPopUp("当前任务绑定了【挑战任务 - 非首档岗位】且当前任务不存在前置任务，但挑战任务存在前置任务，配置出错，请联系开发人员检查软件逻辑！");
                    return false;
                }

                if (!ChallengeChecks(challengeMission.id, false)) {
                    return false;
                }
            }
        }
    }
    return true;
}

protected override bool ChallengeChecks(int challengeMissionId, bool hasPredecessorMission) {
    string jsonObj = MainUtils.ChallengeTaskUtil.Read(challengeMissionId.ToString());
    ChallengeTask? task = JsonConvert.DeserializeObject<ChallengeTask>(jsonObj);

    bool hasPartsBarCode = false;
    if (_partsBarCodeMatchingRules.ContainsKey(_mission.id)) {
        hasPartsBarCode = _partsBarCodeMatchingRules[_mission.id].Count > 0;
    }

    if (task == null || !task.IsToday()) {
        WidgetUtils.ShowWarningPopUp("此任务还未通过挑战任务校验！");
        return false;
    } else if (!hasPredecessorMission && !task.ProductBarCodeErrorOK()) {
        WidgetUtils.ShowWarningPopUp("此任务还未通过挑战任务【追溯码-错码】校验！");
        return false;
    } else if (hasPredecessorMission && !task.ProductPredecessorOK()) {
        WidgetUtils.ShowWarningPopUp("此任务还未通过挑战任务【追溯码-上一道岗位未完成】校验！");
        return false;
    } else if (!hasPredecessorMission && !task.ProductBarCodeRedoOK()) {
        WidgetUtils.ShowWarningPopUp("此任务还未通过挑战任务【追溯码-重码】校验！");
        return false;
    } else if (hasPartsBarCode && !task.PartsBarCodeErrorOK()) {
        WidgetUtils.ShowWarningPopUp("此任务还未通过挑战任务【物料码-错码】校验！");
        return false;
    } else if (hasPredecessorMission && !task.PartsPredecessorOK()) {
        WidgetUtils.ShowWarningPopUp("此任务还未通过挑战任务【物料码-上一道岗位未完成】校验！");
        return false;
    } else if (hasPartsBarCode && !task.PartsBarCodeRedoOK()) {
        WidgetUtils.ShowWarningPopUp("此任务还未通过挑战任务【物料码-重码】校验！");
        return false;
    } else if (!task.MissionOK()) {
        WidgetUtils.ShowWarningPopUp("此任务对应挑战任务未完成！");
        return false;
    }
    return true;
}

public override void AddChallengeResult(int challengeMissionId, ChallengeTaskEnum type) {
    string jsonObj = MainUtils.ChallengeTaskUtil.Read(challengeMissionId.ToString());
    ChallengeTask? task = JsonConvert.DeserializeObject<ChallengeTask>(jsonObj);

    if (task == null) {
        task = new();
    }

    task.MissionId = challengeMissionId;
    task.AddResult(type);

    MainUtils.ChallengeTaskUtil.Write(challengeMissionId.ToString(), JsonConvert.SerializeObject(task));
}

protected override void OnMissionComplete() {
    if (_mission.is_challenge_mission == (int) YesOrNo.YES) {
        AddChallengeResult(_mission.id, ChallengeTaskEnum.MISSION_OK);
    }
}

protected override bool IsChallengeMission() {
    return _mission.is_challenge_mission == (int) YesOrNo.YES;
}
```

- [ ] **Step 2: 构建验证**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs
git commit -m "feat: restore challenge mission logic in SCII workplace via virtual overrides"
```

---

### Task 9: ABarCodeInputPopUpForm — 提取挑战检查为 virtual

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/ABarCodeInputPopUpForm.cs:245-518`

- [ ] **Step 1: 添加 5 个 protected virtual 方法到类中**

在类末尾（类闭合 `}` 之前）添加：
```csharp
protected virtual void CheckProductBarCodeErrorForChallenge() { }
protected virtual void CheckProductPredecessorForChallenge(bool predecessorExists, DateTime? createTime) { }
protected virtual void CheckProductBarCodeRedoForChallenge() { }
protected virtual void CheckPartsBarCodeErrorForChallenge() { }
protected virtual void CheckPartsPredecessorForChallenge(bool predecessorExists, DateTime? createTime) { }
protected virtual void CheckPartsBarCodeRedoForChallenge() { }
```

- [ ] **Step 2: 替换产品条码错码检查（第 246-248 行）**

将：
```csharp
if (_mission.is_challenge_mission == (int) YesOrNo.YES) {
    logger.Info($"*Current mission id = [{_mission.id}] is a challenge mission, barcode = [{barCode}]...");
    _workplace.AddChallengeResult(_mission.id, ChallengeTaskEnum.PRODUCT_BAR_CODE_ERROR);
}
```
替换为：
```csharp
CheckProductBarCodeErrorForChallenge();
```

- [ ] **Step 3: 替换产品条码错码检查（第 280-283 行）**

同上替换。

- [ ] **Step 4: 替换产品前置任务挑战检查（第 319-331 行）**

将：
```csharp
if (_mission.is_challenge_mission == (int) YesOrNo.YES) {
    logger.Info($"*Current mission id = [{mission.id}] is a challenge mission, barcode = [{barCode}]...");
    if (rsp.Yes) {
        if (rsp.MissionRecordDTO.create_time.Date != DateTime.Now.Date) {
            logger.Info($"*Checking predecessor mission, ...");
            _workplace.AddChallengeResult(_mission.id, ChallengeTaskEnum.PRODUCT_PREDECESSOR);
        }
    } else {
        logger.Info($"*Checking predecessor mission, ...");
        _workplace.AddChallengeResult(_mission.id, ChallengeTaskEnum.PRODUCT_PREDECESSOR);
    }
}
```
替换为：
```csharp
CheckProductPredecessorForChallenge(rsp.Yes, rsp.MissionRecordDTO?.create_time);
```

- [ ] **Step 5: 替换产品条码返工检查（第 346-349 行）**

将：
```csharp
if (_mission.is_challenge_mission == (int) YesOrNo.YES) {
    logger.Info($"*Current mission id = [{_mission.id}], barcode = [{barCode}] is a challenge mission, checking REDO...");
    _workplace.AddChallengeResult(_mission.id, ChallengeTaskEnum.PRODUCT_BAR_CODE_REDO);
}
```
替换为：
```csharp
CheckProductBarCodeRedoForChallenge();
```

- [ ] **Step 6: 替换物料条码错码检查（第 450-453 行）**

将：
```csharp
if (_mission.is_challenge_mission == (int) YesOrNo.YES) {
    logger.Info($"*Current mission id = [{_mission.id}] is a challenge mission, parts barcode = [{barCode}], checking parts bar code...");
    _workplace.AddChallengeResult(_mission.id, ChallengeTaskEnum.PARTS_BAR_CODE_ERROR);
}
```
替换为：
```csharp
CheckPartsBarCodeErrorForChallenge();
```

- [ ] **Step 7: 替换物料前置任务挑战检查（第 476-488 行）**

将：
```csharp
if (_mission.is_challenge_mission == (int) YesOrNo.YES) {
    logger.Info($"*Current mission id = [{_mission.id}] is a challenge mission, barcode = [{barCode}]...");
    if (rsp.Yes) {
        if (rsp.MissionRecordDTO.create_time.Date != DateTime.Now.Date) {
            logger.Info($"*Checking parts predecessor mission, ...");
            _workplace.AddChallengeResult(_mission.id, ChallengeTaskEnum.PARTS_PREDECESSOR);
        }
    } else {
        logger.Info($"*Checking parts predecessor mission, ...");
        _workplace.AddChallengeResult(_mission.id, ChallengeTaskEnum.PARTS_PREDECESSOR);
    }
}
```
替换为：
```csharp
CheckPartsPredecessorForChallenge(rsp.Yes, rsp.MissionRecordDTO?.create_time);
```

- [ ] **Step 8: 替换物料条码返工检查（第 515-518 行）**

将：
```csharp
if (_mission.is_challenge_mission == (int) YesOrNo.YES) {
    logger.Info($"*Current mission id = [{_mission.id}], barcode = [{barCode}] is a challenge mission, checking REDO...");
    _workplace.AddChallengeResult(_mission.id, ChallengeTaskEnum.PARTS_BAR_CODE_REDO);
}
```
替换为：
```csharp
CheckPartsBarCodeRedoForChallenge();
```

- [ ] **Step 9: 构建验证**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`

- [ ] **Step 10: Commit**

```bash
git add OperationGuidance_new/Views/AbstractViews/ABarCodeInputPopUpForm.cs
git commit -m "refactor: extract challenge barcode checks to virtual methods in ABarCodeInputPopUpForm"
```

---

### Task 10: BarCodeInputPopUpForm_SCII — 重写挑战检查方法

**Files:**
- Modify: `OperationGuidance_new/Views/ReusableWidgets/BarCodeInputPopUpForm_SCII.cs`

- [ ] **Step 1: 在类中添加 6 个 override 方法**

在类末尾（类闭合 `}` 之前）添加：

```csharp
protected override void CheckProductBarCodeErrorForChallenge() {
    if (_mission.is_challenge_mission == (int) YesOrNo.YES) {
        logger.Info($"*Current mission id = [{_mission.id}] is a challenge mission, barcode = [{_productBarCodeBox.GetTextBox(0).Box.Text}]...");
        _workplace.AddChallengeResult(_mission.id, ChallengeTaskEnum.PRODUCT_BAR_CODE_ERROR);
    }
}

protected override void CheckProductPredecessorForChallenge(bool predecessorExists, DateTime? createTime) {
    if (_mission.is_challenge_mission == (int) YesOrNo.YES) {
        logger.Info($"*Current mission id = [{_mission.id}] is a challenge mission...");
        if (!predecessorExists || (createTime != null && createTime.Value.Date != DateTime.Now.Date)) {
            _workplace.AddChallengeResult(_mission.id, ChallengeTaskEnum.PRODUCT_PREDECESSOR);
        }
    }
}

protected override void CheckProductBarCodeRedoForChallenge() {
    if (_mission.is_challenge_mission == (int) YesOrNo.YES) {
        logger.Info($"*Current mission id = [{_mission.id}] is a challenge mission, checking REDO...");
        _workplace.AddChallengeResult(_mission.id, ChallengeTaskEnum.PRODUCT_BAR_CODE_REDO);
    }
}

protected override void CheckPartsBarCodeErrorForChallenge() {
    if (_mission.is_challenge_mission == (int) YesOrNo.YES) {
        logger.Info($"*Current mission id = [{_mission.id}] is a challenge mission, checking parts bar code...");
        _workplace.AddChallengeResult(_mission.id, ChallengeTaskEnum.PARTS_BAR_CODE_ERROR);
    }
}

protected override void CheckPartsPredecessorForChallenge(bool predecessorExists, DateTime? createTime) {
    if (_mission.is_challenge_mission == (int) YesOrNo.YES) {
        logger.Info($"*Current mission id = [{_mission.id}] is a challenge mission...");
        if (!predecessorExists || (createTime != null && createTime.Value.Date != DateTime.Now.Date)) {
            _workplace.AddChallengeResult(_mission.id, ChallengeTaskEnum.PARTS_PREDECESSOR);
        }
    }
}

protected override void CheckPartsBarCodeRedoForChallenge() {
    if (_mission.is_challenge_mission == (int) YesOrNo.YES) {
        logger.Info($"*Current mission id = [{_mission.id}] is a challenge mission, checking REDO...");
        _workplace.AddChallengeResult(_mission.id, ChallengeTaskEnum.PARTS_BAR_CODE_REDO);
    }
}
```

添加必要的 using：
```csharp
using OperationGuidance_service.Constants;
```

- [ ] **Step 2: 构建验证**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/ReusableWidgets/BarCodeInputPopUpForm_SCII.cs
git commit -m "feat: override challenge check methods in BarCodeInputPopUpForm_SCII"
```

---

### Task 11: BarCodeInputPopUpForm_SCII_XT — 调整继承链

**Files:**
- Modify: `OperationGuidance_new/Views/ReusableWidgets/BarCodeInputPopUpForm_SCII_XT.cs:14`

- [ ] **Step 1: 将继承从 `BarCodeInputPopUpForm_SCII` 改为 `ABarCodeInputPopUpForm`**

将第 14 行：
```csharp
public class BarCodeInputPopUpForm_SCII_XT: BarCodeInputPopUpForm_SCII {
```
改为：
```csharp
public class BarCodeInputPopUpForm_SCII_XT: ABarCodeInputPopUpForm {
```

- [ ] **Step 2: 更新构造函数 base 调用**

将构造函数（第 24 行）的 `: base(workplace, initStr, mission, activated, productBarCodeRules, partsBarCodeRules, barCode, boltRules, isForBolt)` 保持不变，因为 `ABarCodeInputPopUpForm` 也有相同的构造函数签名。

- [ ] **Step 3: 更新 using（去掉不再需要的 SCII 引用，确认基类引用在）**

确保文件顶部有：
```csharp
using OperationGuidance_new.Views.AbstractViews;
```

可删除不再需要的 import（如果有 SCII 特有的），但 `BarCodeInputPopUpForm_SCII_XT` 当前没有引用 SCII 特有的类，所以无需修改。

- [ ] **Step 4: 构建验证**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`

- [ ] **Step 5: Commit**

```bash
git add OperationGuidance_new/Views/ReusableWidgets/BarCodeInputPopUpForm_SCII_XT.cs
git commit -m "refactor: change BarCodeInputPopUpForm_SCII_XT to inherit ABarCodeInputPopUpForm directly"
```

---

### Task 12: WorkplaceContentPanel_SCII_XT — 点检任务运行时逻辑

**Files:**
- Modify: `OperationGuidance_new/Views/WorkplaceMissionView_SCII_XT.cs:104-112, 174-192, 282-344`

- [ ] **Step 1: 修改 `TerminateMission` — 点检任务分支**

将第 104-112 行的 `TerminateMission` 方法改为：
```csharp
public override async Task TerminateMission(WorkplaceProcessStatus status) {
    if (_mission.is_challenge_mission == (int) YesOrNo.YES) {
        await SendCheckToMES(_operationDataDTOs);
    } else {
        await SendDataToMES(_operationDataDTOs);
    }
    _inBoundStationOk = false;

    if (_mission.is_challenge_mission != (int) YesOrNo.YES) {
        if (await OutBound()) {
            await base.TerminateMission(status);
            SwitchMissionByRecipe(_getRecipeCode());
        }
    } else {
        await base.TerminateMission(status);
    }
}
```

- [ ] **Step 2: 添加 `SendCheckToMES` 方法**

在 `SendDataToMES` 方法之后，添加：
```csharp
private async Task SendCheckToMES(List<OperationDataDTO> operationDataDTOs) {
    if (operationDataDTOs.Count > 0) {
        EquipmentCheckReq req = new() {
            equipmentCheckInfos = new(),
            employeeNumber = SystemUtils.UserInfo.account,
            equipmentCode = _getEquipmentCode(),
        };

        EquipmentCheckReq.EquipmentCheckInfo checkInfo = new() {
            attributeList = new(),
        };

        List<Dictionary<string, object>> value = new();
        foreach (OperationDataDTO operationDataDTO in operationDataDTOs) {
            Dictionary<string, object> eachValue = new();
            OperationDataDTO_SCII_XT data = new OperationDataDTO_SCII_XT();
            CommonUtils.ObjectConverter<OperationDataDTO, OperationDataDTO_SCII_XT>(operationDataDTO, data);

            data.parts_bar_codes = _missionRecord?.parts_bar_code;
            data.batch_code = _getBatchNo();
            data.time = DateTime.Now.ToString(MainUtils.DATETIME_FORMAT_YYYY_MM_DD_HH_MM_SS);

            PropertyInfo[] propertyInfos = data.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (PropertyInfo property in propertyInfos) {
                IEnumerable<Attribute> fieldAttrs = property.GetCustomAttributes();
                foreach (Attribute fieldAttr in fieldAttrs) {
                    if (fieldAttr is SCII_XT_Column column) {
                        object propertyValue = property.GetValue(data);
                        eachValue[column.Name ?? property.Name] = propertyValue;
                    }
                }
            }

            value.Add(eachValue);
        }
        checkInfo.attributeList.Add(new EquipmentCheckReq.Attribute() {
            attributeName = $"{_mission.name}_拧紧数据",
            attributeCode = $"{_mission.name}_Screw",
            attributeUnit = "json",
            attributeType = 2,
            value = JsonConvert.SerializeObject(value),
        });
        req.equipmentCheckInfos.Add(checkInfo);

        var dto = await Workflow_SCII_XT.EquipmentCheck(req);
        if (!dto.checkSuccess) {
            logger.Warn($"设备点检数据上传 MES 失败！[任务：{_mission.name}, 产品条码：{operationDataDTOs[0].vin_number}] 错误信息：{dto.message}");
        } else {
            logger.Info($"设备点检数据上传 MES 成功！[任务：{_mission.name}, 产品条码：{operationDataDTOs[0].vin_number}] 。");
        }

        _operationDataDTOs = new();
    }
}
```

添加必要的 using：
```csharp
using System.Reflection;
using OperationGuidance_service.Attributes;
using OperationGuidance_new.HttpObjects.Requests.SCII_XT;
```

- [ ] **Step 3: 修改 `SwitchMissionByRecipe` — 点检任务保护**

在 `SwitchMissionByRecipe` 方法开头（第 174 行之后）添加：
```csharp
if (_mission != null && _mission.is_challenge_mission == (int) YesOrNo.YES) {
    return;
}
```

- [ ] **Step 4: 修改 `ProductBarCodeExtraCheck` — 点检跳过进站**

在 `BarCodeInputPopUpForm_SCII_XT.cs` 的 `ProductBarCodeExtraCheck` 方法（第 119 行）开头添加：
```csharp
protected override bool ProductBarCodeExtraCheck(string barCode) {
    if (_mission.is_challenge_mission == (int) YesOrNo.YES) {
        return true;
    }
    // 原逻辑保持不变
    WorkplaceContentPanel_SCII_XT workplace = (WorkplaceContentPanel_SCII_XT) _workplace;
    inBoundStationOk = workplace.InBound(barCode, _productBatch);

    if (inBoundStationOk) {
        _ = workplace.SendToPrinter();
    }

    return inBoundStationOk;
}
```

- [ ] **Step 5: 添加 IsChallengeMission override（返回 false — 点检不是挑战任务）**

在 `WorkplaceContentPanel_SCII_XT` 类中添加：
```csharp
protected override bool IsChallengeMission() {
    return false;
}
```

- [ ] **Step 6: 构建验证**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`

- [ ] **Step 7: Commit**

```bash
git add OperationGuidance_new/Views/WorkplaceMissionView_SCII_XT.cs
git add OperationGuidance_new/Views/ReusableWidgets/BarCodeInputPopUpForm_SCII_XT.cs
git commit -m "feat: route point inspection tasks to /check endpoint and skip inbound/outbound"
```

---

### Task 13: 创建 DataQueryView_SCII_XT

**Files:**
- Create: `OperationGuidance_new/Views/DataQueryView_SCII_XT.cs`

- [ ] **Step 1: 编写 DataQueryView_SCII_XT**

```csharp
using CustomLibrary.ComboBoxes;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_new.Views {
    public class DataQueryView_SCII_XT : DataQueryView_SCII {
        public DataQueryView_SCII_XT() : base() {
            // Replace "是否挑战任务" label with "是否点检任务"
            var comboBoxField = _dataGridView.FindComboBoxField("是否挑战任务");
            if (comboBoxField != null) {
                comboBoxField.Label = "是否点检任务";
            }
        }
    }
}
```

注意：由于 `DataQueryView_SCII` 的 `_isChallengMissionComboBox` 和 `_dataGridView` 是 private，需要确认访问方式。如果无法直接访问，可改用以下方式：

在 `DataQueryView_SCII` 中将 `_isChallengMissionComboBox` 的访问修饰符改为 `protected`，或将组合框标签提取为 `protected virtual string ChallengeMissionFilterLabel => "是否挑战任务";`，然后 `DataQueryView_SCII_XT` 重写属性返回 `"是否点检任务"`。

**推荐方案**：在 `DataQueryView_SCII.cs` 第 99 行，将：
```csharp
_isChallengMissionComboBox = _dataGridView.AddComboBox("是否挑战任务", ...)
```
改为：
```csharp
_isChallengMissionComboBox = _dataGridView.AddComboBox(ChallengeMissionFilterLabel, ...)
```

并添加：
```csharp
protected virtual string ChallengeMissionFilterLabel => "是否挑战任务";
```

然后在 `DataQueryView_SCII_XT` 中重写：
```csharp
protected override string ChallengeMissionFilterLabel => "是否点检任务";
```

同时将 `DataQueryView_SCII` 中的 `_isChallengMissionComboBox` 字段从 `private` 改为 `protected`。

- [ ] **Step 2: 如果需要，修改 `DataQueryView_SCII` 支持重写**

修改 `DataQueryView_SCII.cs`:
- 第 36 行: `private CustomComboBoxGroup<bool?> _isChallengMissionComboBox;` → `protected CustomComboBoxGroup<bool?> _isChallengMissionComboBox;`
- 第 99 行: `"是否挑战任务"` → `ChallengeMissionFilterLabel`
- 添加: `protected virtual string ChallengeMissionFilterLabel => "是否挑战任务";`

- [ ] **Step 3: 构建验证**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_new/Views/DataQueryView_SCII_XT.cs
git add OperationGuidance_new/Views/DataQueryView_SCII.cs
git commit -m "feat: add DataQueryView_SCII_XT with point inspection filter label"
```

---

### Task 14: 全量构建验证

**Files:** None (verification only)

- [ ] **Step 1: Clean + Build 整个解决方案**

Run: `dotnet clean && dotnet build`
Expected: Build succeeded with 0 errors.

- [ ] **Step 2: 检查是否有引入新的编译警告**

Run: `dotnet build 2>&1 | grep -i "warning" | grep -v "CS8618"`
Expected: No new warnings beyond existing CS8618 nullable warnings.

---

## 验证清单

实施完成后，逐项验证：

1. **任务编辑弹窗 (SCII_XT)**: 打开 SCII_XT 任务详情弹窗 → 确认只显示"是否是点检任务"ToggleButton，不显示挑战对应任务/首道岗位/前置任务/物料前置任务
2. **保存**: 勾选"是否是点检任务" → 保存 → 重新打开弹窗 → 确认勾选状态被正确回填
3. **SCII 不受影响**: 打开 SCII 任务详情弹窗 → 确认"是否挑战任务"及相关控件正常显示和工作
4. **工作台点检任务**: 进站 → 点检任务被手动选中 → 不触发进站请求 → 任务结束时调 `/check` 不调 `BindProductData`
5. **工作台普通任务**: 进站 → 普通任务 → 正常进站/出站 → 任务结束时调 `BindProductData`
6. **配方切换保护**: 选中点检任务 → PLC 发配方变更 → 点检任务不被强制切换
7. **条码弹窗**: 点检任务扫条码 → 不触发挑战任务检查（无 `AddChallengeResult` 调用）
8. **数据查询**: 打开 SCII_XT 数据查询 → 筛选标签显示"是否点检任务"而非"是否挑战任务"
