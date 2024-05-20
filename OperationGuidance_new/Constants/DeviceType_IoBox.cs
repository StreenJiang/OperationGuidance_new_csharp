using OperationGuidance_new.Utils;

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
        protected int _currentPosition; // Io write
        protected int _currentStatus; // Io read
        protected Command _command_write;

        public Command COMMAND_READ;
        public int CurrentStatus => _currentStatus;
        public DeviceTypeIoBox(int id, string name) : base(id, name) {
            _current_signal = _original_signal;
            COMMAND_READ = new("0903000000044541");
            _command_write = new("0906000000{0}");
        }

        public abstract Command GetWriteCommand(int position);
        protected abstract string GetCommand();
        public abstract bool ResetIsOk(string? resultMsg);
        public abstract void AnalyzeData(string dataMessage, Action<int>? _ioBoxActionAfterAnalysis);

        public Command GetResetAllCommand() => new(_original_signal);

        public Command GetResetCommand() {
            _currentPosition = 0;
            return new(GetCommand());
        }

        public bool WriteOk(string? resultMsg) {
            string currentCommand = GetCommand();
            bool isOk = !string.IsNullOrEmpty(resultMsg) && currentCommand == resultMsg;
            return isOk;
        }
    }

    public abstract class IoBoxSetterSelector: DeviceTypeIoBox {
        public int SetterNum { get; set; }
        public IoBoxSetterSelector(int id, string name, int setterNum) : base(id, name) {
            SetterNum = setterNum;
        }

        public override Command GetWriteCommand(int position) {
            _currentPosition = position;
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

        public override bool ResetIsOk(string? resultMsg) => !string.IsNullOrEmpty(resultMsg) && string.Join("", resultMsg.Skip(7).Take(1)) == "0";

        public override void AnalyzeData(string dataMessage, Action<int>? _ioBoxActionAfterAnalysis) {
            string low = string.Join("", dataMessage.Skip(9).Take(1));
            _currentStatus = MainUtils.ToIntByHexString(low);

            if (_ioBoxActionAfterAnalysis != null) {
                _ioBoxActionAfterAnalysis(_currentStatus);
            }
        }
    }

    public class IoBoxSetterSelector_4: IoBoxSetterSelector {
        public IoBoxSetterSelector_4() : base(1, "SetterSelector_4", 4) { }

        public override Command GetWriteCommand(int position) {
            int min = 1;
            int max = SetterNum;
            if (position > max || position < min) {
                string errorMsg = $"Position of {Name} can not less then {min} or grater then {max}, please check.";
                logger.Error(errorMsg);
                throw new IndexOutOfRangeException(errorMsg);
            }

            return base.GetWriteCommand(position);
        }
    }

    public class IoBoxSetterSelector_8: IoBoxSetterSelector {
        public IoBoxSetterSelector_8() : base(2, "SetterSelector_8", 8) { }

        public override Command GetWriteCommand(int position) {
            int min = 1;
            int max = SetterNum;
            if (position > max || position < min) {
                string errorMsg = $"Position of {Name} can not less then {min} or grater then {max}, please check.";
                logger.Error(errorMsg);
                throw new IndexOutOfRangeException(errorMsg);
            }

            return base.GetWriteCommand(position);
        }
    }

    public class IoBoxArranger: DeviceTypeIoBox {
        private int _sendingPosition = 0;
        public IoBoxArranger() : base(3, "Arranger") { }

        public override Command GetWriteCommand(int position) {
            _currentPosition = position;

            int min = 1;
            int max = 4;
            if (_currentPosition > max || _currentPosition < min) {
                string errorMsg = $"Position of {Name} can not less then {min} or grater then {max}, please check.";
                logger.Error(errorMsg);
                throw new IndexOutOfRangeException(errorMsg);
            }

            return new(GetCommand());
        }

        protected override string GetCommand() {
            string[] highTemp = { "0", "0", "0", "0" };
            if (_currentPosition > 0) {
                highTemp[_currentPosition - 1] = "1";
                _sendingPosition = _currentPosition;
            }
            string high = string.Join("", highTemp.Reverse());
            string low = string.Join("", _current_signal.Skip(4));

            _current_signal = high + low;

            string temp = _command_write.GetMessage(MainUtils.ToHexString(_current_signal));
            byte[] bytes = MainUtils.ToBytes(temp);
            return temp + MainUtils.Crc16ToString(bytes);
        }

        public override bool ResetIsOk(string? resultMsg) => !string.IsNullOrEmpty(resultMsg) && string.Join("", resultMsg.Skip(6).Take(1)) == "0";

        public override void AnalyzeData(string dataMessage, Action<int>? _ioBoxActionAfterAnalysis) {
            if (_sendingPosition > 0) {
                string high = string.Join("", dataMessage.Skip(8).Take(1));
                String reversedBinaryStr = new(MainUtils.ToBinaryString(high).Reverse().ToArray());
                _currentStatus = int.Parse(reversedBinaryStr[_sendingPosition - 1].ToString());

                if (_ioBoxActionAfterAnalysis != null) {
                    if (_currentStatus == 0) {
                        _ioBoxActionAfterAnalysis(_sendingPosition);
                        _sendingPosition = 0;
                    }
                }
            }
        }
    }
}
