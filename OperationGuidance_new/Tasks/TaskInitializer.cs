using log4net;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks.AbstractClasses;
using OperationGuidance_new.Tasks.DeviceManagers;
using OperationGuidance_new.Tasks.DeviceTypes;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;
using RJCP.IO.Ports;

namespace OperationGuidance_new.Tasks {
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
                        MainUtils.Info(logger, "Starting device synchronization cycle...");

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

                        MainUtils.Info(logger, "Device synchronization cycle completed");

                        // Delay in task looping
                        await Task.Delay(LoopingDelay);
                    } catch (Exception ex) {
                        MainUtils.Error(logger, $"Error in task checking loop: {ex.Message}");
                        // 发生错误时等待一段时间再继续
                        await Task.Delay(LoopingDelay);
                    }
                }
            });
        }

        /// <summary>
        /// 同步IoBox和Arm设备（保持原有逻辑，因为逻辑与其他设备不同）
        /// </summary>
        private static async Task SynchronizeIoBoxAndArmDevicesAsync() {
            // Initialize ioBox tasks, arm devices included
            List<DeviceIoDTO> ioBoxDTOs = apis.QueryDeviceIoList(new(SystemUtils.MacAddressesDTO.id) { ForTask = true }).DeviceIoDTOs;
            List<DeviceArmDTO> armDTOs = apis.QueryDeviceArmList(new(SystemUtils.MacAddressesDTO.id) { ForTask = true }).DeviceArmDTOs;
            // Remove ioBoxs which had been deleted
            foreach (string key in MainUtils.IoBoxTasks.Keys) {
                Tuple<string, int> tuple = MainUtils.GetHostFromTCPClientKey(key);
                List<DeviceIoDTO> ioBoxDtos = ioBoxDTOs.Where(dto => dto.ip == tuple.Item1 && dto.port == tuple.Item2).ToList();
                List<DeviceArmDTO> armDtos = armDTOs.Where(dto => dto.ip == tuple.Item1 && dto.port == tuple.Item2).ToList();
                if ((ioBoxDtos.Count == 0 && armDtos.Count == 0) || (ioBoxDtos.Find(dto => dto.deleted == (int) YesOrNo.NO) == null && armDtos.Find(dto => dto.deleted == (int) YesOrNo.NO) == null)) {
                    MainUtils.IoBoxTasks[key].CloseConnection();
                    MainUtils.Info(logger, $"all devices in ioBox[{key}] had been deleted, remove it.");
                    MainUtils.RemoveIoBoxTask(key);
                }
            }
            ioBoxDTOs = ioBoxDTOs.Where(dto => dto.deleted == (int) YesOrNo.NO).ToList();
            armDTOs = armDTOs.Where(dto => dto.deleted == (int) YesOrNo.NO).ToList();
            // Loop to check all ioBoxs
            ioBoxDTOs.ForEach(async dto => {
                string key = MainUtils.GetTCPClientKey(dto.ip, dto.port);

                IoBoxTask? ioBoxTask = MainUtils.TryGetIoBoxTask(key);
                if (ioBoxTask == null) {
                    DeviceTypeIoBox? deviceIoBox = DeviceType_IoBox.GetById(dto.type);
                    if (deviceIoBox != null) {
                        MainUtils.Info(logger, $"Connecting to ioBox[{dto.ip}: {dto.port}]...");
                        ioBoxTask = MainUtils.NewIoBoxTask(dto.ip, dto.port);
                    }
                } else {
                    if (ioBoxTask.Ip != dto.ip || ioBoxTask.Port != dto.port) {
                        ioBoxTask.CloseConnection();
                        MainUtils.RemoveIoBoxTask(ioBoxTask.Ip, ioBoxTask.Port);
                        await Task.Delay(ioBoxTask.AutoReconnectingTrialDelay);

                        MainUtils.Info(logger, $"ioBox info changed, Reconnecting to ioBox[{dto.ip}: {dto.port}]...");
                        MainUtils.NewIoBoxTask(dto.ip, dto.port);
                    } else if (!ioBoxTask.Connected && ioBoxTask.Status != ATaskBase.CONNECTING) {
                        Reconnect(ioBoxTask, $"ioBox[{dto.ip}: {dto.port}]");
                    }
                }

                if (ioBoxTask != null) {
                    DeviceTypeIoBox? deviceIoBox = DeviceType_IoBox.GetById(dto.type);
                    if (deviceIoBox is IoBoxArranger arranger && ioBoxTask.ArrangerType == null) {
                        ioBoxTask.ArrangerType = new(ioBoxTask, arranger, dto.id);
                        ioBoxTask.ArrangerType.Reset();
                    } else if (deviceIoBox is IoBoxSetterSelector setterSelector && ioBoxTask.SetterSelectorType == null) {
                        if (setterSelector is IoBoxSetterSelectorPlus selectorPlus) {
                            IoBoxTypeSetterSelectorPlus ioBoxTypeSetterSelectorPlus = new IoBoxTypeSetterSelectorPlus(ioBoxTask, selectorPlus, dto.id);
                            ioBoxTypeSetterSelectorPlus.Reset();
                            ioBoxTask.SetterSelectorType = ioBoxTypeSetterSelectorPlus;
                        } else {
                            ioBoxTask.SetterSelectorType = new(ioBoxTask, setterSelector, dto.id);
                            ioBoxTask.SetterSelectorType.Reset();
                        }
                    }
                }
            });
            // Loop to check all arms
            armDTOs.ForEach(async dto => {
                string key = MainUtils.GetTCPClientKey(dto.ip, dto.port);

                IoBoxTask? armTask = MainUtils.TryGetIoBoxTask(key);
                if (armTask == null) {
                    DeviceTypeArm? deviceArm = DeviceType_Arm.GetById(dto.type);
                    if (deviceArm != null) {
                        MainUtils.Info(logger, $"Connecting to arm[{dto.ip}: {dto.port}]...");
                        armTask = MainUtils.NewIoBoxTask(dto.ip, dto.port);
                    }
                } else {
                    if (armTask.Ip != dto.ip || armTask.Port != dto.port) {
                        armTask.CloseConnection();
                        MainUtils.RemoveIoBoxTask(armTask.Ip, armTask.Port);
                        await Task.Delay(armTask.AutoReconnectingTrialDelay);

                        MainUtils.Info(logger, $"arm info changed, Reconnecting to arm[{dto.ip}: {dto.port}]...");
                        MainUtils.NewIoBoxTask(dto.ip, dto.port);
                    } else if (!armTask.Connected && armTask.Status != ATaskBase.CONNECTING) {
                        Reconnect(armTask, $"arm[{dto.ip}: {dto.port}]");
                    }
                }

                if (armTask != null) {
                    DeviceTypeArm? deviceArm = DeviceType_Arm.GetById(dto.type);
                    if (deviceArm != null && armTask.ArmType == null) {
                        armTask.ArmType = new(deviceArm, dto.id);
                    }
                }
            });
        }

        /// <summary>
        /// 重新连接设备（异步版本）
        /// </summary>
        /// <param name="task">设备任务</param>
        /// <param name="deviceInfo">设备信息</param>
        private static async Task ReconnectAsync(ATaskBase task, string deviceInfo) {
            await Task.Run(async () => {
                MainUtils.Warn(logger, $"Disconnected to {deviceInfo}, trying to reconnect...");
                await task.ConnectAsync();
                MainUtils.Info(logger, $"Reconnected to {deviceInfo}");
            });
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

