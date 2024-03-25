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
using System.Data.Common;

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
        [Autowired]
        private BarCodeMatchingRuleService _barCodeMatchingRuleService;
        [Autowired]
        private MissionRecordService _missionRecordService;

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
            List<UserAccountInfo> userAccountInfos;
            if (SystemUtils.IsAdmin) {
                userAccountInfos = _userAccountInfoService.QueryListWithoutUserId();
            } else {
                userAccountInfos = _userAccountInfoService.QueryList(req.UserId);
            }
            userAccountInfos = userAccountInfos.Where(u => u.user_id != -1).ToList();
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

            string? password = userAccountInfo.password;
            string? operation_password = userAccountInfo.operation_password;
            if (password != null && !SystemUtils.IsMD5(password)) {
                userAccountInfo.password = SystemUtils.ToMD5String(password);
            }
            if (operation_password != null && !SystemUtils.IsMD5(operation_password)) {
                userAccountInfo.operation_password = SystemUtils.ToMD5String(operation_password);
            }

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
        // 根据条件查找用户信息，用于新增、编辑的判断
        public FindUserByConditionForCheckingRsp FindUserByConditionForChecking(FindUserByConditionForCheckingReq req) {
            UserAccountInfoDTO? userAccountInfoDTO = null;
            string sql = $"select * from {_userAccountInfoService.TableName} where {_userAccountInfoService.ConditionWithoutUserId}";
            Dictionary<string, object> parameters = new();

            int id = req.Id;
            int? staff_id = req.StaffId;
            string? account = req.Account;
            if (staff_id != null || !string.IsNullOrEmpty(account)) {
                string condition = "";
                if (staff_id != null) {
                    condition += "staff_id = @staff_id";
                    parameters.Add("staff_id", staff_id);
                }
                if (!string.IsNullOrEmpty(account)) {
                    if (!string.IsNullOrEmpty(condition)) {
                        condition += " or ";
                    }
                    condition += "account = @account";
                    parameters.Add("account", account);
                }
                if (!string.IsNullOrEmpty(condition)) {
                    sql += $" and ({condition})";
                }
                if (id > 0) {
                    sql += " and id <> @id";
                    parameters.Add("id", id);
                }
                List<UserAccountInfo> userAccountInfos = _userAccountInfoService.FindBySql(sql + " limit 1", parameters);
                if (userAccountInfos.Count > 0) {
                    userAccountInfoDTO = new();
                    CommonUtils.ObjectConverter<UserAccountInfo, UserAccountInfoDTO>(userAccountInfos[0], userAccountInfoDTO);
                }
            }
            return new() {
                UserAccountInfoDTO = userAccountInfoDTO,
            };
        }
        // 登录验证
        public LoginValidateRsp LoginValidate(LoginValidateReq req) {
            bool succeed = true;
            string failedReason = string.Empty;
            UserAccountInfoDTO? userDTO = null;
            string sql = $"select * from {_userAccountInfoService.TableName} where {_userAccountInfoService.ConditionWithoutUserId}";
            List<UserAccountInfo> users = _userAccountInfoService.FindBySql($"{sql} and account = @account limit 1", new { @account = req.Account });
            if (users.Count <= 0) {
                succeed = false;
                failedReason = "账户名不存在";
            } else {
                UserAccountInfo? user = users.SingleOrDefault(u => u.account == req.Account);
                if (user == null) {
                    succeed = false;
                    failedReason = "账户名不存在";
                } else {
                    userDTO = new();
                    CommonUtils.ObjectConverter<UserAccountInfo, UserAccountInfoDTO>(user, userDTO);
                    
                    string? password = req.Password;
                    if (user.password != null && password != null) {
                        string md5_password = SystemUtils.ToMD5String(password);
                        if (user.password != md5_password && user.password.ToLower() != md5_password.ToLower() && user.password != password) {
                            succeed = false;
                            failedReason = "密码错误";
                        }
                    } else if (string.IsNullOrEmpty(user.password) && !string.IsNullOrEmpty(password) 
                        || !string.IsNullOrEmpty(user.password) && string.IsNullOrEmpty(password)) {
                        succeed = false;
                        failedReason = "密码错误";
                    }
                }
            }
            return new() {
                Succeed = succeed,
                FailedReason = failedReason,
                UserAccountInfoDTO = userDTO,
            };
        }
        // 管理员密码验证
        public AdminPasswordValidateRsp AdminPasswordValidate(AdminPasswordValidateReq req) {
            bool succeed = true;
            string sql = $"select * from {_userAccountInfoService.TableName} where {_userAccountInfoService.ConditionWithoutUserId}";
            List<UserAccountInfo> users = _userAccountInfoService.FindBySql($"{sql} and operation_password = @operation_password limit 1", 
                    new { @operation_password = SystemUtils.ToMD5String(req.AdminPassword) });
            if (users.Count <= 0) {
                succeed = false;
            }
            return new() {
                Succeed = succeed,
            };
        }
        #endregion

        #region 产品任务相关
        // 查询所有未被删除的产品任务列表
        public QueryProductMissionsRsp QueryProductMissions(QueryProductMissionsReq req) {
            // 先查询任务清单
            double start = DateTime.Now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            System.Console.WriteLine($"-------------------------------------------------------------------------------- start");
            List<ProductMission> missions = _productMissionService.QueryListWithoutUserId();
            List<ProductMissionDTO> productMissionDTOs = new();
            CommonUtils.ObjectConverter<ProductMission, ProductMissionDTO>(missions, productMissionDTOs);

            System.Console.WriteLine($"-------------------------------------------------------------------------------- end: {DateTime.Now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds - start}");
            return new() {
                ProductMissionsDTOs = productMissionDTOs
            };
        }
        public QueryProductMissionsWithCoverRsp QueryProductMissionsWithCover(QueryProductMissionsWithCoverReq req) {
            // 先查询任务清单
            double start = DateTime.Now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            System.Console.WriteLine($"-------------------------------------------------------------------------------- start");
            List<ProductMission> missions = _productMissionService.QueryListWithoutUserId();
            System.Console.WriteLine($"-------------------------------------------------------------------------------- _productMissionService.QueryListWithoutUserId: {DateTime.Now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds - start}");
            List<ProductMissionDTO> productMissionDTOs = new();
            CommonUtils.ObjectConverter<ProductMission, ProductMissionDTO>(missions, productMissionDTOs);

            List<ProductSide> sides = new();
            List<ProductBolt> bolts = new();
            // 根据任务查询关联的其他表
            List<int> missionIds = missions.Select(m => m.id).ToList();
            if (missionIds.Count > 0) {
                sides = _productSideService.FindBySqlWithoutUserId($"mission_id in ({string.Join(",", missionIds)})").OrderBy(m => missionIds.IndexOf(m.id)).ToList();
                System.Console.WriteLine($"-------------------------------------------------------------------------------- _productSideService.FindBySqlWithoutUserId: {DateTime.Now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds - start}");
                List<int> boltIds = sides.Select(s => s.id).ToList();
                if (boltIds.Count > 0) {
                    bolts = _productBoltService.FindBySqlWithoutUserId($"side_id in ({string.Join(",", boltIds)})").OrderBy(s => boltIds.IndexOf(s.id)).ToList();
                    System.Console.WriteLine($"-------------------------------------------------------------------------------- _productBoltService.FindBySqlWithoutUserId: {DateTime.Now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds - start}");
                }
            }

            // 根据任务找到第一个side，用该side的图片做封面
            // TODO: 后面再优化这个
            for (int i = 0 ; i < missions.Count ; i++) {
                ProductMissionDTO missionDTO = productMissionDTOs[i];
                List<ProductSideDTO> productSideDTOs = new();
                CommonUtils.ObjectConverter<ProductSide, ProductSideDTO>(sides.Where(m => m.mission_id == missionDTO.id).ToList(), productSideDTOs);
                // 根据当前任务的所有side遍历找到对应的所有bolts
                foreach (ProductSideDTO sideDTO in productSideDTOs) {
                    List<ProductBoltDTO> productBoltDTOs = new();
                    CommonUtils.ObjectConverter<ProductBolt, ProductBoltDTO>(bolts.Where(b => b.side_id == sideDTO.id).ToList(), productBoltDTOs);
                    sideDTO.Bolts = productBoltDTOs;
                }
                // 设定当前mission的所有sides
                productMissionDTOs[i].ProductSides = productSideDTOs;
            }
            System.Console.WriteLine($"-------------------------------------------------------------------------------- end: {DateTime.Now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds - start}");
            return new() {
                ProductMissionsDTOs = productMissionDTOs
            };
        }
        public QueryProductMissionsAndDetailsRsp QueryProductMissionsAndDetails(QueryProductMissionsAndDetailsReq req) {
            // 先查询任务清单
            double start = DateTime.Now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            System.Console.WriteLine($"-------------------------------------------------------------------------------- start");
            List<ProductMission> missions = _productMissionService.QueryListWithoutUserId();
            List<ProductMissionDTO> productMissionDTOs = new();
            CommonUtils.ObjectConverter<ProductMission, ProductMissionDTO>(missions, productMissionDTOs);

            List<ProductSide> sides = new();
            List<ProductBolt> bolts = new();
            // 根据任务查询关联的其他表
            List<int> missionIds = missions.Select(m => m.id).ToList();
            if (missionIds.Count > 0) {
                sides = _productSideService.FindBySqlWithoutUserId($"mission_id in ({string.Join(",", missionIds)})").OrderBy(m => missionIds.IndexOf(m.id)).ToList();
                System.Console.WriteLine($"-------------------------------------------------------------------------------- _productSideService.FindBySqlWithoutUserId: {DateTime.Now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds - start}");
                List<int> boltIds = sides.Select(s => s.id).ToList();
                if (boltIds.Count > 0) {
                    bolts = _productBoltService.FindBySqlWithoutUserId($"side_id in ({string.Join(",", boltIds)})").OrderBy(s => boltIds.IndexOf(s.id)).ToList();
                    System.Console.WriteLine($"-------------------------------------------------------------------------------- _productBoltService.FindBySqlWithoutUserId: {DateTime.Now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds - start}");
                }
            }

            // 根据任务找到第一个side，用该side的图片做封面
            // TODO: 后面再优化这个
            for (int i = 0 ; i < missions.Count ; i++) {
                ProductMissionDTO missionDTO = productMissionDTOs[i];
                List<ProductSideDTO> productSideDTOs = new();
                CommonUtils.ObjectConverter<ProductSide, ProductSideDTO>(sides.Where(m => m.mission_id == missionDTO.id).ToList(), productSideDTOs);
                // 根据当前任务的所有side遍历找到对应的所有bolts
                foreach (ProductSideDTO sideDTO in productSideDTOs) {
                    List<ProductBoltDTO> productBoltDTOs = new();
                    CommonUtils.ObjectConverter<ProductBolt, ProductBoltDTO>(bolts.Where(b => b.side_id == sideDTO.id).ToList(), productBoltDTOs);
                    sideDTO.Bolts = productBoltDTOs;
                }
                // 设定当前mission的所有sides
                productMissionDTOs[i].ProductSides = productSideDTOs;
            }

            System.Console.WriteLine($"-------------------------------------------------------------------------------- end: {DateTime.Now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds - start}");
            return new() {
                ProductMissionsDTOs = productMissionDTOs
            };
        }
        // 新增或修改任务
        public AddOrUpdateProductMissionRsp AddOrUpdateProductMission(AddOrUpdateProductMissionReq req) {
            AddOrUpdateProductMissionRsp rsp = new();
            // 使用同一个connection确保当前所有操作都在同一个事务下
            using DbConnection conn = DbConnector.GetConnection();
            // 开启事务
            using (DbTransaction transaction = conn.BeginTransaction()) {
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
            List<Workstation> workstations = _workstationService.QueryListWithoutUserId();
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

        #region 任务记录相关
        // 查询任务记录列表
        public QueryMissionRecordListRsp QueryMissionRecordList(QueryMissionRecordListReq req) {
            string sql = $"select * from {_missionRecordService.TableName} where {_missionRecordService.ConditionWithoutUserId}";
            Dictionary<string, object> parameters = new();

            string condition = "";
            if (req.Ids != null && req.Ids.Count > 0) {
                condition += " and id in @ids";
                parameters.Add("ids", req.Ids);
            }
            if (req.Date != null) {
                condition += " and create_time between @date1 and @date2";
                string date1 = req.Date.Value.Date.ToString("yyyy-MM-dd HH:mm:ss");
                string date2 = req.Date.Value.Date.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-dd HH:mm:ss");
                parameters.Add("date1", date1);
                parameters.Add("date2", date2);
            }
            if (req.UserId != null) {
                condition += " and user_id = @userId";
                parameters.Add("userId", req.UserId.Value);
            }
            
            List<MissionRecord> missionRecords = _missionRecordService.FindBySql(sql + condition, parameters);
            List<MissionRecordDTO> missionRecordDTOs = new();
            CommonUtils.ObjectConverter<MissionRecord, MissionRecordDTO>(missionRecords, missionRecordDTOs);

            return new() {
                MissionRecordDTOs = missionRecordDTOs,
            };
        }
        // 新增或修改任务记录
        public AddOrUpdateMissionRecordRsp AddOrUpdateMissionRecord(AddOrUpdateMissionRecordReq req) {
            MissionRecordDTO missionRecordDTO = req.MissionRecordDTO;
            MissionRecord missionRecord = new();
            CommonUtils.ObjectConverter<MissionRecordDTO, MissionRecord>(missionRecordDTO, missionRecord);
            MissionRecord? missionRecordNew = _missionRecordService.InsertOrUpdate(missionRecord);
            if (missionRecordNew != null) {
                missionRecordDTO.id = missionRecordNew.id;
            }

            return new() {
                MissionRecordDTO = missionRecordDTO,
            };
        }
        // 检查当前条码是否存在于任务记录表中，用于判断是否需要返工
        public CheckIfBarCodeExistsInMissionRecordRsp CheckIfBarCodeExistsInMissionRecord(CheckIfBarCodeExistsInMissionRecordReq req) {
            string sql = $"select 1 from {_missionRecordService.TableName} where product_bar_code = @product_bar_code";
            return new() {
                Yes = _missionRecordService.FindBySql(sql, new { @product_bar_code = req.ProductBarCode }).Count > 0,
            };
        }
        // 查询最新一条任务记录，用于获取其产品批次，作回填使用
        public QueryLatestMissionRecordRsp QueryLatestMissionRecord(QueryLatestMissionRecordReq req) {
            string sql = $"select * from {_missionRecordService.TableName} order by id desc limit 1";
            List<MissionRecord> missionRecords = _missionRecordService.FindBySql(sql, null);
            QueryLatestMissionRecordRsp rsp = new();
            if (missionRecords.Count > 0) {
                List<MissionRecordDTO>?  missionRecordDTOs = new();
                CommonUtils.ObjectConverter<MissionRecord, MissionRecordDTO>(missionRecords, missionRecordDTOs);
                rsp.MissionRecordDTO = missionRecordDTOs[0];
            }
            return rsp;
        }
        #endregion

        #region 拧紧数据相关
        public QueryOperationDataListRsp QueryOperationDataList(QueryOperationDataListReq req) {
            List<OperationData> operationDatas = _operationDataService.QueryListWithoutUserId();
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
            List<DeviceArm> deviceCategories;
            if (!req.IncludingDeleted) {
                deviceCategories = _deviceArmService.QueryListWithoutUserId();
            } else {
                deviceCategories = _deviceArmService.FindBySql($"select * from {_deviceArmService.TableName}", null);
            }
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
            List<DeviceTool> deviceCategories;
            if (!req.IncludingDeleted) {
                deviceCategories = _deviceToolService.QueryListWithoutUserId();
            } else {
                deviceCategories = _deviceToolService.FindBySql($"select * from {_deviceToolService.TableName}", null);
            }
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
            List<DeviceSerialPort> deviceCategories;
            if (!req.IncludingDeleted) {
                deviceCategories = _deviceSerialPortService.QueryListWithoutUserId();
            } else {
                deviceCategories = _deviceSerialPortService.FindBySql($"select * from {_deviceSerialPortService.TableName}", null);
            }
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
            List<DeviceCommunication> deviceCategories;
            if (!req.IncludingDeleted) {
                deviceCategories = _deviceCommunicationService.QueryListWithoutUserId();
            } else {
                deviceCategories = _deviceCommunicationService.FindBySql($"select * from {_deviceCommunicationService.TableName}", null);
            }
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

        #region 条码匹配规则相关
        // 查询条码匹配规则列表
        public QueryBarCodeMatchingRuleListRsp QueryBarCodeMatchingRuleList(QueryBarCodeMatchingRuleListReq req) {
            string? condition = "";
            if (req.MissionId != null) {
                condition = $"mission_id = {req.MissionId}";
            }
            List<BarCodeMatchingRule> deviceCategories = _barCodeMatchingRuleService.FindBySqlWithoutUserId(condition);
            List<BarCodeMatchingRuleDTO> barCodeMatchingRuleDTOs = new();
            CommonUtils.ObjectConverter<BarCodeMatchingRule, BarCodeMatchingRuleDTO>(deviceCategories, barCodeMatchingRuleDTOs);

            return new() {
                BarCodeMatchingRuleDTOs = barCodeMatchingRuleDTOs,
            };
        }
        // 新增或修改条码匹配规则
        public AddOrUpdateBarCodeMatchingRuleRsp AddOrUpdateBarCodeMatchingRule(AddOrUpdateBarCodeMatchingRuleReq req) {
            BarCodeMatchingRuleDTO barCodeMatchingRuleDTO = req.BarCodeMatchingRuleDTO;
            BarCodeMatchingRule barCodeMatchingRule = new();
            CommonUtils.ObjectConverter<BarCodeMatchingRuleDTO, BarCodeMatchingRule>(barCodeMatchingRuleDTO, barCodeMatchingRule);
            BarCodeMatchingRule? barCodeMatchingRuleNew = _barCodeMatchingRuleService.InsertOrUpdate(barCodeMatchingRule);
            if (barCodeMatchingRuleNew != null) {
                barCodeMatchingRuleDTO.id = barCodeMatchingRuleNew.id;
            }

            return new() {
                BarCodeMatchingRuleDTO = barCodeMatchingRuleDTO,
            };
        }
        // 删除条码匹配规则
        public DeleteBarCodeMatchingRuleByIdsRsp DeleteBarCodeMatchingRule(DeleteBarCodeMatchingRuleByIdsReq req) {
            int deletedRows = _barCodeMatchingRuleService.DeleteByIds(req.Ids);

            DeleteBarCodeMatchingRuleByIdsRsp rsp = new();
            if (deletedRows < req.Ids.Count) {
                rsp.RsponseCode = HttpResponseCode.ERROR;
                rsp.RsponseMessage = $"删除失败！应该删除{req.Ids.Count}条数据，实际只删除了{deletedRows}条数据，请检查！";
            }
            return rsp;
        }
        // 根据任务ID查找对应的条码匹配规则
        public FindBarCodeMatchingRulesByMissionIdRsp FindBarCodeMatchingRulesByMissionId(FindBarCodeMatchingRulesByMissionIdReq req) {
            string sql = $"select * from {_barCodeMatchingRuleService.TableName} where {_barCodeMatchingRuleService.ConditionWithoutUserId} and mission_id = @mission_id";
            Dictionary<string, object> parameters = new();
            parameters.Add("mission_id", req.MissionId);
            if (req.Type != null) {
                sql += " and type = @type";
                parameters.Add("type", req.Type);
            }

            List<BarCodeMatchingRule> barCodeMatchingRules = _barCodeMatchingRuleService.FindBySql(sql, parameters);
            List<BarCodeMatchingRuleDTO> barCodeMatchingRuleDTOs = new();
            CommonUtils.ObjectConverter<BarCodeMatchingRule, BarCodeMatchingRuleDTO>(barCodeMatchingRules, barCodeMatchingRuleDTOs);

            return new() {
                BarCodeMatchingRuleDTOs = barCodeMatchingRuleDTOs,
            };
        }
        #endregion
    }
}
