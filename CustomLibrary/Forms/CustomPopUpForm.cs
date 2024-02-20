using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Events;
using CustomLibrary.Panels;
using CustomLibrary.Utils;

namespace CustomLibrary.Forms
{

    [System.ComponentModel.DesignerCategory("Code")] // This makes it directly open the code window except design mode window
    public class CustomPopUpForm: Form {
        private Form _popUpFormBackboard;
        private Color? _borderColor;
        private Rectangle? _borderRect;
        private readonly int _borderThickness = 1;
        // All containers
        private CustomContentPanel _outerPanel;
        private CustomContentPanel _titlePanel;
        private CustomContentPanel _contentPanel;
        private Panel _buttonsPanel;
        private CustomContentPanel _buttonsInnerPanel;
        
        // Outer panel
        // Title panel
        private string _title;
        private Font? _titleFont;
        private CloseButton _closeButton;
        // Content panel
        // Buttons panel
        private List<FunctionButton> _buttons;
        private HorizontalAlignment _buttonAlignment;

        // -- Properties --
        // Used in EventFuncs
        public Form BackForm { get => _popUpFormBackboard; set => _popUpFormBackboard = value; }
        public Color? BorderColor { 
            get => _borderColor; 
            set {
                _borderColor = value; 
                if (value != null) {
                    _outerPanel.Location = new(_borderThickness, _borderThickness);
                } else {
                    _outerPanel.Location = new(0, 0);
                }
            }
        }
        // Outer panel
        public CustomContentPanel OuterPanel { get => _outerPanel; set => _outerPanel = value; }
        // Title panel
        public CustomContentPanel TitlePanel { get => _titlePanel; set => _titlePanel = value; }
        public string Title { get => _title; set => _title = value; }
        public CloseButton CloseButton { get => _closeButton; set => _closeButton = value; }
        public bool HasTitleBar { get => TitlePanel.Visible; set => TitlePanel.Visible = value; }
        // Content panel
        public CustomContentPanel ContentPanel { get => _contentPanel; set => _contentPanel = value; }
        // Buttons panel
        public Panel ButtonsPanel { get => _buttonsPanel; set => _buttonsPanel = value; }
        public HorizontalAlignment ButtonAlignment {
            get => _buttonAlignment;
            set {
                _buttonAlignment = value;
                RelocateButtons();
            }
        }

        public CustomPopUpForm() : base() {
            Control mainParent = WidgetUtils.MainPanel.Parent;
             // Initialize self
            Owner = _popUpFormBackboard;
            StartPosition = FormStartPosition.Manual;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            // Initialize backboard
            _popUpFormBackboard = new() {
                Owner = (Form) mainParent,
                Size = mainParent.ClientSize,
                StartPosition = FormStartPosition.Manual,
                FormBorderStyle = FormBorderStyle.None,
                Opacity = .5D,
                BackColor = Color.Black,
                ShowInTaskbar = false
            };
            _popUpFormBackboard.Owner.LocationChanged += (sender, eventArgs) => AfterSizeChanged(this, EventArgs.Empty);
            _popUpFormBackboard.Hide();

            // Title panel
            _titlePanel = new() {
                Height = GetTitlePanelHeight(),
                FlowDirection = FlowDirection.RightToLeft,
            };
            _title = "(未命名)";
            _closeButton = new() { Parent = _titlePanel };
            _closeButton.Click += (sender, eventArgs) => Hide();
            _titlePanel.SizeChanged += (sender, eventArgs) => {
                _titleFont = new Font(WidgetsConfigs.SystemFontFamily, _titlePanel.Height * .425F, FontStyle.Regular);
                _closeButton.Size = new((int) (_titlePanel.Height * 1.25), _titlePanel.Height - _borderThickness);
                _closeButton.Location = new(_titlePanel.Width - _closeButton.Width, 0);
            };
            _titlePanel.Paint += (sender, eventArgs) => {
                if (_titleFont != null) {
                    Graphics g = eventArgs.Graphics;
                    int titleHeight = _titlePanel.Height;
                    g.DrawString(_title, _titleFont, new SolidBrush(Color.Black), new Point(_titleFont.Height / 4, (int) (titleHeight - _titleFont.Height) / 2));
                    Pen pen = new Pen(_borderColor != null ? _borderColor.Value : Color.Black, _borderThickness);
                    Point point1 = new(0, titleHeight - _borderThickness);
                    Point point2 = new Point(_titlePanel.Width, titleHeight - _borderThickness);
                    g.DrawLine(pen, point1, point2);
                }
            };
            // Content panel
            _contentPanel = new() {
                Margin = new(0), 
                Padding = GetContentPadding(),
            };
            // Buttons panel
            _buttonsPanel = new() {
                Padding = GetButtonsPanelPadding(),
                Height = GetButtonsPanelHeight(),
                Margin = new(0),
                Visible = false,
            };
            _buttonsInnerPanel = new() {
                Parent =_buttonsPanel, 
            };
            _buttons = new();
            _buttonAlignment = HorizontalAlignment.Right;
            _buttonsPanel.SizeChanged += (sender, eventArgs) => {
                _buttonsInnerPanel.Height = _buttonsPanel.Height - _buttonsPanel.Padding.Size.Height;
                int buttonHeight = _buttonsInnerPanel.Height;
                int buttonGap = buttonHeight / 3;
                int innerPanelWidth = 0;
                for (int i = 0; i < _buttons.Count; i++) {
                    FunctionButton button = _buttons[i];
                    // Height must be set first then ResizeTextLabel can be invoked, then the Font can be set
                    button.Height = buttonHeight;
                    button.Width = TextRenderer.MeasureText(button.Label, button.Font).Width + button.Height;
                    innerPanelWidth += button.Width;
                    if (i != 0) {
                        button.Margin = new(buttonGap, 0, 0, 0);
                        innerPanelWidth += buttonGap;
                    }
                }
                _buttonsInnerPanel.Width = innerPanelWidth;
                RelocateButtons();
            };
            // Outer panel
            _outerPanel = new() {
                Parent = this,
                FlowDirection = FlowDirection.TopDown,
            };
            _outerPanel.Controls.Add(_titlePanel);
            _outerPanel.Controls.Add(_contentPanel);
            _outerPanel.Controls.Add(_buttonsPanel);
            _outerPanel.SizeChanged += (sender, eventArgs) => {
                _titlePanel.Width = _outerPanel.Width;
                _contentPanel.Width = _outerPanel.Width;
                _buttonsPanel.Width = _outerPanel.Width;
            };
        }

        public virtual void PretendToShowToCreateHandlesForChildren() {
            base.Show();
            Opacity = 0D;
        }

        public new void Dispose() {
            base.Dispose();
        }

        public new void Show() {
            // Sometimes cursor will hide and don't know why for now
            Cursor.Show();
            base.Hide();
            Opacity = 1D;
            base.ShowDialog();
        }

        public new void Hide() {
            base.Dispose();
        }

        public FunctionButton AddButton(string label) {
            FunctionButton button = new() {
                Label = label,
                Parent = _buttonsInnerPanel,
                Height = _buttonsInnerPanel.Height,
            };
            _buttons.Add(button);
            if (!_buttonsPanel.Visible) {
                _buttonsPanel.Visible = true;
            }
            return button;
        }
        public void RemoveButton(FunctionButton button) {
            _buttons.Remove(button);
            if (_buttons.Count == 0) {
                _buttonsPanel.Visible = false;
            }
        }
        private void RelocateButtons() {
            switch (_buttonAlignment) {
                case HorizontalAlignment.Left:
                    _buttonsInnerPanel.Location = new(_buttonsPanel.Padding.Left, _buttonsPanel.Padding.Top);
                    break;
                case HorizontalAlignment.Right:
                    _buttonsInnerPanel.Location = new(_buttonsPanel.Width - _buttonsInnerPanel.Width - _buttonsPanel.Padding.Left, _buttonsPanel.Padding.Top);
                    break;
                default:
                    _buttonsInnerPanel.Location = new((_buttonsPanel.Width - _buttonsInnerPanel.Width - _buttonsPanel.Padding.Size.Width) / 2, _buttonsPanel.Padding.Top);
                    break;
            }
        }

        public void CalculateDetailProperties() {
            _popUpFormBackboard.Size = WidgetUtils.MainPanel.ClientSize;
            _titlePanel.Height = GetTitlePanelHeight();
            _buttonsPanel.Padding = GetButtonsPanelPadding();
            _buttonsPanel.Height = GetButtonsPanelHeight();
            _contentPanel.Padding = GetContentPadding();
        }
        private int GetTitlePanelHeight() => WidgetUtils.PopUpOrFloatingFormTitle();
        private int GetButtonsPanelHeight() => GetButtonsPanelPadding().Size.Height + WidgetUtils.CommonButtonHeight();
        private Padding GetContentPadding() => WidgetUtils.PopUpOrFloatingFormContentPadding();
        private Padding GetButtonsPanelPadding() => WidgetUtils.PopUpOrFloatingFormButtonsPadding();

        public void SetContentSizeAndSelfSize(Size contentSize) {
            ContentPanel.Height = contentSize.Height;
            int formHeight = ContentPanel.Height;
            if (TitlePanel.Visible) formHeight += TitlePanel.Height;
            if (ButtonsPanel.Visible) formHeight += ButtonsPanel.Height;
            if (_borderColor != null) formHeight += _borderThickness * 2;
            Size = new(contentSize.Width, formHeight);
        }

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            CalculateDetailProperties();
            SizeChanged += ResizeChildren;
            SizeChanged += AfterSizeChanged;
        }

        protected sealed override void OnSizeChanged(EventArgs e) => base.OnSizeChanged(e);
        public void ResizeChildren() => ResizeChildren(EventArgs.Empty);
        public void ResizeChildren(EventArgs eventArgs) => ResizeChildren(this, eventArgs);
        protected virtual void ResizeChildren(object? sender, EventArgs eventArgs) {
            // CalculateDetailProperties();
            // Border
            if (_borderColor != null) {
                _borderRect = new(0, 0, Width - 1, Height - 1);
            }
            // Reset width to outer panel, it will cause width reseting of its children
            Size outerSize = Size;
            if (_borderColor != null) {
                outerSize = new(Width - _borderThickness * 2, Height - _borderThickness * 2);
            }
            _outerPanel.Size = outerSize;
        }

        private void AfterSizeChanged(object? sender, EventArgs eventArgs) {
            if (WidgetUtils.MainPanel == null || WidgetUtils.MainPanel.IsDisposed) {
                return;
            }
            _popUpFormBackboard.Location = WidgetUtils.MainPanel.PointToScreen(Point.Empty);
            int x = _popUpFormBackboard.Location.X;
            int y = _popUpFormBackboard.Location.Y;
            int width = _popUpFormBackboard.Width;
            int height = _popUpFormBackboard.Height;
            Location = new(x + (width - Width) / 2, y + (height - Height) / 2);
        }

        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.Clear(BackColor);
            base.OnPaint(e);
            // Draw border
            if (_borderColor != null && _borderRect != null) {
                e.Graphics.DrawRectangle(new(_borderColor.Value, _borderThickness), _borderRect.Value);
            }
        }

        protected override void OnVisibleChanged(EventArgs e) {
            base.OnVisibleChanged(e);
            _popUpFormBackboard.Visible = Visible;
            if (Visible) {
                EventFuncs.CurrentPopUpForm = this;
            } else {
                EventFuncs.CurrentPopUpForm = null;
            }
        }

        protected override void OnHandleDestroyed(EventArgs e) {
            base.OnHandleDestroyed(e);
            _popUpFormBackboard.Dispose();
            EventFuncs.CurrentPopUpForm = null;
        }
    }

    public class FunctionButton: CommonButton {
        protected override void ResizeTextLabel() {
            if (!IsDisposed) {
                if (Label != null) {
                    Font = new Font(WidgetsConfigs.SystemFontFamily, (int) (Height * .55), FontStyle.Bold, GraphicsUnit.Pixel);
                    using (Graphics g = CreateGraphics()) {
                        LabelX = (int) ((Width - g.MeasureString(Label, Font).Width) / 2 + Width * .02);
                    }
                    LabelY = (Height - Font.Height) / 2;
                }
            }
        }
    }
}
