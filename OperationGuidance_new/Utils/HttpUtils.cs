using OperationGuidance_new.HttpObjects.AbstractClasses;
using Newtonsoft.Json;
using System.Text;
using log4net;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Utils {
    public static class HttpUtils {
        private static ILog logger = MainUtils.GetLogger(typeof(HttpUtils));

        public static async Task<V> SendPost<T, V>(string uri, T reqeust) where T : HttpRequestBase_WHYC where V : HttpResponseBase_WHYC, new() {
            logger.Info($"SendPost: uri = [{uri}]");

            V? response = new();
            using (HttpClient client = new()) {
                string json = JsonConvert.SerializeObject(reqeust);
                logger.Info($"SendPost: parameters = [{json}]");

                StringContent content = new(json, Encoding.UTF8, "application/json");
                using (HttpResponseMessage rspMsg = await client.PostAsync(uri, content)) {
                    logger.Info($"SendPost: rspMsg = [{JsonConvert.SerializeObject(rspMsg)}]");

                    if (rspMsg.IsSuccessStatusCode) {
                        string rspContent = await rspMsg.Content.ReadAsStringAsync();
                        logger.Info($"SendPost: rspContent = [{rspContent}]");

                        response = CommonUtils.CannotBeNull(JsonConvert.DeserializeObject<V>(rspContent));
                    }
                }

                return response;
            }
        }
    }
}
