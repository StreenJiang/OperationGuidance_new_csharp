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
        private Color _borderColor;
        private InnerButton _innerButton;
        private Color _buttonColor;
        private Color _imageBorderColor;

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
        public Color BorderColor {
            get => _borderColor;
            set => _borderColor = value;
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
                _innerButton.ImageBorderColor = value;
            }
        }

        public ProductMissionBlock(T t, Image? coverImage, Image defaultImage, string missionName, Color borderColor, Color buttonColor, Color imageBorderColor) {
            _innerButton = new InnerButton(this, defaultImage) {
                Icon = coverImage,
                Label = missionName,
                BackColor = buttonColor,
                ImageBorderColor = imageBorderColor,
                BlockHoverUp = true,
                BlockHoverDown = true,
                Parent = this,
            };
            _t = t;
            _coverImage = coverImage;
            _missionName = missionName;
            _borderColor = borderColor;
            _buttonColor = buttonColor;
            _imageBorderColor = imageBorderColor;
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            e.Graphics.Clear(BackColor);
            e.Graphics.DrawRectangle(new Pen(_borderColor, _borderSize), _innerBorderRect);
        }

        protected override void InvokeResizing(object? sender, EventArgs eventArgs) {
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

        private class InnerButton: CustomImageTextButtonBase {
            private readonly ProductMissionBlock<T> _missionBlock;
            private Image _defaultImage;
            private readonly float ImageRatio = 0.6F;
            private int _imageBorderSize;
            private Color _imageBorderColor;
            private Rectangle _imageBorderRect;

            public int ImageBorderSize {
                get => _imageBorderSize;
                set => _imageBorderSize = value;
            }
            public Color ImageBorderColor {
                get => _imageBorderColor;
                set => _imageBorderColor = value;
            }

            public InnerButton(ProductMissionBlock<T> missionBlock, Image defaultImage) : base() {
                _missionBlock = missionBlock;
                _defaultImage = defaultImage;
            }

            protected override void PaintAfter(PaintEventArgs e) {
                base.PaintAfter(e);
                e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                if (this.Icon != null) {
                    e.Graphics.DrawRectangle(new Pen(_imageBorderColor, _imageBorderSize), _imageBorderRect);
                }
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
                    ImageShowing = WidgetUtils.ResizeImageWithoutLosingQuality(Icon, imageNewSize);
                } else if (_defaultImage != null) {
                    ImageShowing = WidgetUtils.ResizeImageWithoutLosingQuality(_defaultImage, imageNewSize);
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
                    LabelY = (int) ((Height * 0.85 - Font.Height - imageNewSize.Height) / 2 + imageNewSize.Height * 1.25);
                }
            }

            private Size CalcImageSize() {
                int newHeight = (int) (Height * ImageRatio);
                int newWidth;
                if (this.Icon != null) {
                    newWidth = (int) (newHeight / (decimal) this.Icon.Height * this.Icon.Width);
                } else if (_defaultImage != null) {
                    newWidth = (int) (newHeight / (decimal) _defaultImage.Height * _defaultImage.Width);
                } else {
                    newWidth = (int) (Height * .8);
                }
                return new(newWidth, newHeight);
            }

            protected override void OnClick(EventArgs e) {
                base.OnClick(e);
                _missionBlock.OnClick(e);
            }
        }
    }
}

