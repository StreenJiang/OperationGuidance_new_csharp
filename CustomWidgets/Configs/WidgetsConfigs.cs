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
    }
}
