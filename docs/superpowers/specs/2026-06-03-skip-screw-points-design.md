# 跳过螺丝点位功能 — 设计文档

**日期**: 2026-06-03
**分支**: v1.6.x
**目标**: SCII 站点新增「跳过螺丝点位」开关，激活后跳过所有点位自动以 OK 完成任务

---

## 1. 数据库

### 1.1 新字段

`product_mission` 表新增 `skip_screw_points int(1) NULL`，类型仿照 `is_challenge_mission`。

### 1.2 迁移文件

三个新迁移文件，日期 `20260603`：

- `Database/sqls/modify_sqlserver_20260603.sql`
- `Database/sqls/modify_mysql_20260603.sql`
- `Database/sqls/modify_sqlite_20260603.sql`

#### SQL Server
```sql
ALTER TABLE [dbo].[product_mission] ADD [skip_screw_points] int NULL;
```

#### MySQL
```sql
ALTER TABLE `product_mission`
  ADD COLUMN `skip_screw_points` int(1) NULL AFTER `challenge_mission_id`;
```

#### SQLite
重建表，在 `challenge_mission_id` 之后新增 `skip_screw_points integer(1)` 列。

### 1.3 Resource 更新

- `Database/Resource.resx` — 新增 3 个 data entry（modify_sqlserver_20260603, modify_mysql_20260603, modify_sqlite_20260603），指向对应的 .sql 文件
- `Database/Resource.Designer.cs` — 自动生成的属性（如已有则无需手动修改）

### 1.4 Model / DTO

**`Models/ProductMission.cs`**:
```csharp
public int? skip_screw_points { get; set; } = (int) YesOrNo.NO;
```

**`Models/DTOs/ProductMissionDTO.cs`**:
```csharp
public int? skip_screw_points { get; set; }
```

---

## 2. UI — 任务详情弹窗

### 2.1 文件范围

仅修改 `OperationGuidance_new/Views/MissionEditionView_SCII.cs` 中的 `MissionDetailPopUpForm` 类。

### 2.2 新增开关

- **字段名**: `private ToggleButtonGroup _skipScrewPoints;`
- **标签**: "跳过螺丝点位"
- **位置**: 在 `_challengMission`（挑战对应任务）之后、`_maxNGNum`（最大NG数）之前
- **控件类型**: `ToggleButtonGroup`，与 `_isChallengeMission` / `_isFirstMission` 一致

### 2.3 改动清单

1. **构造函数**: 创建 `_skipScrewPoints`，设置 `Ratio = _boxRatio`，`ColumnSpan = _columnCount`
2. **`AfterShown()`**: 回填 `_missionDTO.skip_screw_points` 的值
3. **"确定"按钮 Click**: 将 `_skipScrewPoints.Checked` 写入 `_missionDTO.skip_screw_points`
4. **复制任务**（`_buttonDuplicate.Click`）: 复制 `skip_screw_points` 字段

---

## 3. 控制台任务列表过滤

### 3.1 文件

`OperationGuidance_service/Controllers/OperationGuidanceApis.cs` — `QueryProductMissionList()` 方法

### 3.2 当前逻辑 (line 414)

```csharp
if (noSide || (!isEditing && (hasNullImageSide || hasNullBoltSide))) {
    productMissionDTOs.Remove(missionDTO);
}
```

### 3.3 改动后

```csharp
bool skipScrewPoints = missionDTO.skip_screw_points == (int)YesOrNo.YES;
if (noSide || (!isEditing && !skipScrewPoints && (hasNullImageSide || hasNullBoltSide))) {
    productMissionDTOs.Remove(missionDTO);
}
```

`skip_screw_points = YES` 时，即使没有点位也保留在任务列表中。

---

## 4. 任务激活流程

### 4.1 文件

`OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs`

### 4.2 ValidationBeforeActivatingMission — 跳过校验

在 SCII **已有的** `ValidationBeforeActivatingMission()` 覆写方法**最顶部**加 3 行早返回，开关打开时跳过所有校验；原有批头计数器逻辑保持不变（开关关闭时走原路径）：

```csharp
protected override async Task<bool> ValidationBeforeActivatingMission() {
    // 新增：跳过螺丝点位开关打开时，跳过所有点位相关校验
    if (_mission.skip_screw_points == (int)YesOrNo.YES) {
        return true;
    }
    // 以下为原有逻辑不变
    if (await base.ValidationBeforeActivatingMission()) {
        // 批头计数器检查...
    }
    return false;
}
```

跳过的校验项：
- 站点/工具/力臂配置检查（基类）
- 排列机组 IO 盒检查（基类）
- 套筒选择器 IO 盒检查（基类）
- SCII 特有的批头计数器检查

### 4.3 ActivateMission — 快速通道直通 OK

覆写方法，开关打开时走简化流程：

```csharp
public override async void ActivateMission() {
    if (_mission.skip_screw_points == (int)YesOrNo.YES) {
        // 清理后台任务
        _backgroundTaskCts.ForEach(cts => { cts.Cancel(); cts.Dispose(); });
        _backgroundTaskCts.Clear();
        _activeMissionCts.Cancel();
        _activeMissionCts.Dispose();
        _activeMissionCts = new CancellationTokenSource();

        PrepareBeforeActivatingMission();       // ✓ 重置状态、side、变量
        _arrangerNeeded = false;                // 防御性重置，跳过 Validation 未执行这两个赋值
        _setterSelectorNeeded = false;
        _activated = true;
        await ActionAfterActivatingMission();   // ✓ 创建 mission_record

        _missionRecord.mission_result = (int)TighteningStatus.OK;
        _apis.AddOrUpdateMissionRecord(new(_missionRecord));
        TerminateMission(WorkplaceProcessStatus.FINISHED_OK);
        return;
    }
    base.ActivateMission();
}
```

执行的初始化逻辑：
- `PrepareBeforeActivatingMission()` — 重置 side index、locating、拧紧状态、NG 原因、所有点位状态
- `ActionAfterActivatingMission()` — 创建 `mission_record`、设置 `product_batch`、清空 DataGridView

跳过的逻辑：
- `ValidationBeforeActivatingMission()` — 所有点位/工具校验
- `InitializeBeforeActivatingMission()` — 切换到第一个点位
- 所有后台点位循环任务（拧紧数据接收、排列机、套筒选择器）

### 4.4 ActivateMissionAutomatically — 禁止自动循环

在 SCII 已有的 `ActivateMissionAutomatically()` 覆写最顶部加早返回，防止 OK 后自动重新激活：

```csharp
protected override async void ActivateMissionAutomatically() {
    if (_mission.skip_screw_points == (int)YesOrNo.YES) {
        return;  // 跳过螺丝点位的任务不自动循环，需手动激活
    }
    // ... 原有逻辑不变
}
```

基类 `TerminateMission()` 末尾的自动激活逻辑不变 — 它正常调用，SCII 覆写直接返回。

---

## 5. 改动文件总结

| 文件 | 改动 |
|---|---|
| `Database/sqls/modify_sqlserver_20260603.sql` | **新建** — ALTER TABLE ADD skip_screw_points |
| `Database/sqls/modify_mysql_20260603.sql` | **新建** — ALTER TABLE ADD COLUMN skip_screw_points |
| `Database/sqls/modify_sqlite_20260603.sql` | **新建** — 重建表含 skip_screw_points |
| `Database/Resource.resx` | 修改 — 3 个 data entry |
| `Database/Resource.Designer.cs` | 修改 — 3 个 property（auto-generated） |
| `Models/ProductMission.cs` | 修改 — 加字段 |
| `Models/DTOs/ProductMissionDTO.cs` | 修改 — 加字段 |
| `Controllers/OperationGuidanceApis.cs` | 修改 — QueryProductMissionList 过滤逻辑 |
| `Views/MissionEditionView_SCII.cs` | 修改 — MissionDetailPopUpForm 新开关 + 保存/回填/复制逻辑 |
| `Views/WorkplaceMissionView_SCII.cs` | 修改 — ActivateMission + ValidationBeforeActivatingMission 覆写 |
