using log4net;
using OperationGuidance_new.Configs;
using OperationGuidance_new.HttpServer;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.Requests;

namespace OperationGuidance_new.HttpObjects {
    public static class SciiXtController {
        private static ILog log = LogManager.GetLogger(typeof(SciiXtController));
        private const string SWITCH_RECIPE = "/mes/switch-recipe";
        private const string SWITCH_BATCH = "/mes/switch-batch";

        public static Action<string>? ActionAfterReceivedRecipe;
        public static Action<string>? ActionAfterReceivedBatch;

        public static void AddControllers(RestfulHttpServer server) {
            // 切换配方
            server.AddPost(SWITCH_RECIPE, async (SCII_XT_SwitchRecipeReq req) => await Task.Run<SCII_XT_Response>(() => {
                var response_XT = new SCII_XT_Response();
                log.Info($"接收到配方变更请求，请求内容：{req.ToJson()}");

                try {
                    if (!string.IsNullOrEmpty(req.recipeCode)) {
                        ConfigUtils.LoadConfig<SciiXtConfig>().File.Write(ConfigName_SCII_XT.RecipeCode, req.recipeCode);
                        response_XT.code = (int) SCII_XT_ResponseCode.OK;
                        response_XT.message = "配方切换成功！";

                        ActionAfterReceivedRecipe?.Invoke(req.recipeCode);
                    } else {
                        response_XT.code = (int) SCII_XT_ResponseCode.ERROR;
                        response_XT.message = $"请求中包含的配方编码为空。request body：{req.ToJson()}";
                    }
                } catch (Exception ex) {
                    response_XT.code = (int) SCII_XT_ResponseCode.ERROR;

                    string errorMsg = $"Error while handling request [{SWITCH_RECIPE}, request body = {req.ToJson()}], error: ";
                    log.Error(errorMsg, ex);
                    response_XT.message = errorMsg + ex.Message;
                }

                return response_XT;
            }));

            // 切换批次
            server.AddPost(SWITCH_BATCH, async (SCII_XT_SwitchBatchReq req) => await Task.Run<SCII_XT_Response>(() => {
                var response_XT = new SCII_XT_Response();
                log.Info($"接收到批次变更请求，请求内容：{req.ToJson()}");

                try {
                    if (!string.IsNullOrEmpty(req.batchNo)) {
                        ConfigUtils.LoadConfig<SciiXtConfig>().File.Write(ConfigName_SCII_XT.BatchNo, req.batchNo);
                        response_XT.code = (int) SCII_XT_ResponseCode.OK;
                        response_XT.message = "批次切换成功！";

                        ActionAfterReceivedBatch?.Invoke(req.batchNo);
                    } else {
                        response_XT.code = (int) SCII_XT_ResponseCode.ERROR;
                        response_XT.message = $"请求中包含的批次号为空。request body：{req.ToJson()}";
                    }
                } catch (Exception ex) {
                    response_XT.code = (int) SCII_XT_ResponseCode.ERROR;

                    string errorMsg = $"Error while handling request [{SWITCH_BATCH}, request body = {req.ToJson()}], error: ";
                    log.Error(errorMsg, ex);
                    response_XT.message = errorMsg + ex.Message;
                }

                return response_XT;
            }));
        }
    }
}
