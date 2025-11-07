using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using CustomLibrary.Utils;
using log4net;
using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Utils;
using Newtonsoft.Json;

namespace OperationGuidance_new.HttpServer {
    /// <summary>
    /// 基于 HttpListener 的轻量级 RESTful API 服务器
    /// </summary>
    public partial class RestfulHttpServer: IDisposable {
        private ILog log = LogManager.GetLogger(typeof(RestfulHttpServer));

        private readonly HttpListener _listener;
        private readonly Thread _listenerThread;
        private readonly Dictionary<string, RouteHandler> _routes;
        private readonly object _lock = new object();
        private bool _isRunning = false;

        // 委托定义 - 直接操作 HttpListenerResponse
        public delegate Task RouteHandler(HttpListenerRequest request, HttpListenerResponse response);

        public string? Ip { get; private set; }
        public int Port { get; private set; }
        public bool IsRunning => _isRunning;

        public RestfulHttpServer(string? ip, int port = 8080) {
            Ip = ip;
            Port = port;
            _listener = new HttpListener();
            if (string.IsNullOrEmpty(ip)) {
                _listener.Prefixes.Add($"http://*:{port}/");
            } else {
                _listener.Prefixes.Add($"http://{ip}:{port}/");
            }
            _routes = new Dictionary<string, RouteHandler>();
            _listenerThread = new Thread(HandleRequests) {
                IsBackground = true,
                Name = "HttpServerThread"
            };
        }

        /// <summary>
        /// 启动服务器
        /// </summary>
        public void Start() {
            if (_isRunning) return;

            try {
                _listener.Start();
                _isRunning = true;
                _listenerThread.Start();
                log.Info("Http server started...");


                // 获取本机真实 IP 地址（排除回环、虚拟网卡等）
                string localIp = string.IsNullOrEmpty(Ip) ? (GetLocalIpAddress() ?? "127.0.0.1") : Ip;
                // 构造可访问的 URL
                string accessibleUrl = $"http://{localIp}:{Port}/";
                // Log and show
                log.Info($"HTTP 服务器已启动，监听地址: {accessibleUrl}");
                WidgetUtils.ShowNoticePopUp($"HTTP 服务器已启动，监听地址: {accessibleUrl}");
            } catch (Exception ex) {
                SystemUtils.ShowWarningPopUp($"无法启动 Http 服务器: {ex.Message}");
                // throw new InvalidOperationException($"无法启动服务器: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 停止服务器
        /// </summary>
        public void Stop() {
            if (!_isRunning) return;

            _isRunning = false;
            _listener.Stop();
            log.Info("Http server stopped...");
            _listenerThread.Join(2000);
        }

        /// <summary>
        /// 添加 GET 路由
        /// </summary>
        public void AddGet(string path, RouteHandler handler) {
            AddRoute("GET", path, handler);
        }

        /// <summary>
        /// 添加 POST 路由
        /// </summary>
        public void AddPost(string path, RouteHandler handler) {
            AddRoute("POST", path, handler);
        }

        /// <summary>
        /// 添加 PUT 路由
        /// </summary>
        public void AddPut(string path, RouteHandler handler) {
            AddRoute("PUT", path, handler);
        }

        /// <summary>
        /// 添加 DELETE 路由
        /// </summary>
        public void AddDelete(string path, RouteHandler handler) {
            AddRoute("DELETE", path, handler);
        }

        private void AddRoute(string method, string path, RouteHandler handler) {
            lock (_lock) {
                string key = $"{method}:{path.ToLower()}";
                _routes[key] = handler;
            }
        }

        private async void HandleRequests() {
            while (_isRunning) {
                try {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => ProcessRequest(context));
                } catch (HttpListenerException) {
                    break;
                } catch (Exception ex) {
                    log.Warn($"Http 服务器错误: {ex.Message}");
                }
            }
        }

        private async Task ProcessRequest(HttpListenerContext context) {
            var request = context.Request;
            var response = context.Response;

            try {
                string routeKey = $"{request.HttpMethod}:{request.Url.AbsolutePath.ToLower()}";

                if (_routes.TryGetValue(routeKey, out var handler)) {
                    await handler(request, response);
                } else {
                    var matchedHandler = FindParameterizedRoute(request.HttpMethod, request.Url.AbsolutePath);
                    if (matchedHandler != null) {
                        await matchedHandler(request, response);
                    } else {
                        // 404 Not Found
                        response.StatusCode = 404;
                        await WriteJsonResponse(response, new { error = "API endpoint not found" });
                    }
                }
            } catch (Exception ex) {
                response.StatusCode = 500;
                await WriteJsonResponse(response, new { error = "Internal server error", details = ex.Message });
            } finally {
                response.Close();
            }
        }

        private RouteHandler FindParameterizedRoute(string method, string path) {
            var pathParts = path.ToLower().Split('/', StringSplitOptions.RemoveEmptyEntries);
            foreach (var route in _routes.Keys) {
                var routeParts = route.Split(':', 2);
                if (routeParts[0] != method) continue;

                var routePathParts = routeParts[1].Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (pathParts.Length == routePathParts.Length) {
                    bool matches = true;
                    for (int i = 0; i < pathParts.Length; i++) {
                        if (routePathParts[i].StartsWith("{") && routePathParts[i].EndsWith("}")) {
                            continue;
                        } else if (routePathParts[i] != pathParts[i]) {
                            matches = false;
                            break;
                        }
                    }
                    if (matches) {
                        return _routes[route];
                    }
                }
            }
            return null;
        }

        // 添加泛型路由方法
        public void AddGet<TResponse>(string path, Func<Task<TResponse>> handler)
          where TResponse : HttpResponse {
            AddGet(path, async (request, response) => {
                try {
                    var result = await handler();
                    await WriteJsonResponse(response, result);
                } catch (Exception ex) {
                    await HandleException(response, ex);
                }
            });
        }

        public void AddPost<TRequest, TResponse>(string path, Func<TRequest, Task<TResponse>> handler)
          where TRequest : HttpRequest where TResponse : HttpResponse {
            AddPost(path, async (request, response) => {
                try {
                    var requestData = await ReadJsonRequestBody<TRequest>(request);
                    var result = await handler(requestData);
                    await WriteJsonResponse(response, result);
                } catch (Exception ex) {
                    await HandleException(response, ex);
                }
            });
        }

        public void AddPut<TRequest, TResponse>(string path, Func<TRequest, Task<TResponse>> handler)
          where TRequest : HttpRequest where TResponse : HttpResponse {
            AddPut(path, async (request, response) => {
                try {
                    var requestData = await ReadJsonRequestBody<TRequest>(request);
                    var result = await handler(requestData);
                    await WriteJsonResponse(response, result);
                } catch (Exception ex) {
                    await HandleException(response, ex);
                }
            });
        }

        public void AddDelete<TResponse>(string path, Func<Task<TResponse>> handler)
          where TResponse : HttpResponse {
            AddDelete(path, async (request, response) => {
                try {
                    var result = await handler();
                    response.StatusCode = 204; // No Content
                    response.ContentLength64 = 0;
                } catch (Exception ex) {
                    await HandleException(response, ex);
                }
            });
        }


        // 辅助方法：读取 JSON 请求体（Newtonsoft.Json 版本）
        public static async Task<T> ReadJsonRequestBody<T>(HttpListenerRequest request) {
            if (!request.HasEntityBody) {
                throw new ArgumentException("请求体为空，但需要提供 JSON 数据");
            }

            try {
                using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                string json = await reader.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(json)) {
                    throw new ArgumentException("请求体包含空或无效的 JSON 数据");
                }

                // 配置 Newtonsoft.Json 反序列化选项
                var settings = new JsonSerializerSettings {
                    // 大小写不敏感（默认行为，但显式设置更清晰）
                    MissingMemberHandling = MissingMemberHandling.Ignore, // 忽略 JSON 中多余的字段
                    NullValueHandling = NullValueHandling.Ignore,
                    // 可选：如果你希望更严格，可以设为 Error
                    // MissingMemberHandling = MissingMemberHandling.Error,

                    // 可选：自定义合同解析器（如需 camelCase 序列化，但反序列化仍大小写不敏感）
                    // ContractResolver = new CamelCasePropertyNamesContractResolver()
                };

                // 反序列化
                T result = JsonConvert.DeserializeObject<T>(json, settings);
                if (result == null) {
                    throw new Newtonsoft.Json.JsonException("JSON 反序列化返回 null，可能缺少必需的属性或根节点格式错误");
                }

                return result;
            } catch (JsonReaderException ex) {
                throw new ArgumentException($"JSON 格式无效: {ex.Message}", ex);
            } catch (JsonSerializationException ex) {
                throw new ArgumentException($"JSON 数据结构与目标类型不匹配: {ex.Message}", ex);
            } catch (Newtonsoft.Json.JsonException ex) {
                throw new ArgumentException($"JSON 处理错误: {ex.Message}", ex);
            }
        }

        // 辅助方法：写入 JSON 响应
        public static async Task WriteJsonResponse(HttpListenerResponse response, object data) {
            response.ContentType = "application/json";
            string json = System.Text.Json.JsonSerializer.Serialize(data, new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }

        // 辅助方法：处理异常
        private static async Task HandleException(HttpListenerResponse response, Exception ex) {
            if (ex is Newtonsoft.Json.JsonException || ex is System.Text.Json.JsonException) {
                response.StatusCode = 400;
                await WriteJsonResponse(response, new { error = "Invalid JSON format" });
            } else if (ex is ArgumentException) {
                response.StatusCode = 400;
                await WriteJsonResponse(response, new { error = ex.Message });
            } else {
                response.StatusCode = 500;
                await WriteJsonResponse(response, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// 获取本机真实 IP 地址（非 127.0.0.1，非虚拟网卡）
        /// </summary>
        private string GetLocalIpAddress() {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces()) {
                // 跳过未启用或回环接口
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses) {
                    // 只取 IPv4 地址（IPv6 可选支持）
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork && !ip.Address.IsIPv6LinkLocal) {
                        return ip.Address.ToString();
                    }
                }
            }

            // 如果没找到，返回 localhost（安全兜底）
            return "127.0.0.1";
        }

        public void Dispose() {
            Stop();
            _listener?.Close();
        }
    }
}
