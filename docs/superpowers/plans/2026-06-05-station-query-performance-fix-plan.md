# 站点查询性能与数据完整性修复 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复 `QueryMissionRecordList` 按站点查询的性能（索引 + 移除 FORCE INDEX）和数据完整性（双源子查询覆盖历史数据）

**Architecture:** 新建数据库索引 `ix_mr_ws` + 修改 `QueryMissionRecordList` 双源 OR + 条件 FORCE INDEX + GridView 翻页加载遮罩

**Tech Stack:** C# / MySQL / SQLite / SQLServer / Dapper

**Spec:** `docs/superpowers/specs/2026-06-05-station-query-performance-fix-design.md`

---

## File Map

| File | Action | Responsibility |
|---|---|---|
| `.../sqls/modify_mysql_20260605.sql` | Create | MySQL 索引 |
| `.../sqls/modify_sqlite_20260605.sql` | Create | SQLite 索引 |
| `.../sqls/modify_sqlserver_20260605.sql` | Create | SQLServer 索引 |
| `.../Database/Resource.resx` | Modify | 注册 3 个 SQL 文件 |
| `.../Database/Resource.Designer.cs` | Modify | 添加 3 个属性 |
| `.../Controllers/OperationGuidanceApis.cs` | Modify | 条件 FORCE INDEX + 双源 OR |
| `.../ReusableWidgets/DataGridViewGroup.cs` | Modify | 提取 `ShowLoadingOverlay`，公开 `HideLoadingOverlay` |
| `.../ReusableWidgets/DataGridViewPanel.cs` | Modify | 翻页时显示/隐藏遮罩 |

---

### Task 1: 创建数据库索引 migration

**Files:**
- Create: `OperationGuidance_service/Database/sqls/modify_mysql_20260605.sql`
- Create: `OperationGuidance_service/Database/sqls/modify_sqlite_20260605.sql`
- Create: `OperationGuidance_service/Database/sqls/modify_sqlserver_20260605.sql`

- [ ] **Step 1: 创建 MySQL migration**

```sql
-- modify_mysql_20260605.sql
SET @sql := IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'mission_record'
     AND INDEX_NAME = 'ix_mr_ws') = 0,
    'CREATE INDEX ix_mr_ws ON mission_record(workstation_id, deleted)',
    'SELECT "index ix_mr_ws already exists" AS message'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
```

- [ ] **Step 2: 创建 SQLite migration**

```sql
-- modify_sqlite_20260605.sql
CREATE INDEX IF NOT EXISTS ix_mr_ws ON mission_record(workstation_id, deleted);
```

- [ ] **Step 3: 创建 SQLServer migration**

```sql
-- modify_sqlserver_20260605.sql
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'ix_mr_ws' AND object_id = OBJECT_ID('mission_record'))
    CREATE INDEX ix_mr_ws ON mission_record(workstation_id, deleted);
```

- [ ] **Step 4: 注册到 Resource.resx**

在 `Resource.resx` 的 `</root>` 之前插入 3 条：

```xml
  <data name="modify_mysql_20260605" type="System.Resources.ResXFileRef, System.Windows.Forms">
    <value>sqls\modify_mysql_20260605.sql;System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089;utf-8</value>
  </data>
  <data name="modify_sqlite_20260605" type="System.Resources.ResXFileRef, System.Windows.Forms">
    <value>sqls\modify_sqlite_20260605.sql;System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089;utf-8</value>
  </data>
  <data name="modify_sqlserver_20260605" type="System.Resources.ResXFileRef, System.Windows.Forms">
    <value>sqls\modify_sqlserver_20260605.sql;System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089;utf-8</value>
  </data>
```

- [ ] **Step 5: 注册到 Resource.Designer.cs**

在 `Resource.Designer.cs` 中添加 3 个属性（与其他 modify_ 属性同一区域）：

```csharp
        internal static string modify_mysql_20260605 {
            get {
                return ResourceManager.GetString("modify_mysql_20260605", resourceCulture);
            }
        }
        internal static string modify_sqlite_20260605 {
            get {
                return ResourceManager.GetString("modify_sqlite_20260605", resourceCulture);
            }
        }
        internal static string modify_sqlserver_20260605 {
            get {
                return ResourceManager.GetString("modify_sqlserver_20260605", resourceCulture);
            }
        }
```

> `GetResourcesFileNames()` 通过 `ResourceManager.GetResourceSet()` 反射扫描所有 key。`sqlScriptPrefix = "modify_mysql"` 过滤出 MySQL 迁移，按字典序排列执行。resx + Designer.cs 注册后自动被扫到。

- [ ] **Step 6: Commit**

```bash
git add OperationGuidance_service/Database/sqls/modify_mysql_20260605.sql \
        OperationGuidance_service/Database/sqls/modify_sqlite_20260605.sql \
        OperationGuidance_service/Database/sqls/modify_sqlserver_20260605.sql \
        OperationGuidance_service/Database/Resource.resx \
        OperationGuidance_service/Database/Resource.Designer.cs
git commit -m "feat(db): add ix_mr_ws index on mission_record(workstation_id, deleted)

Supports station-based filtering in QueryMissionRecordList.
Paired with existing ix_opdata_ws on operation_data for dual-source lookups.

Register migration files in Resource.resx + Designer.cs for package inclusion."
```

---

### Task 2: QueryMissionRecordList — 双源 OR + 条件 FORCE INDEX

**Files:**
- Modify: `OperationGuidance_service/Controllers/OperationGuidanceApis.cs:728-753`

- [ ] **Step 1: fromClause 条件化**

替换 line 728-731：

```csharp
// 之前 (lines 728-731)
            bool isMysql = SystemUtils.GetDBTypes() == DBTypes.MYSQL;
            string fromClause = isMysql
                ? $"{_missionRecordService.TableName} mr FORCE INDEX (PRIMARY)"
                : $"{_missionRecordService.TableName} mr";

// 之后
            bool isMysql = SystemUtils.GetDBTypes() == DBTypes.MYSQL;
            // FORCE INDEX only when no workstation filter — otherwise let optimizer choose
            string fromClause;
            if (isMysql && req.WorkstationId == null) {
                fromClause = $"{_missionRecordService.TableName} mr FORCE INDEX (PRIMARY)";
            } else {
                fromClause = $"{_missionRecordService.TableName} mr";
            }
```

- [ ] **Step 2: workstation_id 过滤改为双源 OR 子查询**

替换 line 750-753：

```csharp
// 之前 (lines 750-753)
            if (req.WorkstationId != null) {
                condition += " and mr.workstation_id = @workstation_id";
                parameters.Add("workstation_id", req.WorkstationId.Value);
            }

// 之后
            if (req.WorkstationId != null) {
                condition += @" and (mr.workstation_id = @workstation_id
                    or mr.id in (select mission_record_id from operation_data
                                 where workstation_id = @ws_od_id and deleted = @ws_od_deleted))";
                parameters.Add("workstation_id", req.WorkstationId.Value);
                parameters.Add("ws_od_id", req.WorkstationId.Value);
                parameters.Add("ws_od_deleted", (int)YesOrNo.NO);
            }
```

> 两个分支用不同的参数名（`@workstation_id` / `@ws_od_id`）防止 Dapper 跨 provider 参数复用问题。值相同。

- [ ] **Step 3: 更新注释**

替换 line 726-727：

```csharp
// 之前
            // FORCE INDEX (PRIMARY) ensures MySQL scans mr in PK order for ORDER BY id LIMIT.
            // Do NOT force index for COUNT — let the optimizer choose the best plan for the aggregate.

// 之后
            // FORCE INDEX (PRIMARY) ensures MySQL scans mr in PK order for ORDER BY id LIMIT
            // when filtering by mr.id IN (...) — avoids filesort on large IN lists.
            // Skip force when workstation_id filter is active — let optimizer use ix_mr_ws.
            // Do NOT force index for COUNT — let the optimizer choose the best plan for the aggregate.
```

- [ ] **Step 4: 构建验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

预期：Build succeeded，0 errors

- [ ] **Step 5: Commit**

```bash
git add OperationGuidance_service/Controllers/OperationGuidanceApis.cs
git commit -m "fix(api): dual-source station filter with OR subquery for history coverage

QueryMissionRecordList workstation_id filter now uses:
  mr.workstation_id = @id (new data)
  OR mr.id IN (SELECT mission_record_id FROM operation_data WHERE ...) (history)

- Removes FORCE INDEX(PRIMARY) when WorkstationId is set — ix_mr_ws handles it
- Subquery uses existing covering index ix_opdata_ws(workstation_id, mission_record_id)
- Separate param names (@workstation_id / @ws_od_id) for Dapper cross-provider safety"
```

---

### Task 3: DataGridView 翻页/跳页加载遮罩

**Files:**
- Modify: `OperationGuidance_new/Views/ReusableWidgets/DataGridViewGroup.cs:176-198`
- Modify: `OperationGuidance_new/Views/ReusableWidgets/DataGridViewPanel.cs:570-642`

- [ ] **Step 1: DataGridViewGroup — 提取 ShowLoadingOverlay + 公开方法**

将 `HideLoadingOverlay` 从 `private` 改为 `internal`（只改访问修饰符，方法体不变）。

新增 `internal ShowLoadingOverlay()` 方法——将 `_queryData` 回调中 lines 252-280 的内联遮罩显示代码提取到此方法：

```csharp
        /// <summary>Show loading overlay on the grid area during async operations (query, paging).</summary>
        internal void ShowLoadingOverlay() {
            if (_loadingOverlay.Visible) return;
            _loadingOverlay.Size = _voGridView.Size;
            _loadingOverlay.Location = _voGridView.Location;
            if (_loadingOverlay.Width > 0 && _loadingOverlay.Height > 0) {
                try {
                    using (Bitmap bmp = new(_loadingOverlay.Width, _loadingOverlay.Height)) {
                        _voGridView.DrawToBitmap(bmp, new(0, 0, _loadingOverlay.Width, _loadingOverlay.Height));
                        using (Graphics g = Graphics.FromImage(bmp)) {
                            using (Brush brush = new SolidBrush(Color.FromArgb(120, 0, 0, 0))) {
                                g.FillRectangle(brush, 0, 0, bmp.Width, bmp.Height);
                            }
                            using (Font font = new(CustomLibrary.Configs.WidgetsConfigs.SystemFontFamily, 16, FontStyle.Regular))
                            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center }) {
                                g.DrawString("加载中...", font, Brushes.White, new RectangleF(0, 0, bmp.Width, bmp.Height), sf);
                            }
                        }
                        _loadingOverlay.BackgroundImage = bmp;
                    }
                } catch {
                    // DrawToBitmap failed — fallback to label
                }
            }
            if (_loadingOverlay.BackgroundImage == null) {
                _loadingLabel.Size = new(_loadingOverlay.Width, 40);
                _loadingLabel.Location = new(0, (_loadingOverlay.Height - 40) / 2);
            }
            _loadingLabel.Visible = (_loadingOverlay.BackgroundImage == null);
            _loadingOverlay.Visible = true;
            _loadingOverlay.BringToFront();
        }
```

然后找到 `_queryData` 回调中原来的内联遮罩代码（lines 252-280），替换为一行：

```csharp
// 之前 (lines 252-280) — 内联遮罩逻辑
                _loadingOverlay.Size = _voGridView.Size;
                ... (20+ 行) ...
                _loadingOverlay.BringToFront();

// 之后
                ShowLoadingOverlay();
```

- [ ] **Step 2: DataGridViewPanel — 翻页时调用遮罩**

在 `Paging` 方法中：

**2a.** 在 `_isPaging = true` 后捕获 parent 引用（UI 线程安全）：

```csharp
            _isPaging = true;
            var group = this.Parent as DataGridViewGroup<T>;
```

**2b.** 在缓存检查和 `ServerFetch` 之间显示遮罩。找到 `try {` 行（约 line 596），在它之前插入：

```csharp
                // Show mask only when we actually need a server fetch (skip on cache hit)
                group?.ShowLoadingOverlay();

                try {
```

**2c.** 在 `finally` 块隐藏遮罩。修改 lines 631-633：

```csharp
// 之前
                } finally {
                    _isPaging = false;
                    BeginInvoke(new Action(RestorePageButtons));
                }

// 之后
                } finally {
                    _isPaging = false;
                    BeginInvoke(new Action(() => {
                        RestorePageButtons();
                        group?.HideLoadingOverlay();
                    }));
                }
```

> `DataGridViewGroup<T>` 和 `DataGridViewPanel<T>` 在同一 namespace，直接泛型 cast，编译时类型安全。遮罩只在需要 `ServerFetch` 时才显示（缓存命中直接返回，无需遮罩）。`ShowLoadingOverlay` 内部有 `if (_loadingOverlay.Visible) return` 防重复显示。`HideLoadingOverlay` 通过 `BeginInvoke` 回到 UI 线程，`?` 空传播在 Parent 为空或已 Dispose 时安全跳过。

- [ ] **Step 3: 构建验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

预期：Build succeeded，0 errors

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_new/Views/ReusableWidgets/DataGridViewGroup.cs \
        OperationGuidance_new/Views/ReusableWidgets/DataGridViewPanel.cs
git commit -m "feat(ui): show loading mask during grid pagination

Extract ShowLoadingOverlay from inline query code into reusable method.
Show/hide overlay during DataGridViewPanel LoadPageData server fetch."
```

---

### Task 4: 整体验证

- [ ] **Step 1: 完整构建**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

预期：Build succeeded，0 errors

- [ ] **Step 2: 确认所有改动文件**

```bash
git diff --stat HEAD~3
```

预期输出：
```
OperationGuidance_service/Database/sqls/modify_mysql_20260605.sql
OperationGuidance_service/Database/sqls/modify_sqlite_20260605.sql
OperationGuidance_service/Database/sqls/modify_sqlserver_20260605.sql
OperationGuidance_service/Database/Resource.resx
OperationGuidance_service/Database/Resource.Designer.cs
OperationGuidance_service/Controllers/OperationGuidanceApis.cs
OperationGuidance_new/Views/ReusableWidgets/DataGridViewGroup.cs
OperationGuidance_new/Views/ReusableWidgets/DataGridViewPanel.cs
```
