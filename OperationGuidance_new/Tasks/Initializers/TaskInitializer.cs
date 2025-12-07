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

        public static bool Started { get; set; } = false;

        public static void Init() {
            if (!Started) {
                Started = true;
                TaskCheckingLoop();
            }
        }

        private static async void TaskCheckingLoop() {
            await Task.Run(async () => {
                while (true) {
                    try {
                        MainUtils.Info(logger, "Starting device synchronization cycle...", false);

                        // Query all workstations for devices configuration
                        Dictionary<int, int> toolMaps = new();
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

                        // 并行同步所有设备类型
                        await Task.WhenAll(
                            // Tool设备
                            Task.Run(async () => {
                                var toolDTOs = apis.QueryDeviceToolList(new(SystemUtils.MacAddressesDTO.id) { ForTask = true }).DeviceToolDTOs
                                    .Where(dto => dto.deleted == (int) YesOrNo.NO).ToList();
                                await _toolManager.SynchronizeDevicesAsync(toolDTOs, toolMaps);
                            }),

                            // Communication设备
                            Task.Run(async () => {
                                var communicationDTOs = apis.QueryDeviceCommunicationList(new(SystemUtils.MacAddressesDTO.id) { ForTask = true }).DeviceCommunicationDTOs
                                    .Where(dto => dto.deleted == (int) YesOrNo.NO).ToList();
                                await _communicationManager.SynchronizeDevicesAsync(communicationDTOs, communicationMaps);
                            }),

                            // SerialPort设备
                            Task.Run(async () => {
                                var serialPortDTOs = apis.QueryDeviceSerialPortList(new(SystemUtils.MacAddressesDTO.id) { ForTask = true }).DeviceSerialPortDTOs
                                    .Where(dto => dto.deleted == (int) YesOrNo.NO).ToList();
                                await _serialPortManager.SynchronizeDevicesAsync(serialPortDTOs, serialPortMaps);
                            }),

                            // IoBox和Arm设备（仍使用旧逻辑，因为逻辑不同）
                            Task.Run(async () => {
                                await SynchronizeIoBoxAndArmDevicesAsync();
                            })
                        );

                        MainUtils.Info(logger, "Device synchronization cycle completed", false);

                        // Delay in task looping
                        await Task.Delay(LoopingDelay);
                    } catch (Exception ex) {
                        MainUtils.Error(logger, $"Error in task checking loop: {ex.Message}", false);
                        // 发生错误时等待一段时间再继续
                        await Task.Delay(LoopingDelay);
                    }
                }
            });
        }

        /// <summary>
        /// 同步IoBox和Arm设备
        /// </summary>
        private static async Task SynchronizeIoBoxAndArmDevicesAsync() {
            // Query device lists
            List<DeviceIoDTO> ioBoxDTOs = apis.QueryDeviceIoList(new(SystemUtils.MacAddressesDTO.id) { ForTask = true }).DeviceIoDTOs;
            List<DeviceArmDTO> armDTOs = apis.QueryDeviceArmList(new(SystemUtils.MacAddressesDTO.id) { ForTask = true }).DeviceArmDTOs;

            // Build active keys set for quick lookup
            var activeIoBoxKeys = ioBoxDTOs
                .Where(dto => dto.deleted == (int) YesOrNo.NO)
                .Select(dto => MainUtils.GetTCPClientKey(dto.ip, dto.port))
                .ToHashSet();
            var activeArmKeys = armDTOs
                .Where(dto => dto.deleted == (int) YesOrNo.NO)
                .Select(dto => MainUtils.GetTCPClientKey(dto.ip, dto.port))
                .ToHashSet();

            // Remove deleted IoBox tasks
            var keysToRemove = MainUtils.IoBoxTasks.Keys
                .Where(key => !activeIoBoxKeys.Contains(key) && !activeArmKeys.Contains(key))
                .ToList();
            foreach (string key in keysToRemove) {
                MainUtils.IoBoxTasks[key].CloseConnection();
                MainUtils.Info(logger, $"ioBox[{key}] 已被删除，正在移除...");
                MainUtils.RemoveIoBoxTask(key);
            }

            // Filter active DTOs
            var activeIoBoxDTOs = ioBoxDTOs.Where(dto => dto.deleted == (int) YesOrNo.NO).ToList();
            var activeArmDTOs = armDTOs.Where(dto => dto.deleted == (int) YesOrNo.NO).ToList();

            // Process IoBox devices
            foreach (var dto in activeIoBoxDTOs) {
                await ProcessIoBoxDeviceAsync(dto);
            }

            // Process Arm devices
            foreach (var dto in activeArmDTOs) {
                await ProcessArmDeviceAsync(dto);
            }
        }

        /// <summary>
        /// Process single IoBox device
        /// </summary>
        private static async Task ProcessIoBoxDeviceAsync(DeviceIoDTO dto) {
            string key = MainUtils.GetTCPClientKey(dto.ip, dto.port);
            DeviceTypeIoBox? deviceIoBox = DeviceType_IoBox.GetById(dto.type);

            if (deviceIoBox == null)
                return;

            IoBoxTask? ioBoxTask = MainUtils.TryGetIoBoxTask(key);

            // Create new task if not exists
            if (ioBoxTask == null) {
                // 获取设备名称
                string deviceDisplayName = !string.IsNullOrEmpty(dto.name) ? $"【{dto.name}】" : "";
                ioBoxTask = MainUtils.NewIoBoxTask(dto.ip, dto.port);

                // 显示连接状态日志给UI（后台执行）
                _ = Task.Run(async () => {
                    try {
                        // 等待连接完成或超时
                        await Task.Delay(3000); // 最多等待3秒显示状态

                        if (ioBoxTask != null && ioBoxTask.Connected) {
                            MainUtils.Info(logger, $"✓ 成功连接到 ioBox[{dto.ip}:{dto.port}] {deviceDisplayName}");
                        } else {
                            MainUtils.Warn(logger, $"✗ 连接到 ioBox[{dto.ip}:{dto.port}] {deviceDisplayName} 失败");
                        }
                    } catch (Exception ex) {
                        MainUtils.Warn(logger, $"检查 ioBox[{dto.ip}:{dto.port}] {deviceDisplayName} 连接状态时出错: {ex.Message}");
                    }
                });
            } else if (!ioBoxTask.Connected && ioBoxTask.Status != ATaskBase.CONNECTING) {
                // Reconnect if disconnected
                // 获取设备名称
                string deviceDisplayName = !string.IsNullOrEmpty(dto.name) ? $"【{dto.name}】" : "";
                MainUtils.Info(logger, $"正在重连 ioBox[{dto.ip}:{dto.port}] {deviceDisplayName}", false);
                await ReconnectAsync(ioBoxTask, $"ioBox[{dto.ip}:{dto.port}] {deviceDisplayName}");
            }

            // Initialize device type handlers
            if (ioBoxTask != null) {
                InitializeIoBoxType(ioBoxTask, deviceIoBox, dto.id);
            }
        }

        /// <summary>
        /// Initialize IoBox type handler (Arranger or SetterSelector)
        /// </summary>
        private static void InitializeIoBoxType(IoBoxTask task, DeviceTypeIoBox deviceType, int deviceId) {
            if (deviceType is IoBoxArranger arranger && task.ArrangerType == null) {
                task.ArrangerType = new(task, arranger, deviceId);
                task.ArrangerType.Reset();
            } else if (deviceType is IoBoxSetterSelector setterSelector && task.SetterSelectorType == null) {
                if (setterSelector is IoBoxSetterSelectorPlus selectorPlus) {
                    var selectorPlusType = new IoBoxTypeSetterSelectorPlus(task, selectorPlus, deviceId);
                    selectorPlusType.Reset();
                    task.SetterSelectorType = selectorPlusType;
                } else {
                    task.SetterSelectorType = new(task, setterSelector, deviceId);
                    task.SetterSelectorType.Reset();
                }
            }
        }

        /// <summary>
        /// Process single Arm device
        /// </summary>
        private static async Task ProcessArmDeviceAsync(DeviceArmDTO dto) {
            string key = MainUtils.GetTCPClientKey(dto.ip, dto.port);
            DeviceTypeArm? deviceArm = DeviceType_Arm.GetById(dto.type);

            if (deviceArm == null)
                return;

            IoBoxTask? armTask = MainUtils.TryGetIoBoxTask(key);

            // Create new task if not exists
            if (armTask == null) {
                MainUtils.Info(logger, $"Connecting to arm[{dto.ip}:{dto.port}]...", false);
                armTask = MainUtils.NewIoBoxTask(dto.ip, dto.port);
            } else if (!armTask.Connected && armTask.Status != ATaskBase.CONNECTING) {
                // Reconnect if disconnected
                MainUtils.Info(logger, $"正在重连 arm[{dto.ip}:{dto.port}]", false);
                await ReconnectAsync(armTask, $"arm[{dto.ip}:{dto.port}]");
            }

            // Initialize arm type handler
            if (armTask != null && armTask.ArmType == null) {
                armTask.ArmType = new(deviceArm, dto.id);
            }
        }

        /// <summary>
        /// 重新连接设备（异步版本）
        /// </summary>
        /// <param name="task">设备任务</param>
        /// <param name="deviceInfo">设备信息</param>
        private static async Task ReconnectAsync(ATaskBase task, string deviceInfo) {
            try {
                MainUtils.Warn(logger, $"Disconnected to {deviceInfo}, trying to reconnect...", false);
                await task.ConnectAsync();
                MainUtils.Info(logger, $"Reconnected to {deviceInfo}", false);
            } catch (Exception ex) {
                MainUtils.Error(logger, $"Failed to reconnect to {deviceInfo}: {ex.Message}", false);
            }
        }

        /// <summary>
        /// 重新连接设备（同步版本，向后兼容）
        /// </summary>
        /// <param name="task">设备任务</param>
        /// <param name="deviceInfo">设备信息</param>
        private static void Reconnect(ATaskBase task, string deviceInfo) {
            _ = Task.Run(async () => await ReconnectAsync(task, deviceInfo));
        }
    }
}

