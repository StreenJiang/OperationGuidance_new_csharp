using log4net;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks.Abstracts;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.DTOs;
using RJCP.IO.Ports;

namespace OperationGuidance_new.Tasks.DeviceManagers {
    /// <summary>
    /// 串口设备管理器
    /// 负责SerialPortTask的创建、更新、删除和重连
    /// </summary>
    public class SerialPortManager : DeviceManagerBase<DeviceSerialPortDTO, SerialPortTask> {

        public SerialPortManager() : base(MainUtils.GetLogger(typeof(SerialPortManager))) { }

        protected override string GetDeviceTypeName() {
            return "SERIALPORT";
        }

        protected override string? GetDeviceName(DeviceSerialPortDTO dto) {
            return dto.name;
        }

        protected override SerialPortTask? CreateTaskInstance(DeviceSerialPortDTO dto) {
            try {
                // 获取设备类型
                DeviceTypeSerialPort? deviceSerialPort = DeviceType_SerialPort.GetById(dto.type);

                if (deviceSerialPort == null) {
                    MainUtils.Warn(Logger, $"Cannot find SerialPort type with ID {dto.type}");
                    return null;
                }

                // 创建SerialPortTask实例
                SerialPortTask task = new(
                    dto.id,
                    dto.port_full_name,
                    dto.port_name,
                    dto.baud_rate,
                    (Parity) dto.parity,
                    dto.data_bit,
                    (StopBits) dto.stop_bit,
                    (DataTypes) dto.data_type,
                    deviceSerialPort
                );

                // 启动连接（在新线程中异步连接）
                _ = Task.Run(() => task.Connect());

                return task;
            } catch (Exception ex) {
                MainUtils.Error(Logger, $"Error creating SerialPortTask for {dto.port_full_name}: {ex.Message}");
                return null;
            }
        }

        protected override bool NeedsReconnectionCore(SerialPortTask task, DeviceSerialPortDTO dto) {
            // 检查串口配置是否改变
            // 防御性空检查
            if (task.SerialPortType == null) {
                MainUtils.Warn(Logger, $"SerialPort[{dto.id}] SerialPortType为null，需要重连", false);
                return true;
            }

            bool needsReconnect = task.PortName != dto.port_name ||
                                  task.BaudRate != dto.baud_rate ||
                                  (int) task.Parity != dto.parity ||
                                  task.DataBits != dto.data_bit ||
                                  (int) task.StopBits != dto.stop_bit ||
                                  (int) task.DataType != dto.data_type ||
                                  task.SerialPortType.Id != dto.type;

            // 添加详细日志以便调试
            if (needsReconnect) {
                MainUtils.Info(Logger, $"SERIALPORT[{dto.id}] 需要重连 - " +
                    $"PortName变化: {task.PortName} -> {dto.port_name}, " +
                    $"BaudRate变化: {task.BaudRate} -> {dto.baud_rate}, " +
                    $"Parity变化: {(int)task.Parity} -> {dto.parity}, " +
                    $"DataBits变化: {task.DataBits} -> {dto.data_bit}, " +
                    $"StopBits变化: {(int)task.StopBits} -> {dto.stop_bit}, " +
                    $"DataType变化: {(int)task.DataType} -> {dto.data_type}, " +
                    $"Type变化: {task.SerialPortType.Id} -> {dto.type}", false);
            }

            return needsReconnect;
        }

        protected override string GetDeviceInfoCore(SerialPortTask task) {
            // 防御性空检查
            if (task.SerialPortType == null) return $"{task.PortName} - Unknown SerialPort Type";

            return $"{task.PortName} - {task.SerialPortType.Name}";
        }

        protected override SerialPortTask? GetExistingTask(int deviceId) {
            return MainUtils.TryGetSerialPortTask(deviceId);
        }

        protected override IEnumerable<SerialPortTask> GetAllCachedTasks() {
            return MainUtils.SerialPortTasks.Values.ToList();
        }

        protected override void RemoveTaskFromCache(int deviceId) {
            MainUtils.RemoveSerialPortTask(deviceId);
        }

        protected override void AddTaskToCache(int deviceId, SerialPortTask task) {
            MainUtils.AddSerialPortTask(deviceId, task);
        }
    }
}
