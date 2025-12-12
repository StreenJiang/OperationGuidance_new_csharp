using OperationGuidance_new.Constants;

namespace OperationGuidance_new.Tasks.DeviceTypes {
    public class IoBoxTypeSetterSelectorPlus: IoBoxTypeSetterSelector {
        private int _current = 0;
        private int[] _allPositionsCached = { 0, 0, 0, 0 };

        public int Current { get => _current; set => _current = value; }

        public IoBoxTypeSetterSelectorPlus(IoBoxTask task, IoBoxSetterSelectorPlus deviceType, int deviceId) : base(task, deviceType, deviceId) { }

        public void WritePositionPlus(int position) => ((IoBoxSetterSelectorPlus) DeviceType).CurrentPosition = position;

        /// <summary>
        /// 重置设备到位置0（同步版本，为向后兼容保留）
        /// </summary>
        public override void Reset() {
            WritePositionPlus(0);
        }
    }
}
