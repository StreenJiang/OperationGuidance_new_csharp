using log4net;
using OperationGuidance_new.Constants;

namespace OperationGuidance_new.Tasks.AbstractClasses {
    public abstract class AIoBoxDevice<T> where T : DeviceTypeBase {
        protected ILog log;

        private bool _retrieveResult = false;
        private T _deviceType;
        private int _deviceId;

        public bool RetrieveResult { get => _retrieveResult; set => _retrieveResult = value; }
        public T DeviceType { get => _deviceType; set => _deviceType = value; }
        public int DeviceId { get => _deviceId; set => _deviceId = value; }

        public AIoBoxDevice(T deviceType, int deviceId) {
            log = LogManager.GetLogger(GetType());

            _deviceType = deviceType;
            _deviceId = deviceId;
        }
    }
}
