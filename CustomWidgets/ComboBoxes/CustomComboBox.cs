using System.ComponentModel;
using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Events;
using CustomLibrary.Panels;
using CustomLibrary.Panels.BaseClasses;
using CustomLibrary.Utils;
using Timer = System.Windows.Forms.Timer;

namespace CustomLibrary.ComboBoxs {
    [DesignerCategory("Code")] // This makes it directly open the code window except design mode window
    public class CustomComboBox<T>: UserControl {
        #region Fields
        private readonly int _collapseStep = 20; // How many pixels increase/decrease each interval
        private readonly int _collapseSpend = 30; // How many milliseconds will the collapse cost
        private readonly int _maxItemsShown = 5; // Maximum of shown items, will has scroll bar if greater than this number
        private readonly int _borderThickness = 1;
        private ComboBoxSelectButton<T> _selectButton;
        private ItemsScrollPanel? _itemsScrollPanel;
        private ItemsPanel? _itemsPanel;
        private int _itemsPanelHeight;
        private int _itemsScrollPanelHeight;
        private List<ComboBoxItem<T>> _itemButtons;
        private int _itemHeight;
        private Timer _collapseTimer;
        private Rectangle _borderRect;
        private Color _backColorSaved;
        private Color _independentBackColor;
        private Color? _disabledBackColor;
        private Color? _borderColor;
        private Color? _borderColorError;
        private bool _isError;
        private FontStyle? _boxFontStyle;
        private readonly string _defaultLabel = "-请选择-";
        private bool _needDefaultLabel;
        private bool _noItem;
        private Action _itemSelected;
        #endregion

        #region Properties
        public new bool Enabled {
            get => base.Enabled;
            set {
                base.Enabled = value;
                _selectButton.Enabled = value;
            }
        }
        public bool NeedDefaultLabel { 
            get => _needDefaultLabel; 
            set {
                _needDefaultLabel = value; 
                if (value) {
                    AddDefault();
                } else {
                    RemoveDefault();
                }
            }
        }
        public new Color BackColor {
            get => _independentBackColor;
            set {
                _independentBackColor = value;
                // Don't use OnBackColorChanged for bellow because if Disable will Change BackColor automatically 
                _selectButton.BackColor = value;
                foreach (ComboBoxItem<T> itemButton in _itemButtons) {
                    itemButton.BackColor = value;
                }
                _backColorSaved = value;
                _disabledBackColor = WidgetUtils.DarkerColor(value, .1);
                _selectButton.DisabledBackColor = _disabledBackColor;
                }
        }
        public new Color ForeColor {
            get => base.ForeColor;
            set {
                base.ForeColor = value;
                _selectButton.ForeColor = value;
            }
        }
        public Color? DisabledBackColor { get => _disabledBackColor; set => _disabledBackColor = value; }
        public Color? BorderColor {
            get => this._borderColor;
            set {
                this._borderColor = value;
                if (value != null) {
                    Padding newP = Padding;
                    newP.Left += _borderThickness;
                    newP.Right += _borderThickness;
                    newP.Top += _borderThickness;
                    newP.Bottom += _borderThickness;
                    Padding = newP;
                } else {
                    Padding newP = Padding;
                    newP.Left -= _borderThickness;
                    newP.Right -= _borderThickness;
                    newP.Top -= _borderThickness;
                    newP.Bottom -= _borderThickness;
                    Padding = newP;

                }
            }
        }
        public Color? BorderColorError { get => _borderColorError; set => _borderColorError = value; }
        public bool IsError { 
            get => _isError; 
            set {
                _isError = value; 
                Invalidate();
            }
        }
        public bool ShowRealValue { get => _selectButton.ShowRealValue; set => _selectButton.ShowRealValue = value; }
        public List<T?> Items { 
            get {
                List<T?> items = new();
                foreach (ComboBoxItem<T> item in _itemButtons) {
                    if (item.Label == _defaultLabel) {
                        continue;
                    }
                    items.Add(item.Object);
                } 
                return items;
            }
        }
        public T? Value { get => GetChosenValue(); }
        #endregion

        #region Events
        public event Action ItemSelected { 
            add => _itemSelected += value; 
            remove {
                if (_itemSelected.GetInvocationList().Contains(value)) {
                    _itemSelected -= value; 
                }
            }
        }
        #endregion

        #region Constructors
        public CustomComboBox() {
            Margin = new(0);
            _needDefaultLabel = true;
            _selectButton = new(this) {
                Parent = this,
                Label = _defaultLabel,
            };
            _noItem = true;
            _itemButtons = new() {
                new("-无-"),
            };
            _itemSelected += OnItemSelected;

            _collapseTimer = new();
            _collapseTimer.Tick += TimerTick;
            _selectButton.Click += (sender, eventArgs) => {
                if (!_selectButton.IsCollapsed) {
                    if (_itemsScrollPanel == null || _itemsScrollPanel.IsDisposed) {
                        _itemsPanel = new() {
                            Margin = new(0), 
                            FlowDirection = FlowDirection.TopDown,
                            BackColor = this.BackColor,
                        };
                        _itemsPanel.NewHeight = _itemHeight * _itemButtons.Count;
                        _itemsPanel.SizeChanged += (sender, eventArgs) => {
                            if (_itemsScrollPanel != null) {
                                foreach (ComboBoxItem<T> item in _itemButtons) {
                                    item.Size = new(_itemsPanel.Width - _itemsScrollPanel.Padding.Size.Width, _itemHeight);
                                }
                            }
                        };
                        Point point = PointToScreen(Point.Empty);
                        _itemsScrollPanel = new(_itemsPanel) {
                            Margin = new(0), 
                            Size = new(Width, 0),
                            BorderColor = this.BorderColor,
                            NeedsPadding = false,
                            Visible = false,
                        };
                        if (EventFuncs.CurrentPopUpForm != null) {
                            _itemsScrollPanel.Parent = EventFuncs.CurrentPopUpForm;
                        } else {
                            _itemsScrollPanel.Parent = WidgetUtils.MainPanel.Parent;
                        }
                        _itemsScrollPanel.BringToFront();
                        _itemsScrollPanel.Location = _itemsScrollPanel.PointToClient(new(point.X, point.Y + Height));
                        _itemsScrollPanel.Visible = true;
                    }
                    if (_itemsPanel != null && !_itemsPanel.IsDisposed && _itemsPanel.Controls.Count == 0) {
                        foreach (ComboBoxItem<T> item in _itemButtons) {
                            _itemsPanel.Controls.Add(item);
                            if (item.Toggled) {
                                item.BringToFront();
                            }
                        }
                    }
                } else {
                    _itemSelected();
                }
                if (_itemsPanel != null) {
                    // This is to prevent VScrollBar from showing while timer is running
                    _itemsPanel.Visible = false;
                }
                
                _collapseTimer.Start();
            };
            EventFuncs.AddClickAction(ItemsPanelAutoClose);
        }
        #endregion

        private void ResetInterval() {
            if (_itemsPanelHeight != 0) {
                _collapseTimer.Interval = (int) (_collapseSpend / ((decimal) _itemsPanelHeight / _collapseStep));
            }
        }

        // Add item
        public int AddItem(string itemName, T? obj) {
            // If there is no any item
            if (_noItem) {
                _noItem = false;
                // Clear <none> item first
                _itemButtons.Clear();
                if (_needDefaultLabel) {
                    // Add a default item at the top to provide a null option
                    AddDefault();
                }
            }
            // Add new option button according to item
            _itemButtons.Add(new(itemName, obj, _selectButton) {
                ForeColor = this.ForeColor,
                BackColor = this.BackColor,
                ToggledColor = WidgetUtils.LighterColor(BackColor, .925),
            });
            // Reset timer interval
            ResetInterval();
            // Return item index (from 0 to end, ignore first default button)
            return _itemButtons.Count - (_needDefaultLabel ? 2 : 1);
        }
        // Add default
        public void AddDefault() {
            if (_itemButtons.Count == 0 || _itemButtons[0].Label != _defaultLabel) {
                _itemButtons.Insert(0, new(_defaultLabel, default(T), _selectButton) {
                    ForeColor = this.ForeColor,
                    BackColor = this.BackColor,
                    ToggledColor = WidgetUtils.LighterColor(BackColor, .925),
                });
                // Reset timer interval
                ResetInterval();
            }
        }
        // Remove default
        public void RemoveDefault() {
            if (_itemButtons.Count != 0 && _itemButtons[0].Label == _defaultLabel) {
                _itemButtons.RemoveAt(0);
                // Reset timer interval
                ResetInterval();
            }
        }

        public void RemoveItem(int index) {
            int trueIndex = _needDefaultLabel ? index + 1 : index;
            if (_selectButton.SelectedItem == _itemButtons[trueIndex]) {
                _selectButton.SelectedItem = _itemButtons[0];
                _selectButton.SelectedItem.SetToggle(true);
            }
            _itemButtons.RemoveAt(trueIndex);
            _selectButton.Invalidate();
        }
        
        public void Reset() {
            if (_selectButton.SelectedItem != null) {
                _selectButton.SelectedItem.SetToggle(false);
            }
            _itemButtons[0].SetToggle(true);
            _selectButton.SelectedItem = null;
        }

        public void SetCurrent(int index) {
            int trueIndex = _needDefaultLabel ? index + 1 : index;
            if (_selectButton.SelectedItem != null) {
                _selectButton.SelectedItem.SetToggle(false);
            }
            _selectButton.SelectedItem = _itemButtons[trueIndex];
            _selectButton.SelectedItem.SetToggle(true);
        }

        public T? GetChosenValue() {
            T? t = default(T);
            if (_selectButton.SelectedItem != null) {
                t = _selectButton.SelectedItem.Object;
            }
            return t;
        }

        public ComboBoxItem<T>? GetChosenItem() {
            return _selectButton.SelectedItem;
        }

        private void TimerTick(object? sender, EventArgs eventArgs) {
            if (_itemsScrollPanel == null) {
                _collapseTimer.Stop();
                return;
            }
            if (_selectButton.IsCollapsed) {
                if (_itemsScrollPanel.Height - _collapseStep <= 0) {
                    _itemsScrollPanel.Height = 0;
                    if (_itemsPanel != null) {
                        _itemsPanel.Controls.Clear();
                    }
                    _itemsScrollPanel.Dispose();
                    _itemsScrollPanel = null;
                    _collapseTimer.Stop();
                } else {
                    _itemsScrollPanel.Height -= _collapseStep;
                    _itemsScrollPanel.ResizeChildren(eventArgs);
                    _itemsScrollPanel.Invalidate();
                }
            } else {
                if (_itemsScrollPanel.Height + _collapseStep >= _itemsScrollPanelHeight) {
                    _itemsScrollPanel.Height = _itemsScrollPanelHeight;
                    if (_itemsPanel != null) {
                        _itemsPanel.Visible = true;
                    }
                    // Do this method
                    ResetWidthOfItemScrollPanelByItemProperWidth();
                    _collapseTimer.Stop();
                } else {
                    _itemsScrollPanel.Height += _collapseStep;
                }
                _itemsScrollPanel.ResizeChildren(eventArgs);
                _itemsScrollPanel.Invalidate();
            }
        }

        private void ResetWidthOfItemScrollPanelByItemProperWidth() {
            if (_itemsScrollPanel != null) {
                _itemsScrollPanel.ResizeChildren();
                int properWidth = 0;
                foreach (ComboBoxItem<T> item in _itemButtons) {
                    if (item.ProperWidth > properWidth) {
                        properWidth = item.ProperWidth;
                    }
                }
                if (properWidth > 0) {
                    if (_itemsScrollPanel.VScrollBar.Visible) {
                        _itemsScrollPanel.Width = properWidth + _itemsScrollPanel.VScrollBar.Width;
                    } else {
                        _itemsScrollPanel.Width = properWidth;
                    }
                }
            }
        }

        private void ItemsPanelAutoClose() {
            if (_itemsScrollPanel != null) {
                Point realTimePoint = EventFuncs.RealTimePoint;
                if (!new Rectangle(_itemsScrollPanel.PointToScreen(Point.Empty), _itemsScrollPanel.Size).Contains(realTimePoint) 
                        && !new Rectangle(_selectButton.PointToScreen(Point.Empty), _selectButton.Size).Contains(realTimePoint)) {
                    if (!_selectButton.IsCollapsed) {
                        _selectButton.IsCollapsed = true;
                    }
                    if (_itemsPanel != null) {
                        _itemsPanel.Controls.Clear();
                    }
                    _itemsScrollPanel.Dispose();
                    _itemsScrollPanel = null;
                }
            }
        }

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            SizeChanged += ResizeChildren;
        }

        public void ResizeChildren() => ResizeChildren(this, EventArgs.Empty);
        public void ResizeChildren(object? sender, EventArgs eventArgs) {
            _itemHeight = (int) (Height * .95);
            if (_itemButtons.Count >= _maxItemsShown) {
                _itemsPanelHeight = _itemHeight * _maxItemsShown;
            } else {
                _itemsPanelHeight = _itemHeight * _itemButtons.Count;
            }
            _itemsScrollPanelHeight = _itemsPanelHeight + Padding.Size.Height;
            ResetInterval();

            _selectButton.Size = new(Width - Padding.Size.Width, Height - Padding.Size.Height);
            _selectButton.Location = new(Padding.Left, Padding.Top);

            // Create border rectangle if border color is not null
            if (_borderColor != null) {
                _borderRect = new(0, 0, Width - _borderThickness, Height - _borderThickness);
            }
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            // Draw border if border color is not null
            if (_borderColorError != null && _isError) {
                g.DrawRectangle(new(_borderColorError.Value, 1), _borderRect);
            } else if (_borderColor != null) {
                g.DrawRectangle(new(_borderColor.Value, 1), _borderRect);
            }
        }

        protected override void OnForeColorChanged(EventArgs e) {
            base.OnForeColorChanged(e);
            _selectButton.ForeColor = ForeColor;
            foreach (ComboBoxItem<T> itemButton in _itemButtons) {
                itemButton.ForeColor = ForeColor;
            }
        }

        protected void OnItemSelected() {}

        // Select button
        public class ComboBoxSelectButton<I>: CommonButton {
            private CustomComboBox<I> _parentControl;
            private bool _isCollapsed;
            private Image _iconExpand;
            private Image? _iconExpandShowing;
            private Image _iconCollapse;
            private Image? _iconCollapseShowing;
            private Image? _iconShowing;
            private Point _iconPosition;
            private ComboBoxItem<I>? _selectedItem;
            private bool _showRealValue;
            private Color? _disabledBackColor;

            public bool IsCollapsed { get => _isCollapsed; set => _isCollapsed = value; }
            public ComboBoxItem<I>? SelectedItem { 
                get => _selectedItem; 
                set {
                    _selectedItem = value; 
                    RefreshLabel();
                }
            }
            public bool ShowRealValue { 
                get => _showRealValue; 
                set {
                    _showRealValue = value; 
                    RefreshLabel();
                }
            }
            public Color? DisabledBackColor { get => _disabledBackColor; set => _disabledBackColor = value; }

            public ComboBoxSelectButton(CustomComboBox<I> parentControl) {
                _parentControl = parentControl;
                BlockHoverUp = true;
                BlockHoverDown = true;
                _isCollapsed = true;
                _iconExpand = Resources.CustomResources.combo_expand;
                _iconCollapse = Resources.CustomResources.combo_collapse;
                _iconPosition = new(0, 0);
                // Need to register this first to make sure this 'Clicking' is fired before others
                Click += Clicking;
            }

            private void RefreshLabel() {
                if (_selectedItem != null) {
                    if (_showRealValue) {
                        I? obj = _selectedItem.Object;
                        if (obj == null || obj.Equals(default(I))) {
                            Label = _selectedItem?.Name;
                        } else {
                            Label = obj.ToString();
                        }
                    } else {
                        Label = _selectedItem.Name;
                    }
                    ResizeTextLabel();
                }
            }

            protected override void OnHandleCreated(EventArgs e) {
                // Register 'Resizing' after handle created to make sure every widget has been initialized
                SizeChanged += Resizing;
                Resizing(this, e);
                Invalidate();
            }

            private void Clicking(object? s, EventArgs e) {
                // Switch status
                _isCollapsed = !_isCollapsed;
                // Change icon depends on status of Collapsed
                SetIcon();
                // Repaint after icon chagned
                Invalidate();
            }

            private void SetIcon() {
                if (_isCollapsed) {
                    _iconShowing = _iconExpandShowing;
                } else {
                    _iconShowing = _iconCollapseShowing;
                }
            }

            private void Resizing(object? s, EventArgs e) {
                // Resize and relocate icon
                int iconSide = (int) (Height * .5);
                _iconCollapseShowing = WidgetUtils.ResizeImageWithoutLosingQuality(_iconCollapse, iconSide, iconSide);
                _iconExpandShowing = WidgetUtils.ResizeImageWithoutLosingQuality(_iconExpand, iconSide, iconSide);
                SetIcon();
                _iconPosition = new(Width - (int) (iconSide + (Height / 3.5)), (Height - iconSide) / 2);
            }

            protected override void ResizeTextLabel() {
                if (this.Label != null) {
                    this.Font = new Font(WidgetsConfigs.SystemFontFamily, Height * .53F, FontStyle.Regular, GraphicsUnit.Pixel);
                    this.LabelX = (int) (Height / 3.5);
                    this.LabelY = (this.Height - this.Font.Height) / 2;
                }
            }

            protected override void PaintAfter(PaintEventArgs e) {
                Graphics g = e.Graphics;
                if (!Enabled && _disabledBackColor != null) {
                    g.Clear(_disabledBackColor.Value);
                }
                base.PaintAfter(e);
                if (_iconShowing != null) {
                    g.DrawImage(_iconShowing, _iconPosition);
                }
            }

            public void TriggerClick() {
                OnClick(EventArgs.Empty);
            }
        }

        // Items panel
        public class ItemsPanel: CustomContentPanel {
            public ItemsPanel() => DoubleBuffered = true;

            public override bool CheckNeedsScrollBar(int parentNewHeight) {
                if (!Visible) {
                    return false;
                }
                return this.NewHeight > parentNewHeight;
            }
        }

        // Items scroll panel
        public class ItemsScrollPanel: CustomVScrollingContentPanel {
            private Color? _borderColor;
            private Rectangle _borderRect;

            public Color? BorderColor {
                get => this._borderColor;
                set {
                    this._borderColor = value;
                    if (value != null) {
                        Padding newP = Padding;
                        newP.Left += 1;
                        newP.Right += 1;
                        newP.Top += 1;
                        newP.Bottom += 1;
                        Padding = newP;
                    } else {
                        Padding newP = Padding;
                        newP.Left -= 1;
                        newP.Right -= 1;
                        newP.Top -= 1;
                        newP.Bottom -= 1;
                        Padding = newP;

                    }
                }
            }

            public ItemsScrollPanel(CustomContentPanel contentPanel) : base(null, contentPanel) => DoubleBuffered = true;

            protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
                if (IsHandleCreated) {
                    base.ResizeChildren(sender, eventArgs);
                    OuterPanel.Size = new(OuterPanel.Width - Padding.Size.Width, OuterPanel.Height - Padding.Size.Height);
                    VScrollBar.Height -= Padding.Size.Height;
                    // Create border rectangle if border color is not null
                    if (_borderColor != null) {
                        _borderRect = new(0, 0, Width - 1, Height - 1);
                    }
                    // Resize item buttons if width of item label grater than this.Width
                    foreach (Control control in Controls) {
                        if (control is ComboBoxItem<T>) {
                            control.Width = Width;
                        }
                    }
                }
            }

            protected override void OnPaint(PaintEventArgs e) {
                base.OnPaint(e);
                Graphics g = e.Graphics;
                // Draw border if border color is not null
                if (_borderColor != null) {
                    e.Graphics.DrawRectangle(new(_borderColor.Value, 1), _borderRect);
                }
            }
        }

        // Item
        public class ComboBoxItem<I>: CommonButton {
            private I? _object;
            private ComboBoxSelectButton<I>? _selectButton;
            private int _properWidth;

            public new string Name { get => Label != null ? Label : ""; set => Label = value; }
            public I? Object { get => _object; set => _object = value; }
            public ComboBoxSelectButton<I>? SelectButton { get => _selectButton; }
            public int ProperWidth { get => _properWidth; }

            public ComboBoxItem(string name) {
                Initialize(name);
            }

            private void Initialize(string name) {
                ToggledButton = true;
                GroupMode = true;
                BlockHoverUp = true;
                BlockHoverDown = true;
                Name = name;
            }

            public ComboBoxItem(string name, I? obj, ComboBoxSelectButton<I>? selectButton) {
                Initialize(name);
                _object = obj;
                _selectButton = selectButton;
            }

            protected override void OnClick(EventArgs e) {
                base.OnClick(e);
                if (_selectButton != null) {
                    if (_selectButton.SelectedItem != null) {
                        _selectButton.SelectedItem.SetToggle(false);
                    }
                    SetToggle(true);
                    _selectButton.SelectedItem = this;
                    _selectButton.TriggerClick();
                }
            }

            protected override void ResizeTextLabel() {
                if (this.Label != null) {
                    this.Font = new Font(WidgetsConfigs.SystemFontFamily, Height * .53F, FontStyle.Regular, GraphicsUnit.Pixel);
                    int hPadding = (int) (Height / 3.5);
                    using (Graphics g = CreateGraphics()) {
                        float labelWidth = g.MeasureString(Label, Font).Width;
                        if (labelWidth >= (Width * .975) - hPadding * 2) {
                            _properWidth = (int) (labelWidth * .975 + hPadding * 2);
                        }
                    }
                    this.LabelX = hPadding;
                    this.LabelY = (this.Height - this.Font.Height) / 2;
                }
            }
        }
    }
}
