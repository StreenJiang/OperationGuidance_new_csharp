using OperationGuidance_new.Constants;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;
using RJCP.IO.Ports;

namespace OperationGuidance_new.Tasks {
    public static class TaskInitializer {
        private static readonly int LoopingDelay = 5000;
        private static OperationGuidanceApis apis = SystemUtils.GetApis();

        public static bool Started { get; set; } = false;

        public static void Init() {
            if (!Started) {
                Started = true;
                TaskCheckingLoop();
            }
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
                    List<DeviceToolDTO>.Enumerator toolsEnumerator = toolDTOs.GetEnumerator();
                    toolDTOs.ForEach(dto => {
                        ToolTask? toolTask = MainUtils.TryGetToolTask(dto.id);
                        if (toolTask == null) {
                            DeviceTypeTool? deviceTool = DeviceType_Tool.GetById(dto.type);
                            if (deviceTool != null) {
                                MainUtils.NewToolTask(dto.id, dto.name, dto.ip, dto.port, deviceTool);
                                MainUtils.Log($"Connecting to TOOL[{dto.name} - {dto.ip}: {dto.port}]...");
                            }
                        } else {
                            if (!toolTask.Connected && toolTask.Status != ATaskBase.CONNECTING) {
                                Task.Run(async () => {
                                    MainUtils.Log($"Disconnected to TOOL[{dto.name} - {dto.ip}: {dto.port}], trying to reconnect...");
                                    await toolTask.Connect();
                                    MainUtils.Log($"Reconnected to TOOL[{dto.name} - {dto.ip}: {dto.port}]");
                                });
                            } else if (toolTask.Ip != dto.ip || toolTask.Port != dto.port) {
                                toolTask.CloseConnection();
                                toolTask.Ip = dto.ip;
                                toolTask.Port = dto.port;
                                toolTask.CloseConnectionManually = false;
                                toolTask.Connect();
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
                            DeviceTypeArm? deviceArm = DeviceType_Arm.GetById(dto.type);
                            if (deviceArm != null) {
                                MainUtils.NewArmTask(dto.id, dto.name, dto.ip, dto.port, deviceArm);
                                MainUtils.Log($"Connecting to ARM[{dto.name} - {dto.ip}: {dto.port}]...");
                            }
                        } else {
                            if (!armTask.Connected && armTask.Status != ATaskBase.CONNECTING) {
                                Task.Run(async () => {
                                    MainUtils.Log($"Disconnected to ARM[{dto.name} - {dto.ip}: {dto.port}], trying to reconnect...");
                                    await armTask.Connect();
                                    MainUtils.Log($"Reconnected to ARM[{dto.name} - {dto.ip}: {dto.port}]");
                                });
                            } else if (armTask.Ip != dto.ip || armTask.Port != dto.port) {
                                armTask.CloseConnection();
                                armTask.Ip = dto.ip;
                                armTask.Port = dto.port;
                                armTask.CloseConnectionManually = false;
                                armTask.Connect();
                            }
                        }
                    });
                    
                    // Initialize communication tasks
                    List<DeviceCommunicationDTO> communicationDTOs = apis.QueryDeviceCommunicationList(new()).DeviceCommunicationDTOs;
                    // Remove communications which had been deleted
                    foreach (KeyValuePair<int, CommunicationTask> pair in MainUtils.CommunicationTasks.Where(pair => !communicationDTOs.Select(dto => dto.id).Contains(pair.Key)).ToList()) {
                        pair.Value.CloseConnection();
                        MainUtils.CommunicationTasks.Remove(pair.Key);
                    }
                    // Loop to check all communications
                    communicationDTOs.ForEach(dto => {
                        CommunicationTask? communicationTask = MainUtils.TryGetCommunicationTask(dto.id);
                        if (communicationTask == null) {
                            DeviceTypeCommunication? deviceCommunication = DeviceType_Communication.GetById(dto.type);
                            if (deviceCommunication != null) {
                                MainUtils.NewCommunicationTask(dto.id, dto.name, dto.ip, dto.port, deviceCommunication);
                                MainUtils.Log($"Connecting to Communication[{dto.name} - {dto.ip}: {dto.port}]...");
                            }
                        } else {
                            if (!communicationTask.Connected && communicationTask.Status != ATaskBase.CONNECTING) {
                                Task.Run(async () => {
                                    MainUtils.Log($"Disconnected to Communication[{dto.name} - {dto.ip}: {dto.port}], trying to reconnect...");
                                    await communicationTask.Connect();
                                    MainUtils.Log($"Reconnected to Communication[{dto.name} - {dto.ip}: {dto.port}]");
                                });
                            } else if (communicationTask.Ip != dto.ip || communicationTask.Port != dto.port) {
                                communicationTask.CloseConnection();
                                communicationTask.Ip = dto.ip;
                                communicationTask.Port = dto.port;
                                communicationTask.CloseConnectionManually = false;
                                communicationTask.Connect();
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
                            DeviceTypeSerialPort? deviceSerialPort = DeviceType_SerialPort.GetById(dto.type);
                            if (deviceSerialPort != null) {
                                MainUtils.NewSerialPortTask(dto.id, dto.port_full_name, 
                                    dto.port_name, dto.baud_rate, (Parity) dto.parity, dto.data_bit, 
                                    (StopBits) dto.stop_bit, (DataTypes) dto.data_type, deviceSerialPort);
                                MainUtils.Log($"Connecting to SerialPort[{dto.name}]");
                            }
                        } else {
                            if (!serialPortTask.Connected && serialPortTask.Status != ATaskBase.CONNECTING) {
                                Task.Run(async () => {
                                    MainUtils.Log($"Disconnected to SerialPort[{dto.name}], trying to reconnect...");
                                    await serialPortTask.Connect();
                                    MainUtils.Log($"Reconnected to SerialPort[{dto.name}]");
                                });
                            } else if(serialPortTask.PortName != dto.port_name || serialPortTask.BaudRate != dto.baud_rate
                                        || (int) serialPortTask.Parity != dto.parity || serialPortTask.DataBits != dto.data_bit
                                        || (int) serialPortTask.StopBits != dto.stop_bit || (int) serialPortTask.DataType != dto.data_type) {
                                serialPortTask.CloseConnection();
                                serialPortTask.PortName = dto.port_name;
                                serialPortTask.FullName = dto.port_full_name;
                                serialPortTask.BaudRate = dto.baud_rate;
                                serialPortTask.Parity = (Parity) dto.parity;
                                serialPortTask.DataBits = dto.data_bit;
                                serialPortTask.StopBits = (StopBits) dto.stop_bit;
                                serialPortTask.DataType = (DataTypes) dto.data_type;
                                serialPortTask.CloseConnectionManually = false;
                                serialPortTask.Connect();
                                //
                                // serialPortTask.CloseConnection();
                                // MainUtils.SerialPortTasks.Remove(dto.id);
                                // DeviceTypeSerialPort deviceSerialPort = DeviceType_SerialPort.GetById(dto.type);
                                // MainUtils.NewSerialPortTask(dto.id, dto.port_full_name, 
                                //     dto.port_name, dto.baud_rate, (Parity) dto.parity, dto.data_bit, 
                                //     (StopBits) dto.stop_bit, (DataTypes) dto.data_type, deviceSerialPort);
                                // MainUtils.Log($"Connecting to SerialPort[{dto.name}]");
                            }
                        }
                    });

                    // Delay in task looping
                    await Task.Delay(LoopingDelay);
                }
            });

        }
    }
}
