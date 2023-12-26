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
    public class CustomComboBox<T>: UserControl where T: class {
        private readonly int _collapseStep = 20; // How many pixels increase/decrease each interval
        private readonly int _collapseSpend = 30; // How many milliseconds will the collapse cost
        private readonly int _maxItemsShown = 5; // Maximum of shown items, will has scroll bar if greater than this number
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
        private Color _disabledBackColor;
        private Color? _borderColor;
        private Color? _borderColorError;
        private bool _isError;
        private FontStyle? _boxFontStyle;
        private readonly string _defaultLabel = "-请选择-";
        private bool _noItem;
        private Action _itemSelected;

        public new Color ForeColor {
            get => base.ForeColor;
            set {
                base.ForeColor = value;
                _selectButton.ForeColor = value;
            }
        }
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
        public Color? BorderColorError { get => _borderColorError; set => _borderColorError = value; }
        public bool IsError { 
            get => _isError; 
            set {
                _isError = value; 
                Invalidate();
            }
        }
        public event Action ItemSelected { 
            add => _itemSelected += value; 
            remove {
                if (_itemSelected.GetInvocationList().Contains(value)) {
                    _itemSelected -= value; 
                }
            }
        }

        public CustomComboBox() {
            Margin = new(0);
            _selectButton = new(this) {
                Parent = this,
                Label = _defaultLabel,
            };
            _noItem = true;
            _itemButtons = new() {
                new("-无-"),
            };
            // _itemSelected = new(OnItemSelected);
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
                            Parent = WidgetUtils.MainPanel.Parent,
                            BorderColor = this.BorderColor,
                            NeedsPadding = false,
                            Visible = false,
                        };
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
                // Add a default item at the top to provide a null option
                AddItem(_defaultLabel, null);
            }
            // Add new option button according to item
            _itemButtons.Add(new(itemName, obj, _selectButton) {
                ForeColor = this.ForeColor,
                BackColor = this.BackColor,
                ToggledColor = WidgetUtils.ChangeColor(BackColor, .925),
            });
            // Reset timer interval
            ResetInterval();
            // Return item index (from 0 to end, ignore first default button)
            return _itemButtons.Count - 2;
        }

        public void RemoveItem(int index) {
            if (_selectButton.SelectedItem == _itemButtons[index + 1]) {
                _selectButton.SelectedItem = _itemButtons[0];
                _selectButton.SelectedItem.SetToggle(true);
                _selectButton.Label = _selectButton.SelectedItem.Name;
            }
            _itemButtons.RemoveAt(index + 1);
            _selectButton.Invalidate();
        }

        public void SetDefault(int index) {
            _selectButton.SelectedItem = _itemButtons[index + 1];
            _selectButton.SelectedItem.SetToggle(true);
            _selectButton.Label = _selectButton.SelectedItem.Name;
        }

        public T? GetChosenItem() {
            return _selectButton.SelectedItem?.Object;
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
                    _itemsScrollPanel.InvokeResizing();
                    _itemsScrollPanel.Invalidate();
                }
            } else {
                if (_itemsScrollPanel.Height + _collapseStep >= _itemsScrollPanelHeight) {
                    _itemsScrollPanel.Height = _itemsScrollPanelHeight;
                    if (_itemsPanel != null) {
                        _itemsPanel.Visible = true;
                    }
                    _collapseTimer.Stop();
                } else {
                    _itemsScrollPanel.Height += _collapseStep;
                }
                _itemsScrollPanel.InvokeResizing();
                _itemsScrollPanel.Invalidate();
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

        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            InvokeResizing();
        }

        private void InvokeResizing() {
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
                _borderRect = new(0, 0, Width - 1, Height - 1);
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

        protected override void OnBackColorChanged(EventArgs e) {
            base.OnBackColorChanged(e);
            _selectButton.BackColor = BackColor;
            foreach (ComboBoxItem<T> itemButton in _itemButtons) {
                itemButton.BackColor = BackColor;
            }
        }

        protected void OnItemSelected() {}


        // Select button
        public class ComboBoxSelectButton<I>: CommonButton {
            private CustomComboBox<T> _parentControl;
            private bool _isCollapsed;
            private Image _iconExpand;
            private Image? _iconExpandShowing;
            private Image _iconCollapse;
            private Image? _iconCollapseShowing;
            private Image? _iconShowing;
            private Point _iconPosition;
            private ComboBoxItem<I>? _selectedItem;

            public bool IsCollapsed { get => _isCollapsed; set => _isCollapsed = value; }
            public ComboBoxItem<I>? SelectedItem { get => _selectedItem; set => _selectedItem = value; }

            public ComboBoxSelectButton(CustomComboBox<T> parentControl) {
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
                _iconPosition = new(Width - (int) (iconSide + (Width / 20)), (Height - iconSide) / 2);
            }

            protected override void ResizeTextLabel() {
                if (this.Label != null) {
                    this.Font = new Font(WidgetsConfigs.SystemFontFamily, (int) (Height * .4), FontStyle.Regular, GraphicsUnit.Pixel);
                    this.LabelX = (int) (Width / 20);
                    this.LabelY = (this.Height - this.Font.Height) / 2;
                }
            }

            protected override void PaintAfter(PaintEventArgs e) {
                base.PaintAfter(e);
                Graphics g = e.Graphics;
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

            public ItemsScrollPanel(CustomContentPanel contentPanel) : base(null, contentPanel) {}

            public override void InvokeResizing() {
                if (IsHandleCreated) {
                    base.InvokeResizing();
                    OuterPanel.Size = new(OuterPanel.Width - Padding.Size.Width, OuterPanel.Height - Padding.Size.Height);
                    VScrollBar.Height -= Padding.Size.Height;
                    // Create border rectangle if border color is not null
                    if (_borderColor != null) {
                        _borderRect = new(0, 0, Width - 1, Height - 1);
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

            public new string? Name {
                get => Label; 
                set {
                    Label = value;
                }
            }
            public I? Object { get => _object; set => _object = value; }
            public ComboBoxSelectButton<I>? SelectButton { get => _selectButton; }

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
                    _selectButton.Label = Name;
                    _selectButton.SelectedItem = this;
                    _selectButton.TriggerClick();
                }
            }

            protected override void ResizeTextLabel() {
                if (this.Label != null) {
                    this.Font = new Font(WidgetsConfigs.SystemFontFamily, (int) (Height * .4), FontStyle.Regular, GraphicsUnit.Pixel);
                    this.LabelX = (int) (Width / 20);
                    this.LabelY = (this.Height - this.Font.Height) / 2;
                }
            }
        }
    }
}
