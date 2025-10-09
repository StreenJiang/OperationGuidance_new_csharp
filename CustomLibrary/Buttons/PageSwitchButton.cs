using CustomLibrary.Buttons.BaseClasses;
using CustomLibrary.Utils;

namespace OperationGuidance_new.Views {
    public class PageSwitchButton: CustomImageTextButtonBase {
        private int _totalPages;
        private int _currentPage;

        public int TotalPages {
            get => _totalPages; set => _totalPages = value;
        }
        public int CurrentPage {
            get => _currentPage; set => _currentPage = value;
        }

        protected override void ResizeIconImage() {
            if (Icon != null) {
                ImageShowing = WidgetUtils.ResizeImage(Icon, new(Height, Height));
                // Recalculate image location
                ImageX = 0;
                ImageY = 0;
            }
        }

        protected override void ResizeTextLabel() {
        }

    }
}
