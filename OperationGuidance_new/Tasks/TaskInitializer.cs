using log4net;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks.AsbtractClasses;
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

                    // Initialize tool tasks
                    List<DeviceToolDTO> toolDTOs = apis.QueryDeviceToolList(new(SystemUtils.MacAddressesDTO.id) { ForTask = true }).DeviceToolDTOs;
                    // Remove tools which had been deleted
                    toolDTOs.ForEach(dto => {
                        if (MainUtils.ToolTasks.ContainsKey(dto.id) && dto.deleted == (int) YesOrNo.YES) {
                            MainUtils.ToolTasks[dto.id].CloseConnection();
                            MainUtils.Info(logger, $"TOOL[{dto.name} - {dto.ip}: {dto.port}] had been deleted, remove it.");
                            MainUtils.RemoveToolTask(dto.id);
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
                                MainUtils.Info(logger, $"Connecting to TOOL[{dto.name} - {dto.ip}: {dto.port} - {deviceTool.Name}]...");
                            }
                        } else {
                            if (toolMaps.ContainsKey(toolTask.DeviceId)) {
                                toolTask.WorkstationId = toolMaps[toolTask.DeviceId];
                            } else {
                                toolTask.WorkstationId = null;
                            }

                            if (toolTask.Ip != dto.ip || toolTask.Port != dto.port || toolTask.ToolType.Id != dto.type) {
                                toolTask.CloseConnection();
                                MainUtils.RemoveToolTask(toolTask.DeviceId);
                                await Task.Delay(toolTask.AutoReconnectingTrialDelay);

                                DeviceTypeTool? deviceTool = DeviceType_Tool.GetById(dto.type);
                                if (deviceTool != null) {
                                    MainUtils.Info(logger, $"TOOL info changed, Reconnecting to TOOL[{dto.name} - {dto.ip}: {dto.port} - {deviceTool.Name}]...");
                                    MainUtils.NewToolTask(dto.id, dto.name, dto.ip, dto.port, deviceTool);
                                } else {
                                    MainUtils.Warn(logger, $"TOOL[{dto.name} - {dto.ip}: {dto.port}] removed, can't find tool type [{dto.type}].");
                                }
                            } else if (!toolTask.Connected && toolTask.Status != ATaskBase.CONNECTING) {
                                Reconnect(toolTask, $"TOOL[{dto.name} - {dto.ip}: {dto.port} - {toolTask.ToolType.Name}]");
                            }
                        }
                    });

                    // Initialize communication tasks
                    List<DeviceCommunicationDTO> communicationDTOs = apis.QueryDeviceCommunicationList(new(SystemUtils.MacAddressesDTO.id) { ForTask = true }).DeviceCommunicationDTOs;
                    // Remove communications which had been deleted
                    communicationDTOs.ForEach(dto => {
                        if (MainUtils.CommunicationTasks.ContainsKey(dto.id) && dto.deleted == (int) YesOrNo.YES) {
                            MainUtils.CommunicationTasks[dto.id].CloseConnection();
                            MainUtils.Info(logger, $"Communication device[{dto.name} - {dto.ip}: {dto.port}] had been deleted, remove it.");
                            MainUtils.RemoveCommunicationTask(dto.id);
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
                                MainUtils.Info(logger, $"Connecting to Communication device[{dto.name} - {dto.ip}: {dto.port} - {deviceCommunication.Name}]...");
                            }
                        } else {
                            if (communicationMaps.ContainsKey(communicationTask.DeviceId)) {
                                communicationTask.WorkstationId = communicationMaps[communicationTask.DeviceId];
                            } else {
                                communicationTask.WorkstationId = null;
                            }

                            if (communicationTask.Ip != dto.ip || communicationTask.Port != dto.port || communicationTask.CommunicationType.Id != dto.type) {
                                communicationTask.CloseConnection();
                                MainUtils.RemoveCommunicationTask(communicationTask.DeviceId);
                                await Task.Delay(communicationTask.AutoReconnectingTrialDelay);

                                DeviceTypeCommunication? deviceCommunication = DeviceType_Communication.GetById(dto.type);
                                if (deviceCommunication != null) {
                                    MainUtils.Info(logger, $"Communication device info changed, Reconnecting to Communication device[{dto.name} - {dto.ip}: {dto.port} - {deviceCommunication.Name}]...");
                                    MainUtils.NewCommunicationTask(dto.id, dto.name, dto.ip, dto.port, deviceCommunication);
                                } else {
                                    MainUtils.Warn(logger, $"Communication device[{dto.name} - {dto.ip}: {dto.port}] removed, can't find Communication device type [{dto.type}].");
                                }
                            } else if (!communicationTask.Connected && communicationTask.Status != ATaskBase.CONNECTING) {
                                Reconnect(communicationTask, $"Communication device[{dto.name} - {dto.ip}: {dto.port} - {communicationTask.CommunicationType.Name}]");
                            }
                        }
                    });

                    // Initialize serialPort tasks
                    List<DeviceSerialPortDTO> serialPortDTOs = apis.QueryDeviceSerialPortList(new(SystemUtils.MacAddressesDTO.id) { ForTask = true }).DeviceSerialPortDTOs;
                    // Remove serialPorts which had been deleted
                    serialPortDTOs.ForEach(dto => {
                        if (MainUtils.SerialPortTasks.ContainsKey(dto.id) && dto.deleted == (int) YesOrNo.YES) {
                            MainUtils.SerialPortTasks[dto.id].CloseConnection();
                            MainUtils.Info(logger, $"SerialPort device[{dto.name}] had been deleted, remove it.");
                            MainUtils.RemoveSerialPortTask(dto.id);
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
                                MainUtils.Info(logger, $"Connecting to SerialPort device[{dto.name} - {deviceSerialPort.Name}]");
                            }
                        } else {
                            if (serialPortMaps.ContainsKey(serialPortTask.DeviceId)) {
                                serialPortTask.WorkstationId = serialPortMaps[serialPortTask.DeviceId];
                            } else {
                                serialPortTask.WorkstationId = null;
                            }

                            if (serialPortTask.PortName != dto.port_name || serialPortTask.BaudRate != dto.baud_rate
                                        || (int) serialPortTask.Parity != dto.parity || serialPortTask.DataBits != dto.data_bit
                                        || (int) serialPortTask.StopBits != dto.stop_bit || (int) serialPortTask.DataType != dto.data_type
                                        || serialPortTask.SerialPortType.Id != dto.type) {
                                serialPortTask.CloseConnection();
                                MainUtils.RemoveSerialPortTask(serialPortTask.DeviceId);
                                await Task.Delay(serialPortTask.AutoReconnectingTrialDelay);

                                DeviceTypeSerialPort? deviceSerialPort = DeviceType_SerialPort.GetById(dto.type);
                                if (deviceSerialPort != null) {
                                    MainUtils.Info(logger, $"SerialPort device info changed, Reconnecting to SerialPort device[{dto.name} - {serialPortTask.SerialPortType.Name}]");
                                    MainUtils.NewSerialPortTask(dto.id, dto.port_full_name,
                                        dto.port_name, dto.baud_rate, (Parity) dto.parity, dto.data_bit,
                                        (StopBits) dto.stop_bit, (DataTypes) dto.data_type, deviceSerialPort);
                                } else {
                                    MainUtils.Warn(logger, $"SerialPort device[{dto.name}] removed, can't find SerialPort device type [{dto.type}].");
                                }
                            } else if (!serialPortTask.Connected && serialPortTask.Status != ATaskBase.CONNECTING) {
                                Reconnect(serialPortTask, $"SerialPort device[{dto.name} - {serialPortTask.SerialPortType.Name}]");
                            }
                        }
                    });

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

                    // Delay in task looping
                    await Task.Delay(LoopingDelay);
                }

                async void Reconnect(ATaskBase task, string deviceInfo) {
                    await Task.Run(async () => {
                        MainUtils.Warn(logger, $"Disconnected to {deviceInfo}, trying to reconnect...");
                        await task.ConnectAsync();
                        MainUtils.Info(logger, $"Reconnected to {deviceInfo}");
                    });
                }
            });
        }
    }
}
