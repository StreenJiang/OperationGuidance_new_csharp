# 点位切换管理员密码确认

**日期:** 2026-07-14
**状态:** 已批准

## 概述

在工作台螺丝点位弹窗中，点击"切换到此点位"按钮后，先弹出管理员密码确认弹窗。密码验证通过后才执行后续切换逻辑，验证失败则不做任何操作。

## 现状

`AWorkplaceContentPanel.AddBtnToBoltPopUpForm`（line 1087-1133）中，"切换到此点位"按钮的 Click 事件直接执行切换逻辑，无管理员密码验证。

管理员密码弹窗机制已存在 — `OpenAdminPasswordPopUpForm`（line 2285）封装了完整的 `AdminPasswordDialog` → API 校验流程，返回 `true`/`false`。项目中已有类似场景（工具手动控制、NG 确认等）使用相同模式。

## 设计

### 流程

```
点击"切换到此点位"
  → 任务未激活？→ 提示错误，关闭弹窗，return
  → OpenAdminPasswordPopUpForm("切换点位需要管理员操作密码")
  → 密码错误/取消？→ return（不做任何事，点位弹窗保持打开）
  → 执行原有切换逻辑（SwitchBolt → ChangeBoltStatusToWorking → 更新缓存 → 关闭弹窗）
```

### 改动

**单文件、单方法：** `AWorkplaceContentPanel.AddBtnToBoltPopUpForm`

在 `else` 分支（任务已激活）中，切换逻辑前插入 2 行：

```csharp
if (!OpenAdminPasswordPopUpForm("切换点位需要管理员操作密码"))
    return;
```

完整改动如下（`+` 为新增行）：

```diff
 switchBtn.Click += (s, e) => {
     if (!_activated) {
         WidgetUtils.ShowErrorPopUp("任务未激活或已完成，无法切换点位！");
         _boltPopUpForm.Dispose();
     } else {
+        if (!OpenAdminPasswordPopUpForm("切换点位需要管理员操作密码"))
+            return;
+
         BoltButton? currentBoltBtn;
         int sideId = _sides[_currentSideIndex].id;
         ...
     }
 };
```

### 行为细节

- **密码弹窗标题：** "切换点位需要管理员操作密码"
- **`allowCancel`：** 使用默认值 `true`，用户可取消
- **取消后行为：** 不做任何事，点位弹窗保持打开
- **无配置开关：** 所有站点强制要求管理员密码

### 影响范围

- `AddBtnToBoltPopUpForm` 是基类 `AWorkplaceContentPanel` 的 `protected virtual` 方法
- 无子类重写该方法（SCII、GLB、YF 等均使用基类实现）
- 所有工作台视图自动继承此改动

## 不需要改动

- `BoltPopUpForm` / `BoltPopUpForm_SCII` — 弹窗本身不变
- `OpenAdminPasswordPopUpForm` — 复用已有方法
- `AdminPasswordDialog` — 无需改动
- 任何配置文件或数据库
