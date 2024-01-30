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
        private DeviceService _deviceService;
        [Autowired]
        private BrandService _brandService;
        [Autowired]
        private DeviceCategoryService _deviceCategoryService;
        [Autowired]
        private DeviceArmService _deviceArmService;
        [Autowired]
        private DeviceToolService _deviceToolService;
        [Autowired]
        private DeviceSerialPortService _deviceSerialPortService;
        [Autowired]
        private DeviceCommunicationService _deviceCommunicationService;
        [Autowired]
        private DeviceModelService _deviceModelService;
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

                            // 循环每个螺栓点位查询工具信息
                            for (int k = 0 ; k < bolts.Count ; k++) {
                                int? toolId = bolts[k].tool_id;
                                if (toolId != null) {
                                    Device? device = _deviceService.FindById(toolId.Value);
                                    if (device != null) {
                                        productBoltDTOs[i].tool_name = device.name;
                                        productBoltDTOs[i].tool_description = device.description;
                                        productBoltDTOs[i].tool_ip = device.ip;
                                        productBoltDTOs[i].tool_port = device.port;

                                        DeviceModel? type = _deviceModelService.FindById(device.model_id);
                                        if (type != null) {
                                            productBoltDTOs[i].tool_type_name = type.name;
                                            DeviceCategory? category = _deviceCategoryService.FindById(type.category_id);
                                            if (category != null) {
                                                productBoltDTOs[i].tool_category_name = category.name;
                                                productBoltDTOs[i].tool_category_icon_normal = category.icon_normal;
                                                productBoltDTOs[i].tool_category_icon_error = category.icon_error;
                                            }
                                            Brand? brand = _brandService.FindById(type.brand_id);
                                            if (brand != null) {
                                                productBoltDTOs[i].tool_brand_name = brand.name;
                                            }
                                        }
                                    }
                                }
                            }
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

        #region 设备相关
        // 查询设备列表
        public QueryDeviceListRsp QueryDeviceList(QueryDeviceListReq req) {
            // 先查询Device表
            List<Device> devices = _deviceService.QueryList(req.UserId);
            List<DeviceDTO> deviceDTOs = new();
            CommonUtils.ObjectConverter<Device, DeviceDTO>(devices, deviceDTOs);

            // 遍历Device清单，查询DeviceType、DeviceCategory、Brand
            for (int i = 0 ; i < deviceDTOs.Count ; i++) {
                Device device = devices[i];
                DeviceDTO deviceDTO = deviceDTOs[i];
                // Device type
                DeviceModel? type = _deviceModelService.FindById(device.model_id);
                if (type != null) {
                    deviceDTO.model_id = device.model_id;
                    deviceDTO.model_name = type.name;

                    // Device category
                    DeviceCategory? category = _deviceCategoryService.FindById(type.category_id);
                    if (category != null) {
                        deviceDTO.category_id = category.id;
                        deviceDTO.category_name = category.name;
                        deviceDTO.can_manipulate = category.can_manipulate;
                        deviceDTO.icon_normal = category.icon_normal;
                        deviceDTO.icon_error = category.icon_error;
                    }
                    // Device brand
                    Brand? brand = _brandService.FindById(type.brand_id);
                    if (brand != null) {
                        deviceDTO.brand_id = brand.id;
                        deviceDTO.brand_name = brand.name;
                    }
                }
            }

            return new() {
                DeviceDTOs = deviceDTOs
            };
        }
        // 新增或修改设备
        public AddOrUpdateDeviceRsp AddOrUpdateDevice(AddOrUpdateDeviceReq req) {
            DeviceDTO deviceDTO = req.DeviceDTO;
            Device device = new();
            CommonUtils.ObjectConverter<DeviceDTO, Device>(deviceDTO, device);
            Device? deviceNew = _deviceService.InsertOrUpdate(device);
            if (deviceNew != null) {
                deviceDTO.id = deviceNew.id;
            }

            return new() {
                DeviceDTO = deviceDTO,
            };
        }
        // 删除设备
        public DeleteDeviceByIdsRsp DeleteDevice(DeleteDeviceByIdsReq req) {
            int deletedRows = _deviceService.DeleteByIds(req.Ids);

            DeleteDeviceByIdsRsp rsp = new();
            if (deletedRows < req.Ids.Count) {
                rsp.RsponseCode = HttpResponseCode.ERROR;
                rsp.RsponseMessage = $"删除失败！应该删除{req.Ids.Count}条数据，实际只删除了{deletedRows}条数据，请检查！";
            }
            return rsp;
        }
        #endregion

        #region 品牌相关
        // 查询品牌列表
        public QueryBrandListRsp QueryBrandList(QueryBrandListReq req) {
            List<Brand> brands = _brandService.QueryList(req.UserId);
            List<BrandDTO> brandDTOs = new();
            CommonUtils.ObjectConverter<Brand, BrandDTO>(brands, brandDTOs);

            return new() {
                BrandDTOs = brandDTOs,
            };
        }
        // 新增品牌
        public AddOrUpdateBrandRsp AddOrUpdateBrand(AddOrUpdateBrandReq req) {
            BrandDTO brandDTO = req.BrandDTO;
            Brand brand = new();
            CommonUtils.ObjectConverter<BrandDTO, Brand>(brandDTO, brand);
            Brand? brandNew = _brandService.InsertOrUpdate(brand);
            if (brandNew != null) {
                brandDTO.id = brandNew.id;
            }

            return new() {
                BrandDTO = brandDTO,
            };
        }
        // 删除品牌
        public DeleteBrandByIdsRsp DeleteBrand(DeleteBrandByIdsReq req) {
            int deletedRows = _brandService.DeleteByIds(req.Ids);

            DeleteBrandByIdsRsp rsp = new();
            if (deletedRows < req.Ids.Count) {
                rsp.RsponseCode = HttpResponseCode.ERROR;
                rsp.RsponseMessage = $"删除失败！应该删除{req.Ids.Count}条数据，实际只删除了{deletedRows}条数据，请检查！";
            }
            return rsp;
        }
        #endregion

        #region 设备类型相关
        // 查询设备类型列表
        public QueryDeviceCategoryListRsp QueryDeviceCategoryList(QueryDeviceCategoryListReq req) {
            List<DeviceCategory> deviceCategories = _deviceCategoryService.QueryList(req.UserId);
            List<DeviceCategoryDTO> deviceCategoryDTOs = new();
            CommonUtils.ObjectConverter<DeviceCategory, DeviceCategoryDTO>(deviceCategories, deviceCategoryDTOs);

            return new() {
                DeviceCategoryDTOs = deviceCategoryDTOs,
            };
        }
        // 新增或修改设备类型
        public AddOrUpdateDeviceCategoryRsp AddOrUpdateDeviceCategory(AddOrUpdateDeviceCategoryReq req) {
            DeviceCategoryDTO deviceCategoryDTO = req.DeviceCategoryDTO;
            DeviceCategory deviceCategory = new();
            CommonUtils.ObjectConverter<DeviceCategoryDTO, DeviceCategory>(deviceCategoryDTO, deviceCategory);
            DeviceCategory? deviceCategoryNew = _deviceCategoryService.InsertOrUpdate(deviceCategory);
            if (deviceCategoryNew != null) {
                deviceCategoryDTO.id = deviceCategoryNew.id;
            }

            return new() {
                DeviceCategoryDTO = deviceCategoryDTO,
            };
        }
        // 删除设备类型
        public DeleteDeviceCategoryByIdsRsp DeleteDeviceCategory(DeleteDeviceCategoryByIdsReq req) {
            int deletedRows = _deviceCategoryService.DeleteByIds(req.Ids);

            DeleteDeviceCategoryByIdsRsp rsp = new();
            if (deletedRows < req.Ids.Count) {
                rsp.RsponseCode = HttpResponseCode.ERROR;
                rsp.RsponseMessage = $"删除失败！应该删除{req.Ids.Count}条数据，实际只删除了{deletedRows}条数据，请检查！";
            }
            return rsp;
        }
        #endregion

        #region 设备型号相关
        // 查询设备型号列表
        public QueryDeviceModelListRsp QueryDeviceModelList(QueryDeviceModelListReq req) {
            List<DeviceModel> deviceModels = _deviceModelService.QueryList(req.UserId);
            List<DeviceModelDTO> deviceModelDTOs = new();
            CommonUtils.ObjectConverter<DeviceModel, DeviceModelDTO>(deviceModels, deviceModelDTOs);
            foreach (DeviceModelDTO dto in deviceModelDTOs) {
                if (dto.category_id != null) {
                    DeviceCategory? deviceCategory = _deviceCategoryService.FindById(dto.category_id.Value);
                    if (deviceCategory != null) {
                        dto.category_name = deviceCategory.name;
                    }
                }
                if (dto.brand_id != null) {
                    Brand? brand = _brandService.FindById(dto.brand_id.Value);
                    if (brand != null) {
                        dto.brand_name = brand.name;
                    }
                }
            }
            
            return new() {
                DeviceModelDTOs = deviceModelDTOs,
            };
        }
        // 新增或修改设备型号
        public AddOrUpdateDeviceModelRsp AddOrUpdateDeviceModel(AddOrUpdateDeviceModelReq req) {
            DeviceModelDTO deviceModelDTO = req.DeviceModelDTO;
            DeviceModel deviceModel = new();
            CommonUtils.ObjectConverter<DeviceModelDTO, DeviceModel>(deviceModelDTO, deviceModel);
            DeviceModel? deviceModelNew = _deviceModelService.InsertOrUpdate(deviceModel);
            if (deviceModelNew != null) {
                deviceModelDTO.id = deviceModelNew.id;
            }

            return new() {
                DeviceModelDTO = deviceModelDTO,
            };
        }
        // 删除设备型号
        public DeleteDeviceModelByIdsRsp DeleteDeviceModel(DeleteDeviceModelByIdsReq req) {
            int deletedRows = _deviceModelService.DeleteByIds(req.Ids);

            DeleteDeviceModelByIdsRsp rsp = new();
            if (deletedRows < req.Ids.Count) {
                rsp.RsponseCode = HttpResponseCode.ERROR;
                rsp.RsponseMessage = $"删除失败！应该删除{req.Ids.Count}条数据，实际只删除了{deletedRows}条数据，请检查！";
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
                    Device? device = _deviceService.FindById(dto.tool_id.Value);
                    if (device != null) {
                        dto.tool_name = device.name;
                        dto.tool_description = device.description;
                        dto.tool_ip = device.ip;
                        dto.tool_port = device.port;
                        dto.tool_device_model_id = device.model_id;
                        DeviceModel? deviceModel = _deviceModelService.FindById(device.model_id);
                        if (deviceModel != null) {
                            dto.tool_device_model_name = deviceModel.name;
                            dto.tool_device_category_id = deviceModel.category_id;
                            DeviceCategory? deviceCategory = _deviceCategoryService.FindById(deviceModel.category_id);
                            if (deviceCategory != null) {
                                dto.tool_device_category_name = deviceCategory.name;
                                dto.tool_can_manipulate = deviceCategory.can_manipulate;
                            }
                            dto.tool_brand_id = deviceModel.brand_id;
                            Brand? brand = _brandService.FindById(deviceModel.brand_id);
                            if (brand != null) {
                                dto.tool_brand_name = brand.name;
                            }
                        }
                    }
                }
                if (dto.arm_id != null) {
                    Device? device = _deviceService.FindById(dto.arm_id.Value);
                    if (device != null) {
                        dto.arm_name = device.name;
                        dto.arm_description = device.description;
                        dto.arm_ip = device.ip;
                        dto.arm_port = device.port;
                        dto.arm_device_model_id = device.model_id;
                        DeviceModel? deviceModel = _deviceModelService.FindById(device.model_id);
                        if (deviceModel != null) {
                            dto.arm_device_model_name = deviceModel.name;
                            dto.arm_device_category_id = deviceModel.category_id;
                            DeviceCategory? deviceCategory = _deviceCategoryService.FindById(deviceModel.category_id);
                            if (deviceCategory != null) {
                                dto.arm_device_category_name = deviceCategory.name;
                                dto.arm_can_manipulate = deviceCategory.can_manipulate;
                            }
                            dto.arm_brand_id = deviceModel.brand_id;
                            Brand? brand = _brandService.FindById(deviceModel.brand_id);
                            if (brand != null) {
                                dto.arm_brand_name = brand.name;
                            }
                        }
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
