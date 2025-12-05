using CustomLibrary.Buttons.BaseClasses;
using CustomLibrary.Forms;
using CustomLibrary.Utils;
using OperationGuidance_new.Constants;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views.SubViews {
    public class DeviceBlock: CustomImageTextButtonBase {
        private DeviceCategory _category;

        private readonly float _imageRatio = 0.75F;
        private Rectangle _borderRect;
        private Color? _borderColor;
        private string _categoryName;
        private CustomFloatingForm? _floatingForm;
        private CustomPopUpForm? _popUpForm;

        public DeviceCategory Category { get => _category; set => _category = value; }
        public Color? BorderColor { get => _borderColor; set => _borderColor = value; }
        public string CategoryName { get => _categoryName; set => _categoryName = value; }
        public CustomFloatingForm? FloatingForm { get => _floatingForm; set => _floatingForm = value; }
        public CustomPopUpForm? PopUpForm { get => _popUpForm; set => _popUpForm = value; }

        public DeviceBlock(DeviceCategory category) : base() {
            _category = category;
            _categoryName = category.Name;
            Icon = CommonUtils.ImageBase64ToImage(category.IconEmptyStr);
        }

        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            _borderRect = new(0, 0, Width, Height);
        }

        public void ResetIconByStatus(DeviceStatus status) {
            switch (status) {
                case DeviceStatus.NORMAL:
                    Icon = CommonUtils.ImageBase64ToImage(_category.IconStr);
                    break;
                case DeviceStatus.ERROR:
                    Icon = CommonUtils.ImageBase64ToImage(_category.IconErrorStr);
                    break;
                case DeviceStatus.EMPTY:
                    Icon = CommonUtils.ImageBase64ToImage(_category.IconEmptyStr);
                    break;
            }
            ResizeIconImage();
        }

        protected override void PaintAfter(PaintEventArgs e) {
            base.PaintAfter(e);
            if (_borderColor != null) {
                e.Graphics.DrawRectangle(new Pen(_borderColor.Value, 1), _borderRect);
            }
        }

        protected override void ResizeIconImage() {
            if (Icon != null) {
                Size newSize = (Size * _imageRatio).ToSize();
                ImageShowing = WidgetUtils.ResizeImage(Icon, newSize);
                // Recalculate image position
                ImageX = (Width - newSize.Width) / 2;
                ImageY = (Height - newSize.Height) / 2;
            }
        }

        protected override void ResizeTextLabel() {
        }
    }
}

