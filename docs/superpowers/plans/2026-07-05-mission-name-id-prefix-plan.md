# 任务名称自动 ID 前缀 — 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 任务保存时自动在名称前添加 `{id} - ` 前缀，编辑框中不显示此前缀。

**Architecture:** 新增 `MissionNameHelper` 静态工具类提供 `StripPrefix`/`ApplyPrefix` 两个方法。在两个编辑视图的 4 个加载点调用 `StripPrefix`，4 个保存点（保存按钮 ×2 + 复制按钮 ×2）调用 `ApplyPrefix` 并处理新建任务 ID 反向更新。

**Tech Stack:** C#, WinForms, .NET

## Global Constraints

- 前缀格式: `{id} - `（无方括号），如 `123 - 拧紧任务A`
- id ≤ 0 时（新建任务）不做前缀处理
- `ApplyPrefix` 须幂等（已有前缀不重复添加）
- 改动仅限 `MissionEditionView.cs` 和 `MissionEditionView_SCII.cs`，两者逻辑对称
- 详情弹窗确认后不 ApplyPrefix，由主保存按钮统一处理

---

### Task 1: 创建 MissionNameHelper 工具类

**Files:**
- Create: `OperationGuidance_new/Utils/MissionNameHelper.cs`

**Interfaces:**
- Produces: `MissionNameHelper.StripPrefix(string name, int id)` → `string`, `MissionNameHelper.ApplyPrefix(string name, int id)` → `string`

- [ ] **Step 1: 创建文件并编写实现**

```csharp
namespace OperationGuidance_new.Utils
{
    public static class MissionNameHelper
    {
        public static string MakePrefix(int id) => $"{id} - ";

        /// <summary>加载显示时去掉前缀。id ≤ 0 或名称为空时原样返回。</summary>
        public static string StripPrefix(string name, int id)
        {
            if (id <= 0 || string.IsNullOrEmpty(name)) return name;
            string prefix = MakePrefix(id);
            return name.StartsWith(prefix) ? name.Substring(prefix.Length) : name;
        }

        /// <summary>保存时加上前缀。id ≤ 0、名称为空、或已有前缀时原样返回（幂等）。</summary>
        public static string ApplyPrefix(string name, int id)
        {
            if (id <= 0 || string.IsNullOrEmpty(name)) return name;
            string prefix = MakePrefix(id);
            return name.StartsWith(prefix) ? name : prefix + name;
        }
    }
}
```

- [ ] **Step 2: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 3: 提交**

```bash
git add OperationGuidance_new/Utils/MissionNameHelper.cs
git commit -m "feat: add MissionNameHelper for auto ID prefix on task names"
```

---

### Task 2: 修改 MissionEditionView.cs

**Files:**
- Modify: `OperationGuidance_new/Views/MissionEditionView.cs`
  - 主界面加载 (行 199)、详情弹窗回填 (行 1448)、保存按钮 (行 294-321)、复制按钮 (行 371-461)

**Interfaces:**
- Consumes: `MissionNameHelper.StripPrefix(string, int)`, `MissionNameHelper.ApplyPrefix(string, int)`

- [ ] **Step 1: 主界面加载名称时去掉前缀（行 199）**

将：
```csharp
missionNameBox.Text = _missionDTO.name;
```
改为：
```csharp
missionNameBox.Text = MissionNameHelper.StripPrefix(_missionDTO.name, _missionDTO.id);
```

- [ ] **Step 2: 详情弹窗回填时去掉前缀（行 1448）**

将：
```csharp
_missionName.SetValue(0, missionDTO.name);
```
改为：
```csharp
_missionName.SetValue(0, MissionNameHelper.StripPrefix(missionDTO.name, missionDTO.id));
```

- [ ] **Step 3: 保存按钮 — 加前缀 + 新建任务反向更新（行 294-321）**

将保存按钮 Click 处理器改为（新增行标记 `// NEW`）：

```csharp
_buttonSave.Click += (sender, eventArgs) => {
    _currentProductImageFile.SaveSideInfo();

    // NEW: 保存前确保名称带前缀
    _missionDTO.name = MissionNameHelper.ApplyPrefix(_missionDTO.name, _missionDTO.id);

    List<ProductMissionDTO> allMissions = _apis.QueryProductMissions(new(SystemUtils.MacAddressesDTO.id) { Role = SystemUtils.GetRoleNameByUserId(SystemUtils.LoggedUserId) }).ProductMissionsDTOs;
    if (allMissions.Any(m => m.name == _missionDTO.name && m.id != _missionDTO.id)) {
        WidgetUtils.ShowWarningPopUp("任务名称已存在，请修改后再保存");
        return;
    }
    // Store to database
    int oldId = _missionDTO.id;  // NEW: 记录保存前 ID，判断是否新建任务
    AddOrUpdateProductMissionReq req = new(_missionDTO);
    AddOrUpdateProductMissionRsp rsp = _apis.AddOrUpdateProductMission(req);
    if (rsp.RsponseCode == HttpResponseCode.OK) {
        Modified = false;
        _missionDTO = rsp.ProductMissionDTO;

        // NEW: 新建任务首次保存成功后，用真实 ID 更新前缀
        if (oldId <= 0 && _missionDTO.id > 0) {
            _missionDTO.name = MissionNameHelper.ApplyPrefix(_missionDTO.name, _missionDTO.id);
            AddOrUpdateProductMissionReq updateReq = new(_missionDTO);
            AddOrUpdateProductMissionRsp updateRsp = _apis.AddOrUpdateProductMission(updateReq);
            if (updateRsp.RsponseCode == HttpResponseCode.OK) {
                _missionDTO = updateRsp.ProductMissionDTO;
            }
        }

        // 数据保存成功后，保存图片到本地（需要循环保存每一个side的图片）
        foreach (SideButton sideBtn in _sideButtons) {
            MainUtils.SaveProductImage(sideBtn.ProductImageFileNew.Image, sideBtn.ProductImageFileNew.ImageFileName);
            ProductImageCache.Invalidate(sideBtn.ProductImageFileNew.ImageFileName);
        }
        MessageBox.Show(null, "保存成功！", "保存任务", MessageBoxButtons.OK, MessageBoxIcon.Information);
        // 保存后触发事件
        _parentView.MissionSaved?.Invoke(_missionDTO.id, _missionDTO);
        // 保存后跳转至任务列表界面
        WidgetUtils.GetChildMenu(101).TriggerClick(EventArgs.Empty);
        Dispose();
    } else {
        MessageBox.Show(null, "保存失败！错误信息：" + rsp.RsponseMessage, "保存任务", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
};
```

- [ ] **Step 4: 复制按钮 — 源名称去前缀 + 保存后反向更新（行 371-461）**

复制 Click 处理器中修改两处：

**4a. 构造复制名称时去掉源前缀（行 376）：**

将：
```csharp
name = _missionDTO.name + "_copy",
```
改为：
```csharp
name = MissionNameHelper.StripPrefix(_missionDTO.name, _missionDTO.id) + "_copy",
```

**4b. 保存成功后新增 ID 反向更新（行 449 的 success 分支内，`_missionDTO = rsp.ProductMissionDTO;` 之后）：**

在 `_missionDTO = rsp.ProductMissionDTO;` 后插入：
```csharp
// NEW: 复制的新任务首次保存成功后，用真实 ID 更新前缀
if (_missionDTO.id > 0) {
    _missionDTO.name = MissionNameHelper.ApplyPrefix(_missionDTO.name, _missionDTO.id);
    AddOrUpdateProductMissionReq updateReq = new(_missionDTO);
    AddOrUpdateProductMissionRsp updateRsp = _apis.AddOrUpdateProductMission(updateReq);
    if (updateRsp.RsponseCode == HttpResponseCode.OK) {
        _missionDTO = updateRsp.ProductMissionDTO;
    }
}
```

- [ ] **Step 5: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 6: 提交**

```bash
git add OperationGuidance_new/Views/MissionEditionView.cs
git commit -m "feat: add ID prefix logic to MissionEditionView save, copy and display"
```

---

### Task 3: 修改 MissionEditionView_SCII.cs

**Files:**
- Modify: `OperationGuidance_new/Views/MissionEditionView_SCII.cs`
  - 主界面加载 (行 200)、详情弹窗回填 (行 1798)、保存按钮 (行 485-528)、复制按钮 (行 577-667)

**Interfaces:**
- Consumes: `MissionNameHelper.StripPrefix(string, int)`, `MissionNameHelper.ApplyPrefix(string, int)`

- [ ] **Step 1: 主界面加载名称时去掉前缀（行 200）**

将：
```csharp
missionNameBox.Text = _missionDTO.name;
```
改为：
```csharp
missionNameBox.Text = MissionNameHelper.StripPrefix(_missionDTO.name, _missionDTO.id);
```

- [ ] **Step 2: 详情弹窗回填时去掉前缀（行 1798）**

将：
```csharp
_missionName.SetValue(0, _missionDTO.name);
```
改为：
```csharp
_missionName.SetValue(0, MissionNameHelper.StripPrefix(_missionDTO.name, _missionDTO.id));
```

- [ ] **Step 3: 保存按钮 — 加前缀 + 新建任务反向更新（行 485-528）**

将保存按钮 Click 处理器改为：

```csharp
_buttonSave.Click += (sender, eventArgs) => {
    _currentProductImageFile.SaveSideInfo();

    // NEW: 保存前确保名称带前缀
    _missionDTO.name = MissionNameHelper.ApplyPrefix(_missionDTO.name, _missionDTO.id);

    List<ProductMissionDTO> allMissions = _apis.QueryProductMissions(new(SystemUtils.MacAddressesDTO.id) { Role = SystemUtils.GetRoleNameByUserId(SystemUtils.LoggedUserId) }).ProductMissionsDTOs;
    if (allMissions.Any(m => m.name == _missionDTO.name && m.id != _missionDTO.id)) {
        WidgetUtils.ShowWarningPopUp("任务名称已存在，请修改后再保存");
        return;
    }

    // 跳过螺丝点位时，必须至少配置一个点位以获取站点信息
    if (_missionDTO.skip_screw_points == (int)YesOrNo.YES) {
        bool hasAnyBolt = _sideButtons.Count > 0 && _sideButtons.Any(side =>
            side.BoltButtons != null && side.BoltButtons.Values.Any(bolts => bolts.Count > 0));
        if (!hasAnyBolt) {
            WidgetUtils.ShowWarningPopUp("已开启\"跳过螺丝点位\"，但未配置任何螺丝点位。请至少添加一个产品面及点位以确定站点信息");
            return;
        }
    }

    // Store to database
    int oldId = _missionDTO.id;  // NEW: 记录保存前 ID
    AddOrUpdateProductMissionReq req = new(_missionDTO);
    AddOrUpdateProductMissionRsp rsp = _apis.AddOrUpdateProductMission(req);
    if (rsp.RsponseCode == HttpResponseCode.OK) {
        // Save screw bit counters
        _screwBitCounterDTOs.ForEach(dto => _apis.AddOrUpdateScrewBitCounter(new(dto)));

        Modified = false;
        _missionDTO = rsp.ProductMissionDTO;

        // NEW: 新建任务首次保存成功后，用真实 ID 更新前缀
        if (oldId <= 0 && _missionDTO.id > 0) {
            _missionDTO.name = MissionNameHelper.ApplyPrefix(_missionDTO.name, _missionDTO.id);
            AddOrUpdateProductMissionReq updateReq = new(_missionDTO);
            AddOrUpdateProductMissionRsp updateRsp = _apis.AddOrUpdateProductMission(updateReq);
            if (updateRsp.RsponseCode == HttpResponseCode.OK) {
                _missionDTO = updateRsp.ProductMissionDTO;
            }
        }

        // 数据保存成功后，保存图片到本地（需要循环保存每一个side的图片）
        foreach (SideButton sideBtn in _sideButtons) {
            MainUtils.SaveProductImage(sideBtn.ProductImageFileNew.Image, sideBtn.ProductImageFileNew.ImageFileName);
            ProductImageCache.Invalidate(sideBtn.ProductImageFileNew.ImageFileName);
        }
        MessageBox.Show(null, "保存成功！", "保存任务", MessageBoxButtons.OK, MessageBoxIcon.Information);
        _parentView.MissionSaved?.Invoke(_missionDTO.id, _missionDTO);

        // 保存后跳转至任务列表界面
        WidgetUtils.GetChildMenu(101).TriggerClick(EventArgs.Empty);
        Dispose();
    } else {
        MessageBox.Show(null, "保存失败！错误信息：" + rsp.RsponseMessage, "保存任务", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
};
```

- [ ] **Step 4: 复制按钮 — 源名称去前缀 + 保存后反向更新（行 577-667）**

复制 Click 处理器中修改两处：

**4a. 构造复制名称时去掉源前缀（行 582）：**

将：
```csharp
name = _missionDTO.name + "_copy",
```
改为：
```csharp
name = MissionNameHelper.StripPrefix(_missionDTO.name, _missionDTO.id) + "_copy",
```

**4b. 保存成功后新增 ID 反向更新（行 658 的 success 分支内，`_missionDTO = rsp.ProductMissionDTO;` 之后）：**

在 `_missionDTO = rsp.ProductMissionDTO;` 后插入：
```csharp
// NEW: 复制的新任务首次保存成功后，用真实 ID 更新前缀
if (_missionDTO.id > 0) {
    _missionDTO.name = MissionNameHelper.ApplyPrefix(_missionDTO.name, _missionDTO.id);
    AddOrUpdateProductMissionReq updateReq = new(_missionDTO);
    AddOrUpdateProductMissionRsp updateRsp = _apis.AddOrUpdateProductMission(updateReq);
    if (updateRsp.RsponseCode == HttpResponseCode.OK) {
        _missionDTO = updateRsp.ProductMissionDTO;
    }
}
```

- [ ] **Step 5: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 6: 提交**

```bash
git add OperationGuidance_new/Views/MissionEditionView_SCII.cs
git commit -m "feat: add ID prefix logic to MissionEditionView_SCII save, copy and display"
```
