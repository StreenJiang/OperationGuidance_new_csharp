# Skip Screw Points Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a "skip_screw_points" toggle to SCII mission edition and activation flow, allowing operators to complete missions without screw points.

**Architecture:** New `skip_screw_points int(1) NULL` column in `product_mission` table. ToggleButtonGroup in MissionDetailPopUpForm (SCII). Filter override in QueryProductMissionList. Three method overrides in WorkplaceMissionView_SCII: skip validation, direct OK in ActivateMission, suppress auto-reactivation.

**Tech Stack:** C# WinForms, SQL Server / MySQL / SQLite, .NET Framework

---

## File Structure

| File | Action | Responsibility |
|---|---|---|
| `Database/sqls/modify_sqlserver_20260603.sql` | **Create** | SQL Server migration |
| `Database/sqls/modify_mysql_20260603.sql` | **Create** | MySQL migration |
| `Database/sqls/modify_sqlite_20260603.sql` | **Create** | SQLite migration (table rebuild) |
| `Database/Resource.resx` | Modify | Register 3 migration file entries |
| `Database/Resource.Designer.cs` | Modify | C# properties for new Resource entries |
| `Models/ProductMission.cs` | Modify | Entity field |
| `Models/DTOs/ProductMissionDTO.cs` | Modify | DTO field |
| `Controllers/OperationGuidanceApis.cs` | Modify | Mission list filter logic |
| `Views/MissionEditionView_SCII.cs` | Modify | MissionDetailPopUpForm toggle + save/backfill/copy |
| `Views/WorkplaceMissionView_SCII.cs` | Modify | ActivateMission, ValidationBeforeActivatingMission, ActivateMissionAutomatically |

---

### Task 1: Database Migration Files

**Files:**
- Create: `OperationGuidance_service/Database/sqls/modify_sqlserver_20260603.sql`
- Create: `OperationGuidance_service/Database/sqls/modify_mysql_20260603.sql`
- Create: `OperationGuidance_service/Database/sqls/modify_sqlite_20260603.sql`

- [ ] **Step 1: Create SQL Server migration**

```sql
ALTER TABLE [dbo].[product_mission] ADD [skip_screw_points] int NULL;
```

- [ ] **Step 2: Create MySQL migration**

```sql
ALTER TABLE `product_mission`
  ADD COLUMN `skip_screw_points` int(1) NULL AFTER `challenge_mission_id`;
```

- [ ] **Step 3: Create SQLite migration**

Copy the `modify_sqlite_20250315.sql` pattern — rename old table, create new with `skip_screw_points integer(1)` after `challenge_mission_id`, migrate data, drop old table.

```sql
ALTER TABLE "product_mission" RENAME TO "_product_mission_old_20260603";

CREATE TABLE "product_mission" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "name" text(128),
  "pn_code" text(64),
  "max_ng_num" integer(4),
  "password_need_time" integer(4),
  "enabled" integer(1),
  "macs_id" integer,
  "predecessor_mission_id" integer,
  "predecessor_part_mission_ids" text(256),
  "multi_device_independence" integer(1),
  "is_challenge_mission" integer(1),
  "is_first_mission" integer(1),
  "challenge_mission_id" integer,
  "skip_screw_points" integer(1),
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);

INSERT INTO "sqlite_sequence" (name, seq) VALUES ('product_mission', (SELECT COALESCE(MAX(id), 0) FROM "_product_mission_old_20260603"));

INSERT INTO "product_mission" ("id", "name", "pn_code", "max_ng_num", "password_need_time", "enabled", "macs_id", "predecessor_mission_id", "predecessor_part_mission_ids", "multi_device_independence", "is_challenge_mission", "is_first_mission", "challenge_mission_id", "user_id", "deleted", "creator", "modifier", "create_time", "modify_time")
SELECT "id", "name", "pn_code", "max_ng_num", "password_need_time", "enabled", "macs_id", "predecessor_mission_id", "predecessor_part_mission_ids", "multi_device_independence", "is_challenge_mission", "is_first_mission", "challenge_mission_id", "user_id", "deleted", "creator", "modifier", "create_time", "modify_time"
FROM "_product_mission_old_20260603";

DROP TABLE "_product_mission_old_20260603";
```

- [ ] **Step 4: Verify SQL files exist**

```bash
ls -la OperationGuidance_service/Database/sqls/modify_*20260603.sql
```

- [ ] **Step 5: Commit**

```bash
git add OperationGuidance_service/Database/sqls/modify_sqlserver_20260603.sql OperationGuidance_service/Database/sqls/modify_mysql_20260603.sql OperationGuidance_service/Database/sqls/modify_sqlite_20260603.sql
git commit -m "feat(db): add skip_screw_points column to product_mission

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2: Model and DTO Fields

**Files:**
- Modify: `OperationGuidance_service/Models/ProductMission.cs:19`
- Modify: `OperationGuidance_service/Models/DTOs/ProductMissionDTO.cs:18`

- [ ] **Step 1: Add field to ProductMission entity**

In `ProductMission.cs`, after `challenge_mission_id` (line 19), add:

```csharp
public int? skip_screw_points { get; set; } = (int) YesOrNo.NO;
```

The edit target is after:
```csharp
public int? challenge_mission_id { get; set; }
```

Result:
```csharp
public int? challenge_mission_id { get; set; }
public int? skip_screw_points { get; set; } = (int) YesOrNo.NO;
```

- [ ] **Step 2: Add field to ProductMissionDTO**

In `ProductMissionDTO.cs`, after `challenge_mission_id` (line 18), add:

```csharp
public int? skip_screw_points { get; set; }
```

- [ ] **Step 3: Build to verify**

```bash
dotnet build OperationGuidance_service/OperationGuidance_service.csproj
```

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_service/Models/ProductMission.cs OperationGuidance_service/Models/DTOs/ProductMissionDTO.cs
git commit -m "feat(model): add skip_screw_points field to ProductMission and DTO

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 3: Resource.resx and Resource.Designer.cs

**Files:**
- Modify: `OperationGuidance_service/Database/Resource.resx:261`
- Modify: `OperationGuidance_service/Database/Resource.Designer.cs:887`

- [ ] **Step 1: Add 3 data entries to Resource.resx**

After the `modify_sqlserver_20260602` entry (line 261, before `</root>`), add:

```xml
  <data name="modify_mysql_20260603" type="System.Resources.ResXFileRef, System.Windows.Forms">
    <value>sqls\modify_mysql_20260603.sql;System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089;utf-8</value>
  </data>
  <data name="modify_sqlite_20260603" type="System.Resources.ResXFileRef, System.Windows.Forms">
    <value>sqls\modify_sqlite_20260603.sql;System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089;utf-8</value>
  </data>
  <data name="modify_sqlserver_20260603" type="System.Resources.ResXFileRef, System.Windows.Forms">
    <value>sqls\modify_sqlserver_20260603.sql;System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089;utf-8</value>
  </data>
```

Note: Entries follow the alphabetical order pattern from existing — mysql first, then sqlite, then sqlserver.

- [ ] **Step 2: Add C# properties to Resource.Designer.cs**

After `modify_sqlserver_20260602` property (line 887, inside the `Resource` class, before the closing `}`), add:

```csharp

        internal static string modify_sqlserver_20260603 {
            get {
                return ResourceManager.GetString("modify_sqlserver_20260603", resourceCulture);
            }
        }

        internal static string modify_mysql_20260603 {
            get {
                return ResourceManager.GetString("modify_mysql_20260603", resourceCulture);
            }
        }

        internal static string modify_sqlite_20260603 {
            get {
                return ResourceManager.GetString("modify_sqlite_20260603", resourceCulture);
            }
        }
```

- [ ] **Step 3: Build to verify**

```bash
dotnet build OperationGuidance_service/OperationGuidance_service.csproj
```

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_service/Database/Resource.resx OperationGuidance_service/Database/Resource.Designer.cs
git commit -m "feat(db): register skip_screw_points migration scripts in Resource

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 4: Mission List Filter

**Files:**
- Modify: `OperationGuidance_service/Controllers/OperationGuidanceApis.cs:414`

- [ ] **Step 1: Modify the filter condition**

In `QueryProductMissionList()`, replace line 414:

```csharp
                    if (noSide || (!isEditing && (hasNullImageSide || hasNullBoltSide))) {
```

With:

```csharp
                    bool skipScrewPoints = missionDTO.skip_screw_points == (int)YesOrNo.YES;
                    if (noSide || (!isEditing && !skipScrewPoints && (hasNullImageSide || hasNullBoltSide))) {
```

The `skipScrewPoints` variable is declared just before the `if` inside the `foreach` loop that iterates `productMissionDTOs`, so it has access to `missionDTO`.

- [ ] **Step 2: Build to verify**

```bash
dotnet build OperationGuidance_service/OperationGuidance_service.csproj
```

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_service/Controllers/OperationGuidanceApis.cs
git commit -m "feat(api): allow skip_screw_points missions in console list regardless of bolts

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 5: Mission Edition Toggle (UI)

**Files:**
- Modify: `OperationGuidance_new/Views/MissionEditionView_SCII.cs` — MissionDetailPopUpForm class (~10 insertion points)

- [ ] **Step 1: Declare the field**

After `_isFirstMission` declaration (line 1544), add:

```csharp
            private ToggleButtonGroup _skipScrewPoints;
```

- [ ] **Step 2: Add public property**

After `IsFirstMission` property (line 1568), add:

```csharp
            public ToggleButtonGroup SkipScrewPoints { get => _skipScrewPoints; set => _skipScrewPoints = value; }
```

- [ ] **Step 3: Create ToggleButtonGroup in constructor**

After `_challengMission` creation block (line 1596-1601), insert before `_maxNGNum`:

```csharp
                _skipScrewPoints = new("跳过螺丝点位") {
                    Parent = _tablePanel,
                    Ratio = _boxRatio,
                    NameAlignment = HorizontalAlignment.Right,
                };
```

- [ ] **Step 4: Add ColumnSpan for the new control**

After existing `SetColumnSpan` calls (after line 1739 or alongside them), add:

```csharp
                _tablePanel.SetColumnSpan(_skipScrewPoints, _columnCount);
```

- [ ] **Step 5: Backfill value in AfterShown()**

In `AfterShown()` (line 1758), after `_isChallengeMission.Checked = ...` (line 1760), add:

```csharp
                _skipScrewPoints.Checked = _missionDTO.skip_screw_points == (int) YesOrNo.YES;
```

- [ ] **Step 6: Save skip_screw_points on "确定" button click**

In the "确定" button Click handler (inside the `else { }` block after validation passes, around line 406-408), add after the `_missionDTO.is_first_mission` assignment:

```csharp
                            _missionDTO.skip_screw_points = (int) (_detialPopUpForm.SkipScrewPoints.Checked ? YesOrNo.YES : YesOrNo.NO);
```

- [ ] **Step 7: Include skip_screw_points in task copy**

In `_buttonDuplicate.Click` handler (line 549), add to the `ProductMissionDTO` initializer at line 553:

```csharp
                                skip_screw_points = _missionDTO.skip_screw_points,
```

Add it after `multi_device_independence` line (561):

```csharp
                                multi_device_independence = _missionDTO.multi_device_independence,
                                skip_screw_points = _missionDTO.skip_screw_points,
```

- [ ] **Step 8: Build and fix any errors**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 9: Commit**

```bash
git add OperationGuidance_new/Views/MissionEditionView_SCII.cs
git commit -m "feat(ui): add skip_screw_points ToggleButtonGroup to SCII mission detail popup

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 6: Activation Flow Overrides

**Files:**
- Modify: `OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs` — 3 method overrides

- [ ] **Step 1: Add early return in ValidationBeforeActivatingMission**

In the existing `ValidationBeforeActivatingMission()` override (line 1116), add at the top, before the debug log:

```csharp
        protected override async Task<bool> ValidationBeforeActivatingMission() {
            if (_mission.skip_screw_points == (int)YesOrNo.YES) {
                return true;
            }
            logger.Debug($"[SCII:ValidationBeforeActivatingMission] Validating before activating mission");

            if (await base.ValidationBeforeActivatingMission()) {
```

Rest of method remains unchanged.

- [ ] **Step 2: Add early return in ActivateMissionAutomatically**

In the existing `ActivateMissionAutomatically()` override (line 166), add at the top:

```csharp
        protected override async void ActivateMissionAutomatically() {
            if (_mission.skip_screw_points == (int)YesOrNo.YES) {
                return;
            }
            logger.Debug($"[SCII:ActivateMissionAutomatically] Checking if USB scanner is enabled");
```

Rest of method remains unchanged.

- [ ] **Step 3: Add ActivateMission override**

Add a new method in `WorkplaceMissionView_SCII`. Place it near the other activation-related methods (around line 1148, after `ValidationBeforeActivatingMission`):

```csharp
        public override async void ActivateMission() {
            if (_mission.skip_screw_points == (int)YesOrNo.YES) {
                // Cancel previous background tasks (same cleanup as base.ActivateMission)
                _backgroundTaskCts.ForEach(cts => {
                    cts.Cancel();
                    cts.Dispose();
                });
                _backgroundTaskCts.Clear();
                _activeMissionCts.Cancel();
                _activeMissionCts.Dispose();
                _activeMissionCts = new CancellationTokenSource();

                PrepareBeforeActivatingMission();
                _arrangerNeeded = false;
                _setterSelectorNeeded = false;
                _activated = true;
                await ActionAfterActivatingMission();

                _missionRecord.mission_result = (int)TighteningStatus.OK;
                _apis.AddOrUpdateMissionRecord(new(_missionRecord));
                TerminateMission(WorkplaceProcessStatus.FINISHED_OK);
                return;
            }
            base.ActivateMission();
        }
```

Note: `_backgroundTaskCts`, `_activeMissionCts`, `_arrangerNeeded`, `_setterSelectorNeeded`, `_activated`, `_missionRecord`, `_apis` are all `protected` fields in the base class `AWorkplaceContentPanel`. SCII has access to them. `PrepareBeforeActivatingMission()`, `ActionAfterActivatingMission()`, `TerminateMission()` are `protected virtual` methods.

- [ ] **Step 4: Build and fix any errors**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 5: Commit**

```bash
git add OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs
git commit -m "feat(workplace): add skip_screw_points fast-path activation in SCII

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 7: Final Build Verification

- [ ] **Step 1: Build entire solution**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
dotnet build OperationGuidance_service/OperationGuidance_service.csproj
```

Expected: Both projects build without errors.

- [ ] **Step 2: Verify all changed files**

```bash
git status
```

Expected output shows all 10 changed files across both projects.
