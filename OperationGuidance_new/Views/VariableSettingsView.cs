using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Constants;
using CustomLibrary.Panels;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Utils;

namespace OperationGuidance_new.Views {
    public class VariableSettingsView: CustomContentPanel {
        #region Fields
        private readonly IniFile _settings;
        private readonly float _contentHGapRatio = 0.025F;
        private readonly float _contentVGapRatio = 0.05F;
        private readonly float _contentHPaddingRatio = 0.15F;
        private readonly float _contentVPaddingRatio = 0.03F;
        private int _titleHeight;
        private int _contentHGap;
        private int _contentVGap;
        private int _contentHPadding;
        private int _contentVPadding;
        // Resolution options content panel
        private CustomContentPanel _resolutionPanel;
        private TitlePanel _resolutionTitlePanel;
        private CustomContentPanel _resolutionContentPanel;
        private CustomComboBoxGroup<KeyValuePair<Size, SizeRatioNRectColor>> _resolutionOptionsBox;
        private CommonButton _resolutionConfirmButton;
        // Storage path panel
        private CustomContentPanel _storagePanel;
        private TitlePanel _storageTitlePanel;
        private CustomContentPanel _storageContentPanel;
        private CustomTextBoxGroup _storageTextBox;
        private CommonButton _storageBrowseButton;
        private CommonButton _storageConfirmButton;
        #endregion

        #region Constructors
        public VariableSettingsView() {
            _settings = MainUtils.Settings;
            // Default values
            FlowDirection = FlowDirection.TopDown;

            // Initilizations
            InitializeResolutionPanel();
            InitializeStoragePanel();
        }
        #endregion

        #region Override methods
        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            Control mainParent = WidgetUtils.MainPanel;
            _titleHeight = WidgetUtils.ContentTitle();
            _contentHGap = (int) (mainParent.Height * _contentHGapRatio);
            _contentVGap = (int) (mainParent.Height * _contentVGapRatio);
            _contentHPadding = (int) (mainParent.Width * .015);
            _contentVPadding = (int) (mainParent.Height * .03);

            // Resizes
            ResizeResolutionPanel();
            ResizeStoragePanel();
        }
        public override void VisibleToTrue() {
            base.VisibleToTrue();
            // Reset current resolution
            List<KeyValuePair<Size, SizeRatioNRectColor>> items = _resolutionOptionsBox.Items;
            Control mainParent = WidgetUtils.MainPanel.Parent;
            for (int i = 0; i < items.Count; i++) {
                KeyValuePair<Size, SizeRatioNRectColor> item = items[i];
                if (item.Key == mainParent.Size) {
                    _resolutionOptionsBox.SetCurrent(i);
                }
            }
        }
        #endregion

        #region Initialization methods
        private void InitializeResolutionPanel() {
            _resolutionPanel = new() {
                Parent = this,
                FlowDirection = FlowDirection.TopDown,
            };
            _resolutionTitlePanel = new("分辨率") {
                Parent = _resolutionPanel,
                UnderlineColor = ColorConfigs.COLOR_TITLE_UNDERLINE,
            };
            _resolutionContentPanel = new() {
                Parent = _resolutionPanel,
            };
            _resolutionOptionsBox = new("分辨率") {
                Parent = _resolutionContentPanel,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                NameAlignment = HorizontalAlignment.Right,
                Ratio = 7.25,
            };
            Dictionary<Size, SizeRatioNRectColor>.Enumerator enumerator = WidgetsConfigs.Resolutions.GetEnumerator();
            Size screenSize = WidgetUtils.GetScreenResolution();
            while (enumerator.MoveNext()) {
                KeyValuePair<Size, SizeRatioNRectColor> current = enumerator.Current;
                if (current.Key.Width > screenSize.Width || current.Key.Height > screenSize.Height) {
                    continue;
                }
                string itemName = $"{current.Key.Width} x {current.Key.Height}（{current.Value.WidthRatio} x {current.Value.HeightRatio}）";
                if (current.Key.Width == screenSize.Width && current.Key.Height == screenSize.Height) {
                    itemName += "（全屏）";
                }
                _resolutionOptionsBox.AddItem(itemName, current);
            }
            _resolutionConfirmButton = new() {
                Parent = _resolutionContentPanel,
                Label = "应用",
            };
            _resolutionConfirmButton.Click += (sender, eventArgs) => {
                KeyValuePair<Size, SizeRatioNRectColor> value = _resolutionOptionsBox.Value;
                if (value.Key == new Size(0, 0)) {
                    // If user select the defualt item, then set IsError = true
                    _resolutionOptionsBox.SetError(true);
                } else {
                    // Resize main form according to chosen resolution
                    Form mainParent = (Form) WidgetUtils.MainPanel.Parent;
                    Size newSize = value.Key;
                    if (_resolutionOptionsBox.IsError) {
                        _resolutionOptionsBox.SetError(false);
                    }
                    if (newSize != mainParent.Size) {
                        if (newSize == screenSize) {
                            mainParent.WindowState = FormWindowState.Maximized;
                        } else {
                            mainParent.WindowState = FormWindowState.Normal;
                            mainParent.Size = newSize;
                            mainParent.ClientSize = newSize;
                            mainParent.Location = new((screenSize.Width - newSize.Width) / 2, (screenSize.Height - newSize.Height) / 2);
                            _settings.Write(IniFileKeys.Resolution, $"{newSize.Width}, {newSize.Height}");
                        }
                    }
                }
            };
        }
        private void InitializeStoragePanel() {
            _storagePanel = new() {
                Parent = this,
                FlowDirection = FlowDirection.TopDown,
            };
            _storageTitlePanel = new("存储参数") {
                Parent = _storagePanel,
                UnderlineColor = ColorConfigs.COLOR_TITLE_UNDERLINE,
            };
            _storageContentPanel = new() {
                Parent = _storagePanel,
            };
            _storageTextBox = new("数据存储路径") {
                Parent = _storageContentPanel,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                NameAlignment = HorizontalAlignment.Right,
                Ratio = 7.25,
            };
            string dataStoragePath = _settings.Read(IniFileKeys.DataStoragePath);
            if (string.IsNullOrEmpty(dataStoragePath)) {
                dataStoragePath = Directory.GetCurrentDirectory() + "\\";
            }
            _storageTextBox.SetValue(0, dataStoragePath);
            _storageBrowseButton = new() {
                Parent = _storageContentPanel,
                Label = "浏览",
            };
            _storageBrowseButton.Click += (sender, eventArgs) => {
                FolderBrowserDialog dialog = new() {
                    ShowNewFolderButton = true,
                    SelectedPath = dataStoragePath,
                };
                if (dialog.ShowDialog() == DialogResult.OK) {
                    _storageTextBox.SetValue(0, dialog.SelectedPath + "\\");
                }
            };
            _storageConfirmButton = new() {
                Parent = _storageContentPanel,
                Label = "保存",
            };
            _storageConfirmButton.Click += (sender, eventArgs) => {
                string newPath = _storageTextBox.GetTextBox(0).Box.Text;
                if (!Directory.Exists(newPath)) {
                    WidgetUtils.ShowErrorPopUp("当前路径不合法或不存在！");
                } else {
                    _settings.Write(IniFileKeys.DataStoragePath, _storageTextBox.GetTextBox(0).Box.Text);
                    WidgetUtils.ShowNoticePopUp("切换存储路径成功！");
                }
            };
        }
        #endregion

        #region Resize methods
        private void ResizeResolutionPanel() {
            // Resize title
            _resolutionTitlePanel.Size = new(Width, _titleHeight);
            // Resize Content
            int boxHeight = WidgetUtils.TextOrComboBoxHeight();
            int contentHeight = boxHeight + _contentVPadding * 2;
            _resolutionContentPanel.Size = new(Width, contentHeight);
            _resolutionContentPanel.Padding = new(_contentHPadding, _contentVPadding, _contentHPadding, _contentVPadding);
            // Resize box and button
            _resolutionOptionsBox.Size = new((int) (Width - _resolutionContentPanel.Padding.Size.Width - _contentHGap) / 2, boxHeight);
            _resolutionOptionsBox.Margin = new(0, 0, _contentHGap / 2, 0);
            _resolutionConfirmButton.Height = WidgetUtils.CommonButtonHeight();
            int confirmButtonLabelWidth = TextRenderer.MeasureText(_resolutionConfirmButton.Label, _resolutionConfirmButton.Font).Width;
            _resolutionConfirmButton.Width = (int) (confirmButtonLabelWidth + _resolutionConfirmButton.Height * 1.2);
            _resolutionConfirmButton.Margin = new(_contentHGap / 2, 0, 0, 0);
            // Resize outer panel
            _resolutionPanel.Size = new(Width, _resolutionTitlePanel.Height + _resolutionContentPanel.Height);
        }
        private void ResizeStoragePanel() {
            // Resize title
            _storageTitlePanel.Size = new(Width, _titleHeight);
            // Resize Content
            int boxHeight = WidgetUtils.TextOrComboBoxHeight();
            int contentHeight = boxHeight + _contentVPadding * 2;
            _storageContentPanel.Size = new(Width, contentHeight);
            _storageContentPanel.Padding = new(_contentHPadding, _contentVPadding, _contentHPadding, _contentVPadding);
            // Resize box and button
            _storageTextBox.Size = new((int) (Width - _resolutionContentPanel.Padding.Size.Width - _contentHGap) / 2, boxHeight);
            _storageTextBox.Margin = new(0, 0, _contentHGap / 2, 0);
            // Resize browse button
            _storageBrowseButton.Height = WidgetUtils.CommonButtonHeight();
            int browseButtonLabelWidth = TextRenderer.MeasureText(_storageBrowseButton.Label, _storageBrowseButton.Font).Width;
            _storageBrowseButton.Width = (int) (browseButtonLabelWidth + _storageBrowseButton.Height * 1.2);
            _storageBrowseButton.Margin = new(_contentHGap / 2, 0, 0, 0);
            // Resize confirm button
            _storageConfirmButton.Height = WidgetUtils.CommonButtonHeight();
            int confirmButtonLabelWidth = TextRenderer.MeasureText(_storageConfirmButton.Label, _storageConfirmButton.Font).Width;
            _storageConfirmButton.Width = (int) (confirmButtonLabelWidth + _storageConfirmButton.Height * 1.2);
            _storageConfirmButton.Margin = new(_contentHGap / 2, 0, 0, 0);
            // Resize outer panel
            _storagePanel.Size = new(Width, _resolutionTitlePanel.Height + _resolutionContentPanel.Height);
        }
        #endregion
    }
}
