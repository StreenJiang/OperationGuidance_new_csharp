using OperationGuidance_new.HttpObjects.AbstractClasses;
using Newtonsoft.Json;
using System.Text;
using log4net;
using OperationGuidance_service.Utils;
using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_new.Utils {
    public static class HttpUtils {
        private static ILog logger = MainUtils.GetLogger(typeof(HttpUtils));

        public static async Task<V> SendPost_WHYC<T, V>(string uri, T reqeust, IDictionary<string, string>? headers = null)
          where T : HttpRequestBase_WHYC where V : HttpResponseBase_WHYC, new() {
            logger.Info($"SendPost: uri = [{uri}]");

            V? response = new();
            using (HttpClient client = new()) {
                if (headers != null) {
                    foreach (var header in headers) {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }

                string json = JsonConvert.SerializeObject(reqeust);
                logger.Info($"SendPost: parameters = [{json}]");

                StringContent content = new(json, Encoding.UTF8, "application/json");
                using (HttpResponseMessage rspMsg = await client.PostAsync(uri, content)) {
                    logger.Info($"SendPost: rspMsg = [{JsonConvert.SerializeObject(rspMsg)}]");

                    if (rspMsg.IsSuccessStatusCode) {
                        string rspContent = await rspMsg.Content.ReadAsStringAsync();
                        logger.Info($"SendPost: rspContent = [{rspContent}]");

                        response = CommonUtils.CannotBeNull(JsonConvert.DeserializeObject<V>(rspContent));
                    } else {
                        logger.Warn($"POST request failed: {rspMsg.StatusCode}");
                        return new V();
                    }
                }

                return response;
            }
        }

        public static async Task<V> SendPost_SCII_XT<T, V>(string uri, T reqeust, IDictionary<string, string>? headers = null)
          where T : HttpRequestBase_SCII_XT where V : SCII_XT_Response, new() {
            logger.Info($"SendPost: uri = [{uri}]");

            V? response = new();
            using (HttpClient client = new()) {
                if (headers != null) {
                    foreach (var header in headers) {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }
                // client.Timeout = TimeSpan.FromSeconds(10);

                string json = JsonConvert.SerializeObject(reqeust);
                logger.Info($"SendPost: parameters = [{json}]");

                StringContent content = new(json, Encoding.UTF8, "application/json");
                using (HttpResponseMessage rspMsg = await client.PostAsync(uri, content)) {
                    logger.Info($"SendPost: rspMsg = [{JsonConvert.SerializeObject(rspMsg)}]");

                    if (rspMsg.IsSuccessStatusCode) {
                        string rspContent = await rspMsg.Content.ReadAsStringAsync();
                        logger.Info($"SendPost: rspContent = [{rspContent}]");

                        response = CommonUtils.CannotBeNull(JsonConvert.DeserializeObject<V>(rspContent));
                    } else {
                        logger.Warn($"POST request failed: {rspMsg.StatusCode}");
                        return new V();
                    }
                }

                return response;
            }
        }

        public static async Task<V> SendGet_SCII_XT<V>(string uri, IDictionary<string, string>? headers = null)
          where V : SCII_XT_Response, new() {
            logger.Info($"SendGet: uri = [{uri}]");

            using (HttpClient client = new()) {
                if (headers != null) {
                    foreach (var header in headers) {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }

                using (HttpResponseMessage rspMsg = await client.GetAsync(uri)) {
                    logger.Info($"SendGet: StatusCode = [{rspMsg.StatusCode}]");

                    if (rspMsg.IsSuccessStatusCode) {
                        string rspContent = await rspMsg.Content.ReadAsStringAsync();
                        logger.Info($"SendGet: rspContent = [{rspContent}]");

                        var response = JsonConvert.DeserializeObject<V>(rspContent);
                        return CommonUtils.CannotBeNull(response);
                    } else {
                        // 可选：根据业务决定是否抛异常，或返回默认 V
                        // 这里保持和 POST 一致：返回 new V() 但日志警告
                        logger.Warn($"GET request failed: {rspMsg.StatusCode}");
                        return new V();
                    }
                }
            }
        }
    }
}
