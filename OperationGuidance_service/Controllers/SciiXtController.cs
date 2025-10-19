using log4net;
using OperationGuidance_service.Constants;
using OperationGuidance_service.HttpServer;
using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.Requests;

namespace OperationGuidance_service.Controllers {
    public static class SciiXtController {
        private static ILog log = LogManager.GetLogger(typeof(SciiXtController));
        private const string PREFIX_SCII_XT = "api-xt";
        private const string SWITCH_RECIPE = $"/{PREFIX_SCII_XT}/mes/switch-recipe";
        private const string SWITCH_BATCH = $"/{PREFIX_SCII_XT}/mes/switch-batch";

        public static void AddControllers(RestfulHttpServer server) {
            // 切换配方
            server.AddPost(SWITCH_RECIPE, async (SCII_XT_SwitchRecipeReq req) => await Task.Run<SCII_XT_Response>(() => {
                // TODO: Do something to data
                string? recipeCode = req.recipeCode;
                int plcIndex = req.plcIndex;

                // TODO: Return result
                var response_XT = new SCII_XT_Response();
                if (string.IsNullOrEmpty(recipeCode)) {
                    response_XT.code = (int) SCII_XT_ResponseCode.OK;
                } else {
                    response_XT.code = (int) SCII_XT_ResponseCode.ERROR;
                }
                response_XT.datalnfo = recipeCode + "_yeah!";

                return response_XT;
            }));

            // 切换批次
            server.AddPost(SWITCH_BATCH, async (SCII_XT_SwitchBatchReq req) => await Task.Run<SCII_XT_Response>(() => {
                var response_XT = new SCII_XT_Response();

                try {
                    // TODO: Do something to data
                    string? batchNo = req.batchNo + "_Oh_Yeah~";

                    // TODO: Return result
                    response_XT.code = (int) SCII_XT_ResponseCode.OK;
                    response_XT.datalnfo = batchNo;
                } catch (Exception ex) {
                    response_XT.code = (int) SCII_XT_ResponseCode.ERROR;

                    string errorMsg = $"Error while handling request [{SWITCH_BATCH}, req = {req.ToJson()}], error: ";
                    log.Error(errorMsg, ex);
                    response_XT.message = errorMsg + ex.Message;
                }

                return response_XT;
            }));
        }
    }
}
