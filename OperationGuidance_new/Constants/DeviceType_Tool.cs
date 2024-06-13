using System.Text;
using OperationGuidance_new.Utils;

namespace OperationGuidance_new.Constants {
    public class DeviceType_Tool {
        public static List<DeviceTypeTool> Elements = new();
        private static T AddNew<T>() where T : DeviceTypeTool, new() {
            T type = new();
            if (Elements.Find(e => e.Id == type.Id) != null) {
                throw new InvalidDataException($"Duplicated Id for type {typeof(DeviceType_Tool).Name}");
            }
            Elements.Add(type);
            return type;
        }

        public static ToolPF4000 PF4000 { get; } = AddNew<ToolPF4000>();
        public static ToolPF6000OP PF6000_OP { get; } = AddNew<ToolPF6000OP>();
        public static ToolSudongX7 SudongX7 { get; } = AddNew<ToolSudongX7>();

        public static DeviceTypeTool GetById(int id) {
            foreach (DeviceTypeTool type in Elements) {
                if (type.Id == id) {
                    return type;
                }
            }
            throw new NullReferenceException($"Can't find type of tool by type_id = {id}");
        }
        public static string GetNameById(int id) {
            foreach (DeviceTypeTool type in Elements) {
                if (type.Id == id) {
                    return type.Name;
                }
            }
            throw new NullReferenceException($"Can't find type of tool by type_id = {id}");
        }
        public static int GetIdByName(string name) {
            foreach (DeviceTypeTool type in Elements) {
                if (type.Name == name) {
                    return type.Id;
                }
            }
            throw new NullReferenceException($"Can't find type of tool by type_name = {name}");
        }
    }

    public abstract class DeviceTypeTool : DeviceTypeBase {
        public Command COMMAND_LOCK_ASCII;
        public Command COMMAND_UNLOCK_ASCII;
        public Command COMMAND_PSET_ASCII;
        public Command? COMMAND_SEND_BARCODE_ASCII;
        public DeviceTypeTool(int id, string name) : base(id, name) {
            logger = MainUtils.GetLogger(GetType());
        }

        public abstract void AnalyzeData(byte[] msgBytes, Action<bool?, bool?, bool?, bool?, bool?> toolAction, Action<TighteningData, int>? actionAfterAnalysis = null, int? deviceId = null);
    }

    public abstract class ToolPFSeries : DeviceTypeTool {
        public Command? COMMAND_CONNECT_ASCII;
        public Command COMMAND_HEART_ASCII;
        public Command COMMAND_DATA_ASCII;
        public Command COMMAND_DATA_ACK_ASCII;
        public Command COMMAND_CURVE_ASCII;
        public Command COMMAND_CURVE_ACK_ASCII;

        public ToolPFSeries(int id, string name) : base(id, name) {
            COMMAND_HEART_ASCII = new("00209999001         \x00");
            COMMAND_DATA_ASCII = new("002000600031        \x00");
            COMMAND_DATA_ACK_ASCII = new("00200062001         \x00");
            COMMAND_CURVE_ASCII = new("006700080010    00  09000013800000000000000000000000000000002002001\x00");
            COMMAND_CURVE_ACK_ASCII = new("002400050010    00  0900\x00");
            COMMAND_LOCK_ASCII = new("00200042001         \x00");
            COMMAND_UNLOCK_ASCII = new("00200043001         \x00");
            COMMAND_PSET_ASCII = new("00230018001         {0}\x00");
            COMMAND_SEND_BARCODE_ASCII = new("002801500010    00  {0}\x00");
        }

        public virtual string GetMid(string result) {
            string mid = "";
            try {
                mid = result.Substring(4, 4);
            } catch (Exception e) {
                logger.Warn($"Get mid failed from result message = {result}, e = {e}");
            }
            return mid;
        }

        public string GetTail(string result) {
            string tail = "";
            try {
                tail = new string(result.Skip(result.Length - 5).Take(4).ToArray());
            } catch (Exception e) {
                logger.Warn($"Get tail failed from result message = {result}, e = {e}");
            }
            return tail;
        }

        public override void AnalyzeData(byte[] msgBytes, Action<bool?, bool?, bool?, bool?, bool?> toolAction, Action<TighteningData, int>? actionAfterAnalysis = null, int? deviceId = null) {
            string dataMessage = Encoding.ASCII.GetString(msgBytes);
            string mid = GetMid(dataMessage);

            if (mid == "9999") {
                toolAction(true, null, null, null, null);
            } else if (mid == "0005") {
                string tail = GetTail(dataMessage);
                if (tail == "0018") {
                    toolAction(null, true, null, null, null);
                } else if (tail == "0042") {
                    toolAction(null, null, true, null, null);
                } else if (tail == "0043") {
                    toolAction(null, null, false, null, null);
                }
            } else if (mid == "0061") {
                toolAction(null, null, null, true, null);
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
                    if (deviceId == null) {
                        string errorMsg = $"[Device] id can not be null while [actionAfterAnalysis] is not null.";
                        logger.Error(errorMsg);
                        throw new NullReferenceException(errorMsg);
                    }
                    actionAfterAnalysis(tighteningData, deviceId.Value);
                }
            } else if (mid == "0900") {
                toolAction(null, null, null, null, true);
                string header = Encoding.ASCII.GetString(msgBytes.Take(msgBytes.ToList().IndexOf(0)).ToArray());
                string id = new string(header.Skip(20).Take(10).ToArray());
                string time = $"{new string(header.Skip(30).Take(10).ToArray())} {new string(header.Skip(41).Take(8).ToArray())}";
                string dataType = new string(header.Skip(53).Take(3).ToArray());
                byte[] dataBytes = msgBytes.Skip(header.Length + 1).ToArray();
                double coefficient = double.Parse(new string(header.Skip(header.IndexOf("02214") + 17).Take(9).ToArray()));
                int decimals = int.Parse(dataType) == (int)CurveDataType.TORQUE ? 2 : 0;
                double[] values = AnalyseCurveData(dataBytes, coefficient, decimals);
                System.Console.WriteLine($"Curve data received, header = {header}");
                System.Console.WriteLine($"dataType = {dataType} - {Enum.ToObject(typeof(CurveDataType), int.Parse(dataType))}");
                System.Console.WriteLine($"values count = {values.Count()}");
                System.Console.WriteLine($"values = {string.Join(", ", values)}");
            }
        }

        double[] AnalyseCurveData(byte[] dataBytes, double coefficient, int decimals) {
            List<double> results = new();

            int singleDataLength = 2;
            for (int i = 0; i < dataBytes.Length; i += singleDataLength) {
                // Take value
                byte[] bytes = dataBytes.Skip(i).Take(singleDataLength).ToArray();
                double value = MainUtils.ToIntByHexString(MainUtils.ToHexString(bytes));

                // Check if is negative value
                if (value > Math.Pow(2, 15) - 1) {
                    double valueTemp = (int)Math.Pow(2, 16) - value;
                    value = 0 - valueTemp;
                }

                // Calculate to accurate value
                value = Math.Round(value * coefficient, decimals);
                if (value == 0) {
                    value = 0;
                }

                // Add to list
                results.Add(value);
            }

            return results.ToArray();
        }
    }

    public class ToolPF4000 : ToolPFSeries {
        public ToolPF4000() : base(1, "PF4000") {
            COMMAND_CONNECT_ASCII = new("00200001003         \x00");
        }
    }

    public class ToolPF6000OP : ToolPFSeries {
        public ToolPF6000OP() : base(2, "PF6000-OP") {
            COMMAND_CONNECT_ASCII = new("00200001006         \x00");
        }
    }

    public class ToolSudongX7 : DeviceTypeTool {
        public ToolSudongX7() : base(3, "SudongX7") { }

        public override void AnalyzeData(byte[] msgBytes, Action<bool?, bool?, bool?, bool?, bool?> toolAction, Action<TighteningData, int>? actionAfterAnalysis = null, int? deviceId = null) { }

        public bool SendPsetOk(string result) {
            return false;
        }
    }
}
