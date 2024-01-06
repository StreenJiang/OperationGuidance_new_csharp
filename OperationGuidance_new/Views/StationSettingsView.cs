using System.Reflection;
using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.DataGridViewRelateds;
using CustomLibrary.Panels;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.Attributes;
using OperationGuidance_new.Configs;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_service.Apis;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views {
    public class StationSettingsView: CustomContentPanel {
        #region Fields
        // Apis
        private OperationGuidanceApis apis;
        // Common fields
        private readonly int _filtersTableColumnNums = 3;
        private int _textOrComboHeight;
        private int _buttonHeight;
        private int _contentVerticalGap;
        private int _contentHerticalGap;
        // Filters panel
        private TableLayoutPanel _filtersTablePanel;
        private CustomTextBoxGroup _workstationName;
        private CustomTextBoxGroup _toolName;
        private CustomComboBoxGroup<int> _toolModel;
        private CustomTextBoxGroup _armName;
        private CustomComboBoxGroup<int> _armModel;
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
        private CustomContentPanel _gridPanel;
        private DataGridView _gridView;
        private Panel _gridPageInfoPanel;
        private TableLayoutPanel _gridPageInfoButtonsPanel;
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
            _gridPanel = new() {
                Parent = this,
                AutoPadding = false,
                Padding = new(1),
                PenBorderColor = ConfigsVariables.COLOR_CONTENT_PANEL_INNER_BORDER,
            };
        }
        private void InitializeFiltersPanel() {
            _workstationName = new("站点名称") {
                Parent = _filtersTablePanel,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                ForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR,
            };
            _toolName = new("工具名称") {
                Parent = _filtersTablePanel,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                ForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR,
            };
            _toolModel = new("工具型号") {
                Parent = _filtersTablePanel,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                ForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR,
            };
            _armName = new("力臂名称") {
                Parent = _filtersTablePanel,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                ForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR,
            };
            _armModel = new("力臂型号") {
                Parent = _filtersTablePanel,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                ForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR,
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
        private void InitializeGridPanel() {
            List<WorkstationVO> vos = new();
            BindingSource source = new();
            vos.Add(new() {
                    id = 1,
                    name = "test",
                    tool_name = "aaaagasdfasfasdfasdfasfasdf",
                    arm_name = "asdfasgasgsadfsadfsadfasdfasdf",
                    enabled = (int) YesOrNo.YES,
                    });
            vos.Add(new());
            vos.Add(new());
            vos.Add(new());
            vos.Add(new());
            source.DataSource = vos;
            _gridView = new() {
                Parent = _gridPanel,
                Margin = new(0),
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                // AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToAddRows = false,
                AllowUserToResizeColumns = true,
                AutoGenerateColumns = false,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
            };
            DataGridViewColumn[] columnRange = {};
            Type type = typeof(WorkstationVO);
            List<PropertyInfo> props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
            foreach (PropertyInfo property in props) {
                IEnumerable<Attribute> enumerable = property.GetCustomAttributes();
                foreach (Attribute attribute in enumerable) {
                    if (attribute is GridColumnAttribute) {
                        GridColumnAttribute gridColumn = (GridColumnAttribute) attribute;
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
            _gridView.ColumnHeadersDefaultCellStyle.SelectionBackColor = _gridView.ColumnHeadersDefaultCellStyle.BackColor;
            _gridView.Columns.AddRange(columnRange);
            _gridView.Columns[0].Frozen = true;
            HashSet<int> columsPainted = new();
            _gridView.CellPainting += (sender, eventArgs) => {
                // Resize and Relocate ToggleButtonColumns
                if (eventArgs.ColumnIndex > -1 && eventArgs.RowIndex > -1) {
                    DataGridViewColumn column = _gridView.Columns[eventArgs.ColumnIndex];
                    if (column is DataGridViewToggleButtonColumn) {
                        DataGridViewRow row = _gridView.Rows[eventArgs.RowIndex];
                        WorkstationVO vo = (WorkstationVO) row.DataBoundItem;
                        // If vo'id is null, then this row is just a blank row for displaying
                        if (vo != null && vo.id != null && row.Cells[column.Index] is DataGridViewToggleButtonCell) {
                            DataGridViewToggleButtonCell cell = (DataGridViewToggleButtonCell) row.Cells[column.Index];
                            // Set toggle state to button in case they're not matched
                            cell.ToggleButton.Checked = vo.bool_enabled != null ? vo.bool_enabled.Value : false;
                            // Show cell in case it's Hiden
                            cell.ToggleButtonParentPanel.Visible = true;
                            // Resize
                            Size cellSize = cell.Size;
                            int panelHeight = (int) (cellSize.Height * .65);
                            int panelWidth = panelHeight * 3;
                            cell.ToggleButtonParentPanel.Size = new(panelWidth, panelHeight);
                            // Relocate
                            Point cellPoint = _gridView.GetCellDisplayRectangle(eventArgs.ColumnIndex, eventArgs.RowIndex, true).Location;
                            cell.ToggleButtonParentPanel.Location = new(cellPoint.X + (cellSize.Width - panelWidth) / 2, cellPoint.Y + (cellSize.Height - panelHeight) / 2);
                        }
                    }
                }
                columsPainted.Add(eventArgs.ColumnIndex);
            };
            _gridView.Paint += (sender, eventArgs) => {  
                // Hide ToggleButtonColumns if they don't show
                if (columsPainted.Count > 0) {
                    if (_gridView.SelectedColumns.Count != columsPainted.Count) {
                        foreach (DataGridViewColumn column in _gridView.Columns) {
                            if (column is DataGridViewToggleButtonColumn && !columsPainted.Contains(column.Index)) {
                                foreach (DataGridViewRow row in _gridView.Rows) {
                                    if (row.Cells[column.Index] is DataGridViewToggleButtonCell) {
                                        DataGridViewToggleButtonCell cell = (DataGridViewToggleButtonCell) row.Cells[column.Index];
                                        cell.ToggleButtonParentPanel.Visible = false;
                                    }
                                }
                            }
                        }
                    }
                    columsPainted.Clear();
                }
            };
            _gridView.CellValueChanged += (sender, eventArgs) => {
                DataGridViewCell cell = _gridView.Rows[eventArgs.RowIndex].Cells[eventArgs.ColumnIndex];
                System.Console.WriteLine("Value change to: " + cell.Value);
            };
            _gridView.DataSourceChanged += (sender ,eventArgs) => {
                System.Console.WriteLine("DataSourceChanged");
            };
            _gridView.DataSource = source;
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
            _gridPanel.Size = new(contentSize.Width, contentSize.Height - filtersPanelHeight - _buttonHeight - _contentVerticalGap * 2);
        }
        private void ResizeFiltersPanel(Size contentSize) {
            // Width of box
            int boxWidth = (contentSize.Width - _contentHerticalGap * (_filtersTableColumnNums - 1)) / _filtersTableColumnNums;
            // Resize boxes
            TableLayoutControlCollection list = _filtersTablePanel.Controls;
            for (int i = 0; i < list.Count; i++) {
                Control control = list[i];
                control.Size = new(boxWidth, _textOrComboHeight);
                // Calculate margin
                Padding margin = new(0);
                if (i >= _filtersTableColumnNums) margin.Top = _contentVerticalGap;
                if (i % _filtersTableColumnNums != 0) margin.Left = _contentHerticalGap;
                control.Margin = margin;
            }
        }
        private void ResizeButtonsPanel() {
            // Width of button 
            int buttonHeight = (int) (_buttonHeight * 2.5);
            // Resize buttons
            // Left panel width
            int leftPanelWidht = 0;
            ControlCollection listLeft = _buttonsLeftInnerPanel.Controls;
            for (int i = 0; i < listLeft.Count; i++) {
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
            for (int i = 0; i < listRight.Count; i++) {
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
        private void ResizeGridPanel() {
            _gridView.Size = new(_gridPanel.Width - _gridPanel.Padding.Size.Width, _gridPanel.Height - _gridPanel.Padding.Size.Height);
            int newHeaderHeight = WidgetUtils.GridViewHeaderRowHeight();
            if (newHeaderHeight >= 4) {
                _gridView.ColumnHeadersHeight = newHeaderHeight;
                _gridView.ColumnHeadersDefaultCellStyle.Font = new(WidgetsConfigs.SystemFontFamily, newHeaderHeight * .5F, FontStyle.Regular, GraphicsUnit.Pixel);
            }
            int newContentHeight = WidgetUtils.GridViewContentRowHeight();
            if (newContentHeight >= 4) {
                foreach (DataGridViewRow row in _gridView.Rows) {
                    row.Height = newContentHeight;
                }
                _gridView.RowsDefaultCellStyle.Font = new(WidgetsConfigs.SystemFontFamily, newContentHeight * .5F, FontStyle.Regular, GraphicsUnit.Pixel);
            }
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
            ResizeGridPanel();
        }
        public override bool CheckNeedsScrollBar(int parentNewHeight) {
            return false;
        }
        #endregion
    }
}
