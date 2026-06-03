# 批量问题修复实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复 6 个 v1.6.x 缺陷：PSet 重连竞态、mission_record 缺站点、DB 脚本无非模态提示、字段配置 OOM、配置保存后脏标记误报、任务列表缓存不同步

**Architecture:** 每个问题独立修复，无相互依赖。问题 #2 涉及 DB migration → Model → 保存校验 → 写入逻辑的纵向链路。问题 #7 采用 Event 模式连接 EditionView 和 ManagementView

**Tech Stack:** C# WinForms, Dapper, SQLite/MySQL/SQL Server

---

### Task 1: 问题 #1 — PSet 重连竞态修复

**Files:**
- Modify: `OperationGuidance_new/Tasks/ToolTask.cs:246-250`

- [ ] **Step 1: 在 `CloseToTriggerReconnection()` 末尾添加 `socketClient = null`**

```csharp
// 改前
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

- [ ] **Step 2: 编译验证**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeded with 0 errors.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Tasks/ToolTask.cs
git commit -m "fix(tool): set socketClient=null in CloseToTriggerReconnection to prevent race condition

Old RunTask() finally block could close new socket created by ReconnectAndResendPset.
Now socketClient=null ensures Connected returns false and finally block skips cleanup.

Co-Authored-By: Claude Code <noreply@anthropic.com>"
```

---

### Task 2: 问题 #2 — DB Migration SQL 文件

**Files:**
- Create: `OperationGuidance_service/Database/sqls/modify_mysql_20260603_2.sql`
- Create: `OperationGuidance_service/Database/sqls/modify_sqlserver_20260603_2.sql`
- Create: `OperationGuidance_service/Database/sqls/modify_sqlite_20260603_2.sql`
- Modify: `OperationGuidance_service/Database/Resource.resx`
- Modify: `OperationGuidance_service/Database/Resource.Designer.cs`

- [ ] **Step 1: 创建 MySQL migration**

New file `OperationGuidance_service/Database/sqls/modify_mysql_20260603_2.sql`:
```sql
ALTER TABLE `mission_record`
  ADD COLUMN `workstation_id` int(11) NULL AFTER `is_redo`,
  ADD COLUMN `workstation_name` varchar(200) NULL AFTER `workstation_id`;
```

- [ ] **Step 2: 创建 SQL Server migration**

New file `OperationGuidance_service/Database/sqls/modify_sqlserver_20260603_2.sql`:
```sql
ALTER TABLE [dbo].[mission_record] ADD [workstation_id] int NULL;
ALTER TABLE [dbo].[mission_record] ADD [workstation_name] nvarchar(200) NULL;
```

- [ ] **Step 3: 创建 SQLite migration**

New file `OperationGuidance_service/Database/sqls/modify_sqlite_20260603_2.sql`:
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

- [ ] **Step 4: 注册资源到 Resource.resx**

在 `OperationGuidance_service/Database/Resource.resx` 中追加（在 `modify_sqlserver_20260603` 条目之后）:

```xml
  <data name="modify_mysql_20260603_2" type="System.Resources.ResXFileRef, System.Windows.Forms">
    <value>sqls\modify_mysql_20260603_2.sql;System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089;utf-8</value>
  </data>
  <data name="modify_sqlite_20260603_2" type="System.Resources.ResXFileRef, System.Windows.Forms">
    <value>sqls\modify_sqlite_20260603_2.sql;System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089;utf-8</value>
  </data>
  <data name="modify_sqlserver_20260603_2" type="System.Resources.ResXFileRef, System.Windows.Forms">
    <value>sqls\modify_sqlserver_20260603_2.sql;System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089;utf-8</value>
  </data>
```

- [ ] **Step 5: 编译验证**

Run: `dotnet build OperationGuidance_service/OperationGuidance_service.csproj`
Expected: Build succeeded with 0 errors. Resource.Designer.cs 会自动生成新的属性。

- [ ] **Step 6: Commit**

```bash
git add OperationGuidance_service/Database/sqls/modify_mysql_20260603_2.sql \
        OperationGuidance_service/Database/sqls/modify_sqlserver_20260603_2.sql \
        OperationGuidance_service/Database/sqls/modify_sqlite_20260603_2.sql \
        OperationGuidance_service/Database/Resource.resx \
        OperationGuidance_service/Database/Resource.Designer.cs
git commit -m "feat(db): add workstation_id and workstation_name columns to mission_record

Three DB migration files with _2 suffix to avoid conflict with
already-executed skip_screw_points migration.

MySQL/SQL Server: ALTER TABLE ADD COLUMN
SQLite: table rebuild pattern with index recreation

Co-Authored-By: Claude Code <noreply@anthropic.com>"
```

---

### Task 3: 问题 #2 — Model 层添加字段

**Files:**
- Modify: `OperationGuidance_service/Models/MissionRecord.cs`
- Modify: `OperationGuidance_service/Models/DTOs/MissionRecordDTO.cs`

- [ ] **Step 1: MissionRecord 添加字段**

在 `OperationGuidance_service/Models/MissionRecord.cs` 的 `is_redo` 属性之后添加：

```csharp
public int? workstation_id { get; set; }
public string? workstation_name { get; set; }
```

- [ ] **Step 2: MissionRecordDTO 更新注释**

在 `OperationGuidance_service/Models/DTOs/MissionRecordDTO.cs` 中，将 `workstation_id` 和 `workstation_name` 的注释从 `// JOIN 填充的冗余字段` 改为：

```csharp
public int? workstation_id { get; set; }
public string? workstation_name { get; set; }
```

（移除原有的 `// JOIN 填充的冗余字段` 注释，因为现在直接从 mission_record 表取值）

- [ ] **Step 3: 编译验证**

Run: `dotnet build OperationGuidance_service/OperationGuidance_service.csproj`
Expected: Build succeeded with 0 errors.

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_service/Models/MissionRecord.cs \
        OperationGuidance_service/Models/DTOs/MissionRecordDTO.cs
git commit -m "feat(model): add workstation fields to MissionRecord and MissionRecordDTO

Co-Authored-By: Claude Code <noreply@anthropic.com>"
```

---

### Task 4: 问题 #2 — 保存校验：skip_screw_points 时至少一个点位

**Files:**
- Modify: `OperationGuidance_new/Views/MissionEditionView_SCII.cs`

- [ ] **Step 1: 在"确定"按钮校验中添加点位检查**

在 `MissionEditionView_SCII.cs` 中，找到 `_detialPopUpForm.AddButton("确定").Click += (s, e) => {` 内部的校验逻辑。在最后一个校验与 `if (!check)` 之间添加：

```csharp
// 跳过螺丝点位时，必须至少配置一个点位以获取站点信息
if (check && _detialPopUpForm.SkipScrewPoints.Checked) {
    bool hasAnyBolt = _sideButtons.Count > 0 && _sideButtons.Any(side =>
        side.BoltButtons != null && side.BoltButtons.Values.Any(bolts => bolts.Count > 0));
    if (!hasAnyBolt) {
        check = false;
        warningMsg += $"{warningIndex++}. 已开启\"跳过螺丝点位\"，但未配置任何螺丝点位。请至少添加一个产品面及点位以确定站点信息\r\n";
    }
}
```

- [ ] **Step 2: 编译验证**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeded with 0 errors.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/MissionEditionView_SCII.cs
git commit -m "feat(validation): require at least one bolt when skip_screw_points is enabled

Ensures workstation info can always be derived from the first bolt point.

Co-Authored-By: Claude Code <noreply@anthropic.com>"
```

---

### Task 5: 问题 #2 — ActionAfterActivatingMission 填站点

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs:1548-1555`

- [ ] **Step 1: 修改 _missionRecord 创建逻辑**

将现有代码（line 1548-1555）：
```csharp
_missionRecord = new() {
    mission_id = _mission.id,
    product_bar_code = _barCodeObj.ProductBarCode,
    parts_bar_code = string.Join(",", _barCodeObj.PartsBarCodes),
    mission_result = (int) TighteningStatus.NG,
    is_redo = _isRedo,
};
```

替换为：
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
    mission_result = (int) TighteningStatus.NG,
    is_redo = _isRedo,
    workstation_id = workstationId,
    workstation_name = workstationName,
};
```

- [ ] **Step 2: 编译验证**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeded with 0 errors.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs
git commit -m "feat(mission): populate workstation info when creating mission_record

Derive workstation_id and workstation_name from the first bolt point.

Co-Authored-By: Claude Code <noreply@anthropic.com>"
```

---

### Task 6: 问题 #2 — SCII skip 路径填站点

**Files:**
- Modify: `OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs:1173-1181`

- [ ] **Step 1: 修改 skip 路径的 _missionRecord 创建**

将现有代码（line 1173-1181）：
```csharp
_missionRecord = new() {
    mission_id = _mission.id,
    product_bar_code = _barCodeObj.ProductBarCode,
    parts_bar_code = string.Join(",", _barCodeObj.PartsBarCodes),
    mission_result = (int)TighteningStatus.OK,
    is_redo = _isRedo,
    product_batch = _productBatch.GetTextBox(0).Box.Text,
};
```

替换为：
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

- [ ] **Step 2: 编译验证**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeded with 0 errors.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs
git commit -m "feat(mission): populate workstation info in SCII skip screw points path

Query mission detail to get first bolt's workstation before creating
mission_record. Save validation already ensures at least one bolt exists.

Co-Authored-By: Claude Code <noreply@anthropic.com>"
```

---

### Task 7: 问题 #4 — DbConnector 新增静态回调

**Files:**
- Modify: `OperationGuidance_service/Database/DbConnector.cs`

- [ ] **Step 1: 在 DbConnector 类中添加静态回调字段**

在 `DbConnector` 类的字段区域添加：

```csharp
/// <summary>
/// 当检测到待执行的数据库脚本时触发。参数为脚本文件名列表。
/// 客户端可订阅此回调以显示非模态提示窗。
/// </summary>
public static Action<List<string>>? BeforeScriptsExecution;
```

- [ ] **Step 2: 编译验证**

Run: `dotnet build OperationGuidance_service/OperationGuidance_service.csproj`
Expected: Build succeeded with 0 errors.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_service/Database/DbConnector.cs
git commit -m "feat(db): add BeforeScriptsExecution callback to DbConnector

Static Action<List<string>> that connectors fire when pending scripts detected.
Client subscribes to show non-modal popup during long-running migrations.

Co-Authored-By: Claude Code <noreply@anthropic.com>"
```

---

### Task 8: 问题 #4 — 三个 Connector 触发回调

**Files:**
- Modify: `OperationGuidance_service/Database/SQLiteConnector.cs`
- Modify: `OperationGuidance_service/Database/MySqlConnector.cs`
- Modify: `OperationGuidance_service/Database/SqlServerConnector.cs`

- [ ] **Step 1: SQLiteConnector 触发回调**

在 `SQLiteConnector.cs` 的 `GetDbConnection()` 方法中，将当前的脚本检测+执行循环改为先收集再执行。找到 `foreach (string fileName in fileNames)` 循环（约 line 79），改为：

```csharp
// 收集本轮待执行的脚本
List<string> pendingScripts = fileNames
    .Where(f => f.Contains(sqlScriptPrefix) && !executedFileNames.Contains(f))
    .ToList();

if (pendingScripts.Count > 0) {
    DbConnector.BeforeScriptsExecution?.Invoke(pendingScripts);
}

foreach (string fileName in pendingScripts) {
    try {
        string? fileText = Resource.ResourceManager.GetString(fileName);
        if (!string.IsNullOrEmpty(fileText)) {
            logger.Info($"Not executed sql script[{fileName}] found");
            newExecutedSqlFileName.Add(fileName);
            command.CommandText = fileText;
            command.ExecuteNonQuery();
            logger.Info($"Execute sql script[{fileName}] successfully");
        }
    } catch (Exception e) {
        logger.Warn($"Execute sql script[{fileName}] failed, e: {e}");
    }
}
```

- [ ] **Step 2: MySqlConnector 触发回调**

在 `MySqlConnector.cs` 的 `GetDbConnection()` 中，`List<string> fileNames = ConnectionUtils.GetResourcesFileNames();` 之后、`foreach (string fileName in fileNames)` 之前，改为先收集 pending scripts：

```csharp
List<string> fileNames = ConnectionUtils.GetResourcesFileNames();
// 收集本轮待执行的脚本
List<string> pendingScripts = fileNames
    .Where(f => f.Contains(sqlScriptPrefix) && !executedFileNames.Contains(f))
    .ToList();

if (pendingScripts.Count > 0) {
    DbConnector.BeforeScriptsExecution?.Invoke(pendingScripts);
}

foreach (string fileName in pendingScripts) {
    // ... 原有执行逻辑不变（含 Split(';') 和 allOk 跟踪）...
}
```

- [ ] **Step 3: SqlServerConnector 触发回调**

在 `SqlServerConnector.cs` 的 `GetDbConnection()` 中，同样改为先收集 pending scripts：

```csharp
List<string> fileNames = ConnectionUtils.GetResourcesFileNames();
// 收集本轮待执行的脚本
List<string> pendingScripts = fileNames
    .Where(f => f.Contains(sqlScriptPrefix) && !executedFileNames.Contains(f))
    .ToList();

if (pendingScripts.Count > 0) {
    DbConnector.BeforeScriptsExecution?.Invoke(pendingScripts);
}

foreach (string fileName in pendingScripts) {
    // ... 原有执行逻辑不变（含 Split("GO") 批处理）...
}
```

- [ ] **Step 4: 编译验证**

Run: `dotnet build OperationGuidance_service/OperationGuidance_service.csproj`
Expected: Build succeeded with 0 errors.

- [ ] **Step 5: Commit**

```bash
git add OperationGuidance_service/Database/SQLiteConnector.cs \
        OperationGuidance_service/Database/MySqlConnector.cs \
        OperationGuidance_service/Database/SqlServerConnector.cs
git commit -m "feat(db): fire BeforeScriptsExecution callback in all three connectors

Collect pending scripts before execution loop, invoke callback so client
can show non-modal popup to warn user not to terminate the application.

Co-Authored-By: Claude Code <noreply@anthropic.com>"
```

---

### Task 9: 问题 #4 — MainUtils 订阅回调弹非模态窗

**Files:**
- Modify: `OperationGuidance_new/Utils/MainUtils.cs`

- [ ] **Step 1: 添加 scriptsPopup 字段**

在 `MainUtils` 类中添加静态字段（或作为 `CheckDBConnection` 的局部变量，但需跨回调访问）：

在 `CheckDBConnection()` 方法中，`Form formPopup = new Form()` 之后添加：

```csharp
CustomPopUpForm? scriptsPopup = null;
```

- [ ] **Step 2: 在 BeginInvoke 之前注册回调**

在 `formPopup.BeginInvoke(...)` 调用之前（约 line 148），添加：

```csharp
DbConnector.BeforeScriptsExecution = (scriptNames) => {
    formPopup.BeginInvoke(() => {
        scriptsPopup = new CustomPopUpForm {
            Title = "数据库升级",
        };
        scriptsPopup.AddLabel($"正在执行 {scriptNames.Count} 个数据库升级脚本，请勿关闭程序...");
        scriptsPopup.Show();
    });
};
```

- [ ] **Step 3: GetConnection 完成后关闭弹窗**

在 `formPopup.Dispose()` 之后（约 line 176）、`formPopup.ShowDialog()` 之前，添加：

```csharp
if (scriptsPopup != null && !scriptsPopup.IsDisposed) {
    scriptsPopup.Dispose();
}
```

- [ ] **Step 4: 编译验证**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeded with 0 errors.

- [ ] **Step 5: Commit**

```bash
git add OperationGuidance_new/Utils/MainUtils.cs
git commit -m "feat(ui): show non-modal popup during database script execution

Subscribes to DbConnector.BeforeScriptsExecution callback, creates a
modeless popup to warn users not to terminate the application while
DB migration scripts are running. Popup is disposed after GetConnection()
completes. Existing modal popup logic is untouched.

Co-Authored-By: Claude Code <noreply@anthropic.com>"
```

---

### Task 10: 问题 #5 — MovableButton GDI+ 泄漏修复

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/AVariableSettingsView.cs`

- [ ] **Step 1: OnMouseEnter — dispose 旧图再 resize**

在 `MovableButton` 类的 `OnMouseEnter` 方法中（约 line 735），在 resize 之前添加 dispose：

```csharp
protected override void OnMouseEnter(EventArgs e) {
    base.OnMouseEnter(e);
    int btnSide = (int)(Height * .75);
    int margin = btnSide / 3;
    Size imageSize = new(btnSide, btnSide);
    Point imageDownLocation = new(Width - btnSide, (Height - btnSide) / 2);
    Point imageUpLocation = new(Width - btnSide * 2 - margin, (Height - btnSide) / 2);

    _upImageRect = new(imageUpLocation, imageSize);
    _downImageRect = new(imageDownLocation, imageSize);

    var newUp = WidgetUtils.ResizeImage(_upImage, imageSize);
    var newDown = WidgetUtils.ResizeImage(_downImage, imageSize);
    _upImageShowing?.Dispose();
    _downImageShowing?.Dispose();
    _upImageShowing = newUp;
    _downImageShowing = newDown;
}
```

- [ ] **Step 2: OnMouseLeave — dispose 再置 null**

在 `MovableButton` 类的 `OnMouseLeave` 方法中（约 line 779），添加 dispose：

```csharp
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

- [ ] **Step 3: 编译验证**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeded with 0 errors.

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_new/Views/AbstractViews/AVariableSettingsView.cs
git commit -m "fix(ui): dispose resized images in MovableButton to prevent GDI+ leak

OnMouseEnter now disposes old _upImageShowing/_downImageShowing before
creating new resized copies. OnMouseLeave disposes before setting null.
Fixes intermittent OutOfMemoryException when opening field configuration.

Co-Authored-By: Claude Code <noreply@anthropic.com>"
```

---

### Task 11: 问题 #6 — SaveStorageSettings 补 original 值

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/AVariableSettingsView.cs:446-449`

- [ ] **Step 1: 在 SaveStorageSettings 末尾添加两行**

在 `SaveStorageSettings()` 方法中，`ExportConfig.Instance.Reload();` 之后添加：

```csharp
_enableExcelExportOriginal = _enableExcelExportToggle.Checked;
_enableTxtExportOriginal = _enableTxtExportToggle.Checked;
```

完整上下文：
```csharp
ExportConfig.Instance.SetExcelExportEnabled(_enableExcelExportToggle.Checked);
ExportConfig.Instance.SetTxtExportEnabled(_enableTxtExportToggle.Checked);
ExportConfig.Instance.Reload();

_enableExcelExportOriginal = _enableExcelExportToggle.Checked;
_enableTxtExportOriginal = _enableTxtExportToggle.Checked;
```

- [ ] **Step 2: 编译验证**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeded with 0 errors.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/AbstractViews/AVariableSettingsView.cs
git commit -m "fix(settings): update original values for Excel/Txt export toggles after save

SaveStorageSettings was missing _enableExcelExportOriginal and
_enableTxtExportOriginal updates, causing false 'unsaved changes'
warning when leaving settings page after saving.

Co-Authored-By: Claude Code <noreply@anthropic.com>"
```

---

### Task 12: 问题 #7 — MissionListPanel.RefreshMissionBlockById

**Files:**
- Modify: `OperationGuidance_new/Views/ReusableWidgets/MissionListPanel.cs`

- [ ] **Step 1: 添加 RefreshMissionBlockById 方法**

在 `MissionListPanel` 类中，`RefreshMissionBlocks` 方法之后添加：

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
        if (newBlock.InnerButton.ToggledButton) {
            if (_currentToggledMission == null) {
                _currentToggledMission = newBlock;
            } else {
                _currentToggledMission.InnerButton.SetToggle(false);
                if (_currentToggledMission == newBlock) {
                    _currentToggledMission = null;
                } else {
                    _currentToggledMission = newBlock;
                    _currentToggledMission.InnerButton.SetToggle(true);
                }
            }
        }
        if (blockClickAction != null) {
            blockClickAction(newBlock.Entity.id);
        }
    };

    _contentPanel.MissionsTable.Controls.Add(newBlock);
    _contentPanel.MissionsTable.Controls.SetChildIndex(newBlock, ctrlIndex);

    // 异步加载封面
    _ = LoadOneCoverAsync(newBlock, CancellationToken.None);
}

/// <summary>
/// 清空缓存，强制下次 RefreshMissionBlocks 执行全量刷新。
/// 适用场景：删除任务后 ID 列表变更，需要全量重建。
/// </summary>
public void InvalidateCache() {
    _missionDTOs.Clear();
}
```

- [ ] **Step 2: 编译验证**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeded with 0 errors.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/ReusableWidgets/MissionListPanel.cs
git commit -m "feat(mission-list): add RefreshMissionBlockById for targeted cache refresh

Allows refreshing a single mission block by ID without rebuilding the
entire list. Updates both the _missionDTOs cache and the UI control.

Co-Authored-By: Claude Code <noreply@anthropic.com>"
```

---

### Task 13: 问题 #7 — MissionEditionView 添加 MissionSaved 事件

**Files:**
- Modify: `OperationGuidance_new/Views/MissionEditionView.cs`

- [ ] **Step 1: 在 MissionEditionView 类中添加事件**

在 `MissionEditionView` 类的属性区域添加：

```csharp
public event Action<int, ProductMissionDTO?>? MissionSaved;
```

- [ ] **Step 2: 在保存成功后触发事件**

在 `EditionPage` 内部类中，找到 `_buttonSave.Click` 处理器中 `MessageBox.Show(null, "保存成功！"...)` 之后、`TriggerClick` 之前，添加：

```csharp
_parentView.MissionSaved?.Invoke(_missionDTO.id, _missionDTO);
```

- [ ] **Step 3: 在删除成功后触发事件**

在 `EditionPage` 内部类中，找到 `_buttonDelete.Click` 处理器中 `MessageBox.Show(null, "删除成功！"...)` 之后、`TriggerClick` 之前，添加：

```csharp
_parentView.MissionSaved?.Invoke(_parentView.MissionDTO.id, null);
```

- [ ] **Step 4: 在复制成功后触发事件**

在 `EditionPage` 内部类中，找到"复制"保存路径（`MessageBox.Show(null, "复制成功！"...)` 之后、`TriggerClick` 之前），添加：

```csharp
_parentView.MissionSaved?.Invoke(_missionDTO.id, _missionDTO);
```

- [ ] **Step 5: 编译验证**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeded with 0 errors.

- [ ] **Step 6: Commit**

```bash
git add OperationGuidance_new/Views/MissionEditionView.cs
git commit -m "feat(edition): add MissionSaved event to MissionEditionView

Fires after successful save (dto=updated mission) or delete (dto=null).
Allows MissionManagementView to refresh the mission list cache.

Co-Authored-By: Claude Code <noreply@anthropic.com>"
```

---

### Task 14: 问题 #7 — MissionEditionView_SCII 添加 MissionSaved 事件

**Files:**
- Modify: `OperationGuidance_new/Views/MissionEditionView_SCII.cs`

- [ ] **Step 1: 在 MissionEditionView_SCII 类中添加事件**

在 `MissionEditionView_SCII` 类中添加：

```csharp
public event Action<int, ProductMissionDTO?>? MissionSaved;
```

- [ ] **Step 2: 在保存成功后触发事件**

在 `EditionPage` 内部类中，`_buttonSave.Click` 处理器的 `MessageBox.Show(null, "保存成功！"...)` 之后、`TriggerClick` 之前：

```csharp
_parentView.MissionSaved?.Invoke(_missionDTO.id, _missionDTO);
```

- [ ] **Step 3: 在删除成功后触发事件**

在 `EditionPage` 内部类中，`_buttonDelete.Click` 处理器：

```csharp
_parentView.MissionSaved?.Invoke(_parentView.MissionDTO.id, null);
```

- [ ] **Step 4: 在复制成功后触发事件**

在 `EditionPage` 内部类中，"复制"保存路径（`MessageBox.Show(null, "复制成功！"...)` 之后、`TriggerClick` 之前）：

```csharp
_parentView.MissionSaved?.Invoke(_missionDTO.id, _missionDTO);
```

- [ ] **Step 5: 编译验证**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeded with 0 errors.

- [ ] **Step 6: Commit**

```bash
git add OperationGuidance_new/Views/MissionEditionView_SCII.cs
git commit -m "feat(edition-scii): add MissionSaved event to MissionEditionView_SCII

Same pattern as MissionEditionView — fires after save or delete to
notify the management view to refresh the mission list.

Co-Authored-By: Claude Code <noreply@anthropic.com>"
```

---

### Task 15: 问题 #7 — MissionManagementView 订阅事件

**Files:**
- Modify: `OperationGuidance_new/Views/MissionManagementView.cs`

- [ ] **Step 1: 在获取 EditionView 后订阅事件**

在 `MissionManagementView` 中，找到 `_editionView` 的初始化位置（`EditionView` getter），在 `_editionView = WidgetUtils.GetView<MissionEditionView>();` 之后添加：

```csharp
_editionView.MissionSaved += (missionId, dto) => {
    if (dto != null) {
        _missionListPanel.RefreshMissionBlockById(missionId, dto, OpenEditionPageView);
    } else {
        _missionListPanel.InvalidateCache();
    }
};
```

- [ ] **Step 2: 编译验证**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeded with 0 errors.

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_new/Views/MissionManagementView.cs
git commit -m "feat(management): subscribe to MissionSaved event for cache refresh

On save: calls RefreshMissionBlockById for targeted update.
On delete: calls InvalidateCache, subsequent VisibleToTrue does full refresh.

Co-Authored-By: Claude Code <noreply@anthropic.com>"
```

---

### Task 16: 问题 #7 — MissionManagementView_SCII 订阅事件

**Files:**
- Modify: `OperationGuidance_new/Views/MissionManagementView_SCII.cs`

- [ ] **Step 1: 同样订阅 SCII EditionView 的 MissionSaved 事件**

在 `MissionManagementView_SCII` 中，找到 `_editionView` 初始化位置，添加与 Task 15 相同的订阅逻辑：

```csharp
_editionView.MissionSaved += (missionId, dto) => {
    if (dto != null) {
        _missionListPanel.RefreshMissionBlockById(missionId, dto, OpenEditionPageView);
    } else {
        _missionListPanel.InvalidateCache();
    }
};
```

- [ ] **Step 2: 编译验证**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeded with 0 errors.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/MissionManagementView_SCII.cs
git commit -m "feat(management-scii): subscribe to MissionSaved event for cache refresh

Same pattern as MissionManagementView.

Co-Authored-By: Claude Code <noreply@anthropic.com>"
```

---

### Task 17: 全项目编译验证

- [ ] **Step 1: 编译 service 项目**

Run: `dotnet build OperationGuidance_service/OperationGuidance_service.csproj`
Expected: Build succeeded with 0 errors.

- [ ] **Step 2: 编译主项目**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeded with 0 errors.

---

## 验证清单

- [ ] `dotnet build` 全项目通过（两个项目均 0 errors）
- [ ] 问题 #1：PSet 下发失败后，重连重试能正常断开旧连接
- [ ] 问题 #2：skip_screw_points 开启且无点位时，保存被拦截并有明确提示
- [ ] 问题 #2：skip_screw_points 任务激活后，mission_record 有 workstation_id 和 workstation_name
- [ ] 问题 #4：有未执行脚本时，弹出非模态提示窗；脚本执行完毕后关闭
- [ ] 问题 #5：频繁进出字段配置界面不再出现 OOM
- [ ] 问题 #6：修改导出开关后保存，离开配置界面不再提示"有未保存内容"
- [ ] 问题 #7：编辑任务名称后保存，返回任务列表能看到更新后的名称
