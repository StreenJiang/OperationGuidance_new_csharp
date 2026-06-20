# 永茂泰（YMT）厂商 — 工作台与日聚合导出设计

**日期:** 2026-06-20
**关联枚举:** `AppVersion.YMT = 7`
**厂商:** 永茂泰

## 概述

永茂泰（YMT）使用标准版工作台布局，导出逻辑与所有其他厂商不同：每天每个任务一个文件，
所有拧紧数据追加写入同一文件（而非每次完成生成独立文件）。当导出字段配置在中途变化时，
新表头以空行分隔追加到同一文件中。

## 导出规则

### 路径

```
{StoragePath}/yyyy-MM/MissionName-yyyy-MM-dd.xlsx
{StoragePath}/yyyy-MM/MissionName-yyyy-MM-dd.txt
```

- `yyyy-MM`: 年-月，如 `2026-06`
- `MissionName`: 任务名称
- `yyyy-MM-dd`: 年-月-日，如 `2026-06-20`

> 与标准版不同：YMT 路径不含 `WorkstationName`、不含 `ProductBatch`、不含 `BarCode_Timestamp_Result`，
> 所有数据按任务 + 日期聚合到一个文件中。

### 多表头共存

每次导出写入时：
1. 构建本次 header（可见字段名列表）和 data rows
2. 若文件不存在 → 创建目录，写入 header + data rows
3. 若文件存在 → 读取已有首行 header，与本次 header 比较
   - **一致**（字段名相同、数量相同、顺序相同）→ 追加 data rows（不重复写 header）
   - **不一致** → 追加 1 空行 + 新 header + data rows

"不一致"的场景：管理员在执行任务中修改了导出字段配置（增减可见列、调整顺序）。

### 并发安全

使用静态 `ConcurrentDictionary<string, SemaphoreSlim>` 按文件路径加锁，
防止多个任务同时写同一文件导致 Excel 文件锁定冲突。

## 架构

### 基类改动（最小侵入）

`AWorkplaceContentPanel.OnMissionCompleted` 中仅改动 1 行：

```csharp
// 原来
await new DataExportService().ExportAsync(request);

// 改为
await ExportDataAsync(request);
```

新增 virtual 方法：

```csharp
protected virtual async Task ExportDataAsync(ExportRequest request) {
    await new DataExportService().ExportAsync(request);
}
```

其余逻辑（guard、排空、snapshot、回填 parts_bar_code、构造 ExportRequest、clear、UI refresh）全部不变。

### 文件变更

| 文件 | 操作 | 说明 |
|------|------|------|
| `Views/AbstractViews/AWorkplaceContentPanel.cs` | 修改 1 行 + 新增 4 行 | 抽取 `ExportDataAsync` virtual 方法 |
| `Views/AbstractViews/AVariableSettingsView.cs` | 修改 1 行 + 新增 4 行 | 抽取 `ExportTestAsync` virtual 方法 |
| `Views/WorkplaceMissionView_YMT.cs` | **新建** | 继承标准版 WorkPlaceContentPanel，override `ExportDataAsync` |
| `Views/VariableSettingsView_YMT.cs` | **新建** | 继承标准版 VariableSettingsView，override `ExportTestAsync` |
| `Utils/YmtDataExportService.cs` | **新建** | YMT 日聚合导出服务 |
| `Configs/SystemConfigs.cs` | 修改 | 注册 YMT 到工作台 + 系统设置菜单 |

### WorkplaceContentPanel_YMT

继承 `WorkplaceContentPanel`（STANDARD），零布局代码。仅 override 导出路由：

```csharp
public class WorkplaceContentPanel_YMT: WorkplaceContentPanel {
    public WorkplaceContentPanel_YMT() { }
    public WorkplaceContentPanel_YMT(int? missionId, Action<string> resetMissionName)
        : base(missionId, resetMissionName) { }

    // 导出开关(BasePath/SortConfig) 继承自 WorkplaceContentPanel → 读 ExportConfig.Instance
    protected override async Task ExportDataAsync(ExportRequest request) {
        await new YmtDataExportService().ExportAsync(request);
    }
}
```

### YmtDataExportService

```
ExportAsync(request):
  1. 从 request.Fields 提取可见字段名 → headers, propertyNames
  2. BuildRows(data, propertyNames)
  3. 计算路径: basePath/yyyy-MM/MissionName-yyyy-MM-dd.{xlsx|txt}
  4. 按文件路径获取 SemaphoreSlim，await WaitAsync
  5. 若启用 Excel → AppendOrCreateExcel
  6. 若启用 Txt  → AppendOrCreateTxt
  7. 释放信号量

AppendOrCreateExcel(path, headers, rows):
  - 文件不存在 → 创建 + 写 header + rows
  - 文件存在 → 用 ClosedXML 读取 sheet 首行 header
    - 一致 → 追加 rows
    - 不一致 → 加 1 空行 + 新 header + rows

AppendOrCreateTxt(path, headers, rows):
  - 文件不存在 → 创建 + 写 header + rows（Tab 分隔）
  - 文件存在 → 读首行 header 比对
    - 一致 → 追加 rows
    - 不一致 → 加 1 空行 + 新 header + rows
```

### 菜单注册

在 `SystemConfigs.cs`：

```csharp
// 工作台
{AppVersion.YMT, typeof(WorkplaceMissionView_YMT)}

// 系统设置（YMT 专用 VariableSettingsView，override ExportTestAsync → YmtDataExportService）
{AppVersion.YMT, typeof(VariableSettingsView_YMT)}
```

### VariableSettingsView_YMT

继承 `VariableSettingsView`（标准版），零 UI 代码。仅 override 导出测试路由：

```csharp
public class VariableSettingsView_YMT: VariableSettingsView {
    protected override async Task ExportTestAsync(ExportRequest request) {
        await new YmtDataExportService().ExportAsync(request);
    }
}
```

> 基类 `AVariableSettingsView` 中 `RunExportTest` 方法原直接调用 `DataExportService`，
> 现抽取为 `protected virtual Task ExportTestAsync(ExportRequest)`，默认行为不变。

## 不变更项

- `DataExportService` — 原样保留，标准版等仍使用
- `AppVersion.YMT = 7` — 已存在，不修改
- XAIA（西艾爱）/ SCII_XT — 零改动
- 所有其他厂商 — 零改动
- `ExportRequest` / `ExportConfig` — 不变

## 自检

- [ ] YMT 枚举值已存在
- [ ] 基类改动 ≤ 5 行
- [ ] 不过度设计——无接口、无策略模式、无消息队列
- [ ] XAIA/SCII_XT 零改动
- [ ] 并发安全：per-file SemaphoreSlim
- [ ] Excel + TXT 双格式均可开关
- [ ] 多表头共存：空行分隔
