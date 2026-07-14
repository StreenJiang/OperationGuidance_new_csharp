# 点位切换管理员密码确认 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在工作台点位弹窗的"切换到此点位"操作前增加管理员密码验证

**Architecture:** 在 `AWorkplaceContentPanel.AddBtnToBoltPopUpForm` 中，复用已有的 `OpenAdminPasswordPopUpForm` 方法，切换逻辑执行前插入一行密码验证调用。单方法改动，无新增依赖。

**Tech Stack:** C# WinForms, .NET 6 (net6.0-windows)

## Global Constraints

- 无配置开关，所有站点强制要求管理员密码
- 密码弹窗允许取消（`allowCancel: true`）
- 取消后不做任何操作，点位弹窗保持打开

---

### Task 1: 在切换逻辑前插入管理员密码验证

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs:1094`

**Interfaces:**
- Consumes: `OpenAdminPasswordPopUpForm(string title)` — 已有方法，同一类中 line 2285，返回 `bool`
- Produces: 无新接口

- [ ] **Step 1: 编辑 AddBtnToBoltPopUpForm，插入密码验证**

在 line 1094 `} else {` 之后、原有切换逻辑之前，插入 2 行：

```csharp
if (!OpenAdminPasswordPopUpForm("切换点位需要管理员操作密码"))
    return;
```

使用 Edit 工具，`old_string`:

```
                    } else {
                        BoltButton? currentBoltBtn;
```

`new_string`:

```
                    } else {
                        if (!OpenAdminPasswordPopUpForm("切换点位需要管理员操作密码"))
                            return;
                        BoltButton? currentBoltBtn;
```

- [ ] **Step 2: 构建验证编译通过**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

预期：Build succeeded，0 errors。

- [ ] **Step 3: Commit**

由用户手动运行 `/git-commit`。
