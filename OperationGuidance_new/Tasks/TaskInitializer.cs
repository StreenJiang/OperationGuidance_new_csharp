using System.IO.Ports;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Tasks {
    public static class TaskInitializer {
        private static readonly int LoopingDelay = 5000;
        private static OperationGuidanceApis apis = SystemUtils.GetApis();

        public static void Init() {
            TaskCheckingLoop();
        }

        private static void TaskCheckingLoop() {
            Task.Run(async () => {
                while (true) {
                    // Initialize tool tasks
                    List<DeviceToolDTO> toolDTOs = apis.QueryDeviceToolList(new()).DeviceToolDTOs;
                    // Remove tools which had been deleted
                    foreach (KeyValuePair<int, ToolTask> pair in MainUtils.ToolTasks.Where(pair => !toolDTOs.Select(dto => dto.id).Contains(pair.Key)).ToList()) {
                        pair.Value.CloseConnection();
                        MainUtils.ToolTasks.Remove(pair.Key);
                    }
                    // Loop to check all tools
                    toolDTOs.ForEach(dto => {
                        ToolTask? toolTask = MainUtils.TryGetToolTask(dto.id);
                        if (toolTask == null) {
                            if (dto.ip != null && dto.port != null && dto.type != null) {
                                DeviceTool? deviceTool = DeviceType_Tool.GetById(dto.type.Value);
                                if (deviceTool != null) {
                                    MainUtils.NewToolTask(dto.id, dto.name, dto.ip, dto.port.Value, deviceTool);
                                }
                            }
                        } else {
                            if (!toolTask.Connected && toolTask.Status != ATaskBase.CONNECTING) {
                                Task.Run(async () => {
                                    MainUtils.PrintEventLog($"Disconnected to TOOL[{dto.name} - {dto.ip}: {dto.port}], trying to reconnect...");
                                    await toolTask.Connect();
                                    MainUtils.PrintEventLog($"Reconnected to TOOL[{dto.name} - {dto.ip}: {dto.port}]");
                                });
                            }
                        }
                    });

                    // Initialize arm tasks
                    List<DeviceArmDTO> armDTOs = apis.QueryDeviceArmList(new()).DeviceArmDTOs;
                    // Remove arms which had been deleted
                    foreach (KeyValuePair<int, ArmTask> pair in MainUtils.ArmTasks.Where(pair => !armDTOs.Select(dto => dto.id).Contains(pair.Key)).ToList()) {
                        pair.Value.CloseConnection();
                        MainUtils.ArmTasks.Remove(pair.Key);
                    }
                    // Loop to check all arms
                    armDTOs.ForEach(dto => {
                        ArmTask? armTask = MainUtils.TryGetArmTask(dto.id);
                        if (armTask == null) {
                            if (dto.ip != null && dto.port != null && dto.type != null) {
                                DeviceArm? deviceArm = DeviceType_Arm.GetById(dto.type.Value);
                                if (deviceArm != null) {
                                    MainUtils.NewArmTask(dto.id, dto.name, dto.ip, dto.port.Value, deviceArm);
                                }
                            }
                        } else {
                            if (!armTask.Connected && armTask.Status != ATaskBase.CONNECTING) {
                                Task.Run(async () => {
                                    MainUtils.PrintEventLog($"Disconnected to ARM[{dto.name} - {dto.ip}: {dto.port}], trying to reconnect...");
                                    await armTask.Connect();
                                    MainUtils.PrintEventLog($"Reconnected to ARM[{dto.name} - {dto.ip}: {dto.port}]");
                                });
                            }
                        }
                    });

                    // Initialize serialPort tasks
                    List<DeviceSerialPortDTO> serialPortDTOs = apis.QueryDeviceSerialPortList(new()).DeviceSerialPortDTOs;
                    // Remove serialPorts which had been deleted
                    foreach (KeyValuePair<int, SerialPortTask> pair in MainUtils.SerialPortTasks.Where(pair => !serialPortDTOs.Select(dto => dto.id).Contains(pair.Key)).ToList()) {
                        pair.Value.CloseConnection();
                        MainUtils.SerialPortTasks.Remove(pair.Key);
                    }
                    // Loop to check all serialPorts
                    serialPortDTOs.ForEach(dto => {
                        SerialPortTask? serialPortTask = MainUtils.TryGetSerialPortTask(dto.id);
                        if (serialPortTask == null) {
                            if (dto.type != null && dto.port_name != null && dto.port_full_name != null 
                                && dto.baud_rate != null && dto.parity != null && dto.data_bit != null 
                                && dto.stop_bit != null && dto.data_type != null && MainUtils.TryGetSerialPortTask(dto.id) == null) {
                                DeviceSerialPort? deviceSerialPort = DeviceType_SerialPort.GetById(dto.type.Value);
                                if (deviceSerialPort != null) {
                                    MainUtils.NewSerialPortTask(dto.id, dto.port_full_name, 
                                        dto.port_name, dto.baud_rate.Value, (Parity) dto.parity, dto.data_bit.Value, 
                                        (StopBits) dto.stop_bit, (DataTypes) dto.data_type, deviceSerialPort);
                                }
                            }
                        } else {
                            if (!serialPortTask.Connected && serialPortTask.Status != ATaskBase.CONNECTING) {
                                Task.Run(async () => {
                                    MainUtils.PrintEventLog($"Disconnected to SerialPort[{dto.name}], trying to reconnect...");
                                    await serialPortTask.Connect();
                                    MainUtils.PrintEventLog($"Reconnected to SerialPort[{dto.name}]");
                                });
                            }
                        }
                    });
                    
                    await Task.Delay(LoopingDelay);
                }
            });
        }
    }
}
