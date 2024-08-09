using S7.Net;

namespace OperationGuidance_new.Constants {
    public class PlcServer_GLB: PlcServerBase {
        public PlcServer_GLB(CpuType cpuType, string ip, int db, string address, int bitAddress, int dataLength) : base(cpuType, ip, db, address, bitAddress, dataLength) {
        }
    }
}
