# HTTP 服务器配置 UI 实现方案
> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Goal:** 在 `VariableSettingsView_`SCII_ XT` 中新增 HTTP 服务器配置面板，支持配置 HTTP 服务器的启用、IP 和端口。
>
> **Architecture:** 在现有 SCII_XT 设置界面中新增 `InitializeHttpServerSettingsPanel()` 方法，模式完全复用现有的 printer/MES panel。HTTP 配置值通过 `MainUtils. HttpConfig` 读写 INI 文件，UI 不预填默认值（空值走运行时 fallback）。保存/重置/校验逻���与现有面板保持一致。
>
> **Tech Stack:** .NET 6.0 WinForms, `CustomContentPanel` / `TitlePanel` / `ToggleButtonGroup` / `CustomTextBoxGroup`

---

## 文件结构

**Modify:** `OperationGuidance_ new/Views/VariableSettingsView_`SCII_ XT`.cs`
- 字段声明区：7 个新字段（panel × 3、toggle、2 个 textbox、original × 1）
- `InitializeHttpServerSettingsPanel()` — 创建面板和控件
- `ResizeHttpServerSettings()` — 定位和尺寸
- `SaveHttpServerSettings()` — 保存到 INI
- 4 处调用修改：`InitializeMissionSettings`、`ResizeMissionSettings`、`LoadSettings`、`ResetAllToDefault`、`CheckBeforeSave`、`CheckSavedFunc_ detail`

---

## Chunk 1: 字段声明

**Files:** Modify: `OperationGuidance_ new/Views/VariableSettingsView_`SCII_ XT`.cs:1-70`

- [ ] **Step 1: 在 `Printer settings panel` 字段声明区之前插入 HTTP server 相关字段**

在 line 13 `}` 之前插入：

```csharp
// HTTP server settings panel
private CustomContentPanel _httpServerSettingsPanel;
private TitlePanel _httpServerSettingsTitlePanel;
private CustomContentPanel _httpServerSettingsContentPanel;

private ToggleButtonGroup _enableHttpServer;
private bool _enableHttpServerOriginal;
private CustomTextBoxGroup _httpIpBox;
private string _httpIpOriginal;
private CustomTextBoxGroup _httpPortBox;
private string _httpPortOriginal;
```

---

## Chunk 2: InitializeHttpServerSettingsPanel

**Files:** Modify: `OperationGuidance_ new/Views/VariableSettingsView_`SCII_ XT`.cs:82-86`

- [ ] **Step 2: 在 `InitializeMissionSettings()` 中添加 `InitializeHttpServerSettingsPanel()` 调用**

修改 `InitializeMissionSettings()` body：
```csharp
protected override void InitializeMissionSettings() {
    base.InitializeMissionSettings();
    InitializeHttpServerSettingsPanel();
    InitializePrinterSettingsPanel();
    InitializeMesSettingsPanel();
}
```

- [ ] **Step 3: 在 `InitializePrinterSettingsPanel()` 之前（line 137）插入完整方法**

```csharp
protected virtual void InitializeHttpServerSettingsPanel() {
    _httpServerSettingsPanel = new() {
        Parent = WorkPanel,
        FlowDirection = FlowDirection.TopDown,
    };
    _httpServerSettingsTitlePanel = new("HTTP服务器配置") {
        Parent = _httpServerSettingsPanel,
        UnderlineColor = ColorConfigs.COLOR_TITLE_UNDERLINE,
    };
    _httpServerSettingsContentPanel = new() {
        Parent = _httpServerSettingsPanel,
    };

    _enableHttpServer = new("启用HTTP服务器") {
        Parent = _httpServerSettingsContentPanel,
        Ratio = 6.95,
    };

    _httpIpBox = new("监听IP") {
        Parent = _httpServerSettingsContentPanel,
        Ratio = 6.95,
    };

    _httpPortBox = new("监听端口") {
        Parent = _httpServerSettingsContentPanel,
        Ratio = 6.95,
        PositiveIntOnly = true,
    };

    _enableHttpServer.CheckedChanged += (s, e) => {
        _httpIpBox.Enabled = _enableHttpServer.Checked;
        _httpPortBox.Enabled = _enableHttpServer.Checked;
    };
}
```

---

## Chunk 3: ResizeHttpServerSettings

**Files:** Modify: `OperationGuidance_ new/Views/VariableSettingsView_`SCII_ XT`.cs:278-324`

- [ ] **Step 4: 在 `ResizeMissionSettings()` 的 `ResizePrinterSettingsPanel` 调用之后、`ResizeMesSettings()` 调用之前插入 HTTP 面板定位和尺寸**

在 line 323 `ResizeMesSettings();` 之前插入：

```csharp
// HTTP server settings panel
_httpServerSettingsPanel.Location = new(0, _printerSettingsPanel.Location.Y + _printerSettingsPanel.Height + ContentVPadding);
_httpServerSettingsPanel.Size = new(Width, 0);
_httpServerSettingsTitlePanel.Size = new(Width, TitleHeight);

int boxWidth = (Width - ContentHPadding * 3) / 2;
int boxVMargin = BoxNBtnHeight / 2;

_enableHttpServer.Size = new(boxWidth, BoxNBtnHeight);
_enableHttpServer.Margin = new(0, boxVMargin, ContentHGap / 2, 0);
_httpIpBox.Size = new(boxWidth, BoxNBtnHeight);
_httpIpBox.Margin = new(0, boxVMargin, 0, 0);
// Row 2 - IP + Port
_httpPortBox.Size = new(boxWidth, BoxNBtnHeight);
_httpPortBox.Margin = new(0, boxVMargin, ContentHGap / 2, 0);

_httpServerSettingsContentPanel.Size = new(Width, BoxNBtnHeight * 2 + ContentVPadding * 2 + boxVMargin * 1);
_httpServerSettingsContentPanel.Padding = new(ContentHPadding, ContentVPadding, ContentHPadding, ContentVPadding);
_httpServerSettingsPanel.Size = new(Width, _httpServerSettingsTitlePanel.Height + _httpServerSettingsContentPanel.Height);
```

- [ ] **Step 5: 修改 `ResizeMissionSettings()` 末尾，将 `_printerSettingsPanel` 的位置计算加上 HTTP panel**

在 line 283 修改 `_printerSettingsPanel. Location` 的注释说明中已正确，但需要在末尾的 `ResizeMesSettings()` 调用**之前**确保 HTTP panel 位置已计算。实际不需要改 `_printerSettingsPanel. Location`，因为 HTTP panel 的 Location 已在步骤 4 正确设置。

- [ ] **Step 6: 修改 `ResizeMesSettings()` 中的 WorkPanel.Size 计算，加上 HTTP panel 高度**

在 line 368 修改 WorkPanel.Size：
```csharp
WorkPanel.Size = new(Width, WorkTitlePanel.Height + WorkContentPanel.Height + _httpServerSettingsPanel.Height + _printerSettingsPanel.Height + _mesSettingsPanel.Height + ContentVPadding * 3);
```

同时在 line 328 修改 `_mesSettingsPanel. Location`，将 `+ _printerSettingsPanel. Height` 改为 `+ _httpServerSettingsPanel. Height + _printerSettingsPanel. Height + ContentVPadding`：
```csharp
_mesSettingsPanel.Location = new(0, _httpServerSettingsPanel.Location.Y + _httpServerSettingsPanel.Height + ContentVPadding);
```

---

## Chunk 4: SaveHttpServerSettings

**Files:** Modify: `OperationGuidance_ new/Views/VariableSettingsView_`SCII_ XT`.cs:206-243`

- [ ] **Step 7: 在 `SaveMissionSettings()` 中 `SaveMesSettings()` 调用之后插入 `SaveHttpServerSettings()` 调用**

在 line 239 `SaveMesSettings();` 之后插入：
```csharp
SaveHttpServerSettings();
```

- [ ] **Step 8: 在 `SaveMesSettings()` 方法之后（line 264）插入 `SaveHttpServerSettings()` 方法**

```csharp
protected virtual void SaveHttpServerSettings() {
    var httpConfig = MainUtils.HttpConfig;
    httpConfig.Write(ConfigName_Http.IsHost, _enableHttpServer.Checked. ToYesOrNoInt().ToString());
    httpConfig.Write(ConfigName_Http.HostIp, _httpIpBox. GetTextBox(0).Box.Text);
    httpConfig.Write(ConfigName_Http.HostPort, _httpPortBox.GetTextBox(0).Box.Text);

    _enableHttpServerOriginal = _enableHttpServer.Checked;
    _httpIpOriginal = _httpIpBox.GetTextBox(0).Box.Text;
    _httpPortOriginal = _httpPortBox.GetTextBox(0).Box.Text;
}
```

---

## Chunk 5: LoadSettings

**Files:** Modify: `OperationGuidance_ new/Views/VariableSettingsView_`SCII_ XT`.cs:371-451`

- [ ] **Step 9: 在 `LoadSettings()` 的 `base.LoadSettings()` 调用之后（line 373）、`BeginInvoke` 内 MES config 加载之前插入 HTTP config 加载**

在 line 431 `# Load MES config` 注释之前插入：
```csharp
// Load HTTP server config
var httpConfig = MainUtils.HttpConfig;
_enableHttpServer.Checked = httpConfig.Read(ConfigName_Http.IsHost).ToYesOrNoBool();
_httpIpBox.GetTextBox(0).Box.Text = httpConfig.Read(ConfigName_Http.HostIp);
_httpPortBox.GetTextBox(0).Box.Text = httpConfig.Read(ConfigName_Http.HostPort);

// Initialize HTTP server original values
_enableHttpServerOriginal = _enableHttpServer.Checked;
_httpIpOriginal = _httpIpBox.GetTextBox(0).Box.Text;
_httpPortOriginal = _httpPortBox.GetTextBox(0).Box.Text;
```

同时在 line 395 附近添加 `_httpIpBox` 和 `_httpPortBox` 的 enabled 状态：
在 `_printerName.Enabled = ...` 之后添加：
```csharp
_httpIpBox.Enabled = _enableHttpServer.Checked;
_httpPortBox.Enabled = _enableHttpServer.Checked;
```

---

## Chunk 6: ResetAllToDefault

**Files:** Modify: `OperationGuidance_ new/Views/VariableSettingsView_`SCII_ XT`.cs:453-501`

- [ ] **Step 10: 在 `ResetAllToDefault()` 的 `BeginInvoke` 内、MES config 重置之前插入 HTTP config 重置**

在 line 490 `# Reset MES config to default` 注释之前插入：
```csharp
// Reset HTTP server config to default (empty = use runtime fallback)
var defaultHttpConfig = MainUtils.HttpConfig;
_enableHttpServer.Checked = defaultHttpConfig.Read(ConfigName_Http.IsHost).ToYesOrNoBool();
_httpIpBox.GetTextBox(0).Box.Text = defaultHttpConfig.Read(ConfigName_Http.HostIp);
_httpPortBox.GetTextBox(0).Box.Text = defaultHttpConfig.Read(ConfigName_Http.HostPort);
```

同样在 `_enablePrinter. Checked = ...` 之后添加 `_httpIpBox` 和 `_httpPortBox` 的 enabled 状态：
```csharp
_httpIpBox.Enabled = _enableHttpServer.Checked;
_httpPortBox.Enabled = _enableHttpServer.Checked;
```

---

## Chunk 7: CheckBeforeSave

**Files:** Modify: `OperationGuidance_ new/Views/VariableSettingsView_`SCII_ XT`.cs:503-520`

- [ ] **Step 11: 在 `CheckBeforeSave()` 中新增 HTTP 服务器端口校验**

在 return 语句 `return base.CheckBeforeSave();` 之前插入：
```csharp
if (_enableHttpServer?.Checked == true) {
    string? portText = _httpPortBox?.GetTextBox(0)?.Box?.Text;
    if (string.IsNullOrEmpty(portText) || !int.TryParse(portText, out int port) || port <= 0) {
        return "请输入有效的监听端口（大于0的整数）";
    }
}
```

---

## Chunk 8: CheckSavedFunc_detail

**Files:** Modify: `OperationGuidance_ new/Views/VariableSettingsView_`SCII_ XT`.cs:522-535`

- [ ] **Step 12: 在 `CheckSavedFunc_ detail()` 中新增 HTTP 服务器字段的未保存检测**

在 return 语句之前追加：
```csharp
|| CheckSvedFuncSeparately(_enableHttpServer.Checked != _enableHttpServerOriginal, "HTTP服务器启用")
|| CheckSvedFuncSeparately(_httpIpBox.GetTextBox(0).Box.Text != _httpIpOriginal, "监听IP")
|| CheckSvedFuncSeparately(_httpPortBox.GetTextBox(0).Box.Text != _httpPortOriginal, "监听端口")
```