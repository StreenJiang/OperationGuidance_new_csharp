using System.Data;
using OperationGuidance_service.Attributes;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Services;
using OperationGuidance_service.Database;
using OperationGuidance_service.Models;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Requests;
using OperationGuidance_service.Models.Responses;
using OperationGuidance_service.Utils;
using System.Data.SQLite;

namespace OperationGuidance_service.Controllers {
    [Api]
    public sealed class OperationGuidanceApis {
        [Autowired]
        private UserAccountInfoService _userAccountInfoService;
        [Autowired]
        private ProductMissionService _productMissionService;
        [Autowired]
        private ProductSideService _productSideService;
        [Autowired]
        private ProductBoltService _productBoltService;
        [Autowired]
        private DeviceArmService _deviceArmService;
        [Autowired]
        private DeviceToolService _deviceToolService;
        [Autowired]
        private DeviceSerialPortService _deviceSerialPortService;
        [Autowired]
        private DeviceCommunicationService _deviceCommunicationService;
        [Autowired]
        private WorkstationService _workstationService;
        [Autowired]
        private OperationDataService _operationDataService;

        #region 用户账户信息相关
        // 根据用户ID查询用户信息
        public FindUserByIdRsp FindUserById(FindUserByIdReq req) {
            FindUserByIdRsp rsp = new();
            UserAccountInfo? userAccountInfo = _userAccountInfoService.FindById(req.UserId);
            if (userAccountInfo != null) {
                CommonUtils.ObjectConverter<UserAccountInfo, UserAccountInfoDTO>(userAccountInfo, rsp.UserAccountInfoDTO);
            }
            return rsp;
        }
        // 查询用户列表
        public QueryUserAccountInfoListRsp QueryUserAccountInfoList(QueryUserAccountInfoListReq req) {
            List<UserAccountInfo> userAccountInfos = _userAccountInfoService.QueryList(req.UserId);
            List<UserAccountInfoDTO> userAccountInfoDTOs = new();
            CommonUtils.ObjectConverter<UserAccountInfo, UserAccountInfoDTO>(userAccountInfos, userAccountInfoDTOs);

            return new() {
                UserAccountInfoDTOs = userAccountInfoDTOs,
            };
        }
        // 新增用户
        public AddOrUpdateUserAccountInfoRsp AddOrUpdateUserAccountInfo(AddOrUpdateUserAccountInfoReq req) {
            UserAccountInfoDTO userAccountInfoDTO = req.UserAccountInfoDTO;
            UserAccountInfo userAccountInfo = new();
            CommonUtils.ObjectConverter<UserAccountInfoDTO, UserAccountInfo>(userAccountInfoDTO, userAccountInfo);
            UserAccountInfo? userAccountInfoNew = _userAccountInfoService.InsertOrUpdate(userAccountInfo);
            if (userAccountInfoNew != null) {
                userAccountInfoDTO.id = userAccountInfoNew.id;
            }

            return new() {
                UserAccountInfoDTO = userAccountInfoDTO,
            };
        }
        // 删除用户
        public DeleteUserAccountInfoByIdsRsp DeleteUserAccountInfo(DeleteUserAccountInfoByIdsReq req) {
            int deletedRows = _userAccountInfoService.DeleteByIds(req.Ids);

            DeleteUserAccountInfoByIdsRsp rsp = new();
            if (deletedRows < req.Ids.Count) {
                rsp.RsponseCode = HttpResponseCode.ERROR;
                rsp.RsponseMessage = $"删除失败！应该删除{req.Ids.Count}条数据，实际只删除了{deletedRows}条数据，请检查！";
            }
            return rsp;
        }
        public CheckUserAccountExistsRsp CheckUserAccountExists(CheckUserAccountExistsReq req) {
            bool accountExists = false;
            string? account = req.Account;
            if (!string.IsNullOrEmpty(account)) {
                List<UserAccountInfo> userAccountInfos = _userAccountInfoService.FindBySqlCondition($"account = '{account}'");
                if (userAccountInfos.Count > 0) {
                    accountExists = true;
                }
            }
            return new() {
                Exists = accountExists,
            };
        }
        #endregion

        #region 产品任务相关
        // 查询所有未被删除的产品任务列表
        public QueryProductMissionListRsp QueryProductMissionListRsp(QueryProductMissionListReq req) {
            // 先查询任务清单
            List<ProductMission> missions = _productMissionService.QueryList(req.UserId);
            List<ProductMissionDTO> productMissionDTOs = new();
            CommonUtils.ObjectConverter<ProductMission, ProductMissionDTO>(missions, productMissionDTOs);

            // 根据任务查询关联的其他表
            for (int i = 0 ; i < missions.Count ; i++) {
                ProductMission mission = missions[i];
                ProductMissionDTO productMissionDTO = productMissionDTOs[i];

                // 根据mission_id查询产品面列表
                List<ProductSide> sides = _productSideService.FindBySqlCondition($"mission_id = {mission.id}");
                if (sides.Count > 0) {
                    // 将产品面信息存入任务对象
                    List<ProductSideDTO> productSideDTOs = new();
                    CommonUtils.ObjectConverter<ProductSide, ProductSideDTO>(sides, productSideDTOs);
                    productMissionDTO.ProductSides = productSideDTOs;

                    // 循环产品面查询螺栓点位信息
                    for (int j = 0 ; j < sides.Count ; j++) {
                        ProductSide side = sides[j];
                        ProductSideDTO productSideDTO = productSideDTOs[j];

                        List<ProductBolt> bolts = _productBoltService.FindBySqlCondition($"side_id = {side.id}");
                        if (bolts.Count > 0) {
                            // 将螺栓点位信息存入产品面对象
                            List<ProductBoltDTO> productBoltDTOs = new();
                            CommonUtils.ObjectConverter<ProductBolt, ProductBoltDTO>(bolts, productBoltDTOs);
                            productSideDTO.Bolts = productBoltDTOs;
                        }
                    }
                }
            }

            return new() {
                ProductMissionsDTOs = productMissionDTOs
            };
        }
        // 新增或修改任务
        public AddOrUpdateProductMissionRsp AddOrUpdateProductMission(AddOrUpdateProductMissionReq req) {
            AddOrUpdateProductMissionRsp rsp = new();
            // 使用同一个connection确保当前所有操作都在同一个事务下
            using SQLiteConnection conn = DbConnector.GetConnection();
            // 开启事务
            using (SQLiteTransaction transaction = conn.BeginTransaction()) {
                _productMissionService.UseConnection(conn);
                _productSideService.UseConnection(conn);
                _productBoltService.UseConnection(conn);
                try {
                    ProductMissionDTO missionDTOReq = req.ProductMissionDTO;
                    ProductMission? mission = _productMissionService.FindById(missionDTOReq.id);
                    if (mission == null) {
                        mission = new();
                    }
                    // 将请求中的数据转移到entity中
                    CommonUtils.ObjectConverter<ProductMissionDTO, ProductMission>(missionDTOReq, mission);
                    // 执行插入或者更新操作
                    mission = _productMissionService.InsertOrUpdate(mission);

                    // 判断是否成功保存到数据库
                    if (mission != null) {
                        ProductMissionDTO missionDTORsp = new();
                        // 将保存好的数据放到rsp中
                        CommonUtils.ObjectConverter<ProductMission, ProductMissionDTO>(mission, missionDTORsp);

                        // 如果有产品面信息，则存起来
                        if (missionDTOReq.ProductSides != null) {
                            missionDTORsp.ProductSides = AddOrUpdateProductSides(mission.id, missionDTOReq.ProductSides);
                        }
                        rsp.ProductMissionDTO = missionDTORsp;
                    } else {
                        throw new DataException("Insert or Update ProductMission failed, please check.");
                    }

                    // 保存数据，结束事务
                    transaction.Commit();
                } catch (Exception e) {
                    Console.WriteLine("AddProductMission error: " + e);
                    rsp.RsponseCode = HttpResponseCode.ERROR;
                    rsp.RsponseMessage = e.Message;

                    transaction.Rollback();
                } finally { 
                    _productMissionService.ReleaseConnection();
                    _productSideService.ReleaseConnection();
                    _productBoltService.ReleaseConnection();
                }
            }
            return rsp;
        }
        private List<ProductSideDTO> AddOrUpdateProductSides(int missionId, List<ProductSideDTO> sideDTOsReq) {
            List<ProductSideDTO> sideDTOsRsp = new();
            foreach (ProductSideDTO sideDTOReq in sideDTOsReq) {
                ProductSide? side = _productSideService.FindById(sideDTOReq.id);
                if (side == null) {
                    side = new();
                }
                // 将请求中的数据转移到eneity中
                CommonUtils.ObjectConverter<ProductSideDTO, ProductSide>(sideDTOReq, side);
                side.mission_id = missionId;
                // 执行插入或者更新操作
                side = _productSideService.InsertOrUpdate(side);

                // 判断是否成功存入数据库
                if (side != null) {
                    ProductSideDTO sideRsp = new();
                    // 将保存好的数据放到rsp中
                    CommonUtils.ObjectConverter<ProductSide, ProductSideDTO>(side, sideRsp);
                    if (sideDTOReq.Bolts != null) {
                        sideRsp.Bolts = AddOrUpdateProductBolts(side.id, sideDTOReq.Bolts);
                    }
                    sideDTOsRsp.Add(sideRsp);
                } else {
                    throw new DataException("Insert or Update ProductSide failed, please check.");
                }
            }
            return sideDTOsRsp;
        }
        private List<ProductBoltDTO> AddOrUpdateProductBolts(int sideId, List<ProductBoltDTO> boltDTOsReq) {
            List<ProductBoltDTO> boltDTOsRsp = new();
            foreach (ProductBoltDTO boltDTOReq in boltDTOsReq) {
                ProductBoltDTO boltRsp = new();
                ProductBolt? bolt = _productBoltService.FindById(boltDTOReq.id);
                if (bolt == null) {
                    bolt = new();
                }
                // 将请求中的数据转移到eneity中
                CommonUtils.ObjectConverter<ProductBoltDTO, ProductBolt>(boltDTOReq, bolt);
                bolt.side_id = sideId;
                // 执行插入或者更新操作
                bolt = _productBoltService.InsertOrUpdate(bolt);

                // 判断是否成功存入数据库
                if (bolt != null) {
                    // 将保存好的数据放到rsp中
                    CommonUtils.ObjectConverter<ProductBolt, ProductBoltDTO>(bolt, boltRsp);
                    boltDTOsRsp.Add(boltRsp);
                } else {
                    throw new DataException("Insert or Update ProductBolt failed, please check.");
                }
            }
            return boltDTOsRsp;
        }
        // 删除任务
        public DeleteProductMissionRsp DeleteProductMission(DeleteProductMissionReq req) {
            DeleteProductMissionRsp rsp = new();
            ProductMission productMission = new();
            CommonUtils.ObjectConverter<ProductMissionDTO, ProductMission>(req.ProductMissionDTO, productMission);
            if (!_productMissionService.DeleteEntity(productMission)) {
                rsp.RsponseCode = HttpResponseCode.ERROR;
                rsp.RsponseMessage = "Delete failed, don't know what happened.";
            }
            return rsp;
        }
        #endregion

        #region 站点（或者叫工作站、工位？）相关
        // 查询站点列表
        public QueryWorkstationListRsp QueryWorkstationList(QueryWorkstationListReq req) {
            List<Workstation> workstations = _workstationService.QueryList(req.UserId);
            List<WorkstationDTO> workstationDTOs = new();
            CommonUtils.ObjectConverter<Workstation, WorkstationDTO>(workstations, workstationDTOs);

            foreach (WorkstationDTO dto in workstationDTOs) {
                if (dto.tool_id != null) {
                    DeviceTool? tool = _deviceToolService.FindById(dto.tool_id.Value);
                    if (tool != null) {
                        dto.tool_name = tool.name;
                        dto.tool_description = tool.description;
                        dto.tool_ip = tool.ip;
                        dto.tool_port = tool.port;
                        dto.tool_type = tool.type;
                    }
                }
                if (dto.arm_id != null) {
                    DeviceArm? arm = _deviceArmService.FindById(dto.arm_id.Value);
                    if (arm != null) {
                        dto.arm_name = arm.name;
                        dto.arm_description = arm.description;
                        dto.arm_ip = arm.ip;
                        dto.arm_port = arm.port;
                        dto.arm_type = arm.type;
                    }
                }
                if (dto.serial_port_id != null) {
                    DeviceSerialPort? serial_port = _deviceSerialPortService.FindById(dto.serial_port_id.Value);
                    if (serial_port != null) {
                        dto.serial_port_name = serial_port.name;
                        dto.serial_port_description = serial_port.description;
                        dto.serial_port_port_name = serial_port.port_name;
                        dto.serial_port_baud_rate = serial_port.baud_rate;
                        dto.serial_port_data_bit = serial_port.data_bit;
                        dto.serial_port_parity = serial_port.parity;
                        dto.serial_port_stop_bit = serial_port.stop_bit;
                        dto.serial_port_data_type = serial_port.data_type;
                        dto.serial_port_type = serial_port.type;
                    }
                }
                if (dto.communication_id != null) {
                    DeviceCommunication? communication = _deviceCommunicationService.FindById(dto.communication_id.Value);
                    if (communication != null) {
                        dto.communication_name = communication.name;
                        dto.communication_description = communication.description;
                        dto.communication_ip = communication.ip;
                        dto.communication_port = communication.port;
                        dto.communication_type = communication.type;
                    }
                }
            }            

            return new() {
                WorkstationsDTOs = workstationDTOs,
            };
        }
        // 新增或修改站点
        public AddOrUpdateWorkstationRsp AddOrUpdateWorkstation(AddOrUpdateWorkstationReq req) {
            WorkstationDTO workstationDTO = req.WorkstationDTO;
            Workstation workstation = new();
            CommonUtils.ObjectConverter<WorkstationDTO, Workstation>(workstationDTO, workstation);
            Workstation? workstationNew = _workstationService.InsertOrUpdate(workstation);
            if (workstationNew != null) {
                workstationDTO.id = workstationNew.id;
            }

            return new() {
                WorkstationDTO = workstationDTO,
            };
        }
        // 删除站点
        public DeleteWorkstationByIdsRsp DeleteWorkstation(DeleteWorkstationByIdsReq req) {
            int deletedRows = _workstationService.DeleteByIds(req.Ids);

            DeleteWorkstationByIdsRsp rsp = new();
            if (deletedRows < req.Ids.Count) {
                rsp.RsponseCode = HttpResponseCode.ERROR;
                rsp.RsponseMessage = $"删除失败！应该删除{req.Ids.Count}条数据，实际只删除了{deletedRows}条数据，请检查！";
            }
            return rsp;
        }
        #endregion

        #region 数据查询相关
        public QueryOperationDataListRsp QueryOperationDataList(QueryOperationDataListReq req) {
            List<OperationData> operationDatas = _operationDataService.QueryList(req.UserId);
            List<OperationDataDTO> operationDataDTOs = new();
            CommonUtils.ObjectConverter<OperationData, OperationDataDTO>(operationDatas, operationDataDTOs);

            return new() {
                OperationDataDTOs = operationDataDTOs,
            };
        }
        // 批量数据插入
        public BatchAddOperationDataRsp BatchAddOperationData(BatchAddOperationDataReq req) {
            List<OperationDataDTO> operationDataDTOs = req.OperationDataDTOs;
            List<OperationData> operationDatas = new();
            CommonUtils.ObjectConverter<OperationDataDTO, OperationData>(operationDataDTOs, operationDatas);
            int num = _operationDataService.AddBatch(operationDatas);

            return new() {
                Num = num,
            };
        }
        #endregion

        #region 力臂相关
        // 查询力臂列表
        public QueryDeviceArmListRsp QueryDeviceArmList(QueryDeviceArmListReq req) {
            List<DeviceArm> deviceCategories = _deviceArmService.QueryList(req.UserId);
            List<DeviceArmDTO> deviceArmDTOs = new();
            CommonUtils.ObjectConverter<DeviceArm, DeviceArmDTO>(deviceCategories, deviceArmDTOs);

            return new() {
                DeviceArmDTOs = deviceArmDTOs,
            };
        }
        // 新增或修改力臂
        public AddOrUpdateDeviceArmRsp AddOrUpdateDeviceArm(AddOrUpdateDeviceArmReq req) {
            DeviceArmDTO deviceArmDTO = req.DeviceArmDTO;
            DeviceArm deviceArm = new();
            CommonUtils.ObjectConverter<DeviceArmDTO, DeviceArm>(deviceArmDTO, deviceArm);
            DeviceArm? deviceArmNew = _deviceArmService.InsertOrUpdate(deviceArm);
            if (deviceArmNew != null) {
                deviceArmDTO.id = deviceArmNew.id;
            }

            return new() {
                DeviceArmDTO = deviceArmDTO,
            };
        }
        // 删除力臂
        public DeleteDeviceArmByIdsRsp DeleteDeviceArm(DeleteDeviceArmByIdsReq req) {
            int deletedRows = _deviceArmService.DeleteByIds(req.Ids);

            DeleteDeviceArmByIdsRsp rsp = new();
            if (deletedRows < req.Ids.Count) {
                rsp.RsponseCode = HttpResponseCode.ERROR;
                rsp.RsponseMessage = $"删除失败！应该删除{req.Ids.Count}条数据，实际只删除了{deletedRows}条数据，请检查！";
            }
            return rsp;
        }
        #endregion

        #region 工具相关
        // 查询工具列表
        public QueryDeviceToolListRsp QueryDeviceToolList(QueryDeviceToolListReq req) {
            List<DeviceTool> deviceCategories = _deviceToolService.QueryList(req.UserId);
            List<DeviceToolDTO> deviceToolDTOs = new();
            CommonUtils.ObjectConverter<DeviceTool, DeviceToolDTO>(deviceCategories, deviceToolDTOs);

            return new() {
                DeviceToolDTOs = deviceToolDTOs,
            };
        }
        // 新增或修改工具
        public AddOrUpdateDeviceToolRsp AddOrUpdateDeviceTool(AddOrUpdateDeviceToolReq req) {
            DeviceToolDTO deviceToolDTO = req.DeviceToolDTO;
            DeviceTool deviceTool = new();
            CommonUtils.ObjectConverter<DeviceToolDTO, DeviceTool>(deviceToolDTO, deviceTool);
            DeviceTool? deviceToolNew = _deviceToolService.InsertOrUpdate(deviceTool);
            if (deviceToolNew != null) {
                deviceToolDTO.id = deviceToolNew.id;
            }

            return new() {
                DeviceToolDTO = deviceToolDTO,
            };
        }
        // 删除工具
        public DeleteDeviceToolByIdsRsp DeleteDeviceTool(DeleteDeviceToolByIdsReq req) {
            int deletedRows = _deviceToolService.DeleteByIds(req.Ids);

            DeleteDeviceToolByIdsRsp rsp = new();
            if (deletedRows < req.Ids.Count) {
                rsp.RsponseCode = HttpResponseCode.ERROR;
                rsp.RsponseMessage = $"删除失败！应该删除{req.Ids.Count}条数据，实际只删除了{deletedRows}条数据，请检查！";
            }
            return rsp;
        }
        #endregion

        #region 串口设备相关
        // 查询串口设备列表
        public QueryDeviceSerialPortListRsp QueryDeviceSerialPortList(QueryDeviceSerialPortListReq req) {
            List<DeviceSerialPort> deviceCategories = _deviceSerialPortService.QueryList(req.UserId);
            List<DeviceSerialPortDTO> deviceSerialPortDTOs = new();
            CommonUtils.ObjectConverter<DeviceSerialPort, DeviceSerialPortDTO>(deviceCategories, deviceSerialPortDTOs);

            return new() {
                DeviceSerialPortDTOs = deviceSerialPortDTOs,
            };
        }
        // 新增或修改串口设备
        public AddOrUpdateDeviceSerialPortRsp AddOrUpdateDeviceSerialPort(AddOrUpdateDeviceSerialPortReq req) {
            DeviceSerialPortDTO deviceSerialPortDTO = req.DeviceSerialPortDTO;
            DeviceSerialPort deviceSerialPort = new();
            CommonUtils.ObjectConverter<DeviceSerialPortDTO, DeviceSerialPort>(deviceSerialPortDTO, deviceSerialPort);
            DeviceSerialPort? deviceSerialPortNew = _deviceSerialPortService.InsertOrUpdate(deviceSerialPort);
            if (deviceSerialPortNew != null) {
                deviceSerialPortDTO.id = deviceSerialPortNew.id;
            }

            return new() {
                DeviceSerialPortDTO = deviceSerialPortDTO,
            };
        }
        // 删除串口设备
        public DeleteDeviceSerialPortByIdsRsp DeleteDeviceSerialPort(DeleteDeviceSerialPortByIdsReq req) {
            int deletedRows = _deviceSerialPortService.DeleteByIds(req.Ids);

            DeleteDeviceSerialPortByIdsRsp rsp = new();
            if (deletedRows < req.Ids.Count) {
                rsp.RsponseCode = HttpResponseCode.ERROR;
                rsp.RsponseMessage = $"删除失败！应该删除{req.Ids.Count}条数据，实际只删除了{deletedRows}条数据，请检查！";
            }
            return rsp;
        }
        #endregion

        #region 通讯设备相关
        // 查询通讯设备列表
        public QueryDeviceCommunicationListRsp QueryDeviceCommunicationList(QueryDeviceCommunicationListReq req) {
            List<DeviceCommunication> deviceCategories = _deviceCommunicationService.QueryList(req.UserId);
            List<DeviceCommunicationDTO> deviceCommunicationDTOs = new();
            CommonUtils.ObjectConverter<DeviceCommunication, DeviceCommunicationDTO>(deviceCategories, deviceCommunicationDTOs);

            return new() {
                DeviceCommunicationDTOs = deviceCommunicationDTOs,
            };
        }
        // 新增或修改通讯设备
        public AddOrUpdateDeviceCommunicationRsp AddOrUpdateDeviceCommunication(AddOrUpdateDeviceCommunicationReq req) {
            DeviceCommunicationDTO deviceCommunicationDTO = req.DeviceCommunicationDTO;
            DeviceCommunication deviceCommunication = new();
            CommonUtils.ObjectConverter<DeviceCommunicationDTO, DeviceCommunication>(deviceCommunicationDTO, deviceCommunication);
            DeviceCommunication? deviceCommunicationNew = _deviceCommunicationService.InsertOrUpdate(deviceCommunication);
            if (deviceCommunicationNew != null) {
                deviceCommunicationDTO.id = deviceCommunicationNew.id;
            }

            return new() {
                DeviceCommunicationDTO = deviceCommunicationDTO,
            };
        }
        // 删除通讯设备
        public DeleteDeviceCommunicationByIdsRsp DeleteDeviceCommunication(DeleteDeviceCommunicationByIdsReq req) {
            int deletedRows = _deviceCommunicationService.DeleteByIds(req.Ids);

            DeleteDeviceCommunicationByIdsRsp rsp = new();
            if (deletedRows < req.Ids.Count) {
                rsp.RsponseCode = HttpResponseCode.ERROR;
                rsp.RsponseMessage = $"删除失败！应该删除{req.Ids.Count}条数据，实际只删除了{deletedRows}条数据，请检查！";
            }
            return rsp;
        }
        #endregion

    }
}
