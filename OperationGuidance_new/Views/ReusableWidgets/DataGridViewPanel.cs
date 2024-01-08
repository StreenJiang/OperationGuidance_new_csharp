using System.Reflection;
using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.DataGridViewRelateds;
using CustomLibrary.Panels;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.Attributes;
using OperationGuidance_new.Configs;
using OperationGuidance_new.ViewObjects.AbstractClasses;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class DataGridViewPanel<V>: CustomContentPanel where V : AVOBase {
        #region Feilds 
        private DataGridView _gridView;
        private Panel _gridPageInfoPanel;
        private TableLayoutPanel _gridPageInfoButtonsPanel;
        private Label _dataCountInfo;
        private Label _pageInfo;
        private Label _countPerPage;
        private Label _jumpToText;
        private CustomTextBox _jumpToBox;
        private Label _jumpToTextRight;
        private PageSwitchButton _first;
        private PageSwitchButton _backward;
        private PageSwitchButton _forward;
        private PageSwitchButton _last;
        private int _currentPage;
        private int _pageSize;
        private int _totalPages;
        private BindingSource _bindingSource;
        private List<V> _dataSource;
        #endregion

        #region Properties
        public DataGridView GridView { get => _gridView; }
        public int CurrentPage { get => _currentPage; set => _currentPage = value; }
        public int TotalPages { get => _totalPages; set => _totalPages = value; }
        public int PageSize { 
            get => _pageSize; 
            set {
                _pageSize = value; 
                _countPerPage.Text = $"{_pageSize} 条/页";
            }
        }
        public List<V> DataSource {
            get => _dataSource;
            set {
                _dataSource = value;
                _bindingSource.DataSource = value;
                _gridView.DataSource = _bindingSource;
                _totalPages = (int) Math.Ceiling(value.Count / (double) _pageSize);
                _dataCountInfo.Text = $"共 {value.Count} 条";
            }
        }
        #endregion

        #region Constructors
        public DataGridViewPanel() {
            // Self properties
            AutoPadding = false;
            Padding = new(1);
            PenBorderColor = ConfigsVariables.COLOR_CONTENT_PANEL_INNER_BORDER;
            // Data source
            _bindingSource = new();
            _dataSource = new();
            // Page info
            _pageSize = 20;
            _currentPage = 1;
            _gridPageInfoPanel = new() {
                Parent = this,
                Margin = new(0),
                Padding = new(0),
            };
            _gridPageInfoButtonsPanel = new() {
                Parent = _gridPageInfoPanel,
                Margin = new(0),
                Padding = new(0),
            };
            _dataCountInfo = new() {
                Parent = _gridPageInfoButtonsPanel,
                Margin = new(0),
                Padding = new(0),
            };
            _pageInfo = new() {
                Parent = _gridPageInfoButtonsPanel,
                Margin = new(0),
                Padding = new(0),
            };
            _countPerPage = new() {
                Parent = _gridPageInfoButtonsPanel,
                Margin = new(0),
                Padding = new(0),
                Text = $"{_pageSize} 条/页",
            };
            _jumpToText = new() {
                Parent = _gridPageInfoButtonsPanel,
                Margin = new(0),
                Padding = new(0),
            };
            _jumpToTextRight = new() {
                Parent = _gridPageInfoButtonsPanel,
                Margin = new(0),
                Padding = new(0),
            };
            _first = new() {
                Icon = Properties.Resources.page_btn_backward_fast,
            };
            _backward = new() {
                Icon = Properties.Resources.page_btn_backward,
            };
            _forward = new() {
                Icon = Properties.Resources.page_btn_forward,
            };
            _last = new() {
                Icon = Properties.Resources.page_btn_forward_fast,
            };
            // Data grid view
            _gridView = new() {
                Parent = this,
                Margin = new(0),
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.ColumnHeader,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToAddRows = false,
                AllowUserToResizeColumns = true,
                AutoGenerateColumns = false,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
            };
            DataGridViewColumn[] columnRange = { };
            Type type = typeof(V);
            List<PropertyInfo> props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
            foreach (PropertyInfo property in props) {
                IEnumerable<Attribute> enumerable = property.GetCustomAttributes();
                foreach (Attribute attribute in enumerable) {
                    if (attribute is GridColumnAttribute) {
                        GridColumnAttribute gridColumn = (GridColumnAttribute)attribute;
                        string columnName;
                        if (gridColumn.ColumnName != null && gridColumn.ColumnName != string.Empty) {
                            columnName = gridColumn.ColumnName;
                        } else {
                            columnName = property.Name;
                        }
                        if (gridColumn.CellType != null && gridColumn.CellType == typeof(ToggleButton)) {
                            DataGridViewToggleButtonColumn column = new()
                            {
                                DataPropertyName = property.Name,
                                HeaderText = columnName,
                                ReadOnly = true,
                            };
                            columnRange = columnRange.Append(column).ToArray();
                        } else {
                            DataGridViewTextBoxColumn column = new()
                            {
                                DataPropertyName = property.Name,
                                HeaderText = columnName,
                                ReadOnly = true,
                            };
                            columnRange = columnRange.Append(column).ToArray();
                        }
                    }
                }
            }
            _gridView.ColumnHeadersDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#E86C10");
            _gridView.ColumnHeadersDefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#FEFEFE");
            _gridView.ColumnHeadersDefaultCellStyle.SelectionBackColor = _gridView.ColumnHeadersDefaultCellStyle.BackColor;
            _gridView.RowsDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#F0F0F0");
            _gridView.RowsDefaultCellStyle.SelectionBackColor = WidgetUtils.LighterColor(ColorTranslator.FromHtml("#E86C10"), .6);
            _gridView.RowsDefaultCellStyle.SelectionForeColor = WidgetUtils.DarkerColor(ColorTranslator.FromHtml("#E86C10"), .8);
            _gridView.AlternatingRowsDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#FEFEFE");
            _gridView.BackgroundColor = WidgetUtils.LighterColor(ColorTranslator.FromHtml("#E86C10"), .95);
            _gridView.Columns.AddRange(columnRange);
            _gridView.Columns[0].Frozen = true;
            HashSet<int> columsPainted = new();
            _gridView.CellPainting += (sender, eventArgs) => {
                // Resize and Relocate ToggleButtonColumns
                if (eventArgs.ColumnIndex > -1 && eventArgs.RowIndex > -1) {
                    DataGridViewColumn column = _gridView.Columns[eventArgs.ColumnIndex];
                    DataGridViewRow row = _gridView.Rows[eventArgs.RowIndex];
                    if (column.Frozen) {
                        // Forzen columns have different color
                        if (eventArgs.RowIndex % 2 != 0) {
                            row.Cells[eventArgs.ColumnIndex].Style.BackColor = WidgetUtils.LighterColor(_gridView.ColumnHeadersDefaultCellStyle.BackColor, .9);
                        } else {
                            row.Cells[eventArgs.ColumnIndex].Style.BackColor = WidgetUtils.LighterColor(_gridView.ColumnHeadersDefaultCellStyle.BackColor, .85);
                        }
                    }
                    if (column is DataGridViewToggleButtonColumn) {
                        // DataGridViewRow row = _gridView.Rows[eventArgs.RowIndex];
                        V vo = (V)row.DataBoundItem;
                        // If vo'id is null, then this row is just a blank row for displaying
                        if (vo != null && vo.id != null && row.Cells[column.Index] is DataGridViewToggleButtonCell) {
                            DataGridViewToggleButtonCell cell = (DataGridViewToggleButtonCell)row.Cells[column.Index];
                            // Set toggle state to button in case they're not matched
                            cell.ToggleButton.Checked = (bool) cell.Value;
                            // Show cell in case it's Hiden
                            cell.ToggleButtonParentPanel.Visible = true;
                            // Resize
                            Size cellSize = cell.Size;
                            int panelHeight = (int)(cellSize.Height * .65);
                            int panelWidth = panelHeight * 3;
                            cell.ToggleButtonParentPanel.Size = new(panelWidth, panelHeight);
                            // Relocate
                            DataGridViewContentAlignment headerAlignment = DataGridViewContentAlignment.NotSet;
                            if (column.HeaderCell.Style.Alignment != DataGridViewContentAlignment.NotSet) {
                                headerAlignment = column.HeaderCell.Style.Alignment;
                            } else if (_gridView.ColumnHeadersDefaultCellStyle.Alignment != DataGridViewContentAlignment.NotSet) {
                                headerAlignment = _gridView.ColumnHeadersDefaultCellStyle.Alignment;
                            }
                            Point cellPoint = _gridView.GetCellDisplayRectangle(eventArgs.ColumnIndex, eventArgs.RowIndex, true).Location;
                            switch (headerAlignment) {
                                case DataGridViewContentAlignment.TopLeft:
                                    break;
                                default:
                                case DataGridViewContentAlignment.MiddleLeft:
                                    cellPoint.Y += (cellSize.Height - panelHeight) / 2;
                                    break;
                                case DataGridViewContentAlignment.BottomLeft:
                                    cellPoint.Y += cellSize.Height - panelHeight;
                                    break;
                                case DataGridViewContentAlignment.TopRight:
                                    cellPoint.X += cellSize.Width - panelWidth;
                                    break;
                                case DataGridViewContentAlignment.MiddleRight:
                                    cellPoint.X += (cellSize.Width - panelWidth) / 2;
                                    cellPoint.Y += (cellSize.Height - panelHeight) / 2;
                                    break;
                                case DataGridViewContentAlignment.BottomRight:
                                    cellPoint.X += cellSize.Width - panelWidth;
                                    cellPoint.Y += cellSize.Height - panelHeight;
                                    break;
                                case DataGridViewContentAlignment.TopCenter:
                                    cellPoint.X += (cellSize.Width - panelWidth) / 2;
                                    break;
                                case DataGridViewContentAlignment.MiddleCenter:
                                    cellPoint.X += (cellSize.Width - panelWidth) / 2;
                                    cellPoint.Y += (cellSize.Height - panelHeight) / 2;
                                    break;
                                case DataGridViewContentAlignment.BottomCenter:
                                    cellPoint.X += cellSize.Width - panelWidth;
                                    break;
                            }
                            cell.ToggleButtonParentPanel.Location = cellPoint;
                        }
                    }
                }
                columsPainted.Add(eventArgs.ColumnIndex);
            };
            _gridView.Paint += (sender, eventArgs) => {
                // Hide ToggleButtonColumns if they don't show
                if (columsPainted.Count > 1) {
                    foreach (DataGridViewColumn column in _gridView.Columns) {
                        if (column is DataGridViewToggleButtonColumn && !columsPainted.Contains(column.Index)) {
                            foreach (DataGridViewRow row in _gridView.Rows) {
                                if (row.Cells[column.Index] is DataGridViewToggleButtonCell) {
                                    DataGridViewToggleButtonCell cell = (DataGridViewToggleButtonCell)row.Cells[column.Index];
                                    cell.ToggleButtonParentPanel.Visible = false;
                                }
                            }
                        }
                    }
                    columsPainted.Clear();
                }
            };
            _gridView.CellValueChanged += (sender, eventArgs) => {
                DataGridViewCell cell = _gridView.Rows[eventArgs.RowIndex].Cells[eventArgs.ColumnIndex];
                Console.WriteLine("Value change to: " + cell.Value);
            };
            _gridView.DataSourceChanged += (sender, eventArgs) => {
                Console.WriteLine("DataSourceChanged");
            };
            _gridView.DataBindingComplete += (sender, eventArgs) => {
                // Clear auto selection of first row
                _gridView.ClearSelection();
            };
            _gridView.CellMouseEnter += (sender, eventArgs) => {
                if (eventArgs.RowIndex > -1 && eventArgs.ColumnIndex > -1) {
                    DataGridViewCell cell = _gridView.Rows[eventArgs.RowIndex].Cells[eventArgs.ColumnIndex];
                    if (cell.Value == null) {
                        _gridView.ShowCellToolTips = false;
                    } else {
                        _gridView.ShowCellToolTips = true;
                    }
                }

            };
            _gridView.CellMouseMove += (sender, eventArgs) => {
                if (eventArgs.RowIndex > -1) {
                    DataGridViewRow row = _gridView.Rows[eventArgs.RowIndex];
                    V vo = (V)row.DataBoundItem;
                    if (vo.id != null) {
                        row.DefaultCellStyle.BackColor = WidgetUtils.LighterColor(ColorTranslator.FromHtml("#E86C10"), .8);
                    }
                }
            };
            _gridView.CellMouseLeave += (sender, eventArgs) => {
                if (eventArgs.RowIndex > -1) {
                    _gridView.Rows[eventArgs.RowIndex].DefaultCellStyle.BackColor = Color.Empty;
                }
            };
            _gridView.SelectionChanged += (sender, eventArgs) => {
                if (_gridView.Focused) {
                    DataGridViewSelectedRowCollection selectedRows = _gridView.SelectedRows;
                    for (int i = 0; i < selectedRows.Count; i++) {
                        DataGridViewRow row = selectedRows[i];
                        V vo = (V)row.DataBoundItem;
                        if (vo.id == null) {
                            row.Selected = false;
                        }
                    }
                }
            };
        }
        #endregion

        #region Override methods
        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
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
            int newPageInfoHeight = WidgetUtils.GridViewPageInfoHeight();
            if (newPageInfoHeight > 0) {
                _gridPageInfoPanel.Size = new(Width - Padding.Size.Width, newPageInfoHeight);
            }
            _gridView.Size = new(Width - Padding.Size.Width, Height - Padding.Size.Height - newPageInfoHeight);
        }
        #endregion
    }
}
