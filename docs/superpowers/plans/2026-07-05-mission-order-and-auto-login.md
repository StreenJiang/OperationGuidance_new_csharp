# 任务列表排序 & 自动登录即时生效 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 任务列表按 create_time ASC 排序；自动登录开关开启后立即保存当前用户 MD5 凭证到配置文件。

**Architecture:** 两个独立改动 — (A) 后端 `QueryProductMissionList` 两条 SQL 路径各加 `order by create_time asc`；(B) 前端设置页 `SaveSystemSettings` 开启自动登录时立即写凭证，同时顺手给 `LoginView.ClickLogin` 的凭证保存加 `IsMD5` 检查。

**Tech Stack:** C# WinForms (.NET 6), MySQL 5.7, Dapper 风格手动 SQL 查询

## Global Constraints

- MySQL 5.7 兼容 — `order by create_time asc` 语法 5.7 原生支持
- `IsMD5()` / `ToMD5String()` 来自 `OperationGuidance_service.Utils.SystemUtils`
- 凭证格式 `account,password` 与现有 `LoginView.cs:91` 保持一致
- 不可修改泛型基类 `AServiceBase.QueryListWithoutUserId()`

---

### Task 1: 任务列表 SQL 添加 ORDER BY create_time ASC

**Files:**
- Modify: `OperationGuidance_service/Controllers/OperationGuidanceApis.cs:364-377`

**Interfaces:**
- Consumes: `_productMissionService.TableName`, `_productMissionService.FindBySql()`
- Produces: `QueryProductMissionListRsp` — 返回的 `ProductMissionDTOs` 按 `create_time` 升序

- [ ] **Step 1: 修改非 Developer 路径 SQL（第370行）**

`OperationGuidance_service/Controllers/OperationGuidanceApis.cs` 第370行，在 SQL 字符串末尾添加 `order by create_time asc`：

```csharp
// 改前：
string sql = $"select * from {_productMissionService.TableName} where deleted = @deleted and macs_id = @macs_id";

// 改后：
string sql = $"select * from {_productMissionService.TableName} where deleted = @deleted and macs_id = @macs_id order by create_time asc";
```

- [ ] **Step 2: 修改 Developer 路径（第375-377行）**

将 `QueryListWithoutUserId()` 调用替换为显式 `FindBySql`，加上 ORDER BY。`ConditionWithoutUserId()` 等价于 `deleted = 0`：

```csharp
// 改前：
} else {
    missions = _productMissionService.QueryListWithoutUserId();
}

// 改后：
} else {
    string sql = $"select * from {_productMissionService.TableName} where deleted = @deleted order by create_time asc";
    missions = _productMissionService.FindBySql(sql, new() { { "@deleted", (int)YesOrNo.NO } });
}
```

- [ ] **Step 3: 构建验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

预期：Build succeeded.

- [ ] **Step 4: 提交**

```bash
git add OperationGuidance_service/Controllers/OperationGuidanceApis.cs
git commit -m "feat(mission-list): order missions by create_time ascending"
```

---

### Task 2: SaveSystemSettings 开启自动登录时立即保存凭证

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/AVariableSettingsView.cs:17`（添加 using）
- Modify: `OperationGuidance_new/Views/AbstractViews/AVariableSettingsView.cs:321-326`（修改保存逻辑）

**Interfaces:**
- Consumes: `SystemUtils.UserInfo` (from `OperationGuidance_service.Utils`), `SystemUtils.IsMD5()`, `SystemUtils.ToMD5String()`, `MainUtils.SetAutoLoginInfo()`
- Produces: 配置文件写入 `account,MD5(password)`

- [ ] **Step 1: 添加 using 语句**

`AVariableSettingsView.cs` 第17行附近，现有 `using OperationGuidance_service.Constants;`，在其后添加：

```csharp
using OperationGuidance_service.Utils;
```

- [ ] **Step 2: 修改 SaveSystemSettings 自动登录保存逻辑（第321-326行）**

```csharp
// 改前：
// Auto login
MainUtils.SetAutoLoginEnabled(_autoLoginToggle.Checked);
_autoLoginOriginal = _autoLoginToggle.Checked;
if (!_autoLoginOriginal) {
    MainUtils.SetAutoLoginInfo(MainUtils.GetDefaultAutoLoginInfo());
}

// 改后：
// Auto login
MainUtils.SetAutoLoginEnabled(_autoLoginToggle.Checked);
_autoLoginOriginal = _autoLoginToggle.Checked;
if (_autoLoginOriginal) {
    var userInfo = SystemUtils.UserInfo;
    if (userInfo != null) {
        string password = userInfo.password ?? "";
        if (!password.IsMD5()) {
            password = SystemUtils.ToMD5String(password);
        }
        MainUtils.SetAutoLoginInfo($"{userInfo.account},{password}");
    }
} else {
    MainUtils.SetAutoLoginInfo(MainUtils.GetDefaultAutoLoginInfo());
}
```

- [ ] **Step 3: 构建验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

预期：Build succeeded.

- [ ] **Step 4: 提交**

```bash
git add OperationGuidance_new/Views/AbstractViews/AVariableSettingsView.cs
git commit -m "feat(auto-login): save current user credentials immediately when toggle enabled"
```

---

### Task 3: LoginView.ClickLogin 凭证保存加 IsMD5 检查

**Files:**
- Modify: `OperationGuidance_new/Views/LoginView.cs:90-93`

**Interfaces:**
- Consumes: `SystemUtils.UserInfo`, `MainUtils.IsAutoLoginEnabled()`, `MainUtils.SetAutoLoginInfo()`
- Produces: 配置文件写入 `account,MD5(password)`

- [ ] **Step 1: 修改凭证保存逻辑（第90-93行）**

```csharp
// 改前：
// Store current account info
if (MainUtils.IsAutoLoginEnabled()) {
    String loginInfo = $"{SystemUtils.UserInfo.account},{SystemUtils.UserInfo.password}";
    MainUtils.SetAutoLoginInfo(loginInfo);
}

// 改后：
// Store current account info
if (MainUtils.IsAutoLoginEnabled()) {
    string password = SystemUtils.UserInfo.password ?? "";
    if (!password.IsMD5()) {
        password = password.ToMD5String();
    }
    MainUtils.SetAutoLoginInfo($"{SystemUtils.UserInfo.account},{password}");
}
```

> `LoginView.cs` 已有 `using OperationGuidance_service.Utils;`（第6行），无需额外添加。

- [ ] **Step 2: 构建验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

预期：Build succeeded.

- [ ] **Step 3: 提交**

```bash
git add OperationGuidance_new/Views/LoginView.cs
git commit -m "fix(auto-login): ensure password saved as MD5 in config file"
```
