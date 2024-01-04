using CustomLibrary.Buttons;
using CustomLibrary.ComboBoxs;
using CustomLibrary.Panels;
using CustomLibrary.Panels.BaseClasses;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.Configs;
using OperationGuidance_service.Apis;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views {
    public class StationSettingsView: CustomContentPanel {
        #region Fields
        // Apis
        private OperationGuidanceApis apis;
        // Common fields
        private readonly int _filtersTableColumnNums = 3;
        private int _contentVerticalGap;
        // Filters panel
        private TableLayoutPanel _filtersPanel;
        private CustomTextBoxGroup _workstationName;
        private CustomTextBoxGroup _toolName;
        private CustomTextBoxGroup _toolModel;
        private CustomTextBoxGroup _armName;
        private CustomTextBoxGroup _armModel;
        // Buttons panel
        private Panel _buttonsPanel;
        private CustomContentPanel _buttonsLeftInnerPanel;
        private CustomContentPanel _buttonsRightInnerPanel;
        private CommonButton _searchButton;
        private CommonButton _ResetButton;
        private CommonButton _addNewButton;
        private CommonButton _modifyButton;
        private CommonButton _deleteButton;
        // DataGridView panel
        private Panel _gridPanel;
        private DataGridView _gridView;
        #endregion

        #region Constructors
        public StationSettingsView() {
            // Default values
            FlowDirection = FlowDirection.TopDown;
            
            // Get Apis
            apis = SystemUtils.GetApis();

            // Initialization
            InitializeContents();
            InitializeFiltersPanel();
            InitializeButtonsPanel();
            InitializeGridPanel();
        }
        #endregion

        #region Initialize methods
        private void InitializeContents() {
            _filtersPanel = new() {
                Parent = this,
                Padding = new(0),
                ColumnCount = _filtersTableColumnNums,
                BackColor = Color.AliceBlue,
            };
            _buttonsPanel = new() {
                Parent = this,
                Margin = new(0),
                Padding = new(0),
                BackColor = Color.AliceBlue,
            };
            _gridPanel = new() {
                Parent = this,
                Margin = new(0),
                Padding = new(0),
                BackColor = Color.AliceBlue,
            };
        }
        private void InitializeFiltersPanel() {
            _workstationName = new("站点名称") {
                Parent = _filtersPanel,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                ForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 6.25,
                NameAlignment = HorizontalAlignment.Right,
            };
            _toolName = new("工具名称") {
                Parent = _filtersPanel,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                ForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 6.25,
                NameAlignment = HorizontalAlignment.Right,
            };
            _toolModel = new("工具型号") {
                Parent = _filtersPanel,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                ForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 6.25,
                NameAlignment = HorizontalAlignment.Right,
            };
            _armName = new("力臂名称") {
                Parent = _filtersPanel,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                ForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 6.25,
                NameAlignment = HorizontalAlignment.Right,
            };
            _armModel = new("力臂型号") {
                Parent = _filtersPanel,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                ForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 6.25,
                NameAlignment = HorizontalAlignment.Right,
            };
        }
        private void InitializeButtonsPanel() {
            _buttonsLeftInnerPanel = new() {
                Parent = this,
                Padding = new(0),
                BackColor = Color.Gray,
            };
            _buttonsRightInnerPanel = new() {
                Parent = this,
                Padding = new(0),
                BackColor = Color.Gray,
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
        private void InitializeGridPanel() {
            BindingSource source = new();
            _gridView = new() {
                Parent = _gridPanel,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToAddRows = false,
                DataSource = source,
            };
        }
        #endregion

        #region Resize methods
        private void ResizeContents() {
            Size contentSize = new(Width - Padding.Size.Width, Height - Padding.Size.Height);
            _contentVerticalGap = (int) (contentSize.Height * .015);
            int filtersPanelHeight = WidgetUtils.TextOrComboBoxHeight() * 2 + _contentVerticalGap;
            int buttonsPanelHeight = WidgetUtils.CommonButtonHeight();

            // Filters panel
            _filtersPanel.Size = new(contentSize.Width, filtersPanelHeight);
            _filtersPanel.Margin = new(0, 0, 0, _contentVerticalGap);
            // Buttons panel
            _buttonsPanel.Size = new(contentSize.Width, buttonsPanelHeight);
            _buttonsPanel.Margin = new(0, 0, 0, _contentVerticalGap);
            // Grid panel
            _gridPanel.Size = new(contentSize.Width, contentSize.Height - filtersPanelHeight - buttonsPanelHeight - _contentVerticalGap * 2);
        }
        private void ResizeFiltersPanel() {
        }
        private void ResizeButtonsPanel() {
        }
        private void ResizeGridPanel() {
        }
        #endregion

        #region Override methods
        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            ResizeContents();
            ResizeFiltersPanel();
            ResizeButtonsPanel();
            ResizeGridPanel();
        }
        public override bool CheckNeedsScrollBar(int parentNewHeight) {
            return false;
        }
        #endregion
    }
}
