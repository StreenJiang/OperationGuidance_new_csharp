using System.IO.Ports;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Tasks {
    public static class TaskInitializer {
        private static OperationGuidanceApis apis = SystemUtils.GetApis();

        public static void Init() {
            // Initialize tool tasks
            List<DeviceToolDTO> toolDTOs = apis.QueryDeviceToolList(new()).DeviceToolDTOs;
            foreach (DeviceToolDTO dto in toolDTOs) {
                if (dto.ip != null && dto.port != null && dto.type != null) {
                    DeviceTool? deviceTool = DeviceType_Tool.GetById(dto.type.Value);
                    if (deviceTool != null) {
                        MainUtils.NewToolTask(dto.id, dto.ip, dto.port.Value, deviceTool);
                    }
                }
            }

            // Initialize arm tasks
            List<DeviceArmDTO> armDTOs = apis.QueryDeviceArmList(new()).DeviceArmDTOs;
            foreach (DeviceArmDTO dto in armDTOs) {
                if (dto.ip != null && dto.port != null && dto.type != null) {
                    DeviceArm? deviceArm = DeviceType_Arm.GetById(dto.type.Value);
                    if (deviceArm != null) {
                        MainUtils.NewArmTask(dto.id, dto.ip, dto.port.Value, deviceArm);
                    }
                }
            }

            // Initialize serialPort tasks
            List<DeviceSerialPortDTO> serialPortDTOs = apis.QueryDeviceSerialPortList(new()).DeviceSerialPortDTOs;
            foreach (DeviceSerialPortDTO dto in serialPortDTOs) {
                if (dto.type != null && dto.port_name != null && dto.port_full_name != null 
                    && dto.baud_rate != null && dto.parity != null && dto.data_bit != null 
                    && dto.stop_bit != null && dto.data_type != null) {
                    DeviceSerialPort? deviceSerialPort = DeviceType_SerialPort.GetById(dto.type.Value);
                    if (deviceSerialPort != null) {
                        MainUtils.NewSerialPortTask(dto.id, dto.port_full_name, 
                            dto.port_name, dto.baud_rate.Value, (Parity) dto.parity, dto.data_bit.Value, 
                            (StopBits) dto.stop_bit, (DataTypes) dto.data_type, deviceSerialPort);
                    }
                }
            }
        }
    }
}
