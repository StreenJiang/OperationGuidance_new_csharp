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

                        // IoBox和Arm设备（使用IoBoxManager）
                        Task.Run(async () => {
                            try {
                                var ioBoxDTOs = apis.QueryDeviceIoList(new(SystemUtils.MacAddressesDTO.id) { ForTask = true }).DeviceIoDTOs
                                    .Where(dto => dto.deleted == (int) YesOrNo.NO).ToList();
                                var armDTOs = apis.QueryDeviceArmList(new(SystemUtils.MacAddressesDTO.id) { ForTask = true }).DeviceArmDTOs
                                    .Where(dto => dto.deleted == (int) YesOrNo.NO).ToList();
                                await _ioBoxManager.SynchronizeDevicesAsync(ioBoxDTOs, armDTOs, ioMaps, armMaps);
                            } catch (Exception ex) {
                                MainUtils.Error(logger, $"同步IoBox/Arm设备失败: {ex.Message}");
                            }
                        })
                    );

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
