namespace OperationGuidance_new.Constants {
    public class DeviceType_Tool {
        public static List<DeviceTool> Elements = new();
        private static T AddNew<T>() where T: DeviceTool, new() {
            T type = new();
            Elements.Add(type);
            return type;
        }

        public static DeviceCategory Category = DeviceCategories.TOOL;
        public static ToolPF4000 PF4000 { get; } = AddNew<ToolPF4000>();
        public static ToolPF6000OP PF6000_OP { get; } = AddNew<ToolPF6000OP>();
        public static ToolSudongX7 SudongX7 { get; } = AddNew<ToolSudongX7>();

        public static DeviceTool? GetById(int id) {
            foreach (DeviceTool type in Elements) {
                if (type.Id == id) {
                    return type;
                }
            }
            return null;
        }
        public static string? GetNameById(int id) {
            foreach (DeviceTool type in Elements) {
                if (type.Id == id) {
                    return type.Name;
                }
            }
            return null;
        }
        public static int? GetIdByName(string name) {
            foreach (DeviceTool type in Elements) {
                if (type.Name == name) {
                    return type.Id;
                }
            }
            return null;
        }
    }

    public abstract class DeviceTool: DeviceTypeBase {
        public Command? COMMAND_CONNECT_ASCII;
        public Command? COMMAND_DATA_ASCII;
        public Command? COMMAND_HEART_ASCII;
        public Command? COMMAND_LOCK_ASCII;
        public Command? COMMAND_UNLOCK_ASCII;
        public Command? COMMAND_PSET_ASCII;
        public Command? COMMAND_SEND_BARCODE_ASCII;
        public DeviceTool(int id, string name) : base(id, name) { }

        public abstract string? AnalyzeData(string dataMessage, Action<TighteningData>? actionAfterAnalysis = null);
        public abstract string GetMidFromResult(string result);
    }

    public class ToolPF4000: DeviceTool {
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
                    CellID = int.Parse(dataMessage.Substring(22, 4)),
                    ChannelID = int.Parse(dataMessage.Substring(28, 2)),
                    TorqueControllerName = dataMessage.Substring(32, 25),
                    VINNumber = dataMessage.Substring(59, 25),
                    JobID = int.Parse(dataMessage.Substring(86, 4)),
                    ParameterSetNumber = int.Parse(dataMessage.Substring(92, 3)),

                    Strategy = int.Parse(dataMessage.Substring(97, 2)),
                    StrategyOptions = int.Parse(dataMessage.Substring(101, 5)),

                    BatchSize = int.Parse(dataMessage.Substring(108, 4)),
                    BatchCounter = int.Parse(dataMessage.Substring(114, 4)),

                    TighteningStatus = int.Parse(dataMessage.Substring(120, 1)),
                    BatchStatus = int.Parse(dataMessage.Substring(123, 1)),
                    TorqueStatus = int.Parse(dataMessage.Substring(126, 1)),
                    AngleStatus = int.Parse(dataMessage.Substring(129, 1)),
                    RundownAngleStatus = int.Parse(dataMessage.Substring(132, 1)),
                    CurrentMonitoringStatu = int.Parse(dataMessage.Substring(135, 1)),
                    SelfTapStatus = int.Parse(dataMessage.Substring(138, 1)),
                    PrevailTorqueMonitoringStatus = int.Parse(dataMessage.Substring(141, 1)),
                    PrevailTorqueCompensateStatus = int.Parse(dataMessage.Substring(144, 1)),
                    TighteningErrorStatus = int.Parse(dataMessage.Substring(147, 10)),

                    TorqueMinLimit = float.Parse(dataMessage.Substring(159, 6)) / 100,
                    TorqueMaxLimit = float.Parse(dataMessage.Substring(167, 6)) / 100,
                    TorqueFinalTarget = float.Parse(dataMessage.Substring(175, 6)) / 100,
                    Torque = float.Parse(dataMessage.Substring(183, 6)) / 100,

                    AngleMin = int.Parse(dataMessage.Substring(191, 5)),
                    AngleMax = int.Parse(dataMessage.Substring(198, 5)),
                    AngleFinalTarget = int.Parse(dataMessage.Substring(205, 5)),
                    Angle = int.Parse(dataMessage.Substring(212, 5)),

                    RundownAngleMin = int.Parse(dataMessage.Substring(219, 5)),
                    RundownAngleMax = int.Parse(dataMessage.Substring(226, 5)),
                    RundownAngle = int.Parse(dataMessage.Substring(233, 5)),

                    CurrentMonitoringMin = int.Parse(dataMessage.Substring(240, 3)),
                    CurrentMonitoringMax = int.Parse(dataMessage.Substring(245, 3)),
                    CurrentMonitoringValue = int.Parse(dataMessage.Substring(250, 3)),

                    SelfTapMin = float.Parse(dataMessage.Substring(255, 6)) / 100,
                    SelfTapMax = float.Parse(dataMessage.Substring(263, 6)) / 100,
                    SelfTapTorque = float.Parse(dataMessage.Substring(271, 6)) / 100,

                    PrevailTorqueMonitoringMin = float.Parse(dataMessage.Substring(279, 6)) / 100,
                    PrevailTorqueMonitoringMax = float.Parse(dataMessage.Substring(287, 6)) / 100,
                    PrevailTorque = float.Parse(dataMessage.Substring(295, 6)) / 100,

                    TighteningID = int.Parse(dataMessage.Substring(303, 10)),
                    JobSequenceNumber = int.Parse(dataMessage.Substring(315, 5)),
                    SyncTighteningID = int.Parse(dataMessage.Substring(322, 5)),
                    ToolSerialNumber = dataMessage.Substring(329, 14),

                    TimeStamp = dataMessage.Substring(345, 19),
                    DateOrTimeOfLastChangeInParameterSetSettings = dataMessage.Substring(366, 19),

                    ParameterSetName = dataMessage.Substring(387, 25),
                    TorqueValuesUnit = int.Parse(dataMessage.Substring(414, 1)),
                    ResultType = int.Parse(dataMessage.Substring(417, 2)),
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

    public class ToolPF6000OP: DeviceTool {
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

    public class ToolSudongX7: DeviceTool {
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
