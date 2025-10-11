using System.Net;
using System.Text;
using System.Text.Json;

namespace OperationGuidance_service.HttpServer {
    /// <summary>
    /// 基于 HttpListener 的轻量级 RESTful API 服务器
    /// </summary>
    public partial class RestfulHttpServer: IDisposable {
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
            } catch (Exception ex) {
                throw new InvalidOperationException($"无法启动服务器: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 停止服务器
        /// </summary>
        public void Stop() {
            if (!_isRunning) return;

            _isRunning = false;
            _listener.Stop();
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
                    Console.WriteLine($"服务器错误: {ex.Message}");
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

        // 辅助方法：写入 JSON 响应
        public static async Task WriteJsonResponse(HttpListenerResponse response, object data) {
            response.ContentType = "application/json";
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }

        public void Dispose() {
            Stop();
            _listener?.Close();
        }
    }
}
