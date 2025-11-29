using OperationGuidance_new.Configs.DTOs;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Constants;
using System.Text;

namespace OperationGuidance_new.Constants {
    public class DeviceType_IoBox {
        public static List<DeviceTypeBase> Elements = new();
        private static T AddNew<T>() where T : DeviceTypeBase, new() {
            T type = new();
            if (Elements.Find(e => e.Id == type.Id) != null) {
                throw new InvalidDataException($"Duplicated Id for type {typeof(DeviceType_IoBox).Name}");
            }
            Elements.Add(type);
            return type;
        }

        public static IoBoxSetterSelector_4 SetterSelector_4 { get; } = AddNew<IoBoxSetterSelector_4>();
        public static IoBoxSetterSelector_8 SetterSelector_8 { get; } = AddNew<IoBoxSetterSelector_8>();
        public static IoBoxSetterSelector_4_plus SetterSelector_4_plus { get; } = AddNew<IoBoxSetterSelector_4_plus>();
        public static IoBoxArranger Arranger { get; } = AddNew<IoBoxArranger>();

        public static DeviceTypeIoBox GetById(int id) {
            foreach (DeviceTypeIoBox type in Elements) {
                if (type.Id == id) {
                    return type;
                }
            }
            throw new NullReferenceException($"Can't find type of tool by type_id = {id}");
        }
        public static string GetNameById(int id) {
            foreach (DeviceTypeIoBox type in Elements) {
                if (type.Id == id) {
                    return type.Name;
                }
            }
            throw new NullReferenceException($"Can't find type of tool by type_id = {id}");
        }
        public static int GetIdByName(string name) {
            foreach (DeviceTypeIoBox type in Elements) {
                if (type.Name == name) {
                    return type.Id;
                }
            }
            throw new NullReferenceException($"Can't find type of tool by type_name = {name}");
        }
    }

    public abstract class DeviceTypeIoBox: DeviceTypeBase {
        protected string _original_signal = "00000000";
        protected string _current_signal;
        protected Command _command_write;

        public Command COMMAND_READ;
        public DeviceTypeIoBox(int id, string name) : base(id, name) {
            _current_signal = _original_signal;
            COMMAND_READ = new("0903000000044541");
            _command_write = new("0906000000{0}");
        }

        protected abstract string GetCommand();
        public abstract Command GetResetCommand();
        public Command GetResetAllCommand() => new(_original_signal);

        public bool WriteOk(string? resultMsg) {
            string currentCommand = GetCommand();
            bool isOk = !string.IsNullOrEmpty(resultMsg) && currentCommand == resultMsg;
            return isOk;
        }
    }

    public abstract class IoBoxSetterSelector: DeviceTypeIoBox {
        protected int _currentPosition; // Io write
        protected int _currentStatus; // Io read

        public int CurrentStatus => _currentStatus;
        public int SetterNum { get; set; }

        public IoBoxSetterSelector(int id, string name, int setterNum) : base(id, name) {
            SetterNum = setterNum;
        }

        public virtual Command GetWriteCommand(int position) {
            int min = 1;
            int max = SetterNum;
            if (position > max || position < min) {
                string errorMsg = $"Position[{position}] of {Name} can not less then {min} or grater then {max}, please check.";
                logger.Error(errorMsg);
                throw new IndexOutOfRangeException(errorMsg);
            }

            _currentPosition = position;
            return new(GetCommand());
        }
        public override Command GetResetCommand() {
            _currentPosition = 0;
            return new(GetCommand());
        }

        protected override string GetCommand() {
            string high = string.Join("", _current_signal.Take(4));
            string low;
            if (_currentPosition > 0) {
                low = MainUtils.ToBinaryString_half(_currentPosition);
            } else {
                low = "0000";
            }

            _current_signal = high + low;

            string temp = _command_write.GetMessage(MainUtils.ToHexString(_current_signal));
            byte[] bytes = MainUtils.ToBytes(temp);
            return temp + MainUtils.Crc16ToString(bytes);
        }

        public virtual void AnalyzeData(string dataMessage, Action<int>? _ioBoxActionAfterAnalysis) {
            string low = string.Join("", dataMessage.Skip(9).Take(1));
            _currentStatus = MainUtils.ToIntByHexString(low);

            if (_ioBoxActionAfterAnalysis != null) {
                _ioBoxActionAfterAnalysis(_currentStatus);
            }
        }
    }

    public abstract class IoBoxSetterSelectorPlus: IoBoxSetterSelector {
        private int[] _positionsInUse = { 0, 0, 0, 0 };
        private int[] _positionStatuses = { 0, 0, 0, 0 };

        public int CurrentPosition { get => _currentPosition; set => _currentPosition = value; }
        public int[] PositionsInUse { get => _positionsInUse; set => _positionsInUse = value; }
        public int[] PositionStatuses => _positionStatuses;

        public IoBoxSetterSelectorPlus(int id, string name, int setterNum) : base(id, name, setterNum) {
            // No need to use this one
            _currentStatus = 0;
        }

        public Command LoopingWriteCommand() {
#if DEBUG
            logger.Debug($"_positionStatuses = {string.Join(", ", _positionStatuses)}");
            logger.Debug($"_positionsInUse = {string.Join(", ", _positionsInUse)}");
#endif

            string high = "0000";
            string low = "1111";
            char[] newHigh = high.ToArray();
            char[] newLow = low.ToArray();

            bool lastOneBack = true;
            for (int i = 0; i < _positionStatuses.Length; i++) {
                if (_positionsInUse[i] == 1 && _positionStatuses[i] == 1) {
                    newLow[i] = '0';
                    lastOneBack = false;
                }
            }
            for (int i = 0; i < _positionStatuses.Length; i++) {
                if (_currentPosition > 0 && i == _currentPosition - 1) {
                    newHigh[i] = '1';
                    if (lastOneBack) {
                        newLow[i] = '0';
                    }
                }
            }

            _current_signal = new String(newHigh.ToArray()) + new String(newLow.Reverse().ToArray());

            string temp = _command_write.GetMessage(MainUtils.ToHexString(_current_signal));
            byte[] bytes = MainUtils.ToBytes(temp);
            return new Command(temp + MainUtils.Crc16ToString(bytes));
        }

        public void AnalyzeDataAndAction(string dataMessage) {
            string low = string.Join("", dataMessage.Skip(9).Take(1));
            // Reverse to check which one has been took out
            string realLow = ReverseBit(MainUtils.ToBinaryString(low));
            for (int i = 0; i < realLow.Length; i++) {
                char c = realLow[i];
                // Equals to 1 means current one has been took out
                // Means: current one is empty
                if (c == '1') {
                    _positionStatuses[i] = 1;
                } else {
                    _positionStatuses[i] = 0;
                }
            }
        }

        private string ReverseBit(string bits) {
            StringBuilder sb = new();
            for (int i = 0; i < bits.Length; i++) {
                if (bits[i] == '1') {
                    sb.Append('0');
                } else {
                    sb.Append('1');
                }
            }
            return sb.ToString();
        }
    }

    public class IoBoxSetterSelector_4: IoBoxSetterSelector {
        public IoBoxSetterSelector_4() : base(1, "SetterSelector_4", 4) { }
    }

    public class IoBoxSetterSelector_8: IoBoxSetterSelector {
        public IoBoxSetterSelector_8() : base(2, "SetterSelector_8", 8) { }
    }

    public class IoBoxSetterSelector_4_plus: IoBoxSetterSelectorPlus {
        public IoBoxSetterSelector_4_plus() : base(4, "SetterSelector_4_plus", 4) { }
    }

    public class IoBoxArranger: DeviceTypeIoBox {
        private int?[] _currentPositions = new int?[] { null, null, null, null, null, null, null, null };
        private int?[] _sendingPositions = new int?[] { null, null, null, null, null, null, null, null };
        private int?[] _currentStatuses = new int?[] { null, null, null, null, null, null, null, null };
        private int?[] _inPositions = new int?[] { null, null, null, null, null, null, null, null };

        public IoBoxArranger() : base(3, "Arranger") { }

        public Command GetWriteCommand(int?[] positions) {
            _currentPositions = positions;

            int min = 1;
            int max = 8;
            foreach (int? curr in _currentPositions) {
                if (curr != null && curr > max || curr < min) {
                    string errorMsg = $"Position[{curr}] of {Name} can not less then {min} or grater then {max}, please check.";
                    logger.Error(errorMsg);
                    throw new IndexOutOfRangeException(errorMsg);
                }
            }

            return new(GetCommand());
        }

        public override Command GetResetCommand() {
            _currentPositions = new int?[] { null, null, null, null };
            return new(GetCommand());
        }

        protected override string GetCommand() {
            Settings settings = ConfigUtils.LoadConfig<Settings>();
            bool reverseFirstFour = settings.reverse_arranger.ToYesOrNoBool();

            // 构建二进制字符串
            string binaryString = "00000000";

            if (_currentPositions.ToList().Find(p => p != null) != null) {
                char[] binaryArray = binaryString.ToCharArray();
                for (int i = 0; i < _currentPositions.Length; i++) {
                    int? curr = _currentPositions[i];
                    if (curr != null && curr != 0 && i < 8) {
                        binaryArray[i] = '1';
                    }
                }
                binaryString = new string(binaryArray);
                _sendingPositions = _currentPositions;
            }

            // 处理二进制字符串
            char[] processedArray = binaryString.ToCharArray();

            if (reverseFirstFour) {
                // 先进行1↔4, 2↔3对换
                (processedArray[0], processedArray[3]) = (processedArray[3], processedArray[0]);
                (processedArray[1], processedArray[2]) = (processedArray[2], processedArray[1]);
            }

            // 完全反转
            Array.Reverse(processedArray);
            string processedBinary = new string(processedArray);

            // 组装结果 + 计算CRC
            string hexValue = MainUtils.ToHexString(processedBinary);
            string temp = _command_write.GetMessage(hexValue);
            byte[] bytes = MainUtils.ToBytes(temp);
            string result = temp + MainUtils.Crc16ToString(bytes);
#if DEBUG
            logger.Debug($"原始: {binaryString}, 处理后: {processedBinary}, 十六进制: {hexValue}, 模式: {(reverseFirstFour ? "对换+反转" : "仅反转")}，最终结果：{result}");
#endif
            return result;
        }

        public void AnalyzeData(string dataMessage, Action<int?[]>? _ioBoxActionAfterAnalysis) {
            if (_sendingPositions.ToList().Find(p => p != null) != null) {
                try {
                    // string high = string.Join("", dataMessage.Skip(7).Take(1));
                    // String binaryStr = new(MainUtils.ToBinaryString(high).ToArray());

                    // 1. 读取输出字节
                    string outputByteHex = dataMessage.Substring(6, 2); // 字符索引 6~7
                    int outputByte = Convert.ToInt32(outputByteHex, 16);
                    string outputBin = Convert.ToString(outputByte, 2).PadLeft(8, '0'); // 补齐8位
                    string outStr = new string(outputBin.Reverse().ToArray()); // 反转：bit0 -> out1

                    // 2. 读取输入字节
                    string inputByteHex = dataMessage.Substring(8, 2); // 字符索引 8~9
                    int inputByte = Convert.ToInt32(inputByteHex, 16);
                    string inputBin = Convert.ToString(inputByte, 2).PadLeft(8, '0'); // 补齐8位
                    string inStr = new string(inputBin.Reverse().ToArray()); // 反转：bit0 -> in1

                    // 3. 读取模拟量
                    string analogHex = dataMessage.Substring(10, 4); // 可选：如果存在


#if DEBUG
                    logger.Debug($"dataMessage = {dataMessage}");
                    // logger.Debug($"high = {high}");
                    // logger.Debug($"binaryStr = {binaryStr}");

                    // 4. 输出结果
                    logger.Debug($"输出: {outStr}"); // 例如 "10000010" 表示 out1=1, out2=0, ..., out8=1
                    logger.Debug($"输入: {inStr}");
                    logger.Debug($"模拟量（hex）: {analogHex}");

                    logger.Debug($"输入: {inStr}"); // 每位对应 in1~in8，'1'=on

                    string temp6 = string.Join("", dataMessage.Skip(6).Take(1));
                    string temp7 = string.Join("", dataMessage.Skip(7).Take(1));
                    string temp8 = string.Join("", dataMessage.Skip(8).Take(1));
                    string temp9 = string.Join("", dataMessage.Skip(9).Take(1));
                    logger.Debug($"temp6 = {new(MainUtils.ToBinaryString(temp6).ToArray())}");
                    logger.Debug($"temp6 reversed = {new(MainUtils.ToBinaryString(temp6).Reverse().ToArray())}");
                    logger.Debug($"temp7 = {new(MainUtils.ToBinaryString(temp7).ToArray())}");
                    logger.Debug($"temp7 reversed = {new(MainUtils.ToBinaryString(temp7).Reverse().ToArray())}");
                    logger.Debug($"temp8 = {new(MainUtils.ToBinaryString(temp8).ToArray())}");
                    logger.Debug($"temp8 reversed = {new(MainUtils.ToBinaryString(temp8).Reverse().ToArray())}");
                    logger.Debug($"temp9 = {new(MainUtils.ToBinaryString(temp9).ToArray())}");
                    logger.Debug($"temp9 reversed = {new(MainUtils.ToBinaryString(temp9).Reverse().ToArray())}");
#endif

                    // for (int i = 0; i < binaryStr.Length; i++) {
                    //     if (_sendingPositions[i] != null) {
                    //         char c = binaryStr.ElementAt(i);
                    //         _currentStatuses[i] = int.Parse(c.ToString());
                    //     } else {
                    //         _currentStatuses[i] = null;
                    //     }
                    // }

                    for (int i = 0; i < outStr.Length; i++) {
                        if (_sendingPositions[i] != null) {
                            char c = outStr.ElementAt(i);
                            _currentStatuses[i] = int.Parse(c.ToString());
                        } else {
                            _currentStatuses[i] = null;
                        }
                    }
#if DEBUG
                    logger.Debug($"_currentStatuses = {string.Join(",", _currentStatuses)}");
#endif

                    for (int i = 0; i < inStr.Length; i++) {
                        char c = inStr.ElementAt(i);
                        _inPositions[i] = int.Parse(c.ToString());
                    }
#if DEBUG
                    logger.Debug($"_inPositions = {string.Join(",", _inPositions)}");
#endif


                    if (_ioBoxActionAfterAnalysis != null) {
                        _ioBoxActionAfterAnalysis(_currentStatuses);
                    }
                } catch (Exception e) {
                    logger.Error($"Error while analyzing data from arranger, _sendingPositions = {string.Join(", ", _sendingPositions)}, e = {e}");
                    throw e;
                }
            }
        }
    }
}
