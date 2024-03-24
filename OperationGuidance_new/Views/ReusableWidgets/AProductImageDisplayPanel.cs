// using CustomLibrary.Constants;

using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public abstract class AProductImageDisplayPanel: Panel {
        private ProductSideDTO? _originalSideDTO;
        private ProductSideDTO? _newSideDTO;

        private Image _productDefaultImage;
        private Image? _productDefaultImageShowing;
        private Image? _productImage;
        private Point? _imageLocation;
        // private List<Rectangle> _differentRects;
        private Size _maxRectSize;
        private int _maxRectWidth;
        private int _maxRectHeight;
        private Point _maxRectLocation;
        private Rectangle _maxRect;

        protected Image ProductDefaultImage { get => _productDefaultImage; set => _productDefaultImage = value; }
        protected Image? ProductDefaultImageShowing { get => _productDefaultImageShowing; set => _productDefaultImageShowing = value; }
        protected Image? ProductImage { get => _productImage; set => _productImage = value; }
        protected Point? ImageLocation { get => _imageLocation; set => _imageLocation = value; }
        // protected List<Rectangle> DifferentRects { get => _differentRects; set => _differentRects = value; }
        public Size MaxRectSize { get => _maxRectSize; protected set => _maxRectSize = value; }
        public int MaxRectWidth { get => _maxRectWidth; protected set => _maxRectWidth = value; }
        public int MaxRectHeight { get => _maxRectHeight; protected set => _maxRectHeight = value; }
        public Point MaxRectLocation { get => _maxRectLocation; protected set => _maxRectLocation = value; }
        public Rectangle MaxRect { get => _maxRect; protected set => _maxRect = value; }

        public AProductImageDisplayPanel() {
            // _differentRects = new();
            // for (int i = 0; i < WidthHeightRatio.Count; i++) {
            //     _differentRects.Add(new());
            // }
            _maxRect = new();
        }

        public void SetImage(Image? productImage, Point? imageLocation) {
            _productImage = productImage;
            _imageLocation = imageLocation;
            Invalidate();
        }

        public bool CanTriggerClick() {
            return _productImage == null || _imageLocation == null;
        }

        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            InvokeResizing();
        }

        protected abstract void InvokeResizing();

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            InvokePaint(e.Graphics);
        }

        protected abstract void InvokePaint(Graphics g);
    }
}
