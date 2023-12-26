using CustomLibrary.Buttons;
using CustomLibrary.Panels.BaseClasses;

namespace CustomLibrary.Panels
{
    public class CustomChildMenuFirstPanel: CustomMenuPanelBase {
        private const float _childFirstButtonHeightRatio = 0.07F;
        private bool _needFoldButton;
        private bool _isFolded;
        private Panel? _foldButtonPanel;
        private FoldButton? _foldButton;
        private const float _foldButtonWidthRatio = 0.35F;
        private const float _foldButtonHeightRatio = 0.035F;
        private int _allMenuButtonHeight;

        public bool NeedFoldButton {
            get => _needFoldButton;
            set {
                _needFoldButton = value;
                if (value) {
                    if (_foldButtonPanel == null) {
                        _foldButtonPanel = new();
                        _foldButtonPanel.Margin = new(0);
                        _foldButtonPanel.Parent = this;
                    }
                    if (_foldButton == null) {
                        _foldButton = new();
                        _foldButton.Parent = _foldButtonPanel;
                    }
                } else {
                    if (_foldButtonPanel != null && !_foldButtonPanel.IsDisposed) {
                        _foldButtonPanel.Dispose();
                    }
                    if (_foldButton != null && !_foldButton.IsDisposed) {
                        _foldButton.Dispose();
                    }
                }
            }
        }
        public bool IsFolded {
            get => _isFolded;
            set => _isFolded = value;
        }
        public FoldButton? FoldButton {
            get => _foldButton;
            set => _foldButton = value;
        }

        public CustomChildMenuFirstPanel() : base() {
            _needFoldButton = false;
            _isFolded = false;
            _allMenuButtonHeight = 0;
        }

        protected override void OnSizeChanged(EventArgs e) {
            //if (!this.Visible && this.Created) {
            //    // 开始异步调用，提升性能
            //    IAsyncResult asyncResult = this.BeginInvoke(new(() => {
            //        base.OnSizeChanged(e);
            //        this.InvokeResizing();
            //    }));

            //    // 结束异步调用
            //    this.EndInvoke(asyncResult);
            //} else {
            //    base.OnSizeChanged(e);
            //    this.InvokeResizing();
            //}
            base.OnSizeChanged(e);
            this.InvokeResizing();
        }

        private void InvokeResizing() {
            if (CheckFoldButtonPanel() && CheckFoldButton()) {
                _foldButtonPanel.Size = new(this.Width, this.Height - _allMenuButtonHeight);
                _foldButton.Location = new(_foldButtonPanel.Width - _foldButton.Width, _foldButtonPanel.Height - _foldButton.Height);
            }
        }

        protected override void OnControlAdded(ControlEventArgs e) {
            base.OnControlAdded(e);
            if (CheckFoldButtonPanel()) {
                _foldButtonPanel.SendToBack();
            }
        }

        // Ratio of height of main menu panel in main form
        protected override float GetResizeRatio() {
            if (!this.OnlyIconMode) {
                return 0.13F;
            } else {
                return 0.05F;
            }
        }

        protected override void ResizeButtons() {
            Size newButtonSize = new(this.Width, (int) (this.Height * _childFirstButtonHeightRatio));
            _allMenuButtonHeight = 0;
            foreach (Control button in this.Controls) {
                if (button is Panel) {
                    _foldButton.Folded = this.OnlyIconMode;
                    if (this.OnlyIconMode) {
                        _foldButton.Size = new(this.Width, (int) (this.Height * _foldButtonHeightRatio));
                    } else {
                        _foldButton.Size = new((int) (this.Width * _foldButtonWidthRatio), (int) (this.Height * _foldButtonHeightRatio));
                    }
                    continue;
                }
                button.Size = newButtonSize;
                _allMenuButtonHeight += newButtonSize.Height;
            }
        }

        private bool CheckFoldButtonPanel() {
            if (_needFoldButton) {
                if (_foldButtonPanel == null) {
                    throw new NullReferenceException("FoldButtonPanel can not be null if NeedFoldButton is true.");
                }
                return true;
            }
            return false;
        }

        private bool CheckFoldButton() {
            if (_needFoldButton) {
                if (_foldButton == null) {
                    throw new NullReferenceException("FoldButton can not be null if NeedFoldButton is true.");
                }
                return true;
            }
            return false;
        }
    }
}
