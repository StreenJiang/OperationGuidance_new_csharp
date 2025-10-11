using OperationGuidance_service.HttpServer;
using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.Requests;

namespace OperationGuidance_service.Controllers {
    public static class SciiXtController {
        private const string PREFIX_SCII_XT = "api-xt";
        private const string SWITCH_RECIPE = $"/{PREFIX_SCII_XT}/mes/switch-recipe";
        private const string SWITCH_BATCH = $"/{PREFIX_SCII_XT}/mes/switch-batch";

        public static void AddControllers(RestfulHttpServer server) {
            // 切换配方
            server.AddPost(SWITCH_RECIPE, async (SwitchRecipeReq_SCII_XT req) => await Task.Run<Response_SCII_XT>(() => {
                // TODO: Do something to data
                string? recipeCode = req.recipeCode;
                int plcIndex = req.plcIndex;

                // TODO: Return result
                var response_XT = new Response_SCII_XT();
                response_XT.code = plcIndex + 10000000;
                response_XT.datalnfo = recipeCode + "_yeah!";

                return response_XT;
            }));

            // 切换批次
            server.AddPost(SWITCH_BATCH, async (SwitchBatchReq_SCII_XT req) => await Task.Run<Response_SCII_XT>(() => {
                // TODO: Do something to data
                string? batchNo = req.batchNo + "_Oh_Yeah~";

                // TODO: Return result
                var response_XT = new Response_SCII_XT();
                response_XT.datalnfo = batchNo;

                return response_XT;
            }));
        }
    }
}
