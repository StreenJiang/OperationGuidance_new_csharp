using log4net;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks.AbstractClasses;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_new.Tasks.DeviceManagers {
    /// <summary>
    /// 工具设备管理器
    /// 负责ToolTask的创建、更新、删除和重连
    /// </summary>
    public class ToolManager : DeviceManagerBase<DeviceToolDTO, ToolTask> {

        public ToolManager() : base(MainUtils.GetLogger(typeof(ToolManager))) { }

        protected override string GetDeviceTypeName() {
            return "TOOL";
        }

        protected override ToolTask? CreateTaskInstance(DeviceToolDTO dto) {
            try {
                // 获取设备类型
                DeviceTypeTool? deviceTool = DeviceType_Tool.GetById(dto.type);

                if (deviceTool == null) {
                    MainUtils.Warn(Logger, $"Cannot find Tool type with ID {dto.type}");
                    return null;
                }

                // 创建ToolTask实例
                ToolTask task = new(dto.id, dto.name, dto.ip, dto.port, deviceTool);

                // 启动连接（在新线程中异步连接）
                _ = Task.Run(() => task.Connect());

                return task;
            } catch (Exception ex) {
                MainUtils.Error(Logger, $"Error creating ToolTask for {dto.name}: {ex.Message}");
                return null;
            }
        }

        protected override bool NeedsReconnectionCore(ToolTask task, DeviceToolDTO dto) {
            // 检查IP地址、端口或设备类型是否改变
            return task.Ip != dto.ip ||
                   task.Port != dto.port ||
                   task.ToolType.Id != dto.type;
        }

        protected override string GetDeviceInfoCore(ToolTask task) {
            return $"{task.Ip}:{task.Port} - {task.ToolType.Name}";
        }

        protected override ToolTask? GetExistingTask(int deviceId) {
            return MainUtils.TryGetToolTask(deviceId);
        }

        protected override IEnumerable<ToolTask> GetAllCachedTasks() {
            return MainUtils.ToolTasks.Values.ToList();
        }

        protected override void RemoveTaskFromCache(int deviceId) {
            MainUtils.RemoveToolTask(deviceId);
        }
    }
}
