# SCII XT — 物料码上传改造 & 点检去MES化 & 料号默认值

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace upper-cover binding with BindAccessory, remove all MES interaction from point inspection, add part_no default "1" for new barcode rules.

**Architecture:** Three independent changes touching three files. No new files, no shared state between changes.

**Tech Stack:** C#, WinForms, .NET

---

### Task 1: 料号默认值 "1"

**Files:**
- Modify: `OperationGuidance_new/Views/BarCodeMatchingRuleManagementView_SCII_XT.cs:45-48`

- [ ] **Step 1: Add default value for new rules**

In `OpenEditEntityPopUpForm`, after the `partNo` text box setup, add a condition to set default "1" for new rules:

```csharp
// Current (lines 43-48):
CustomTextBoxGroup partNo = _editEntityPopUpForm.AddTextBox("料号", false,
    (BarCodeMatchingRuleDTO dto, string? value) => dto.part_no = value ?? null);
partNo.Hide();
if (!string.IsNullOrEmpty(dto.part_no)) {
    partNo.SetValue(0, dto.part_no);
}
```

Change to:

```csharp
CustomTextBoxGroup partNo = _editEntityPopUpForm.AddTextBox("料号", false,
    (BarCodeMatchingRuleDTO dto, string? value) => dto.part_no = value ?? null);
partNo.Hide();
if (!string.IsNullOrEmpty(dto.part_no)) {
    partNo.SetValue(0, dto.part_no);
} else if (dto.id <= 0) {
    partNo.SetValue(0, "1");
}
```

- [ ] **Step 2: Build and verify compilation**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/BarCodeMatchingRuleManagementView_SCII_XT.cs
git commit -m "feat: default part_no to '1' for new barcode matching rules in SCII_XT

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

### Task 2: CheckCanActivateMission — BindAccessory 替代上盖

**Files:**
- Modify: `OperationGuidance_new/Views/ReusableWidgets/BarCodeInputPopUpForm_SCII_XT.cs:39-84`

- [ ] **Step 1: Replace upper-cover logic with BindAccessory**

Replace the `send_upper_cover` / `BindUppderCover` block in `CheckCanActivateMission()`:

**Remove** (lines 49-70):
```csharp
            // 向 MES 发送绑定上盖请求。只有一个工站会用这个
            SciiXtConfig config = ConfigUtils.LoadConfig<SciiXtConfig>();
            if (config.send_upper_cover.ToYesOrNoBool()) {
                if (_partsBarCodeRules.ContainsKey(_mission.id) && _partsBarCodeRules[_mission.id].Count > 0) {
                    var req = new SCII_XT_BindUpperCoverReq() {
                        productCode = _workplace.BarCodeObj.ProductBarCode,
                        upperCoverCode = _workplace.BarCodeObj.PartsBarCodes[0],
                        employeeNumber = SystemUtils.UserInfo.account,
                    };

                    var dto = Task.Run(async () => await Workflow_SCII_XT.BindUppderCover(req))
                                  .GetAwaiter()
                                  .GetResult();
                    if (!dto.bindSuccess) {
                        string msg = $"上盖绑定请求失败，详细信息：{dto.message}";
                        logger.Warn(msg);
                        WidgetUtils.ShowWarningPopUp(msg);
                        return false;
                    }
                    logger.Info($"【{_workplace.BarCodeObj.PartsBarCodes[0]}】上盖绑定成功。");
                }
            }
```

**Insert** in its place:
```csharp
            // 向 MES 上传配件绑定
            if (_partsBarCodeRules.ContainsKey(_mission.id)) {
                var partsRules = _partsBarCodeRules[_mission.id]
                    .Where(r => r.type == BarCodeTypes.PARTS.Id)
                    .OrderBy(r => r.id)
                    .ToList();

                if (partsRules.Count > 0 && _workplace.BarCodeObj.PartsBarCodes.Count > 0) {
                    var accessories = new List<SCII_XT_BindAccessoryReq.Accessory>();
                    for (int i = 0; i < _workplace.BarCodeObj.PartsBarCodes.Count; i++) {
                        int ruleId = _workplace.BarCodeObj.PartsMatchingRulesCached[i];
                        var rule = partsRules.FirstOrDefault(r => r.id == ruleId);
                        if (rule != null) {
                            accessories.Add(new SCII_XT_BindAccessoryReq.Accessory {
                                accessoryCode = _workplace.BarCodeObj.PartsBarCodes[i],
                                accessoryType = rule.name ?? "",
                                partNo = rule.part_no ?? "",
                                orderId = partsRules.IndexOf(rule) + 1,
                            });
                        }
                    }

                    if (accessories.Count > 0) {
                        SciiXtConfig config = ConfigUtils.LoadConfig<SciiXtConfig>();
                        var req = new SCII_XT_BindAccessoryReq() {
                            productCode = _workplace.BarCodeObj.ProductBarCode,
                            procedureCode = config.procedure_code,
                            recipeCode = _mission.name,
                            accessorys = accessories,
                            createBy = SystemUtils.UserInfo.staff_id,
                            employeeNumber = SystemUtils.UserInfo.account,
                        };

                        var dto = Task.Run(async () => await Workflow_SCII_XT.BindAccessory(req))
                                      .GetAwaiter()
                                      .GetResult();
                        if (!dto.bindSuccess) {
                            string msg = $"配件绑定请求失败，详细信息：{dto.message}";
                            logger.Warn(msg);
                            WidgetUtils.ShowWarningPopUp(msg);
                            return false;
                        }
                        logger.Info("配件绑定成功。");
                    }
                }
            }
```

- [ ] **Step 2: Build and verify compilation**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/ReusableWidgets/BarCodeInputPopUpForm_SCII_XT.cs
git commit -m "feat: replace upper-cover BindUppderCover with BindAccessory in SCII_XT

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

### Task 3: 点检去 MES 化

**Files:**
- Modify: `OperationGuidance_new/Views/WorkplaceMissionView_SCII_XT.cs:109-123` (TerminateMission)
- Modify: `OperationGuidance_new/Views/WorkplaceMissionView_SCII_XT.cs:354-385` (delete SendCheckToMES)

- [ ] **Step 1: Remove SendCheckToMES call from point inspection path**

In `TerminateMission`, change the point inspection branch from:
```csharp
            if (isPointInspection) {
                await SendCheckToMES(_operationDataDTOs);
                _inBoundStationOk = false;
                await base.TerminateMission(status);
            }
```
To:
```csharp
            if (isPointInspection) {
                _inBoundStationOk = false;
                await base.TerminateMission(status);
            }
```

- [ ] **Step 2: Delete the SendCheckToMES method**

Delete the entire `SendCheckToMES` method (lines 354-385):
```csharp
        private async Task SendCheckToMES(List<OperationDataDTO> operationDataDTOs) {
            if (_operationDataDTOs != null && operationDataDTOs.Count > 0) {
                EquipmentCheckReq req = new() {
                    equipmentCheckInfos = new(),
                    employeeNumber = SystemUtils.UserInfo.account,
                    equipmentCode = _getEquipmentCode(),
                };

                EquipmentCheckReq.EquipmentCheckInfo checkInfo = new() {
                    attributeList = new(),
                };

                var value = BuildAttributeValues(operationDataDTOs);
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

- [ ] **Step 3: Clean up unused using directives**

Remove `using OperationGuidance_new.HttpObjects.Requests.SCII_XT;` if `EquipmentCheckReq` was its only usage. Check first:

```bash
grep -n "EquipmentCheckReq\|using OperationGuidance_new.HttpObjects.Requests.SCII_XT" OperationGuidance_new/Views/WorkplaceMissionView_SCII_XT.cs
```

`SCII_XT_InOrOutBoundStationReq` and `SCII_XT_BindProductDataReq` also come from this namespace, so the using is still needed. No cleanup required.

- [ ] **Step 4: Build and verify compilation**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: Build succeeds.

- [ ] **Step 5: Commit**

```bash
git add OperationGuidance_new/Views/WorkplaceMissionView_SCII_XT.cs
git commit -m "feat: remove MES upload from point inspection in SCII_XT

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```
