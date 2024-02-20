using System.Reflection;
using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.DataGridViewRelateds;
using CustomLibrary.Panels;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.Attributes;
using OperationGuidance_new.ViewObjects.AbstractClasses;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class DataGridViewPanel<T>: CustomContentPanel where T : AVOBase {
        #region Feilds 
        private DataGridView _gridView;
        private Panel _blankPanel;
        private Panel _pageInfoPanel;
        private CustomContentPanel _pageInfoContentPanel;
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
        private List<T> _dataSource;
        #endregion

        #region Properties
        public DataGridView GridView { get => _gridView; }
        public int CurrentPage { 
            get => _currentPage; 
            set {
                _currentPage = value; 
                Paging(_currentPage, _pageSize);
            }
        }
        public int TotalPages { get => _totalPages; set => _totalPages = value; }
        public int PageSize {
            get => _pageSize;
            set {
                _pageSize = value;
                Paging(_currentPage, _pageSize);
            }
        }
        public List<T> DataSource {
            get => _dataSource;
            set {
                _dataSource = value;
                _totalPages = (int) Math.Ceiling(value.Count / (double) _pageSize);
                Paging(_currentPage, _pageSize);
            }
        }
        #endregion

        #region Constructors
        public DataGridViewPanel() {
            // Self properties
            AutoPadding = false;
            Padding = new(1);
            PenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER;
            // Data source
            _dataSource = new();
            // Page info
            _pageSize = 20;
            _currentPage = 1;
            _pageInfoPanel = new() {
                Parent = this,
                Margin = new(0),
                Padding = new(0),
                BackColor = WidgetUtils.LightColor(ColorTranslator.FromHtml("#E86C10"), .9),
            };
            _pageInfoContentPanel = new() {
                Parent = _pageInfoPanel,
                Margin = new(0),
                Padding = new(0),
                Dock = DockStyle.Right,
            };
            _dataCountInfo = new() {
                Parent = _pageInfoContentPanel,
                Margin = new(0),
                Padding = new(0),
                AutoSize = true,
            };
            _countPerPage = new() {
                Parent = _pageInfoContentPanel,
                Margin = new(0),
                Padding = new(0),
                AutoSize = true,
            };
            _first = new() {
                Parent = _pageInfoContentPanel,
                Icon = Properties.Resources.page_btn_backward_fast,
            };
            _first.Click += (sender, evnetArgs) => {
                if (_currentPage != 1) {
                    CurrentPage = 1;
                }
            };
            _backward = new() {
                Parent = _pageInfoContentPanel,
                Icon = Properties.Resources.page_btn_backward,
            };
            _backward.Click += (sender, evnetArgs) => {
                if (_currentPage > 1) {
                    CurrentPage--;
                }
            };
            _pageInfo = new() {
                Parent = _pageInfoContentPanel,
                Margin = new(0),
                Padding = new(0),
                AutoSize = true,
            };
            _forward = new() {
                Parent = _pageInfoContentPanel,
                Icon = Properties.Resources.page_btn_forward,
            };
            _forward.Click += (sender, evnetArgs) => {
                if (_currentPage < _totalPages) {
                    CurrentPage++;
                }
            };
            _last = new() {
                Parent = _pageInfoContentPanel,
                Icon = Properties.Resources.page_btn_forward_fast,
            };
            _last.Click += (sender, evnetArgs) => {
                if (_currentPage != _totalPages) {
                    CurrentPage = _totalPages;
                }
            };
            _jumpToText = new() {
                Parent = _pageInfoContentPanel,
                Margin = new(0),
                Padding = new(0),
                Text = "跳转至第",
                AutoSize = true,
            };
            _jumpToBox = new() {
                Parent = _pageInfoContentPanel,
                NumberOnly = true,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
            };
            _jumpToTextRight = new() {
                Parent = _pageInfoContentPanel,
                Margin = new(0),
                Padding = new(0),
                Text = "页",
                AutoSize = true,
            };
            // Data grid view
            InitializeGridView();
            Paging(1, _pageSize);
        }
        #endregion

        #region Initialization methods
        private void InitializeGridView() {
            _gridView = new() {
                Parent = this,
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
            _gridView.BringToFront();
            _gridView.ColumnHeadersDefaultCellStyle.BackColor = WidgetUtils.LightColor(ColorTranslator.FromHtml("#E86C10"), .15);
            _gridView.ColumnHeadersDefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#FEFEFE");
            _gridView.ColumnHeadersDefaultCellStyle.SelectionBackColor = _gridView.ColumnHeadersDefaultCellStyle.BackColor;
            _gridView.RowsDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#F0F0F0");
            _gridView.RowsDefaultCellStyle.SelectionBackColor = WidgetUtils.LightColor(ColorTranslator.FromHtml("#E86C10"), .8);
            _gridView.RowsDefaultCellStyle.SelectionForeColor = WidgetUtils.DarkenColor(ColorTranslator.FromHtml("#E86C10"), .7);
            _gridView.AlternatingRowsDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#FEFEFE");
            _gridView.BackgroundColor = WidgetUtils.LightColor(ColorTranslator.FromHtml("#E86C10"), .975);
            // Initialize column headers
            InitializeColumnHeaders();
            InitializeEventBindings();
            InitializeOthers();
        }
        private void InitializeColumnHeaders() {
            DataGridViewColumn[] columnRange = {};
            Type type = typeof(T);
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
            // _gridView.VirtualMode = true;
            _gridView.Columns.AddRange(columnRange);
            _gridView.Columns[0].Frozen = true;
        }
        private void InitializeEventBindings() {
            HashSet<int> columnsPainted = new();
            HashSet<int> rowsPainted = new();
            _gridView.CellPainting += (sender, eventArgs) => {
                // Resize and Relocate ToggleButtonColumns
                if (eventArgs.ColumnIndex > -1 && eventArgs.RowIndex > -1) {
                    DataGridViewColumn column = _gridView.Columns[eventArgs.ColumnIndex];
                    DataGridViewRow row = _gridView.Rows[eventArgs.RowIndex];
                    if (column.Frozen) {
                        // Forzen columns have different color
                        if (eventArgs.RowIndex % 2 != 0) {
                            // Alternating rows
                            row.Cells[eventArgs.ColumnIndex].Style.BackColor = WidgetUtils.DarkenColor(_gridView.AlternatingRowsDefaultCellStyle.BackColor, .1);
                        } else {
                            row.Cells[eventArgs.ColumnIndex].Style.BackColor = WidgetUtils.DarkenColor(_gridView.RowsDefaultCellStyle.BackColor, .1);
                        }
                    }
                    if (column is DataGridViewToggleButtonColumn) {
                        // DataGridViewRow row = _gridView.Rows[eventArgs.RowIndex];
                        T vo = (T) row.DataBoundItem;
                        // If vo'id is null, then this row is just a blank row for displaying
                        if (vo != null && vo.id != null && row.Cells[column.Index] is DataGridViewToggleButtonCell cell && cell.Value != null) {
                            // Set toggle state to button in case they're not matched
                            if (cell.ToggleButton.Checked != (bool) cell.Value) {
                                cell.ToggleButton.Checked = (bool) cell.Value;
                            }
                            // Show cell in case it's Hiden
                            if (!cell.ToggleButtonParentPanel.Visible) {
                                cell.ToggleButtonParentPanel.Visible = true;
                            }
                            // Resize
                            Size cellSize = cell.Size;
                            int panelHeight = (int) (cellSize.Height * .65);
                            int panelWidth = panelHeight * 3;
                            if (cell.ToggleButtonParentPanel.Size != cellSize) {
                                cell.ToggleButtonParentPanel.Size = new(panelWidth, panelHeight);
                            }
                            // Relocate
                            DataGridViewContentAlignment headerAlignment = DataGridViewContentAlignment.NotSet;
                            if (column.HeaderCell.Style.Alignment != DataGridViewContentAlignment.NotSet) {
                                headerAlignment = column.HeaderCell.Style.Alignment;
                            } else if (_gridView.ColumnHeadersDefaultCellStyle.Alignment != DataGridViewContentAlignment.NotSet) {
                                headerAlignment = _gridView.ColumnHeadersDefaultCellStyle.Alignment;
                            }
                            Point cellPoint = _gridView.GetCellDisplayRectangle(eventArgs.ColumnIndex, eventArgs.RowIndex, true).Location;
                            float paddingRatio = .05F;
                            switch (headerAlignment) {
                                case DataGridViewContentAlignment.TopLeft:
                                    cellPoint.X += (int) (column.Width * paddingRatio);
                                    break;
                                default:
                                case DataGridViewContentAlignment.MiddleLeft:
                                    cellPoint.X += (int) (column.Width * paddingRatio);
                                    cellPoint.Y += (cellSize.Height - panelHeight) / 2;
                                    break;
                                case DataGridViewContentAlignment.BottomLeft:
                                    cellPoint.X += (int) (column.Width * paddingRatio);
                                    cellPoint.Y += cellSize.Height - panelHeight;
                                    break;
                                case DataGridViewContentAlignment.TopRight:
                                    cellPoint.X += cellSize.Width - panelWidth - (int) (column.Width * paddingRatio);
                                    break;
                                case DataGridViewContentAlignment.MiddleRight:
                                    cellPoint.X += (cellSize.Width - panelWidth) / 2 - (int) (column.Width * paddingRatio);
                                    cellPoint.Y += (cellSize.Height - panelHeight) / 2;
                                    break;
                                case DataGridViewContentAlignment.BottomRight:
                                    cellPoint.X += cellSize.Width - panelWidth - (int) (column.Width * paddingRatio);
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
                            if (cell.ToggleButtonParentPanel.Location != cellPoint) {
                                cell.ToggleButtonParentPanel.Location = cellPoint;
                            }
                        }
                    }
                }
                columnsPainted.Add(eventArgs.ColumnIndex);
                rowsPainted.Add(eventArgs.RowIndex);
            };
            Tuple<int, int> cellDirectionDown = new(-1, -1);
            Tuple<int, int> cellDirectionUp = new(-1, -1);
            bool rowChanged = false;
            bool columnChanged = false;
            _gridView.CellMouseDown += (sender, eventArgs) => locateCellClick(ref cellDirectionDown, eventArgs.RowIndex, eventArgs.ColumnIndex);
            _gridView.CellMouseUp += (sender, eventArgs) => locateCellClick(ref cellDirectionUp, eventArgs.RowIndex, eventArgs.ColumnIndex);
            void locateCellClick(ref Tuple<int, int> direction, int rowIndex, int columnIndex) {
                if (direction.Item1 == -1 && direction.Item2 == -1) {
                    rowChanged = true;
                    columnChanged = true;
                } else {
                    if (direction.Item1 != rowIndex) {
                        rowChanged = true;
                    }
                    if (direction.Item2 != columnIndex) {
                        columnChanged = true;
                    }
                }
                direction = new(rowIndex, columnIndex);
            }
            _gridView.Paint += (sender, eventArgs) => {
                if (columnsPainted.Count > 1) {
                    foreach (DataGridViewColumn column in _gridView.Columns) {
                        if (column is DataGridViewToggleButtonColumn && !columnsPainted.Contains(column.Index)) {
                            foreach (DataGridViewRow row in _gridView.Rows) {
                                if (row.Cells[column.Index] is DataGridViewToggleButtonCell cell) {
                                    cell.ToggleButtonParentPanel.Visible = false;
                                }
                            }
                        }
                    }
                    columnsPainted.Clear();
                }
                if (rowsPainted.Count > 1 && !(rowChanged && _gridView.SelectedRows.Count == 1) && !(!rowChanged && columnChanged)) {
                    foreach (DataGridViewRow row in _gridView.Rows) {
                        if (!rowsPainted.Contains(row.Index)) {
                            foreach (DataGridViewColumn column in _gridView.Columns) {
                                if (column is DataGridViewToggleButtonColumn) {
                                    if (row.Cells[column.Index] is DataGridViewToggleButtonCell cell) {
                                        cell.ToggleButtonParentPanel.Visible = false;
                                    }
                                }
                            }
                        }
                    }
                    rowsPainted.Clear();
                }
                if (_blankPanel != null) {
                    VScrollBar vScrollBar = _gridView.Controls.OfType<VScrollBar>().First();
                    HScrollBar hScrollBar = _gridView.Controls.OfType<HScrollBar>().First();
                    if (vScrollBar.Visible && hScrollBar.Visible) {
                        _blankPanel.Visible = true;
                        _blankPanel.Size = new(vScrollBar.Width, _gridView.Height - vScrollBar.Height);
                        _blankPanel.Location = new(vScrollBar.Location.X, vScrollBar.Height);
                        _blankPanel.BringToFront();
                    } else {
                        _blankPanel.Visible = false;
                    }
                }
                rowChanged = false;
                columnChanged = false;
            };
            _gridView.CellValueChanged += (sender, eventArgs) => {
                DataGridViewCell cell = _gridView.Rows[eventArgs.RowIndex].Cells[eventArgs.ColumnIndex];
                Console.WriteLine("Value change to: " + cell.Value);
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
                    T vo = (T) row.DataBoundItem;
                    if (vo.id != null) {
                        row.DefaultCellStyle.BackColor = WidgetUtils.LightColor(ColorTranslator.FromHtml("#E86C10"), .9);
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
                    foreach (DataGridViewRow row in selectedRows) {
                        T vo = (T) row.DataBoundItem;
                        if (vo.id == null) {
                            row.Selected = false;
                        }
                    }
                }
            };
        }
        private void InitializeOthers() {
            _blankPanel = new() {
                Parent = _gridView,
                Margin = new(0),
                Padding = new(0),
                BackColor = _gridView.BackgroundColor,
            };
        }
        #endregion

        #region Reusable methods
        private void ClearAllToggleButtonCells() {
            foreach (DataGridViewRow row in _gridView.Rows) {
                foreach (DataGridViewCell cell in row.Cells) {
                    if (cell is DataGridViewToggleButtonCell tbCell) {
                        tbCell.ToggleButtonParentPanel.Dispose();
                    }
                }
            }
        }
        private void ResetPageInfo() {
            _countPerPage.Text = $"{_pageSize} 条/页";
            _pageInfo.Text = $"{_currentPage}/{_totalPages}";
            _dataCountInfo.Text = $"共 {_dataSource.Count} 条";
            _pageInfo.Text = $"{_currentPage}/{_totalPages}";
        }
        private void ResizePageInfoContent(Label label, int newPageInfoHeight) {
            label.Font = new(WidgetsConfigs.SystemFontFamily, newPageInfoHeight * .5F, FontStyle.Regular, GraphicsUnit.Pixel);
            label.Margin = new(0, (newPageInfoHeight - label.Height) / 2, 0, 0);
        }
        private void Paging(int currentPage, int pageSize) {
            ClearAllToggleButtonCells();
            Task.Run(() => {
                BeginInvoke(() => {
                    if (IsHandleCreated) {
                        BindingSource bindingSource = new();
                        if (_dataSource.Count > 0) {
                            bindingSource.DataSource = _dataSource.Skip((currentPage - 1) * pageSize).Take(pageSize);
                        } else {
                            bindingSource.DataSource = null;
                        }
                        LoadDataAsync(bindingSource);
                    }
                });
            });
        }
        private void LoadDataAsync(BindingSource bindingSource) {
            if (IsHandleCreated) {
                Task.Run(() => {
                    BeginInvoke(() => {
                        _gridView.DataSource = bindingSource;
                        ResetPageInfo();
                        ResizeChildren();
                    });
                });
            }
        }
        #endregion

        #region Override methods
        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            int columnMaxWidth = WidgetUtils.GridViewContentColumnMaxWidth();
            foreach (DataGridViewColumn column in _gridView.Columns) {
                if (column.Width == columnMaxWidth) {
                    continue;
                } else if (column.Width > columnMaxWidth) {
                    if (column.AutoSizeMode != DataGridViewAutoSizeColumnMode.None) {
                        column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    }
                    column.Width = columnMaxWidth;
                } else if (column.AutoSizeMode == DataGridViewAutoSizeColumnMode.None) {
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                }
            }
            // Grid header height
            int newHeaderHeight = WidgetUtils.GridViewHeaderRowHeight();
            if (newHeaderHeight >= 4) {
                _gridView.ColumnHeadersHeight = newHeaderHeight;
                _gridView.ColumnHeadersDefaultCellStyle.Font = new(WidgetsConfigs.SystemFontFamily, newHeaderHeight * .45F, FontStyle.Regular, GraphicsUnit.Pixel);
                // Check if any image column exists
                foreach (DataGridViewColumn column in _gridView.Columns) {
                    if (column is DataGridViewImageColumn imageColumn) {
                        // TODO
                        System.Console.WriteLine("Need to resize image column here");
                    }
                }
            }
            // Grid content height
            int newContentHeight = WidgetUtils.GridViewContentRowHeight();
            if (newContentHeight >= 4) {
                foreach (DataGridViewRow row in _gridView.Rows) {
                    row.Height = newContentHeight;
                }
                _gridView.RowsDefaultCellStyle.Font = new(WidgetsConfigs.SystemFontFamily, newContentHeight * .45F, FontStyle.Regular, GraphicsUnit.Pixel);
            }
            // Page info panel height
            int newPageInfoHeight = WidgetUtils.GridViewPageInfoHeight();
            if (newPageInfoHeight > 0) {
                // Whole page info content panel size
                _pageInfoPanel.Size = new(Width - Padding.Size.Width, newPageInfoHeight);
                // Calculate every part of page info
                // All labels
                ResizePageInfoContent(_dataCountInfo, newPageInfoHeight);
                ResizePageInfoContent(_countPerPage, newPageInfoHeight);
                ResizePageInfoContent(_pageInfo, newPageInfoHeight);
                ResizePageInfoContent(_jumpToText, newPageInfoHeight);
                ResizePageInfoContent(_jumpToTextRight, newPageInfoHeight);
                // All buttons
                int buttonSide = (int) (newPageInfoHeight * .7);
                _first.Size = new(buttonSide, buttonSide);
                _forward.Size = new(buttonSide, buttonSide);
                _backward.Size = new(buttonSide, buttonSide);
                _last.Size = new(buttonSide, buttonSide);
                int buttonOuterHMargin = (int) (buttonSide * .8);
                int buttonInnerHMargin = (int) (buttonSide * .35);
                int buttonMarginTop = (newPageInfoHeight - buttonSide) / 2;
                _first.Margin = new(buttonOuterHMargin, buttonMarginTop, 0, 0);
                _forward.Margin = new(buttonInnerHMargin, buttonMarginTop, buttonInnerHMargin, 0);
                _backward.Margin = new(buttonInnerHMargin, buttonMarginTop, buttonInnerHMargin, 0);
                _last.Margin = new(0, buttonMarginTop, buttonOuterHMargin, 0);
                // Text box
                _jumpToBox.Size = new((int) (newPageInfoHeight * 1.8), (int) (newPageInfoHeight * .95));
                _jumpToBox.Margin = new(0, (newPageInfoHeight - _jumpToBox.Height) / 2, 0, 0);
                // All part width
                int sumWidth = 0;
                foreach (Control control in _pageInfoContentPanel.Controls) {
                    sumWidth += control.Width;
                    sumWidth += control.Margin.Size.Width;
                }
                _pageInfoContentPanel.Size = new(sumWidth, newPageInfoHeight);
            }
            // Grid size
            _gridView.Size = new(Width - Padding.Size.Width, Height - Padding.Size.Height - newPageInfoHeight);
        }
        #endregion
    }
}
