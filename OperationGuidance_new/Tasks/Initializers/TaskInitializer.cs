using log4net;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks.Abstracts;
using OperationGuidance_new.Tasks.DeviceManagers;
using OperationGuidance_new.Tasks.DeviceTypes;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Tasks.Initializers {
    public static class TaskInitializer {
        private static ILog logger = MainUtils.GetLogger(typeof(TaskInitializer));

        private static readonly int LoopingDelay = 5000;
        private static OperationGuidanceApis apis = SystemUtils.GetApis();

        // 设备管理器实例
        private static readonly ToolManager _toolManager = new();
        private static readonly CommunicationManager _communicationManager = new();
        private static readonly SerialPortManager _serialPortManager = new();
        private static readonly IoBoxManager _ioBoxManager = new();

        private static readonly object _initLock = new object();
        public static bool Started { get; set; } = false;

        public static void Init() {
            lock (_initLock) {
                if (!Started) {
                    Started = true;
                    _ = Task.Run(async () => await TaskCheckingLoopAsync());
                }
            }
        }

        private static int _loopCounter = 0;

        /// <summary>
        /// 等待任务完成，带超时功能
        /// </summary>
        private static async Task<bool> WaitForTasksWithTimeout(IEnumerable<Task> tasks, TimeSpan timeout) {
            var taskList = tasks.ToList();
            var delayTask = Task.Delay(timeout);
            var whenAllTask = Task.WhenAll(taskList);
            var completedTask = await Task.WhenAny(whenAllTask, delayTask);
            return completedTask == whenAllTask && whenAllTask.Status == TaskStatus.RanToCompletion;
        }

        private static async Task TaskCheckingLoopAsync() {
            while (true) {
                try {
                    _loopCounter++;
                    var startTime = DateTime.Now;
                    MainUtils.Info(logger, $"[Loop #{_loopCounter}] Starting device synchronization cycle...", false);

                    // Query all workstations for devices configuration
                    Dictionary<int, int> toolMaps = new();
                    Dictionary<int, int> ioMaps = new();
                    Dictionary<int, int> armMaps = new();
                    Dictionary<int, int> communicationMaps = new();
                    Dictionary<int, int> serialPortMaps = new();
                    List<WorkstationDTO> workstations = apis.QueryWorkstationList(new(SystemUtils.MacAddressesDTO.id)).WorkstationsDTOs;
                    foreach (WorkstationDTO workstation in workstations) {
                        if (workstation.tool_id != null) {
                            toolMaps.Add(workstation.tool_id.Value, workstation.id);
                        }
                        if (workstation.arm_id != null) {
                            armMaps.Add(workstation.arm_id.Value, workstation.id);
                        }
                        if (workstation.communication_id != null) {
                            communicationMaps.Add(workstation.communication_id.Value, workstation.id);
                        }
                        if (workstation.serial_port_id != null) {
                            serialPortMaps.Add(workstation.serial_port_id.Value, workstation.id);
                        }
                    }

                    // 并行同步所有设备类型（添加超时和异常处理）
                    var syncTasks = new List<Task>();

                    // Tool设备
                    syncTasks.Add(Task.Run(async () => {
                        try {
                            var toolDTOs = apis.QueryDeviceToolList(new(SystemUtils.MacAddressesDTO.id) { ForTask = true }).DeviceToolDTOs
                                .Where(dto => dto.deleted == (int) YesOrNo.NO).ToList();
                            await _toolManager.SynchronizeDevicesAsync(toolDTOs, toolMaps);
                        } catch (Exception ex) {
                            MainUtils.Error(logger, $"同步Tool设备失败: {ex.Message}");
                        }
                    }));

                    // Communication设备
                    syncTasks.Add(Task.Run(async () => {
                        try {
                            var communicationDTOs = apis.QueryDeviceCommunicationList(new(SystemUtils.MacAddressesDTO.id) { ForTask = true }).DeviceCommunicationDTOs
                                .Where(dto => dto.deleted == (int) YesOrNo.NO).ToList();
                            await _communicationManager.SynchronizeDevicesAsync(communicationDTOs, communicationMaps);
                        } catch (Exception ex) {
                            MainUtils.Error(logger, $"同步Communication设备失败: {ex.Message}");
                        }
                    }));

                    // SerialPort设备
                    syncTasks.Add(Task.Run(async () => {
                        try {
                            var serialPortDTOs = apis.QueryDeviceSerialPortList(new(SystemUtils.MacAddressesDTO.id) { ForTask = true }).DeviceSerialPortDTOs
                                .Where(dto => dto.deleted == (int) YesOrNo.NO).ToList();
                            await _serialPortManager.SynchronizeDevicesAsync(serialPortDTOs, serialPortMaps);
                        } catch (Exception ex) {
                            MainUtils.Error(logger, $"同步SerialPort设备失败: {ex.Message}");
                        }
                    }));

                    // IoBox和Arm设备（使用IoBoxManager）
                    syncTasks.Add(Task.Run(async () => {
                        try {
                            var ioBoxDTOs = apis.QueryDeviceIoList(new(SystemUtils.MacAddressesDTO.id) { ForTask = true }).DeviceIoDTOs
                                .Where(dto => dto.deleted == (int) YesOrNo.NO).ToList();
                            var armDTOs = apis.QueryDeviceArmList(new(SystemUtils.MacAddressesDTO.id) { ForTask = true }).DeviceArmDTOs
                                .Where(dto => dto.deleted == (int) YesOrNo.NO).ToList();
                            await _ioBoxManager.SynchronizeDevicesAsync(ioBoxDTOs, armDTOs, ioMaps, armMaps);
                        } catch (Exception ex) {
                            MainUtils.Error(logger, $"同步IoBox/Arm设备失败: {ex.Message}");
                        }
                    }));

                    // 使用Task.WhenAll等待所有任务完成，并设置60秒超时
                    bool allCompleted = await WaitForTasksWithTimeout(syncTasks, TimeSpan.FromSeconds(60));
                    if (!allCompleted) {
                        MainUtils.Error(logger, $"设备同步超时（60秒），某些设备可能无法响应");
                    }

                    var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                    MainUtils.Info(logger, $"[Loop #{_loopCounter}] Device synchronization cycle completed in {elapsed:F0}ms", false);

                    // Delay in task looping
                    await Task.Delay(LoopingDelay);
                } catch (Exception ex) {
                    MainUtils.Error(logger, $"Error in task checking loop: {ex.Message}", false);
                    // 发生错误时等待一段时间再继续
                    await Task.Delay(LoopingDelay);
                }
            }
        }
    }
}
