using System.Data;
using Microsoft.Data.Sqlite;
using OperationGuidance_service.Attributes;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Database;
using OperationGuidance_service.Models;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Requests;
using OperationGuidance_service.Models.Responses;
using OperationGuidance_service.Utils;

namespace OperationGuidance_service.Apis
{
    [Api]
    public sealed class OperationGuidanceApis {
        [Autowired]
        public UserAccountInfoService _userAccountInfoService;
        [Autowired]
        public ProductMissionService _productMissionService;
        [Autowired]
        public ProductSideService _productSideService;
        [Autowired]
        public ProductService _productService;
        [Autowired]
        public ProductBoltService _productBoltService;
        [Autowired]
        public DeviceService _deviceService;
        [Autowired]
        public DeviceCategoryService _deviceCategoryService;
        [Autowired]
        public DeviceTypeService _deviceTypeService;
        [Autowired]
        public BrandService _brandService;

        public QueryProductMissionListRsp QueryProductMissionListRsp(QueryProductMissionListReq req) {
            // 先查询任务清单
            List<ProductMission> missions = _productMissionService.QueryList(req.UserId);
            List<ProductMissionDTO> productMissionDTOs = new();
            CommonUtils.ObjectConverter<ProductMission, ProductMissionDTO>(missions, productMissionDTOs);

            // 根据任务查询关联的其他表
            for (int i = 0; i < missions.Count; i++) {
                ProductMission mission = missions[i];
                ProductMissionDTO productMissionDTO = productMissionDTOs[i];

                // 如果产品id不为空
                int? productId = mission.product_id;
                if (productId != null) {
                    Product? product = _productService.FindById(productId.Value);
                    if (product != null) {
                        productMissionDTO.product_name = product.name;
                        productMissionDTO.product_description = product.description;
                    }
                }

                // 根据mission_id查询产品面列表
                List<ProductSide> sides = _productSideService.FindBySqlCondition($"mission_id = {mission.id}");
                if (sides.Count > 0) {
                    // 将产品面信息存入任务对象
                    List<ProductSideDTO> productSideDTOs = new();
                    CommonUtils.ObjectConverter<ProductSide, ProductSideDTO>(sides, productSideDTOs);
                    productMissionDTO.ProductSides = productSideDTOs;

                    // 循环产品面查询螺栓点位信息
                    for (int j = 0; j < sides.Count; j++) {
                        ProductSide side = sides[j];
                        ProductSideDTO productSideDTO = productSideDTOs[j];

                        List<ProductBolt> bolts = _productBoltService.FindBySqlCondition($"side_id = {side.id}");
                        if (bolts.Count > 0) {
                            // 将螺栓点位信息存入产品面对象
                            List<ProductBoltDTO> productBoltDTOs = new();
                            CommonUtils.ObjectConverter<ProductBolt, ProductBoltDTO>(bolts, productBoltDTOs);
                            productSideDTO.Bolts = productBoltDTOs;

                            // 循环每个螺栓点位查询工具信息
                            for (int k = 0; k < bolts.Count; k++) {
                                int? toolId = bolts[k].tool_id;
                                if (toolId != null) {
                                    //Tool? tool = _toolService.FindById(toolId.Value);
                                    //if (tool != null) {
                                    //    productBoltDTOs[i].ToolName = tool.Name;
                                    //    productBoltDTOs[i].ToolDescription = tool.Description;
                                    //}
                                    Device? device = _deviceService.FindById(toolId.Value);
                                    if (device != null) {
                                        productBoltDTOs[i].tool_name = device.name;
                                        productBoltDTOs[i].tool_description = device.description;
                                        productBoltDTOs[i].tool_ip = device.ip;
                                        productBoltDTOs[i].tool_port = device.port;

                                        DeviceModel? type = _deviceTypeService.FindById(device.model_id);
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

            QueryProductMissionListRsp rsp = new() {
                ProductMissionsDTOs = productMissionDTOs
            };
            return rsp;
        }

        public AddOrUpdateProductMissionRsp AddOrUpdateProductMission(AddOrUpdateProductMissionReq req) {
            AddOrUpdateProductMissionRsp rsp = new();
            // 使用同一个connection确保当前所有操作都在同一个事务下
            using SqliteConnection conn = DbConnector.GetConnection();
            // 开启事务
            using (SqliteTransaction transaction = conn.BeginTransaction()) {
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

        public QueryDeviceListRsp QueryDeviceList(QueryDeviceListReq req) {
            // 先查询Device表
            List<Device> devices = _deviceService.QueryList(req.UserId);
            List<DeviceDTO> deviceDTOs = new();
            CommonUtils.ObjectConverter<Device, DeviceDTO>(devices, deviceDTOs);

            // 遍历Device清单，查询DeviceType、DeviceCategory、Brand
            for (int i = 0; i < deviceDTOs.Count; i++) {
                Device device = devices[i];
                DeviceDTO deviceDTO = deviceDTOs[i];
                // Device type
                DeviceModel? type = _deviceTypeService.FindById(device.model_id);
                if (type != null) {
                    deviceDTO.device_type_id = device.model_id;
                    deviceDTO.device_type_name = type.name;

                    // Device category
                    DeviceCategory? category = _deviceCategoryService.FindById(type.category_id);
                    if (category != null) {
                        deviceDTO.device_category_id = category.id;
                        deviceDTO.device_category_name = category.name;
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

            QueryDeviceListRsp rsp = new() {
                DeviceDTOs = deviceDTOs
            };
            return rsp;
        }


    }
}
