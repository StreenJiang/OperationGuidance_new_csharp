# 任务列表排序 & 自动登录即时生效 设计文档

**日期：** 2026-07-05
**状态：** 已批准

## 概述

两个独立的小改动：
1. 任务列表按创建时间升序排列
2. 自动登录开关开启后立即保存当前用户凭证，无需重新登录

---

## 需求1：任务列表按创建时间排序

### 当前行为

`QueryProductMissionList` API 的 SQL 查询没有 `ORDER BY`，任务列表顺序依赖数据库默认行为（不可控）。

### 目标行为

所有任务列表按 `create_time` 升序排列（先创建的排在前面）。

### 改动

**文件：** `OperationGuidance_service/Controllers/OperationGuidanceApis.cs`
**方法：** `QueryProductMissionList`（第364行）

在两处 SQL 查询添加排序：

1. **非 Developer 角色路径**（第371行）：在 SQL 末尾添加 `order by create_time asc`
   - 改前：`select * from {table} where deleted = @deleted and macs_id = @macs_id`
   - 改后：`... order by create_time asc`

2. **Developer 路径**（第374行）：将 `QueryListWithoutUserId()` 改为显式 `FindBySql`，与路径1保持一致
   - 改前：`missions = _productMissionService.QueryListWithoutUserId();`
   - 改后：使用 `FindBySql($"select * from {table} where {condition} order by create_time asc")`
   - 原因：`QueryListWithoutUserId()` 是泛型基类方法，被多个 Service 共用，不应为其单独修改

### 影响范围

- 选择任务页面（操作工视角）
- 任务管理页面（编辑视角）
- WHYC + SCII 两个站点均生效
- 客户端无需改动

---

## 需求2：自动登录开关即时生效

### 当前行为

1. 用户在设置中开启"自动登录"并保存 → 只写入开关状态，不保存凭证
2. 下次启动 → 自动登录读不到凭证 → 回退到手动登录
3. 用户重新登录 → `LoginView.ClickLogin()` 检测到开关开启 → 才保存凭证

**用户体验问题：** 开启开关后必须重新登录一次才能生效。

### 目标行为

开启"自动登录"开关并保存时，立即读取当前登录用户信息写入配置文件，无需重新登录。

### 密码存储策略

`SystemUtils.UserInfo.password` 来自数据库，正常情况已是 MD5。但为防御性编程，保存前显式确保 MD5 格式：用 `SystemUtils.IsMD5()` 判断 → 已是 MD5 直接存，不是则 `ToMD5String()` 转换。**不能盲 MD5**，否则双重 MD5 会导致自动登录验证失败。

### 改动

**改动1：** `OperationGuidance_new/Views/AbstractViews/AVariableSettingsView.cs` — `SaveSystemSettings()`（第321-326行附近）

```csharp
// 改前：
MainUtils.SetAutoLoginEnabled(_autoLoginToggle.Checked);
_autoLoginOriginal = _autoLoginToggle.Checked;
if (!_autoLoginOriginal) {
    MainUtils.SetAutoLoginInfo(MainUtils.GetDefaultAutoLoginInfo());
}

// 改后：
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

> `ToMD5String()` 是 `SystemUtils` 的静态方法，非 string 扩展方法。

**改动2（顺手优化）：** `OperationGuidance_new/Views/LoginView.cs:91` — `ClickLogin()` 内凭证保存，同样加 `IsMD5` 检查保证一致性

```csharp
// 改前：
if (MainUtils.IsAutoLoginEnabled()) {
    String loginInfo = $"{SystemUtils.UserInfo.account},{SystemUtils.UserInfo.password}";
    MainUtils.SetAutoLoginInfo(loginInfo);
}

// 改后：
if (MainUtils.IsAutoLoginEnabled()) {
    string password = SystemUtils.UserInfo.password ?? "";
    if (!password.IsMD5()) {
        password = SystemUtils.ToMD5String(password);
    }
    MainUtils.SetAutoLoginInfo($"{SystemUtils.UserInfo.account},{password}");
}
```

### 影响范围

- 设置页保存 + 登录页凭证保存，两处统一
- `IsMD5()` 和 `ToMD5String()` 来自 `OperationGuidance_service.Utils.SystemUtils`
- `SystemUtils.UserInfo` 登录后始终可用（设置页需要登录才能进入），保留 null 检查做防御
