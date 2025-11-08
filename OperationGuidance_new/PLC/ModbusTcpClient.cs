using EasyModbus;
using log4net;

namespace OperationGuidance_new.PLC {

    /// <summary>
    /// 封装与 PLC 通过 Modbus TCP 通信的客户端类
    /// </summary>
    public class ModbusTcpClient {
        private ILog log = LogManager.GetLogger(typeof(ModbusTcpClient));
        private ModbusClient _client;

        /// <summary>
        /// 构造函数：初始化 ModbusClient 实例
        /// </summary>
        public ModbusTcpClient() {
            _client = new ModbusClient();
        }

        /// <summary>
        /// 连接到 Modbus TCP
        /// </summary>
        /// <param name="ipAddress">PLC 的 IP 地址</param>
        /// <param name="port">PLC 的端口号</param>
        public void Connect(string ipAddress, int port) {
            try {
                _client.IPAddress = ipAddress;
                _client.Port = port;
                _client.Connect();
            } catch (Exception ex) {
                log.Warn($"连接PLC失败：{ex.Message}", ex);
                throw new Exception($"连接PLC失败：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 断开与 PLC 的连接
        /// </summary>
        public void Disconnect() {
            try {
                _client.Disconnect();
            } catch (Exception ex) {
                // 可选：记录异常或忽略
                log.Warn($"断开连接时发生错误：{ex.Message}", ex);
                throw new Exception($"断开连接时发生错误：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 读取单个寄存器 (16位整数)
        /// </summary>
        /// <param name="address">寄存器地址</param>
        /// <returns>返回寄存器值</returns>
        public int ReadRegister(int address) {
            try {
                int[] regs = _client.ReadHoldingRegisters(address, 1);
                return regs[0];
            } catch (Exception ex) {
                log.Warn($"读取单个寄存器失败：{ex.Message}", ex);
                throw new Exception($"读取单个寄存器失败：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 读取多个寄存器值
        /// </summary>
        /// <param name="startAddress">起始寄存器地址</param>
        /// <param name="count">读取寄存器数量</param>
        /// <returns>返回读取到的寄存器值数组</returns>
        public int[] ReadRegisters(int startAddress, int count) {
            try {
                return _client.ReadHoldingRegisters(startAddress, count);
            } catch (Exception ex) {
                log.Warn($"读取多个寄存器失败：{ex.Message}", ex);
                throw new Exception($"读取多个寄存器失败：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 写入单个寄存器 (16位整数)
        /// </summary>
        /// <param name="address">寄存器地址</param>
        /// <param name="value">要写入的整数值</param>
        public void WriteRegister(int address, int value) {
            try {
                _client.WriteSingleRegister(address, value);
            } catch (Exception ex) {
                log.Warn($"写入单个寄存器失败：{ex.Message}", ex);
                throw new Exception($"写入单个寄存器失败：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 写入多个寄存器值
        /// </summary>
        /// <param name="startAddress">起始寄存器地址</param>
        /// <param name="values">要写入的整数数组</param>
        public void WriteRegisters(int startAddress, int[] values) {
            try {
                _client.WriteMultipleRegisters(startAddress, values);
            } catch (Exception ex) {
                log.Warn($"写入多个寄存器失败：{ex.Message}", ex);
                throw new Exception($"写入多个寄存器失败：{ex.Message}", ex);
            }
        }
    }
}
