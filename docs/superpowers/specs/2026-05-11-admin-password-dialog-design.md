# AdminPasswordDialog — 统一管理员密码弹窗

**日期:** 2026-05-11  
**状态:** 待实现  
**分支:** v1.5.x

## 背景

`AWorkplaceContentPanel.OpenAdminPasswordPopUpForm` 目前使用 `CustomPopUpForm` 实现管理员密码弹窗，存在两个使用场景：

- **场景 A（可取消）：** 手动控制工具、信号点测试等，用户取消后退出流程即可
- **场景 B（不可取消）：** 拧紧 NG 锁止后，必须输入正确密码才能解锁。当前通过 `CheckAdminConfirmationForLockMsg` 轮询重弹来实现，用户体验差且存在死循环风险

两套逻辑分散（场景 B 的轮询重弹 + 场景 A 的普通弹窗），且 `WaitDialog` 已提供了关闭保护的基础设施但未被利用。

## 目标

- 新建 `AdminPasswordDialog` 统一两种场景
- 利用 `WaitDialog` 的关闭保护机制解决场景 B 的轮询重弹问题
- 保持场景 A 向后兼容

## 设计

### 1. WaitDialog 微小改动

`_isClosingAllowed` 从 `private` 改为 `protected`，允许派生类在 `allowCancel` 场景下放行关闭。

### 2. 新类 `AdminPasswordDialog : WaitDialog`

**文件:** `CustomLibrary/Forms/AdminPasswordDialog.cs`

```
构造函数(title, passwordValidator, allowCancel)
  - base("管理员密码") — 复用 WaitDialog 的 TextBox
  - 设置 PasswordChar = '*'
  - 添加"确定"按钮 → OnConfirm
  - Enter 键绑定 OnConfirm
  - allowCancel=true  → 添加"取消"按钮 → IsClosingAllowed=true → Close()
  - allowCancel=false → 无取消按钮，关闭保护生效
  - 隐藏标题栏关闭按钮（两种模式都隐藏）

OnConfirm:
  正确 → IsPasswordCorrect=true → SignalComplete()
  错误 → ShowErrorPopUp("密码错误") + IsError=true

属性: IsPasswordCorrect : bool
```

`SignalComplete()` 内部设置 `IsClosingAllowed=true` 后关闭窗体。

### 3. OpenAdminPasswordPopUpForm 改造

**文件:** `Views/AbstractViews/AWorkplaceContentPanel.cs`

签名新增 `allowCancel` 参数（默认 `true`）：

```csharp
public bool OpenAdminPasswordPopUpForm(
    string title, bool needExctraActions,
    Action<bool>? actionAfterTrue = null,
    bool allowCancel = true)
```

内部：
- 用 `new AdminPasswordDialog(...)` 替代 `new CustomPopUpForm()`
- 移除本地 `Confirm()` 函数（逻辑移入 `AdminPasswordDialog`）
- 移除 `DialogResult` 操作，改用 `dialog.IsPasswordCorrect` 判断
- 验证成功后的回调逻辑保持不变

### 4. BoltNGConfirmPopUp 改造

```csharp
protected void BoltNGConfirmPopUp() =>
    OpenAdminPasswordPopUpForm("拧紧错误，工具已锁止。请输入管理员密码解锁。", false, allowCancel: false);
```

### 5. CheckAdminConfirmationForLockMsg

逻辑基本不变。由于 `allowCancel=false` 下用户无法取消，`ShowDialog` 返回时必然验证通过，轮询自然收敛，不再出现反复重弹。

## 数据流（场景 B）

```
拧紧NG → _adminConfirmed = false
  → 轮询 CheckAdminConfirmationForLockMsg
    → form 未创建 → BoltNGConfirmPopUp
      → AdminPasswordDialog(allowCancel=false)
        → ShowDialog 阻塞后台任务线程
        → 用户无法取消（无取消按钮、无 X、Alt+F4 被拦截）
        → 错误 → 弹错误提示 → 留在弹窗
        → 正确 → SignalComplete() → 关闭
      → IsPasswordCorrect=true
    → _adminConfirmed = true
  → 下一轮轮询 → 移除锁止状态 → _adminConfirmed = null
```

## 涉及文件

| 文件 | 改动类型 |
|------|----------|
| `CustomLibrary/Forms/WaitDialog.cs` | `_isClosingAllowed` private→protected |
| `CustomLibrary/Forms/AdminPasswordDialog.cs` | **新建** |
| `Views/AbstractViews/AWorkplaceContentPanel.cs` | `OpenAdminPasswordPopUpForm` 改写 |
