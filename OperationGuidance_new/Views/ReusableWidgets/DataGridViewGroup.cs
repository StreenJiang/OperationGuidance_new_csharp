using System.Reflection;
using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.ViewObjects.AbstractClasses;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class DataGridViewGroup<T>: CustomContentPanel where T: AVOBase, new() {
        #region Fields
        // Common fields
        private T _filterParametersVO;
        private readonly int _filtersTableColumnNums = 3;
        private int _textOrComboHeight;
        private int _buttonHeight;
        private int _contentVerticalGap;
        private int _contentHerticalGap;
        // Filters panel
        private TableLayoutPanel _filtersTablePanel;
        // Buttons panel
        private Panel _buttonsPanel;
        private CustomContentPanel _buttonsLeftInnerPanel;
        private CustomContentPanel _buttonsRightInnerPanel;
        private CommonButton _searchButton;
        private CommonButton _resetButton;
        private CommonButton _addNewButton;
        private CommonButton _modifyButton;
        private CommonButton _deleteButton;
        private List<CommonButton> _extraButtons;
        // DataGridView panel
        private DataGridViewPanel<T> _voGridView;
        // Delegates
        private Func<T, List<T>> _queryData;
        private Action<Action> _addNewClick;
        private Action<Action> _modifyClick;
        private Action<Action> _deleteClick;
        // Events
        #endregion

        #region Properties
        public List<CommonButton> ExtraButtons { get => _extraButtons; }
        public List<T> DataSource { get => _voGridView.DataSource; set => _voGridView.DataSource = value; }
        public Func<T, List<T>> QueryData { get => _queryData; set => _queryData = value; }
        public Action<Action> AddNewClick { get => _addNewClick; set => _addNewClick = value; }
        public Action<Action> ModifyClick { get => _modifyClick; set => _modifyClick = value; }
        public Action<Action> DeleteClick { get => _deleteClick; set => _deleteClick = value; }
        public bool SearchButtonVisible { get => _searchButton.Visible; set => _searchButton.Visible = value; }
        public bool ResetButtonVisible { get => _resetButton.Visible; set => _resetButton.Visible = value; }
        public bool AddNewButtonVisible { get => _addNewButton.Visible; set => _addNewButton.Visible = value; }
        public bool ModifyButtonVisible { get => _modifyButton.Visible; set => _modifyButton.Visible = value; }
        public bool DeleteButtonVisible { get => _deleteButton.Visible; set => _deleteButton.Visible = value; }
        #endregion

        #region Events
        #endregion

        #region Constructors
        public DataGridViewGroup() {
            // Default values
            FlowDirection = FlowDirection.TopDown;
            _filterParametersVO = new();
            _queryData = new((vo) => {
                WidgetUtils.ShowNoticePopUp("Func<QueryData> has not been set.");
                return new();
            });
            _addNewClick = new((a) => WidgetUtils.ShowNoticePopUp("Func<AddNewClick> has not been set."));
            _modifyClick = new((a) => WidgetUtils.ShowNoticePopUp("Func<ModifyClick> has not been set."));
            _deleteClick = new((a) => WidgetUtils.ShowNoticePopUp("Func<DeleteClick> has not been set."));

            // Initialization
            InitializeContents();
            InitializeButtonsPanel();
            InitializeGridView();
        }
        #endregion

        #region Initialize methods
        private void InitializeContents() {
            _filtersTablePanel = new() {
                Parent = this,
                Padding = new(0),
                ColumnCount = _filtersTableColumnNums,
            };
            _buttonsPanel = new() {
                Parent = this,
                Margin = new(0),
                Padding = new(0),
            };
            _voGridView = new() {
                Parent = this,
            };
        }
        private void InitializeButtonsPanel() {
            _buttonsLeftInnerPanel = new() {
                Parent = _buttonsPanel,
                Padding = new(0),
                Dock = DockStyle.Left,
            };
            _buttonsRightInnerPanel = new() {
                Parent = _buttonsPanel,
                Padding = new(0),
                Dock = DockStyle.Right,
            };
            _extraButtons = new();
            // Buttons
            _searchButton = new() {
                Parent = _buttonsLeftInnerPanel,
                Label = "查询",
            };
            _searchButton.Click += (sender, eventArgs) => {
                QueryAndRefresh();
                _voGridView.ResizeChildren();
            };
            _resetButton = new() {
                Parent = _buttonsLeftInnerPanel,
                Label = "重置",
            };
            _resetButton.Click += (sender, eventArgs) => {
                _filterParametersVO = new();
                foreach (Control control in _filtersTablePanel.Controls) {
                    if (control is CustomTextBoxGroup textBoxGroup) {
                        foreach (CustomTextBox box in textBoxGroup.TextBoxes) {
                            box.Box.Text = "";
                        }
                    } else if (control.GetType().Name == typeof(CustomComboBoxGroup<>).Name) {
                        Type type = control.GetType();
                        MethodInfo? methodInfo = type.GetMethod(CustomComboBoxGroup<object>.ResetName());
                        if (methodInfo != null) {
                            methodInfo.Invoke(control, null);
                        }
                    }
                }
            };
            _addNewButton = new() {
                Parent = _buttonsRightInnerPanel,
                Label = "新增",
            };
            _addNewButton.Click += (sender, eventArgs) => {
                _addNewClick(QueryAndRefresh);
            };
            _modifyButton = new() {
                Parent = _buttonsRightInnerPanel,
                Label = "修改",
            };
            _modifyButton.Click += (sender, eventArgs) => {
                _modifyClick(QueryAndRefresh);
            };
            _deleteButton = new() {
                Parent = _buttonsRightInnerPanel,
                Label = "删除",
            };
            _deleteButton.Click += (sender, eventArgs) => {
                _deleteClick(QueryAndRefresh);
            };
        }
        private void InitializeGridView() {
        }
        #endregion

        #region Reusable methods
        public CustomTextBoxGroup AddTextBox<V>(string boxName, bool numberOnly, Action<T, V?> propertySetter) {
            CustomTextBoxGroup boxGroup = new(boxName) {
                Parent = _filtersTablePanel,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                NumberOnly = numberOnly,
            };
            TextBox box = boxGroup.GetTextBox(0).Box;
            V? value = default(V);
            box.TextChanged += (sender, eventArgs) => HandleTextChanged(box, out value);
            propertySetter(_filterParametersVO, value);
            return boxGroup;
        }
        public CustomTextBoxGroup AddSeparateTextBox<V>(string boxName, string separator, bool numberOnly, Action<T, V?, V?> propertiesSetter) {
            CustomTextBoxGroup boxGroup = new(boxName) {
                Parent = _filtersTablePanel,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                Separator = separator,
                NumberOnly = numberOnly,
            };
            // Need two boxes
            boxGroup.AddTextBox();
            // Handle values
            V? value1 = default(V);
            V? value2 = default(V);
            TextBox box1 = boxGroup.GetTextBox(0).Box;
            TextBox box2 = boxGroup.GetTextBox(1).Box;
            box1.TextChanged += (sender, eventArgs) => HandleTextChanged(box1, out value1);
            box2.TextChanged += (sender, eventArgs) => HandleTextChanged(box2, out value2);
            propertiesSetter(_filterParametersVO, value1, value2);
            return boxGroup;
        }
        private void HandleTextChanged<V>(TextBox box, out V? value) {
            value = default(V);
            try {
                value = (V?) Convert.ChangeType(box.Text, typeof(V));
            } catch (InvalidCastException e) {
                System.Console.WriteLine($"Can not convert string to type<{typeof(V)}>. Exception: {e}");
            }
        }
        public CustomComboBoxGroup<V> AddComboBox<V>(string boxName, bool numberOnly, Action<T, V?> propertySetter, Dictionary<string, V> items) {
            CustomComboBoxGroup<V> boxGroup = new(boxName) {
                Parent = _filtersTablePanel,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
            };
            boxGroup.ItemSelected += () => {
                propertySetter(_filterParametersVO, boxGroup.Value);
            };
            Dictionary<string, V>.Enumerator enumerator = items.GetEnumerator();
            while (enumerator.MoveNext()) {
                KeyValuePair<string, V> current = enumerator.Current;
                boxGroup.AddItem(current.Key, current.Value);
            }
            return boxGroup;
        }
        public CommonButton AddExtraButton(string buttonName) {
            CommonButton extraButton = new() {
                Parent = _buttonsRightInnerPanel,
                Label = buttonName,
            };
            _extraButtons.Add(extraButton);
            return extraButton;
        }
        private void QueryAndRefresh() {
            _voGridView.DataSource = _queryData(_filterParametersVO);
        }
        #endregion

        #region Resize methods
        private void ResizeContents(Size contentSize) {
            // Filters panel
            int filtersPanelHeight = 0;
            if (_filtersTablePanel.Controls.Count > 0) {
                int lines = (int) Math.Ceiling(_filtersTablePanel.Controls.Count / (decimal) _filtersTableColumnNums);
                for (int i = 0; i < lines; i++) {
                    filtersPanelHeight += _textOrComboHeight;
                    if (i > 0) {
                        filtersPanelHeight += _contentVerticalGap;
                    }
                }
                _filtersTablePanel.Size = new(contentSize.Width, filtersPanelHeight);
                _filtersTablePanel.Margin = new(0, 0, 0, _contentVerticalGap);
                _filtersTablePanel.Visible = true;
            } else {
                _filtersTablePanel.Visible = false;
            }
            // Buttons panel
            _buttonsPanel.Size = new(contentSize.Width, _buttonHeight);
            _buttonsPanel.Margin = new(0, 0, 0, _contentVerticalGap);
            // Grid panel
            int gridHeight = contentSize.Height - filtersPanelHeight - _buttonHeight - _contentVerticalGap;
            if (_filtersTablePanel.Visible) {
                gridHeight -= _contentVerticalGap;
            }
            _voGridView.Size = new(contentSize.Width, gridHeight);
        }
        private void ResizeFiltersPanel(Size contentSize) {
            // Width of box
            int boxWidth = (contentSize.Width - _contentHerticalGap * (_filtersTableColumnNums - 1)) / _filtersTableColumnNums;
            // Resize boxes
            TableLayoutControlCollection list = _filtersTablePanel.Controls;
            for (int i = 0 ; i < list.Count ; i++) {
                Control control = list[i];
                control.Size = new(boxWidth, _textOrComboHeight);
                // Calculate margin
                Padding margin = new(0);
                if (i >= _filtersTableColumnNums)
                    margin.Top = _contentVerticalGap;
                if (i % _filtersTableColumnNums != 0)
                    margin.Left = _contentHerticalGap;
                control.Margin = margin;
            }
        }
        private void ResizeButtonsPanel() {
            // Width of button 
            int buttonWidth = (int)(_buttonHeight * 2.5);
            // Resize buttons
            // Left panel width
            int leftPanelWidht = 0;
            ControlCollection listLeft = _buttonsLeftInnerPanel.Controls;
            for (int i = 0 ; i < listLeft.Count ; i++) {
                Control control = listLeft[i];
                control.Size = new(buttonWidth, _buttonHeight);
                leftPanelWidht += buttonWidth;
                // Calculate margin
                if (i != 0) {
                    control.Margin = new(_contentHerticalGap, 0, 0, 0);
                    leftPanelWidht += _contentHerticalGap;
                }
            }
            // Right panel width
            int rightPanelWidht = 0;
            int count = 0;
            foreach (Control control in _buttonsRightInnerPanel.Controls) {
                if (control.Visible) {
                    control.Size = new(buttonWidth, _buttonHeight);
                    rightPanelWidht += buttonWidth;
                    // Calculate margin
                    if (count != 0) {
                        control.Margin = new(_contentHerticalGap, 0, 0, 0);
                        rightPanelWidht += _contentHerticalGap;
                    }
                    count++;
                }
            }
            _buttonsLeftInnerPanel.Size = new(leftPanelWidht, _buttonHeight);
            _buttonsRightInnerPanel.Size = new(rightPanelWidht, _buttonHeight);
        }
        #endregion

        #region Override methods
        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            Size contentSize = new(Width - Padding.Size.Width, Height - Padding.Size.Height);
            // Calculate gaps
            _contentVerticalGap = (int) (contentSize.Height * .015);
            _contentHerticalGap = (int) (contentSize.Width * .015);
            // Calculate box and button Height
            _textOrComboHeight = WidgetUtils.TextOrComboBoxHeight();
            _buttonHeight = WidgetUtils.CommonButtonHeight();
            // Resize
            ResizeContents(contentSize);
            ResizeFiltersPanel(contentSize);
            ResizeButtonsPanel();
        }
        public override bool CheckNeedsScrollBar(int parentNewHeight) {
            return false;
        }
        #endregion
    }
}
