# 上盖码录入 → API 获取追溯码 → 打印

## 背景

当前 `CheckSecondBarCode` 中的 Enter 事件错误绑定了 `ProcessSecondBarCode`（分流器/第二台打印机的处理逻辑）。上盖码应走第一台 ZPL 标签打印机，且追溯码来源从本地生成改为 MES API 获取。

## 24位追溯码结构

```
00 + 制造地(2) + 年份尾号(1) + 自然日(3) + 流水号(4) + 0000 + 零件号(8)
```

位置：`0-1`="00", `2-3`=制造地, `4`=年尾号, `5-7`=日序, `8-11`=流水号, `12-15`="0000", `16-23`=零件号

## 改动范围

三个文件，四处改动。

### 1. `Workflow_SCII_XT.cs` — 新增 `GetUpperCode`

```csharp
// GET /api/product/GetUpperCode/{productCode}
public static async Task<string?> GetUpperCode(string productCode)
```

- GET 请求，使用已有 `HttpUtils.SendGet_SCII_XT<SCII_XT_Response>`
- 成功（code == OK）返回 `dataInfo?.ToString()`（24位追溯码）
- 失败/异常返回 null

### 2. `ZplQrCodePrinter.cs` — 新增方法

#### 2a. `ParseTraceCode` — 解析并校验24位追溯码

```csharp
// 返回 (serialNumber, date) 元组，校验失败抛 ArgumentException
private static (int serialNumber, DateTime date) ParseTraceCode(string traceCode)
```

**校验规则：**

| 位置 | 规则 |
|------|------|
| 全长 | = 24 |
| 0-1 | = "00" |
| 2-3 | 制造地（不校验具体值） |
| 4 | 年份尾号，0-9 |
| 5-7 | 自然日，001-366 |
| 8-11 | 流水号，0000-9999 |
| 12-15 | = "0000" |
| 16-23 | 零件号（不校验具体值） |

**日期推导：**
```
baseYear = DateTime.Now.Year / 10 * 10 + yearDigit
if baseYear > DateTime.Now.Year → baseYear -= 10
date = new DateTime(baseYear, 1, 1).AddDays(dayOfYear - 1)
```
日期不能是未来日期（超出则报错）。

#### 2b. `PrintWithTraceCode` — 组装ZPL并打印

```csharp
public bool PrintWithTraceCode(SciiXtPrinterConfig config, string traceCode)
```

内部逻辑：
1. 调 `ParseTraceCode` 解析追溯码
2. 提取 `serialNumber` → 更新 `config.sn`
3. 用推导出的 `date` 格式化日期文本（`yyyy/MM/dd`）
4. 调 `GenerateZplCommand` 的**新重载** `GenerateZplCommand(config, traceCode, date)` — 直接用 API 的24位追溯码，不调 `Generate24BitTraceCode`，日期用推导值而非 `DateTime.Now`
5. 调 `PrintViaZpl` 打印

校验失败返回 false，调用方提示错误。

#### 2c. `GenerateZplCommand` 新重载

```csharp
// 原有重载保持不动（内部生成追溯码 + DateTime.Now）
public string GenerateZplCommand(SciiXtPrinterConfig sProfile, string traceCode, int moduleSize = 5)

// 新增重载：接收外部追溯码 + 外部日期
private string GenerateZplCommand(SciiXtPrinterConfig sProfile, string traceCode, DateTime date, int moduleSize = 5)
```

新增重载与原有区别：
- 追溯码：用参数传入的（不调 `Generate24BitTraceCode`）
- 日期字段：用 `date.ToString("yyyy/MM/dd")` 代替 `DateTime.Now`

### 3. `WorkplaceMissionView_SCII_XT.cs` — 新增 `ProcessUpperCoverCode`

```csharp
public async void ProcessUpperCoverCode()
```

核心流程：
1. 取 `_barcodeDialog` 中的条码作为 `productCode`
2. 调 `Workflow_SCII_XT.GetUpperCode(productCode)` 获取24位追溯码
3. API 失败 → `ShowWarningPopUp` + return（对话框保持，可重试）
4. API 成功 → 调 `new ZplQrCodePrinter().PrintWithTraceCode(config, traceCode)`
5. 打印成功 → 关闭对话框，标记 `_lidCodePrinted = true`，缓存 `_lastPrintedConfig`
6. 打印失败 → `ShowWarningPopUp` + return

### 4. `BarCodeInputPopUpForm_SCII_XT.CheckSecondBarCode` — 改事件绑定

第135行 `workplace.ProcessSecondBarCode` → `workplace.ProcessUpperCoverCode`

**不动 `ProcessSecondBarCode` 本身** — 该方法属于分流器流程，保持不变。

## 不动的内容

- `ProcessSecondBarCode` — 分流器流程，不动
- `SendQRCodeToPrinter` / `SendToPrinter` — 不动
- `LidCodeReprintPopUpForm` — 重打弹窗不动
- `CheckSecondBarCode` 的 WaitDialog UI — 不动

## 错误处理

| 场景 | 行为 |
|------|------|
| API 网络异常 | 提示"获取追溯码失败：{ex.Message}"，对话框保持 |
| API 返回 code != OK | 提示"获取追溯码失败：{rsp.message}"，对话框保持 |
| dataInfo 为空 | 提示"追溯码数据为空"，对话框保持 |
| 追溯码格式校验失败 | 提示"追溯码格式不正确：{具体原因}"，对话框保持 |
| 打印机未配置 | 提示"打印机名称配置未设置" |
| 打印机未找到 | 提示"未找到指定配置的打印机" |
| 打印发送失败 | 提示"发送指令至打印机失败" |
