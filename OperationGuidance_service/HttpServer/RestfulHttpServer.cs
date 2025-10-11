using System.Net;
using System.Text;
using System.Text.Json;
using log4net;
using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Utils;

namespace OperationGuidance_service.HttpServer {
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

        public int Port { get; private set; }
        public bool IsRunning => _isRunning;

        public RestfulHttpServer(int port = 8080) {
            Port = port;
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://*:{port}/");
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

        // 辅助方法：读取 JSON 请求体
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

                var options = new JsonSerializerOptions {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    // 严格模式：不允许额外的属性（可选）
                    AllowTrailingCommas = false,
                    ReadCommentHandling = JsonCommentHandling.Disallow
                };

                return JsonSerializer.Deserialize<T>(json, options)
                       ?? throw new JsonException("JSON 反序列化返回 null，可能缺少必需的属性");
            } catch (JsonException ex) {
                // 重新抛出更友好的错误信息
                throw new ArgumentException($"JSON 格式错误或数据结构不符合要求: {ex.Message}", ex);
            } catch (NotSupportedException ex) {
                throw new ArgumentException($"JSON 数据类型不支持: {ex.Message}", ex);
            } catch (InvalidOperationException ex) {
                throw new ArgumentException($"JSON 数据验证失败: {ex.Message}", ex);
            }
        }

        // 辅助方法：写入 JSON 响应
        public static async Task WriteJsonResponse(HttpListenerResponse response, object data) {
            response.ContentType = "application/json";
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }

        // 辅助方法：处理异常
        private static async Task HandleException(HttpListenerResponse response, Exception ex) {
            if (ex is JsonException) {
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

        public void Dispose() {
            Stop();
            _listener?.Close();
        }
    }
}
