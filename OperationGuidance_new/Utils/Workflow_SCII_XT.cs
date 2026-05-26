using log4net;
using Newtonsoft.Json;
using OperationGuidance_new.Configs;
using OperationGuidance_new.HttpObjects.Requests.SCII_XT;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_new.Utils {
    public static class Workflow_SCII_XT {
        private static ILog log = LogManager.GetLogger(typeof(Workflow_SCII_XT));
        private static string RequestPrefix;

        static Workflow_SCII_XT() {
            SciiXtConfig config = ConfigUtils.LoadConfig<SciiXtConfig>();
            var httpHost = config.http_host;
            if (string.IsNullOrEmpty(httpHost)) {
                log.Warn("未配置 MES 服务器地址，请检查配置（将使用默认写死的测试服务器地址）。");
                RequestPrefix = "http://10.10.59.1:5400";
            } else {
                RequestPrefix = httpHost;
            }
        }

        // 1. 员工登录
        public static async Task<SCII_XT_OperatorLoginDTO> OperatorLogin(SCII_XT_OperatorLoginReq req) {
            var api = "/api/employee/login";
            var result = new SCII_XT_OperatorLoginDTO();

            try {
                var rsp = await HttpUtils.SendPost_SCII_XT<SCII_XT_OperatorLoginReq, SCII_XT_Response>(RequestPrefix + api, req);
                result.loginSuccess = rsp.code == (int) SCII_XT_ResponseCode.OK;
                if (result.loginSuccess && rsp.dataInfo != null) {
                    result.userId = Convert.ToInt32(rsp.dataInfo);
                }
                result.message = rsp.message;
            } catch (Exception ex) {
                result.loginSuccess = false;
                result.message = ex.Message;
            }

            return result;
        }

        // *1.1. 获取员工信息，用于存储到本地全局变量中
        public static async Task<SCII_XT_UserInfoDTO?> UserInfo(int userId) {
            var api = "/api/employee";
            SCII_XT_UserInfoDTO? result = new SCII_XT_UserInfoDTO();

            try {
                var rsp = await HttpUtils.SendGet_SCII_XT<SCII_XT_Response>(RequestPrefix + api + $"/{userId}");
                if (rsp.code == (int) SCII_XT_ResponseCode.OK) {
                    if (rsp.dataInfo != null) {
                        result = JsonConvert.DeserializeObject<SCII_XT_UserInfoDTO>((string) rsp.dataInfo);
                    }
                }
                result.message = rsp.message;
            } catch (Exception ex) {
                result.message = ex.Message;
            }

            return result;
        }

        // 2. 权限获取(客户要求权限就根据获取的权限做判断,不要求就无需请求)
        public static async Task<SCII_XT_UserPermissionDTO?> UserPermissions(int userId) {
            var api = "/api/employee/permissions";
            SCII_XT_UserPermissionDTO? result = new SCII_XT_UserPermissionDTO();

            try {
                var rsp = await HttpUtils.SendGet_SCII_XT<SCII_XT_Response>(RequestPrefix + api + $"/{userId}");
                if (rsp.code == (int) SCII_XT_ResponseCode.OK) {
                    if (rsp.dataInfo != null) {
                        result = JsonConvert.DeserializeObject<SCII_XT_UserPermissionDTO>((string) rsp.dataInfo);
                    }
                }
                result.message = rsp.message;
            } catch (Exception ex) {
                result.message = ex.Message;
            }

            return result;
        }

        // 3. 进站
        public static async Task<SCII_XT_InOrOutBoundStationDTO> InBoundStation(SCII_XT_InOrOutBoundStationReq req) {
            var api = "/api/station-control/inbound";
            var result = new SCII_XT_InOrOutBoundStationDTO();

            try {
                var rsp = await HttpUtils.SendPost_SCII_XT<SCII_XT_InOrOutBoundStationReq, SCII_XT_Response>(RequestPrefix + api, req);
                result.inOrOutSuccess = rsp.code == (int) SCII_XT_ResponseCode.OK;
                result.message = rsp.message;
            } catch (Exception ex) {
                result.inOrOutSuccess = false;
                result.message = ex.Message;
            }

            return result;
        }

        // INFO: 上传配件绑定
        public static async Task<SCII_XT_BindAccessoryDTO> BindAccessory(SCII_XT_BindAccessoryReq req) {
            var api = "/api/product-accessory/product/bind";
            var result = new SCII_XT_BindAccessoryDTO();

            try {
                var rsp = await HttpUtils.SendPost_SCII_XT<SCII_XT_BindAccessoryReq, SCII_XT_Response>(RequestPrefix + api, req);
                result.bindSuccess = rsp.code == (int) SCII_XT_ResponseCode.OK;
                result.message = rsp.message;
            } catch (Exception ex) {
                result.bindSuccess = false;
                result.message = ex.Message;
            }

            return result;
        }

        // 4. 绑定上盖（特殊）
        [Obsolete("This is not the one, use [BindAccessory] instead.")]
        public static async Task<SCII_XT_BindUpperCoverDTO> BindUppderCover(SCII_XT_BindUpperCoverReq req) {
            var api = "/api/product/upper-cover/bind";
            var result = new SCII_XT_BindUpperCoverDTO();

            try {
                var rsp = await HttpUtils.SendPost_SCII_XT<SCII_XT_BindUpperCoverReq, SCII_XT_Response>(RequestPrefix + api, req);
                result.bindSuccess = rsp.code == (int) SCII_XT_ResponseCode.OK;
                result.message = rsp.message;
            } catch (Exception ex) {
                result.bindSuccess = false;
                result.message = ex.Message;
            }

            return result;
        }

        // 上盖码 → 追溯码
        public static async Task<string?> GetUpperCode(string productCode) {
            var api = $"/api/product/GetUpperCode/{productCode}";

            try {
                var rsp = await HttpUtils.SendGet_SCII_XT<SCII_XT_Response>(RequestPrefix + api);
                if (rsp.code == (int) SCII_XT_ResponseCode.OK && rsp.dataInfo != null) {
                    return rsp.dataInfo.ToString();
                }
                log.Warn($"GetUpperCode 失败，productCode = [{productCode}]，code = [{rsp.code}]，message = [{rsp.message}]");
                return null;
            } catch (Exception ex) {
                log.Error($"GetUpperCode 异常，productCode = [{productCode}]", ex);
                return null;
            }
        }

        // 5. 产品数据绑定(绑定扭力枪的数据)
        public static async Task<SCII_XT_BindProductDataDTO> BindProductData(SCII_XT_BindProductDataReq req) {
            var api = "/api/product-data/bind";
            var result = new SCII_XT_BindProductDataDTO();

            try {
                var rsp = await HttpUtils.SendPost_SCII_XT<SCII_XT_BindProductDataReq, SCII_XT_Response>(RequestPrefix + api, req);
                result.bindSuccess = rsp.code == (int) SCII_XT_ResponseCode.OK;
                result.message = rsp.message;
            } catch (Exception ex) {
                result.bindSuccess = false;
                result.message = ex.Message;
            }

            return result;
        }

        // 6. 出站
        public static async Task<SCII_XT_InOrOutBoundStationDTO> OutBoundStation(SCII_XT_InOrOutBoundStationReq req) {
            var api = "/api/station-control/outbound";
            var result = new SCII_XT_InOrOutBoundStationDTO();

            try {
                var rsp = await HttpUtils.SendPost_SCII_XT<SCII_XT_InOrOutBoundStationReq, SCII_XT_Response>(RequestPrefix + api, req);
                result.inOrOutSuccess = rsp.code == (int) SCII_XT_ResponseCode.OK;
                result.message = rsp.message;
            } catch (Exception ex) {
                result.inOrOutSuccess = false;
                result.message = ex.Message;
            }

            return result;
        }

        // 7. 设备点检数据上报
        public static async Task<EquipmentCheckDTO> EquipmentCheck(EquipmentCheckReq req) {
            var api = "/check";
            var result = new EquipmentCheckDTO();

            try {
                var rsp = await HttpUtils.SendPost_SCII_XT<EquipmentCheckReq, SCII_XT_Response>(RequestPrefix + api, req);
                result.checkSuccess = rsp.code == (int) SCII_XT_ResponseCode.OK;
                result.message = rsp.message;
            } catch (Exception ex) {
                result.checkSuccess = false;
                result.message = ex.Message;
            }

            return result;
        }
    }
}
