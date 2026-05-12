# 条码跨配方校验 & 错码管理员密码 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 产品码/物料码重码检查跨配方，错码加入管理员密码确认

**Architecture:** DTO 类型变更 int→int? → API SQL 适配 nullable → 调用点传 null 跳过配方过滤 → 6 处 ShowWarningPopUp 替换为 OpenAdminPasswordPopUpForm

**Tech Stack:** C# .NET 6, Dapper (SQL)

---

### Task 1: CheckIfBarCodeExistsInMissionRecordReq.MissionId 改为可空

**Files:**
- Modify: `OperationGuidance_service/Models/Requests/CheckIfBarCodeExistsInMissionRecordReq.cs`

- [ ] **Step 1: 将 MissionId 从 int 改为 int?**

```csharp
// 修改构造函数参数和字段类型
public class CheckIfBarCodeExistsInMissionRecordReq: ControlRequest {
    public string? ProductBarCode { get; set; }
    public string? PartsBarCode { get; set; }
    public int? MissionId { get; set; }
    public int? MissionResult { get; set; }

    public CheckIfBarCodeExistsInMissionRecordReq(int? missionId, int? missionResult = null) {
        MissionId = missionId;
        MissionResult = missionResult;
    }
}
```

- [ ] **Step 2: 验证编译**

```bash
dotnet build OperationGuidance_service/OperationGuidance_service.csproj
```

> 预期：编译成功（所有调用点传的是 `int`，可隐式转为 `int?`）

---

### Task 2: CheckPartsBarCodeReq.MissionId 改为可空

**Files:**
- Modify: `OperationGuidance_service/Models/Requests/CheckPartsBarCodeReq.cs`

- [ ] **Step 1: 将 MissionId 从 int 改为 int?**

```csharp
public class CheckPartsBarCodeReq: ControlRequest {
    public int? MissionId { get; set; }
    public string PartsBarCode { get; set; }

    public CheckPartsBarCodeReq(int? missionId, string partsBarCode) {
        MissionId = missionId;
        PartsBarCode = partsBarCode;
    }
}
```

- [ ] **Step 2: 验证编译**

```bash
dotnet build OperationGuidance_service/OperationGuidance_service.csproj
```

---

### Task 3: CheckIfBarCodeExistsInMissionRecord API 适配 nullable MissionId

**Files:**
- Modify: `OperationGuidance_service/Controllers/OperationGuidanceApis.cs:824-850`

- [ ] **Step 1: 改造 SQL 构建逻辑，MissionId 为 null 时跳过配方条件**

```csharp
public CheckIfBarCodeExistsInMissionRecordRsp CheckIfBarCodeExistsInMissionRecord(CheckIfBarCodeExistsInMissionRecordReq req) {
    string sql;
    Dictionary<string, object> parameters = new();

    if (req.MissionId != null) {
        sql = $"select 1 from {_missionRecordService.TableName} where mission_id = @mission_id";
        parameters.Add("mission_id", req.MissionId.Value);
    } else {
        sql = $"select 1 from {_missionRecordService.TableName} where 1=1";
    }

    if (req.MissionResult != null) {
        sql += " and mission_result = @mission_result";
        parameters.Add("mission_result", req.MissionResult);
    }

    if (req.ProductBarCode != null) {
        sql += " and product_bar_code = @product_bar_code";
        parameters.Add("product_bar_code", req.ProductBarCode);
    }
    if (req.PartsBarCode != null) {
        sql += " and parts_bar_code like @parts_bar_code";
        parameters.Add("parts_bar_code", $"%{req.PartsBarCode}%");
    }

    List<MissionRecord> missionRecords = _missionRecordService.FindBySql(sql, parameters);
    CheckIfBarCodeExistsInMissionRecordRsp rsp = new() {
        Yes = missionRecords.Count > 0,
    };
    if (rsp.Yes) {
        CommonUtils.ObjectConverter<MissionRecord, MissionRecordDTO>(missionRecords[0], rsp.MissionRecordDTO);
    }
    return rsp;
}
```

- [ ] **Step 2: 验证编译**

```bash
dotnet build OperationGuidance_service/OperationGuidance_service.csproj
```

---

### Task 4: CheckPartsBarCode API 适配 nullable MissionId

**Files:**
- Modify: `OperationGuidance_service/Controllers/OperationGuidanceApis.cs:890-908`

- [ ] **Step 1: MissionId 为 null 时，只要 parts_bar_code 存在即返回 true**

```csharp
public CheckPartsBarCodeRsp CheckPartsBarCode(CheckPartsBarCodeReq req) {
    string sql = $"select * from {_partsBarCodeService.TableName} where parts_bar_code = @parts_bar_code";
    Dictionary<string, object> parameters = new();
    parameters.Add("parts_bar_code", req.PartsBarCode);

    List<PartsBarCode> partsBarCodes = _partsBarCodeService.FindBySql(sql, parameters);
    if (partsBarCodes.Count > 0) {
        // 跨配方：只要有记录即视为重码
        if (req.MissionId == null) {
            return new CheckPartsBarCodeRsp(true);
        }

        string sql2 = $"select distinct(mission_id) from {_missionRecordService.TableName} where id in @id";
        Dictionary<string, object> parameters2 = new();
        parameters2.Add("id", partsBarCodes.Select(p => p.mission_record_id).Distinct().ToList());

        List<MissionRecord> missionRecords = _missionRecordService.FindBySql(sql2, parameters2);
        if (missionRecords.Count > 0) {
            return new CheckPartsBarCodeRsp(missionRecords.Select(m => m.mission_id).ToList().IndexOf(req.MissionId.Value) != -1);
        }
    }

    return new CheckPartsBarCodeRsp(false);
}
```

- [ ] **Step 2: 验证编译**

```bash
dotnet build OperationGuidance_service/OperationGuidance_service.csproj
```

---

### Task 5: 产品码重码检查改为跨配方

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/ABarCodeInputPopUpForm.cs:342`

- [ ] **Step 1: 将 mission.id 改为 null**

```csharp
// 旧：
_workplace.Apis.CheckIfBarCodeExistsInMissionRecord(new(mission.id) { ProductBarCode = barCode }).Yes

// 新：
_workplace.Apis.CheckIfBarCodeExistsInMissionRecord(new(null) { ProductBarCode = barCode }).Yes
```

- [ ] **Step 2: 验证编译**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

---

### Task 6: 物料码重码检查改为跨配方

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/ABarCodeInputPopUpForm.cs:511`

- [ ] **Step 1: 将 _mission.id 改为 null**

```csharp
// 旧：
_workplace.Apis.CheckPartsBarCode(new(_mission.id, barCode)).Yes

// 新：
_workplace.Apis.CheckPartsBarCode(new(null, barCode)).Yes
```

- [ ] **Step 2: 验证编译**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

---

### Task 7: 错码弹窗改为管理员密码确认 — ValidateProductBarCodeAsync（3 处）

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/ABarCodeInputPopUpForm.cs:258,287,337`

- [ ] **Step 1: 第 1 处 — 已选任务但条码不匹配当前任务 (line 257-259)**

```csharp
// 旧：
                        checkPassed = false;
                        WidgetUtils.ShowWarningPopUp($"当前条码【{barCode}】与选择的任务不匹配");
                        _productBarCodeBox.GetTextBox(0).IsError = true;

// 新：
                        checkPassed = false;
                        _productBarCodeBox.GetTextBox(0).IsError = true;
                        _workplace.OpenAdminPasswordPopUpForm($"当前条码【{barCode}】与选择的任务不匹配", allowCancel: false);
```

- [ ] **Step 2: 第 2 处 — 未选任务且匹配不到任何任务 (line 286-288)**

```csharp
// 旧：
                    checkPassed = false;
                    WidgetUtils.ShowWarningPopUp($"没有检索到匹配条码【{barCode}】的任务");
                    _productBarCodeBox.GetTextBox(0).IsError = true;

// 新：
                    checkPassed = false;
                    _productBarCodeBox.GetTextBox(0).IsError = true;
                    _workplace.OpenAdminPasswordPopUpForm($"没有检索到匹配条码【{barCode}】的任务", allowCancel: false);
```

- [ ] **Step 3: 第 3 处 — 前置任务未完成 (line 337-338)**

```csharp
// 旧：
                        WidgetUtils.ShowWarningPopUp("未检测到前置任务的加工完成记录，请先完成前置任务");
                        checkPassed = false;

// 新：
                        checkPassed = false;
                        _workplace.OpenAdminPasswordPopUpForm("未检测到前置任务的加工完成记录，请先完成前置任务", allowCancel: false);
```

- [ ] **Step 4: 验证编译**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

---

### Task 8: 错码弹窗改为管理员密码确认 — ValidatePartsBarCode（3 处）

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/ABarCodeInputPopUpForm.cs:433,437,456`

- [ ] **Step 1: 第 1 处 — 物料码为空 (line 433-435)**

```csharp
// 旧：
            if (string.IsNullOrEmpty(barCode)) {
                WidgetUtils.ShowWarningPopUp($"请输入或扫描条码");
                box.GetTextBox(0).IsError = true;
                return;

// 新：
            if (string.IsNullOrEmpty(barCode)) {
                box.GetTextBox(0).IsError = true;
                _workplace.OpenAdminPasswordPopUpForm($"请输入或扫描条码", allowCancel: false);
                return;
```

- [ ] **Step 2: 第 2 处 — 物料码重复录入 (line 436-440)**

```csharp
// 旧：
            } else if (_workplace.BarCodeObj.PartsBarCodes.Contains(barCode)) {
                WidgetUtils.ShowWarningPopUp($"请勿重复录入物料");
                box.GetTextBox(0).IsError = true;
                return;

// 新：
            } else if (_workplace.BarCodeObj.PartsBarCodes.Contains(barCode)) {
                box.GetTextBox(0).IsError = true;
                _workplace.OpenAdminPasswordPopUpForm($"请勿重复录入物料", allowCancel: false);
                return;
```

- [ ] **Step 3: 第 3 处 — 物料码与规则不匹配 (line 456-457)**

```csharp
// 旧：
                WidgetUtils.ShowWarningPopUp($"当前物料条码【{barCode}】与当前任务所配置的物料条码不匹配");
                box.GetTextBox(0).IsError = true;

// 新：
                box.GetTextBox(0).IsError = true;
                _workplace.OpenAdminPasswordPopUpForm($"当前物料条码【{barCode}】与当前任务所配置的物料条码不匹配", allowCancel: false);
```

- [ ] **Step 4: 验证编译**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

---

### 验证

全部 task 完成后，运行完整构建确认无编译错误：

```bash
dotnet build
```
