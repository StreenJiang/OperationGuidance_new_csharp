using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Constants;
using CustomLibrary.Panels;
using CustomLibrary.ComboBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Utils;
using CustomLibrary.TextBoxes;
using CustomLibrary.Forms;
using OperationGuidance_new.Views.ReusableWidgets;
using Newtonsoft.Json;
using CustomLibrary.Panels.BaseClasses;
using CustomLibrary.Events;
using OperationGuidance_service.Constants;

namespace OperationGuidance_new.Views {
    public class VariableSettingsView: CustomContentPanel {
        #region Fields
        private readonly IniFile _settings;
        private readonly float _contentHGapRatio = 0.025F;
        private readonly float _contentVGapRatio = 0.05F;
        private readonly float _contentHPaddingRatio = 0.15F;
        private readonly float _contentVPaddingRatio = 0.03F;
        private int _titleHeight;
        private int _boxNBtnHeight;
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
        private ToggleButtonGroup _storeLooseningDataToggle;
        // 任务配置
        private CustomContentPanel _missionPanel;
        private TitlePanel _missionTitlePanel;
        private CustomContentPanel _missionContentPanel;
        private ToggleButtonGroup _enableArmLocatingToggle;
        private CustomTextBoxButtonGroup _armLocatingAccuracyBox;
        #endregion

        private List<OperationDataField> Fields { get; set; } = new();
        private List<int> SortConfig { get; set; }

        #region Constructors
        public VariableSettingsView() {
            _settings = MainUtils.Settings;
            // Default values
            FlowDirection = FlowDirection.TopDown;

            // Initilizations
            InitializeResolutionPanel();
            InitializeStoragePanel();
            InitializeMissionSettings();
        }
        #endregion

        #region Override methods
        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            Control mainForm = WidgetUtils.MainForm;
            _titleHeight = WidgetUtils.ContentTitleHeight();
            _boxNBtnHeight = WidgetUtils.TextOrComboBoxHeight();
            _contentHGap = (int) (mainForm.Height * _contentHGapRatio);
            _contentVGap = (int) (mainForm.Height * _contentVGapRatio);
            _contentHPadding = (int) (mainForm.Width * .015);
            _contentVPadding = (int) (mainForm.Height * .03);

            // Resizes
            ResizeResolutionPanel();
            ResizeStoragePanel();
            ResizeMissionSettings();
       }
        public override void VisibleToTrue() {
            base.VisibleToTrue();
            // Reset current resolution
            List<KeyValuePair<Size, SizeRatioNRectColor>> items = _resolutionOptionsBox.Items;
            Control mainParent = WidgetUtils.MainForm;
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
                    Form mainParent = (Form) WidgetUtils.MainForm;
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
                    ResizeChildren();
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
            SortConfig = MainUtils.GetSortConfig();
            Fields = MainUtils.GetOperationDataFields(SortConfig);
            _storageTitlePanel.AddRightButton<TitlePanel.RightButton>("保存").Click += (sender, eventArgs) => {
                string newPath = _storagePathTextBox.GetTextBox(0).Box.Text;
                if (!Directory.Exists(newPath)) {
                    WidgetUtils.ShowErrorPopUp("当前存储路径不合法或不存在！");
                    return;
                }
                string nameFormat = _storageFileNameTextBox.GetTextBox(0).Box.Text;
                string fieldsSortConfig = JsonConvert.SerializeObject(SortConfig);
                // Save
                _settings.Write(IniFileKeys.DataStorageNameFormat, nameFormat);
                _settings.Write(IniFileKeys.DataStoragePath, newPath);
                _settings.Write(IniFileKeys.DataStorageFieldsSort, fieldsSortConfig);
                _settings.Write(IniFileKeys.DataStorageFieldsSortCurr, null);
                if (_storeLooseningDataToggle.Checked) {
                    _settings.Write(IniFileKeys.DataStorageStoreLooseningData, (int) YesOrNo.YES + "");
                } else {
                    _settings.Write(IniFileKeys.DataStorageStoreLooseningData, (int) YesOrNo.NO + "");
                }
                WidgetUtils.ShowNoticePopUp("存储参数配置信息保存成功！");
            };
            _storageContentPanel = new() {
                Parent = _storagePanel,
            };
            _storageFileNameTextBox = new("数据文件名称") {
                Parent = _storageContentPanel,
                Ratio = 8.5,
            };
            _storageFileNameTextBox.SetValue(0, MainUtils.GetStorageFileName());
            _storagePathTextBox = new("数据存储路径") {
                Parent = _storageContentPanel,
                Ratio = 8.5,
            };
            string dataStoragePath = MainUtils.GetStoragePath();
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
            _storageFieldsButton = new("数据存储字段") {
                Parent = _storageContentPanel,
                Ratio = 8.5,
            };
            CommonButton storageFieldsButton = _storageFieldsButton.GetButton(0);
            storageFieldsButton.Label = "配置字段";
            storageFieldsButton.MouseUp += (sender, eventArgs) => {
                PopUpFieldsConfigurationForm(SortConfig, Fields);
            };
            _storageFieldsButton.AddButton("字段预览").MouseUp += (sender, eventArgs) => {
                PopUpFieldsPreviewForm(Fields);
            };
            _storeLooseningDataToggle = new("记录反松数据") {
                Parent = _storageContentPanel,
                Ratio = 8.5,
                Checked = MainUtils.GetStoreLooseningData(),
            };
        }
        private void PopUpFieldsConfigurationForm(List<int> sortConfig, List<OperationDataField> fields) {
            FieldsConfiguration configPanel = new(fields);
            CustomPopUpForm form = new() {
                Title = "配置数据字段",
            };
            CommonButton confirmButton = form.AddButton("确定");
            confirmButton.Click += (s, e) => {
                (SortConfig, Fields) = configPanel.UpdateSortAndFields();
                form.Dispose();
            };
            CommonButton closeButton = form.AddButton("关闭");
            closeButton.Click += (s, e) => {
                form.Dispose();
            };
            form.PretendToShowToCreateHandlesForChildren();
            CustomVScrollingContentPanel scrollPanel = new(ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER, configPanel) {
                Parent = form.ContentPanel,
            };
            // Calculate size
            Padding contentPadding = form.ContentPanel.Padding;
            int buttonHeight = WidgetUtils.CommonButtonHeight();
            int buttonMargin = buttonHeight / 5;
            Size contentSize = new((int) (WidgetUtils.MainSize.Width * .4), (int) (WidgetUtils.MainSize.Height * .6));
            configPanel.ToggleButtonHeight = buttonHeight;
            configPanel.ButtonMargin = buttonMargin;
            scrollPanel.Size = new(contentSize.Width - contentPadding.Size.Width, contentSize.Height - contentPadding.Size.Height);
            form.SetContentSizeAndSelfSize(contentSize);
            form.Show();
        }
        private class FieldsConfiguration: CustomContentPanel {
            private readonly List<OperationDataField> _fields;
            private int _toggleButtonHeight;
            private int _buttonMargin;

            public int ToggleButtonHeight { get => _toggleButtonHeight; set => _toggleButtonHeight = value; }
            public int ButtonMargin { get => _buttonMargin; set => _buttonMargin = value; }

            public FieldsConfiguration(List<OperationDataField> fields) {
                _fields = fields;
                int serial = 1;
                foreach (OperationDataField field in _fields) {
                    MovableButton btn = new(serial++, field) {
                        Parent = this,
                        Checked = field.Visible,
                        Ratio = 5,
                    };
                    btn.CheckedChanged += (sender, eventArgs) => {
                        int currentIndex = Controls.IndexOf(btn);
                        if (!btn.Field.Visible && btn.Checked) {
                            btn.Field.Visible = true;
                            int movementCount = VisibleToTrueMovementCount(currentIndex - 1);
                            btn.SerialNum -= movementCount;
                            Controls.SetChildIndex(btn, currentIndex - movementCount);
                        } else if (btn.Field.Visible && !btn.Checked) {
                            btn.Field.Visible = false;
                            int movementCount = VisibleToFalseMovementCount(currentIndex + 1);
                            btn.SerialNum += movementCount;
                            Controls.SetChildIndex(btn, currentIndex + movementCount);
                        }
                    };        
                    btn.PressUp += () => {
                        int currentIndex = Controls.IndexOf(btn);
                        if (!btn.Checked) {
                            btn.Checked = true;
                        } else if (currentIndex > 0) {
                            MovableButton previousBtn = (MovableButton) Controls[currentIndex - 1];
                            previousBtn.SerialNum += 1;
                            btn.SerialNum -= 1;
                            Controls.SetChildIndex(btn, currentIndex - 1);
                        }
                    };        
                    btn.PressDown += () => {
                        int currentIndex = Controls.IndexOf(btn);
                        if (btn.Checked && currentIndex < Controls.Count - 1) {
                            MovableButton nextBtn = (MovableButton) Controls[currentIndex + 1];
                            if (nextBtn.Checked) {
                                nextBtn.SerialNum -= 1;
                                btn.SerialNum += 1;
                                Controls.SetChildIndex(btn, currentIndex + 1);
                            }
                        }
                    };        
                }
                int VisibleToTrueMovementCount(int previousIndex) {
                    int count = 0;
                    if (previousIndex >= 0) {
                        MovableButton previousBtn = (MovableButton) Controls[previousIndex];
                        if (!previousBtn.Checked) {
                            count++;
                            previousBtn.SerialNum += 1;
                            return count + VisibleToTrueMovementCount(previousIndex - 1);
                        }
                    }
                    return count;
                }
                int VisibleToFalseMovementCount(int nextIndex) {
                    int count = 0;
                    if (nextIndex < Controls.Count) {
                        MovableButton nextBtn = (MovableButton) Controls[nextIndex];
                        if (nextBtn.Checked) {
                            count++;
                            nextBtn.SerialNum -= 1;
                            return count + VisibleToFalseMovementCount(nextIndex + 1);
                        }
                    }
                    return count;
                }
            }

            public Tuple<List<int>, List<OperationDataField>> UpdateSortAndFields() {
                List<int> sortConfig = new();
                List<OperationDataField> fieldsConfig = new();
                List<MovableButton> btns = new();
                foreach (Control ctrl in Controls) {
                    if (ctrl is MovableButton btn) {
                        btns.Add(btn);
                        fieldsConfig.Add(btn.Field);
                    }
                }
                // fieldsConfig = fieldsConfig.OrderBy(f => {
                //     int indexTemp = f.Id;
                //     if (indexTemp == -1) {
                //         indexTemp = _fields.Count;
                //     }
                //     return indexTemp;
                // }).ToList();
                fieldsConfig.ForEach(f => {
                    if (f.Visible) {
                        sortConfig.Add(f.Id);
                    }
                });
                return new(sortConfig, fieldsConfig);
            }

            protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
                base.ResizeChildren(sender, eventArgs);
                int marginTop = _buttonMargin / 2;
                int marginBottom = _buttonMargin - marginTop;
                foreach (Control ctrl in Controls) {
                    ctrl.Size = new(Width - _buttonMargin * 2, _toggleButtonHeight);
                    ctrl.Margin = new(_buttonMargin, marginTop, 0, marginBottom);
                }
            }

            public override bool CheckNeedsScrollBar(int parentNewHeight) {
                NewHeight = (_toggleButtonHeight + _buttonMargin) * Controls.Count;
                return NewHeight > parentNewHeight;
            }

            private class MovableButton : ToggleButtonGroup {
                private int _serialNum;
                private OperationDataField _field;
                private Image _upImage = Properties.Resources.direction_up;
                private Image _downImage = Properties.Resources.direction_down;
                private Rectangle? _upImageRect;
                private Rectangle? _downImageRect;
                private Image? _upImageShowing;
                private Image? _downImageShowing;
                private bool _upIsDown = false;
                private bool _downIsDown = false;
                private bool _focusOnUp = false;
                private bool _focusOnDown = false;
                private Action? _onUp;
                private Action? _onDown;
                private bool _isPressing = false;

                public new bool Checked {
                    get => base.Checked;
                    set {
                        base.Checked = value;
                        if (value) {
                            ForeColor = ColorConfigs.COLOR_FIELD_TOGGLE_FOREGROUND;
                        } else {
                            ForeColor = ColorConfigs.COLOR_FIELD_TOGGLE_FOREGROUND_INVISIBLE;
                        }
                    }
                }
                public int SerialNum { 
                    get => _serialNum; 
                    set {
                        _serialNum = value; 
                        TextName = $"{_serialNum}. {_field.FieldName}";
                    }
                }
                public OperationDataField Field { get => _field; set => _field = value; }

                public event Action PressUp { add => _onUp += value; remove => _onUp -= value; }
                public event Action PressDown { add => _onDown += value; remove => _onDown -= value; }

                public MovableButton(int serialNum, OperationDataField field): base($"{serialNum}. {field.FieldName}") {
                    _serialNum = serialNum;
                    _field = field;
                    ToggleButton.CheckedChanged += (sender, eventArgs) => {
                        if (Checked) {
                            ForeColor = ColorConfigs.COLOR_FIELD_TOGGLE_FOREGROUND;
                        } else {
                            ForeColor = ColorConfigs.COLOR_FIELD_TOGGLE_FOREGROUND_INVISIBLE;
                        }
                    };
                }

                protected override void OnMouseEnter(EventArgs e) {
                    base.OnMouseEnter(e);
                    int btnSide = (int) (Height * .75);
                    int margin = btnSide / 3;
                    Size imageSize = new(btnSide, btnSide);
                    Point imageDownLocation = new(Width - btnSide, (Height - btnSide) / 2);
                    Point imageUpLocation = new(Width - btnSide * 2 - margin, (Height - btnSide) / 2);

                    _upImageRect = new(imageUpLocation, imageSize);
                    _downImageRect = new(imageDownLocation, imageSize);

                    _upImageShowing = WidgetUtils.ResizeImageWithoutLosingQuality(_upImage, imageSize);
                    _downImageShowing = WidgetUtils.ResizeImageWithoutLosingQuality(_downImage, imageSize);
                }
                private void ClickUpAnimation(bool goDown) {
                    if (_upImageRect != null) {
                        if (goDown) {
                            if (!_upIsDown) {
                                _upImageRect = new(new(_upImageRect.Value.X + 1, _upImageRect.Value.Y + 1), _upImageRect.Value.Size);
                                _upIsDown = true;
                            }
                        } else {
                            if (_upIsDown && !_isPressing) {
                                _upImageRect = new(new(_upImageRect.Value.X - 1, _upImageRect.Value.Y - 1), _upImageRect.Value.Size);
                                _upIsDown = false;
                            }
                        }
                    }
                }
                private void ClickDownAnimation(bool goDown) {
                    if (_downImageRect != null) {
                        if (goDown) {
                            if (!_downIsDown) {
                                _downImageRect = new(new(_downImageRect.Value.X + 1, _downImageRect.Value.Y + 1), _downImageRect.Value.Size);
                                _downIsDown = true;
                            }
                        } else {
                            if (_downIsDown && !_isPressing) {
                                _downImageRect = new(new(_downImageRect.Value.X - 1, _downImageRect.Value.Y - 1), _downImageRect.Value.Size);
                                _downIsDown = false;
                            }
                        }
                    }
                }
                protected override void OnMouseLeave(EventArgs e) {
                    base.OnMouseLeave(e);
                    _upImageRect = null;
                    _downImageRect = null;
                    _upImageShowing = null;
                    _downImageShowing = null;
                    Invalidate();
                }
                protected override void OnMouseMove(MouseEventArgs mevent) {
                    base.OnMouseMove(mevent);
                    if (_upImageRect != null) {
                        if (EventFuncs.MouseInArea(new(PointToScreen(_upImageRect.Value.Location), _upImageRect.Value.Size))) {
                            _focusOnUp = true;
                        } else {
                            ClickUpAnimation(false);
                            _focusOnUp = false;
                        }
                        Invalidate();
                    }
                    if (_downImageRect != null) {
                        if (EventFuncs.MouseInArea(new(PointToScreen(_downImageRect.Value.Location), _downImageRect.Value.Size))) {
                            _focusOnDown = true;
                        } else {
                            ClickDownAnimation(false);
                            _focusOnDown = false;
                        }
                        Invalidate();
                    }
                }
                protected override void OnMouseDown(MouseEventArgs mevent) {
                    base.OnMouseDown(mevent);
                    _isPressing = true;
                    if (_focusOnUp && _upImageRect != null) {
                        ClickUpAnimation(true);
                        Invalidate();
                    }
                    if (_focusOnDown && _downImageRect != null) {
                        ClickDownAnimation(true);
                        Invalidate();
                    }
                }
                protected override void OnMouseUp(MouseEventArgs mevent) {
                    base.OnMouseUp(mevent);
                    _isPressing = false;
                    if (_focusOnUp && _upImageRect != null) {
                        ClickUpAnimation(false);
                        Invalidate();
                        if (_onUp != null) {
                            _onUp();
                        }
                    }
                    if (_focusOnDown && _downImageRect != null) {
                        ClickDownAnimation(false);
                        Invalidate();
                        if (_onDown != null) {
                            _onDown();
                        }
                    }
                }
                protected override void OnPaint(PaintEventArgs e) {
                    base.OnPaint(e);
                    if (_upImageShowing != null && _upImageRect != null) {
                        e.Graphics.DrawImage(_upImageShowing, _upImageRect.Value.Location);
                    }
                    if (_downImageShowing != null && _downImageRect != null) {
                        e.Graphics.DrawImage(_downImageShowing, _downImageRect.Value.Location);
                    }
                }
            }
        }
        private void PopUpFieldsPreviewForm(List<OperationDataField> fields) {
            CustomPopUpForm form = new() {
                Title = "数据字段配置预览",
                BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
            };
            CommonButton closeButton = form.AddButton("关闭");
            closeButton.Click += (s, e) => {
                form.Dispose();
            };
            form.PretendToShowToCreateHandlesForChildren();
            WorkplacePiece outer = new() {
                Parent = form.ContentPanel,
                Padding = new(1),
                OuterPenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
            };
            DataGridView gridView = new() {
                Parent = outer,
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
                ScrollBars = ScrollBars.None,
            };
            int newHeaderHeight = WidgetUtils.GridViewHeaderHeight();
            int padding = newHeaderHeight / 2;
            if (newHeaderHeight >= 4) {
                gridView.ColumnHeadersHeight = newHeaderHeight;
                gridView.ColumnHeadersDefaultCellStyle.Font = new(WidgetsConfigs.SystemFontFamily, newHeaderHeight * .45F, FontStyle.Regular, GraphicsUnit.Pixel);
            }
            gridView.ColumnHeadersDefaultCellStyle.BackColor = WidgetUtils.LightColor(ColorTranslator.FromHtml("#E86C10"), .15);
            gridView.ColumnHeadersDefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#FEFEFE");
            gridView.ColumnHeadersDefaultCellStyle.Padding = new(padding, 0, padding, 0);
            gridView.ColumnHeadersDefaultCellStyle.SelectionBackColor = gridView.ColumnHeadersDefaultCellStyle.BackColor;
            DataGridViewColumn[] columnRange = {};
            foreach (OperationDataField field in fields) {
                if (field.Visible) {
                    DataGridViewTextBoxColumn column = new() {
                        HeaderText = field.FieldName,
                        ReadOnly = true,
                    };
                    columnRange = columnRange.Append(column).ToArray();
                } 
            }
            gridView.Columns.AddRange(columnRange);
            // Calculate size
            int gridHeaderHeight = WidgetUtils.GridViewHeaderHeight();
            int contentWidth = (int) (WidgetUtils.MainPanel.Width * .85);
            outer.Size = new(contentWidth - form.ContentPanel.Padding.Size.Width, gridHeaderHeight + 2);
            outer.Padding = new(1);
            gridView.Size = new(outer.Width - 2, gridHeaderHeight);
            gridView.ColumnHeadersHeight = gridHeaderHeight;
            int columnsWidth = 0;
            foreach (DataGridViewColumn column in gridView.Columns) {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                columnsWidth += column.Width;
            }
            // Check for scroll bar
            if (columnsWidth > gridView.Width) {
                HScrollBar hScrollBar = new() {
                    Parent = gridView,
                    Margin = new(0),
                    Size = new(gridView.Width, WidgetUtils.ScrollBarThickness()),
                    Location = new(0, gridHeaderHeight),
                };
                hScrollBar.ValueChanged += (sender, eventArgs) => {
                    gridView.HorizontalScrollingOffset = hScrollBar.Value;
                };
                gridView.Height += hScrollBar.Height;
                outer.Height += hScrollBar.Height;
                WidgetUtils.CalculateScrollBar(hScrollBar, gridView.Width, columnsWidth);
            }
            form.SetContentSizeAndSelfSize(new(contentWidth, outer.Height + form.ContentPanel.Padding.Size.Height));
            form.Show();
        }
        private void InitializeMissionSettings() {
            _missionPanel = new() {
                Parent = this,
                FlowDirection = FlowDirection.TopDown,
            };
            _missionTitlePanel = new("任务配置") {
                Parent = _missionPanel,
                UnderlineColor = ColorConfigs.COLOR_TITLE_UNDERLINE,
            };
            _missionContentPanel = new() {
                Parent = _missionPanel,
            };
            _enableArmLocatingToggle = new("开启力臂定位") {
                Parent = _missionContentPanel,
                Ratio = 6.95,
                Checked = MainUtils.IsArmLocatingEnabled(),
            };
            _enableArmLocatingToggle.CheckedChanged += (s, e) => {
                string v;
                if (_enableArmLocatingToggle.Checked) {
                    v = (int) YesOrNo.YES + "";
                } else {
                    v = (int) YesOrNo.NO + "";
                }
                _settings.Write(IniFileKeys.MissionArmLocatingEnabled, v);
            };
            _armLocatingAccuracyBox = new("力臂定位精度") {
                Parent = _missionContentPanel,
                Ratio = 6.95,
                NumberOnly = true,
            };
            _armLocatingAccuracyBox.SetValue(0, MainUtils.GetArmLocatingAccuracy() + "");
            _armLocatingAccuracyBox.GetTextBox(0).Box.LostFocus += (s, e) => {
                _settings.Write(IniFileKeys.MissionArmLocatingAccuracy, _armLocatingAccuracyBox.GetTextBox(0).Box.Text);
            };
        }
        #endregion

        #region Resize methods
        private void ResizeResolutionPanel() {
            // Resize title
            _resolutionTitlePanel.Size = new(Width, _titleHeight);
            int contentHeight = _boxNBtnHeight + _contentVPadding * 2;
            // Resize Content
            _resolutionContentPanel.Size = new(Width, contentHeight);
            _resolutionContentPanel.Padding = new(_contentHPadding, _contentVPadding, _contentHPadding, _contentVPadding);
            // Resize box and button
            _resolutionOptionsBox.Size = new(Width - _resolutionContentPanel.Padding.Size.Width, _boxNBtnHeight);
            _resolutionOptionsBox.Margin = new(0, 0, _contentHGap / 2, 0);
            // Resize outer panel
            _resolutionPanel.Size = new(Width, _resolutionTitlePanel.Height + _resolutionContentPanel.Height);
        }
        private void ResizeStoragePanel() {
            // Resize title
            _storageTitlePanel.Size = new(Width, _titleHeight);
            int boxWidth = (Width - _contentHPadding * 2);
            int boxVMargin = this._boxNBtnHeight / 2;
            int contentHeight = this._boxNBtnHeight * 4 + _contentVPadding * 2 + boxVMargin * 3;
            // Resize Content
            _storageContentPanel.Size = new(Width, contentHeight);
            _storageContentPanel.Padding = new(_contentHPadding, _contentVPadding, _contentHPadding, _contentVPadding);
            // Resize box
            _storageFileNameTextBox.Size = new(boxWidth, this._boxNBtnHeight);
            _storageFileNameTextBox.Margin = new(0, 0, _contentHGap / 2, 0);
            _storagePathTextBox.Size = new(boxWidth, this._boxNBtnHeight);
            _storagePathTextBox.Margin = new(0, boxVMargin, _contentHGap / 2, 0);
            _storageFieldsButton.Size = new(boxWidth, _boxNBtnHeight);
            _storageFieldsButton.Margin = new(0, boxVMargin, _contentHGap / 2, 0);
            _storeLooseningDataToggle.Size = new(boxWidth, _boxNBtnHeight);
            _storeLooseningDataToggle.Margin = new(0, boxVMargin, _contentHGap / 2, 0);
            // // Resize outer panel
            _storagePanel.Size = new(Width, _storageTitlePanel.Height + _storageContentPanel.Height);
        }
        private void ResizeMissionSettings() {
            // Resize title
            _missionTitlePanel.Size = new(Width, _titleHeight);
            int boxWidth = (Width - _contentHPadding * 3) / 2;
            int boxVMargin = this._boxNBtnHeight / 2;
            int contentHeight = this._boxNBtnHeight * 1 + _contentVPadding * 2 + boxVMargin * 0;
            // Resize Content
            _missionContentPanel.Size = new(Width, contentHeight);
            _missionContentPanel.Padding = new(_contentHPadding, _contentVPadding, _contentHPadding, _contentVPadding);
            // Resize box
            _enableArmLocatingToggle.Size = new(boxWidth, this._boxNBtnHeight);
            _enableArmLocatingToggle.Margin = new(0, 0, _contentHGap / 2, 0);
            _armLocatingAccuracyBox.Size = new(boxWidth, _boxNBtnHeight);
            _armLocatingAccuracyBox.Margin = new(0, 0, 0, 0);
            // // Resize outer panel
            _missionPanel.Size = new(Width, _storageTitlePanel.Height + _storageContentPanel.Height);
        }
        #endregion
    }
}
