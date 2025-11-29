namespace OperationGuidance_service.Constants {
    public class CommonConstants {
    }

    public enum DataTypes {
        ASCII = 0,
        BINARY = 2,     // 二进制
        OCTAL = 8,      // 八进制
        DECIMAL = 10,    // 十进制
        HEX = 16,        // 十六进制
    }

    public enum YesOrNo {
        YES = 1, NO = 2
    }

    public static class YesOrNoExtensions {
        public static int ToInt(this YesOrNo value) {
            return (int) value;
        }
    }

    public static class BoolExtensions {
        public static int ToYesOrNoInt(this bool value) {
            return (int) (value ? YesOrNo.YES : YesOrNo.NO);
        }

        public static YesOrNo ToYesOrNo(this bool value) {
            return value ? YesOrNo.YES : YesOrNo.NO;
        }
    }

    public static class IntExtensions {
        public static bool ToYesOrNoBool(this int value) {
            return value == (int) YesOrNo.YES;
        }
    }
}
