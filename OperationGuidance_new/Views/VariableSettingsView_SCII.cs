using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using Newtonsoft.Json;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Configs.DTOs;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views {
    public class VariableSettingsView_SCII: AVariableSettingsView {
        private ToggleButtonGroup _enableBatchCounter;
        private bool _enableBatchCounterOriginal;

        private ToggleButtonGroup _buzzerEnabledToggle;
        private CommonButtonGroup _buzzerTestButtons;
        private bool _buzzerEnabledOriginal;

        private ToggleButtonGroup _errorPromptForWrongBarcodeToggle;
        private bool _errorPromptForWrongBarcodeOriginal;

        // Arranger open-lid config panel
        protected CustomContentPanel _arrangerConfigPanel;
        private TitlePanel _arrangerConfigTitlePanel;
        private CustomContentPanel _arrangerConfigContentPanel;
        private List<ArrangerGroupRow> _arrangerGroupRows;
        private string _arrangerGroupsOriginal;
        private List<DeviceIoDTO> _deviceIoDTOs;
        private List<DeviceIoDTO> _arrangerDTOs;

        public VariableSettingsView_SCII() {
            MissionSelfLoopingModeToggle.Hide();
            StoreLooseningDataToggle.Hide();   // redundant (base hides it) but harmless
            AutoLockToolToggle.Hide();
            EnableArmLocatingToggle.Hide();
            ArmLocatingAccuracyBox.Hide();
            ErrorPromptForArmToggle.Show();

            // 导出相关 — SCII 可见
            StoragePanel.Show();
            EnableExcelExportToggle.Show();
            StoragePathTextBox.Show();
            StorageFieldsButton.Show();
            ExportTestButton.Show();
        }

        public override void VisibleToTrue() {
            base.VisibleToTrue();
            // Set as current panel for unsaved check
            WidgetUtils.CurrentPanel = this;
        }

        protected override void InitializeMissionSettings() {
            base.InitializeMissionSettings();
            WorkPanel.WrapContents = false;

            _enableBatchCounter = new("启用批次计数") {
                Parent = WorkContentPanel,
                Ratio = 6.95,
            };

            _errorPromptForWrongBarcodeToggle = new("启用错码验证") {
                Parent = WorkContentPanel,
                Ratio = 6.95,
            };

            _buzzerEnabledToggle = new("启用蜂鸣器") {
                Parent = WorkContentPanel,
                Ratio = 6.95,
            };
            _buzzerTestButtons = new("蜂鸣器测试") {
                Parent = WorkContentPanel,
                Ratio = 6.95,
                Enabled = false,
            };
            CommonButton testOnButton = _buzzerTestButtons.GetButton(0);
            testOnButton.Label = "测试开";
            testOnButton.MouseUp += (s, e) => {
                _ = BuzzerController.TurnOnAsync();
                WidgetUtils.ShowNoticePopUp("蜂鸣器已启动");
            };
            _buzzerTestButtons.AddButton("测试关").MouseUp += (s, e) => {
                _ = BuzzerController.TurnOffAsync();
                WidgetUtils.ShowNoticePopUp("蜂鸣器已关闭");
            };
            _buzzerEnabledToggle.CheckedChanged += (s, e) => {
                _buzzerTestButtons.Enabled = _buzzerEnabledToggle.Checked;
            };

            InitializeArrangerConfigPanel();
        }

        protected override void SaveMissionSettings() {
            base.SaveMissionSettings();

            var sciiBatchConfig = ConfigUtils.LoadConfig<SciiBatchConfig>();
            sciiBatchConfig.enabled = _enableBatchCounter.Checked.ToYesOrNoInt();
            ConfigUtils.SaveConfig(sciiBatchConfig);
            _enableBatchCounterOriginal = sciiBatchConfig.enabled.ToYesOrNoBool();

            bool wrongBarcodeEnabled = _errorPromptForWrongBarcodeToggle.Checked;
            MainUtils.SetErrorPromptForWrongBarcodeEnabled(wrongBarcodeEnabled);
            _errorPromptForWrongBarcodeOriginal = wrongBarcodeEnabled;

            bool buzzerEnabled = _buzzerEnabledToggle.Checked;
            MainUtils.SetBuzzerEnabled(buzzerEnabled);
            _buzzerEnabledOriginal = buzzerEnabled;

            SaveArrangerConfig();
        }

        protected override void ResizeStoragePanel() {
            base.ResizeStoragePanel();

            int boxVMargin = BoxNBtnHeight / 2;
            int contentHeight = BoxNBtnHeight * 3 + ContentVPadding * 2 + boxVMargin * 2;
            StorageContentPanel.Size = new(Width, contentHeight);

            StoragePanel.Size = new(Width, StorageTitlePanel.Height + StorageContentPanel.Height);
        }

        protected override void ResizeMissionSettings() {
            base.ResizeMissionSettings();

            int boxWidth = (Width - ContentHPadding * 3) / 2;
            int boxVMargin = BoxNBtnHeight / 2;
            ErrorPromptForArmToggle.Margin = new(0, 0, ContentHGap / 2, 0);
            UsbScannerEnabledToggle.Margin = new(0, 0, 0, 0);

            _buzzerEnabledToggle.Size = new(boxWidth, BoxNBtnHeight);
            _buzzerEnabledToggle.Margin = new(0, boxVMargin, ContentHGap / 2, 0);
            _buzzerTestButtons.Size = new(boxWidth, BoxNBtnHeight);
            _buzzerTestButtons.Margin = new(0, boxVMargin, 0, 0);

            _enableBatchCounter.Size = new(boxWidth, BoxNBtnHeight);
            _enableBatchCounter.Margin = new(0, boxVMargin, ContentHGap / 2, 0);

            _errorPromptForWrongBarcodeToggle.Size = new(boxWidth, BoxNBtnHeight);
            _errorPromptForWrongBarcodeToggle.Margin = new(0, boxVMargin, 0, 0);

            WorkContentPanel.Size = new(Width, BoxNBtnHeight * 3 + ContentVPadding * 2 + boxVMargin * 2);
            ResizeArrangerConfigPanel(WorkContentPanel.Bottom);
        }

        protected override async void LoadSettings() {
            await Task.Run(() => {
                BeginInvoke(() => {
                    base.LoadSettings();

                    _buzzerEnabledOriginal = MainUtils.IsBuzzerEnabled();
                    _buzzerEnabledToggle.Checked = _buzzerEnabledOriginal;

                    var sciiBatchConfig = ConfigUtils.LoadConfig<SciiBatchConfig>();
                    _enableBatchCounter.Checked = sciiBatchConfig.enabled.ToYesOrNoBool();

                    _errorPromptForWrongBarcodeOriginal = MainUtils.IsErrorPromptForWrongBarcodeEnabled();
                    _errorPromptForWrongBarcodeToggle.Checked = _errorPromptForWrongBarcodeOriginal;

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
                                .Where(dto => dto.deleted == (int) YesOrNo.NO)
                                .FirstOrDefault(dto => dto.id == group.arranger_id.Value);
                            if (deviceIoDTO != null) {
                                int idx = row.ArrangerBox.IndexOf(deviceIoDTO);
                                if (idx >= 0) {
                                    row.ArrangerBox.SetCurrent(idx);
                                } else {
                                    // Device found but not arranger type (type changed)
                                    row.ArrangerBox.SetError(true);
                                    WidgetUtils.ShowWarningPopUp(
                                        $"螺丝机开盖配置「{group.name}」：螺丝机组设备（ID={group.arranger_id}）" +
                                        "不是螺丝机组类型，可能已被修改");
                                }
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
                });
            });
        }

        protected override async void ResetAllToDefault() {
            await Task.Run(() => {
                BeginInvoke(() => {
                    base.ResetAllToDefault();

                    _buzzerEnabledToggle.Checked = MainUtils.DefaultIsBuzzerEnabled();

                    var sciiBatchConfig = ConfigUtils.GetDefault<SciiBatchConfig>();
                    _enableBatchCounter.Checked = sciiBatchConfig.enabled.ToYesOrNoBool();

                    _errorPromptForWrongBarcodeToggle.Checked = MainUtils.DefaultIsErrorPromptForWrongBarcodeEnabled();

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
                    ResizeMissionSettings();
                    OuterVScrollPanel?.ResizeChildren();
                });
            });
        }

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
                .Where(dto => dto.type == DeviceType_IoBox.Arranger.Id && dto.deleted == (int) YesOrNo.NO)
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

        private string GetBoxRealText(CustomTextBox textBox) {
            string text = textBox.Box.Text?.Trim() ?? "";
            return text == textBox.DefaultText ? "" : text;
        }

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

            // Add/remove buttons (standalone, placed after ArrangerBox)
            if (isFirst) {
                SignButton addButton = new() {
                    Parent = row.Panel,
                    Icon = Properties.Resources.sign_plus,
                };
                row.ActionButton = addButton;
                addButton.Click += (s, e) => {
                    if (HasIncompleteArrangerGroup()) {
                        WidgetUtils.ShowWarningPopUp("请先完成当前开盖组的配置（名称、条码、点位和螺丝机组均不可为空），或清空不完整的组后再新增");
                        return;
                    }
                    AddArrangerGroup(false);
                };
            } else {
                SignButton minusButton = new() {
                    Parent = row.Panel,
                    Icon = Properties.Resources.sign_minus,
                };
                row.ActionButton = minusButton;
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

        protected void ResizeArrangerConfigPanel(int top) {
            _arrangerConfigPanel.Location = new(0, top);
            _arrangerConfigPanel.Size = new(Width, 0);

            _arrangerConfigTitlePanel.Size = new(Width, TitleHeight);

            int boxVMargin = BoxNBtnHeight / 2;
            int contentInnerWidth = Width - ContentHPadding * 2;
            int gap = BoxNBtnHeight / 4;
            int btnWidth = BoxNBtnHeight;
            int textBoxWidth = (int) ((contentInnerWidth - gap * 2 - btnWidth) * 0.68);
            int arrangerWidth = contentInnerWidth - textBoxWidth - btnWidth - gap * 2;

            foreach (ArrangerGroupRow row in _arrangerGroupRows) {
                row.Panel.Size = new(contentInnerWidth, BoxNBtnHeight);
                row.Panel.Margin = new(0, boxVMargin, 0, 0);
                row.TextBoxGroup.Size = new(textBoxWidth, BoxNBtnHeight);
                row.ArrangerBox.Size = new(arrangerWidth, BoxNBtnHeight);
                row.ArrangerBox.Location = new(textBoxWidth + gap, 0);
                if (row.ActionButton != null) {
                    row.ActionButton.Size = new(btnWidth, BoxNBtnHeight);
                    row.ActionButton.Location = new(textBoxWidth + gap + arrangerWidth + gap, 0);
                }
            }

            int groupCount = _arrangerGroupRows.Count;
            _arrangerConfigContentPanel.Size = new(Width,
                BoxNBtnHeight * groupCount + ContentVPadding * 2 + boxVMargin * groupCount);
            _arrangerConfigContentPanel.Padding = new(ContentHPadding, ContentVPadding, ContentHPadding, ContentVPadding);

            _arrangerConfigPanel.Size = new(Width,
                _arrangerConfigTitlePanel.Height + _arrangerConfigContentPanel.Height);

            WorkPanel.Size = new(Width, top + _arrangerConfigPanel.Height);
        }

        protected override string? CheckBeforeSave() {
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
            return base.CheckBeforeSave();
        }

        protected override bool CheckSavedFunc_detail() => base.CheckSavedFunc_detail()
            && _errorPromptForWrongBarcodeToggle.Checked == _errorPromptForWrongBarcodeOriginal
            && _buzzerEnabledToggle.Checked == _buzzerEnabledOriginal
            && _enableBatchCounter.Checked == _enableBatchCounterOriginal
            && !CheckSvedFuncSeparately(GetCurrentArrangerGroupsJson() != _arrangerGroupsOriginal, "螺丝机开盖配置");
    }
}
