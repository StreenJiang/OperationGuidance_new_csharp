using OperationGuidance_new.Constants;

namespace OperationGuidance_new.Tasks.DeviceTypes {
    public class IoBoxTypeSetterSelectorPlus: IoBoxTypeSetterSelector {
        private int _current = 0;

        public int Current { get => _current; set => _current = value; }

        public IoBoxTypeSetterSelectorPlus(IoBoxTask task, IoBoxSetterSelectorPlus deviceType, int deviceId) : base(task, deviceType, deviceId) { }

        // private async void RunLooping() {
        //     ActionAfterIoSignalReceived += CheckCurrent;
        //     await Task.Run(() => {
        //         while (_task != null) {
        //         }
        //     });
        // }

        private void CheckCurrent(int current) => _current = current;

        public override async void Reset() {
            ActionAfterIoSignalReceived += CheckCurrent;
            while (_current > 0) {
                await Task.Delay(500);
            }
            ActionAfterIoSignalReceived -= CheckCurrent;

            base.Reset();
        }
    }
}
