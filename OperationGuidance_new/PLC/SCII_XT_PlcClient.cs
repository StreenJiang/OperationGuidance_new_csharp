using OperationGuidance_new.Configs;
using OperationGuidance_new.Utils;

namespace OperationGuidance_new.PLC {
    public class SCII_XT_PlcClient: APlcClient {
        private readonly object _modbusLock = new();

        public SCII_XT_PlcClient(string ip, int port) : base(ip, port) { }

        public async Task<bool> IsReadyToWrite() {
            var config = ConfigUtils.LoadConfig<SciiXtConfig>();
            int readyAddress = int.Parse(config.plc_is_ready_addr);
            int okValue = (int) SciiXtConfig.PlcStatus.OK;
            int rspValue = (int) SciiXtConfig.PlcStatus.RSP;

            log.Info($"Reading 'isReady' from address = {readyAddress}");

            // 在线程池线程中执行 Modbus 操作（带锁）
            bool isReady = await Task.Run(() => {
                lock (_modbusLock) {
                    try {
                        int currentValue = _modbusTcpClient.ReadRegister(readyAddress);
                        log.Info($"Result of 'isReady' from address = {readyAddress} is [{currentValue}]");
                        if (currentValue == okValue) {
                            _modbusTcpClient.WriteRegister(readyAddress, rspValue);
                            return true;
                        }
                        return false;
                    } catch (Exception ex) {
                        log.Warn($"PLC 读取失败: {ex.Message}");
                        return false;
                    }
                }
            });

            return isReady;
        }

        public override async Task WriteResult(bool result) {
            var config = ConfigUtils.LoadConfig<SciiXtConfig>();
            log.Info($"Write result to address = {config.plc_register_addr}, value = {result}");

            await Task.Run(() => {
                int address = int.Parse(config.plc_register_addr);
                int value = result ? (int) SciiXtConfig.PlcResult.OK : (int) SciiXtConfig.PlcResult.NOK;

                lock (_modbusLock) {
                    _modbusTcpClient.WriteRegister(address, value);
                }
            });
        }
    }
}
