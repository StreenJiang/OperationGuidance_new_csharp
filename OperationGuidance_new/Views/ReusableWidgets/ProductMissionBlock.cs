using CustomLibrary.Buttons.BaseClasses;
using CustomLibrary.Configs;
using CustomLibrary.Panels.BaseClasses;
using CustomLibrary.Utils;
using System.Drawing.Drawing2D;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class ProductMissionBlock<T>: CustomContentPanelBase {
        private T _t;
        private Image? _coverImage;
        private string _missionName;
        private Rectangle _innerBorderRect;
        private int _borderSize;
        private Color? _borderColor;
        private InnerButton<T> _innerButton;
        private Color _buttonColor;
        private Color _imageBorderColor;

        // P1级性能优化：缓存Pen对象，避免重复创建
        private Pen? _cachedBorderPen;
        private Pen? _cachedImageBorderPen;

        // 修复竞态条件：使用锁确保线程安全
        private readonly object _borderPenLock = new object();
        private readonly object _imageBorderPenLock = new object();

        public T Entity {
            get => _t;
            set => _t = value;
        }
        public Image? CoverImage {
            get => _coverImage;
            set {
                _coverImage = value;
                _innerButton.Image = value;
            }
        }
        public string MissionName {
            get => _missionName;
            set {
                _missionName = value;
                _innerButton.Label = value;
            }
        }
        public Color? BorderColor {
            get => _borderColor;
            set {
                _borderColor = value;
                // P1级优化：更新缓存的Pen对象
                _cachedBorderPen?.Dispose();
                _cachedBorderPen = null;
            }
        }
        public Color ButtonColor {
            get => _buttonColor;
            set {
                _buttonColor = value;
                _innerButton.BackColor = value;
            }
        }
        public Color ImageBorderColor {
            get => _imageBorderColor;
            set {
                _imageBorderColor = value;
                // P1级优化：更新缓存的Pen对象
                _cachedImageBorderPen?.Dispose();
                _cachedImageBorderPen = null;
                _innerButton.ImageBorderColor = value;
            }
        }
        public InnerButton<T> InnerButton { get => _innerButton; set => _innerButton = value; }

        public ProductMissionBlock(T t, Image? coverImage, Image defaultImage, string missionName, Color? borderColor, Color buttonColor, Color imageBorderColor) {
            _innerButton = new InnerButton<T>(this, defaultImage) {
                Icon = coverImage,
                Label = missionName,
                BackColor = buttonColor,
                ImageBorderColor = imageBorderColor,
                BlockHoverUp = true,
                BlockHoverDown = true,
                Parent = this,
                ConerRadius = WidgetUtils.ControlRadius(),
            };
            _t = t;
            _coverImage = coverImage;
            _missionName = missionName;
            // _borderColor = borderColor;
            _buttonColor = buttonColor;
            _imageBorderColor = imageBorderColor;
            ConerRadius = WidgetUtils.ControlRadius();
        }

        /// <summary>
        /// P1级性能优化：获取缓存的Border Pen对象
        /// 修复竞态条件：使用锁确保线程安全
        /// </summary>
        private Pen GetBorderPen() {
            if (_borderColor == null) {
                return null;
            }

            lock (_borderPenLock) {
                if (_cachedBorderPen != null &&
                    _cachedBorderPen.Color == _borderColor.Value &&
                    _cachedBorderPen.Width == _borderSize) {
                    return _cachedBorderPen;
                }

                _cachedBorderPen?.Dispose();
                _cachedBorderPen = new Pen(_borderColor.Value, _borderSize);
                return _cachedBorderPen;
            }
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            e.Graphics.Clear(BackColor);
            if (_borderColor != null) {
                Pen? pen = GetBorderPen();
                if (pen != null) {
                    if (ConerRadius > 0) {
                        Rectangle rect = new(_innerBorderRect.Location.X, _innerBorderRect.Location.Y, _innerBorderRect.Width - 1, _innerBorderRect.Height - 1);
                        // P1级优化：使用缓存的Pen对象
                        using (GraphicsPath graphicsPath = WidgetUtils.RoundedRect(rect, ConerRadius)) {
                            e.Graphics.DrawPath(pen, graphicsPath);
                        }
                    } else {
                        e.Graphics.DrawRectangle(pen, _innerBorderRect);
                    }
                }
            }
        }

        /// <summary>
        /// P1级性能优化：释放资源
        /// </summary>
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _cachedBorderPen?.Dispose();
                _cachedImageBorderPen?.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            // Recalculate the border size
            _borderSize = (int) Math.Ceiling((double) ((Width + Height) / 400D));
            _innerButton.Size = Size - new Size(_borderSize * 2, _borderSize * 2);
            _innerButton.Margin = new(_borderSize);
            _innerButton.ImageBorderSize = _borderSize;

            // Recalcuate rectangle
            _innerBorderRect = new(_borderSize / 2, _borderSize / 2, Width - _borderSize, Height - _borderSize);
            if (Visible) {
                Invalidate();
            }
        }

        protected override void OnClick(EventArgs e) {
            base.OnClick(e);
        }

        public void PerformClick(EventArgs e) {
            OnClick(e);
        }
    }

    public class InnerButton<T>: CustomImageTextButtonBase {
        private readonly ProductMissionBlock<T> _missionBlock;
        private Image _defaultImage;
        private readonly float ImageRatio = 0.6F;
        private int _imageBorderSize;
        private Color _imageBorderColor;
        private Rectangle _imageBorderRect;

        // P1级性能优化：缓存Pen对象，避免重复创建
        private Pen? _cachedImageBorderPen;

        // 修复竞态条件：使用锁确保线程安全
        private readonly object _imageBorderPenLock = new object();

        public int ImageBorderSize {
            get => _imageBorderSize;
            set {
                _imageBorderSize = value;
                // P1级优化：更新缓存的Pen对象
                _cachedImageBorderPen?.Dispose();
                _cachedImageBorderPen = null;
            }
        }

        public Color ImageBorderColor {
            get => _imageBorderColor;
            set {
                _imageBorderColor = value;
                // P1级优化：更新缓存的Pen对象
                _cachedImageBorderPen?.Dispose();
                _cachedImageBorderPen = null;
            }
        }

        public InnerButton(ProductMissionBlock<T> missionBlock, Image defaultImage) : base() {
            _missionBlock = missionBlock;
            _defaultImage = defaultImage;
        }

        /// <summary>
        /// P1级性能优化：获取缓存的ImageBorder Pen对象
        /// 修复竞态条件：使用锁确保线程安全
        /// </summary>
        private Pen GetImageBorderPen() {
            lock (_imageBorderPenLock) {
                if (_cachedImageBorderPen != null &&
                    _cachedImageBorderPen.Color == _imageBorderColor &&
                    _cachedImageBorderPen.Width == _imageBorderSize) {
                    return _cachedImageBorderPen;
                }

                _cachedImageBorderPen?.Dispose();
                _cachedImageBorderPen = new Pen(_imageBorderColor, _imageBorderSize);
                return _cachedImageBorderPen;
            }
        }

        protected override void PaintAfter(PaintEventArgs e) {
            base.PaintAfter(e);
            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            if (this.Icon != null) {
                // P1级优化：使用缓存的Pen对象
                Pen pen = GetImageBorderPen();
                e.Graphics.DrawRectangle(pen, _imageBorderRect);
            }
        }

        /// <summary>
        /// P1级性能优化：释放资源
        /// </summary>
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _cachedImageBorderPen?.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);

            // Recalcuate rectangle
            if (ImageShowing != null) {
                _imageBorderRect = new(ImageX, ImageY, ImageShowing.Width, ImageShowing.Height);
                if (Visible) {
                    Invalidate();
                }
            }
        }

        protected override void ResizeIconImage() {
            Size imageNewSize = CalcImageSize();
            if (Icon != null) {
                ImageShowing = WidgetUtils.ResizeImage(Icon, imageNewSize);
            } else if (_defaultImage != null) {
                ImageShowing = WidgetUtils.ResizeImage(_defaultImage, imageNewSize);
            }
            // Recalculate image position
            ImageX = (Width - imageNewSize.Width) / 2;
            ImageY = (int) ((Height * 0.85 - imageNewSize.Height) / 2);
        }

        protected override void ResizeTextLabel() {
            if (Label != null) {
                Font = new Font(WidgetsConfigs.SystemFontFamily, Height / 15 + 1.25F, FontStyle.Bold);
                Size imageNewSize = CalcImageSize();
                using (Graphics g = CreateGraphics()) {
                    LabelX = (int) ((Width - g.MeasureString(Label, Font).Width) / 2 + Width * .02);
                }
                int newHeight = (int) (Height * ImageRatio);
                LabelY = (int) ((Height * 0.85 - Font.Height - newHeight) / 2 + newHeight * 1.25);
            }
        }

        private Size CalcImageSize() {
            int newHeight = (int) (Height * ImageRatio);
            int newWidth;
            if (this.Icon != null) {
                newWidth = (int) (newHeight / (decimal) this.Icon.Height * this.Icon.Width);
                if (newWidth > (Width * .9)) {
                    newWidth = (int) (Width * .9);
                    newHeight = (int) (newWidth / (decimal) this.Icon.Width * this.Icon.Height);
                }
            } else if (_defaultImage != null) {
                newWidth = (int) (newHeight / (decimal) _defaultImage.Height * _defaultImage.Width);
            } else {
                newWidth = (int) (Width * .8);
            }
            return new(newWidth, newHeight);
        }

        protected override void OnClick(EventArgs e) {
            base.OnClick(e);
            _missionBlock.PerformClick(e);
        }
    }
}

