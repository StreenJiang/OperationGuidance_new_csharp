using System.ComponentModel;

namespace OperationGuidance_service.Constants {
    public enum TorqueUnit: int {
        [Description("牛·米 (Nm)")]
        Nm = 1,

        [Description("磅·英尺 (Lbf.ft)")]
        Lbf_ft = 2,

        [Description("磅·英寸 (Lbf.in)")]
        Lbf_in = 3,

        [Description("公斤·米 (Kpm)")]
        Kpm = 4,

        [Description("公斤·厘米 (Kgf.cm)")]
        Kgf_cm = 5,

        [Description("盎司·英寸 (ozf.in)")]
        ozf_in = 6,

        [Description("百分比 (%)")]
        Percent = 7,

        [Description("牛·厘米 (Ncm)")]
        Ncm = 8
    }

    public static class TorqueUnitExtensions {
        public static TorqueUnit FromValue(int value) {
            return Enum.IsDefined(typeof(TorqueUnit), value)
                ? (TorqueUnit) value
                : throw new ArgumentOutOfRangeException(nameof(value), $"未知扭矩单位编码: {value}");
        }

        public static int ToInt(this TorqueUnit unit) {
            return (int) unit;
        }

        public static string GetDescription(this TorqueUnit unit) {
            var fieldInfo = unit.GetType().GetField(unit.ToString());
            var descriptionAttributes = (DescriptionAttribute[]) fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return descriptionAttributes.Length > 0 ? descriptionAttributes[0].Description : unit.ToString();
        }
    }
}
