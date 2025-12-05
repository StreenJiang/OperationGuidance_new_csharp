using System.Globalization;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks.Abstracts;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Tasks.DeviceTypes {
    public class IoBoxTypeArm: AIoBoxDevice<DeviceTypeArm> {
        public Action<int, Coordinates3D>? ActionAfterCoordinatesReceived { get; set; } = null;

        public IoBoxTypeArm(DeviceTypeArm deviceType, int deviceId) : base(deviceType, deviceId) { }

        public async Task<Coordinates3D?> GetCurrentCoordinates() {
            Coordinates3D? coordinates = null;
            RetrieveResult = true;
            ActionAfterCoordinatesReceived = (maxValue, data) => coordinates = data;

            int maxWaitTime = 2000;
            int waitDelay = 100;
            int waitCount = 0;
            while (coordinates == null && waitDelay < maxWaitTime) {
                await Task.Delay(waitDelay);
                waitCount += waitDelay;
            }

            RetrieveResult = false;
            ActionAfterCoordinatesReceived = null;
            return coordinates;
        }

        public int ParseResult(string result) {
            int coordinate = 0;
            if (result != null && result != "") {
                string lowData = result.Substring(6, 4);
                string HighData = result.Substring(10, 4);
                if (lowData != "ffff" && HighData != "ffff") {
                    coordinate = int.Parse(lowData, NumberStyles.HexNumber);
                    // coordinate = Convert.ToInt32(lowData, 16);
                }
            }
            return coordinate;
        }
    }
}
