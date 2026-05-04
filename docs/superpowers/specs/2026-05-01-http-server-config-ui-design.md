# HTTP 服务器配置 UI 设计

**日期:** 2026-05-01
**状态:** 已批准

## 概述

在 `VariableSettingsView_SCII_XT` 中新增 "HTTP服务器配置" 面板，允许用户在界面上配置 HTTP 服务器的启用开关、监听 IP 和端口。

## 现有配置

**配置文件:** `HttpConfig.ini`
- `is_host` — 是否启用（0/1）
- `host_ip` — 监听 IP（空 = 自动取本地 IP）
- `host_port` — 监听端口（空 = 默认 5000）

**已有代码:**
- `HttpConfig.cs` — INI 读写工具类（继承 `SettingsFileUtil`）
- `ConfigName_Http.cs` — 配置键名常量
- `MainUtils.HttpConfig` — 全局访问点

## UI 设计

### 位置
在 `InitializeMissionSettings()` 中，`InitializePrinterSettingsPanel()` 之后、`InitializeMesSettingsPanel()` 之前调用 `InitializeHttpServerSettingsPanel()`。

同样在 `ResizeMissionSettings()` 中，在 `printerSettingsPanel` 之后处理 HTTP panel 的位置和尺寸。

### 字段

| 字段名 | 类型 | 说明 |
|--------|------|------|
| `_httpServerSettingsPanel` | `CustomContentPanel` | 面板容器 |
| `_httpServerSettingsTitlePanel` | `TitlePanel` | "HTTP服务器配置" 标题 |
| `_httpServerSettingsContentPanel` | `CustomContentPanel` | 内容面板 |
| `_enableHttpServer` | `ToggleButtonGroup` | 启用开关 |
| `_httpIpBox` | `CustomTextBoxGroup` | 监听 IP |
| `_httpPortBox` | `CustomTextBoxGroup` | 监听端口（正整数） |

### 布局（2 列）
- Row 1: 启用开关（IP 和 Port 空时可用）
- Row 2: IP 输入框 + 端口输入框

### 默认值行为
- `is_host` 空 → 默认不启用（运行时 fallback）
- `host_ip` 空 → 运行时自动取本地局域网 IP
- `host_port` 空 → 运行时默认 5000

UI 不预填默认值。字段为空时显示空白，依赖运行时的兜底逻辑。

## 功能

### 读取/加载
从 `MainUtils.HttpConfig` 读取三个配置项的值，填充 UI 字段。

### 保存
在 `SaveMissionSettings()` 中新增 `SaveHttpServerSettings()` 方法，将 UI 值写回 `MainUtils.HttpConfig`。

### 重置
在 `ResetAllToDefault()` 中将三个字段恢复为空（调用 `httpConfig.Write`）。

### 未保存检测
在 `CheckSavedFunc_detail()` 中新增对三个字段的检测。

### 保存前校验
在 `CheckBeforeSave()` 中新增：
- 如果启用了 HTTP 服务器，Port 必须为有效正整数。

### 联动
- `_enableHttpServer` 开关控制 `_httpIpBox` 和 `_httpPortBox` 的启用/禁用状态。

## 改动范围

1. `VariableSettingsView_SCII_XT.cs` — 新增字段声明、4 个方法、3 处调用修改
2. **无** 新文件
3. `HttpConfig.cs` / `ConfigName_Http.cs` — 无需改动（已有）