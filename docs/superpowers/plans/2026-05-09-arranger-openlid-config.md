# 螺丝机开盖配置优化 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 SCII_XT 变量设置页的螺丝机开盖配置中，每行新增螺丝机组 Combobox 选择，并同步调整工作台螺丝机弹窗以使用配置中的 arranger_id 查找对应 IO 设备。

**Architecture:** ArrangerGroupDTO 新增 arranger_id 字段；创建 ArrangerGroupRow 容器类，用 Panel 内嵌 TextBoxGroup + ComboBoxGroup 实现同行并排；变量设置页加载时查询 DeviceIoList，过滤 type=Arranger 设备填充下拉框，保存时校验点位唯一性；ArrangerOperationPopUpForm 改为接收 ioBoxTasks 字典，按 arranger_id 逐组查找 IoBoxTask。

**Tech Stack:** C# .NET 6.0, WinForms, CustomLibrary components

---

### Task 1: ArrangerGroupDTO — 新增 arranger_id 字段

**Files:**
- Modify: `OperationGuidance_new/Configs/DTOs/ArrangerGroupDTO.cs`

- [ ] **Step 1: 添加 arranger_id 字段**

```csharp
namespace OperationGuidance_new.Configs.DTOs {
    public class ArrangerGroupDTO {
        public string name { get; set; } = "";
        public string barcode { get; set; } = "";
        public int position { get; set; } = 1;
        public int? arranger_id { get; set; }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add OperationGuidance_new/Configs/DTOs/ArrangerGroupDTO.cs
git commit -m "feat: add arranger_id field to ArrangerGroupDTO

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

### Task 2: 创建 ArrangerGroupRow 容器类

**Files:**
- Create: `OperationGuidance_new/Views/ReusableWidgets/ArrangerGroupRow.cs`

- [ ] **Step 1: 创建 ArrangerGroupRow.cs**

```csharp
using CustomLibrary.ComboBoxes;
using CustomLibrary.TextBoxes;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_new.Views.ReusableWidgets {
    /// <summary>
    /// 螺丝机开盖配置的单行容器，用 Panel 内嵌名称/条码/点位文本框组 + 螺丝机组下拉框并排显示
    /// </summary>
    public class ArrangerGroupRow {
        public Panel Panel { get; }
        public CustomTextBoxButtonGroup TextBoxGroup { get; }
        public CustomComboBoxGroup<DeviceIoDTO> ArrangerBox { get; }

        public string TextName {
            get => TextBoxGroup.TextName;
            set => TextBoxGroup.TextName = value;
        }

        public ArrangerGroupRow(string textName) {
            Panel = new() {
                Margin = new(0),
                Padding = new(0),
            };

            TextBoxGroup = new(textName) {
                Parent = Panel,
                Separator = "->",
                Ratio = null,
            };
            TextBoxGroup.AddTextBox();
            TextBoxGroup.AddTextBox();

            ArrangerBox = new("螺丝机组") {
                Parent = Panel,
                NeedDefaultLabel = true,
            };
        }

        public void Dispose() {
            ArrangerBox.Dispose();
            TextBoxGroup.Dispose();
            Panel.Dispose();
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add OperationGuidance_new/Views/ReusableWidgets/ArrangerGroupRow.cs
git commit -m "feat: add ArrangerGroupRow container widget

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

### Task 3: VariableSettingsView_SCII_XT — 替换行结构和数据加载

**Files:**
- Modify: `OperationGuidance_new/Views/VariableSettingsView_SCII_XT.cs`

- [ ] **Step 1: 替换字段声明**

将旧的 `_arrangerGroups` 和 `_arrangerGroupsOriginal` 字段声明（约 line 83-84）替换为：

```csharp
// Arranger open-lid config panel
private CustomContentPanel _arrangerConfigPanel;
private TitlePanel _arrangerConfigTitlePanel;
private CustomContentPanel _arrangerConfigContentPanel;
private List<ArrangerGroupRow> _arrangerGroupRows;
private string _arrangerGroupsOriginal;
private List<DeviceIoDTO> _deviceIoDTOs;
private List<DeviceIoDTO> _arrangerDTOs;
```

- [ ] **Step 2: 更新 using 声明**

在文件顶部添加：
```csharp
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Models.DTOs;
```

- [ ] **Step 3: 重写 InitializeArrangerConfigPanel**

将 `InitializeArrangerConfigPanel` 方法（约 line 165-189）替换为：

```csharp
protected virtual void InitializeArrangerConfigPanel() {
    _arrangerConfigPanel = new() {
        Parent = WorkPanel,
        FlowDirection = FlowDirection.TopDown,
    };
    _arrangerConfigTitlePanel = new("螺丝机开盖配置") {
        Parent = _arrangerConfigPanel,
        UnderlineColor = ColorConfigs.COLOR_TITLE_UNDERLINE,
    };
    _arrangerConfigContentPanel = new() {
        Parent = _arrangerConfigPanel,
    };

    _arrangerGroupRows = new();

    // Load device list and filter arranger-type devices
    OperationGuidanceApis apis = SystemUtils.GetApis();
    _deviceIoDTOs = apis.QueryDeviceIoList(
        new(SystemUtils.MacAddressesDTO.id)).DeviceIoDTOs
        .Where(dto => dto.macs_id == SystemUtils.MacAddressesDTO.id)
        .ToList();
    _arrangerDTOs = _deviceIoDTOs
        .Where(dto => dto.type == DeviceType_IoBox.Arranger.Id && dto.deleted == (int)YesOrNo.NO)
        .ToList();

    var config = ConfigUtils.LoadConfig<SciiXtArrangerConfig>();
    var groups = config.GroupList;
    if (groups.Count == 0) {
        AddArrangerGroup(isFirst: true);
    } else {
        for (int i = 0; i < groups.Count; i++) {
            AddArrangerGroup(isFirst: i == 0);
        }
    }
}
```

- [ ] **Step 4: 重写 AddArrangerGroup**

将 `AddArrangerGroup` 方法（约 line 211-262）替换为：

```csharp
private void AddArrangerGroup(bool isFirst) {
    int index = _arrangerGroupRows.Count;
    ArrangerGroupRow row = new($"开盖组{index + 1}");

    // Set parent (Panel goes into the content panel's FlowLayoutPanel)
    row.Panel.Parent = _arrangerConfigContentPanel;

    // Configure text boxes
    row.TextBoxGroup.SetDefaultText(0, "名称");
    row.TextBoxGroup.SetDefaultText(1, "条码");
    row.TextBoxGroup.SetDefaultText(2, "点位(1-8)");
    row.TextBoxGroup.GetTextBox(2).PositiveIntOnly = true;

    // Populate arranger combobox
    foreach (DeviceIoDTO dto in _arrangerDTOs) {
        row.ArrangerBox.AddItem(CommonUtils.CannotBeNull(dto.name), dto);
    }

    // Add/remove buttons
    if (isFirst) {
        SignButton addButton = row.TextBoxGroup.AddButton<SignButton>();
        addButton.Icon = Properties.Resources.sign_plus;
        addButton.Click += (s, e) => {
            if (HasIncompleteArrangerGroup()) {
                WidgetUtils.ShowWarningPopUp("请先完成当前开盖组的配置（名称、条码、点位和螺丝机组均不可为空），或清空不完整的组后再新增");
                return;
            }
            AddArrangerGroup(false);
        };
    } else {
        SignButton minusButton = row.TextBoxGroup.AddButton<SignButton>();
        minusButton.Icon = Properties.Resources.sign_minus;
        minusButton.Click += (s, e) => {
            _arrangerGroupRows.Remove(row);
            row.Dispose();
            for (int i = 0; i < _arrangerGroupRows.Count; i++) {
                _arrangerGroupRows[i].TextName = $"开盖组{i + 1}";
                _arrangerGroupRows[i].TextBoxGroup.ResizeChildren();
            }
            if (IsHandleCreated) {
                ResizeMissionSettings();
                OuterVScrollPanel?.ResizeChildren();
            }
        };
    }

    _arrangerGroupRows.Add(row);
    for (int i = 0; i < _arrangerGroupRows.Count; i++) {
        _arrangerGroupRows[i].TextName = $"开盖组{i + 1}";
    }

    if (IsHandleCreated) {
        ResizeMissionSettings();
        OuterVScrollPanel?.ResizeChildren();
    }
}
```

- [ ] **Step 5: 更新 HasIncompleteArrangerGroup**

将 `HasIncompleteArrangerGroup` 方法（约 line 196-209）替换为：

```csharp
private bool HasIncompleteArrangerGroup() {
    foreach (var row in _arrangerGroupRows) {
        string name = GetBoxRealText(row.TextBoxGroup.GetTextBox(0));
        string barcode = GetBoxRealText(row.TextBoxGroup.GetTextBox(1));
        string posText = GetBoxRealText(row.TextBoxGroup.GetTextBox(2));
        bool arrangerSelected = !row.ArrangerBox.IsDefaultValue();
        bool nameFilled = !string.IsNullOrEmpty(name);
        bool barcodeFilled = !string.IsNullOrEmpty(barcode);
        bool posFilled = !string.IsNullOrEmpty(posText);
        // 任一有值则四个全必填
        if ((nameFilled || barcodeFilled || posFilled || arrangerSelected)
            && !(nameFilled && barcodeFilled && posFilled && arrangerSelected))
            return true;
    }
    return false;
}
```

- [ ] **Step 6: Commit**

```bash
git add OperationGuidance_new/Views/VariableSettingsView_SCII_XT.cs
git commit -m "feat: replace arranger group rows with ArrangerGroupRow container

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

### Task 4: VariableSettingsView_SCII_XT — 加载 arranger 配置

**Files:**
- Modify: `OperationGuidance_new/Views/VariableSettingsView_SCII_XT.cs`

- [ ] **Step 1: 更新 LoadSettings 中的 arranger config 加载**

在 `LoadSettings` 方法内（约 line 755-765），将 arranger config 加载部分替换为：

```csharp
// Load arranger config
var arrangerConfig = ConfigUtils.LoadConfig<SciiXtArrangerConfig>();
var arrangerGroups = arrangerConfig.GroupList;
for (int i = 0; i < arrangerGroups.Count && i < _arrangerGroupRows.Count; i++) {
    var group = arrangerGroups[i];
    var row = _arrangerGroupRows[i];
    row.TextBoxGroup.GetTextBox(0).Box.Text = group.name;
    row.TextBoxGroup.GetTextBox(1).Box.Text = group.barcode;
    row.TextBoxGroup.GetTextBox(2).Box.Text = group.position > 0 ? group.position.ToString() : "";

    // Set arranger combobox
    if (group.arranger_id != null) {
        DeviceIoDTO? deviceIoDTO = _deviceIoDTOs
            .Where(dto => dto.deleted == (int)YesOrNo.NO)
            .FirstOrDefault(dto => dto.id == group.arranger_id.Value);
        if (deviceIoDTO != null) {
            row.ArrangerBox.SetCurrent(row.ArrangerBox.IndexOf(deviceIoDTO));
        } else {
            // Device not found (deleted or missing)
            row.ArrangerBox.SetError(true);
            WidgetUtils.ShowWarningPopUp(
                $"螺丝机开盖配置「{group.name}」：螺丝机组设备（ID={group.arranger_id}）" +
                "找不到指定设备，可能已被删除");
        }
    }
}
_arrangerGroupsOriginal = arrangerConfig.groups;
```

- [ ] **Step 2: Commit**

```bash
git add OperationGuidance_new/Views/VariableSettingsView_SCII_XT.cs
git commit -m "feat: load arranger_id from config with missing-device detection

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

### Task 5: VariableSettingsView_SCII_XT — 保存和校验

**Files:**
- Modify: `OperationGuidance_new/Views/VariableSettingsView_SCII_XT.cs`

- [ ] **Step 1: 更新 SaveArrangerConfig**

将 `SaveArrangerConfig` 方法（约 line 438-456）替换为：

```csharp
protected virtual void SaveArrangerConfig() {
    var config = ConfigUtils.LoadConfig<SciiXtArrangerConfig>();
    var groups = new List<ArrangerGroupDTO>();
    foreach (var row in _arrangerGroupRows) {
        string name = GetBoxRealText(row.TextBoxGroup.GetTextBox(0));
        string barcode = GetBoxRealText(row.TextBoxGroup.GetTextBox(1));
        string posText = GetBoxRealText(row.TextBoxGroup.GetTextBox(2));
        DeviceIoDTO? arrangerDto = row.ArrangerBox.Value;
        bool hasContent = !string.IsNullOrEmpty(name)
            || !string.IsNullOrEmpty(barcode)
            || !string.IsNullOrEmpty(posText)
            || (arrangerDto != null);
        if (!hasContent)
            continue;
        int.TryParse(posText, out int position);
        if (position < 1 || position > 8)
            position = 1;
        groups.Add(new ArrangerGroupDTO {
            name = name,
            barcode = barcode,
            position = position,
            arranger_id = arrangerDto?.id
        });
    }
    config.GroupList = groups;
    ConfigUtils.SaveConfig(config);
    _arrangerGroupsOriginal = config.groups;
}
```

- [ ] **Step 2: 更新 GetCurrentArrangerGroupsJson**

将 `GetCurrentArrangerGroupsJson` 方法（约 line 458-471）替换为：

```csharp
private string GetCurrentArrangerGroupsJson() {
    var groups = new List<ArrangerGroupDTO>();
    foreach (var row in _arrangerGroupRows) {
        string name = GetBoxRealText(row.TextBoxGroup.GetTextBox(0));
        string barcode = GetBoxRealText(row.TextBoxGroup.GetTextBox(1));
        string posText = GetBoxRealText(row.TextBoxGroup.GetTextBox(2));
        DeviceIoDTO? arrangerDto = row.ArrangerBox.Value;
        int.TryParse(posText, out int position);
        if (position < 1 || position > 8)
            position = 1;
        if (!string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(barcode)
            || position != 1 || arrangerDto != null)
            groups.Add(new ArrangerGroupDTO {
                name = name,
                barcode = barcode,
                position = position,
                arranger_id = arrangerDto?.id
            });
    }
    return JsonConvert.SerializeObject(groups);
}
```

- [ ] **Step 3: 更新 CheckBeforeSave 中的校验**

将 `CheckBeforeSave` 中 arranger config 校验部分（约 line 872-885）替换为：

```csharp
// Validate arranger config
var arrangerPositions = new Dictionary<int, List<int>>(); // arranger_id -> list of positions
foreach (var row in _arrangerGroupRows) {
    string name = GetBoxRealText(row.TextBoxGroup.GetTextBox(0));
    string barcode = GetBoxRealText(row.TextBoxGroup.GetTextBox(1));
    string posText = GetBoxRealText(row.TextBoxGroup.GetTextBox(2));
    DeviceIoDTO? arrangerDto = row.ArrangerBox.Value;
    bool hasAny = !string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(barcode)
        || !string.IsNullOrEmpty(posText) || arrangerDto != null;
    if (hasAny) {
        if (string.IsNullOrEmpty(name))
            return "螺丝机开盖配置：名称不能为空";
        if (string.IsNullOrEmpty(barcode))
            return "螺丝机开盖配置：条码不能为空";
        if (!int.TryParse(posText, out int pos) || pos < 1 || pos > 8)
            return "螺丝机开盖配置：点位必须是1-8之间的整数";
        if (arrangerDto == null)
            return "螺丝机开盖配置：螺丝机组不能为空";

        // Track position per arranger for uniqueness check
        int arrangerId = arrangerDto.id;
        if (!arrangerPositions.ContainsKey(arrangerId)) {
            arrangerPositions[arrangerId] = new List<int>();
        }
        if (arrangerPositions[arrangerId].Contains(pos)) {
            return $"螺丝机开盖配置：螺丝机组「{arrangerDto.name}」的点位 {pos} 重复，同一个螺丝机组的点位不能重复";
        }
        arrangerPositions[arrangerId].Add(pos);
    }
}
```

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_new/Views/VariableSettingsView_SCII_XT.cs
git commit -m "feat: add arranger_id save/validate logic with position uniqueness check

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

### Task 6: VariableSettingsView_SCII_XT — 更新 Resize 和 Reset 逻辑

**Files:**
- Modify: `OperationGuidance_new/Views/VariableSettingsView_SCII_XT.cs`

- [ ] **Step 1: 更新 ResizeArrangerConfigPanel**

将 `ResizeArrangerConfigPanel` 方法（约 line 629-653）替换为：

```csharp
protected virtual void ResizeArrangerConfigPanel() {
    _arrangerConfigPanel.Location = new(0, _mesSettingsPanel.Location.Y + _mesSettingsPanel.Height);
    _arrangerConfigPanel.Size = new(Width, 0);

    _arrangerConfigTitlePanel.Size = new(Width, TitleHeight);

    int boxVMargin = BoxNBtnHeight / 2;
    int contentInnerWidth = Width - ContentHPadding * 2;
    int gap = BoxNBtnHeight / 4;
    int textBoxWidth = (int)((contentInnerWidth - gap) * 0.72);
    int arrangerWidth = contentInnerWidth - textBoxWidth - gap;

    foreach (ArrangerGroupRow row in _arrangerGroupRows) {
        row.Panel.Size = new(contentInnerWidth, BoxNBtnHeight);
        row.Panel.Margin = new(0, boxVMargin, 0, 0);
        row.TextBoxGroup.Size = new(textBoxWidth, BoxNBtnHeight);
        row.ArrangerBox.Size = new(arrangerWidth, BoxNBtnHeight);
        row.ArrangerBox.Location = new(textBoxWidth + gap, 0);
    }

    int groupCount = _arrangerGroupRows.Count;
    _arrangerConfigContentPanel.Size = new(Width,
        BoxNBtnHeight * groupCount + ContentVPadding * 2 + boxVMargin * groupCount);
    _arrangerConfigContentPanel.Padding = new(ContentHPadding, ContentVPadding, ContentHPadding, ContentVPadding);

    _arrangerConfigPanel.Size = new(Width,
        _arrangerConfigTitlePanel.Height + _arrangerConfigContentPanel.Height);

    WorkPanel.Size = new(Width, WorkTitlePanel.Height + WorkContentPanel.Height
        + _printerSettingsPanel.Height + _httpServerSettingsPanel.Height
        + _mesSettingsPanel.Height + _arrangerConfigPanel.Height);
}
```

- [ ] **Step 2: 更新 ResetAllToDefault 中的 arranger 重置**

将 `ResetAllToDefault` 中 arranger config 重置部分（约 line 831-844）替换为：

```csharp
// Reset arranger config to default (empty)
while (_arrangerGroupRows.Count > 1) {
    var last = _arrangerGroupRows[_arrangerGroupRows.Count - 1];
    _arrangerGroupRows.Remove(last);
    last.Dispose();
}
if (_arrangerGroupRows.Count == 1) {
    _arrangerGroupRows[0].TextBoxGroup.GetTextBox(0).Box.Text = "";
    _arrangerGroupRows[0].TextBoxGroup.GetTextBox(1).Box.Text = "";
    _arrangerGroupRows[0].TextBoxGroup.GetTextBox(2).Box.Text = "";
    _arrangerGroupRows[0].ArrangerBox.Reset();
}
_arrangerGroupsOriginal = "[]";
```

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/VariableSettingsView_SCII_XT.cs
git commit -m "feat: update resize and reset logic for ArrangerGroupRow

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

### Task 7: ArrangerOperationPopUpForm — 按 arranger_id 查找 IoBoxTask

**Files:**
- Modify: `OperationGuidance_new/Views/SubViews/ArrangerOperationPopUpForm.cs`

- [ ] **Step 1: 更新构造函数和字段**

将全部内容替换为：

```csharp
using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Forms;
using CustomLibrary.Utils;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Configs.DTOs;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_new.Views.SubViews {
    public class ArrangerOperationPopUpForm: CustomPopUpForm {

        private AWorkplaceContentPanel _workplace;
        private string _categoryName;
        private Dictionary<string, IoBoxTask> _ioBoxTasks;
        private List<DeviceIoDTO> _deviceIoDTOs;

        private Panel _contentInnerPanel;
        private CommonButton _ioTestButton;
        private TitlePanel _openLidTitle;
        private List<CommonButtonGroup> _openLidGroups;

        private SciiXtArrangerConfig _config;
        private int _boxHeight;
        private int _boxMargin;

        public int OpenLidButtonCount => _config.GroupList.Count;

        public ArrangerOperationPopUpForm(string categoryName,
                                          AWorkplaceContentPanel workplace,
                                          Dictionary<string, IoBoxTask> ioBoxTasks,
                                          List<DeviceIoDTO> deviceIoDTOs) {
            _workplace = workplace;
            _categoryName = categoryName;
            _ioBoxTasks = ioBoxTasks;
            _deviceIoDTOs = deviceIoDTOs;

            BorderColor = ColorConfigs.COLOR_POP_UP_BORDER;
            Title = "螺丝机信号点测试 - " + categoryName;

            _config = ConfigUtils.LoadConfig<SciiXtArrangerConfig>();

            _contentInnerPanel = new() {
                Parent = ContentPanel,
            };

            // IO test button at top
            _ioTestButton = new() {
                Label = "IO点位测试",
                Parent = _contentInnerPanel,
            };
            _ioTestButton.Click += IoTestClick;

            // Subtitle
            _openLidTitle = new("螺丝机开盖") {
                Parent = _contentInnerPanel,
                UnderlineColor = ColorConfigs.COLOR_TITLE_UNDERLINE,
            };

            // Open-lid groups
            _openLidGroups = new();
            foreach (var group in _config.GroupList) {
                CommonButtonGroup btnGroup = new(group.name) {
                    Parent = _contentInnerPanel,
                };

                IoBoxTask? resolvedTask = ResolveIoBoxTask(group);
                if (resolvedTask != null) {
                    btnGroup.GetButton(0).Label = "点击开盖";
                    var capturedGroup = group;
                    var capturedTask = resolvedTask;
                    btnGroup.GetButton(0).Click += (s, e) => OpenLidScan(capturedGroup, capturedTask);
                } else {
                    btnGroup.GetButton(0).Label = "设备已删除";
                    btnGroup.GetButton(0).Enabled = false;
                    btnGroup.GetButton(0).ForeColor = Color.Red;
                }
                _openLidGroups.Add(btnGroup);
            }
        }

        private IoBoxTask? ResolveIoBoxTask(ArrangerGroupDTO group) {
            if (group.arranger_id == null)
                return null;

            DeviceIoDTO? dto = _deviceIoDTOs.FirstOrDefault(d => d.id == group.arranger_id.Value);
            if (dto == null)
                return null;

            string key = MainUtils.GetTCPClientKey(dto.ip, dto.port);
            if (!_ioBoxTasks.TryGetValue(key, out IoBoxTask? task))
                return null;

            if (task.ArrangerType == null)
                return null;

            return task;
        }

        private void OpenLidScan(ArrangerGroupDTO group, IoBoxTask ioBoxTask) {
            using ArrangerOpenLidScanPopUpForm scanForm = new(group, ioBoxTask, _workplace);
            scanForm.PretendToShowToCreateHandlesForChildren();

            int contentWidth = (int)(WidgetUtils.MainSize.Width * .65);
            Padding contentPadding = scanForm.ContentPanel.Padding;
            int boxHeight = WidgetUtils.TextOrComboBoxHeight();
            int boxMargin = boxHeight / 5;
            scanForm.BarcodeBox.Size = new(contentWidth - contentPadding.Size.Width - boxMargin * 2, boxHeight);
            scanForm.BarcodeBox.Margin = new(boxMargin);
            int contentHeight = boxHeight + boxMargin * 2 + contentPadding.Size.Height;

            scanForm.SetContentSizeAndSelfSize(new(contentWidth, contentHeight));
            scanForm.ShowDialog();
        }

        private void IoTestClick(object? sender, EventArgs e) {
            // Find any available arranger for IO test
            IoBoxTask? ioBoxTask = null;
            foreach (var task in _ioBoxTasks.Values) {
                if (task.ArrangerType != null) {
                    ioBoxTask = task;
                    break;
                }
            }
            if (ioBoxTask == null) {
                WidgetUtils.ShowConfirmPopUp("没有可用的螺丝机设备");
                return;
            }

            bool confirmed = _workplace.OpenAdminPasswordPopUpForm("IO点位测试需要管理员操作密码", false);
            if (confirmed) {
                int panelHeight = WidgetUtils.TextOrComboBoxHeight();
                int boxMargin = panelHeight / 5;
                int tableHeight = 2 * (panelHeight + boxMargin * 2) + boxMargin;

                using ArrangerIoTestPopUpForm ioTestForm = new(_categoryName, ioBoxTask);
                ioTestForm.PretendToShowToCreateHandlesForChildren();
                Size contentSize = new((int)(WidgetUtils.MainSize.Width * .5),
                    tableHeight + ioTestForm.ContentPanel.Padding.Size.Height);
                ioTestForm.SetContentSizeAndSelfSize(contentSize);
                ioTestForm.ShowDialog();
            }
        }

        public void ResizeSelf() {
            CalculateSizes();
            Invalidate();
        }

        private void CalculateSizes() {
            Padding contentPadding = ContentPanel.Padding;
            int contentWidth = ContentPanel.Width - contentPadding.Size.Width;

            _boxHeight = WidgetUtils.TextOrComboBoxHeight();
            _boxMargin = _boxHeight / 5;
            int boxWithMargin = _boxHeight + _boxMargin * 2;

            int fullWidth = contentWidth - _boxMargin * 2;
            int titleHeight = (int)(_boxHeight * 1.25);
            int titleBoxWithMargin = titleHeight + _boxMargin * 2;
            int y = _boxMargin;

            _ioTestButton.Location = new(_boxMargin, y);
            _ioTestButton.Size = new(fullWidth, _boxHeight);
            y += boxWithMargin;

            _openLidTitle.Location = new(0, y);
            _openLidTitle.Size = new(contentWidth, titleHeight);
            y += titleBoxWithMargin;

            foreach (CommonButtonGroup grp in _openLidGroups) {
                grp.Ratio = null;
                grp.ButtonFlowDirection = FlowDirection.RightToLeft;
                grp.Location = new(_boxMargin, y);
                grp.Size = new(fullWidth, _boxHeight);
                y += boxWithMargin;
            }

            int panelHeight = y + _boxMargin;

            _contentInnerPanel.Location = new(contentPadding.Left, contentPadding.Top);
            _contentInnerPanel.Size = new(contentWidth, panelHeight);
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            base.ResizeChildren(sender, eventArgs);
            CalculateSizes();
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add OperationGuidance_new/Views/SubViews/ArrangerOperationPopUpForm.cs
git commit -m "feat: refactor ArrangerOperationPopUpForm to resolve IoBoxTask per group by arranger_id

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

### Task 8: AWorkplaceContentPanel — 更新 ArrangerOperationPopUpForm 调用

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs`

- [ ] **Step 1: 更新 IOBOX_ARRANGER 分支**

将 `DeviceCategories.IOBOX_ARRANGER` 分支（约 line 701-725）替换为：

```csharp
} else if (deviceBlock.Category == DeviceCategories.IOBOX_ARRANGER) {
    if (_ioBoxTasks.Count > 0) {
        bool hasAnyArranger = _ioBoxTasks.Values.Any(task => task.ArrangerType != null);
        if (!hasAnyArranger) {
            WidgetUtils.ShowConfirmPopUp("没有配置螺丝机");
            return;
        }

        ArrangerOperationPopUpForm popUpForm = new(
            deviceBlock.CategoryName, this, _ioBoxTasks, _ioBoxes);
        deviceBlock.PopUpForm = popUpForm;
        contentSize.Width = (int)(WidgetUtils.MainSize.Width * .30);
        int boxMargin = panelHeight / 5;
        int boxWithMargin = panelHeight + boxMargin * 2;
        int titleHeight = (int)(panelHeight * 1.25);
        int titleBoxWithMargin = titleHeight + boxMargin * 2;
        int normalRows = popUpForm.OpenLidButtonCount + 1; // IO test btn + open-lid btns
        contentSize.Height = normalRows * boxWithMargin + titleBoxWithMargin + boxMargin + deviceBlock.PopUpForm.ContentPanel.Padding.Size.Height;
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs
git commit -m "feat: pass ioBoxTasks and deviceIoDTOs to ArrangerOperationPopUpForm

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

### Task 9: 编译验证

- [ ] **Step 1: 编译解决方案**

```bash
dotnet build
```

- [ ] **Step 2: 修复编译错误**

检查并修复所有编译错误。预期可能的问题：
- `ArrangerGroupRow` 命名空间引用
- `DeviceIoDTO` 的 `deleted` 属性访问
- `using` 语句
- `_ioBoxes` 在 `AWorkplaceContentPanel` 中的字段名确认（若命名为 `_ioBoxes`）

- [ ] **Step 3: Commit（如有修复）**

```bash
git add -A
git commit -m "fix: resolve compilation errors for arranger config optimization

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```
