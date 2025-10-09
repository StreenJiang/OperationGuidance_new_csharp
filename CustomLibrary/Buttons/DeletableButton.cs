using CustomLibrary.Buttons.BaseClasses;
using CustomLibrary.Events;
using CustomLibrary.Utils;

namespace CustomLibrary.Buttons {
    public class DeletableButton: CommonButtonBase {
        private Image _closeImage = ResxUtils.Load("button_close");
        private Rectangle? _imageRect;
        private Image? _imageShowing;
        private bool _down = false;
        private bool _focusOnImage = false;
        private Action? _onDeleted;

        public bool PressingClose { get => _focusOnImage; set => _focusOnImage = value; }
        public event Action Deleted { add => _onDeleted += value; remove => _onDeleted -= value; }

        protected override void OnMouseEnter(EventArgs e) {
            base.OnMouseEnter(e);
            int closeBtnSide = (int) (Height * .75);
            if (ToggleBarRect != null) {
                closeBtnSide = (int) ((Height - ToggleBarRect.Value.Height) * .85);
            }
            Size imageSize = new(closeBtnSide, closeBtnSide);
            Point imageLocation = new(Width - closeBtnSide, (Height - closeBtnSide) / 2);
            if (ToggleBarRect != null) {
                imageLocation = new(Width - closeBtnSide, (Height - ToggleBarRect.Value.Height - closeBtnSide) / 2);
            }
            _imageRect = new(imageLocation, imageSize);
            _imageShowing = WidgetUtils.ResizeImage(_closeImage, imageSize);
        }

        private void ClickAnimation(bool goDown) {
            if (_imageRect != null) {
                if (goDown) {
                    if (!_down) {
                        _imageRect = new(new(_imageRect.Value.X + 1, _imageRect.Value.Y + 1), _imageRect.Value.Size);
                        _down = true;
                    }
                } else {
                    if (_down && !IsPressing) {
                        _imageRect = new(new(_imageRect.Value.X - 1, _imageRect.Value.Y - 1), _imageRect.Value.Size);
                        _down = false;
                    }
                }
            }
        }
        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            _imageRect = null;
            _imageShowing = null;
        }
        protected override void OnMouseMove(MouseEventArgs mevent) {
            base.OnMouseMove(mevent);
            if (_imageRect != null) {
                if (EventFuncs.MouseInArea(new(PointToScreen(_imageRect.Value.Location), _imageRect.Value.Size))) {
                    _focusOnImage = true;
                } else {
                    ClickAnimation(false);
                    _focusOnImage = false;
                }
                Invalidate();
            }
        }
        protected override void OnMouseDown(MouseEventArgs mevent) {
            if (_focusOnImage) {
                BlockHoverDown = true;
            } else {
                BlockHoverDown = false;
            }
            base.OnMouseDown(mevent);
            if (_focusOnImage && _imageRect != null) {
                ClickAnimation(true);
                Invalidate();
            }
        }
        protected override void OnMouseUp(MouseEventArgs mevent) {
            base.OnMouseUp(mevent);
            if (_focusOnImage && _imageRect != null) {
                ClickAnimation(false);
                Invalidate();
                Dispose();
                if (_onDeleted != null) {
                    _onDeleted();
                }
            }
        }
        protected override void PaintAfter(PaintEventArgs e) {
            base.PaintAfter(e);
            if (_imageShowing != null && _imageRect != null) {
                e.Graphics.DrawImage(_imageShowing, _imageRect.Value.Location);
            }
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                _closeImage?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
