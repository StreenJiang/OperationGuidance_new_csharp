using System.Reflection;
using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.DataGridViewRelateds;
using CustomLibrary.Panels;
using CustomLibrary.Utils;
using OperationGuidance_new.Attributes;
using OperationGuidance_new.ViewObjects.AbstractClasses;
using CustomLibrary.TextBoxes;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class DataGridViewPanel<T>: CustomContentPanel where T : AVOBase {
        #region Feilds 
        private Panel _gridViewPanel;
        private int _headerHeight = WidgetUtils.GridViewHeaderHeight();
        private int _rowsHeight = WidgetUtils.GridViewContentRowHeight();
        private int _pageHeight = WidgetUtils.GridViewPageInfoHeight();
        private float _columnsPaddingRatio = WidgetUtils.GridViewColumnsPaddingRatio();
        private HScrollBar? _hScrollBar;
        private VScrollBar? _vScrollBar;
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
        private Action<DataGridView>? _initializeColumnHeader;
        private List<T> _dataSource;
        #endregion

        #region Properties
        public DataGridView GridView { get => _gridView; }
        public bool AutoDown { get; set; } = false;
        public int HeaderHeight { get => _headerHeight; set => _headerHeight = value; }
        public int RowsHeight { get => _rowsHeight; set => _rowsHeight = value; }
        public int PageHeight { get => _pageHeight; set => _pageHeight = value; }
        public float ColumnsPaddingRatio { get => _columnsPaddingRatio; set => _columnsPaddingRatio = value; }
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
        public Action<DataGridView>? InitializeColumnHeader { get => _initializeColumnHeader; set => _initializeColumnHeader = value; }
        public List<T> DataSource {
            get => _dataSource;
            set {
                _dataSource = value;
                _totalPages = (int) Math.Ceiling(value.Count / (double) _pageSize);
                if (_totalPages == 0) {
                    _totalPages = 1;
                }
                Paging(_currentPage, _pageSize);
            }
        }
        #endregion

        #region Constructors
        public DataGridViewPanel(Action<DataGridView>? initializeColumnHeader = null) {
            _initializeColumnHeader = initializeColumnHeader;
            // Self properties
            AutoPadding = false;
            Padding = new(1);
            PenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER;
            // Data source
            _dataSource = new();
            // Data grid view
            InitializeGridView(initializeColumnHeader);
            // Page info
            InitializePagePanel();
            Paging(1, _pageSize);
        }
        #endregion

        #region Initialization methods
        private void InitializeGridView(Action<DataGridView>? initializeColumnHeader) {
            _gridViewPanel = new() {
                Parent = this,
                Margin = new(0),
            };
            _gridView = new() {
                Parent = _gridViewPanel,
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
            _gridView.ColumnHeadersDefaultCellStyle.BackColor = WidgetUtils.LightColor(ColorTranslator.FromHtml("#E86C10"), .15);
            _gridView.ColumnHeadersDefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#FEFEFE");
            _gridView.ColumnHeadersDefaultCellStyle.SelectionBackColor = _gridView.ColumnHeadersDefaultCellStyle.BackColor;
            _gridView.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _gridView.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _gridView.RowsDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#F0F0F0");
            _gridView.RowsDefaultCellStyle.SelectionBackColor = WidgetUtils.LightColor(ColorTranslator.FromHtml("#E86C10"), .8);
            _gridView.RowsDefaultCellStyle.SelectionForeColor = WidgetUtils.DarkenColor(ColorTranslator.FromHtml("#E86C10"), .7);
            _gridView.AlternatingRowsDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#FEFEFE");
            _gridView.BackgroundColor = WidgetUtils.LightColor(ColorTranslator.FromHtml("#E86C10"), .975);
            // Initialize column headers
            if (initializeColumnHeader != null) {
                initializeColumnHeader(_gridView);
            } else {
                InitializeColumnHeaders();
            }
            
            _gridView.MouseWheel += (sender, e) => {
                if (_vScrollBar != null && !_vScrollBar.IsDisposed && _vScrollBar.Visible) {
                    int realMaximum = this._vScrollBar.Maximum - this._vScrollBar.LargeChange + 1;
                    int currentValue = this._vScrollBar.Value;
                    if (e.Delta > 0) {
                        currentValue -= this._vScrollBar.SmallChange;
                    } else {
                        currentValue += this._vScrollBar.SmallChange;
                    }
                    if (currentValue < 0) {
                        currentValue = 0;
                    } else if (currentValue > realMaximum) {
                        currentValue = realMaximum;
                    }
                    this._vScrollBar.Value = currentValue;
                }
            };
            InitializeEventBindings();
            InitializeOthers();    
        }
        private void InitializeColumnHeaders() {
            DataGridViewColumn[] columnRange = {};
            List<PropertyInfo> props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
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
                // 不知道为啥自定义column的值设置的时候，这个offset会被重置为0，所以在这里手动设置一下
                if (_hScrollBar != null && _gridView.HorizontalScrollingOffset != _hScrollBar.Value) {
                    _gridView.HorizontalScrollingOffset = _hScrollBar.Value;
                }
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
            _gridView.Scroll += (sender, eventArgs) => {
                if (_vScrollBar != null && _vScrollBar.Visible && eventArgs.ScrollOrientation == ScrollOrientation.VerticalScroll) {
                    _vScrollBar.Value = eventArgs.NewValue;
                }
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
                    if (row.DataBoundItem != null) {
                        T vo = (T) row.DataBoundItem;
                        if (vo.id != null) {
                            row.DefaultCellStyle.BackColor = WidgetUtils.LightColor(ColorTranslator.FromHtml("#E86C10"), .9);
                        }
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
        private void InitializePagePanel() {
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
            _jumpToBox.Box.KeyUp += async (sender, eventArgs) => {
                if (eventArgs.KeyCode == Keys.Enter && !string.IsNullOrEmpty(_jumpToBox.Box.Text)) {
                    int page = int.Parse(_jumpToBox.Box.Text);
                    if (page >= 1 && page <= _totalPages) {
                        _currentPage = page;
                        Paging(page, _pageSize);
                        eventArgs.Handled = true;
                    } else {
                        _jumpToBox.IsError = true;
                        await Task.Delay(2000);
                        _jumpToBox.IsError = false;
                    }
                    _jumpToBox.Box.Text = "";
                }
            };
            _jumpToBox.Box.LostFocus += async (sender, eventArgs) => {
                if (!string.IsNullOrEmpty(_jumpToBox.Box.Text)) {
                    int page = int.Parse(_jumpToBox.Box.Text);
                    if (page >= 1 && page <= _totalPages) {
                        _currentPage = page;
                        Paging(page, _pageSize);
                    } else {
                        _jumpToBox.IsError = true;
                        await Task.Delay(2000);
                        _jumpToBox.IsError = false;
                    }
                    _jumpToBox.Box.Text = "";
                }
            };
            _jumpToTextRight = new() {
                Parent = _pageInfoContentPanel,
                Margin = new(0),
                Padding = new(0),
                Text = "页",
                AutoSize = true,
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
        public void ResetColumnHeaders() {
            if (_initializeColumnHeader != null) {
                _initializeColumnHeader(_gridView);
                Paging(_currentPage, _pageSize);
            }
        }
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
            System.Console.WriteLine($"========================================== Paging");
            if (IsHandleCreated) {
                BeginInvoke(new Action(ClearAllToggleButtonCells));
                BindingSource bindingSource = new();
                if (_dataSource.Count > 0) {
                    bindingSource.DataSource = _dataSource.Skip((currentPage - 1) * pageSize).Take(pageSize);
                } else {
                    bindingSource.DataSource = null;
                }
                BeginInvoke(new Action<BindingSource>(LoadDataAsync), bindingSource);
            }
        }
        private void LoadDataAsync(BindingSource bindingSource) {
            System.Console.WriteLine($"========================================== LoadDataAsync");
            if (IsHandleCreated) {
                _gridView.DataSource = bindingSource;
                BeginInvoke(new Action(() => {
                    ResetPageInfo();
                    ResizeChildren();
                }));
            }
        }
        #endregion

        #region Override methods
        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            _headerHeight = WidgetUtils.GridViewHeaderHeight();
            _rowsHeight = WidgetUtils.GridViewContentRowHeight();
            _pageHeight = WidgetUtils.GridViewPageInfoHeight();

            // Grid header height
            int newHeaderHeight = _headerHeight;
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
            int columnMaxWidth = WidgetUtils.GridViewContentColumnMaxWidth();
            int padding = (int) (newHeaderHeight * _columnsPaddingRatio);
            _gridView.ColumnHeadersDefaultCellStyle.Padding = new(padding, 0, padding, 0);
            _gridView.DefaultCellStyle.Padding = new(padding, 0, padding, 0);
            // Grid content height
            int newContentHeight = _rowsHeight;
            if (newContentHeight >= 4) {
                _gridView.RowTemplate.Height = newContentHeight;
                _gridView.RowsDefaultCellStyle.Font = new(WidgetsConfigs.SystemFontFamily, newContentHeight * .425F, FontStyle.Regular, GraphicsUnit.Pixel);
            }
            // Page info panel height
            int newPageInfoHeight = _pageHeight;
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
            _gridViewPanel.Size = new(Width - Padding.Size.Width, Height - Padding.Size.Height - newPageInfoHeight);
            int columnsWidth = 0;
            foreach (DataGridViewColumn column in _gridView.Columns) {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                columnsWidth += column.Width;
            }
            int rowsHeight = 0;
            foreach (DataGridViewRow row in _gridView.Rows) {
                rowsHeight += row.Height;
            }
            // Check for scroll bar
            int scrollBarThickness = WidgetUtils.ScrollBarThickness();
            if (columnsWidth > _gridViewPanel.Width) {
                if (_hScrollBar == null) {
                    _hScrollBar = new() {
                        Parent = _gridViewPanel,
                        Margin = new(0),
                    };
                }
                _hScrollBar.Size = new(_gridViewPanel.Width, scrollBarThickness);
                _hScrollBar.Location = new(0, _gridViewPanel.Height - scrollBarThickness);
                _hScrollBar.ValueChanged += (sender, eventArgs) => {
                    _gridView.HorizontalScrollingOffset = _hScrollBar.Value;
                };
                _hScrollBar.Show();
                WidgetUtils.CalculateScrollBar(_hScrollBar, _gridViewPanel.Width, columnsWidth);
            } else {
                _hScrollBar?.Hide();
            }
            if (rowsHeight > _gridViewPanel.Height - newHeaderHeight) {
                if (_vScrollBar == null) {
                    _vScrollBar = new() {
                        Parent = _gridViewPanel,
                        Margin = new(0),
                    };
                }
                _vScrollBar.Size = new(scrollBarThickness, _gridViewPanel.Height);
                _vScrollBar.Location = new(_gridViewPanel.Width - scrollBarThickness, 0);
                _vScrollBar.ValueChanged += (sender, eventArgs) => {
                    if (_vScrollBar.Value >= 0) {
                        _gridView.FirstDisplayedScrollingRowIndex = _vScrollBar.Value;
                    }
                };
                _vScrollBar.Show();
                _vScrollBar.Maximum = _gridView.RowCount;
                _vScrollBar.LargeChange = _gridView.DisplayedRowCount(true);
                _vScrollBar.SmallChange = 1;
                if (AutoDown) {
                    _gridView.FirstDisplayedScrollingRowIndex = _gridView.RowCount - _gridView.DisplayedRowCount(true) + 1;
                }
            } else {
                _vScrollBar?.Hide();
            }
            Size gridSize = _gridViewPanel.Size;
            if (_vScrollBar != null && _vScrollBar.Visible && _vScrollBar.Width > 0) {
                gridSize.Width -= _vScrollBar.Width;
            }
            if (_hScrollBar != null && _hScrollBar.Visible && _hScrollBar.Height > 0) {
                gridSize.Height -= _hScrollBar.Height;
            }
            _gridView.Size = gridSize;
            if (_vScrollBar != null && _vScrollBar.Visible && _hScrollBar != null && _hScrollBar.Visible){
                _hScrollBar.Width -= scrollBarThickness;
                _vScrollBar.Height -= scrollBarThickness;
                WidgetUtils.CalculateScrollBar(_hScrollBar, _gridView.Width, columnsWidth);
            }
            System.Console.WriteLine($"========================================== Resize done: Size = {Size}");
        }
        #endregion
    }
}
