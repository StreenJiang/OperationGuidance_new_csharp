using log4net;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks.AbstractClasses;
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
            return task.Ip != dto.ip ||
                   task.Port != dto.port ||
                   task.CommunicationType.Id != dto.type;
        }

        protected override string GetDeviceInfoCore(CommunicationTask task) {
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
    }
}
