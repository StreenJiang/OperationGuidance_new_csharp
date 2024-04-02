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
                    List<DeviceToolDTO> toolDTOs = apis.QueryDeviceToolList(new(SystemUtils.MacAddressesDTO.id) { ForTask = true }).DeviceToolDTOs;
                    // Remove tools which had been deleted
                    toolDTOs.ForEach(dto => {
                        if (MainUtils.ToolTasks.ContainsKey(dto.id) && dto.deleted == (int) YesOrNo.YES) {
                            MainUtils.ToolTasks[dto.id].CloseConnection();
                            MainUtils.Log($"TOOL[{dto.name} - {dto.ip}: {dto.port}] had been deleted, remove it.");
                            MainUtils.ToolTasks.Remove(dto.id);
                        }
                    });
                    toolDTOs = toolDTOs.Where(dto => dto.deleted == (int) YesOrNo.NO).ToList();
                    // Loop to check all tools
                    List<DeviceToolDTO>.Enumerator toolsEnumerator = toolDTOs.GetEnumerator();
                    toolDTOs.ForEach(async dto => {
                        ToolTask? toolTask = MainUtils.TryGetToolTask(dto.id);
                        if (toolTask == null) {
                            DeviceTypeTool? deviceTool = DeviceType_Tool.GetById(dto.type);
                            if (deviceTool != null) {
                                MainUtils.NewToolTask(dto.id, dto.name, dto.ip, dto.port, deviceTool);
                                MainUtils.Log($"Connecting to TOOL[{dto.name} - {dto.ip}: {dto.port} - {deviceTool.Name}]...");
                            }
                        } else {
                            if (toolTask.Ip != dto.ip || toolTask.Port != dto.port || toolTask.ToolType.Id != dto.type) {
                                toolTask.CloseConnection();
                                await Task.Delay(toolTask.AuotReconnectingTrialDelay);

                                DeviceTypeTool? deviceTool = DeviceType_Tool.GetById(dto.type);
                                if (deviceTool != null) {
                                    MainUtils.Log($"TOOL info changed, Reconnecting to TOOL[{dto.name} - {dto.ip}: {dto.port} - {deviceTool.Name}]...");
                                    toolTask.Ip = dto.ip;
                                    toolTask.Port = dto.port;
                                    toolTask.ToolType = deviceTool;
                                    toolTask.CloseConnectionManually = false;
                                    toolTask.Connect();
                                } else {
                                    MainUtils.ToolTasks.Remove(dto.id);
                                    MainUtils.Log($"TOOL[{dto.name} - {dto.ip}: {dto.port}] removed, can't find tool type [{dto.type}].");
                                }
                            } else if (!toolTask.Connected && toolTask.Status != ATaskBase.CONNECTING) {
                                Task.Run(async () => {
                                    MainUtils.Log($"Disconnected to TOOL[{dto.name} - {dto.ip}: {dto.port} - {toolTask.ToolType.Name}], trying to reconnect...");
                                    await toolTask.Connect();
                                    MainUtils.Log($"Reconnected to TOOL[{dto.name} - {dto.ip}: {dto.port} - {toolTask.ToolType.Name}]");
                                });
                            }
                        }
                    });

                    // Initialize arm tasks
                    List<DeviceArmDTO> armDTOs = apis.QueryDeviceArmList(new(SystemUtils.MacAddressesDTO.id) { ForTask = true }).DeviceArmDTOs;
                    // Remove arms which had been deleted
                    armDTOs.ForEach(dto => {
                        if (MainUtils.ArmTasks.ContainsKey(dto.id) && dto.deleted == (int) YesOrNo.YES) {
                            MainUtils.ArmTasks[dto.id].CloseConnection();
                            MainUtils.Log($"ARM[{dto.name} - {dto.ip}: {dto.port}] had been deleted, remove it.");
                            MainUtils.ArmTasks.Remove(dto.id);
                        }
                    });
                    armDTOs = armDTOs.Where(dto => dto.deleted == (int) YesOrNo.NO).ToList();
                    // Loop to check all arms
                    armDTOs.ForEach(async dto => {
                        ArmTask? armTask = MainUtils.TryGetArmTask(dto.id);
                        if (armTask == null) {
                            DeviceTypeArm? deviceArm = DeviceType_Arm.GetById(dto.type);
                            if (deviceArm != null) {
                                MainUtils.NewArmTask(dto.id, dto.name, dto.ip, dto.port, deviceArm);
                                MainUtils.Log($"Connecting to ARM[{dto.name} - {dto.ip}: {dto.port} - {deviceArm.Name}]...");
                            }
                        } else {
                            if (armTask.Ip != dto.ip || armTask.Port != dto.port || armTask.ArmType.Id != dto.type) {
                                armTask.CloseConnection();
                                await Task.Delay(armTask.AuotReconnectingTrialDelay);

                                DeviceTypeArm? deviceArm = DeviceType_Arm.GetById(dto.type);
                                if (deviceArm != null) {
                                    MainUtils.Log($"ARM info changed, Reconnecting to ARM[{dto.name} - {dto.ip}: {dto.port} - {deviceArm.Name}]...");
                                    armTask.Ip = dto.ip;
                                    armTask.Port = dto.port;
                                    armTask.ArmType = deviceArm;
                                    armTask.CloseConnectionManually = false;
                                    armTask.Connect();
                                } else {
                                    MainUtils.ArmTasks.Remove(dto.id);
                                    MainUtils.Log($"ARM[{dto.name} - {dto.ip}: {dto.port}] removed, can't find arm type [{dto.type}].");
                                }
                            } else if (!armTask.Connected && armTask.Status != ATaskBase.CONNECTING) {
                                Task.Run(async () => {
                                    MainUtils.Log($"Disconnected to ARM[{dto.name} - {dto.ip}: {dto.port} - {armTask.ArmType.Name}], trying to reconnect...");
                                    await armTask.Connect();
                                    MainUtils.Log($"Reconnected to ARM[{dto.name} - {dto.ip}: {dto.port} - {armTask.ArmType.Name}]");
                                });
                            }
                        }
                    });
                    
                    // Initialize communication tasks
                    List<DeviceCommunicationDTO> communicationDTOs = apis.QueryDeviceCommunicationList(new(SystemUtils.MacAddressesDTO.id) { ForTask = true }).DeviceCommunicationDTOs;
                    // Remove communications which had been deleted
                    communicationDTOs.ForEach(dto => {
                        if (MainUtils.CommunicationTasks.ContainsKey(dto.id) && dto.deleted == (int) YesOrNo.YES) {
                            MainUtils.CommunicationTasks[dto.id].CloseConnection();
                            MainUtils.Log($"Communication device[{dto.name} - {dto.ip}: {dto.port}] had been deleted, remove it.");
                            MainUtils.CommunicationTasks.Remove(dto.id);
                        }
                    });
                    communicationDTOs = communicationDTOs.Where(dto => dto.deleted == (int) YesOrNo.NO).ToList();
                    // Loop to check all communications
                    communicationDTOs.ForEach(async dto => {
                        CommunicationTask? communicationTask = MainUtils.TryGetCommunicationTask(dto.id);
                        if (communicationTask == null) {
                            DeviceTypeCommunication? deviceCommunication = DeviceType_Communication.GetById(dto.type);
                            if (deviceCommunication != null) {
                                MainUtils.NewCommunicationTask(dto.id, dto.name, dto.ip, dto.port, deviceCommunication);
                                MainUtils.Log($"Connecting to Communication device[{dto.name} - {dto.ip}: {dto.port} - {deviceCommunication.Name}]...");
                            }
                        } else {
                            if (communicationTask.Ip != dto.ip || communicationTask.Port != dto.port || communicationTask.ComminucationType.Id != dto.type) {
                                communicationTask.CloseConnection();
                                await Task.Delay(communicationTask.AuotReconnectingTrialDelay);

                                DeviceTypeCommunication? deviceCommunication = DeviceType_Communication.GetById(dto.type);
                                if (deviceCommunication != null) {
                                    MainUtils.Log($"Communication device info changed, Reconnecting to Communication device[{dto.name} - {dto.ip}: {dto.port} - {deviceCommunication.Name}]...");
                                    communicationTask.Ip = dto.ip;
                                    communicationTask.Port = dto.port;
                                    communicationTask.ComminucationType = deviceCommunication;
                                    communicationTask.CloseConnectionManually = false;
                                    communicationTask.Connect();
                                } else {
                                    MainUtils.CommunicationTasks.Remove(dto.id);
                                    MainUtils.Log($"Communication device[{dto.name} - {dto.ip}: {dto.port}] removed, can't find Communication device type [{dto.type}].");
                                }
                            } else if (!communicationTask.Connected && communicationTask.Status != ATaskBase.CONNECTING) {
                                Task.Run(async () => {
                                    MainUtils.Log($"Disconnected to Communication device[{dto.name} - {dto.ip}: {dto.port} - {communicationTask.ComminucationType.Name}], trying to reconnect...");
                                    await communicationTask.Connect();
                                    MainUtils.Log($"Reconnected to Communication device[{dto.name} - {dto.ip}: {dto.port} - {communicationTask.ComminucationType.Name}]");
                                });
                            }
                        }
                    });

                    // Initialize serialPort tasks
                    List<DeviceSerialPortDTO> serialPortDTOs = apis.QueryDeviceSerialPortList(new(SystemUtils.MacAddressesDTO.id) { ForTask = true }).DeviceSerialPortDTOs;
                    // Remove serialPorts which had been deleted
                    serialPortDTOs.ForEach(dto => {
                        if (MainUtils.SerialPortTasks.ContainsKey(dto.id) && dto.deleted == (int) YesOrNo.YES) {
                            MainUtils.SerialPortTasks[dto.id].CloseConnection();
                            MainUtils.Log($"SerialPort device[{dto.name}] had been deleted, remove it.");
                            MainUtils.SerialPortTasks.Remove(dto.id);
                        }
                    });
                    serialPortDTOs = serialPortDTOs.Where(dto => dto.deleted == (int) YesOrNo.NO).ToList();
                    // Loop to check all serialPorts
                    serialPortDTOs.ForEach(async dto => {
                        SerialPortTask? serialPortTask = MainUtils.TryGetSerialPortTask(dto.id);
                        if (serialPortTask == null) {
                            DeviceTypeSerialPort? deviceSerialPort = DeviceType_SerialPort.GetById(dto.type);
                            if (deviceSerialPort != null) {
                                MainUtils.NewSerialPortTask(dto.id, dto.port_full_name, 
                                    dto.port_name, dto.baud_rate, (Parity) dto.parity, dto.data_bit, 
                                    (StopBits) dto.stop_bit, (DataTypes) dto.data_type, deviceSerialPort);
                                MainUtils.Log($"Connecting to SerialPort device[{dto.name} - {deviceSerialPort.Name}]");
                            }
                        } else {
                            if(serialPortTask.PortName != dto.port_name || serialPortTask.BaudRate != dto.baud_rate
                                        || (int) serialPortTask.Parity != dto.parity || serialPortTask.DataBits != dto.data_bit
                                        || (int) serialPortTask.StopBits != dto.stop_bit || (int) serialPortTask.DataType != dto.data_type
                                        || serialPortTask.SerialPortType.Id != dto.type) {
                                serialPortTask.CloseConnection();
                                await Task.Delay(serialPortTask.AuotReconnectingTrialDelay);

                                DeviceTypeSerialPort? deviceSerialPort = DeviceType_SerialPort.GetById(dto.type);
                                if (deviceSerialPort != null) {
                                    MainUtils.Log($"SerialPort device info changed, Reconnecting to SerialPort device[{dto.name} - {serialPortTask.SerialPortType.Name}]");
                                    serialPortTask.PortName = dto.port_name;
                                    serialPortTask.FullName = dto.port_full_name;
                                    serialPortTask.BaudRate = dto.baud_rate;
                                    serialPortTask.Parity = (Parity) dto.parity;
                                    serialPortTask.DataBits = dto.data_bit;
                                    serialPortTask.StopBits = (StopBits) dto.stop_bit;
                                    serialPortTask.DataType = (DataTypes) dto.data_type;
                                    serialPortTask.SerialPortType = deviceSerialPort;
                                    serialPortTask.CloseConnectionManually = false;
                                    serialPortTask.Connect();
                                } else {
                                    MainUtils.SerialPortTasks.Remove(dto.id);
                                    MainUtils.Log($"SerialPort device[{dto.name}] removed, can't find SerialPort device type [{dto.type}].");
                                }

                                //
                                // serialPortTask.CloseConnection();
                                // MainUtils.SerialPortTasks.Remove(dto.id);
                                // DeviceTypeSerialPort deviceSerialPort = DeviceType_SerialPort.GetById(dto.type);
                                // MainUtils.NewSerialPortTask(dto.id, dto.port_full_name, 
                                //     dto.port_name, dto.baud_rate, (Parity) dto.parity, dto.data_bit, 
                                //     (StopBits) dto.stop_bit, (DataTypes) dto.data_type, deviceSerialPort);
                                // MainUtils.Log($"Connecting to SerialPort[{dto.name}]");
                            } else if (!serialPortTask.Connected && serialPortTask.Status != ATaskBase.CONNECTING) {
                                Task.Run(async () => {
                                    MainUtils.Log($"Disconnected to SerialPort device[{dto.name} - {serialPortTask.SerialPortType.Name}], trying to reconnect...");
                                    await serialPortTask.Connect();
                                    MainUtils.Log($"Reconnected to SerialPort device[{dto.name} - {serialPortTask.SerialPortType.Name}]");
                                });
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
