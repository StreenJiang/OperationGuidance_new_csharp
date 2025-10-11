namespace OperationGuidance_service.HttpServer {
    public abstract class HttpOrganizer {
        protected RestfulHttpServer? _restfulHttpServer;
        private int? _port;

        public HttpOrganizer(int? port = null) {
            _port = port;
        }

        public RestfulHttpServer StartServer() {
            if (_port == null) {
                _restfulHttpServer = new();
            } else {
                _restfulHttpServer = new(_port.Value);
            }

            AddControllers();

            _restfulHttpServer.Start();
            return _restfulHttpServer;
        }

        protected abstract void AddControllers();
    }
}
