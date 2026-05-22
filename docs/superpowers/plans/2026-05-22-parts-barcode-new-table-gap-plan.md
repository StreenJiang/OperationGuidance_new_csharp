# 物料码新表读写缺口修复 & 重新导入功能事务化 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复 `ActionAfterActivatingMission` 未写 `parts_bar_code` 新表导致重码校验失效；`ReimportPartsBarcode` 加事务+分页防超时和数据丢失。

**Architecture:** 两处独立改动——客户端 `AWorkplaceContentPanel` 异步写新表（不改激活流程），服务端 `OperationGuidanceApis` 复用现有事务模式包裹 DELETE/SELECT/INSERT。

**Tech Stack:** C#, WinForms, Dapper, MySQL/SQLite

---

### Task 1: ActionAfterActivatingMission 异步写入 parts_bar_code 新表

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs:1442-1451`

- [ ] **Step 1: 添加 using 引用**

在 `AWorkplaceContentPanel.cs` 第 22 行后插入：

```csharp
using OperationGuidance_service.Models.Requests;
```

- [ ] **Step 2: 在 ActionAfterActivatingMission 末尾前添加异步写入逻辑**

在 `_apis.AddOrUpdateMissionRecord(new(_missionRecord));`（line 1451）之后，`// Send barcode to PF series tools`（line 1453）之前，插入：

```csharp
            // 异步写入 parts_bar_code 新表（v2.1.x 重码校验依赖此表）
            if (_barCodeObj.PartsBarCodes.Count > 0) {
                int missionRecordId = _missionRecord.id;
                List<string> partsBarCodes = new(_barCodeObj.PartsBarCodes);
                _ = Task.Run(async () => {
                    const int maxRetries = 3;
                    for (int attempt = 0; attempt < maxRetries; attempt++) {
                        try {
                            foreach (string barCode in partsBarCodes) {
                                PartsBarCodeDTO dto = new() {
                                    mission_record_id = missionRecordId,
                                    parts_bar_code = barCode,
                                };
                                _apis.AddOrUpdatePartsBarCode(new AddOrUpdatePartsBarCodeReq(dto));
                            }
                            logger.Info($"Parts barcodes synced to new table: mission_record_id={missionRecordId}, count={partsBarCodes.Count}");
                            return;
                        } catch (Exception ex) {
                            logger.Warn($"Failed to sync parts barcodes (attempt {attempt + 1}/{maxRetries}): mission_record_id={missionRecordId}, e={ex.Message}");
                            if (attempt < maxRetries - 1) {
                                await Task.Delay((int)Math.Pow(2, attempt) * 1000);
                            }
                        }
                    }
                    logger.Error($"All {maxRetries} retries failed to sync parts barcodes to new table: mission_record_id={missionRecordId}, barcodes=[{string.Join(", ", partsBarCodes)}]");
                });
            }
```

- [ ] **Step 3: 编译验证**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeded with 0 errors.

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs
git commit -m "fix: sync parts barcodes to new table after mission activation

Async write to parts_bar_code table after AddOrUpdateMissionRecord with
3-retry backoff (1s/2s/4s). On total failure logs Error with
mission_record_id and barcode list for manual recovery via
ReimportPartsBarcode."
```

---

### Task 2: ReimportPartsBarcode 事务 + 分页

**Files:**
- Modify: `OperationGuidance_service/Controllers/OperationGuidanceApis.cs:244-303`

- [ ] **Step 1: 用事务模式包裹并添加分页**

替换 `ReimportPartsBarcode` 方法体（line 244-303）：

```csharp
        public ReimportPartsBarcodeRsp ReimportPartsBarcode(ReimportPartsBarcodeReq req) {
            ReimportPartsBarcodeRsp rsp = new();

            if (!SystemUtils.IsAdmin) {
                rsp.ErrorMessage = "权限不足";
                return rsp;
            }

            using DbConnection conn = DbConnector.GetConnection();
            DbTransaction transaction = conn.BeginTransaction();
            _partsBarCodeService.UseConnection(conn, transaction);
            _missionRecordService.UseConnection(conn, transaction);
            _sqlExecuteRecordService.UseConnection(conn, transaction);
            try {
                // 1. 删除 parts_bar_code 表数据
                string deleteSql = $"delete from parts_bar_code where deleted = {(int) YesOrNo.NO}";
                rsp.DeletedRows = _partsBarCodeService.ExecuteSql(deleteSql);

                // 2. 分页查询 mission_record 中有 parts_bar_code 的记录
                string baseSelectSql = $"select * from {_missionRecordService.TableName} where {_missionRecordService.ConditionWithoutUserId} and parts_bar_code is not null and parts_bar_code != ''";
                const int batchSize = 1000;
                int offset = 0;
                int totalInserted = 0;

                while (true) {
                    string selectSql = $"{baseSelectSql} LIMIT {batchSize} OFFSET {offset}";
                    List<MissionRecord> records = _missionRecordService.FindBySql(selectSql);
                    if (records.Count == 0) break;

                    // 3. 拆分逗号分隔的条码
                    List<PartsBarCode> entities = new();
                    foreach (MissionRecord record in records) {
                        if (string.IsNullOrEmpty(record.parts_bar_code)) continue;

                        string[] barcodes = record.parts_bar_code.Split(',');
                        foreach (string barcode in barcodes) {
                            string trimmed = barcode.Trim();
                            if (string.IsNullOrEmpty(trimmed)) continue;

                            entities.Add(new PartsBarCode {
                                mission_record_id = record.id,
                                parts_bar_code = trimmed,
                            });
                        }
                    }

                    if (entities.Count > 0) {
                        totalInserted += _partsBarCodeService.AddBatch(entities);
                    }

                    offset += batchSize;
                }
                rsp.InsertedRows = totalInserted;

                // 4. 检查 sql_execute_record 是否有 20250625_1 记录，没有则补上
                DBTypes dbType = SystemUtils.GetDBTypes();
                string fileName = dbType switch {
                    DBTypes.MYSQL => "modify_mysql_20250625_1",
                    DBTypes.SQLSERVER => "modify_sqlserver_20250625_1",
                    _ => "modify_sqlite_20250625_1",
                };

                string checkSql = $"select * from sql_execute_record where file_name = '{fileName}' and deleted = {(int) YesOrNo.NO}";
                List<SqlExecuteRecord> existingRecords = _sqlExecuteRecordService.FindBySql(checkSql);

                if (existingRecords.Count == 0) {
                    SqlExecuteRecord newRecord = new() {
                        file_name = fileName,
                    };
                    _sqlExecuteRecordService.AddEntity(newRecord);
                }

                transaction.Commit();
            } catch (Exception ex) {
                transaction.Rollback();
                rsp.ErrorMessage = ex.Message;
            } finally {
                _partsBarCodeService.ReleaseConnection();
                _missionRecordService.ReleaseConnection();
                _sqlExecuteRecordService.ReleaseConnection();
            }

            return rsp;
        }
```

- [ ] **Step 2: ReleaseConnection 已确认存在**

`AServiceBase.cs:21-23` 已暴露 `ReleaseConnection()`，无需改动。

- [ ] **Step 3: 编译验证**

Run: `dotnet build OperationGuidance_service/OperationGuidance_service.csproj`
Expected: Build succeeded with 0 errors.

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_service/Controllers/OperationGuidanceApis.cs
git commit -m "fix: add transaction and pagination to ReimportPartsBarcode

Wrap DELETE/SELECT/INSERT in a single transaction to prevent data loss
when the SELECT times out. Add batch pagination (1000 rows per batch)
to avoid MySQL timeout on large mission_record tables."
```
