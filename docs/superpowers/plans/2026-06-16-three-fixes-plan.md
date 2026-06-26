# Three Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix SQLite log noise, ToolTask PF4000 handshake cleanup, and mission list rename refresh.

**Architecture:** Three independent fixes in three separate files groups. No cross-dependencies.

**Tech Stack:** C#, WinForms, System.Data.SQLite, MySql.Data, System.Data.SqlClient

---

### Task 1: SQLite — inline table existence check to eliminate log noise

**Files:**
- Modify: `OperationGuidance_service/Database/SQLiteConnector.cs:30`
- Modify: `OperationGuidance_service/Database/MySqlConnector.cs:42`
- Modify: `OperationGuidance_service/Database/SqlServerConnector.cs:33`
- Modify: `OperationGuidance_service/Controllers/OperationGuidanceApis.cs:1489`
- Modify: `OperationGuidance_service/Utils/ConnectionUtils.cs:20-61`

- [ ] **Step 1: Replace CheckTableExists call in SQLiteConnector**

In `OperationGuidance_service/Database/SQLiteConnector.cs`, replace line 30:

`conn.CreateCommand()` returns `DbCommand` (base class), not `SQLiteCommand`. Use `new SQLiteCommand()` + set `Connection` property to avoid cast:

```csharp
// BEFORE
                bool tableExists = ConnectionUtils.CheckTableExists(conn, new UserAccountInfo().TableName());

// AFTER
                bool tableExists;
                string tableName = new UserAccountInfo().TableName();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name=@name";
                    cmd.Parameters.AddWithValue("@name", tableName);
                    object? result = cmd.ExecuteScalar();
                    tableExists = result != null && Convert.ToInt32(result) > 0;
                }
```

- [ ] **Step 2: Replace CheckTableExists call in MySqlConnector**

In `OperationGuidance_service/Database/MySqlConnector.cs`, replace line 42:

Same pattern as SQLite — `new MySqlCommand()` avoids `CreateCommand()` return-type issues:

```csharp
// BEFORE
                if (!ConnectionUtils.CheckTableExists(conn, new UserAccountInfo().TableName())) {

// AFTER
                bool tableExists;
                string tableName = new UserAccountInfo().TableName();
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "SELECT COUNT(1) FROM information_schema.tables WHERE table_schema=@db AND table_name=@name";
                    cmd.Parameters.AddWithValue("@db", Database);
                    cmd.Parameters.AddWithValue("@name", tableName);
                    object? result = cmd.ExecuteScalar();
                    tableExists = result != null && Convert.ToInt32(result) > 0;
                }
                if (!tableExists) {
```

- [ ] **Step 3: Replace CheckTableExists call in SqlServerConnector**

In `OperationGuidance_service/Database/SqlServerConnector.cs`, replace line 33:

Same pattern — `new SqlCommand()` avoids `CreateCommand()` return-type issues:

```csharp
// BEFORE
                if (!ConnectionUtils.CheckTableExists(conn, new UserAccountInfo().TableName())) {

// AFTER
                bool tableExists;
                string tableName = new UserAccountInfo().TableName();
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "SELECT COUNT(1) FROM information_schema.tables WHERE table_schema=@db AND table_name=@name";
                    cmd.Parameters.AddWithValue("@db", Database);
                    cmd.Parameters.AddWithValue("@name", tableName);
                    object? result = cmd.ExecuteScalar();
                    tableExists = result != null && Convert.ToInt32(result) > 0;
                }
                if (!tableExists) {
```

- [ ] **Step 4: Replace CheckTableExists call in OperationGuidanceApis**

In `OperationGuidance_service/Controllers/OperationGuidanceApis.cs`, replace line 1489:

```csharp
// BEFORE
if (!ConnectionUtils.CheckTableExists(conn, database, tableName)) {

// AFTER
bool tableExists;
using (DbCommand cmd = conn.CreateCommand())
{
    cmd.CommandText = "SELECT COUNT(1) FROM information_schema.tables WHERE table_schema=@db AND table_name=@name";
    DbParameter dbParam = cmd.CreateParameter();
    dbParam.ParameterName = "@db";
    dbParam.Value = database;
    cmd.Parameters.Add(dbParam);
    DbParameter nameParam = cmd.CreateParameter();
    nameParam.ParameterName = "@name";
    nameParam.Value = tableName;
    cmd.Parameters.Add(nameParam);
    object? result = cmd.ExecuteScalar();
    tableExists = result != null && Convert.ToInt32(result) > 0;
}
if (!tableExists) {
```

Note: `conn` here is `DbConnection` (abstract), so we use `DbCommand`/`DbParameter` instead of concrete types.

- [ ] **Step 5: Delete dead code from ConnectionUtils and ConnectionContants**

In `OperationGuidance_service/Utils/ConnectionUtils.cs`, delete:

- Line 2: `using OperationGuidance_service.Constants;` (was only needed for deleted `CheckConnection` → `ConnectionStatus`)
- Lines 16-17: `CheckConnection` method (always returns `CONNECTED`, zero callers)
- Lines 20-61: both `CheckTableExists` overloads

In `OperationGuidance_service/Constants/ConnectionContants.cs`, delete:

- Lines 2-3: empty `ConnectionContants` class (zero references)
- Lines 5-8: `ConnectionStatus` enum (only used by deleted `CheckConnection`)

Do NOT delete `HttpResponseCode` enum (lines 10-13) — it has 17 callers across the codebase.

- [ ] **Step 6: Build and verify**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeded, no compilation errors.

- [ ] **Step 7: Commit**

```bash
git add OperationGuidance_service/Database/SQLiteConnector.cs \
        OperationGuidance_service/Database/MySqlConnector.cs \
        OperationGuidance_service/Database/SqlServerConnector.cs \
        OperationGuidance_service/Controllers/OperationGuidanceApis.cs \
        OperationGuidance_service/Utils/ConnectionUtils.cs \
        OperationGuidance_service/Constants/ConnectionContants.cs
git commit -m "refactor(db): inline CheckTableExists into each connector, remove dead ConnectionUtils code"
```

---

### Task 2: ToolTask — fix socket cleanup and enhance handshake logging

**Files:**
- Modify: `OperationGuidance_new/Tasks/ToolTask.cs:290-384`

- [ ] **Step 1: Add socket cleanup in inner catch block**

In `ConnectToServer()`, line 363-365, change:

```csharp
// BEFORE
                    } catch (Exception e) {
                        logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] Socket connection error", e);
                    }

// AFTER
                    } catch (Exception e) {
                        logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] Socket connection error", e);
                        socketClient?.Close();
                        socketClient = null;
                        connectSuccess = false;
                    }
```

- [ ] **Step 2: Enhance connect handshake log when MID mismatches**

Line 322-323, change:

```csharp
// BEFORE
                                    sendConnectMsgSuceess = mid1 == "0002" || mid1 == "0005";
                                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Connect response: {mid1}");

// AFTER
                                    sendConnectMsgSuceess = mid1 == "0002" || mid1 == "0005";
                                    if (sendConnectMsgSuceess) {
                                        logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Connect handshake OK, MID={mid1}");
                                    } else {
                                        logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Connect handshake failed: expected MID 0002/0005, got {mid1}");
                                    }
```

- [ ] **Step 3: Enhance log when no connect response received**

Line 325, change:

```csharp
// BEFORE
                                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] No connect response");

// AFTER
                                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Connect handshake failed: no response from device");
```

- [ ] **Step 4: Enhance data enable handshake log when MID mismatches**

Lines 334-339, change:

```csharp
// BEFORE
                                        string mid2 = toolPF.GetMid(result2);
                                        dataEnableMsgSuccess = mid2 == "0002" || mid2 == "0005";
                                        logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Data enable response: {mid2}");

// AFTER
                                        string mid2 = toolPF.GetMid(result2);
                                        dataEnableMsgSuccess = mid2 == "0002" || mid2 == "0005";
                                        if (dataEnableMsgSuccess) {
                                            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Data enable handshake OK, MID={mid2}");
                                        } else {
                                            logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Data enable handshake failed: expected MID 0002/0005, got {mid2}");
                                        }
```

- [ ] **Step 5: Enhance log when no data enable response received**

Line 338, change:

```csharp
// BEFORE
                                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] No data enable response");

// AFTER
                                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Data enable handshake failed: no response from device");
```

- [ ] **Step 6: Build and verify**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeded.

- [ ] **Step 7: Commit**

```bash
git add OperationGuidance_new/Tasks/ToolTask.cs
git commit -m "fix(tooltask): close socket on handshake exception, add detailed failure logging for PF4000"
```

---

### Task 3: Mission list — update display name on Entity change

**Files:**
- Modify: `OperationGuidance_new/Views/ReusableWidgets/MissionListPanel.cs:98`

**Context:** `ProductMissionBlock<T>` is declared `ProductMissionBlock<T>: CustomContentPanelBase` with no generic constraint on `T`. The `Entity` setter cannot access `value.name` directly because the compiler doesn't know `T` has a `name` property. The class already has a `MissionName` property setter that updates both `_missionName` and `_innerButton.Label` (line 42-48). The fix goes in `RefreshMissionBlocks` where we have the concrete `ProductMissionDTO` type and can access `.name`.

**Repaint concern:** `MissionName` setter calls `_innerButton.Label = value` → `ResizeTextLabel()` → sets `Font`. But `Font` setter checks `Equals()` — if the new font is logically identical (same family/size/style), WinForms skips `Invalidate()`. So we must explicitly call `_innerButton.Invalidate()` after setting `MissionName` to guarantee the text repaints.

- [ ] **Step 1: Add MissionName update + Invalidate in sameIds path**

In `OperationGuidance_new/Views/ReusableWidgets/MissionListPanel.cs`, line 98, change:

```csharp
// BEFORE
                for (int i = 0; i < blocks.Count && i < missionDTOs.Count; i++) {
                    blocks[i].Entity = missionDTOs[i];
                }

// AFTER
                for (int i = 0; i < blocks.Count && i < missionDTOs.Count; i++) {
                    blocks[i].Entity = missionDTOs[i];
                    blocks[i].MissionName = missionDTOs[i].name;
                    blocks[i].InnerButton.Invalidate();
                }
```

- [ ] **Step 2: Build and verify**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/ReusableWidgets/MissionListPanel.cs
git commit -m "fix(ui): update mission block display name when Entity changes in sameIds path"
```

---

## Self-Review

**Spec coverage:**
- Fix 1 (SQLite log noise): Tasks cover all 5 files, each connector gets its own native query, ConnectionUtils methods deleted, OperationGuidanceApis also converted. ✅
- Fix 2 (ToolTask handshake): Socket cleanup in catch block, enhanced MID mismatch logging for connect + data enable, enhanced null-response logging. ✅
- Fix 3 (Mission list rename): MissionName updated in sameIds path using existing setter (which updates `_innerButton.Label`). No position change (sameIds path, no ResizeChildren call). ✅

**Placeholder scan:** No TBD/TODO. All code shown. ✅

**Type consistency:** `MissionName` setter exists at `ProductMissionBlock.cs:42-48`, already sets `_innerButton.Label`. `missionDTOs[i].name` is `string` per ProductMissionDTO line 6. ✅
