# 前道工序链追溯码重码排除 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 产品码重码检查时排除前道工序链的记录，避免正常流转被误判为返工。

**Architecture:** 客户端在 `GetBarCodeMatchingRules` 中利用已有的 `QueryProductMissions` 调用构建前道工序链缓存（`Dictionary<int, HashSet<int>>`），公开 `GetPredecessorChainMissionIds` 方法。API 层 `CheckIfBarCodeExistsInMissionRecordReq` 新增 `ExcludeMissionIds` 属性，SQL 拼接 `NOT IN` 排除。

**Tech Stack:** C# WinForms, SQLite/MySQL

---

### Task 1: API Request DTO 新增 ExcludeMissionIds

**Files:**
- Modify: `OperationGuidance_service/Models/Requests/CheckIfBarCodeExistsInMissionRecordReq.cs`

- [ ] **Step 1: Add `ExcludeMissionIds` property**

Read the file (lines 1-15), then add the property after `MissionResult`:

```csharp
// After line 9 (MissionResult property):
public List<int>? ExcludeMissionIds { get; set; }
```

- [ ] **Step 2: Build to verify compilation**

Run: `dotnet build OperationGuidance_service/OperationGuidance_service.csproj`

Expected: Build succeeds (new property has no consumers yet, no breakage).

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_service/Models/Requests/CheckIfBarCodeExistsInMissionRecordReq.cs
git commit -m "feat: add ExcludeMissionIds to CheckIfBarCodeExistsInMissionRecordReq"
```

---

### Task 2: API SQL 拼接 NOT IN 排除条件

**Files:**
- Modify: `OperationGuidance_service/Controllers/OperationGuidanceApis.cs:955-959`

- [ ] **Step 1: Add NOT IN clause**

In `CheckIfBarCodeExistsInMissionRecord` method, insert after line 959 (closing brace of `PartsBarCode` condition):

```csharp
// Insert after line 959:
if (req.ExcludeMissionIds != null && req.ExcludeMissionIds.Count > 0) {
    sql += $" and mission_id not in ({string.Join(",", req.ExcludeMissionIds)})";
}
```

Note: `ExcludeMissionIds` values come from internal `predecessor_mission_id` chain — all ints, no SQL injection risk.

- [ ] **Step 2: Build to verify compilation**

Run: `dotnet build OperationGuidance_service/OperationGuidance_service.csproj`

Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_service/Controllers/OperationGuidanceApis.cs
git commit -m "feat: add NOT IN exclusion for predecessor missions in barcode redo check"
```

---

### Task 3: AWorkplaceContentPanel 添加字段和公开方法

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs`

- [ ] **Step 1: Add `_predecessorChains` field**

Add after line 123 (`_barcodeRelatedDone`):

```csharp
// 前道工序链缓存: mission_id → 所有祖先任务ID集合
protected Dictionary<int, HashSet<int>> _predecessorChains = new();
```

- [ ] **Step 2: Add `GetPredecessorChainMissionIds` public method**

Add a new method anywhere in the `#region Methods` section of the class (e.g., after existing barcode-related methods or near the bottom of the region):

```csharp
public List<int> GetPredecessorChainMissionIds(int missionId) {
    return _predecessorChains.TryGetValue(missionId, out var ids) ? ids.ToList() : new();
}
```

Note: `System.Linq` is already imported at the top of the file for `.ToList()`.

- [ ] **Step 3: Build to verify compilation**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`

Expected: Build succeeds (field and method added but not yet called).

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs
git commit -m "feat: add predecessor chain cache field and accessor method"
```

---

### Task 4: GetBarCodeMatchingRules 中构建链缓存

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs:788-802`

- [ ] **Step 1: Replace the mission ID extraction with chain building**

Replace lines 787-802:

```csharp
// Old (lines 787-802):
// 检查匹配规则中所对应的任务是否还存在
List<int> missionIds = _apis.QueryProductMissions(new(SystemUtils.MacAddressesDTO.id) { Role = SystemUtils.GetRoleNameByUserId(SystemUtils.LoggedUserId) }).ProductMissionsDTOs.Select(m => m.id).ToList();
Dictionary<int, List<BarCodeMatchingRuleDTO>>.Enumerator productCodes = _productBarCodeMatchingRules.GetEnumerator();
while (productCodes.MoveNext()) {
    int currId = productCodes.Current.Key;
    if (!missionIds.Contains(currId)) {
        _productBarCodeMatchingRules.Remove(currId);
    }
}
Dictionary<int, List<BarCodeMatchingRuleDTO>>.Enumerator partsCodes = _partsBarCodeMatchingRules.GetEnumerator();
while (partsCodes.MoveNext()) {
    int currId = partsCodes.Current.Key;
    if (!missionIds.Contains(currId)) {
        _partsBarCodeMatchingRules.Remove(currId);
    }
}
```

New:

```csharp
// 检查匹配规则中所对应的任务是否还存在，同时构建前道工序链缓存
List<ProductMissionDTO> missions = _apis.QueryProductMissions(new(SystemUtils.MacAddressesDTO.id) { Role = SystemUtils.GetRoleNameByUserId(SystemUtils.LoggedUserId) }).ProductMissionsDTOs;
List<int> missionIds = missions.Select(m => m.id).ToList();

// 构建前道工序链缓存
_predecessorChains.Clear();
var predecessorMap = missions.ToDictionary(m => m.id, m => m.predecessor_mission_id);
foreach (var m in missions) {
    var ancestors = new HashSet<int>();
    int? cursor = m.predecessor_mission_id;
    while (cursor != null && cursor > 0 && !ancestors.Contains(cursor.Value)) {
        ancestors.Add(cursor.Value);
        cursor = predecessorMap.ContainsKey(cursor.Value) ? predecessorMap[cursor.Value] : null;
    }
    _predecessorChains[m.id] = ancestors;
}

Dictionary<int, List<BarCodeMatchingRuleDTO>>.Enumerator productCodes = _productBarCodeMatchingRules.GetEnumerator();
while (productCodes.MoveNext()) {
    int currId = productCodes.Current.Key;
    if (!missionIds.Contains(currId)) {
        _productBarCodeMatchingRules.Remove(currId);
    }
}
Dictionary<int, List<BarCodeMatchingRuleDTO>>.Enumerator partsCodes = _partsBarCodeMatchingRules.GetEnumerator();
while (partsCodes.MoveNext()) {
    int currId = partsCodes.Current.Key;
    if (!missionIds.Contains(currId)) {
        _partsBarCodeMatchingRules.Remove(currId);
    }
}
```

Note: Original `missionIds` + cleanup loop logic is preserved verbatim. `System.Linq` already imported.

- [ ] **Step 2: Build to verify compilation**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`

Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs
git commit -m "feat: build predecessor chain cache in GetBarCodeMatchingRules"
```

---

### Task 5: ABarCodeInputPopUpForm 调用点传入排除列表

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/ABarCodeInputPopUpForm.cs:322`

- [ ] **Step 1: Replace the redo check with exclusion list**

At line 322, replace:

```csharp
if (checkPassed && _workplace._checkRedo && _workplace.Apis.CheckIfBarCodeExistsInMissionRecord(new(null) { ProductBarCode = barCode }).Yes) {
```

With:

```csharp
var redoReq = new CheckIfBarCodeExistsInMissionRecordReq(null) {
    ProductBarCode = barCode,
    ExcludeMissionIds = _workplace.GetPredecessorChainMissionIds(_mission.id),
};
if (checkPassed && _workplace._checkRedo && _workplace.Apis.CheckIfBarCodeExistsInMissionRecord(redoReq).Yes) {
```

- [ ] **Step 2: Build to verify compilation**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`

Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/AbstractViews/ABarCodeInputPopUpForm.cs
git commit -m "feat: exclude predecessor chain missions from product barcode redo check"
```

---

### 验证清单

- [ ] `dotnet build` 全项目通过
- [ ] 场景1：工序链 A→B→C，产品码 X 依次扫入 A、B、C，C 不触发返工提示
- [ ] 场景2：工序 C，产品码 X 已在 C 加工过一次（非前道工序），再次扫码 C 仍触发返工提示
- [ ] 场景3：无前置工序的任务，重码行为与之前完全一致
- [ ] 场景4：前置工序检查（`predecessor_mission_id` + `MissionResult = OK`）不受影响，逻辑独立
