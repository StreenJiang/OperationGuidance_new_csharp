using System.Text;
using CustomLibrary.Utils;
using OperationGuidance_service.Constants;

namespace OperationGuidance_new.Configs.DTOs {
    public class SciiBatchConfig: ConfigBase {
        public string CharSet { get; set; } = "GBK";
        public int enabled { get; set; } = (int) YesOrNo.NO;

        public string day_shift_1 { get; set; } = string.Empty;
        public string day_shift_2 { get; set; } = string.Empty;
        public string day_shift_3 { get; set; } = string.Empty;
        public string day_shift_4 { get; set; } = string.Empty;
        public string day_shift_5 { get; set; } = string.Empty;
        public string day_shift_6 { get; set; } = string.Empty;
        public string day_shift_7 { get; set; } = string.Empty;
        public string day_shift_8 { get; set; } = string.Empty;
        public string day_shift_9 { get; set; } = string.Empty;
        public string day_shift_10 { get; set; } = string.Empty;

        public string night_shift_1 { get; set; } = string.Empty;
        public string night_shift_2 { get; set; } = string.Empty;
        public string night_shift_3 { get; set; } = string.Empty;
        public string night_shift_4 { get; set; } = string.Empty;
        public string night_shift_5 { get; set; } = string.Empty;
        public string night_shift_6 { get; set; } = string.Empty;
        public string night_shift_7 { get; set; } = string.Empty;
        public string night_shift_8 { get; set; } = string.Empty;
        public string night_shift_9 { get; set; } = string.Empty;
        public string night_shift_10 { get; set; } = string.Empty;

        public Dictionary<string, string>? GetDayShifts() {
            Dictionary<string, string> dict = new();

            bool ok = true;
            ok = ok && AddShift(dict, day_shift_1);
            ok = ok && AddShift(dict, day_shift_2);
            ok = ok && AddShift(dict, day_shift_3);
            ok = ok && AddShift(dict, day_shift_4);
            ok = ok && AddShift(dict, day_shift_5);
            ok = ok && AddShift(dict, day_shift_6);
            ok = ok && AddShift(dict, day_shift_7);
            ok = ok && AddShift(dict, day_shift_8);
            ok = ok && AddShift(dict, day_shift_9);
            ok = ok && AddShift(dict, day_shift_10);

            if (!ok) {
                WidgetUtils.ShowWarningPopUp("班次与批次号对应配置格式错误！请参照“班次=批次号=报警数量”的格式填写。");
                return null;
            }

            return dict;
        }

        public Dictionary<string, string>? GetNightShifts() {
            Dictionary<string, string> dict = new();

            bool ok = true;
            ok = ok && AddShift(dict, night_shift_1);
            ok = ok && AddShift(dict, night_shift_2);
            ok = ok && AddShift(dict, night_shift_3);
            ok = ok && AddShift(dict, night_shift_4);
            ok = ok && AddShift(dict, night_shift_5);
            ok = ok && AddShift(dict, night_shift_6);
            ok = ok && AddShift(dict, night_shift_7);
            ok = ok && AddShift(dict, night_shift_8);
            ok = ok && AddShift(dict, night_shift_9);
            ok = ok && AddShift(dict, night_shift_10);

            if (!ok) {
                WidgetUtils.ShowWarningPopUp("班次与批次号对应配置格式错误！请参照“班次=批次号=报警数量”的格式填写。");
                return null;
            }

            return dict;
        }

        private bool AddShift(Dictionary<string, string> dict, string shift) {
            if (!string.IsNullOrEmpty(shift)) {
                string[] strings = shift.Trim().Split("=");
                if (strings.Length != 3) {
                    return false;
                } else {
                    dict.Add($"{strings[1].Trim()},{strings[2].Trim()}", Encoding.UTF8.GetString(Encoding.GetEncoding(CharSet).GetBytes(strings[0].Trim())));
                }
            }

            return true;
        }
    }
}
