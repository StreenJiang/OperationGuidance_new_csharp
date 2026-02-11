using OperationGuidance_new.Utils;
using System.Text;

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

    public abstract class DeviceTypeTool: DeviceTypeBase {
        public Command COMMAND_LOCK_ASCII;
        public Command COMMAND_UNLOCK_ASCII;
        protected Command COMMAND_PSET_ASCII;
        public Command? COMMAND_SEND_BARCODE_ASCII;
        public DeviceTypeTool(int id, string name) : base(id, name) {
            logger = MainUtils.GetLogger(GetType());
        }

        public abstract string GetPSetCommand(int pSetNumber);

        public abstract void AnalyzeData(byte[] msgBytes, Action<bool?, bool?, bool?, bool?, bool?> toolAction, Action<TighteningData, int>? actionAfterAnalysis = null, Action<CurveDataTemp, int>? _actionAfterCurveDataReceived = null, int? deviceId = null);
    }

    public abstract class ToolPFSeries: DeviceTypeTool {
        public Command? COMMAND_CONNECT_ASCII;
        public Command COMMAND_HEART_ASCII;
        public Command COMMAND_DATA_ASCII;
        public Command COMMAND_CURVE_ASCII;
        public Command COMMAND_CURVE_ACK_ASCII;

        public ToolPFSeries(int id, string name) : base(id, name) {
            COMMAND_HEART_ASCII = new("00209999001         \x00");
            COMMAND_DATA_ASCII = new("002000600031        \x00");
            COMMAND_CURVE_ASCII = new("006700080010    00  09000013800000000000000000000000000000002002001\x00");
            COMMAND_CURVE_ACK_ASCII = new("002400050010    00  0900\x00");
            COMMAND_LOCK_ASCII = new("00200042001         \x00");
            COMMAND_UNLOCK_ASCII = new("00200043001         \x00");
            COMMAND_PSET_ASCII = new("00230018001         {0}\x00");
            COMMAND_SEND_BARCODE_ASCII = new("002801500010    00  {0}\x00");
        }

        public override string GetPSetCommand(int pSetNumber) => COMMAND_PSET_ASCII.GetMessage($"{pSetNumber:000}");

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
                char[] chars = result.ToArray();
                tail = new String(chars.Skip(chars.Length - 4).ToArray());
            } catch (Exception e) {
                logger.Warn($"Get tail failed from result message = {result}, e = {e}");
            }
            return tail;
        }

        public override void AnalyzeData(byte[] msgBytes, Action<bool?, bool?, bool?, bool?, bool?> toolAction, Action<TighteningData, int>? actionAfterAnalysis = null, Action<CurveDataTemp, int>? _actionAfterCurveDataReceived = null, int? deviceId = null) {
            string dataMessageTemp = Encoding.ASCII.GetString(msgBytes);
            logger.Info($"Analyzing data from {this.Name}: [{dataMessageTemp}]");


            // Analyze msg one by one
            string[] msgs = dataMessageTemp.Split('\0');
            bool foundTighteningData = false;
            for (int i = 0; i < msgs.Length; i++) {
                string msg = msgs[i];
                if (_getLengthOfOne(msg) > 0) {
                    if (GetMid(msg) == "0900") {
                        if (i == 0) {
                            _analyzeData(msg, msgBytes);
                        } else {
                            List<byte> bytes = msgBytes.ToList();

                            int index = -1;
                            do {
                                index = bytes.IndexOf(0);
                                if (index > 0) {
                                    string msgTemp = Encoding.ASCII.GetString(bytes.Take(index).ToArray());
                                    if (GetMid(msgTemp) == "0900") {
                                        _analyzeData(msgTemp, bytes.Skip(index).Take(bytes.Count).ToArray());
                                        logger.Info("Curve data analysis done ...........");
                                        break;
                                    }
                                }

                                bytes = bytes.Take(index).ToList();
                                logger.Info("Curve data looping ...........");
                            } while (index > 0);
                        }

                        break;
                    } else {
                        if (GetMid(msg) == "0061") {
                            // 如果在同一个消息中找到多个拧紧数据，跳过后面的
                            // 正常情况下不可能存在一次信息有多个拧紧数据
                            // 因为拧紧数据处理完一次才会切换点位、下发程序号、解锁枪等
                            // 一个消息中存在多个数据很可能是控制器出错导致多发
                            if (foundTighteningData) {
                                continue;
                            }
                            foundTighteningData = true;
                        }
                        _analyzeData(msg);
                    }
                }
            }

            // Inner method that check and count length of msg
            int _getLengthOfOne(string msg) {
                if (msg.Length >= 4) {
                    try {
                        return int.Parse(new String(msg.Take(4).ToArray()));
                    } catch (FormatException fe) {
                        logger.Warn($"Exception occurred while checking length of message got from controller: msg = {msg}, e = {fe}");
                        return 0;
                    }
                }
                return 0;
            }

            // Inner method that do analyze
            void _analyzeData(string dataMessage, byte[]? cureData = null) {
                logger.Info($"Analyzing each data for {this.Name}: [{dataMessage}]");

                string mid = GetMid(dataMessage);
                if (mid == "9999") {
                    logger.Info($"Heart beating for {this.Name}...");
                    toolAction(true, null, null, null, null);
                } else if (mid == "0005") {
                    string tail = GetTail(dataMessage);
                    if (tail == "0018") {
                        logger.Info($"Pset sending ok for {this.Name}...");
                        toolAction(null, true, null, null, null);
                    } else if (tail == "0042") {
                        logger.Info($"Lock ok for {this.Name}...");
                        toolAction(null, null, true, null, null);
                    } else if (tail == "0043") {
                        logger.Info($"Unlock ok for {this.Name}...");
                        toolAction(null, null, false, null, null);
                    }
                } else if (mid == "0061") {
                    logger.Info($"Analyzing tightening data...");

                    toolAction(null, null, null, true, null);
                    if (actionAfterAnalysis != null) {
                        if (deviceId == null) {
                            string errorMsg = $"[Device] id can not be null while [actionAfterAnalysis] is not null.";
                            logger.Error(errorMsg);
                            throw new NullReferenceException(errorMsg);
                        }

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

                        actionAfterAnalysis(tighteningData, deviceId.Value);
                    }
                } else if (mid == "0900") {
                    toolAction(null, null, null, null, true);
                    if (_actionAfterCurveDataReceived != null && cureData != null) {
                        if (deviceId == null) {
                            string errorMsg = $"[Device] id can not be null while [actionAfterCurveDataReceived] is not null.";
                            logger.Error(errorMsg);
                            throw new NullReferenceException(errorMsg);
                        }

                        string header = Encoding.ASCII.GetString(cureData.Take(cureData.ToList().IndexOf(0)).ToArray());
                        string id = new(header.Skip(20).Take(10).ToArray());
                        string time = $"{new(header.Skip(30).Take(10).ToArray())} {new(header.Skip(41).Take(8).ToArray())}";
                        string dataType = new(header.Skip(52).Take(2).ToArray());
                        byte[] dataBytes = cureData.Skip(header.Length + 1).ToArray();
                        double coefficient = double.Parse(new(header.Skip(header.IndexOf("02214") + 17).Take(9).ToArray()));
                        int decimals = int.Parse(dataType) == (int) CurveDataType.TORQUE ? 2 : 0;
                        double[] values = AnalyseCurveData(dataBytes, coefficient, decimals);

                        CurveDataTemp curveData = new(id, time, int.Parse(dataType), string.Join(",", values));

                        _actionAfterCurveDataReceived(curveData, deviceId.Value);
                    }
                }
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
                    double valueTemp = (int) Math.Pow(2, 16) - value;
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

    public class ToolPF4000: ToolPFSeries {
        public ToolPF4000() : base(1, "PF4000") {
            COMMAND_CONNECT_ASCII = new("00200001003         \x00");
        }
    }

    public class ToolPF6000OP: ToolPFSeries {
        public ToolPF6000OP() : base(2, "PF6000-OP") {
            COMMAND_CONNECT_ASCII = new("00200001006         \x00");
        }
    }

    public class ToolSudongX7: DeviceTypeTool {
        public string PSET_OK = "55AA058205B9760D0A";

        public ToolSudongX7() : base(3, "SudongX7") {
            COMMAND_LOCK_ASCII = new("55AA070100020000000D0A");
            COMMAND_UNLOCK_ASCII = new("55AA070100000000000D0A");
            COMMAND_PSET_ASCII = new("55AA070205{0}");
        }

        public string GetLockCommand() {
            string cmd = COMMAND_LOCK_ASCII.GetMessage();
            String CrcStr = MainUtils.Crc16ToString(MainUtils.ToBytes(cmd));
            return cmd + CrcStr + "0D0A";
        }

        public string GetUnlockCommand() {
            string cmd = COMMAND_UNLOCK_ASCII.GetMessage();
            String CrcStr = MainUtils.Crc16ToString(MainUtils.ToBytes(cmd));
            return cmd + CrcStr + "0D0A";
        }

        public override string GetPSetCommand(int pSetNumber) {
            string psetCommand = COMMAND_PSET_ASCII.GetMessage($"{pSetNumber:0000}");
            String CrcStr = MainUtils.Crc16ToString(MainUtils.ToBytes(psetCommand));
            return psetCommand + CrcStr + "0D0A";
        }

        public override void AnalyzeData(byte[] msgBytes, Action<bool?, bool?, bool?, bool?, bool?> toolAction, Action<TighteningData, int>? actionAfterAnalysis = null, Action<CurveDataTemp, int>? _actionAfterCurveDataReceived = null, int? deviceId = null) {
            string dataMessage = MainUtils.ToHexString(msgBytes);
            string head = GetHead(dataMessage);
            logger.Info($"dataMessage = {dataMessage}");

            if (dataMessage == PSET_OK) {
                toolAction(null, true, null, null, null);
            } else if (head == "55AA2781") {
                toolAction(null, null, null, true, null);
                if (actionAfterAnalysis != null) {
                    if (deviceId == null) {
                        string errorMsg = $"[Device] id can not be null while [actionAfterAnalysis] is not null.";
                        logger.Error(errorMsg);
                        throw new NullReferenceException(errorMsg);
                    }

                    // Unit of torque
                    int unit = GetIntData(GetData(dataMessage, 12, 2));

                    // Use 1000 as divisor to always get N.m as unit
                    int divisor = 1000;
                    if (unit == 0) {
                        divisor = 100;
                    }
                    float torque = (float) GetIntData(GetData(dataMessage, 14, 4)) / divisor;
                    float torqueMin = (float) GetIntData(GetData(dataMessage, 52, 4)) / divisor;
                    float torqueMax = (float) GetIntData(GetData(dataMessage, 48, 4)) / divisor;
                    int torqueStatus = (int) TighteningCommonStatus.OK;
                    if (torque < torqueMin) {
                        torqueStatus = (int) TighteningCommonStatus.LOW;
                    } else if (torque > torqueMax) {
                        torqueStatus = (int) TighteningCommonStatus.HIGH;
                    }

                    int angle = GetIntData(GetData(dataMessage, 26, 4));
                    int angleMax = GetIntData(GetData(dataMessage, 56, 4));
                    int angleMin = GetIntData(GetData(dataMessage, 60, 4));
                    int angleStatus = (int) TighteningCommonStatus.OK;
                    if (angle < angleMin) {
                        angleStatus = (int) TighteningCommonStatus.LOW;
                    } else if (angle > angleMax) {
                        angleStatus = (int) TighteningCommonStatus.HIGH;
                    }

                    int rundownAngle = GetIntData(GetData(dataMessage, 22, 4));
                    int rundownAngleMax = GetIntData(GetData(dataMessage, 64, 4));
                    int rundownAngleMin = GetIntData(GetData(dataMessage, 68, 4));
                    int rundownAngleStatus = (int) TighteningCommonStatus.OK;
                    if (rundownAngle < rundownAngleMin) {
                        rundownAngleStatus = (int) TighteningCommonStatus.LOW;
                    } else if (rundownAngle > rundownAngleMax) {
                        rundownAngleStatus = (int) TighteningCommonStatus.HIGH;
                    }

                    string tighteningStatusTemp = GetData(dataMessage, 42, 2);
                    int tighteningStatus;
                    if (tighteningStatusTemp == "01" || tighteningStatusTemp == "04") {
                        // 04 equals to CCW, so it's ok to be OK
                        tighteningStatus = (int) TighteningStatus.OK;
                    } else {
                        tighteningStatus = (int) TighteningStatus.NG;
                    }
                    int tighteningErrorStatus = GetIntData(GetData(dataMessage, 44, 2));

                    string resultTypeTemp = GetData(dataMessage, 34, 2);
                    int resultType;
                    if (resultTypeTemp == "00") {
                        resultType = (int) TightenOrLoosen.TIGHTENING;
                    } else {
                        resultType = (int) TightenOrLoosen.LOOSENING;
                    }

                    int runDownTime = GetIntData(GetData(dataMessage, 30, 4));

                    TighteningData tighteningData = new() {
                        tightening_status = tighteningStatus,
                        tightening_error_status = tighteningErrorStatus,
                        torque_status = torqueStatus,
                        angle_status = angleStatus,
                        rundown_status = rundownAngleStatus,

                        torque_min_limit = torqueMin,
                        torque_max_limit = torqueMax,
                        torque = torque,

                        angle_min = angleMin,
                        angle_max = angleMax,
                        angle = angle,

                        rundown_angle_min = rundownAngleMin,
                        rundown_angle_max = rundownAngleMax,
                        rundown_angle = rundownAngle,

                        result_type = resultType,
                        rundown_time = runDownTime,
                    };

                    actionAfterAnalysis(tighteningData, deviceId.Value);
                }

                // } else if (head == "0900") {
                //     toolAction(null, null, null, null, true);
                //     if (_actionAfterCurveDataReceived != null) {
                //         if (deviceId == null) {
                //             string errorMsg = $"[Device] id can not be null while [actionAfterCurveDataReceived] is not null.";
                //             logger.Error(errorMsg);
                //             throw new NullReferenceException(errorMsg);
                //         }
                //
                //         string header = Encoding.ASCII.GetString(msgBytes.Take(msgBytes.ToList().IndexOf(0)).ToArray());
                //         string id = new(header.Skip(20).Take(10).ToArray());
                //         string time = $"{new(header.Skip(30).Take(10).ToArray())} {new(header.Skip(41).Take(8).ToArray())}";
                //         string dataType = new(header.Skip(52).Take(2).ToArray());
                //         byte[] dataBytes = msgBytes.Skip(header.Length + 1).ToArray();
                //         double coefficient = double.Parse(new(header.Skip(header.IndexOf("02214") + 17).Take(9).ToArray()));
                //         int decimals = int.Parse(dataType) == (int) CurveDataType.TORQUE ? 2 : 0;
                //         double[] values = AnalyseCurveData(dataBytes, coefficient, decimals);
                //
                //         CurveDataTemp curveData = new(id, time, int.Parse(dataType), string.Join(",", values));
                //
                //         _actionAfterCurveDataReceived(curveData, deviceId.Value);
                //     }
            }

            string GetData(string dataMessage, int start, int len) => new(dataMessage.Skip(start).Take(len).ToArray());
            int GetIntData(string dataStr) {
                string low = new(dataStr.Take(2).ToArray());
                string high = new(dataStr.Skip(2).Take(2).ToArray());
                return MainUtils.ToIntByHexString(high + low);
            }
        }

        public virtual string GetHead(string result) {
            string head = "";
            try {
                head = new(result.Take(8).ToArray());
            } catch (Exception e) {
                logger.Warn($"Get head failed from result message = {result}, e = {e}");
            }
            return head;
        }
    }
}
