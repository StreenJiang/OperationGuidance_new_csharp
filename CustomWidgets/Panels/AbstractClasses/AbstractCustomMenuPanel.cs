using CustomLibrary.Buttons;
using CustomLibrary.Constants;
using CustomLibrary.Panels.BaseClasses;
using CustomLibrary.Utils;

namespace CustomLibrary.Panels.AbstractClasses
{
    public abstract class AbstractCustomMenuPanel<T>: AbstractCustomPanel where T : CustomMenuButton {
        private MenuPanelDirection? _panelDirection;
        private T? _currentButton;
        private bool _onlyIconMode;
        private System.Windows.Forms.Timer _timer;
        private int _foldingTime;
        private int _step;
        private int _newWidth;
        private List<int>.Enumerator _enumerator;

        
        public MenuPanelDirection? PanelDirection {
            get => _panelDirection;
            set => _panelDirection = value;
        }
        public bool OnlyIconMode {
            get => _onlyIconMode;
            set {
                _onlyIconMode = value;
                foreach (Control control in Controls) {
                    if (control is Panel) {
                        continue;
                    }
                    T button = (T) control;
                    if (value) {
                        button.HideLabel();
                    } else {
                        button.ShowLabel();
                    }
                }
                ((CustomContentPanelBase) this.Parent).InvokeResizing();
            }
        }

        public int FoldingTime {
            get => this._foldingTime;
            set {
                this._foldingTime = value;
                this._step = value / this._timer.Interval;
            }
        }

        public AbstractCustomMenuPanel() : base() {
            this._panelDirection = MenuPanelDirection.LEFT;
            this._timer = new();
            this._timer.Interval = 20;
            this._timer.Tick += this.TimerTick;
            this.FoldingTime = 250;
        }

        protected override void OnControlAdded(ControlEventArgs e) {
            base.OnControlAdded(e);
            Control control = e.Control;
            if (control != null && WidgetUtils.IsSubClass<T>(control.GetType())) {
                T button = (T) control;
                if (button.ToggledButton) {
                    if (_currentButton == null) {
                        button.SetToggle(true);
                        button.ShowContentPanel();
                        _currentButton = button;
                    } else {
                        button.HideContentPanle();
                    }
                }
                if (this.OnlyIconMode) {
                    button.HideLabel();
                }
                button.Click += (sender, eventArgs) => {
                    if (button.ToggledButton) {
                        if (_currentButton != null) {
                            if (button != _currentButton) {
                                _currentButton.SetToggle(false);
                                _currentButton.HideContentPanle();
                                button.ShowContentPanel();
                                button.SetToggle(true);
                                _currentButton = button;
                            }
                        }
                    }
                };
            }
        }

        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            ResizeButtons();
        }

        // Recalculate size
        public void ResizeSelf() {
            if (_panelDirection != null) {
                Size parentSize = this.Parent.Size;
                switch (_panelDirection) {
                    case MenuPanelDirection.TOP:
                    case MenuPanelDirection.BOTTOM:
                        Size = new Size(parentSize.Width, (int) (parentSize.Height * GetResizeRatio()));
                        break;
                    case MenuPanelDirection.LEFT:
                    case MenuPanelDirection.RIGHT:
                        int newWidth = (int) (parentSize.Width * GetResizeRatio());
                        Size = new Size(newWidth, parentSize.Height);
                        // if (this.Height != parentSize.Height) {
                        //     Size = new Size(newWidth, parentSize.Height);
                        // } else if (this.Width != newWidth) {
                        //     if (_newWidth == newWidth) {
                        //         return;
                        //     }
                        //     _newWidth = newWidth;
                        //     int diff;
                        //     if (this.Width > _newWidth) {
                        //         diff = this.Width - _newWidth;
                        //     } else {
                        //         diff = _newWidth - this.Width;
                        //     }
                        //     List<int> widths = WidgetUtils.ArithmeticProgression(diff, _step, 1);
                        //     if (widths.Sum() > diff) {
                        //         widths.Add(widths.Sum() - diff);
                        //     } else {
                        //         widths.Add(diff - widths.Sum());
                        //     }
                        //     widths.Sort((w1, w2) => w2.CompareTo(w1));
                        //     _enumerator = widths.GetEnumerator();
                        //     _enumerator.MoveNext();
                        //     _timer.Start();
                        // }
                        break;
                }
            } else {
                throw new NullReferenceException("Panel direction can not be null.");
            }
        }

        private void TimerTick(object? sender, EventArgs eventArgs) {
            int next = _enumerator.Current;
            if (this.OnlyIconMode) {
                if (this.Width - next <= _newWidth) {
                    this.Size = new(_newWidth, this.Parent.Height);
                    _timer.Stop();
                    _enumerator.Dispose();
                } else {
                    this.Size = new(this.Width - next, this.Parent.Height);
                    _enumerator.MoveNext();
                }
            } else {
                if (this.Width + next >= _newWidth) {
                    this.Size = new(_newWidth, this.Parent.Height);
                    _timer.Stop();
                    _enumerator.Dispose();
                } else {
                    this.Size = new(this.Width + next, this.Parent.Height);
                    _enumerator.MoveNext();
                }
            }
            ((CustomContentPanelBase) this.Parent).InvokeResizing();
        }

        // Change position
        public void ChangePosition() {
            if (_panelDirection == MenuPanelDirection.TOP || _panelDirection == MenuPanelDirection.LEFT) {
                BringToFront();
            } else {
                SendToBack();
            }
        }

        protected abstract float GetResizeRatio();

        protected abstract void ResizeButtons();

        protected void ShowControls(bool flag = true) {
            foreach (Control control in Controls) {
                control.Visible = flag;
            }
        }

    }
}
