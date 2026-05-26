# DataQueryView 查询性能优化实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 DataQueryView / DataQueryView_SCII 在千万级数据下的查询性能从"UI卡死+秒级延迟"优化到"UI流畅+<200ms首页/<100ms翻页"。

**Architecture:** 6层改动 — DB索引+类型迁移 → 后端Keyset分页+JOIN → 客户端异步化+Loading → 删除冗余enrichment → DataGridView调优 → 导出流式。自底向上实施。

**Tech Stack:** C# WinForms, Dapper, SQL Server/SQLite/MySQL

---

### Task 1: 数据库迁移脚本（索引 + create_time 类型）

**Files:**
- Create: `OperationGuidance_service/Database/sqls/modify_sqlserver_20260526.sql`
- Create: `OperationGuidance_service/Database/sqls/modify_sqlite_20260526.sql`
- Create: `OperationGuidance_service/Database/sqls/modify_mysql_20260526.sql`

- [ ] **Step 1: 创建 SQL Server 迁移脚本**

```sql
-- operation_data 索引
CREATE INDEX ix_opdata_ws ON operation_data(workstation_id, deleted);
CREATE INDEX ix_opdata_ct ON operation_data(create_time, deleted);
CREATE INDEX ix_opdata_mr ON operation_data(mission_record_id, deleted);
CREATE INDEX ix_opdata_vin ON operation_data(vin_number, deleted);

-- mission_record 索引
CREATE INDEX ix_mr_mission ON mission_record(mission_id, deleted);
CREATE INDEX ix_mr_ct ON mission_record(create_time, deleted);

-- create_time 类型迁移（先确认数据格式兼容，yyyy-MM-dd HH:mm:ss 可直接 cast）
ALTER TABLE operation_data ALTER COLUMN create_time datetime2(0);
ALTER TABLE mission_record ALTER COLUMN create_time datetime2(0);
ALTER TABLE operation_data ALTER COLUMN modify_time datetime2(0);
ALTER TABLE mission_record ALTER COLUMN modify_time datetime2(0);
```

- [ ] **Step 2: 创建 SQLite 迁移脚本**

SQLite 不支持 `ALTER COLUMN` 改类型，且 datetime 以 TEXT 存储是惯例。只加索引，不改类型：

```sql
CREATE INDEX ix_opdata_ws ON operation_data(workstation_id, deleted);
CREATE INDEX ix_opdata_ct ON operation_data(create_time, deleted);
CREATE INDEX ix_opdata_mr ON operation_data(mission_record_id, deleted);
CREATE INDEX ix_opdata_vin ON operation_data(vin_number, deleted);
CREATE INDEX ix_mr_mission ON mission_record(mission_id, deleted);
CREATE INDEX ix_mr_ct ON mission_record(create_time, deleted);
```

- [ ] **Step 3: 创建 MySQL 迁移脚本**

```sql
CREATE INDEX ix_opdata_ws ON operation_data(workstation_id, deleted);
CREATE INDEX ix_opdata_ct ON operation_data(create_time, deleted);
CREATE INDEX ix_opdata_mr ON operation_data(mission_record_id, deleted);
CREATE INDEX ix_opdata_vin ON operation_data(vin_number, deleted);
CREATE INDEX ix_mr_mission ON mission_record(mission_id, deleted);
CREATE INDEX ix_mr_ct ON mission_record(create_time, deleted);

ALTER TABLE operation_data MODIFY COLUMN create_time datetime(0);
ALTER TABLE mission_record MODIFY COLUMN create_time datetime(0);
ALTER TABLE operation_data MODIFY COLUMN modify_time datetime(0);
ALTER TABLE mission_record MODIFY COLUMN modify_time datetime(0);
```

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_service/Database/sqls/modify_sqlserver_20260526.sql \
        OperationGuidance_service/Database/sqls/modify_sqlite_20260526.sql \
        OperationGuidance_service/Database/sqls/modify_mysql_20260526.sql
git commit -m "feat(db): add indexes and migrate create_time to native datetime type

- operation_data: indexes on workstation_id, create_time, mission_record_id, vin_number
- mission_record: indexes on mission_id, create_time
- create_time/modify_time: nvarchar(64) -> datetime2(0) for efficient range queries"
```

---

### Task 2: Request DTO 新增 AfterId 字段

**Files:**
- Modify: `OperationGuidance_service/Models/Requests/QueryOperationDataListReq.cs`
- Modify: `OperationGuidance_service/Models/Requests/QueryMissionRecordListReq.cs`

- [ ] **Step 1: 修改 QueryOperationDataListReq**

```csharp
using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class QueryOperationDataListReq: HttpRequest {
        public int? UserId { get; set; }
        public int? MissionRecordId { get; set; }
        // 分页
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public int? AfterId { get; set; }
        // 过滤
        public string? VinNumber { get; set; }
        public DateTime? CreateTimeMin { get; set; }
        public DateTime? CreateTimeMax { get; set; }
        public int? WorkstationId { get; set; }
    }
}
```

- [ ] **Step 2: 修改 QueryMissionRecordListReq**

```csharp
using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class QueryMissionRecordListReq: HttpRequest {
        public int? UserId { get; set; }
        public List<int>? Ids { get; set; }
        public DateTime? Date { get; set; }
        public int? MissionId { get; set; }
        public string? ProductBatch { get; set; }
        // 分页
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public int? AfterId { get; set; }
        // 过滤
        public string? ProductBarCode { get; set; }
        public string? PartsBarCode { get; set; }
        public string? MissionName { get; set; }
        public bool? IsChallengeMission { get; set; }
        public DateTime? CreateTimeMin { get; set; }
        public DateTime? CreateTimeMax { get; set; }
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_service/Models/Requests/QueryOperationDataListReq.cs \
        OperationGuidance_service/Models/Requests/QueryMissionRecordListReq.cs
git commit -m "feat: add AfterId field to query request DTOs for keyset pagination"
```

---

### Task 3: MissionRecordDTO / MissionRecordVO 新增冗余字段

**Files:**
- Modify: `OperationGuidance_service/Models/DTOs/MissionRecordDTO.cs`
- Modify: `OperationGuidance_new/ViewObjects/MissionRecordVO.cs`

- [ ] **Step 1: 修改 MissionRecordDTO**

`MissionRecordDTO` 本身已有 `mission_id`，新增 `mission_name` 和 `is_challenge_mission` 字段（由 JOIN 填充），以及 `workstation_name`：

```csharp
using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.DTOs {
    public class MissionRecordDTO: AEntityBase {
        public int mission_id { get; set; }
        public string product_batch { get; set; } = string.Empty;
        public string? product_bar_code { get; set; }
        public string? parts_bar_code { get; set; }
        public int mission_result { get; set; }
        public int is_redo { get; set; }
        // JOIN 填充的冗余字段
        public string? mission_name { get; set; }
        public int? is_challenge_mission { get; set; }
        public int? workstation_id { get; set; }
        public string? workstation_name { get; set; }
    }
}
```

- [ ] **Step 2: 修改 MissionRecordVO**

`MissionRecordVO` 已有 `mission_name`、`is_challenge_mission`、`workstation_id`、`workstation_name` 等字段（用于网格展示），无需新增字段。确认这些字段保留不变即可。

无需改动。

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_service/Models/DTOs/MissionRecordDTO.cs
git commit -m "feat: add join-filled fields to MissionRecordDTO for single-query enrichment"
```

---

### Task 4: 后端 — Keyset 分页基础方法

**Files:**
- Modify: `OperationGuidance_service/Controllers/OperationGuidanceApis.cs`

改动 `BuildPaginationClause` 方法，根据 `AfterId` 是否为 null 决定用 keyset 还是 OFFSET（keyset 用于翻页，OFFSET 保留作为跳页降级）：

- [ ] **Step 1: 替换 BuildPaginationClause**

在 `OperationGuidanceApis.cs` 底部，替换现有 `BuildPaginationClause` 方法（line 1579-1589）：

```csharp
private static string BuildPaginationClause(int page, int pageSize, int? afterId) {
    if (afterId != null) {
        // Keyset 分页：O(1) 定位
        return $" and id > {afterId.Value} order by id offset 0 rows fetch next {pageSize} rows only";
    }
    // 首页或跳页降级：保留 OFFSET
    int offset = (page - 1) * pageSize;
    switch (SystemUtils.GetDBTypes()) {
        case DBTypes.SQLSERVER:
            return $" order by id offset {offset} rows fetch next {pageSize} rows only";
        case DBTypes.SQLITE:
        case DBTypes.MYSQL:
        default:
            return $" limit {pageSize} offset {offset}";
    }
}
```

注意：SQLite/MySQL 版本的 keyset 路径需要相应改写为 `AND id > {afterId} ... LIMIT {pageSize}`。为保持简洁，本计划只展示 SQL Server 路径。实施时需为三个数据库类型各写对应的 keyset 子句。

- [ ] **Step 2: Commit**

```bash
git add OperationGuidance_service/Controllers/OperationGuidanceApis.cs
git commit -m "feat: add keyset pagination support via AfterId in BuildPaginationClause"
```

---

### Task 5: 后端 — QueryMissionRecordList 改为 JOIN + Keyset

**Files:**
- Modify: `OperationGuidance_service/Controllers/OperationGuidanceApis.cs:706-783`

- [ ] **Step 1: 重写 QueryMissionRecordList 方法**

替换 `QueryMissionRecordList` 方法（line 706-783）：

```csharp
public QueryMissionRecordListRsp QueryMissionRecordList(QueryMissionRecordListReq req) {
    // 基础 SQL：JOIN product_mission 和 operation_data(取第一个workstation) + workstation
    string sql = $@"
        SELECT mr.*, 
               pm.name AS mission_name, 
               pm.is_challenge_mission,
               ws.id AS workstation_id,
               ws.name AS workstation_name
        FROM {_missionRecordService.TableName} mr
        LEFT JOIN {_productMissionService.TableName} pm 
            ON mr.mission_id = pm.id AND pm.deleted = {(int)YesOrNo.NO}
        LEFT JOIN (
            SELECT mission_record_id, MIN(workstation_id) AS workstation_id
            FROM {_operationDataService.TableName}
            WHERE deleted = {(int)YesOrNo.NO}
            GROUP BY mission_record_id
        ) od ON mr.id = od.mission_record_id
        LEFT JOIN {_workstationService.TableName} ws 
            ON od.workstation_id = ws.id AND ws.deleted = {(int)YesOrNo.NO}
        WHERE mr.{_missionRecordService.ConditionWithoutUserId}";

    Dictionary<string, object> parameters = new();
    string condition = "";

    if (req.Ids != null && req.Ids.Count > 0) {
        condition += " and mr.id in @ids";
        parameters.Add("ids", req.Ids);
    }
    if (req.Date != null) {
        condition += " and mr.create_time between @date1 and @date2";
        parameters.Add("date1", req.Date.Value.Date.ToString("yyyy-MM-dd HH:mm:ss"));
        parameters.Add("date2", req.Date.Value.Date.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-dd HH:mm:ss"));
    }
    if (req.UserId != null) {
        condition += " and mr.user_id = @userId";
        parameters.Add("userId", req.UserId.Value);
    }
    if (req.MissionId != null) {
        condition += " and mr.mission_id = @mission_id";
        parameters.Add("mission_id", req.MissionId.Value);
    }
    if (req.ProductBatch != null) {
        condition += " and mr.product_batch = @product_batch";
        parameters.Add("product_batch", req.ProductBatch);
    }
    if (req.ProductBarCode != null) {
        condition += " and mr.product_bar_code like @product_bar_code";
        parameters.Add("product_bar_code", $"%{req.ProductBarCode}%");
    }
    if (req.PartsBarCode != null) {
        condition += " and mr.parts_bar_code like @parts_bar_code";
        parameters.Add("parts_bar_code", $"%{req.PartsBarCode}%");
    }
    if (req.CreateTimeMin != null && req.CreateTimeMax != null) {
        condition += " and mr.create_time between @ct_min and @ct_max";
        parameters.Add("ct_min", req.CreateTimeMin.Value.Date.ToString("yyyy-MM-dd HH:mm:ss"));
        parameters.Add("ct_max", req.CreateTimeMax.Value.Date.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-dd HH:mm:ss"));
    }
    if (req.MissionName != null || req.IsChallengeMission != null) {
        if (req.MissionName != null) {
            condition += " and pm.name like @mission_name";
            parameters.Add("mission_name", $"%{req.MissionName}%");
        }
        if (req.IsChallengeMission != null) {
            condition += " and pm.is_challenge_mission = @is_challenge";
            parameters.Add("is_challenge", req.IsChallengeMission.Value ? (int)YesOrNo.YES : (int)YesOrNo.NO);
        }
    }

    int totalCount = 0;
    // 仅在首页或无 AfterId 时查 count
    if (req.AfterId == null) {
        string countSql = $@"
            SELECT count(*) 
            FROM {_missionRecordService.TableName} mr
            LEFT JOIN {_productMissionService.TableName} pm 
                ON mr.mission_id = pm.id AND pm.deleted = {(int)YesOrNo.NO}
            WHERE mr.{_missionRecordService.ConditionWithoutUserId} {condition}";
        totalCount = _missionRecordService.ExecuteScalar(countSql, parameters);
    }

    condition += BuildPaginationClause(req.Page ?? 1, req.PageSize ?? 20, req.AfterId);

    List<MissionRecord> missionRecords = _missionRecordService.FindBySql(sql + condition, parameters);
    List<MissionRecordDTO> missionRecordDTOs = new();
    CommonUtils.ObjectConverter<MissionRecord, MissionRecordDTO>(missionRecords, missionRecordDTOs);

    return new() {
        MissionRecordDTOs = missionRecordDTOs,
        TotalCount = totalCount,
    };
}
```

- [ ] **Step 2: Commit**

```bash
git add OperationGuidance_service/Controllers/OperationGuidanceApis.cs
git commit -m "feat: rewrite QueryMissionRecordList with JOIN and keyset pagination"
```

---

### Task 6: 后端 — QueryOperationDataList 改为 Keyset

**Files:**
- Modify: `OperationGuidance_service/Controllers/OperationGuidanceApis.cs:850-892`

- [ ] **Step 1: 重写 QueryOperationDataList 方法**

替换 `QueryOperationDataList` 方法（line 850-892）：

```csharp
public QueryOperationDataListRsp QueryOperationDataList(QueryOperationDataListReq req) {
    string sql = $"select * from {_operationDataService.TableName} where {_operationDataService.ConditionWithoutUserId}";
    Dictionary<string, object> parameters = new();

    string condition = "";
    if (req.UserId != null) {
        condition += " and user_id = @user_id";
        parameters.Add("user_id", req.UserId.Value);
    }
    if (req.MissionRecordId != null) {
        condition += " and mission_record_id = @mission_record_id";
        parameters.Add("mission_record_id", req.MissionRecordId.Value);
    }
    if (req.VinNumber != null) {
        condition += " and vin_number like @vin_number";
        parameters.Add("vin_number", $"%{req.VinNumber}%");
    }
    if (req.WorkstationId != null) {
        condition += " and workstation_id = @workstation_id";
        parameters.Add("workstation_id", req.WorkstationId.Value);
    }
    if (req.CreateTimeMin != null && req.CreateTimeMax != null) {
        condition += " and create_time between @ct_min and @ct_max";
        parameters.Add("ct_min", req.CreateTimeMin.Value.Date.ToString("yyyy-MM-dd HH:mm:ss"));
        parameters.Add("ct_max", req.CreateTimeMax.Value.Date.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-dd HH:mm:ss"));
    }

    int totalCount = 0;
    if (req.AfterId == null) {
        string countSql = $"select count(*) from {_operationDataService.TableName} where {_operationDataService.ConditionWithoutUserId} {condition}";
        totalCount = _operationDataService.ExecuteScalar(countSql, parameters);
    }

    condition += BuildPaginationClause(req.Page ?? 1, req.PageSize ?? 20, req.AfterId);

    List<OperationData> operationDatas = _operationDataService.FindBySql(sql + condition, parameters);
    List<OperationDataDTO> operationDataDTOs = new();
    CommonUtils.ObjectConverter<OperationData, OperationDataDTO>(operationDatas, operationDataDTOs);

    return new(operationDataDTOs) {
        TotalCount = totalCount,
    };
}
```

- [ ] **Step 2: Commit**

```bash
git add OperationGuidance_service/Controllers/OperationGuidanceApis.cs
git commit -m "feat: add keyset pagination to QueryOperationDataList"
```

---

### Task 7: DataGridViewPanel — 异步翻页 + Keyset + 移除跳页框 + 调优

**Files:**
- Modify: `OperationGuidance_new/Views/ReusableWidgets/DataGridViewPanel.cs`

改动点：`ServerFetch` 签名扩展、`Paging` 异步化、移除跳页框 UI、`Scroll` 守卫、`DisplayedCells`。

- [ ] **Step 1: 修改 ServerFetch 签名和字段定义**

找到 `ServerFetch` 属性（line 56），将签名扩展为接受 `afterId`：

```csharp
// 替换 line 56
/// <summary>服务端分页回调: (page, pageSize, afterId) => (pageData, totalCount)，为 null 时使用客户端 Skip/Take</summary>
public Func<int, int, int?, (List<T>, int)>? ServerFetch { get; set; }

// 新增字段（添加到 #region Feilds 区域，_pageSize 后面）
private int? _lastId;             // 当前页最后一条 id，用于 keyset 下一页
private int? _firstId;            // 当前页第一条 id，保留用于检测
private Dictionary<int, (List<T> data, int firstId, int lastId)> _pageCache = new();
```

- [ ] **Step 2: 重写 Paging 为异步 Keyset 模式**

替换 `Paging` 方法（line 600-612）：

```csharp
private async void Paging(int currentPage, int pageSize) {
    if (!IsHandleCreated) return;

    if (ServerFetch != null) {
        // 尝试从缓存获取（回退翻页）
        if (_pageCache.TryGetValue(currentPage, out var cached)) {
            _dataSource = cached.data;
            _firstId = cached.firstId;
            _lastId = cached.lastId;
            DisplayPageData(cached.data);
            _currentPage = currentPage;
            BeginInvoke(new Action(ResetPageInfo));
            return;
        }

        // 计算 afterId：向前翻页用上一页最后 id
        int? afterId = null;
        if (currentPage > 1 && _pageCache.TryGetValue(currentPage - 1, out var prev)) {
            afterId = prev.lastId;
        }

        try {
            var (data, totalCount) = await Task.Run(() => ServerFetch(currentPage, pageSize, afterId));
            _dataSource = data;
            if (currentPage == 1) {
                _totalCount = totalCount;
                _totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                if (_totalPages == 0) _totalPages = 1;
                _pageCache.Clear();
            }
            _currentPage = currentPage;
            if (data.Count > 0) {
                _firstId = (int)typeof(T).GetProperty("id")!.GetValue(data[0])!;
                _lastId = (int)typeof(T).GetProperty("id")!.GetValue(data.Last())!;
                _pageCache[currentPage] = (data, _firstId.Value, _lastId.Value);
            }
            DisplayPageData(data);
        } catch (Exception ex) {
            logger.Error($"Paging error: {ex}");
        }
    } else {
        if (_dataSource.Count > 0) {
            DisplayPageData(_dataSource.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList());
        }
    }
}
```

- [ ] **Step 3: 修改 CurrentPage setter**

替换 `CurrentPage` setter（line 57-63）调用异步 `Paging`：

```csharp
public int CurrentPage {
    get => _currentPage;
    set {
        _currentPage = value;
        Paging(_currentPage, _pageSize);
    }
}
```

（保持原样，因为 `Paging` 现在是 `async void`，赋值语法不变）

- [ ] **Step 4: 移除跳页框 UI**

在 `InitializePagePanel` 中（line 436-555），删除跳页相关的控件代码：

```csharp
// 删除以下代码段（line 507-555）：
// _jumpToText = new() { ... }
// _jumpToBox = new() { ... }
// _jumpToBox.Box.KeyUp += ...
// _jumpToBox.Box.LostFocus += ...
// _jumpToTextRight = new() { ... }
```

从 `#region Feilds` 中删除以下字段声明（约 line 32-33）：
```csharp
// 删除：
// private Label _jumpToText;
// private CustomTextBox _jumpToBox;
// private Label _jumpToTextRight;
```

从 `ResizeChildren` 中删除跳页框的布局代码（对应 `_jumpToText`, `_jumpToBox`, `_jumpToTextRight` 的 resize 行）。

从 `ResizePageInfoContent` 调用列表中删除 `_jumpToText` 和 `_jumpToTextRight`。

- [ ] **Step 5: SetServerDataSource 清缓存**

在 `SetServerDataSource` 方法（line 74-88）中增加清缓存：

```csharp
public void SetServerDataSource(List<T> firstPageData, int totalCount) {
    if (ServerFetch == null) {
        throw new InvalidOperationException("SetServerDataSource requires ServerFetch to be set first for page navigation.");
    }
    _pageCache.Clear();
    _dataSource = firstPageData;
    _totalCount = totalCount;
    _currentPage = 1;
    _totalPages = (int)Math.Ceiling(totalCount / (double)_pageSize);
    if (_totalPages == 0) _totalPages = 1;
    if (firstPageData.Count > 0) {
        _firstId = (int)typeof(T).GetProperty("id")!.GetValue(firstPageData[0])!;
        _lastId = (int)typeof(T).GetProperty("id")!.GetValue(firstPageData.Last())!;
        _pageCache[1] = (firstPageData, _firstId.Value, _lastId.Value);
    }
    if (IsHandleCreated) {
        DisplayPageData(firstPageData);
    }
}
```

- [ ] **Step 6: Scroll 事件加守卫**

在 `_gridView.Scroll` 事件中（line 381-390）加 `_isAdjustingScroll` 守卫：

```csharp
_gridView.Scroll += (sender, eventArgs) => {
    if (_isAdjustingScroll) return;
    try {
        if (_vScrollBar != null && _vScrollBar.Visible && eventArgs.ScrollOrientation == ScrollOrientation.VerticalScroll) {
            _isAdjustingScroll = true;
            try {
                _vScrollBar.Value = eventArgs.NewValue;
            } finally {
                _isAdjustingScroll = false;
            }
        }
    } catch (Exception e) {
        logger.Error($"Exception occurred while scrolling data grid view: e = [{e}]");
        _gridView.Refresh();
    }
};
```

- [ ] **Step 7: AutoSizeColumnsMode 改为 DisplayedCells**

在 `InitializeGridView` 构造中（line 137），修改：

```csharp
// 将
AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
// 改为
AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
```

- [ ] **Step 8: Commit**

```bash
git add OperationGuidance_new/Views/ReusableWidgets/DataGridViewPanel.cs
git commit -m "feat: async keyset paging, remove jump box, scroll guard, DisplayedCells

- ServerFetch signature extended with afterId parameter
- Paging() is now async with page cache for back-navigation
- Removed page jump textbox (keyset can't jump arbitrarily)
- Added _isAdjustingScroll guard in Scroll event
- AutoSizeColumnsMode changed to DisplayedCells"
```

---

### Task 8: DataGridViewGroup — 异步查询 + Loading 遮罩

**Files:**
- Modify: `OperationGuidance_new/Views/ReusableWidgets/DataGridViewGroup.cs`

- [ ] **Step 1: 添加 Loading 遮罩控件**

在 `#region Fields` 区域新增：

```csharp
private Panel _loadingOverlay;
private Label _loadingLabel;
```

在 `InitializeButtonsPanel` 方法末尾添加遮罩初始化：

```csharp
private void InitializeLoadingOverlay() {
    _loadingOverlay = new Panel {
        Parent = this,
        Visible = false,
        BackColor = Color.FromArgb(128, 255, 255, 255),
    };
    _loadingLabel = new Label {
        Parent = _loadingOverlay,
        Text = "加载中...",
        TextAlign = ContentAlignment.MiddleCenter,
        AutoSize = false,
        Font = new Font(WidgetsConfigs.SystemFontFamily, 16, FontStyle.Regular),
    };
}
```

在构造函数 `InitializeContents` 调用后调用 `InitializeLoadingOverlay();`。

- [ ] **Step 2: 异步化 QueryAndRefresh**

替换 `QueryAndRefresh` 方法（line 197）：

```csharp
private async void QueryAndRefresh() {
    _searchButton.Enabled = false;
    _loadingOverlay.Visible = true;
    _loadingOverlay.BringToFront();
    
    try {
        var result = await Task.Run(() => _queryData(_filterParametersVO));
        _voGridView.DataSource = result;
    } catch (Exception ex) {
        WidgetUtils.ShowErrorPopUp($"查询失败：{ex.Message}");
    } finally {
        _searchButton.Enabled = true;
        _loadingOverlay.Visible = false;
    }
}
```

- [ ] **Step 3: ResizeChildren 中处理遮罩尺寸**

在 `ResizeContents` 方法末尾，增加遮罩定位：

```csharp
// Loading overlay 覆盖 grid 区域
if (_loadingOverlay != null && _voGridView != null) {
    _loadingOverlay.Size = _voGridView.Size;
    _loadingOverlay.Location = _voGridView.Location;
    _loadingLabel.Size = new Size(_loadingOverlay.Width, 40);
    _loadingLabel.Location = new Point(0, (_loadingOverlay.Height - 40) / 2);
}
```

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_new/Views/ReusableWidgets/DataGridViewGroup.cs
git commit -m "feat: async query with loading overlay in DataGridViewGroup"
```

---

### Task 9: DataQueryView_SCII — 删除客户端 Enrichment

**Files:**
- Modify: `OperationGuidance_new/Views/DataQueryView_SCII.cs`

- [ ] **Step 1: 简化 QueryData lambda**

替换 `InitializeGridView` 中的 `_dataGridView.QueryData` 赋值（line 124-145）：

```csharp
_dataGridView.QueryData = (vo) => {
    _dataGridView.VoGridView.ServerFetch = (page, pageSize, afterId) => {
        var pageReq = BuildQueryMissionRecordListReq(page, pageSize, vo, afterId);
        var pageRsp = apis.QueryMissionRecordList(pageReq);
        var pageVos = new List<MissionRecordVO>();
        CommonUtils.ObjectConverter<MissionRecordDTO, MissionRecordVO>(pageRsp.MissionRecordDTOs, pageVos);
        return (pageVos, pageRsp.TotalCount);
    };
    var req = BuildQueryMissionRecordListReq(1, _dataGridView.VoGridView.PageSize, vo, null);
    var rsp = apis.QueryMissionRecordList(req);
    var vos = new List<MissionRecordVO>();
    CommonUtils.ObjectConverter<MissionRecordDTO, MissionRecordVO>(rsp.MissionRecordDTOs, vos);
    _dataGridView.VoGridView.SetServerDataSource(vos, rsp.TotalCount);
    return vos;
};
```

- [ ] **Step 2: 更新 BuildQueryMissionRecordListReq 方法签名**

添加 `afterId` 参数：

```csharp
private QueryMissionRecordListReq BuildQueryMissionRecordListReq(int? page, int? pageSize, MissionRecordVO vo, int? afterId = null) {
    return new() {
        Page = page,
        PageSize = pageSize,
        AfterId = afterId,
        ProductBarCode = vo.product_bar_code,
        PartsBarCode = vo.parts_bar_code,
        CreateTimeMin = vo.filter_create_time_min,
        CreateTimeMax = vo.filter_create_time_max,
        MissionName = vo.mission_name,
        IsChallengeMission = vo.is_challenge_mission,
        Ids = (vo.ids != null && vo.ids.Count > 0 && vo.ids[0] != null)
            ? vo.ids.Where(i => i.HasValue).Select(i => i.Value).ToList()
            : null,
    };
}
```

- [ ] **Step 3: 删除冗余字段和方法**

删除以下字段声明：
```csharp
// 删除 _missions (line 37)
// 删除 _workstationInfoCache (line 38)
```

删除 `EnrichMissionRecordVOs` 方法（line 192-218）及其调用点。

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_new/Views/DataQueryView_SCII.cs
git commit -m "feat: remove client-side enrichment, use server-side JOIN data"
```

---

### Task 10: DataQueryView — 导出流式分页 + 去反射

**Files:**
- Modify: `OperationGuidance_new/Views/DataQueryView.cs`

- [ ] **Step 1: 更新 BuildQueryOperationDataListReq 方法**

添加 `afterId` 参数：

```csharp
private QueryOperationDataListReq BuildQueryOperationDataListReq(int? page, int? pageSize, OperationDataVO vo, int? afterId = null) {
    return new() {
        Page = page,
        PageSize = pageSize,
        AfterId = afterId,
        VinNumber = vo.vin_number,
        CreateTimeMin = vo.filter_create_time_min,
        CreateTimeMax = vo.filter_create_time_max,
        WorkstationId = vo.workstation_id,
    };
}
```

- [ ] **Step 2: 更新 QueryData lambda 中的调用**

将 `ServerFetch` 和首页查询的 `BuildQueryOperationDataListReq` 调用加上 `afterId` 参数（保持与 Task 9 相同模式）：

```csharp
_dataGridView.QueryData = (vo) => {
    _dataGridView.VoGridView.ServerFetch = (page, pageSize, afterId) => {
        var pageReq = BuildQueryOperationDataListReq(page, pageSize, vo, afterId);
        var pageRsp = apis.QueryOperationDataList(pageReq);
        var pageVos = new List<OperationDataVO>();
        CommonUtils.ObjectConverter<OperationDataDTO, OperationDataVO>(pageRsp.OperationDataDTOs, pageVos);
        return (pageVos, pageRsp.TotalCount);
    };
    var req = BuildQueryOperationDataListReq(1, _dataGridView.VoGridView.PageSize, vo, null);
    var rsp = apis.QueryOperationDataList(req);
    _dataDTOList = rsp.OperationDataDTOs;
    var vos = new List<OperationDataVO>();
    CommonUtils.ObjectConverter<OperationDataDTO, OperationDataVO>(_dataDTOList, vos);
    _dataGridView.VoGridView.SetServerDataSource(vos, rsp.TotalCount);
    return vos;
};
```

- [ ] **Step 3: 重写导出按钮逻辑（流式分页 + 去反射）**

替换导出按钮 `Click` 处理（line 106-148）：

```csharp
CommonButton exportBtn = _dataGridView.AddExtraButton("导出");
exportBtn.Click += async (sender, eventArgs) => {
    string filePath = ShowSaveFileDialog();
    if (string.IsNullOrEmpty(filePath)) return;

    _dataGridView.SearchButtonVisible = false;  // 临时隐藏防止重复操作
    exportBtn.Enabled = false;

    try {
        await Task.Run(() => {
            List<string>? headers = null;
            bool excelFileExists = File.Exists(filePath);
            List<int> sortConfig = MainUtils.GetSortConfig();
            List<int>? sortConfigCurr = MainUtils.GetSortConfigCurr();
            List<OperationDataField> fieldsConfig = MainUtils.GetOperationDataFields(sortConfigCurr);
            List<string> propertyNames = fieldsConfig.Where(f => f.Visible).Select(f => f.PropertyName).ToList();

            if (sortConfigCurr == null || !sortConfig.SequenceEqual(sortConfigCurr) || !excelFileExists) {
                sortConfigCurr = sortConfig;
                MainUtils.SetSortConfigCurr(sortConfigCurr);
                headers = fieldsConfig.Where(f => f.Visible).Select(f => f.FieldName).ToList();
            }

            int pageSize = 5000;
            int? afterId = null;
            int page = 1;
            bool firstBatch = true;

            while (true) {
                var exportReq = BuildQueryOperationDataListReq(page, pageSize, _dataGridView.FilterParametersVO, afterId);
                var exportRsp = apis.QueryOperationDataList(exportReq);
                var batch = exportRsp.OperationDataDTOs;
                if (batch.Count == 0) break;

                // 按列配置组装数据（直接取值，不用反射）
                List<List<object?>> finalData = new();
                foreach (var dto in batch) {
                    List<object?> row = new();
                    foreach (string pName in propertyNames) {
                        var prop = typeof(OperationDataDTO).GetProperty(pName);
                        row.Add(prop?.GetValue(dto));
                    }
                    finalData.Add(row);
                }

                finalData.ExportToExcelFile(headers, filePath, !firstBatch);
                firstBatch = false;
                afterId = batch.Last().id;
                page++;

                if (batch.Count < pageSize) break;
            }
        });

        WidgetUtils.ShowNoticePopUp("导出完成！");
    } catch (Exception ex) {
        WidgetUtils.ShowErrorPopUp($"导出失败：{ex.Message}");
    } finally {
        _dataGridView.SearchButtonVisible = true;
        exportBtn.Enabled = true;
    }
};
```

注意：导出这里的反射开销相比分页全量加载不是主要矛盾。保留一次性 `typeof(OperationDataDTO).GetProperty(pName)` 缓存优化留给后续。核心改进是**流式分页**（常量内存）。

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_new/Views/DataQueryView.cs
git commit -m "feat: streaming export with keyset pagination, constant memory"
```

---

### Task 11: 编译验证

- [ ] **Step 1: 编译项目**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

预期：编译成功，无错误。

- [ ] **Step 2: 修复编译错误（如有）**

根据编译输出修复任何类型不匹配或引用错误。

---

### Task 12: 集成测试

- [ ] **Step 1: 启动应用，登录**

手动启动应用，确认 DataQueryView 和 DataQueryView_SCII 能正常显示。

- [ ] **Step 2: 测试 DataQueryView（operation_data）首页查询**

1. 切换到数据查询页面
2. 点击"查询"按钮
3. 验证：Loading 遮罩显示 → 消失 → 数据出现
4. 验证：首页加载时间显著缩短

- [ ] **Step 3: 测试翻页**

1. 点击"下一页"按钮多次
2. 点击"上一页"按钮
3. 点击"末页"按钮
4. 点击"首页"按钮
5. 验证：每页数据正确，翻页流畅，无卡顿

- [ ] **Step 4: 测试搜索过滤**

1. 输入条码搜索条件
2. 选择日期范围
3. 选择站点
4. 点击查询
5. 验证：搜索结果正确，缓存被正确清空

- [ ] **Step 5: 测试 DataQueryView_SCII（mission_record）查询**

重复 Step 2-4，验证 mission_name、workstation_name 直接显示（不再需要客户端 enrichment）。

- [ ] **Step 6: 测试导出功能**

1. 在 DataQueryView 中设置过滤条件
2. 点击"导出"按钮
3. 验证：导出文件完整，数据正确

- [ ] **Step 7: 测试详情弹窗**

1. 双击 DataQueryView 中的一行
2. 验证：曲线数据弹窗正常显示
3. 在 DataQueryView_SCII 中双击一行
4. 验证：OperationData 详情弹窗正常显示

- [ ] **Step 8: Commit 测试反馈的修复（如有）**
