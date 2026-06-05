# ToolTask 握手修复 + 跳过螺丝点位文件夹 + 站点查询兼容 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复三个 bug：ToolTask 握手无限重连失败、跳过螺丝点位任务不创建导出文件夹、站点查询遗漏无拧紧数据的 mission_record

**Architecture:** 三个独立修复，互不依赖，改动范围分别在 ToolTask.cs（单方法）、DataExportService.cs（单方法）、OperationGuidanceApis.cs（两个控制器方法——双源合并）

**Tech Stack:** C# / WinForms / ADO.NET（Dapper-wrapped SQL）/ .NET 6+

**Specs:**
- `docs/superpowers/specs/2026-06-05-tooltask-handshake-fix-design.md`
- `docs/superpowers/specs/2026-06-05-skip-screw-export-and-station-query-fix-design.md`

---

## File Map

| File | Action | Responsibility |
|---|---|---|
| `OperationGuidance_new/Tasks/ToolTask.cs` | Modify | `SendAndReceiveOnlyForPreparingAsync` — 握手收发 |
| `OperationGuidance_new/Utils/DataExportService.cs` | Modify | `ExportAsync` — 创建目录 + 写文件 |
| `OperationGuidance_service/Controllers/OperationGuidanceApis.cs` | Modify | 站点聚合查询——控制器层双源合并 |

---

### Task 1: ToolTask 握手 — ReceiveAsync + 移除递归重发

**Files:**
- Modify: `OperationGuidance_new/Tasks/ToolTask.cs:438-458`

- [ ] **Step 1: 将 lock 内的同步 Receive 改为锁外 ReceiveAsync**

找到 `SendAndReceiveOnlyForPreparingAsync` 方法中 line 435-445，替换 lock 块：

```csharp
// 之前 (lines 435-445)
                    // Send command and receive response under lock for socket safety
                    byte[] msgBytes = new byte[1024 * 1024];
                    int msgLen;
                    lock (SyncObject) {
                        if (!Connected) {
                            logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Handshake send/receive aborted - disconnected");
                            return null;
                        }
                        socketClient.Send(data);
                        msgLen = socketClient.Receive(new ArraySegment<byte>(msgBytes), SocketFlags.None);
                    }

// 之后
                    // Send under lock for socket safety, ReceiveAsync outside lock (no timeout)
                    byte[] msgBytes = new byte[1024 * 1024];
                    lock (SyncObject) {
                        socketClient.Send(data);
                    }
                    int msgLen = await socketClient.ReceiveAsync(new ArraySegment<byte>(msgBytes), SocketFlags.None);
```

- [ ] **Step 2: 移除 catch 块中的递归重发**

找到 line 456-458，替换 catch 块：

```csharp
// 之前 (lines 456-458)
                } catch (Exception e) {
                    logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] Send/receive error", e);
                    return await SendAndReceiveOnlyForPreparingAsync(command);
                }

// 之后
                } catch (Exception e) {
                    logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] Handshake send/receive error", e);
                    return null;
                }
```

- [ ] **Step 3: ConnectToServer 失败时无条件关闭 socket**

找到 `ConnectToServer` 方法中 line 374-378，替换：

```csharp
// 之前 (lines 374-378)
                    if (socketClient != null && socketClient.Connected && MainUtils.PingHost(_ip)) {
                        socketClient.Close();
                        socketClient = null;
                    }

// 之后
                    socketClient?.Close();
                    socketClient = null;
```

`Socket.Connected` 在 `ReceiveAsync` 抛异常后可能返回 false，导致 socket 不释放。改为与 `CloseToTriggerReconnection` / `CloseConnection` 一致的无条件关闭模式。

- [ ] **Step 4: 构建验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

预期：Build succeeded，0 errors

- [ ] **Step 5: Commit**

```bash
git add OperationGuidance_new/Tasks/ToolTask.cs
git commit -m "fix(tooltask): restore ReceiveAsync in handshake, remove recursive retry, fix socket cleanup

- Sync Receive inside lock timed out at 200ms; PF6000-OP needs 300-800ms
- Recursive retry sent duplicate commands, causing protocol state corruption (0004)
- ReceiveAsync (task-based) has no timeout — waits indefinitely for handshake response
- On exception, return null to let Connect() outer loop close socket and retry cleanly
- Socket.Connected check after exception unreliable — always close on failure
- Send stays in lock for thread safety; ReceiveAsync outside lock — RunTask not yet started"
```

---

### Task 2: DataExportService — 空数据时仍然创建目录和文件

**Files:**
- Modify: `OperationGuidance_new/Utils/DataExportService.cs:24-48`

**需求：** 每个任务（含跳过螺丝点位）必须产出文件夹 + .xlsx（若开启）+ .txt（若开启），带完整表头。0 行数据 = 纯表头空文件。

- [ ] **Step 1: 移除空数据提前 return，将目录创建提前**

找到 `ExportAsync` 方法，将 lines 24-48 替换为：

```csharp
// 之前 (lines 24-48)
        public async Task ExportAsync(ExportRequest request) {
            var data = request.Data ?? new List<OperationDataVO>();
            if (data.Count == 0) {
                _logger.Warn("[DataExport] ExportAsync skipped: no data");
                return;
            }
            string workstation = string.IsNullOrEmpty(request.WorkstationName) ? "null" : request.WorkstationName;
            string mission = string.IsNullOrEmpty(request.MissionName) ? "null" : request.MissionName;
            string date = request.CompletedAt.ToString("yyyy-MM-dd");
            string batch = string.IsNullOrEmpty(request.ProductBatch) ? "null" : request.ProductBatch;
            string batchFolder = Path.Combine(request.BasePath, workstation, mission, date, batch);
            string barCode = string.IsNullOrEmpty(request.ProductBarCode) ? "null" : request.ProductBarCode;
            string timestamp = request.CompletedAt.ToString("yyyyMMdd_HHmmss");
            string fileNameBody = $"{barCode}_{timestamp}_{request.Result}";

            try {
                Directory.CreateDirectory(batchFolder);
            } catch (Exception ex) {
                _logger.Error($"[DataExport] Failed to create directory: {batchFolder}", ex);
                throw new IOException($"无法创建导出目录: {batchFolder}", ex);
            }

// 之后
        public async Task ExportAsync(ExportRequest request) {
            var data = request.Data ?? new List<OperationDataVO>();
            string workstation = string.IsNullOrEmpty(request.WorkstationName) ? "null" : request.WorkstationName;
            string mission = string.IsNullOrEmpty(request.MissionName) ? "null" : request.MissionName;
            string date = request.CompletedAt.ToString("yyyy-MM-dd");
            string batch = string.IsNullOrEmpty(request.ProductBatch) ? "null" : request.ProductBatch;
            string batchFolder = Path.Combine(request.BasePath, workstation, mission, date, batch);
            string barCode = string.IsNullOrEmpty(request.ProductBarCode) ? "null" : request.ProductBarCode;
            string timestamp = request.CompletedAt.ToString("yyyyMMdd_HHmmss");
            string fileNameBody = $"{barCode}_{timestamp}_{request.Result}";

            // Always create directory — even for skip-screw missions with no tightening data
            try {
                Directory.CreateDirectory(batchFolder);
            } catch (Exception ex) {
                _logger.Error($"[DataExport] Failed to create directory: {batchFolder}", ex);
                throw new IOException($"无法创建导出目录: {batchFolder}", ex);
            }

            // Always write files — even with 0 rows (header-only .xlsx/.txt)
```

注意：后面 lines 46+ 的文件写入逻辑（`propertyNames`/`headers`/`rows`/`WriteExcelAsync`/`WriteTxtAsync`）完整保留，不做任何修改。0 行时 `BuildRows` 返回空 `List<List<object?>>()`，`WriteExcelAsync` 写表头后 `InsertData([])` 是 no-op，`WriteTxtAsync` 写表头后 `foreach` 空列表——都正确产出纯表头文件。

- [ ] **Step 2: 构建验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

预期：Build succeeded，0 errors

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Utils/DataExportService.cs
git commit -m "fix(export): always create directory and file, even with empty data

Skip-screw missions produce zero tightening records, causing ExportAsync
to return early before creating both the directory and the .xlsx/.txt files.
Remove the early return — always create the folder hierarchy AND the export
files with full headers. Zero data rows = header-only file."
```

---

### Task 3: OperationGuidanceApis — 站点查询双源兼容 mission_record

**Files:**
- Modify: `OperationGuidance_service/Controllers/OperationGuidanceApis.cs`

双源合并逻辑在控制器层实现——`OperationGuidanceApis` 已通过 `[Autowired]` 持有 `_operationDataService` 和 `_missionRecordService`，无需新增注入。`OperationDataService` 的两个方法保持不变。

- [ ] **Step 1: 重写 QueryMissionRecordsByWorkstationIds — 双源合并**

替换 `QueryMissionRecordsByWorkstationIds` 方法（lines 857-863）：

```csharp
// 之前 (lines 857-863)
        public QueryMissionRecordsByWorkstationIdsRsp QueryMissionRecordsByWorkstationIds(QueryMissionRecordsByWorkstationIdsReq req) {
            Dictionary<int, List<int>> result = new();
            if (req.WorkstationIds.Count > 0) {
                result = _operationDataService.GetMissionRecordIdsByWorkstationIds(req.WorkstationIds);
            }
            return new(result);
        }

// 之后
        public QueryMissionRecordsByWorkstationIdsRsp QueryMissionRecordsByWorkstationIds(QueryMissionRecordsByWorkstationIdsReq req) {
            var result = new Dictionary<int, List<int>>();
            if (req.WorkstationIds.Count > 0) {
                // 源1: operation_data — 有拧紧数据的任务（向后兼容）
                result = _operationDataService.GetMissionRecordIdsByWorkstationIds(req.WorkstationIds);

                // 源2: mission_record — 跳过螺丝点位等无拧紧数据的任务
                string sqlMr = $"select id, workstation_id from {_missionRecordService.TableName} " +
                               "where workstation_id in @ids and deleted = @deleted";
                var mrList = _missionRecordService.FindBySql(sqlMr,
                    new() { { "ids", req.WorkstationIds }, { "deleted", (int)YesOrNo.NO } });

                foreach (var mr in mrList) {
                    if (mr.workstation_id != null) {
                        if (!result.TryGetValue(mr.workstation_id.Value, out var list)) {
                            result[mr.workstation_id.Value] = new() { mr.id };
                        } else if (!list.Contains(mr.id)) {
                            list.Add(mr.id);
                        }
                    }
                }
            }
            return new(result);
        }
```

- [ ] **Step 2: 重写 QueryWorkstationInfoByMissionRecordIds — 双源合并**

替换 `QueryWorkstationInfoByMissionRecordIds` 方法（lines 684-704）：

```csharp
// 之前 (lines 684-704)
        public QueryWorkstationInfoByMissionRecordIdsRsp QueryWorkstationInfoByMissionRecordIds(QueryWorkstationInfoByMissionRecordIdsReq req) {
            // 先查询到每条任务记录对应的 workstation_id
            Dictionary<int, Dictionary<int, string>> workstationInfos = new();
            if (req.MissionRecordIds.Count > 0) {
                workstationInfos = _operationDataService.GetWorkstationInfoByMissionRecordIds(req.MissionRecordIds);

                // 根据所有 workstation_ids 查询到每个 id 对应的 name
                List<int> workstationIds = new();
                workstationInfos.Values.ToList().ForEach(dict => workstationIds.AddRange(dict.Keys));
                Dictionary<int, string> workstationInfo = _workstationService.GetWorkstationNamesByIds(workstationIds);
                foreach (var dict in workstationInfos.Values) {
                    foreach (var pair in dict) {
                        if (workstationInfo.ContainsKey(pair.Key)) {
                            dict[pair.Key] = workstationInfo[pair.Key];
                        }
                    }
                }
            }

            return new(workstationInfos);
        }

// 之后
        public QueryWorkstationInfoByMissionRecordIdsRsp QueryWorkstationInfoByMissionRecordIds(QueryWorkstationInfoByMissionRecordIdsReq req) {
            Dictionary<int, Dictionary<int, string>> workstationInfos = new();
            if (req.MissionRecordIds.Count > 0) {
                // 源1: operation_data — 有拧紧数据的记录（向后兼容）
                workstationInfos = _operationDataService.GetWorkstationInfoByMissionRecordIds(req.MissionRecordIds);

                // 源2: mission_record — 跳过螺丝点位等无拧紧数据的记录
                string sqlMr = $"select id, workstation_id from {_missionRecordService.TableName} " +
                               "where id in @ids and workstation_id is not null and deleted = @deleted";
                var mrList = _missionRecordService.FindBySql(sqlMr,
                    new() { { "ids", req.MissionRecordIds }, { "deleted", (int)YesOrNo.NO } });

                foreach (var mr in mrList) {
                    if (mr.workstation_id != null) {
                        if (!workstationInfos.TryGetValue(mr.id, out var inner)) {
                            workstationInfos[mr.id] = new() { { mr.workstation_id.Value, "" } };
                        } else if (!inner.ContainsKey(mr.workstation_id.Value)) {
                            inner[mr.workstation_id.Value] = "";
                        }
                    }
                }

                // 根据所有 workstation_ids 查询到每个 id 对应的 name（后续逻辑不变）
                List<int> workstationIds = new();
                workstationInfos.Values.ToList().ForEach(dict => workstationIds.AddRange(dict.Keys));
                Dictionary<int, string> workstationInfo = _workstationService.GetWorkstationNamesByIds(workstationIds);
                foreach (var dict in workstationInfos.Values) {
                    foreach (var pair in dict) {
                        if (workstationInfo.ContainsKey(pair.Key)) {
                            dict[pair.Key] = workstationInfo[pair.Key];
                        }
                    }
                }
            }

            return new(workstationInfos);
        }
```

- [ ] **Step 3: 构建验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

预期：Build succeeded，0 errors。控制器层已持有 `_missionRecordService` 引用，不需要额外 DI 注册。

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_service/Controllers/OperationGuidanceApis.cs
git commit -m "fix(api): merge mission_record into station-lookup results

QueryMissionRecordsByWorkstationIds and QueryWorkstationInfoByMissionRecordIds
now merge results from both operation_data and mission_record tables. This
catches skip-screw missions that have no tightening rows but do have
mission_record.workstation_id set.

- Aggregation at controller layer — no cross-service injection needed
- Two simple filtered queries per method, C# Dictionary dedup (O(n))
- No SQL NOT IN subquery — both queries use indexed workstation_id/id IN filters
- Backward compatible: operation_data source is always queried first
- OperationDataService methods unchanged"
```

- [ ] **Step 5: 确认 OperationDataService 无变动**

```bash
git diff HEAD -- OperationGuidance_service/Services/OperationDataService.cs
```

预期：无输出（该文件未改动）

---

### Task 4: 整体验证

- [ ] **Step 1: 完整构建**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

预期：Build succeeded，0 errors，0 warnings（如有 pre-existing warnings 忽略）

- [ ] **Step 2: 确认所有改动文件**

```bash
git diff --stat HEAD~3
```

预期输出三个修改文件：
```
OperationGuidance_new/Tasks/ToolTask.cs
OperationGuidance_new/Utils/DataExportService.cs
OperationGuidance_service/Controllers/OperationGuidanceApis.cs
```
