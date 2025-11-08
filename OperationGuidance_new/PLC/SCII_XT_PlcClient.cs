using OperationGuidance_new.Configs;
using OperationGuidance_new.Utils;

namespace OperationGuidance_new.PLC {
    public class SCII_XT_PlcClient: APlcClient {
        public SCII_XT_PlcClient(string ip, int port) : base(ip, port) { }

        public override void WriteResult(bool result) {
            SciiXtConfig sciiXtConfig = ConfigUtils.LoadConfig<SciiXtConfig>();
            _modbusTcpClient.WriteRegister(int.Parse(sciiXtConfig.plc_register_addr),
                                           result ? (int) SciiXtConfig.PlcResult.OK
                                                  : (int) SciiXtConfig.PlcResult.NOK);
        }
    }
}
