using CustomLibrary.Utils;

namespace CustomLibrary.Panels.BaseClasses {
    public class CustomVScrollingContentPanel: CustomContentPanelBase {
        private CustomContentPanel _outerPanel;
        private Panel _innerPanel;
        private ScrollBar _vScrollBar;
        private bool _alwaysShowScrollBar;
        private CustomContentPanelBase _contentPanel;
        private bool _needsPadding;

        public CustomContentPanel OuterPanel { get => _outerPanel; }
        public Panel InnerPanel { get => _innerPanel; }
        public CustomContentPanelBase ContentPanel {
            get => _contentPanel;
            set => _contentPanel = value;
        }
        public ScrollBar VScrollBar { get => _vScrollBar; }
        public bool AlwaysShowScrollBar {
            get => _alwaysShowScrollBar;
            set {
                _alwaysShowScrollBar = value;
                ShowScrollBarAndResizing(value);
            }
        }
        public bool NeedsPadding { get => _needsPadding; set => _needsPadding = value; }

        public CustomVScrollingContentPanel(CustomContentPanelBase contentPanel, bool paddingWithoutBorder = false) : this(null, contentPanel, false) {
            _outerPanel.PaddingWithoutBorder = paddingWithoutBorder;
        }
        public CustomVScrollingContentPanel(Color? borderColor, CustomContentPanelBase contentPanel, bool alwaysShowScrollBar = false) : base() {
            _outerPanel = new() {
                Margin = new(0),
                Parent = this,
                PenBorderColor = borderColor,
            };
            _innerPanel = new() {
                Margin = new(0),
                Parent = _outerPanel,
            };
            _vScrollBar = new VScrollBar() {
                Margin = new(0),
                Parent = this,
                Visible = alwaysShowScrollBar,
            };
            _contentPanel = contentPanel;
            contentPanel.Parent = _innerPanel;
            contentPanel.Location = new(0, 0);
            _needsPadding = true;

            // If scrollbar scrolls, then contentpanel should move too
            _vScrollBar.Scroll += (sender, e) => {
                int realMaximum = _vScrollBar.Maximum - _vScrollBar.LargeChange;
                int value = _vScrollBar.Value < realMaximum ? _vScrollBar.Value : realMaximum;
                _contentPanel.Location = new Point(0, -value);
            };

            // If mousewheel event triggered in range of content panel, then manually trigger scrollbar's mousewheel event
            MouseWheel += (sender, e) => {
                if (_vScrollBar.Visible) {
                    int realMaximum = _vScrollBar.Maximum - _vScrollBar.LargeChange;
                    int currentValue = _vScrollBar.Value;
                    if (e.Delta > 0) {
                        currentValue -= _vScrollBar.SmallChange;
                    } else {
                        currentValue += _vScrollBar.SmallChange;
                    }
                    if (currentValue < 0) {
                        currentValue = 0;
                    } else if (currentValue > realMaximum) {
                        currentValue = realMaximum;
                    }
                    _vScrollBar.Value = currentValue;
                    _contentPanel.Location = new Point(0, -currentValue);
                }
            };

            AlwaysShowScrollBar = alwaysShowScrollBar;
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            if (TopLevelControl == null) {
                return;
            }
            // Recalculate all inner controls
            _vScrollBar.Height = Height;
            _vScrollBar.Width = WidgetUtils.ScrollBarThickness();

            // Check if needs scrollbar
            bool needsScrollBar = _vScrollBar.Visible || _alwaysShowScrollBar;
            int innerHeight = _needsPadding ? Height - (WidgetUtils.ContentInnerBorderMargin(TopLevelControl.Width, TopLevelControl.Height) * 2 + 1) * 2 : Height;
            if (_contentPanel.CheckNeedsScrollBar(innerHeight)) {
                _vScrollBar.Visible = true;
                needsScrollBar = true;
            } else {
                _vScrollBar.Visible = false;
                needsScrollBar = false;
            }

            // Check if scrollbar is shown
            if (needsScrollBar) {
                _outerPanel.Size = new(Width - _vScrollBar.Width, Height);
            } else {
                _outerPanel.Size = new(Width, Height);
            }

            int innerWidth = _outerPanel.Width - _outerPanel.Padding.Size.Width;
            _innerPanel.Size = new(innerWidth, innerHeight);

            if (_contentPanel.NewHeight > 0) {
                _contentPanel.Size = new(innerWidth, _contentPanel.NewHeight);
            } else {
                _contentPanel.Size = new(innerWidth, innerHeight);
            }
            WidgetUtils.CalculateScrollBar(_vScrollBar, _innerPanel.Height, _contentPanel.Height);
        }

        public void ShowScrollBarAndResizing(bool flag = true) {
            _vScrollBar.Visible = flag;
            if (flag) {
                _outerPanel.Size = new(Width - _vScrollBar.Width, Height);
            } else {
                _outerPanel.Size = new(Width, Height);
            }
        }

        public override void VisibleToTrue() {
            Size = new(Parent.Width - Parent.Padding.Size.Width, Parent.Height - Parent.Padding.Size.Height);
        }
    }
}
