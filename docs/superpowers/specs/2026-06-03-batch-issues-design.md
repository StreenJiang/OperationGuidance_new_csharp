# 批量问题修复设计

**日期:** 2026-06-03
**状态:** 草稿
**分支:** v1.6.x

## 问题总览

| # | 问题 | 根因 | 修复方式 |
|---|------|------|----------|
| 1 | PSet 下发失败后重连重试间隔太短 | `CloseToTriggerReconnection()` 未设 `socketClient = null` | 加一行代码 |
| 2 | 跳过螺丝点位任务保存后没有站点信息 | `mission_record` 表无站点字段，依赖 operation_data JOIN | 加字段 + 写入逻辑 |
| 3 | 前置任务未完成误报 | 待排查（v2.1.x 修复不适用） | 添加诊断日志，暂不放 |
| 4 | 数据库脚本执行时无提示 | 缺少进度提示 | 非模态弹窗 |
| 5 | 导出字段配置点击 `Out of memory` | GDI+ 资源反序列化失败 + ResizeImage 泄漏 | dispose 旧图 |
| 6 | 配置保存后仍提示有未保存内容 | `SaveStorageSettings` 漏更新两个 original 值 | 补两行 |
| 7 | 任务列表缓存不同步 | `RefreshMissionBlocks` 仅比较 ID 序列 | 增加按 ID 精准刷新 |

---

## 1. PSet 重连重试修复

### 根因

`ToolTask.CloseToTriggerReconnection()` 调用 `socketClient?.Close()` 后未将 `socketClient` 置 null。

两个并发风险：
1. **RunTask finally 误关新连接**：旧 `RunTask()` 的 finally 块 `if (socketClient != null) { socketClient.Close(); socketClient = null; }` 可能在新 `ConnectToServer()` 创建新 socket 之后才执行，把新连接也关掉
2. **Connected 属性不可靠**：`Connected => socketClient != null && socketClient.Connected && !CloseConnectionManually`，不设 null 依赖 `Socket.Connected` 在 Close 后正确返回 false，但半开连接下行为不确定

### 修复

**文件:** `OperationGuidance_new/Tasks/ToolTask.cs`

```csharp
// 改前 (line 246-250)
public void CloseToTriggerReconnection() {
    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Closing connection to trigger reconnection...");
    _currentPSet = -1;
    socketClient?.Close();
}

// 改后
public void CloseToTriggerReconnection() {
    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Closing connection to trigger reconnection...");
    _currentPSet = -1;
    socketClient?.Close();
    socketClient = null;
}
```

### 不改动

- `CloseConnection()` — 已有 `socketClient = null`
- `ReconnectAndResendPset` — 保持不变
- `SendPSetAsync` — 保持不变

---

## 2. mission_record 添加站点信息

### 背景

当前站点信息仅通过 `operation_data` 表 JOIN 获取。跳过螺丝点位的任务无 operation_data，导致站点信息丢失。查询界面每次查 mission_record 都要 JOIN operation_data 拿站点，性能可优化。

### 设计思路

**核心原则**：从源头保证每个任务至少有一个螺丝点位，从而总能获取站点信息。

两步改动：
1. **保存时校验**：开启"跳过螺栓点位"时，检查是否存在至少一个点位，否则阻止保存
2. **激活时取第一个点位的站点**：写入 `mission_record` 新字段

### DB 变更

三个数据库各新增 migration SQL（`_2` 后缀避免与已执行的 `skip_screw_points` 冲突）。

**MySQL (`modify_mysql_20260603_2.sql`):**
```sql
ALTER TABLE `mission_record`
  ADD COLUMN `workstation_id` int(11) NULL AFTER `is_redo`,
  ADD COLUMN `workstation_name` varchar(200) NULL AFTER `workstation_id`;
```

**SQL Server (`modify_sqlserver_20260603_2.sql`):**
```sql
ALTER TABLE [dbo].[mission_record] ADD [workstation_id] int NULL;
ALTER TABLE [dbo].[mission_record] ADD [workstation_name] nvarchar(200) NULL;
```

**SQLite (`modify_sqlite_20260603_2.sql`):**

SQLite 不支持 `ALTER TABLE ADD COLUMN ... AFTER`，沿用现有 rebuild 模式。重建后需恢复全部索引。

```sql
ALTER TABLE "mission_record" RENAME TO "_mission_record_old_20260603_2";

CREATE TABLE "mission_record" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "mission_id" integer NOT NULL,
  "product_batch" text(64) NOT NULL,
  "product_bar_code" text(512),
  "parts_bar_code" text(512),
  "mission_result" integer(2) NOT NULL,
  "is_redo" integer(2) NOT NULL,
  "workstation_id" integer,
  "workstation_name" text(200),
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);

INSERT INTO "sqlite_sequence" (name, seq) VALUES ('mission_record', (SELECT COALESCE(MAX(id), 0) FROM "_mission_record_old_20260603_2"));

INSERT INTO "mission_record" ("id", "mission_id", "product_batch", "product_bar_code", "parts_bar_code", "mission_result", "is_redo", "user_id", "deleted", "creator", "modifier", "create_time", "modify_time")
SELECT "id", "mission_id", "product_batch", "product_bar_code", "parts_bar_code", "mission_result", "is_redo", "user_id", "deleted", "creator", "modifier", "create_time", "modify_time"
FROM "_mission_record_old_20260603_2";

DROP TABLE "_mission_record_old_20260603_2";

-- 重建索引（DROP TABLE 会导致索引丢失）
CREATE INDEX IF NOT EXISTS idx_mission_record_mission_id_parts ON mission_record(mission_id, parts_bar_code);
CREATE INDEX IF NOT EXISTS idx_mission_record_deleted_time_mission ON mission_record(deleted, create_time, mission_id);
CREATE INDEX ix_mr_mission ON mission_record(mission_id, deleted);
CREATE INDEX ix_mr_ct ON mission_record(create_time, deleted);
CREATE INDEX IF NOT EXISTS ix_mr_deleted_user ON mission_record(deleted, user_id, id);
```

### 模型变更

**`MissionRecord.cs`:**
```csharp
public int? workstation_id { get; set; }
public string? workstation_name { get; set; }
```

**`MissionRecordDTO.cs`:** 将 `workstation_id`、`workstation_name` 的注释从 "JOIN 填充的冗余字段" 改为普通字段。

### 保存时校验

**文件:** `OperationGuidance_new/Views/MissionEditionView_SCII.cs`

在"确定"按钮的校验逻辑中（line 228-400，`check` 变量累积校验结果），于其他校验之后、`if (!check)` 之前添加：

```csharp
// 跳过螺丝点位时，必须至少配置一个点位以获取站点信息
if (check && _detialPopUpForm.SkipScrewPoints.Checked) {
    bool hasAnyBolt = _sideButtons.Count > 0 && _sideButtons.Any(side =>
        side.BoltButtons != null && side.BoltButtons.Values.Any(bolts => bolts.Count > 0));
    if (!hasAnyBolt) {
        check = false;
        warningMsg += $"{warningIndex++}. 已开启"跳过螺丝点位"，但未配置任何螺丝点位。请至少添加一个产品面及点位以确定站点信息\r\n";
    }
}
```

### 写入逻辑

两个创建 `_missionRecord` 的位置都需要填入站点信息：

#### (A) `AWorkplaceContentPanel.ActionAfterActivatingMission()` (line 1548)

从已加载的 `_allBolts` 中取第一个点位的站点：

```csharp
// 从任务的螺栓点位中获取站点信息
int? workstationId = _allBolts.Values
    .SelectMany(b => b)
    .Select(b => b.BoltDTO.workstation_id)
    .FirstOrDefault();
string? workstationName = workstationId != null
    ? _workstationsDTOs.Single(dto => dto.id == workstationId.Value).name
    : null;

_missionRecord = new() {
    mission_id = _mission.id,
    product_bar_code = _barCodeObj.ProductBarCode,
    parts_bar_code = string.Join(",", _barCodeObj.PartsBarCodes),
    mission_result = (int)TighteningStatus.NG,
    is_redo = _isRedo,
    workstation_id = workstationId,
    workstation_name = workstationName,
};
```

#### (B) `WorkplaceContentPanel_SCII.ActivateMission()` skip 路径 (line 1173)

保存时已确保至少有一个点位，激活时查询该点位的站点：

```csharp
// 查询第一个螺丝点位的站点信息（保存时已确保至少存在一个点位）
int? workstationId = null;
string? workstationName = null;
var detailRsp = _apis.QueryProductMissionDetail(new(_mission.id));
var firstBolt = detailRsp.ProductMissionDTO?.ProductSides
    ?.SelectMany(s => s.Bolts ?? new())
    .FirstOrDefault();
if (firstBolt?.workstation_id != null) {
    workstationId = firstBolt.workstation_id;
    workstationName = _workstationsDTOs
        .FirstOrDefault(dto => dto.id == workstationId.Value)?.name;
}

_missionRecord = new() {
    mission_id = _mission.id,
    product_bar_code = _barCodeObj.ProductBarCode,
    parts_bar_code = string.Join(",", _barCodeObj.PartsBarCodes),
    mission_result = (int)TighteningStatus.OK,
    is_redo = _isRedo,
    product_batch = _productBatch.GetTextBox(0).Box.Text,
    workstation_id = workstationId,
    workstation_name = workstationName,
};
```

### 查询优化（顺带）

`MissionRecordDTO` 已有 `workstation_id`、`workstation_name`，之前通过 `GetWorkstationInfoByMissionRecordIds` → `OperationDataService` 二次查询填充。现在 DTO 映射直接取表字段，**可移除或简化**该二次查询路径。

### 不改动

- `operation_data` 表结构不变
- `CheckIfBarCodeExistsInMissionRecord` API 不受影响
- 现有查询界面代码兼容（DTO 字段不变）

---

## 3. 前置任务未完成（暂放）

v2.1.x 的 `851e13f` 和 `0178673` 经分析不适用于 v1.6.x：
- `851e13f` 修复的 `ShowWrongBarcodeGate` 在 v1.6.x 不存在
- `0178673` 修复的跨任务重码检查 v1.6.x 是任务内检查（`MissionId = mission.id`）

建议后续在 `ABarCodeInputPopUpForm.cs` 前置任务检查路径（line 301-327）添加诊断日志，追踪 `predecessor_mission_id`、查询 SQL 返回值，定位数据丢失或保存错位的具体场景。

---

## 4. 数据库脚本执行非模态弹窗

### 现状

- 三个数据库 connector（`SQLiteConnector`、`MySqlConnector`、`SqlServerConnector`）各自在 `GetDbConnection()` 中检测并执行未执行的 migration 脚本
- 脚本执行逻辑：查询 `sql_execute_record` → 对比 `modify_*` 资源文件 → 逐个 `ExecuteNonQuery()` → 插入执行记录
- `MainUtils.cs` 已有模态弹窗包裹整个 `GetConnection()` 调用，但该弹窗是整个 DB 连接过程的，不改动它

### 需求

当检测到有未执行脚本并即将开始执行时，额外弹出一个**非模态**提示窗，告知用户"正在执行数据库升级脚本，请勿关闭程序"，脚本执行完毕后关闭。三个数据库类型（SQLite / MySQL / SQL Server）都要覆盖。

### 方案

**架构**：service 层不能直接引用 WinForms，通过静态回调将"检测到脚本"事件传递给客户端，由客户端负责弹窗。

**文件改动：**

#### (A) `OperationGuidance_service/Database/DbConnector.cs` — 新增静态回调

```csharp
/// <summary>
/// 当检测到待执行的数据库脚本时触发。参数为脚本文件名列表。
/// 客户端可订阅此回调以显示非模态提示窗。
/// </summary>
public static Action<List<string>>? BeforeScriptsExecution;
```

#### (B) 三个 Connector 的 `GetDbConnection()` — 触发回调

以 SQLite 为例（`SQLiteConnector.cs:79-96`），在检测到未执行脚本后、开始执行前触发：

```csharp
// 收集本轮待执行的脚本
List<string> pendingScripts = fileNames
    .Where(f => f.Contains(sqlScriptPrefix) && !executedFileNames.Contains(f))
    .ToList();

if (pendingScripts.Count > 0) {
    DbConnector.BeforeScriptsExecution?.Invoke(pendingScripts);
}

// 原有执行逻辑不变
foreach (string fileName in pendingScripts) {
    ...
    command.CommandText = fileText;
    command.ExecuteNonQuery();
    ...
}
```

MySQL / SQL Server connector 同理。

#### (C) `OperationGuidance_new/Utils/MainUtils.cs` — 订阅回调，弹非模态窗

在 `GetConnection()` 调用前注册回调（一次性订阅即可，`DbConnector` 是静态类）：

```csharp
DbConnector.BeforeScriptsExecution = (scriptNames) => {
    // 回调在后台线程执行，用 formPopup marshal 到 UI 线程
    // （此时 WidgetUtils.MainForm 尚未设置，但 formPopup 句柄已就绪）
    formPopup.BeginInvoke(() => {
        var scriptsPopup = new CustomPopUpForm {
            Title = "数据库升级",
        };
        scriptsPopup.AddLabel($"正在执行 {scriptNames.Count} 个数据库升级脚本，请勿关闭程序...");
        scriptsPopup.Show(); // 非模态
        scriptsPopup = scriptsPopup;
    });
};
```

在 `GetConnection()` 完成后关闭弹窗（紧接 `formPopup.Dispose()` 之后）：

```csharp
if (scriptsPopup != null && !scriptsPopup.IsDisposed) {
    scriptsPopup.Dispose();
    scriptsPopup = null;
}
```

### 要求

- **不改动**原有的 `formPopup` 模态弹窗逻辑
- 非模态：`Show()` 而非 `ShowDialog()`
- 弹窗不含交互按钮，仅提示文字
- 三个数据库类型全部覆盖

---

## 5. 导出字段配置 OOM 修复

### 根因

两处问题叠加：

1. **资源反序列化**：`Properties.Resources.direction_down` 在 `.resx` 中存储的 PNG 被 `DeserializingResourceReader` 反序列化时 GDI+ 抛 OOM（GDI+ 经典问题，并非真内存不足，而是位图格式/句柄问题）

2. **GDI+ 对象泄漏**：`MovableButton.OnMouseEnter`（line 746-747）每次 resize 图像但不 dispose 旧对象；`OnMouseLeave`（line 783-784）置 null 前也不 dispose

### 修复

**文件:** `OperationGuidance_new/Views/AbstractViews/AVariableSettingsView.cs` `MovableButton` 类

```csharp
// OnMouseEnter — resize 前 dispose 旧图
protected override void OnMouseEnter(EventArgs e) {
    base.OnMouseEnter(e);
    int btnSide = (int)(Height * .75);
    int margin = btnSide / 3;
    Size imageSize = new(btnSide, btnSide);
    Point imageDownLocation = new(Width - btnSide, (Height - btnSide) / 2);
    Point imageUpLocation = new(Width - btnSide * 2 - margin, (Height - btnSide) / 2);

    _upImageRect = new(imageUpLocation, imageSize);
    _downImageRect = new(imageDownLocation, imageSize);

    // 先创建新图，再 dispose 旧图（防止 ResizeImage 抛异常时留下已 dispose 的旧图）
    var newUp = WidgetUtils.ResizeImage(_upImage, imageSize);
    var newDown = WidgetUtils.ResizeImage(_downImage, imageSize);
    _upImageShowing?.Dispose();
    _downImageShowing?.Dispose();
    _upImageShowing = newUp;
    _downImageShowing = newDown;
}

// OnMouseLeave — null 前 dispose
protected override void OnMouseLeave(EventArgs e) {
    base.OnMouseLeave(e);
    _upImageRect = null;
    _downImageRect = null;
    _upImageShowing?.Dispose();
    _upImageShowing = null;
    _downImageShowing?.Dispose();
    _downImageShowing = null;
    Invalidate();
}
```

此外检查 `_upImage`/`_downImage` 是否在其他地方被意外 dispose（排查共享资源引用问题）。

---

## 6. 配置保存后脏标记修复

### 根因

`SaveStorageSettings()` 更新了 `_sortConfigOriginal`、`_sotragePathOriginal`、`_sotrageLooseningDataOriginal`，但遗漏了：
- `_enableExcelExportOriginal`
- `_enableTxtExportOriginal`

导致保存后 `CheckSavedFunc_detail()` 的检查 1、2 仍判定为有变更。

### 修复

**文件:** `OperationGuidance_new/Views/AbstractViews/AVariableSettingsView.cs`

```csharp
// SaveStorageSettings() line 446-448 之后追加
protected void SaveStorageSettings() {
    if (!_storagePanel.Visible) return;
    // ... 现有保存逻辑不变 ...

    ExportConfig.Instance.SetExcelExportEnabled(_enableExcelExportToggle.Checked);
    ExportConfig.Instance.SetTxtExportEnabled(_enableTxtExportToggle.Checked);
    ExportConfig.Instance.Reload();

    // 追加：更新 original 值
    _enableExcelExportOriginal = _enableExcelExportToggle.Checked;
    _enableTxtExportOriginal = _enableTxtExportToggle.Checked;
}
```

### 完整校验（CheckSavedFunc_detail 14 项 vs Save 函数对应关系）

| # | 检查项 | 更新位置 | 状态 |
|---|--------|----------|------|
| 1 | `_enableExcelExportToggle` vs `_enableExcelExportOriginal` | `SaveStorageSettings` | ❌→✅ |
| 2 | `_enableTxtExportToggle` vs `_enableTxtExportOriginal` | `SaveStorageSettings` | ❌→✅ |
| 3 | `_resolutionOptionsBox` vs `_resolutionOriginal` | `SaveSystemSettings:313` | ✅ |
| 4 | `_storagePathTextBox` vs `_sotragePathOriginal` | `SaveStorageSettings:443` | ✅ |
| 5 | `SortConfig` vs `_sortConfigOriginal` | `SaveStorageSettings:442` | ✅ |
| 6 | `_storeLooseningDataToggle` vs `_sotrageLooseningDataOriginal` | `SaveStorageSettings:444` | ✅ |
| 7 | `_enableArmLocatingToggle` vs `_enableArmLocatingOriginal` | `SaveMissionSettings:994` | ✅ |
| 8 | `_armLocatingAccuracyBox` vs `_armLocatingAccuracyOriginal` | `SaveMissionSettings:995` | ✅ |
| 9 | `_missionSelfLoopingModeToggle` vs `_missionSelfLoopingModeOriginal` | `SaveMissionSettings:997` | ✅ |
| 10 | `_autoLockToolToggle` vs `_autoLockToolOriginal` | `SaveMissionSettings:998` | ✅ |
| 11 | `_autoLaunchToggle` vs `_autoLaunchOriginal` | `SaveSystemSettings:318` | ✅ |
| 12 | `_autoLoginToggle` vs `_autoLoginOriginal` | `SaveSystemSettings:323` | ✅ |
| 13 | `_logsRetentionDaysBox` vs `_logsRetentionDaysOriginal` | `SaveSystemSettings:331` | ✅ |
| 14 | `_usbScannerEnabledToggle` vs `_usbScannerEnabledOriginal` | `SaveMissionSettings:999` | ✅ |

---

## 7. 任务列表缓存精准刷新

### 根因

`MissionListPanel.RefreshMissionBlocks()` line 86：

```csharp
if (_missionDTOs.Count > 0 && missionDTOs.Select(m => m.id).SequenceEqual(_missionDTOs.Select(m => m.id)))
    return;
```

仅比较 ID 序列是否相同。任务名称、状态等属性变更时 ID 不变 → 跳过刷新。

### 修复

**文件:** `OperationGuidance_new/Views/ReusableWidgets/MissionListPanel.cs`

新增按 ID 精准刷新方法：

```csharp
/// <summary>
/// 按 mission_id 精准刷新单个任务块。适用场景：任务属性变更但 ID 列表不变。
/// </summary>
public void RefreshMissionBlockById(int missionId, ProductMissionDTO updatedMission, Action<int?>? blockClickAction, bool toggleBlock = false) {
    // 更新缓存数据
    int idx = _missionDTOs.FindIndex(m => m.id == missionId);
    if (idx >= 0) {
        _missionDTOs[idx] = updatedMission;
    }

    // 找到对应的 UI block 并重建
    var oldBlock = MissionBlocks.FirstOrDefault(b => b.Entity.id == missionId);
    if (oldBlock == null) return;

    int ctrlIndex = _contentPanel.MissionsTable.Controls.IndexOf((Control)oldBlock);
    oldBlock.Dispose();

    ProductMissionBlock<ProductMissionDTO> newBlock = new(
        updatedMission,
        null,
        Properties.Resources.image_choose,
        updatedMission.name,
        ColorConfigs.COLOR_MISSION_BLOCK_BORDER,
        ColorConfigs.COLOR_MISSION_BLOCK_BACKGROUND,
        ColorConfigs.COLOR_MISSION_BLOCK_IMAGE_BORDER
    ) {
        Parent = _contentPanel.MissionsTable,
    };
    newBlock.InnerButton.ToggledButton = toggleBlock;
    newBlock.InnerButton.MouseUp += (sender, eventArgs) => {
        // ... 复用现有 toggle + click 逻辑 ...
        if (blockClickAction != null) blockClickAction(newBlock.Entity.id);
    };

    _contentPanel.MissionsTable.Controls.Add(newBlock);
    _contentPanel.MissionsTable.Controls.SetChildIndex(newBlock, ctrlIndex);

    // 异步加载封面
    _ = LoadOneCoverAsync(newBlock, CancellationToken.None);
}
```

调用方（`MissionManagementView` / `MissionManagementView_SCII` 订阅 EditionView 事件）：

```csharp
// MissionEditionView / MissionEditionView_SCII 新增事件
public event Action<int, ProductMissionDTO?>? MissionSaved;

// 保存成功后触发
MissionSaved?.Invoke(_missionDTO.id, _missionDTO);

// 删除成功后触发（dto=null 表示删除）
MissionSaved?.Invoke(deletedMissionId, null);
```

```csharp
// MissionManagementView / MissionManagementView_SCII 订阅
_editionView.MissionSaved += (missionId, dto) => {
    if (dto != null) {
        _missionListPanel.RefreshMissionBlockById(missionId, dto, OpenEditionPageView);
    } else {
        // 删除：清缓存，后续 VisibleToTrue 触发全量刷新（ID 列表已变）
        _missionListPanel.InvalidateCache();
    }
};
```

同时新增缓存失效方法：

```csharp
/// <summary>
/// 清空缓存，强制下次 RefreshMissionBlocks 执行全量刷新。
/// 适用场景：删除任务后 ID 列表变更，需要全量重建。
/// </summary>
public void InvalidateCache() {
    _missionDTOs.Clear();
}
```

- `RefreshMissionBlocks()` 全量刷新保持不变，用于初始加载、切换视图等场景
- `RefreshMissionBlockById()` 作为增量刷新补充（保存/更新）
- `InvalidateCache()` 用于删除场景，清空缓存后下次 VisibleToTrue 自动全量刷新

---

## 变更文件清单

| 文件 | 问题 |
|------|------|
| `OperationGuidance_new/Tasks/ToolTask.cs` | #1 — `CloseToTriggerReconnection` 加 `socketClient = null` |
| `OperationGuidance_service/Models/MissionRecord.cs` | #2 — 加 `workstation_id`, `workstation_name` |
| `OperationGuidance_service/Models/DTOs/MissionRecordDTO.cs` | #2 — 字段来源注释更新 |
| `OperationGuidance_service/Database/sqls/modify_mysql_20260603_2.sql` | #2 — ALTER TABLE 加站点字段 |
| `OperationGuidance_service/Database/sqls/modify_sqlite_20260603_2.sql` | #2 — ALTER TABLE 加站点字段 |
| `OperationGuidance_service/Database/sqls/modify_sqlserver_20260603_2.sql` | #2 — ALTER TABLE 加站点字段 |
| `OperationGuidance_new/Views/MissionEditionView_SCII.cs` | #2 — 保存校验：skip_screw_points 时至少一个点位 |
| `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs` | #2 — `ActionAfterActivatingMission` 填站点 |
| `OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs` | #2 — skip 路径取第一个点位站点 |
| `OperationGuidance_service/Database/DbConnector.cs` | #4 — 新增 `BeforeScriptsExecution` 静态回调 |
| `OperationGuidance_service/Database/SQLiteConnector.cs` | #4 — 脚本执行前触发回调 |
| `OperationGuidance_service/Database/MySqlConnector.cs` | #4 — 脚本执行前触发回调 |
| `OperationGuidance_service/Database/SqlServerConnector.cs` | #4 — 脚本执行前触发回调 |
| `OperationGuidance_new/Utils/MainUtils.cs` | #4 — 订阅回调，弹非模态窗 + 完成后关闭 |
| `OperationGuidance_new/Views/AbstractViews/AVariableSettingsView.cs` | #5 — dispose 旧图, #6 — 补 original 值 |
| `OperationGuidance_new/Views/ReusableWidgets/MissionListPanel.cs` | #7 — `RefreshMissionBlockById` + `InvalidateCache` |
| `OperationGuidance_new/Views/MissionEditionView.cs` | #7 — 新增 `MissionSaved` 事件 |
| `OperationGuidance_new/Views/MissionEditionView_SCII.cs` | #7 — 新增 `MissionSaved` 事件 |
| `OperationGuidance_new/Views/MissionManagementView.cs` | #7 — 订阅事件，调精准刷新 |
| `OperationGuidance_new/Views/MissionManagementView_SCII.cs` | #7 — 订阅事件，调精准刷新 |
