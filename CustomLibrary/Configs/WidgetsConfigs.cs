using CustomLibrary.Constants;

namespace CustomLibrary.Configs {
    public static class WidgetsConfigs {
        // Constants
        // System font family
        private const string FONT_FAMILY_DEFAULT = "微软雅黑";

        // Variables
        private static FontFamily _systemFontFamily = new(FONT_FAMILY_DEFAULT);

        public static FontFamily SystemFontFamily {
            get => _systemFontFamily;
            set => _systemFontFamily = value;
        }

        // Resolution configs list
        public static Dictionary<Size, SizeRatioNRectColor> Resolutions { get; set;} = new() {
            // 4 : 3
            { new Size(400, 300), WidthHeightRatio.FourThree },
            { new Size(640, 480), WidthHeightRatio.FourThree }, // VGA
            { new Size(800, 600), WidthHeightRatio.FourThree }, // SVGA
            { new Size(1024, 768), WidthHeightRatio.FourThree }, // XGA
            { new Size(1280, 960), WidthHeightRatio.FourThree },
            { new Size(1400, 1050), WidthHeightRatio.FourThree }, // SXGA
            { new Size(1600, 1200), WidthHeightRatio.FourThree }, // UXGA
            { new Size(1920, 1440), WidthHeightRatio.FourThree },
            { new Size(2048, 1536), WidthHeightRatio.FourThree }, // QXGA
            // 5 : 4
            { new Size(1280, 1024), WidthHeightRatio.FiveFour }, // SXGA
            // 16 : 9
            { new Size(1280, 720), WidthHeightRatio.SixteenNine }, // 720P
            { new Size(1366, 768), WidthHeightRatio.SixteenNine }, // WXGA
            { new Size(1600, 900), WidthHeightRatio.SixteenNine },
            { new Size(1920, 1080), WidthHeightRatio.SixteenNine }, // 1080P
            { new Size(2560, 1440), WidthHeightRatio.SixteenNine }, // 1440P (QHD/2K)
            { new Size(3840, 2160), WidthHeightRatio.SixteenNine }, // 2160P (4K)
            { new Size(7680, 4320), WidthHeightRatio.SixteenNine }, // 4320P (8K)
            // 16 : 10
            { new Size(800, 480), WidthHeightRatio.SixtennTen }, // WVGA
            { new Size(1024, 600), WidthHeightRatio.SixtennTen }, // WSVGA
            { new Size(1280, 800), WidthHeightRatio.SixtennTen }, // WXGA
            { new Size(1440, 900), WidthHeightRatio.SixtennTen }, // WXGA+
            { new Size(1680, 1050), WidthHeightRatio.SixtennTen }, // WSXGA+
            { new Size(1920, 1200), WidthHeightRatio.SixtennTen }, // WUXGA
            { new Size(2560, 1600), WidthHeightRatio.SixtennTen }, // WQXGA
        };
    }
}
