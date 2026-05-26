# DataQueryView 查询性能优化设计

## 目标

`DataQueryView` / `DataQueryView_SCII` 在 40 万+ 数据量下 UI 卡死、查询缓慢。优化后：千万级 `operation_data`、百万级 `mission_record` 下首页加载 < 200ms，翻页 < 100ms，UI 全程不卡顿。

## 当前瓶颈（6 项）

1. **UI 线程同步阻塞** — `QueryData` lambda 在 UI 线程同步调用 HTTP API，40 万数据时 count(*) + 数据查询串行执行，WinForms 消息泵阻塞
2. **每次查询重复拉全量 ProductMission** — `QueryProductMissions` 无分页拉全部任务，只为给当前页 20 条记录填 `mission_name`
3. **EnrichMissionRecordVOs O(n*m)** — `List.SingleOrDefault` 在 foreach 内，整体 O(n*m)
4. **OFFSET 深分页** — `ORDER BY id OFFSET N ROWS FETCH NEXT 20 ROWS ONLY`，大 offset 时扫描所有前置行
5. **DataGridView 过度绘制** — `AutoSizeColumnsMode = AllCells`，CellPainting 每次创建对象
6. **导出全量加载** — 不分页拉全部数据 + 反射逐行处理，千万级 OOM

## 优化方案

### 1. UI 异步化 + Loading 遮罩

**文件**: `DataGridViewGroup.cs`, `DataGridViewPanel.cs`

- `QueryAndRefresh()` 改为 `async void`，`_queryData(...)` 包进 `Task.Run`
- `Paging()` 改为 `async void`，`ServerFetch` 调用包进 `Task.Run`
- 查询/翻页期间：`_searchButton.Enabled = false` + 半透明 Loading 遮罩覆盖 DataGridView 区域
- `DisplayPageData` / `LoadDataAsync` 保持现有 `BeginInvoke` 模式回 UI 线程
- `QueryData` lambda 签名不变，View 子类无需感知异步化

### 2. Keyset 分页替代 OFFSET

**文件**: `DataGridViewPanel.cs`, `OperationGuidanceApis.cs`, Req/Rsp DTO

- 新增 `AfterId` 字段到 `QueryOperationDataListReq` / `QueryMissionRecordListReq`
- 首页 `after_id = null`，翻页携带上一页最后一条的 id
- SQL：`WHERE id > @after_id AND ... ORDER BY id FETCH NEXT 20 ROWS ONLY`
- 翻页 O(1) 定位，与数据总量无关
- **移除页码跳转框**（深分页跳页本身无意义），仅保留首页/上一页/下一页/末页
- `ServerFetch` 签名扩展为 `(page, pageSize, afterId) => (data, totalCount, lastId)`
- Count 查询仅在首页执行，翻页沿用同一个 totalCount

### 3. SQL JOIN 替代客户端 Enrichment

**文件**: `OperationGuidanceApis.cs`, `DataQueryView_SCII.cs`

`QueryMissionRecordList` SQL 改为 JOIN：

```sql
SELECT mr.*, pm.name AS mission_name, pm.is_challenge_mission,
       wi.info AS workstation_info
FROM mission_record mr
LEFT JOIN product_mission pm ON mr.mission_id = pm.id AND pm.deleted = 0
LEFT JOIN (...) wi ON ...
WHERE mr.deleted = 0 AND ...
```

`MissionRecordDTO` / `MissionRecordVO` 新增 `mission_name`、`is_challenge_mission`、`workstation_name` 冗余字段。

删除客户端的 `EnrichMissionRecordVOs` 方法、`_missions` 字段、`_workstationInfoCache` 字典。每次查询从 3 次 API 调用降为 1 次。

### 4. 数据库索引 + create_time 类型迁移

**文件**: 新增 `modify_sqlserver_20260526.sql` / `modify_sqlite_20260526.sql` / `modify_mysql_20260526.sql`

#### operation_data

```sql
CREATE INDEX ix_opdata_ws ON operation_data(workstation_id, deleted);
CREATE INDEX ix_opdata_ct ON operation_data(create_time, deleted);
CREATE INDEX ix_opdata_mr ON operation_data(mission_record_id, deleted);
CREATE INDEX ix_opdata_vin ON operation_data(vin_number, deleted);
```

#### mission_record

```sql
CREATE INDEX ix_mr_mission ON mission_record(mission_id, deleted);
CREATE INDEX ix_mr_ct ON mission_record(create_time, deleted);
```

#### create_time 类型迁移（SQL Server）

```sql
ALTER TABLE operation_data ALTER COLUMN create_time datetime2(0);
ALTER TABLE mission_record ALTER COLUMN create_time datetime2(0);
```

注意：`create_time` 当前为 `nvarchar(64)` 存储 `yyyy-MM-dd HH:mm:ss` 格式字符串。改为 `datetime2(0)` 后，索引效率从 ~40 字节降为 8 字节，范围查询可走 index seek。所有读写 `create_time` 的代码需验证兼容性（Dapper 可自动处理 `datetime2` ↔ `DateTime`/`string` 映射）。

### 5. DataGridView 调优

**文件**: `DataGridViewPanel.cs`

- `AutoSizeColumnsMode` 从 `AllCells` 改为 `DisplayedCells`
- `Scroll` 事件加 `_isAdjustingScroll` 守卫，避免与自定义 `VScrollBar.ValueChanged` 递归触发
- ToggleButton `CellPainting` 中 size 计算移到 `ResizeChildren`（列宽调整不频繁），不再每个 paint 事件计算

### 6. 导出流式分页

**文件**: `DataQueryView.cs`

- Keyset 分页循环拉取，每批 5000 条
- 每批写入 Excel 后释放内存，常量内存占用
- 去掉逐行反射，改为编译委托或按已知属性名直接取值

## 改动文件清单

| 文件 | 改动类型 |
|---|---|
| `OperationGuidance_new/Views/ReusableWidgets/DataGridViewGroup.cs` | 修改：异步化 + Loading 遮罩 |
| `OperationGuidance_new/Views/ReusableWidgets/DataGridViewPanel.cs` | 修改：异步翻页、移除跳页框、滚动守卫、DisplayedCells |
| `OperationGuidance_new/Views/DataQueryView.cs` | 修改：导出流式分页 |
| `OperationGuidance_new/Views/DataQueryView_SCII.cs` | 修改：删除 EnrichMissionRecordVOs、_missions、_workstationInfoCache |
| `OperationGuidance_service/Controllers/OperationGuidanceApis.cs` | 修改：Keyset 分页、JOIN 查询 |
| `OperationGuidance_service/Models/Requests/*.cs` | 修改：新增 AfterId 字段 |
| `OperationGuidance_service/Models/DTOs/MissionRecordDTO.cs` | 修改：新增冗余字段 |
| `OperationGuidance_new/ViewObjects/MissionRecordVO.cs` | 修改：新增冗余字段 |
| `OperationGuidance_service/Database/sqls/modify_sqlserver_20260526.sql` | 新增：索引 + 类型迁移 |
| `OperationGuidance_service/Database/sqls/modify_sqlite_20260526.sql` | 新增：索引 + 类型迁移 |
| `OperationGuidance_service/Database/sqls/modify_mysql_20260526.sql` | 新增：索引 + 类型迁移 |

## 不纳入范围

- Virtual Mode DataGridView（兼容性风险高，当前每页仅 20 行，不需要）
- 全文索引 / FTS5（LIKE 搜索优化另开任务）
- 其他 View（AccountManagement、DeviceTool 等）的 QueryList 优化
