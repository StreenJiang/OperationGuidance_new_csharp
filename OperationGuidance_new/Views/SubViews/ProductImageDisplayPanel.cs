using CustomLibrary.Utils;
using OperationGuidance_new.Views.ReusableWidgets;
using System.Drawing.Drawing2D;

namespace OperationGuidance_new.Views.SubViews {
    public class ProductImageDisplayPanel: AProductImageDisplayPanel {
        public ProductImageDisplayPanel(Image productDefaultImage) : base() {
            ProductDefaultImage = productDefaultImage;
        }

        protected override void InvokeResizing() {
            // MaxRectSize = MainUtils.GetProperSizeAccordingToSizeRatio((Size * .95F).ToSize(), Size);
            // MaxRectSize = MainUtils.GetMaxSizeOfSizeRatioByWidth(Width);
            // if (MaxRectSize.Height > Height) {
            //     MaxRectSize = MainUtils.GetMaxSizeOfSizeRatioByHeight(Height);
            // }
            MaxRectSize = Size;
            MaxRectWidth = MaxRectSize.Width;
            MaxRectHeight = MaxRectSize.Height;
            // Calculate location of max rectangle depends on size
            MaxRectLocation = new((Width - MaxRectWidth) / 2, (Height - MaxRectHeight) / 2);
            MaxRect = new(MaxRectLocation, MaxRectSize);
        }

        protected override void InvokePaint(Graphics g) {
            g.SmoothingMode = SmoothingMode.HighSpeed;
            if (ProductImage == null || ImageLocation == null) {
                int newImageSide = Height / 2;
                ProductDefaultImageShowing = WidgetUtils.ResizeImage(ProductDefaultImage, newImageSide, newImageSide);
                g.DrawImage(ProductDefaultImageShowing, new Point((Width - ProductDefaultImageShowing.Width) / 2, (Height - newImageSide) / 2));
            } else {
                g.DrawImage(ProductImage, ImageLocation.Value);
            }
        }
    }
}

