namespace CustomLibrary.Constants {
    public static class WidgetsConstants {
    }

    public enum MenuPanelDirection {
        TOP, BOTTOM, LEFT, RIGHT
    }

    public sealed class WidthHeightRatio {
        private static List<SizeRatioNRectColor> _innerList = new();
        public static int Count { get => _innerList.Count; }
        private static SizeRatioNRectColor AddNew(int widthRatio, int heightRatio, Color color) {
            SizeRatioNRectColor ratio = new(widthRatio, heightRatio, color);
            _innerList.Add(ratio);
            return ratio;
        }

        public static List<SizeRatioNRectColor>.Enumerator GetEnumerator() {
            return _innerList.GetEnumerator();
        }

        // New ratio should be added here, don't touch any of above, otherwise this class will be ruined
        public static SizeRatioNRectColor FourThree { get; } = AddNew(4, 3, Color.Green);
        public static SizeRatioNRectColor SixteenNine { get; } = AddNew(16, 9, Color.Red);
        public static SizeRatioNRectColor SixtennTen { get; } = AddNew(16, 10, Color.Blue);
        public static SizeRatioNRectColor FiveFour { get; } = AddNew(5, 4, Color.Orange);
    }

    public struct SizeRatioNRectColor {
        public int WidthRatio { set; get; }
        public int HeightRatio { set; get; }
        public Color RectColor { set; get; }

        public SizeRatioNRectColor(int widthRatio, int heightRatio, Color rectColor) {
            WidthRatio = widthRatio;
            HeightRatio = heightRatio;
            RectColor = rectColor;
        }
    }
}
