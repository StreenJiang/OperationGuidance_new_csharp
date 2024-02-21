using System.Reflection;
using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Constants;
using CustomLibrary.DataGridViewRelateds;
using CustomLibrary.Panels;
using CustomLibrary.ComboBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.Attributes;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Utils;
using OperationGuidance_new.ViewObjects;
using CustomLibrary.TextBoxes;

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
        private CustomComboBoxButtonGroup<KeyValuePair<Size, SizeRatioNRectColor>> _resolutionOptionsBox;
        // Storage panel
        private CustomContentPanel _storagePanel;
        private TitlePanel _storageTitlePanel;
        private CustomContentPanel _storageContentPanel;
        private CustomTextBoxGroup _storageFileNameTextBox;
        private CustomTextBoxButtonGroup _storagePathTextBox;
        private CommonButtonGroup _storageFieldsButton;
        private DataGridView _fieldsGridView;
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
                Ratio = 8.5,
            };
            Dictionary<Size, SizeRatioNRectColor>.Enumerator enumerator = WidgetsConfigs.Resolutions.GetEnumerator();
            Size screenSize = WidgetUtils.GetScreenResolution();
            bool hasFullScreenResolution = false;
            while (enumerator.MoveNext()) {
                KeyValuePair<Size, SizeRatioNRectColor> current = enumerator.Current;
                if (current.Key.Width > screenSize.Width || current.Key.Height > screenSize.Height) {
                    continue;
                }
                string itemName = $"{current.Key.Width} x {current.Key.Height}";
                if (current.Key.Width == screenSize.Width && current.Key.Height == screenSize.Height) {
                    itemName += "（全屏）";
                    hasFullScreenResolution = true;
                } else {
                    itemName += $"（{current.Value.WidthRatio} x {current.Value.HeightRatio}）";
                }
                _resolutionOptionsBox.AddItem(itemName, current);
            }
            if (!hasFullScreenResolution) {
                _resolutionOptionsBox.AddItem($"{screenSize.Width} x {screenSize.Height}（全屏）", new KeyValuePair<Size, SizeRatioNRectColor>(screenSize, new()));
            }
            _resolutionOptionsBox.AddButton("应用").Click += (sender, eventArgs) => {
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
                        }
                        _settings.Write(IniFileKeys.Resolution, $"{newSize.Width}, {newSize.Height}");
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
            _storageFileNameTextBox = new("数据文件名称") {
                Parent = _storageContentPanel,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 8.5,
            };
            _storagePathTextBox = new("数据存储路径") {
                Parent = _storageContentPanel,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 8.5,
            };
            string dataStoragePath = _settings.Read(IniFileKeys.DataStoragePath);
            if (string.IsNullOrEmpty(dataStoragePath)) {
                dataStoragePath = Directory.GetCurrentDirectory() + "\\";
            }
            _storagePathTextBox.SetValue(0, dataStoragePath);
            _storagePathTextBox.AddButton("浏览").Click += (sender, eventArgs) => {
                FolderBrowserDialog dialog = new() {
                    ShowNewFolderButton = true,
                    SelectedPath = dataStoragePath,
                };
                if (dialog.ShowDialog() == DialogResult.OK) {
                    _storagePathTextBox.SetValue(0, dialog.SelectedPath + "\\");
                }
            };
            _storagePathTextBox.AddButton("保存").Click += (sender, eventArgs) => {
                string newPath = _storagePathTextBox.GetTextBox(0).Box.Text;
                if (!Directory.Exists(newPath)) {
                    WidgetUtils.ShowErrorPopUp("当前路径不合法或不存在！");
                } else {
                    _settings.Write(IniFileKeys.DataStoragePath, _storagePathTextBox.GetTextBox(0).Box.Text);
                    WidgetUtils.ShowNoticePopUp("切换存储路径成功！");
                }
            };
            _storageFieldsButton = new("数据存储字段") {
                Parent = _storageContentPanel,
                Ratio = 8.5,
            };
            CommonButton storageFieldsButton = _storageFieldsButton.GetButton(0);
            storageFieldsButton.Label = "配置字段";
            _fieldsGridView = new() {
                Parent = _storageContentPanel,
                Margin = new(0),
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToAddRows = false,
                AllowUserToResizeColumns = true,
                AutoGenerateColumns = false,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
            };
            _fieldsGridView.ColumnHeadersDefaultCellStyle.BackColor = WidgetUtils.LightColor(ColorTranslator.FromHtml("#E86C10"), .15);
            _fieldsGridView.ColumnHeadersDefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#FEFEFE");
            _fieldsGridView.ColumnHeadersDefaultCellStyle.SelectionBackColor = _fieldsGridView.ColumnHeadersDefaultCellStyle.BackColor;
            DataGridViewColumn[] columnRange = {};
            Type type = typeof(OperationDataVO);
            List<PropertyInfo> props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
            foreach (PropertyInfo property in props) {
                IEnumerable<Attribute> enumerable = property.GetCustomAttributes();
                foreach (Attribute attribute in enumerable) {
                    if (attribute is GridColumnAttribute gridColumn) {
                        string columnName;
                        if (gridColumn.ColumnName != null && gridColumn.ColumnName != string.Empty) {
                            columnName = gridColumn.ColumnName;
                        } else {
                            columnName = property.Name;
                        }
                        if (gridColumn.CellType != null && gridColumn.CellType == typeof(ToggleButton)) {
                            DataGridViewToggleButtonColumn column = new() {
                                DataPropertyName = property.Name,
                                HeaderText = columnName,
                                ReadOnly = true,
                            };
                            columnRange = columnRange.Append(column).ToArray();
                        } else if (gridColumn.CellType != null && gridColumn.CellType == typeof(Image)) {
                            DataGridViewImageColumn column = new() {
                                DataPropertyName = property.Name,
                                HeaderText = columnName,
                                ReadOnly = true,
                            };
                            columnRange = columnRange.Append(column).ToArray();
                        } else {
                            DataGridViewTextBoxColumn column = new() {
                                DataPropertyName = property.Name,
                                HeaderText = columnName,
                                ReadOnly = true,
                            };
                            columnRange = columnRange.Append(column).ToArray();
                        }
                    }
                }
            }
            _fieldsGridView.Columns.AddRange(columnRange);
            foreach (DataGridViewColumn column in _fieldsGridView.Columns) {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }
        #endregion

        #region Resize methods
        private void ResizeResolutionPanel() {
            // Resize title
            _resolutionTitlePanel.Size = new(Width, _titleHeight);
            int boxHeight = WidgetUtils.TextOrComboBoxHeight();
            int contentHeight = boxHeight + _contentVPadding * 2;
            // Resize Content
            _resolutionContentPanel.Size = new(Width, contentHeight);
            _resolutionContentPanel.Padding = new(_contentHPadding, _contentVPadding, _contentHPadding, _contentVPadding);
            // Resize box and button
            _resolutionOptionsBox.Size = new(Width - _resolutionContentPanel.Padding.Size.Width, boxHeight);
            _resolutionOptionsBox.Margin = new(0, 0, _contentHGap / 2, 0);
            // Resize outer panel
            _resolutionPanel.Size = new(Width, _resolutionTitlePanel.Height + _resolutionContentPanel.Height);
        }
        private void ResizeStoragePanel() {
            // Resize title
            _storageTitlePanel.Size = new(Width, _titleHeight);
            int boxHeight = WidgetUtils.TextOrComboBoxHeight();
            int boxWidth = (Width - _storageContentPanel.Padding.Size.Width);
            int buttonHeight = WidgetUtils.CommonButtonHeight();
            int gridHeaderHeight = WidgetUtils.GridViewHeaderHeight();
            int boxVMargin = boxHeight / 2;
            int contentHeight = boxHeight * 3 + _contentVPadding * 2 + boxVMargin * 3 + gridHeaderHeight;
            // Resize grid view
            _fieldsGridView.Width = boxWidth;
            HScrollBar hScrollBar = _fieldsGridView.Controls.OfType<HScrollBar>().First();
            if (hScrollBar.Visible) {
                gridHeaderHeight += hScrollBar.Height;
                contentHeight += hScrollBar.Height;
            }
            _fieldsGridView.Height = gridHeaderHeight;
            _fieldsGridView.Margin = new(0, boxVMargin, 0, 0);
            int newHeaderHeight = WidgetUtils.GridViewHeaderHeight();
            if (newHeaderHeight >= 4) {
                _fieldsGridView.ColumnHeadersHeight = newHeaderHeight;
                _fieldsGridView.ColumnHeadersDefaultCellStyle.Font = new(WidgetsConfigs.SystemFontFamily, newHeaderHeight * .45F, FontStyle.Regular, GraphicsUnit.Pixel);
            }
            // Resize Content
            _storageContentPanel.Size = new(Width, contentHeight);
            _storageContentPanel.Padding = new(_contentHPadding, _contentVPadding, _contentHPadding, _contentVPadding);
            // Resize box
            _storageFileNameTextBox.Size = new(boxWidth, boxHeight);
            _storageFileNameTextBox.Margin = new(0, 0, _contentHGap / 2, 0);
            _storagePathTextBox.Size = new(boxWidth, boxHeight);
            _storagePathTextBox.Margin = new(0, boxVMargin, _contentHGap / 2, 0);
            _storageFieldsButton.Size = new(boxWidth, buttonHeight);
            _storageFieldsButton.Margin = new(0, boxVMargin, _contentHGap / 2, 0);
            // Resize outer panel
            _storagePanel.Size = new(Width, _storageTitlePanel.Height + _storageContentPanel.Height);
        }
        #endregion
    }
}
