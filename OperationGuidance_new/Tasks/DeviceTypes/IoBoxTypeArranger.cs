using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks.AsbtractClasses;

namespace OperationGuidance_new.Tasks.DeviceTypes {
    public class IoBoxTypeArranger: AIoBoxDevice<IoBoxArranger> {
        private IoBoxTask _task;
        public Action<int?[]>? ActionAfterIoSignalReceived { get; set; } = null;

        public IoBoxTypeArranger(IoBoxTask task, IoBoxArranger deviceType, int deviceId) : base(deviceType, deviceId) => _task = task;

        public string WritePosition(int?[] position) => _task.SendCommand(DeviceType.GetWriteCommand(position).GetMessage());
        public void Reset() => _task.SendCommand(DeviceType.GetResetCommand().GetMessage());
        public void OpenDoor(int?[] position) => WritePosition(position);
    }
}
