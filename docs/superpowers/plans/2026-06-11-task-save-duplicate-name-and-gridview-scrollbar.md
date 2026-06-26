# 任务保存名称重复校验、条码规则名称去重、GridView滚动条修复 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在任务保存时校验名称不重复、条码规则管理界面任务名称去重、修复 GridView 滚动条底部截断

**Architecture:** 三个独立修改，无依赖关系，可并行执行。涉及客户端 WinForms 代码变更（Task 1-4, 6-7）和服务端 API 校验（Task 5）。

**Tech Stack:** C# WinForms (.NET 8+)，CustomLibrary 控件库

**Spec:** `docs/superpowers/specs/2026-06-11-task-save-duplicate-name-and-gridview-scrollbar-design.md`

---

### Task 1: 详情弹窗"确定"按钮 — 名称重复校验（MissionEditionView）

**Files:**
- Modify: `OperationGuidance_new/Views/MissionEditionView.cs:228-233`

- [ ] **Step 1: 在 `MissionEditionView.cs` 详情弹窗"确定" handler 中加入名称重复校验**

在第228-233行，`missionName` 非空校验之后、`maxNGNum` 校验之前，插入名称重复检查：

```csharp
                        string missionName = _detialPopUpForm.MissionName.GetTextBox(0).Box.Text;
                        if (string.IsNullOrEmpty(missionName)) {
                            check = false;
                            _detialPopUpForm.MissionName.GetTextBox(0).IsError = true;
                            warningMsg += $"{warningIndex++}. 任务名称不能为空\r\n";
                        }

                        // ========== 新增 start ==========
                        List<ProductMissionDTO> allMissions = _apis.QueryProductMissions(new(SystemUtils.MacAddressesDTO.id) { Role = SystemUtils.GetRoleNameByUserId(SystemUtils.LoggedUserId) }).ProductMissionsDTOs;
                        if (allMissions.Any(m => m.name == missionName && m.id != _missionDTO.id)) {
                            check = false;
                            _detialPopUpForm.MissionName.GetTextBox(0).IsError = true;
                            warningMsg += $"{warningIndex++}. 任务名称已存在，请修改\r\n";
                        }
                        // ========== 新增 end ==========

                        string maxNGNum = _detialPopUpForm.MaxNGNum.GetTextBox(0).Box.Text;
```

- [ ] **Step 2: 构建验证编译通过**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 3: 提交**

```bash
git add OperationGuidance_new/Views/MissionEditionView.cs
git commit -m "feat(mission): add duplicate name check in detail popup confirm

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2: 详情弹窗"确定"按钮 — 名称重复校验（MissionEditionView_SCII）

**Files:**
- Modify: `OperationGuidance_new/Views/MissionEditionView_SCII.cs:233-238`

- [ ] **Step 1: 在 `MissionEditionView_SCII.cs` 详情弹窗"确定" handler 中加入名称重复校验**

在第233-238行，`missionName` 非空校验之后、`maxNGNum` 校验之前，插入名称重复检查：

```csharp
                        string missionName = _detialPopUpForm.MissionName.GetTextBox(0).Box.Text;
                        if (string.IsNullOrEmpty(missionName)) {
                            check = false;
                            _detialPopUpForm.MissionName.GetTextBox(0).IsError = true;
                            warningMsg += $"{warningIndex++}. 任务名称不能为空\r\n";
                        }

                        // ========== 新增 start ==========
                        List<ProductMissionDTO> allMissions = _apis.QueryProductMissions(new(SystemUtils.MacAddressesDTO.id) { Role = SystemUtils.GetRoleNameByUserId(SystemUtils.LoggedUserId) }).ProductMissionsDTOs;
                        if (allMissions.Any(m => m.name == missionName && m.id != _missionDTO.id)) {
                            check = false;
                            _detialPopUpForm.MissionName.GetTextBox(0).IsError = true;
                            warningMsg += $"{warningIndex++}. 任务名称已存在，请修改\r\n";
                        }
                        // ========== 新增 end ==========

                        string maxNGNum = _detialPopUpForm.MaxNGNum.GetTextBox(0).Box.Text;
```

- [ ] **Step 2: 构建验证编译通过**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 3: 提交**

```bash
git add OperationGuidance_new/Views/MissionEditionView_SCII.cs
git commit -m "feat(mission): add duplicate name check in SCII detail popup confirm

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 3: 外层"保存"按钮 — 名称重复校验（MissionEditionView）

**Files:**
- Modify: `OperationGuidance_new/Views/MissionEditionView.cs:288-292`

- [ ] **Step 1: 在 `MissionEditionView.cs` 保存按钮 handler 中加入名称重复校验**

在第289行 `SaveSideInfo()` 之后、第291行 `AddOrUpdateProductMissionReq` 之前插入：

```csharp
                _buttonSave.Click += (sender, eventArgs) => {
                    _currentProductImageFile.SaveSideInfo();

                    // ========== 新增 start ==========
                    List<ProductMissionDTO> allMissions = _apis.QueryProductMissions(new(SystemUtils.MacAddressesDTO.id) { Role = SystemUtils.GetRoleNameByUserId(SystemUtils.LoggedUserId) }).ProductMissionsDTOs;
                    if (allMissions.Any(m => m.name == _missionDTO.name && m.id != _missionDTO.id)) {
                        WidgetUtils.ShowWarningPopUp("任务名称已存在，请修改后再保存");
                        return;
                    }
                    // ========== 新增 end ==========

                    // Store to database
                    AddOrUpdateProductMissionReq req = new(_missionDTO);
```

- [ ] **Step 2: 构建验证编译通过**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 3: 提交**

```bash
git add OperationGuidance_new/Views/MissionEditionView.cs
git commit -m "feat(mission): add duplicate name check before save button

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 4: 外层"保存"按钮 — 名称重复校验（MissionEditionView_SCII）

**Files:**
- Modify: `OperationGuidance_new/Views/MissionEditionView_SCII.cs:478-493`

- [ ] **Step 1: 在 `MissionEditionView_SCII.cs` 保存按钮 handler 中加入名称重复校验**

在第479行 `SaveSideInfo()` 之后、第482行螺丝点位校验之前插入：

```csharp
                _buttonSave.Click += (sender, eventArgs) => {
                    _currentProductImageFile.SaveSideInfo();

                    // ========== 新增 start ==========
                    List<ProductMissionDTO> allMissions = _apis.QueryProductMissions(new(SystemUtils.MacAddressesDTO.id) { Role = SystemUtils.GetRoleNameByUserId(SystemUtils.LoggedUserId) }).ProductMissionsDTOs;
                    if (allMissions.Any(m => m.name == _missionDTO.name && m.id != _missionDTO.id)) {
                        WidgetUtils.ShowWarningPopUp("任务名称已存在，请修改后再保存");
                        return;
                    }
                    // ========== 新增 end ==========

                    // 跳过螺丝点位时，必须至少配置一个点位以获取站点信息
                    if (_missionDTO.skip_screw_points == (int)YesOrNo.YES) {
```

- [ ] **Step 2: 构建验证编译通过**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 3: 提交**

```bash
git add OperationGuidance_new/Views/MissionEditionView_SCII.cs
git commit -m "feat(mission): add duplicate name check before save button in SCII

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 5: 服务端 `AddOrUpdateProductMission` — 名称重复校验

**Files:**
- Modify: `OperationGuidance_service/Controllers/OperationGuidanceApis.cs:488-490`

- [ ] **Step 1: 在 `AddOrUpdateProductMission` 中加入名称重复校验**

在第489行 `ObjectConverter` 之后、第490行 `InsertOrUpdate` 之前插入：

```csharp
                // 将请求中的数据转移到entity中
                CommonUtils.ObjectConverter<ProductMissionDTO, ProductMission>(missionDTOReq, mission);

                // ========== 新增 start ==========
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
                // ========== 新增 end ==========

                // 执行插入或者更新操作
                mission = _productMissionService.InsertOrUpdate(mission);
```

- [ ] **Step 2: 构建验证 API 项目编译通过**

```bash
dotnet build OperationGuidance_service/OperationGuidance_service.csproj
```

- [ ] **Step 3: 提交**

```bash
git add OperationGuidance_service/Controllers/OperationGuidanceApis.cs
git commit -m "feat(api): add server-side duplicate mission name check

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 6: 条码规则管理界面 — 任务名称去重

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/ABarCodeMatchingRuleManagementView.cs:92-98, 114-118`
- Modify: `OperationGuidance_new/Views/BarCodeMatchingRuleManagementView_SCII.cs:26-30`

- [ ] **Step 1: 在 `ABarCodeMatchingRuleManagementView` 中新增 `PopulateMissionComboBox` 共用方法**

在 `RefreshMissionOptions` 方法之前插入：

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

- [ ] **Step 2: 修改 `RefreshMissionOptions` 调用共用方法**

将第92-98行替换为：

```csharp
        protected void RefreshMissionOptions() {
            _missions = apis.QueryProductMissions(new(SystemUtils.MacAddressesDTO.id) { Role = SystemUtils.GetRoleNameByUserId(SystemUtils.LoggedUserId) }).ProductMissionsDTOs;
            _missionNameComboBox.ClearItem();
            PopulateMissionComboBox(_missionNameComboBox);
        }
```

- [ ] **Step 3: 修改基类 `OpenEditEntityPopUpForm` 调用共用方法**

将第116-118行的 `foreach (ProductMissionDTO mission in _missions) { missionName.AddItem(...); }` 替换为：

```csharp
            PopulateMissionComboBox(missionName);
```

- [ ] **Step 4: 修改 `BarCodeMatchingRuleManagementView_SCII.OpenEditEntityPopUpForm` 调用共用方法**

将 `BarCodeMatchingRuleManagementView_SCII.cs` 第28-30行的 `foreach (ProductMissionDTO mission in _missions) { missionName.AddItem(...); }` 替换为：

```csharp
            PopulateMissionComboBox(missionName);
```

- [ ] **Step 5: 构建验证编译通过**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 6: 提交**

```bash
git add OperationGuidance_new/Views/AbstractViews/ABarCodeMatchingRuleManagementView.cs OperationGuidance_new/Views/BarCodeMatchingRuleManagementView_SCII.cs
git commit -m "feat(barcode): deduplicate mission names in filter dropdowns

Extract PopulateMissionComboBox shared method for three call sites.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 7: GridView 滚动条底部修复

**Files:**
- Modify: `OperationGuidance_new/Views/ReusableWidgets/DataGridViewPanel.cs:791`

- [ ] **Step 1: 修改 `ResizeChildren` 中滚动条 Maximum 计算**

第791行，在 Maximum 上增加 `DisplayedRowCount` 余量：

```csharp
                _vScrollBar.Show();
                _vScrollBar.Maximum = Math.Max(0, _gridView.RowCount - 1 + _gridView.DisplayedRowCount(true));
                _vScrollBar.LargeChange = _gridView.DisplayedRowCount(true);
```

> 已有的 `Math.Clamp(rowIndex, 0, RowCount - 1)` 保护（第779行）确保不会越界。

- [ ] **Step 2: 构建验证编译通过**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 3: 提交**

```bash
git add OperationGuidance_new/Views/ReusableWidgets/DataGridViewPanel.cs
git commit -m "fix(gridview): add scroll margin so last row is fully visible

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## 执行顺序

需求独立，无依赖关系。推荐分批并行执行：

- **批次A（并行）:** Task 1 + Task 2 + Task 6 + Task 7（详情弹窗校验 + 去重 + 滚动条）
- **批次B（并行）:** Task 3 + Task 4 + Task 5（保存按钮校验 + 服务端校验）
