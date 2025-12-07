using log4net;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks.Abstracts;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_new.Tasks.DeviceManagers {
    /// <summary>
    /// 通信设备管理器
    /// 负责CommunicationTask的创建、更新、删除和重连
    /// </summary>
    public class CommunicationManager : DeviceManagerBase<DeviceCommunicationDTO, CommunicationTask> {

        public CommunicationManager() : base(MainUtils.GetLogger(typeof(CommunicationManager))) { }

        protected override string GetDeviceTypeName() {
            return "COMMUNICATION";
        }

        protected override string? GetDeviceName(DeviceCommunicationDTO dto) {
            return dto.name;
        }

        protected override CommunicationTask? CreateTaskInstance(DeviceCommunicationDTO dto) {
            try {
                // 获取设备类型
                DeviceTypeCommunication? deviceCommunication = DeviceType_Communication.GetById(dto.type);

                if (deviceCommunication == null) {
                    MainUtils.Warn(Logger, $"Cannot find Communication type with ID {dto.type}");
                    return null;
                }

                // 创建CommunicationTask实例
                CommunicationTask task = new(dto.id, dto.name, dto.ip, dto.port, deviceCommunication);

                // 启动连接（在新线程中异步连接）
                _ = Task.Run(() => task.Connect());

                return task;
            } catch (Exception ex) {
                MainUtils.Error(Logger, $"Error creating CommunicationTask for {dto.name}: {ex.Message}");
                return null;
            }
        }

        protected override bool NeedsReconnectionCore(CommunicationTask task, DeviceCommunicationDTO dto) {
            // 检查IP地址、端口或设备类型是否改变
            // 防御性空检查
            if (task.CommunicationType == null) {
                MainUtils.Warn(Logger, $"Communication[{dto.id}] CommunicationType为null，需要重连", false);
                return true;
            }

            bool needsReconnect = task.Ip != dto.ip ||
                                  task.Port != dto.port ||
                                  task.CommunicationType.Id != dto.type;

            // 添加详细日志以便调试
            if (needsReconnect) {
                MainUtils.Info(Logger, $"COMMUNICATION[{dto.id}] 需要重连 - " +
                    $"IP变化: {task.Ip} -> {dto.ip}, " +
                    $"Port变化: {task.Port} -> {dto.port}, " +
                    $"Type变化: {task.CommunicationType.Id} -> {dto.type}");
            }

            return needsReconnect;
        }

        protected override string GetDeviceInfoCore(CommunicationTask task) {
            // 防御性空检查
            if (task.CommunicationType == null) return $"{task.Ip}:{task.Port} - Unknown Communication Type";

            return $"{task.Ip}:{task.Port} - {task.CommunicationType.Name}";
        }

        protected override CommunicationTask? GetExistingTask(int deviceId) {
            return MainUtils.TryGetCommunicationTask(deviceId);
        }

        protected override IEnumerable<CommunicationTask> GetAllCachedTasks() {
            return MainUtils.CommunicationTasks.Values.ToList();
        }

        protected override void RemoveTaskFromCache(int deviceId) {
            MainUtils.RemoveCommunicationTask(deviceId);
        }

        protected override void AddTaskToCache(int deviceId, CommunicationTask task) {
            MainUtils.AddCommunicationTask(deviceId, task);
        }
    }
}
