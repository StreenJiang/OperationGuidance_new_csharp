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
            get => this._contentPanel;
            set => this._contentPanel = value;
        }
        public ScrollBar VScrollBar { get => _vScrollBar; }
        public bool AlwaysShowScrollBar {
            get => this._alwaysShowScrollBar;
            set {
                this._alwaysShowScrollBar = value;
                this.ShowScrollBarAndResizing(value);
            }
        }
        public bool NeedsPadding { get => _needsPadding; set => _needsPadding = value; }

        public CustomVScrollingContentPanel(Color? borderColor, CustomContentPanelBase contentPanel, bool alwaysShowScrollBar = false) : base() {
            this._outerPanel = new() {
                Margin = new(0),
                Parent = this,
                PenBorderColor = borderColor,
            };
            this._innerPanel = new() {
                Margin = new(0),
                Parent = this._outerPanel,
            };
            this._vScrollBar = new VScrollBar() {
                Margin = new(0),
                Parent = this,
                Visible = alwaysShowScrollBar,
            };
            this._contentPanel = contentPanel;
            contentPanel.Parent = this._innerPanel;
            contentPanel.Location = new(0, 0);
            _needsPadding = true;

            // If scrollbar scrolls, then contentpanel should move too
            this._vScrollBar.Scroll += (sender, e) => {
                int realMaximum = this._vScrollBar.Maximum - this._vScrollBar.LargeChange;
                int value = this._vScrollBar.Value < realMaximum ? this._vScrollBar.Value : realMaximum;
                this._contentPanel.Location = new Point(0, -value);
            };

            // If mousewheel event triggered in range of content panel, then manually trigger scrollbar's mousewheel event
            this.MouseWheel += (sender, e) => {
                if (this._vScrollBar.Visible) {
                    int realMaximum = this._vScrollBar.Maximum - this._vScrollBar.LargeChange;
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
                    this._contentPanel.Location = new Point(0, -currentValue);
                }
            };

            this.AlwaysShowScrollBar = alwaysShowScrollBar;
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            // Recalculate all inner controls
            this._vScrollBar.Height = this.Height;

            // Check if needs scrollbar
            bool needsScrollBar = this._vScrollBar.Visible || this._alwaysShowScrollBar;
            int innerHeight = _needsPadding ? Height - (WidgetUtils.ContentPadding(TopLevelControl.Width, TopLevelControl.Height) * 2 + 1) * 2 : Height;
            if (this._contentPanel.CheckNeedsScrollBar(innerHeight)) {
                this._vScrollBar.Visible = true;
                needsScrollBar = true;
            } else {
                this._vScrollBar.Visible = false;
                needsScrollBar = false;
            }

            // Check if scrollbar is shown
            if (needsScrollBar) {
                this._outerPanel.Size = new(this.Width - this._vScrollBar.Width, this.Height);
            } else {
                this._outerPanel.Size = new(this.Width, this.Height);
            }

            int innerWidth = this._outerPanel.Width - this._outerPanel.Padding.Size.Width;
            this._innerPanel.Size = new(innerWidth, innerHeight);

            if (this._contentPanel.NewHeight > 0) {
                this._contentPanel.Size = new(innerWidth, this._contentPanel.NewHeight);
            } else {
                this._contentPanel.Size = new(innerWidth, innerHeight);
            }
            int heightDiff = this._contentPanel.Height - this._innerPanel.Height;
            if (heightDiff > 0) {
                double sliderRatio = this._innerPanel.Height / (double) this._contentPanel.Height;
                int sliderHeight = WidgetUtils.CalculateScrollBarSlider(heightDiff, sliderRatio);
                this._vScrollBar.Maximum = heightDiff + sliderHeight;
                this._vScrollBar.SmallChange = sliderHeight / 15;
                this._vScrollBar.LargeChange = sliderHeight;
            }
        }

        public void ShowScrollBarAndResizing(bool flag = true) {
            this._vScrollBar.Visible = flag;
            if (flag) {
                this._outerPanel.Size = new(this.Width - this._vScrollBar.Width, this.Height);
            } else {
                this._outerPanel.Size = new(this.Width, this.Height);
            }
        }

        // public override void VisibleToTrue() {
        //     _contentPanel.VisibleToTrue();
        // }
    }
}
