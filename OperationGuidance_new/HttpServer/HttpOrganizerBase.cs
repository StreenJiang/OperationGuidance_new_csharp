namespace OperationGuidance_new.HttpServer {
    public abstract class HttpOrganizer {
        protected RestfulHttpServer? _restfulHttpServer;
        private string? _ip;
        private int? _port;

        public HttpOrganizer(string? ip, int? port = null) {
            _ip = ip;
            _port = port;
        }

        public RestfulHttpServer StartServer() {
            if (_port == null) {
                _restfulHttpServer = new(_ip);
            } else {
                _restfulHttpServer = new(_ip, _port.Value);
            }

            AddControllers();

            _restfulHttpServer.Start();
            return _restfulHttpServer;
        }

        protected abstract void AddControllers();
    }
}
