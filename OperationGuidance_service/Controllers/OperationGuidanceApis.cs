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
using log4net;
using OperationGuidance_service.Services.AbstractClasses;
using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Wrapper.AbstractClasses;
using Newtonsoft.Json;

namespace OperationGuidance_service.Controllers {
    [Api]
    public sealed class OperationGuidanceApis {
        private ILog logger = SystemUtils.GetLogger(typeof(OperationGuidanceApis));

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
        private DeviceIoService _deviceIoService;
        [Autowired]
        private WorkstationService _workstationService;
        [Autowired]
        private OperationDataService _operationDataService;
        [Autowired]
        private BarCodeMatchingRuleService _barCodeMatchingRuleService;
        [Autowired]
        private MissionRecordService _missionRecordService;
        [Autowired]
        private MacAddressesService _macAddressesService;
        [Autowired]
        private CurveDataService _curveDataService;
        [Autowired]
        private OuterDatabaseConfigGlbService _outerDatabaseConfigGlbService;
        [Autowired]
        private DapperDBService _dapperDBService;
        [Autowired]
        private ScrewBitCounterService _screwBitCounterService;
        [Autowired]
        private MatCodeMapWhycService _matCodeMapWhycService;

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
                List<UserAccountInfo> userAccountInfos = _userAccountInfoService.FindBySql(sql, parameters);
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
            List<UserAccountInfo> users = _userAccountInfoService.FindBySql($"{sql} and account = @account", new() { { "@account", req.Account } });
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
            List<UserAccountInfo> users = _userAccountInfoService.FindBySql($"{sql} and operation_password = @operation_password",
                    new() { { "@operation_password", SystemUtils.ToMD5String(req.AdminPassword) } });
            if (users.Count <= 0) {
                succeed = false;
            }
            return new() {
                Succeed = succeed,
            };
        }
        #endregion

        #region Mac地址相关
        public AddOrUpdateMacAddressesRsp AddOrUpdateMacAddresses(AddOrUpdateMacAddressesReq req) {
            MacAddressesDTO macAddressesDTO = req.MacAddressesDTO;
            MacAddresses macAddresses = new();
            CommonUtils.ObjectConverter<MacAddressesDTO, MacAddresses>(macAddressesDTO, macAddresses);
            MacAddresses? macAddressesNew = _macAddressesService.InsertOrUpdate(macAddresses);
            if (macAddressesNew != null) {
                macAddressesDTO.id = macAddressesNew.id;
                return new() { MacAddressesDTO = macAddressesDTO };
            }

            return new();
        }
        public FindMacAddressesRsp FindMacAddressesByMacs(FindMacAddressesByMacsReq req) {
            List<string> macs = req.Macs;
            if (macs.Count > 0) {
                string sql = $"select * from {_macAddressesService.TableName} where 1 = 1";
                Dictionary<string, object> parameters = new();
                sql += " and (";
                for (int i = 0; i < macs.Count; i++) {
                    string mac = macs[i];
                    if (i > 0) {
                        sql += " or ";
                    }
                    sql += $"macs like @mac{i}";
                    parameters.Add($"mac{i}", $"%{mac}%");
                }
                sql += ")";

                List<MacAddresses> macAddresses = _macAddressesService.FindBySql(sql, parameters);
                if (macAddresses.Count > 0) {
                    MacAddressesDTO macAddressesDTO = new();
                    CommonUtils.ObjectConverter<MacAddresses, MacAddressesDTO>(macAddresses[0], macAddressesDTO);
                    return new() { MacAddressesDTO = macAddressesDTO };
                }
            }

            return new();
        }
        public FindMacAddressesRsp FindMacAddressesById(FindMacAddressesByIdReq req) {
            string sql = $"select * from {_macAddressesService.TableName} where id = @id";
            Dictionary<string, object> parameters = new();
            parameters.Add("id", $"{req.Id}");

            List<MacAddresses> macAddresses = _macAddressesService.FindBySql(sql, parameters);
            if (macAddresses.Count > 0) {
                MacAddressesDTO macAddressesDTO = new();
                CommonUtils.ObjectConverter<MacAddresses, MacAddressesDTO>(macAddresses[0], macAddressesDTO);
                return new() { MacAddressesDTO = macAddressesDTO };
            }

            return new();
        }
        public UpdateMacsIdsRsp UpdateMacsIds(UpdateMacsIdsReq req) {
            string sqlTemp = "Update {0} set macs_id = @macs_id_to where macs_id = @macs_id_from";
            Dictionary<string, object> parameters = new();
            parameters.Add("macs_id_to", req.IdTo);
            parameters.Add("macs_id_from", req.IdFrom);

            int count = 0;
            List<string> failedTables = new();
            // 条码规则
            count += UpdateMacsIdsInner(_barCodeMatchingRuleService, sqlTemp, parameters, failedTables);
            // 力臂
            count += UpdateMacsIdsInner(_deviceArmService, sqlTemp, parameters, failedTables);
            // 工具
            count += UpdateMacsIdsInner(_deviceToolService, sqlTemp, parameters, failedTables);
            // 通讯设备
            count += UpdateMacsIdsInner(_deviceCommunicationService, sqlTemp, parameters, failedTables);
            // 串口设备
            count += UpdateMacsIdsInner(_deviceSerialPortService, sqlTemp, parameters, failedTables);
            // IO设备
            count += UpdateMacsIdsInner(_deviceIoService, sqlTemp, parameters, failedTables);
            // 产品任务
            count += UpdateMacsIdsInner(_productMissionService, sqlTemp, parameters, failedTables);
            // 站点
            count += UpdateMacsIdsInner(_workstationService, sqlTemp, parameters, failedTables);
            // Outer database config glb
            count += UpdateMacsIdsInner(_outerDatabaseConfigGlbService, sqlTemp, parameters, failedTables);
            // mat_code_map_whyc
            count += UpdateMacsIdsInner(_matCodeMapWhycService, sqlTemp, parameters, failedTables);

            if (failedTables.Count > 0) {
                SystemUtils.ShowWarningPopUp(@$"
存在未转换成功（或没有需要转换的数据）的表，请检查是否需要手动迁移。

*（在此之前请不要进行任何数据修改的操作，推荐将此弹窗消息截图或拍照留痕，方便后续操作）

以下是需要检查的表：
- {string.Join("\n- ", failedTables)}"
                );
            }

            return new() {
                UpdateRows = count,
            };
        }
        private int UpdateMacsIdsInner<T, E>(AServiceBase<T, E> service,
                                             string sql,
                                             object param,
                                             IList<string> failedTables)
            where T : AEntityBase, new()
            where E : AWrapperBase<T> {
            string tableName = service.TableName;
            sql = string.Format(sql, tableName);
            logger.Info($"更新表 [{tableName}]，SQL: {sql}，参数: {JsonConvert.SerializeObject(param)}");

            int count = 0;
            try {
                count = service.ExecuteSql(sql, param);
                logger.Info($"成功更新表 [{tableName}]，影响 {count} 行");
            } catch (Exception ex) {
                logger.Error($"更新表 [{tableName}] 失败", ex);
            }

            if (count == 0) {
                failedTables.Add(tableName);
            }
            return count;
        }
        #endregion

        #region 产品任务相关
        // 查询所有未被删除的产品任务列表
        public QueryProductMissionsRsp QueryProductMissions(QueryProductMissionsReq req) {
            List<ProductMission> missions;

            string sql = $"select * from {_productMissionService.TableName} where deleted = @deleted";
            Dictionary<string, object> parameters = new();
            parameters.Add("deleted", (int) YesOrNo.NO);

            if (req.Role != null && req.Role != Roles.DEVELOPER) {
                sql += " and macs_id = @macs_id";
                parameters.Add("macs_id", req.MacsId);
                missions = _productMissionService.FindBySql(sql, parameters);
            } else {
                missions = _productMissionService.FindBySql(sql, parameters);
            }

            List<ProductMissionDTO> productMissionDTOs = new();
            CommonUtils.ObjectConverter<ProductMission, ProductMissionDTO>(missions, productMissionDTOs);

            return new() {
                ProductMissionsDTOs = productMissionDTOs
            };
        }
        public QueryProductMissionListRsp QueryProductMissionList(QueryProductMissionListReq req) {
            // 先查询任务清单
            List<ProductMission> missions;

            Roles? role = SystemUtils.GetRoleNameByUserId(SystemUtils.LoggedUserId);
            if (role != null && role != Roles.DEVELOPER) {
                string sql = $"select * from {_productMissionService.TableName} where deleted = @deleted and macs_id = @macs_id";
                Dictionary<string, object> parameters = new();
                parameters.Add("deleted", (int) YesOrNo.NO);
                parameters.Add("macs_id", req.MacsId);
                missions = _productMissionService.FindBySql(sql, parameters);
            } else {
                missions = _productMissionService.QueryListWithoutUserId();
            }

            // 根据任务清单查询对应的封面 side
            if (missions.Count > 0) {
                List<ProductMissionDTO> productMissionDTOs = new();
                CommonUtils.ObjectConverter<ProductMission, ProductMissionDTO>(missions, productMissionDTOs);

                string sidesSql = $"select * from {_productSideService.TableName} t where mission_id in @mission_ids and deleted = @deleted order by id asc";
                Dictionary<string, object> sideParameters = new() {
                    { "@mission_ids", missions.Select(m => m.id).ToList() },
                    { "@deleted", (int) YesOrNo.NO },
                };
                List<ProductSide> sides = _productSideService.FindBySql(sidesSql, sideParameters);

                string boltsSql = $"select * from {_productBoltService.TableName} where side_id in @side_ids and deleted = @deleted";
                Dictionary<string, object> boltParameters = new() {
                    { "@side_ids", sides.Select(s => s.id).ToList() },
                    { "@deleted", (int) YesOrNo.NO },
                };
                List<ProductBolt> bolts = _productBoltService.FindBySql(boltsSql, boltParameters);

                // 将 sides 组装到对应的 mission 中
                foreach (ProductMissionDTO missionDTO in productMissionDTOs.ToList()) {
                    List<ProductSide> sideTemps = sides.FindAll(side => side.mission_id == missionDTO.id);

                    bool noSide = sideTemps.Count == 0;
                    bool isEditing = req.IsEditing != null && req.IsEditing.Value;
                    bool hasNullImageSide = sideTemps.Find(side => side.image == null || string.IsNullOrEmpty(side.image)) != null;
                    bool hasNullBoltSide = false;
                    foreach (ProductSide side in sideTemps) {
                        bool noBolt = bolts.Find(b => b.side_id == side.id) == null;
                        if (noBolt) {
                            hasNullBoltSide = true;
                            break;
                        }
                    }

                    if (noSide || (!isEditing && (hasNullImageSide || hasNullBoltSide))) {
                        productMissionDTOs.Remove(missionDTO);
                    } else {
                        ProductSideDTO productSideDTO = new();
                        CommonUtils.ObjectConverter<ProductSide, ProductSideDTO>(sideTemps[0], productSideDTO);
                        missionDTO.ProductSides = new() { productSideDTO };
                    }

                    //
                    // ProductSide? sideDTO = sides.Find(side => side.mission_id == missionDTO.id);
                    // if (sideDTO != null) {
                    //     ProductSideDTO productSideDTO = new();
                    //     CommonUtils.ObjectConverter<ProductSide, ProductSideDTO>(sideDTO, productSideDTO);
                    //     missionDTO.ProductSides = new() { productSideDTO };
                    // } else {
                    //     productMissionDTOs.Remove(missionDTO);
                    // }
                }

                return new(productMissionDTOs);
            }

            return new(new());
        }
        public QueryProductMissionDetailRsp QueryProductMissionDetail(QueryProductMissionDetailReq req) {
            // 先查询任务
            ProductMission? productMission = _productMissionService.FindById(req.MissionId);
            if (productMission != null) {
                ProductMissionDTO productMissionDTO = new();
                CommonUtils.ObjectConverter<ProductMission, ProductMissionDTO>(productMission, productMissionDTO);

                List<ProductSide> sides = _productSideService.FindBySql($"select * from {_productSideService.TableName} where mission_id = @mission_id", new() { { "@mission_id", req.MissionId } }).ToList();
                sides = sides.Where(s => s.deleted != (int) YesOrNo.YES).ToList();
                List<ProductBolt> bolts = new();
                if (sides.Count > 0) {
                    bolts = _productBoltService.FindBySql($"select * from {_productBoltService.TableName} where side_id in @side_ids", new() { { "@side_ids", sides.Select(s => s.id).ToList() } }).ToList();
                    bolts = bolts.Where(b => b.deleted != (int) YesOrNo.YES).ToList();
                }

                // 将 sides, bolts 组装到 mission 中
                List<ProductSideDTO> productSideDTOs = new();
                CommonUtils.ObjectConverter<ProductSide, ProductSideDTO>(sides.Where(m => m.mission_id == productMissionDTO.id).ToList(), productSideDTOs);
                productMissionDTO.ProductSides = productSideDTOs;
                productSideDTOs.ForEach(sideDTO => {
                    List<ProductBoltDTO> productBoltDTOs = new();
                    CommonUtils.ObjectConverter<ProductBolt, ProductBoltDTO>(bolts.Where(m => m.side_id == sideDTO.id).ToList(), productBoltDTOs);
                    sideDTO.Bolts = productBoltDTOs;
                });

                return new() {
                    ProductMissionDTO = productMissionDTO
                };
            }
            return new();
        }
        // 新增或修改任务
        public AddOrUpdateProductMissionRsp AddOrUpdateProductMission(AddOrUpdateProductMissionReq req) {
            AddOrUpdateProductMissionRsp rsp = new();
            // 使用同一个connection确保当前所有操作都在同一个事务下
            using DbConnection conn = DbConnector.GetConnection();
            // 开启事务
            DbTransaction transaction = conn.BeginTransaction();
            // Set connection and transaction into every service
            _productMissionService.UseConnection(conn, transaction);
            _productSideService.UseConnection(conn, transaction);
            _productBoltService.UseConnection(conn, transaction);
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
                logger.Error("AddProductMission error: " + e);
                rsp.RsponseCode = HttpResponseCode.ERROR;
                rsp.RsponseMessage = e.Message;

                transaction.Rollback();
            } finally {
                _productMissionService.ReleaseConnection();
                _productSideService.ReleaseConnection();
                _productBoltService.ReleaseConnection();
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
            List<Workstation> workstations;

            Roles? role = SystemUtils.GetRoleNameByUserId(SystemUtils.LoggedUserId);
            if (role != null && role != Roles.DEVELOPER) {
                string sql = $"select * from {_workstationService.TableName} where deleted = @deleted and macs_id = @macs_id";
                Dictionary<string, object> parameters = new();
                parameters.Add("deleted", (int) YesOrNo.NO);
                parameters.Add("macs_id", req.MacsId);
                workstations = _workstationService.FindBySql(sql, parameters);
            } else {
                workstations = _workstationService.QueryListWithoutUserId();
            }

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
        // 根据任务记录 ids 查询对应的站点id和站点名称
        public QueryWorkstationInfoByMissionRecordIdsRsp QueryWorkstationInfoByMissionRecordIds(QueryWorkstationInfoByMissionRecordIdsReq req) {
            // 先查询到每条任务记录对应的 workstation_id
            Dictionary<int, Dictionary<int, string>> workstationInfos = new();
            if (req.MissionRecordIds.Count > 0) {
                workstationInfos = _operationDataService.GetWorkstationInfoByMissionRecordIds(req.MissionRecordIds);

                // 根据所有 workstation_ids 查询到每个 id 对应的 name
                List<int> workstationIds = new();
                workstationInfos.Values.ToList().ForEach(dict => workstationIds.AddRange(dict.Keys));
                Dictionary<int, string> workstationInfo = _workstationService.GetWorkstationNamesByIds(workstationIds);
                foreach (var dict in workstationInfos.Values) {
                    foreach (var pair in dict) {
                        if (workstationInfo.ContainsKey(pair.Key)) {
                            dict[pair.Key] = workstationInfo[pair.Key];
                        }
                    }
                }
            }

            return new(workstationInfos);
        }
        #endregion

        #region 任务记录相关
        // 查询任务记录列表
        public QueryMissionRecordListRsp QueryMissionRecordList(QueryMissionRecordListReq req) {
            // 【分页查询】使用新的分页查询方法
            PagedResult<MissionRecord> pagedResult = _missionRecordService.QueryMissionRecordListWithPagination(req);

            // 转换为DTO
            List<MissionRecordDTO> missionRecordDTOs = new();
            CommonUtils.ObjectConverter<MissionRecord, MissionRecordDTO>(pagedResult.Data, missionRecordDTOs);

            // 构建返回结果
            QueryMissionRecordListRsp rsp = new() {
                PagedResult = new PagedResult<MissionRecordDTO> {
                    Data = missionRecordDTOs,
                    TotalCount = pagedResult.TotalCount,
                    PageNumber = pagedResult.PageNumber,
                    PageSize = pagedResult.PageSize
                }
            };

            return rsp;
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
            string sql = $"select 1 from {_missionRecordService.TableName} where mission_id = @mission_id";
            Dictionary<string, object> parameters = new();
            parameters.Add("mission_id", req.MissionId);
            if (req.MissionResult != null) {
                sql += " and mission_result = @mission_result";
                parameters.Add("mission_result", req.MissionResult);
            }

            if (req.ProductBarCode != null) {
                sql += " and product_bar_code = @product_bar_code";
                parameters.Add("product_bar_code", req.ProductBarCode);
            }
            if (req.PartsBarCode != null) {
                sql += " and parts_bar_code like @parts_bar_code";
                parameters.Add("parts_bar_code", $"%{req.PartsBarCode}%");
            }


            List<MissionRecord> missionRecords = _missionRecordService.FindBySql(sql, parameters);
            CheckIfBarCodeExistsInMissionRecordRsp rsp = new() {
                Yes = missionRecords.Count > 0,
            };
            if (rsp.Yes) {
                CommonUtils.ObjectConverter<MissionRecord, MissionRecordDTO>(missionRecords[0], rsp.MissionRecordDTO);
            }
            return rsp;
        }
        // 查询最新一条任务记录，用于获取其产品批次，作回填使用
        public QueryLatestMissionRecordRsp QueryLatestMissionRecord(QueryLatestMissionRecordReq req) {
            string sql = $"select * from {_missionRecordService.TableName} order by id desc";
            List<MissionRecord> missionRecords = _missionRecordService.FindBySql(sql);
            QueryLatestMissionRecordRsp rsp = new();
            if (missionRecords.Count > 0) {
                List<MissionRecordDTO>? missionRecordDTOs = new();
                CommonUtils.ObjectConverter<MissionRecord, MissionRecordDTO>(missionRecords, missionRecordDTOs);
                rsp.MissionRecordDTO = missionRecordDTOs[0];
            }
            return rsp;
        }
        // 根据站点 ids 查询对应的任务记录列表
        public QueryMissionRecordsByWorkstationIdsRsp QueryMissionRecordsByWorkstationIds(QueryMissionRecordsByWorkstationIdsReq req) {
            Dictionary<int, List<int>> result = new();
            if (req.WorkstationIds.Count > 0) {
                result = _operationDataService.GetMissionRecordIdsByWorkstationIds(req.WorkstationIds);
            }
            return new(result);
        }
        #endregion

        #region 拧紧数据相关
        public QueryOperationDataListRsp QueryOperationDataList(QueryOperationDataListReq req) {
            // 【分页查询】使用新的分页查询方法
            PagedResult<OperationData> pagedResult = _operationDataService.QueryOperationDataListWithPagination(req);

            // 转换为DTO
            List<OperationDataDTO> operationDataDTOs = new();
            CommonUtils.ObjectConverter<OperationData, OperationDataDTO>(pagedResult.Data, operationDataDTOs);

            // 构建返回结果
            var rsp = new QueryOperationDataListRsp {
                PagedResult = new PagedResult<OperationDataDTO> {
                    Data = operationDataDTOs,
                    TotalCount = pagedResult.TotalCount,
                    PageNumber = pagedResult.PageNumber,
                    PageSize = pagedResult.PageSize
                }
            };

            return rsp;
        }
        public AddOrUpdateOperationDataRsp AddOrUpdateOperationData(AddOrUpdateOperationDataReq req) {
            OperationData operationData = new();
            CommonUtils.ObjectConverter<OperationDataDTO, OperationData>(req.OperationDataDTO, operationData);

            operationData = _operationDataService.InsertOrUpdate(operationData);
            OperationDataDTO curveDataDTO = new();
            CommonUtils.ObjectConverter<OperationData, OperationDataDTO>(operationData, curveDataDTO);

            return new AddOrUpdateOperationDataRsp(curveDataDTO);
        }
        public BatchAddOperationDataRsp BatchAddOperationData(BatchAddOperationDataReq req) {
            List<OperationDataDTO> operationDataDTOs = req.OperationDataDTOs;
            List<OperationData> operationDatas = new();
            CommonUtils.ObjectConverter<OperationDataDTO, OperationData>(operationDataDTOs, operationDatas);
            int num = _operationDataService.AddBatch(operationDatas);

            return new() {
                Num = num,
            };
        }
        public FindOperationDataByIdRsp FindOperationDataById(FindOperationDataByIdReq req) {
            OperationData? operationData = _operationDataService.FindById(req.Id);
            OperationDataDTO? operationDataDTO = null;

            if (operationData != null) {
                operationDataDTO = new();
                CommonUtils.ObjectConverter<OperationData, OperationDataDTO>(operationData, operationDataDTO);
            }

            return new FindOperationDataByIdRsp(operationDataDTO);
        }
        #endregion

        #region 曲线数据相关
        public AddOrUpdateCurveDataRsp AddOrUpdateCurveData(AddOrUpdateCurveDataReq req) {
            CurveData curveData = new();
            CommonUtils.ObjectConverter<CurveDataDTO, CurveData>(req.CurveDataDTO, curveData);

            curveData = _curveDataService.InsertOrUpdate(curveData);
            CurveDataDTO curveDataDTO = new();
            CommonUtils.ObjectConverter<CurveData, CurveDataDTO>(curveData, curveDataDTO);

            return new AddOrUpdateCurveDataRsp(curveDataDTO);
        }
        public FindCurveDataByOperationDataIdRsp FindCurveDataByOperationDataId(FindCurveDataByOperationDataIdReq req) {
            List<CurveData> curveDatas = _curveDataService.QueryDataByOperationDataId(req.OperationDataId);
            List<CurveDataDTO> curveDataDTOs = new();
            CommonUtils.ObjectConverter<CurveData, CurveDataDTO>(curveDatas, curveDataDTOs);

            return new FindCurveDataByOperationDataIdRsp(curveDataDTOs);
        }
        #endregion

        #region 力臂相关
        // 查询力臂列表
        public QueryDeviceArmListRsp QueryDeviceArmList(QueryDeviceArmListReq req) {
            List<DeviceArm> deviceCategories;

            Roles? role = SystemUtils.GetRoleNameByUserId(SystemUtils.LoggedUserId);
            string sql = $"select * from {_deviceArmService.TableName} where 1 = 1";
            Dictionary<string, object> parameters = new();
            // 如果是为了启动线程而查询则不需要考虑权限，一定要检查mac地址，并且被删掉的数据也要查询
            if (req.ForTask) {
                sql += " and macs_id = @macs_id";
                parameters.Add("macs_id", req.MacsId);
            }
            // 不是为了线程查询，则需要考虑权限，并且不查询已被删除的数据
            else {
                sql += " and deleted = @deleted";
                parameters.Add("deleted", (int) YesOrNo.NO);
                // 不是管理员就需要检查mac地址
                if (role != null && role != Roles.DEVELOPER) {
                    sql += " and macs_id = @macs_id";
                    parameters.Add("macs_id", req.MacsId);
                }
            }
            deviceCategories = _deviceArmService.FindBySql(sql, parameters);

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

            Roles? role = SystemUtils.GetRoleNameByUserId(SystemUtils.LoggedUserId);
            string sql = $"select * from {_deviceToolService.TableName} where 1 = 1";
            Dictionary<string, object> parameters = new();
            // 如果是为了启动线程而查询则不需要考虑权限，一定要检查mac地址，并且被删掉的数据也要查询
            if (req.ForTask) {
                sql += " and macs_id = @macs_id";
                parameters.Add("macs_id", req.MacsId);
            }
            // 不是为了线程查询，则需要考虑权限，并且不查询已被删除的数据
            else {
                sql += " and deleted = @deleted";
                parameters.Add("deleted", (int) YesOrNo.NO);
                // 不是管理员就需要检查mac地址
                if (role != null && role != Roles.DEVELOPER) {
                    sql += " and macs_id = @macs_id";
                    parameters.Add("macs_id", req.MacsId);
                }
            }
            deviceCategories = _deviceToolService.FindBySql(sql, parameters);

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

            Roles? role = SystemUtils.GetRoleNameByUserId(SystemUtils.LoggedUserId);
            string sql = $"select * from {_deviceSerialPortService.TableName} where 1 = 1";
            Dictionary<string, object> parameters = new();
            // 如果是为了启动线程而查询则不需要考虑权限，一定要检查mac地址，并且被删掉的数据也要查询
            if (req.ForTask) {
                sql += " and macs_id = @macs_id";
                parameters.Add("macs_id", req.MacsId);
            }
            // 不是为了线程查询，则需要考虑权限，并且不查询已被删除的数据
            else {
                sql += " and deleted = @deleted";
                parameters.Add("deleted", (int) YesOrNo.NO);
                // 不是管理员就需要检查mac地址
                if (role != null && role != Roles.DEVELOPER) {
                    sql += " and macs_id = @macs_id";
                    parameters.Add("macs_id", req.MacsId);
                }
            }
            deviceCategories = _deviceSerialPortService.FindBySql(sql, parameters);

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

            Roles? role = SystemUtils.GetRoleNameByUserId(SystemUtils.LoggedUserId);
            string sql = $"select * from {_deviceCommunicationService.TableName} where 1 = 1";
            Dictionary<string, object> parameters = new();
            // 如果是为了启动线程而查询则不需要考虑权限，一定要检查mac地址，并且被删掉的数据也要查询
            if (req.ForTask) {
                sql += " and macs_id = @macs_id";
                parameters.Add("macs_id", req.MacsId);
            }
            // 不是为了线程查询，则需要考虑权限，并且不查询已被删除的数据
            else {
                sql += " and deleted = @deleted";
                parameters.Add("deleted", (int) YesOrNo.NO);
                // 不是管理员就需要检查mac地址
                if (role != null && role != Roles.DEVELOPER) {
                    sql += " and macs_id = @macs_id";
                    parameters.Add("macs_id", req.MacsId);
                }
            }
            deviceCategories = _deviceCommunicationService.FindBySql(sql, parameters);

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

        #region IO设备相关
        // 查询IO设备列表
        public QueryDeviceIoListRsp QueryDeviceIoList(QueryDeviceIoListReq req) {
            List<DeviceIo> deviceCategories;

            Roles? role = SystemUtils.GetRoleNameByUserId(SystemUtils.LoggedUserId);
            string sql = $"select * from {_deviceIoService.TableName} where 1 = 1";
            Dictionary<string, object> parameters = new();
            // 如果是为了启动线程而查询则不需要考虑权限，一定要检查mac地址，并且被删掉的数据也要查询
            if (req.ForTask) {
                sql += " and macs_id = @macs_id";
                parameters.Add("macs_id", req.MacsId);
            }
            // 不是为了线程查询，则需要考虑权限，并且不查询已被删除的数据
            else {
                sql += " and deleted = @deleted";
                parameters.Add("deleted", (int) YesOrNo.NO);
                // 不是管理员就需要检查mac地址
                if (role != null && role != Roles.DEVELOPER) {
                    sql += " and macs_id = @macs_id";
                    parameters.Add("macs_id", req.MacsId);
                }
            }
            deviceCategories = _deviceIoService.FindBySql(sql, parameters);

            List<DeviceIoDTO> deviceIoDTOs = new();
            CommonUtils.ObjectConverter<DeviceIo, DeviceIoDTO>(deviceCategories, deviceIoDTOs);

            return new() {
                DeviceIoDTOs = deviceIoDTOs,
            };
        }
        // 新增或修改IO设备
        public AddOrUpdateDeviceIoRsp AddOrUpdateDeviceIo(AddOrUpdateDeviceIoReq req) {
            DeviceIoDTO deviceIoDTO = req.DeviceIoDTO;
            DeviceIo deviceIo = new();
            CommonUtils.ObjectConverter<DeviceIoDTO, DeviceIo>(deviceIoDTO, deviceIo);
            DeviceIo? deviceIoNew = _deviceIoService.InsertOrUpdate(deviceIo);
            if (deviceIoNew != null) {
                deviceIoDTO.id = deviceIoNew.id;
            }

            return new() {
                DeviceIoDTO = deviceIoDTO,
            };
        }
        // 删除IO设备
        public DeleteDeviceIoByIdsRsp DeleteDeviceIo(DeleteDeviceIoByIdsReq req) {
            int deletedRows = _deviceIoService.DeleteByIds(req.Ids);

            DeleteDeviceIoByIdsRsp rsp = new();
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
            List<BarCodeMatchingRule> barCodeMatchingRule;

            Roles? role = SystemUtils.GetRoleNameByUserId(SystemUtils.LoggedUserId);
            string sql = $"select * from {_barCodeMatchingRuleService.TableName} where deleted = @deleted";
            Dictionary<string, object> parameters = new();
            parameters.Add("deleted", (int) YesOrNo.NO);
            if (role != null && role != Roles.DEVELOPER) {
                sql += " and macs_id = @macs_id";
                parameters.Add("macs_id", req.MacsId);
            }
            if (req.MissionId != null) {
                sql += $" and mission_id = @mission_id";
                parameters.Add("mission_id", req.MissionId);
            }
            barCodeMatchingRule = _barCodeMatchingRuleService.FindBySql(sql, parameters);

            List<BarCodeMatchingRuleDTO> barCodeMatchingRuleDTOs = new();
            CommonUtils.ObjectConverter<BarCodeMatchingRule, BarCodeMatchingRuleDTO>(barCodeMatchingRule, barCodeMatchingRuleDTOs);

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

        #region Outer database config related
        // Query list
        public QueryOuterDatabaseConfigGlbListRsp QueryOuterDatabaseConfigGlbList(QueryOuterDatabaseConfigGlbListReq req) {
            List<OuterDatabaseConfigGlb> deviceCategories;

            Roles? role = SystemUtils.GetRoleNameByUserId(SystemUtils.LoggedUserId);
            string sql = $"select * from {_outerDatabaseConfigGlbService.TableName} where 1 = 1";
            Dictionary<string, object> parameters = new();
            sql += " and deleted = @deleted";
            parameters.Add("deleted", (int) YesOrNo.NO);
            // Should check macs_id if user is not Developer
            if (role != null && role != Roles.DEVELOPER) {
                sql += " and macs_id = @macs_id";
                parameters.Add("macs_id", req.MacsId);
            }
            deviceCategories = _outerDatabaseConfigGlbService.FindBySql(sql, parameters);

            List<OuterDatabaseConfigGlbDTO> outerDatabaseConfigGlbDTOs = new();
            CommonUtils.ObjectConverter<OuterDatabaseConfigGlb, OuterDatabaseConfigGlbDTO>(deviceCategories, outerDatabaseConfigGlbDTOs);

            return new() {
                OuterDatabaseConfigGlbDTOs = outerDatabaseConfigGlbDTOs,
            };
        }
        // Add or update
        public AddOrUpdateOuterDatabaseConfigGlbRsp AddOrUpdateOuterDatabaseConfigGlb(AddOrUpdateOuterDatabaseConfigGlbReq req) {
            OuterDatabaseConfigGlbDTO outerDatabaseConfigGlbDTO = req.OuterDatabaseConfigGlbDTO;
            OuterDatabaseConfigGlb outerDatabaseConfigGlb = new();
            CommonUtils.ObjectConverter<OuterDatabaseConfigGlbDTO, OuterDatabaseConfigGlb>(outerDatabaseConfigGlbDTO, outerDatabaseConfigGlb);
            OuterDatabaseConfigGlb? outerDatabaseConfigGlbNew = _outerDatabaseConfigGlbService.InsertOrUpdate(outerDatabaseConfigGlb);
            if (outerDatabaseConfigGlbNew != null) {
                outerDatabaseConfigGlbDTO.id = outerDatabaseConfigGlbNew.id;
            }

            return new() {
                OuterDatabaseConfigGlbDTO = outerDatabaseConfigGlbDTO,
            };
        }
        // Delete by ids
        public DeleteOuterDatabaseConfigGlbByIdsRsp DeleteOuterDatabaseConfigGlb(DeleteOuterDatabaseConfigGlbByIdsReq req) {
            int deletedRows = _outerDatabaseConfigGlbService.DeleteByIds(req.Ids);

            DeleteOuterDatabaseConfigGlbByIdsRsp rsp = new();
            if (deletedRows < req.Ids.Count) {
                rsp.RsponseCode = HttpResponseCode.ERROR;
                rsp.RsponseMessage = $"删除失败！应该删除{req.Ids.Count}条数据，实际只删除了{deletedRows}条数据，请检查！";
            }
            return rsp;
        }
        // Find one for checking
        public FindOuterDatabaseConfigGlbForCheckingRsp FindOuterDatabaseConfigGlbForChecking(FindOuterDatabaseConfigGlbForCheckingReq req) {
            string sql = $"select * from {_outerDatabaseConfigGlbService.TableName} where 1 = 1";
            Dictionary<string, object> parameters = new();
            sql += " and deleted = @deleted and id <> @id";
            parameters.Add("deleted", (int) YesOrNo.NO);
            parameters.Add("id", req.Id);

            sql += " and ((host = @host and port = @port and database_name = @database_name and database_type = @database_type) or workstation_name = @workstation_name)";
            parameters.Add("host", req.host);
            parameters.Add("port", req.port);
            parameters.Add("database_name", req.database_name);
            parameters.Add("database_type", req.database_type);
            parameters.Add("workstation_name", req.workstation_name);

            List<OuterDatabaseConfigGlb> outerDatabaseConfigGlbs = _outerDatabaseConfigGlbService.FindBySql(sql, parameters);
            List<OuterDatabaseConfigGlbDTO> dtos = new();
            CommonUtils.ObjectConverter<OuterDatabaseConfigGlb, OuterDatabaseConfigGlbDTO>(outerDatabaseConfigGlbs, dtos);

            FindOuterDatabaseConfigGlbForCheckingRsp rsp = new();
            rsp.outerDTOs.AddRange(dtos);
            return rsp;
        }

        // Outer database actions
        public AddDataToOuterDatabaseGlbRsp AddDataToOuterDatabaseGlb(AddDataToOuterDatabaseGlbReq req) {
            try {
                OuterDatabaseConfigGlbDTO configDto = req.OuterDatabaseConfigGlbDTO;
                MissionRecordDTO missionRecord = req.MissionRecordDTO;

                DbConnection? conn = DbConnector.GetOuterConnection(configDto);
                if (conn != null) {
                    string tableName = $"tightening_data_glb_{configDto.workstation_name}";
                    if (!ConnectionUtils.CheckTableExists(conn, tableName)) {
                        logger.Info($"Outer database[{tableName}] does not exsit, trying to create one...");

                        string fieldSql = "";
                        for (int i = 0; i < 50; i++) {
                            fieldSql += $"torque{i + 1} float NULL, angle{i + 1} float NULL, no{i + 1} int NULL, coords{i + 1} nvarchar(255) NULL, ";
                        }
                        string sqlCreate = @$"CREATE TABLE [{tableName}] (
                            [id] int IDENTITY(1,1) NOT NULL, 
                            Serial_Number nvarchar(255) NULL,
                            SoftwareVersion nvarchar(255) NULL,
                            Test_Date nvarchar(255) NULL,
                            Test_Time nvarchar(255) NULL,
                            Operator_Number nvarchar(255) NULL,
                            Model nvarchar(255) NULL,
                            Collect int DEFAULT 0 NULL,
                            Test_Result int NULL,
                            {fieldSql}
                            PRIMARY KEY CLUSTERED ([id])
                            WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
                        );";
                        logger.Info($"Sql = [{sqlCreate}]");

                        int result = _dapperDBService.ExecuteSql(conn, sqlCreate);
                        logger.Info($"Result of creation = [{result}]");
                    }

                    string sql = $"insert into {tableName}";
                    Dictionary<string, object?> parameters = new();
                    parameters.Add("Serial_Number", missionRecord.product_bar_code);
                    parameters.Add("Test_Date", missionRecord.create_time.ToString("yyyy-MM-dd"));
                    parameters.Add("Test_Time", missionRecord.create_time.ToString("HH:mm:ss"));
                    parameters.Add("Test_Result", missionRecord.mission_result);

                    List<OperationDataDTO> operationDataDTOs = req.OperationDataDTOs;
                    string dataSql = "";
                    string paramNamesSql = "";
                    for (int i = 0; i < operationDataDTOs.Count; i++) {
                        OperationDataDTO data = operationDataDTOs[i];

                        // Torque
                        dataSql += $", torque{i + 1}";
                        paramNamesSql += $", @torque{i + 1}";
                        parameters.Add($"torque{i + 1}", data.torque);

                        // Angle
                        dataSql += $", angle{i + 1}";
                        paramNamesSql += $", @angle{i + 1}";
                        parameters.Add($"angle{i + 1}", data.angle);

                        // No
                        dataSql += $", no{i + 1}";
                        paramNamesSql += $", @no{i + 1}";
                        parameters.Add($"no{i + 1}", data.bolt_serial_num);

                        // Coords
                        dataSql += $", coords{i + 1}";
                        paramNamesSql += $", @coords{i + 1}";
                        parameters.Add($"coords{i + 1}", data.arm_position);
                    }

                    sql += $"(Serial_Number, Test_Date, Test_Time, Test_Result{dataSql}) values (@Serial_Number, @Test_Date, @Test_Time, @Test_Result{paramNamesSql})";

                    return new(_dapperDBService.ExecuteSql(conn, sql, parameters));
                }
            } catch (Exception e) {
                logger.Error($"Error while inserting data into outer database, e = [{e}]");
            }
            return new(0);
        }
        #endregion

        #region Screw bit counter related
        // Find by mission id
        public FindScrewBitCounterByMissionIdRsp FindScrewBitCounterByMissionId(FindScrewBitCounterByMissionIdReq req) {
            string sql = $"select * from {_screwBitCounterService.TableName} where {_screwBitCounterService.ConditionWithoutUserId} and mission_id = @mission_id";
            Dictionary<string, object> parameters = new();
            parameters.Add("mission_id", req.MissionId);

            List<ScrewBitCounter> screwBitCounters = _screwBitCounterService.FindBySql(sql, parameters);
            List<ScrewBitCounterDTO> screwBitCounterDTOs = new();
            CommonUtils.ObjectConverter<ScrewBitCounter, ScrewBitCounterDTO>(screwBitCounters, screwBitCounterDTOs);

            return new() {
                ScrewBitCounterDTOs = screwBitCounterDTOs,
            };
        }
        // Add or update
        public AddOrUpdateScrewBitCounterRsp AddOrUpdateScrewBitCounter(AddOrUpdateScrewBitCounterReq req) {
            ScrewBitCounter screwBitCounter = new();
            CommonUtils.ObjectConverter<ScrewBitCounterDTO, ScrewBitCounter>(req.ScrewBitCounterDTO, screwBitCounter);

            screwBitCounter = _screwBitCounterService.InsertOrUpdate(screwBitCounter);
            ScrewBitCounterDTO screwBitCounterDTO = new();
            CommonUtils.ObjectConverter<ScrewBitCounter, ScrewBitCounterDTO>(screwBitCounter, screwBitCounterDTO);

            return new AddOrUpdateScrewBitCounterRsp(screwBitCounterDTO);
        }
        // Delete
        public DeleteScrewBitCounterRsp DeleteScrewBitCounter(DeleteScrewBitCounterReq req) {
            DeleteScrewBitCounterRsp rsp = new();
            ScrewBitCounter screwBitCounter = new();
            CommonUtils.ObjectConverter<ScrewBitCounterDTO, ScrewBitCounter>(req.ScrewBitCounterDTO, screwBitCounter);
            if (!_screwBitCounterService.DeleteEntity(screwBitCounter)) {
                rsp.RsponseCode = HttpResponseCode.ERROR;
                rsp.RsponseMessage = "Delete failed, don't know what happened.";
            }
            return rsp;
        }
        #endregion

        #region MatCodeMapWhyc related
        // Query List
        public QueryMatCodeMapWhycListRsp QueryMatCodeMapWhycList(QueryMatCodeMapWhycListReq req) {
            List<MatCodeMapWhyc> deviceCategories;

            Roles? role = SystemUtils.GetRoleNameByUserId(SystemUtils.LoggedUserId);
            string sql = $"select * from {_matCodeMapWhycService.TableName} where 1 = 1";
            Dictionary<string, object> parameters = new();
            sql += " and deleted = @deleted";
            parameters.Add("deleted", (int) YesOrNo.NO);
            // Need to check macs_id if role is not Admin
            if (role != null && role != Roles.DEVELOPER) {
                sql += " and macs_id = @macs_id";
                parameters.Add("macs_id", req.MacsId);
            }
            deviceCategories = _matCodeMapWhycService.FindBySql(sql, parameters);

            List<MatCodeMapWhycDTO> deviceIoDTOs = new();
            CommonUtils.ObjectConverter<MatCodeMapWhyc, MatCodeMapWhycDTO>(deviceCategories, deviceIoDTOs);

            return new() {
                MatCodeMapWhycDTOs = deviceIoDTOs,
            };
        }
        // Add or update
        public AddOrUpdateMatCodeMapWhycRsp AddOrUpdateMatCodeMapWhyc(AddOrUpdateMatCodeMapWhycReq req) {
            MatCodeMapWhycDTO deviceIoDTO = req.MatCodeMapWhycDTO;
            MatCodeMapWhyc deviceIo = new();
            CommonUtils.ObjectConverter<MatCodeMapWhycDTO, MatCodeMapWhyc>(deviceIoDTO, deviceIo);
            MatCodeMapWhyc? deviceIoNew = _matCodeMapWhycService.InsertOrUpdate(deviceIo);
            if (deviceIoNew != null) {
                deviceIoDTO.id = deviceIoNew.id;
            }

            return new() {
                MatCodeMapWhycDTO = deviceIoDTO,
            };
        }
        // Delete by ids
        public DeleteMatCodeMapWhycByIdsRsp DeleteMatCodeMapWhyc(DeleteMatCodeMapWhycByIdsReq req) {
            int deletedRows = _matCodeMapWhycService.DeleteByIds(req.Ids);

            DeleteMatCodeMapWhycByIdsRsp rsp = new();
            if (deletedRows < req.Ids.Count) {
                rsp.RsponseCode = HttpResponseCode.ERROR;
                rsp.RsponseMessage = $"删除失败！应该删除{req.Ids.Count}条数据，实际只删除了{deletedRows}条数据，请检查！";
            }
            return rsp;
        }
        // Find by mat code
        public FindMatCodeMapByMatCodeRsp FindMatCodeMapByMatCode(FindMatCodeMapByMatCodeReq req) {
            FindMatCodeMapByMatCodeRsp rsp = new();

            string sql = $"select * from {_matCodeMapWhycService.TableName} where {_matCodeMapWhycService.ConditionWithoutUserId} and mat_code = @mat_code";
            Dictionary<string, object> parameters = new();
            parameters.Add("mat_code", req.MatCode);

            List<MatCodeMapWhyc> matCodeMaps = _matCodeMapWhycService.FindBySql(sql, parameters);
            List<MatCodeMapWhycDTO> matCodeMapDTOs = new();
            CommonUtils.ObjectConverter<MatCodeMapWhyc, MatCodeMapWhycDTO>(matCodeMaps, matCodeMapDTOs);

            if (matCodeMapDTOs.Count > 0) {
                rsp.MatCodeMapWhycDTO = matCodeMapDTOs[0];
            }

            return rsp;
        }
        #endregion
    }
}
