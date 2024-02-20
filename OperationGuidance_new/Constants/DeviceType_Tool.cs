namespace OperationGuidance_new.Constants {
    public class DeviceType_Tool {
        public static List<DeviceTypeTool> Elements = new();
        private static T AddNew<T>() where T: DeviceTypeTool, new() {
            T type = new();
            Elements.Add(type);
            return type;
        }

        public static DeviceCategory Category = DeviceCategories.TOOL;
        public static ToolPF4000 PF4000 { get; } = AddNew<ToolPF4000>();
        public static ToolPF6000OP PF6000_OP { get; } = AddNew<ToolPF6000OP>();
        public static ToolSudongX7 SudongX7 { get; } = AddNew<ToolSudongX7>();

        public static DeviceTypeTool? GetById(int id) {
            foreach (DeviceTypeTool type in Elements) {
                if (type.Id == id) {
                    return type;
                }
            }
            return null;
        }
        public static string? GetNameById(int id) {
            foreach (DeviceTypeTool type in Elements) {
                if (type.Id == id) {
                    return type.Name;
                }
            }
            return null;
        }
        public static int? GetIdByName(string name) {
            foreach (DeviceTypeTool type in Elements) {
                if (type.Name == name) {
                    return type.Id;
                }
            }
            return null;
        }
    }

    public abstract class DeviceTypeTool: DeviceTypeBase {
        public Command? COMMAND_CONNECT_ASCII;
        public Command? COMMAND_DATA_ASCII;
        public Command? COMMAND_HEART_ASCII;
        public Command? COMMAND_LOCK_ASCII;
        public Command? COMMAND_UNLOCK_ASCII;
        public Command? COMMAND_PSET_ASCII;
        public Command? COMMAND_SEND_BARCODE_ASCII;
        public DeviceTypeTool(int id, string name) : base(id, name) { }

        public abstract string? AnalyzeData(string dataMessage, Action<TighteningData>? actionAfterAnalysis = null);
        public abstract string GetMidFromResult(string result);
    }

    public class ToolPF4000: DeviceTypeTool {
        public ToolPF4000() : base(1, "PF4000") {
            COMMAND_CONNECT_ASCII           = new("00200001003         \x00");
            COMMAND_DATA_ASCII              = new("002000600031        \x00");
            COMMAND_HEART_ASCII             = new("00209999001         \x00");
            COMMAND_LOCK_ASCII              = new("00200042001         \x00");
            COMMAND_UNLOCK_ASCII            = new("00200043001         \x00");
            COMMAND_PSET_ASCII              = new("00230018001         {0}\x00");
            COMMAND_SEND_BARCODE_ASCII      = new("002801500010    00  {0}\x00");
        }

        public override string? AnalyzeData(string dataMessage, Action<TighteningData>? actionAfterAnalysis = null) {
            string? result = null;
            string mid = GetMidFromResult(dataMessage);
            if (mid == "9999") { // Skip the heart beating result
                result = null;
            } else if (mid == "0061") {
                result = null;
                TighteningData tighteningData = new() {
                    cell_id = int.Parse(dataMessage.Substring(22, 4)),
                    channel_id = int.Parse(dataMessage.Substring(28, 2)),
                    torque_controller_name = dataMessage.Substring(32, 25),
                    vin_number = dataMessage.Substring(59, 25),
                    job_id = int.Parse(dataMessage.Substring(86, 4)),
                    parameter_set_number = int.Parse(dataMessage.Substring(92, 3)),

                    strategy = int.Parse(dataMessage.Substring(97, 2)),
                    strategy_options = int.Parse(dataMessage.Substring(101, 5)),

                    batch_size = int.Parse(dataMessage.Substring(108, 4)),
                    batch_counter = int.Parse(dataMessage.Substring(114, 4)),

                    tightening_status = int.Parse(dataMessage.Substring(120, 1)),
                    batch_status = int.Parse(dataMessage.Substring(123, 1)),
                    torque_status = int.Parse(dataMessage.Substring(126, 1)),
                    angle_status = int.Parse(dataMessage.Substring(129, 1)),
                    rundown_status = int.Parse(dataMessage.Substring(132, 1)),
                    current_monitoring_status = int.Parse(dataMessage.Substring(135, 1)),
                    self_tap_status = int.Parse(dataMessage.Substring(138, 1)),
                    prevail_torque_monitoring_status = int.Parse(dataMessage.Substring(141, 1)),
                    prevail_torque_compensate_status = int.Parse(dataMessage.Substring(144, 1)),
                    tightening_error_status = int.Parse(dataMessage.Substring(147, 10)),

                    torque_min_limit = float.Parse(dataMessage.Substring(159, 6)) / 100,
                    torque_max_limit = float.Parse(dataMessage.Substring(167, 6)) / 100,
                    torque_final_target = float.Parse(dataMessage.Substring(175, 6)) / 100,
                    torque = float.Parse(dataMessage.Substring(183, 6)) / 100,

                    angle_min = int.Parse(dataMessage.Substring(191, 5)),
                    angle_max = int.Parse(dataMessage.Substring(198, 5)),
                    angle_final_target = int.Parse(dataMessage.Substring(205, 5)),
                    angle = int.Parse(dataMessage.Substring(212, 5)),

                    rundown_angle_min = int.Parse(dataMessage.Substring(219, 5)),
                    rundown_angle_max = int.Parse(dataMessage.Substring(226, 5)),
                    rundown_angle = int.Parse(dataMessage.Substring(233, 5)),

                    current_monitoring_min = int.Parse(dataMessage.Substring(240, 3)),
                    current_monitoring_max = int.Parse(dataMessage.Substring(245, 3)),
                    current_monitoring_value = int.Parse(dataMessage.Substring(250, 3)),

                    self_tap_min = float.Parse(dataMessage.Substring(255, 6)) / 100,
                    self_tap_max = float.Parse(dataMessage.Substring(263, 6)) / 100,
                    self_tap_torque = float.Parse(dataMessage.Substring(271, 6)) / 100,

                    prevail_torque_monitoring_min = float.Parse(dataMessage.Substring(279, 6)) / 100,
                    prevail_torque_monitoring_max = float.Parse(dataMessage.Substring(287, 6)) / 100,
                    prevail_torque = float.Parse(dataMessage.Substring(295, 6)) / 100,

                    tightening_id = int.Parse(dataMessage.Substring(303, 10)),
                    job_sequence_number = int.Parse(dataMessage.Substring(315, 5)),
                    sync_tightening_id = int.Parse(dataMessage.Substring(322, 5)),
                    tool_serial_number = dataMessage.Substring(329, 14),

                    timestamp = dataMessage.Substring(345, 19),
                    date_or_time_of_last_change_in_parameter_set_settings = dataMessage.Substring(366, 19),

                    parameter_set_name = dataMessage.Substring(387, 25),
                    torque_values_unit = int.Parse(dataMessage.Substring(414, 1)),
                    result_type = int.Parse(dataMessage.Substring(417, 2)),
                };
                if (actionAfterAnalysis != null) {
                    actionAfterAnalysis(tighteningData);
                }
            } else {
                result = dataMessage;
            }
            return result;
        }
        public override string GetMidFromResult(string result) {
            string mid = "";
            try {
                mid = result.Substring(4, 4);
            } catch (Exception e) {
                System.Console.WriteLine($"Get mid failed from result message: {result}");
            }
            return mid;
        }
    }

    public class ToolPF6000OP: DeviceTypeTool {
        public ToolPF6000OP() : base(2, "PF6000-OP") {
            COMMAND_CONNECT_ASCII           = new("00200001006         \x00");
            COMMAND_DATA_ASCII              = new("002000600031        \x00");
            COMMAND_HEART_ASCII             = new("00209999001         \x00");
            COMMAND_LOCK_ASCII              = new("00200042001         \x00");
            COMMAND_UNLOCK_ASCII            = new("00200043001         \x00");
            COMMAND_PSET_ASCII              = new("00230018001         {0}\x00");
            COMMAND_SEND_BARCODE_ASCII      = new("002801500010    00  {0}\x00");
        }

        public override string? AnalyzeData(string dataMessage, Action<TighteningData>? actionAfterAnalysis = null) {
            string? result = null;
            string mid = GetMidFromResult(dataMessage);
            if (mid == "9999") { // Skip the heart beating result
                result = null;
            } else if (mid == "0061") {
                result = null;
            } else {
                result = dataMessage;
            }
            return result;
        }
        public override string GetMidFromResult(string result) {
            string mid = "";
            try {
                mid = result.Substring(4, 4);
            } catch (Exception e) {
                System.Console.WriteLine($"Get mid failed from result message: {result}");
            }
            return mid;
        }
    }

    public class ToolSudongX7: DeviceTypeTool {
        public ToolSudongX7() : base(3, "SudongX7") { }

        public override string? AnalyzeData(string dataMessage, Action<TighteningData>? actionAfterAnalysis = null) {
            string? result = null;
            return result;
        }
        public override string GetMidFromResult(string result) {
            string mid = "";
            return mid;
        }
    }
}
