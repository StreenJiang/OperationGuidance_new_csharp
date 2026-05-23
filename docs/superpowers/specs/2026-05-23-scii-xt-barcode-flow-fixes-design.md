# SCII XT: 物料码流程修复 + 后台管理优化 + 登录绕过修复

## 背景

客户反馈 SCII XT 版本三个问题：
1. 物料码配置超过2个时，第三个会被跳过直接激活任务
2. 后台管理系统"物料重新导入"应对 SCII XT 隐藏
3. sys 用户登录可绕过 MES 接口调用

---

## 问题1：物料码第3个被跳过 —— 根因及修复

### 根因

**任务配置了物料码规则但未配置产品码（追溯码）规则。**

流程分析：
1. 任务有3条物料码规则，0条产品码规则
2. 条码弹窗打开，产品码输入框获得焦点
3. 用户扫第1个条码(material-1) → 进入产品码框 → `CheckBarCodeMatchedMission` 因无产品规则直接返回 true → **material-1 被当作产品码"吃掉"**
4. 用户只剩 material-2、material-3 两个条码，但要满足3条物料规则
5. 录入2个物料码后触发 `CheckCanActivateMission`，规则数 vs 已录入数出现差异

加上产品码规则后：产品码有专门的条码去匹配，不会侵占物料码的位置，问题消失。

### 修复内容

#### 修复1：SCII XT `CheckCanActivateMission` 增加产品码规则校验
`BarCodeInputPopUpForm_SCII_XT.cs:40-53` — 在 `base.CheckCanActivateMission()` 通过后，检查：若任务有物料码规则但无产品码规则，弹警告并阻止激活。

#### 修复A：`PartsBarCodeExtraCheck` 恢复基类调用
`BarCodeInputPopUpForm_SCII_XT.cs:158` — 从 `=> true` 改为调用 `base.PartsBarCodeExtraCheck(ruleId)`，恢复螺栓绑定规则的排除校验。

#### 修复B：`ValidateProductBarCodeAsync` 增加 `!_activated` 守卫
`ABarCodeInputPopUpForm.cs:379` — `CheckCanActivateMission()` 前增加 `!_workplace.Activated` 条件，防止重复激活。

### 涉及文件
- `OperationGuidance_new/Views/ReusableWidgets/BarCodeInputPopUpForm_SCII_XT.cs`
- `OperationGuidance_new/Views/AbstractViews/ABarCodeInputPopUpForm.cs`

---

## 问题2：物料重新导入隐藏（SCII XT）

### 方案
`AdminManagementView` 构造函数中加 `MainUtils.GetVersion() != AppVersion.SCII_XT` 判断，SCII XT 版本不构建重新导入卡片。`LayoutCards` 中增加 `_reimportCard != null` 空检查。

### 涉及文件
- `OperationGuidance_new/Views/AdminManagementView.cs`

---

## 问题3：sys 用户绕过 MES 登录

### 方案
`LoginView_SCII_XT.CheckLoginByApi` 中，`account == "admin"` 改为 `account == "admin" || account == "sys"`，参照 admin 处理方式。

### 涉及文件
- `OperationGuidance_new/Views/LoginView_SCII_XT.cs`

---

## 问题4：ZPL标签流水号补齐 + 打印机测试SN输入限制

### 流水号不补零
`ZplQrCodePrinter.GenerateZplCommand` 第53行，第四行文本直接使用 `sProfile.sn`：
```csharp
// 修复前：zpl.AppendLine($"{sProfile.text_4}{sProfile.sn}^FS");
// 修复后：
zpl.AppendLine($"{sProfile.text_4}{sProfile.sn.ToString().PadLeft(4, '0')}^FS");
```
注：`Generate24BitTraceCode` 中的流水号已有 `PadLeft(4, '0')`，二维码内溯源码正确。此修复仅针对标签第四行显示文本。

### SN输入框限制
`PrinterTestPopUpForm` 构造函数中，Printer1 模式下 SN 输入框增加 `MaxLength = 4`（已有 `PositiveIntOnly = true`）。

### 涉及文件
- `OperationGuidance_new/Utils/IIPSC/ZplQrCodePrinter.cs`
- `OperationGuidance_new/Views/ReusableWidgets/PrinterTestPopUpForm.cs`
