using CustomLibrary.Buttons;
using CustomLibrary.Panels;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.Configs;
using OperationGuidance_new.ViewObjects.AbstractClasses;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class DataGridViewGroup<T>: CustomContentPanel where T: AVOBase {
        #region Fields
        // Common fields
        private T _filterObject;
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
        private CommonButton _ResetButton;
        private CommonButton _addNewButton;
        private CommonButton _modifyButton;
        private CommonButton _deleteButton;
        private List<CommonButton> _extraButtons;
        // DataGridView panel
        private DataGridViewPanel<T> _voGridView;
        // Events
        private Func<List<T>> _queryData;
        #endregion

        #region Properties
        #endregion

        #region Events
        public event Func<List<T>> QueryData { add => _queryData += value; remove => _queryData -= value; }
        #endregion

        #region Constructors
        public DataGridViewGroup() {
            // Default values
            FlowDirection = FlowDirection.TopDown;

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
            // Buttons
            _searchButton = new() {
                Parent = _buttonsLeftInnerPanel,
                Label = "查询",
            };
            _ResetButton = new() {
                Parent = _buttonsLeftInnerPanel,
                Label = "重置",
            };
            _addNewButton = new() {
                Parent = _buttonsRightInnerPanel,
                Label = "新增",
            };
            _modifyButton = new() {
                Parent = _buttonsRightInnerPanel,
                Label = "修改",
            };
            _deleteButton = new() {
                Parent = _buttonsRightInnerPanel,
                Label = "删除",
            };
        }
        private void InitializeGridView() {
        }
        #endregion

        #region Reusable methods
        public CustomTextBoxGroup AddTextBox<V>(string boxName, bool numberOnly, Action<T, V?> propertySetter) {
            CustomTextBoxGroup boxGroup = new(boxName) {
                Parent = _filtersTablePanel,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                ForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR,
                NumberOnly = numberOnly,
            };
            TextBox box = boxGroup.GetTextBox(0).Box;
            box.TextChanged += (sender, eventArgs) => {
                try {
                    V? value = (V?) Convert.ChangeType(box.Text, typeof(V));
                    propertySetter(_filterObject, value);
                } catch (InvalidCastException e) {
                    System.Console.WriteLine($"Can not convert string to type<{typeof(V)}>. Exception: {e}");
                }
            };
            return boxGroup;
        }
        public CustomComboBoxGroup<V> AddComboBox<V>(string boxName, bool numberOnly, Action<T, V?> propertySetter, Dictionary<string, V> items) {
            CustomComboBoxGroup<V> boxGroup = new(boxName) {
                Parent = _filtersTablePanel,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                ForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR,
            };
            boxGroup.ItemSelected += () => {
                propertySetter(_filterObject, boxGroup.Value);
            };
            Dictionary<string, V>.Enumerator enumerator = items.GetEnumerator();
            while (enumerator.MoveNext()) {
                KeyValuePair<string, V> current = enumerator.Current;
                boxGroup.AddItem(current.Key, current.Value);
            }
            return boxGroup;
        }
        #endregion

        #region Resize methods
        private void ResizeContents(Size contentSize) {
            // Filters panel
            int filtersPanelHeight = _textOrComboHeight * 2 + _contentVerticalGap;
            _filtersTablePanel.Size = new(contentSize.Width, filtersPanelHeight);
            _filtersTablePanel.Margin = new(0, 0, 0, _contentVerticalGap);
            // Buttons panel
            _buttonsPanel.Size = new(contentSize.Width, _buttonHeight);
            _buttonsPanel.Margin = new(0, 0, 0, _contentVerticalGap);
            // Grid panel
            _voGridView.Size = new(contentSize.Width, contentSize.Height - filtersPanelHeight - _buttonHeight - _contentVerticalGap * 2);
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
            int buttonHeight = (int)(_buttonHeight * 2.5);
            // Resize buttons
            // Left panel width
            int leftPanelWidht = 0;
            ControlCollection listLeft = _buttonsLeftInnerPanel.Controls;
            for (int i = 0 ; i < listLeft.Count ; i++) {
                Control control = listLeft[i];
                control.Size = new(buttonHeight, _buttonHeight);
                leftPanelWidht += buttonHeight;
                // Calculate margin
                if (i != 0) {
                    control.Margin = new(_contentHerticalGap, 0, 0, 0);
                    leftPanelWidht += _contentHerticalGap;
                }
            }
            // Right panel width
            int rightPanelWidht = 0;
            ControlCollection listRight = _buttonsRightInnerPanel.Controls;
            for (int i = 0 ; i < listRight.Count ; i++) {
                Control control = listRight[i];
                control.Size = new(buttonHeight, _buttonHeight);
                rightPanelWidht += buttonHeight;
                // Calculate margin
                if (i != 0) {
                    control.Margin = new(_contentHerticalGap, 0, 0, 0);
                    rightPanelWidht += _contentHerticalGap;
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
