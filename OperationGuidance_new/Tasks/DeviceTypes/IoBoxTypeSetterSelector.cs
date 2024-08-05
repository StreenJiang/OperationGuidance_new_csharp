using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks.AsbtractClasses;

namespace OperationGuidance_new.Tasks.DeviceTypes {
    public class IoBoxTypeSetterSelector: AIoBoxDevice<IoBoxSetterSelector> {
        protected IoBoxTask _task;
        public Action<int>? ActionAfterIoSignalReceived { get; set; } = null;

        public IoBoxTypeSetterSelector(IoBoxTask task, IoBoxSetterSelector deviceType, int deviceId) : base(deviceType, deviceId) => _task = task;

        public string WritePosition(int position) => _task.SendCommand(DeviceType.GetWriteCommand(position).GetMessage());
        public virtual async void Reset() {
            string result = _task.SendCommand(DeviceType.GetResetCommand().GetMessage());
            bool ok = false;
            int tryTimes = 0;
            int tryMaxTimes = 10;
            while (ok && tryTimes < tryMaxTimes) {
                ok = DeviceType.WriteOk(result);
                if (ok && DeviceType.CurrentStatus == 0) {
                    tryTimes += tryMaxTimes;
                    break;
                }

                tryTimes++;
                await Task.Delay(100);
            }
        }
    }
}
